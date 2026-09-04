using System;
using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

using SW.Base;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Backrooms.Assembly;
using ProjectR.Backrooms.Generation;
using ProjectR.Backrooms.Lighting;
using ProjectR.Enum;

namespace ProjectR.Backrooms.Debugging
{
    /// <summary>
    /// 빌드한 실행 파일이 라이트맵을 제대로 살려 내는지 명령줄 인자로 자동 확인하는 도구입니다.
    /// </summary>
    /// <remarks>
    /// 프리팹에 구워 넣은 라이트맵은 씬에 속한 텍스처가 아니라서 빌드에서 참조가 끊길 수 있고,
    /// 그러면 실기에서 맵이 새까맣게 나옵니다. 에디터 플레이 모드로는 이 위험을 확인할 수 없어
    /// 빌드한 실행 파일에서 직접 백룸에 들어가 라이트맵 등록 현황을 로그로 남기고 화면을 찍습니다.
    /// 인자가 없으면 아무 일도 하지 않으므로 평소 실행에는 영향을 주지 않습니다.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "ProjectR.Backrooms.Measurement", sourceAssembly: "ProjectR.Backrooms", sourceClassName: "BackroomsBuildVerifier")]
    public class BackroomsBuildVerifier : SWMonoBehaviour
    {
        #region 필드
        /// <summary>확인 절차를 시작시키는 명령줄 인자입니다.</summary>
        private const string VerifyArgument = "-projectr-verify";

        /// <summary>확인 뒤 이어서 돌릴 재생성 측정 횟수를 지정하는 인자입니다.</summary>
        private const string SoakArgument = "-projectr-soak";

        /// <summary>화면을 찍어 저장할 경로를 지정하는 인자입니다.</summary>
        private const string ScreenshotArgument = "-projectr-screenshot";

        /// <summary>맵 조립이 끝난 뒤 화면이 안정될 때까지 기다리는 시간(초)입니다.</summary>
        [SerializeField, Min(0f), Tooltip("맵 조립이 끝난 뒤 화면이 안정될 때까지 기다리는 시간(초)입니다.")]
        private float settleSeconds = 2f;

        /// <summary>백룸 진입과 조립을 기다리는 최대 시간(초)입니다.</summary>
        [SerializeField, Min(1f), Tooltip("백룸 진입과 조립을 기다리는 최대 시간(초)입니다.")]
        private float timeoutSeconds = 180f;

        /// <summary>카메라를 놓을 눈높이(미터)입니다.</summary>
        [SerializeField, Min(0.1f), Tooltip("카메라를 놓을 눈높이(미터)입니다.")]
        private float eyeHeight = 1.6f;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 명령줄에 확인 인자가 있으면 확인 절차를 시작합니다.
        /// </summary>
        private void Start()
        {
            if (FindArgumentIndex(VerifyArgument) < 0) return;

            StartCoroutine(VerifyRoutine());
        }

        /// <summary>
        /// 백룸에 들어가 라이트맵 상태를 확인하고 결과에 맞는 종료 코드로 빠져나갑니다.
        /// </summary>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator VerifyRoutine()
        {
            LogEnvironment();

            if (GameManager.Instance.BeginActivity(new BackroomsActivity()) == false)
            {
                Finish(2, "백룸 활동을 시작하지 못했습니다.");
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            BackroomsMapBuilder mapBuilder = null;

            while (mapBuilder == null)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Finish(2, "제한 시간 안에 백룸 씬으로 들어가지 못했습니다.");
                    yield break;
                }

                mapBuilder = FindAnyObjectByType<BackroomsMapBuilder>();

                yield return null;
            }

            while (mapBuilder.LastResult == null || mapBuilder.IsBuilding)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Finish(2, "제한 시간 안에 맵 조립이 끝나지 않았습니다.");
                    yield break;
                }

                yield return null;
            }

            PlaceCameraAtStart(mapBuilder);

            yield return new WaitForSeconds(settleSeconds);

            BackroomsLightingReport report = BackroomsLightingReport.Capture();

            SWLog.Log($"[{nameof(BackroomsBuildVerifier)}] {mapBuilder.LastResult.ToSummary()}");
            SWLog.Log($"[{nameof(BackroomsBuildVerifier)}] {report.ToSummary()}");
            SWLog.Log($"[{nameof(BackroomsBuildVerifier)}] 라이트맵 상세\n{report.ToTextureDetail()}");

            yield return CaptureScreenshotRoutine();

            yield return RunSoakRoutine(mapBuilder);

            Finish(report.IsHealthy ? 0 : 1, report.ToSummary());
        }

        /// <summary>
        /// 인자로 지정한 횟수만큼 재생성 메모리 측정을 이어서 돌립니다.
        /// </summary>
        /// <param name="mapBuilder">측정 도구가 붙어 있는 맵 조립 컴포넌트입니다.</param>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator RunSoakRoutine(BackroomsMapBuilder mapBuilder)
        {
            int cycleCount = ReadArgumentValue(SoakArgument, 0);

            if (cycleCount < 1) yield break;

            BackroomsSoakRunner soakRunner = mapBuilder.GetComponent<BackroomsSoakRunner>();

            if (soakRunner == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsBuildVerifier)}] 측정 도구가 없어 재생성 측정을 건너뜁니다.");
                yield break;
            }

            soakRunner.StartSoak(cycleCount);

            while (soakRunner.IsSoaking) yield return null;
        }

        /// <summary>
        /// 확인이 이루어진 기기와 화면 설정을 로그로 남깁니다.
        /// </summary>
        private void LogEnvironment()
        {
            SWLog.Log($"[{nameof(BackroomsBuildVerifier)}] 빌드 확인을 시작합니다. " +
                $"{Application.platform} / {SystemInfo.operatingSystem}");
            SWLog.Log($"[{nameof(BackroomsBuildVerifier)}] GPU {SystemInfo.graphicsDeviceName} " +
                $"({SystemInfo.graphicsDeviceType}) / CPU {SystemInfo.processorType} / " +
                $"RAM {SystemInfo.systemMemorySize}MB");
            SWLog.Log($"[{nameof(BackroomsBuildVerifier)}] 해상도 {Screen.width}x{Screen.height} / " +
                $"품질 {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        }

        /// <summary>
        /// 카메라를 미로 시작 칸의 눈높이로 옮기고 열린 통로 쪽을 보게 합니다.
        /// </summary>
        /// <param name="mapBuilder">위치를 계산할 맵 조립 컴포넌트입니다.</param>
        private void PlaceCameraAtStart(BackroomsMapBuilder mapBuilder)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsBuildVerifier)}] 주 카메라가 없어 위치를 옮기지 못했습니다.");
                return;
            }

            // 플레이어가 있으면 카메라는 플레이어에 매달려 있고 시작 칸 배치도 이미 끝났습니다.
            // 여기서 카메라를 따로 옮기면 다음 프레임에 되돌아가므로 건드리지 않습니다.
            if (GameObject.FindGameObjectWithTag("Player") != null) return;

            MazeBuildResult result = mapBuilder.LastResult;
            EMazeDirection openings = EMazeDirection.All & ~result.Grid.GetWalls(result.StartCoordinate);

            mainCamera.transform.SetPositionAndRotation(
                mapBuilder.GetWorldPosition(result.StartCoordinate) + Vector3.up * eyeHeight,
                Quaternion.Euler(0f, GetYaw(openings), 0f));
        }

        /// <summary>
        /// 열린 통로 조합에서 바라볼 방향의 Y축 회전값을 구합니다.
        /// </summary>
        /// <param name="openings">칸에서 열려 있는 방향의 조합입니다.</param>
        /// <returns>북쪽을 0도로 보는 Y축 회전값입니다.</returns>
        private static float GetYaw(EMazeDirection openings)
        {
            if ((openings & EMazeDirection.North) != 0) return 0f;
            if ((openings & EMazeDirection.East) != 0) return 90f;
            if ((openings & EMazeDirection.South) != 0) return 180f;
            if ((openings & EMazeDirection.West) != 0) return 270f;

            return 0f;
        }

        /// <summary>
        /// 인자로 받은 경로에 화면을 찍어 저장합니다. 경로 인자가 없으면 건너뜁니다.
        /// </summary>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator CaptureScreenshotRoutine()
        {
            int argumentIndex = FindArgumentIndex(ScreenshotArgument);
            string[] arguments = Environment.GetCommandLineArgs();

            if (argumentIndex < 0 || argumentIndex + 1 >= arguments.Length) yield break;

            string screenshotPath = arguments[argumentIndex + 1];
            string directoryPath = Path.GetDirectoryName(screenshotPath);

            if (string.IsNullOrEmpty(directoryPath) == false) Directory.CreateDirectory(directoryPath);

            ScreenCapture.CaptureScreenshot(screenshotPath);

            // 화면 캡처는 다음 프레임 이후에 파일로 기록되므로 저장이 끝날 때까지 기다립니다.
            float deadline = Time.realtimeSinceStartup + 10f;

            while (File.Exists(screenshotPath) == false && Time.realtimeSinceStartup < deadline)
                yield return null;

            SWLog.Log($"[{nameof(BackroomsBuildVerifier)}] 화면을 저장했습니다: {screenshotPath} " +
                $"({(File.Exists(screenshotPath) ? "성공" : "실패")})");
        }

        /// <summary>
        /// 확인 결과를 남기고 지정한 종료 코드로 실행을 끝냅니다.
        /// </summary>
        /// <param name="exitCode">0은 정상, 1은 라이트맵 실패, 2는 절차 실패입니다.</param>
        /// <param name="reason">로그에 남길 사유입니다.</param>
        private void Finish(int exitCode, string reason)
        {
            SWLog.Log($"[{nameof(BackroomsBuildVerifier)}] 빌드 확인을 끝냅니다. 종료 코드 {exitCode} / {reason}");

            Application.Quit(exitCode);
        }

        /// <summary>
        /// 명령줄 인자의 위치를 찾습니다.
        /// </summary>
        /// <param name="argumentName">찾을 인자 이름입니다.</param>
        /// <returns>인자의 위치입니다. 없으면 -1을 반환합니다.</returns>
        private static int FindArgumentIndex(string argumentName)
        {
            string[] arguments = Environment.GetCommandLineArgs();

            for (int index = 0; index < arguments.Length; index += 1)
            {
                if (string.Equals(arguments[index], argumentName, StringComparison.OrdinalIgnoreCase)) return index;
            }

            return -1;
        }

        /// <summary>
        /// 명령줄 인자 뒤에 붙은 정수 값을 읽습니다.
        /// </summary>
        /// <param name="argumentName">읽을 인자 이름입니다.</param>
        /// <param name="defaultValue">인자가 없거나 숫자가 아닐 때 쓸 기본값입니다.</param>
        /// <returns>읽어 낸 값입니다.</returns>
        private static int ReadArgumentValue(string argumentName, int defaultValue)
        {
            int argumentIndex = FindArgumentIndex(argumentName);
            string[] arguments = Environment.GetCommandLineArgs();

            if (argumentIndex < 0 || argumentIndex + 1 >= arguments.Length) return defaultValue;

            return int.TryParse(arguments[argumentIndex + 1], out int value) ? value : defaultValue;
        }
        #endregion // 함수
    }
}
