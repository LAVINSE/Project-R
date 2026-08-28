using System.Collections;

using UnityEngine;
using UnityEngine.Profiling;

using SW.Attributes;
using SW.Base;
using SW.Debugging;
using SW.Util;

using ProjectR.Backrooms.Assembly;

namespace ProjectR.Backrooms.Measurement
{
    /// <summary>
    /// 맵을 정해진 횟수만큼 연달아 다시 만들면서 메모리 증가를 재는 측정 도구입니다.
    /// </summary>
    /// <remarks>
    /// 이 방식에서 메모리가 샐 수 있는 지점은 맵을 다시 만들 때마다 라이트맵이 전역 목록에
    /// 다시 등록되어 쌓이는 경우입니다. 사람이 30분을 걸어 다니며 확인하는 대신,
    /// 재생성을 연달아 돌려 같은 위험을 훨씬 빠르게 드러냅니다.
    /// 측정만 하는 도구이므로 조립을 맡은 <see cref="BackroomsMapBuilder"/>와 분리해 둡니다.
    /// </remarks>
    [RequireComponent(typeof(BackroomsMapBuilder))]
    public class BackroomsSoakRunner : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("측정 설정")]
        [SerializeField, Range(1, 1000), Tooltip("인스펙터 버튼으로 시작할 때 사용할 재생성 횟수입니다.")]
        private int defaultCycleCount = 100;

        [SerializeField, Range(1, 100), Tooltip("몇 회마다 중간 결과를 로그로 남길지입니다.")]
        private int logInterval = 10;

        /// <summary>같은 오브젝트에 붙어 있는 맵 조립 컴포넌트입니다.</summary>
        private BackroomsMapBuilder mapBuilder;

        /// <summary>진행 중인 측정 코루틴입니다. 없으면 null입니다.</summary>
        private Coroutine soakRoutine;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>측정이 진행 중인지 여부입니다.</summary>
        public bool IsSoaking => soakRoutine != null;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 맵 조립 컴포넌트를 캐싱하고 디버그 콘솔에 명령을 등록합니다.
        /// </summary>
        private void Awake()
        {
            mapBuilder = GetComponent<BackroomsMapBuilder>();

            SWDebugConsole.RegisterInstance(this);
        }

        /// <summary>
        /// 디버그 콘솔 등록을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            SWDebugConsole.UnregisterInstance(this);
        }

        /// <summary>
        /// 맵 재생성을 지정한 횟수만큼 연달아 돌립니다.
        /// </summary>
        /// <param name="cycleCount">다시 만들 횟수입니다.</param>
        public void StartSoak(int cycleCount)
        {
            if (IsSoaking)
            {
                SWLog.LogWarning($"[{nameof(BackroomsSoakRunner)}] 이미 측정이 진행 중이라 시작하지 않습니다.");
                return;
            }

            if (cycleCount < 1)
            {
                SWLog.LogWarning($"[{nameof(BackroomsSoakRunner)}] 재생성 횟수가 1보다 작아 측정을 건너뜁니다.");
                return;
            }

            soakRoutine = StartCoroutine(SoakRoutine(cycleCount));
        }

        /// <summary>
        /// 맵을 연달아 다시 만들면서 메모리와 등록 현황을 기록합니다.
        /// </summary>
        /// <param name="cycleCount">다시 만들 횟수입니다.</param>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator SoakRoutine(int cycleCount)
        {
            while (mapBuilder.IsBuilding) yield return null;

            int baseSeed = mapBuilder.LastSeed;
            float startTime = Time.realtimeSinceStartup;
            long startManagedBytes = Profiler.GetMonoUsedSizeLong();
            long startTotalBytes = Profiler.GetTotalAllocatedMemoryLong();
            int startLightmapCount = LightmapSettings.lightmaps.Length;

            SWLog.Log($"[{nameof(BackroomsSoakRunner)}] 재생성 측정을 시작합니다. " +
                $"{cycleCount}회 / 기준 시드 {baseSeed} / {DescribeMemory()}");

            for (int cycle = 1; cycle <= cycleCount; cycle += 1)
            {
                mapBuilder.Build(baseSeed + cycle);

                yield return null;

                while (mapBuilder.IsBuilding) yield return null;

                if (cycle % logInterval != 0 && cycle != cycleCount) continue;

                SWLog.Log($"[{nameof(BackroomsSoakRunner)}] {cycle}/{cycleCount}회 / " +
                    $"경과 {(Time.realtimeSinceStartup - startTime) / 60f:F1}분 / " +
                    $"렌더러 {CountRenderers()}개 / {DescribeMemory()}");
            }

            System.GC.Collect();

            yield return null;

            long beforeCleanupBytes = Profiler.GetTotalAllocatedMemoryLong();

            // 참조가 끊긴 에셋 때문에 늘어난 것인지, 진짜로 새는 것인지를 갈라 보기 위해 정리 전후를 함께 잽니다.
            AsyncOperation unloadOperation = Resources.UnloadUnusedAssets();

            while (unloadOperation.isDone == false) yield return null;

            System.GC.Collect();

            yield return null;

            long endManagedBytes = Profiler.GetMonoUsedSizeLong();
            long endTotalBytes = Profiler.GetTotalAllocatedMemoryLong();
            int endLightmapCount = LightmapSettings.lightmaps.Length;

            SWLog.Log($"[{nameof(BackroomsSoakRunner)}] 측정 완료. {cycleCount}회 / " +
                $"경과 {(Time.realtimeSinceStartup - startTime) / 60f:F1}분");
            SWLog.Log($"[{nameof(BackroomsSoakRunner)}] 라이트맵 {startLightmapCount}장 → {endLightmapCount}장 / " +
                $"관리 메모리 {ToMegabytes(startManagedBytes):F1}MB → {ToMegabytes(endManagedBytes):F1}MB " +
                $"({ToMegabytes(endManagedBytes - startManagedBytes):+0.0;-0.0;0.0}MB)");
            SWLog.Log($"[{nameof(BackroomsSoakRunner)}] 총 할당 {ToMegabytes(startTotalBytes):F1}MB → " +
                $"정리 전 {ToMegabytes(beforeCleanupBytes):F1}MB → 정리 후 {ToMegabytes(endTotalBytes):F1}MB " +
                $"(정리 후 증가분 {ToMegabytes(endTotalBytes - startTotalBytes):+0.0;-0.0;0.0}MB)");

            soakRoutine = null;
        }

        /// <summary>
        /// 현재 씬에 있는 렌더러 개수를 셉니다.
        /// </summary>
        /// <returns>활성 렌더러 개수입니다. 맵을 다시 만들어도 늘어나지 않아야 합니다.</returns>
        private static int CountRenderers()
        {
            return FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
        }

        /// <summary>
        /// 지금의 메모리와 라이트맵 등록 현황을 한 줄로 만듭니다.
        /// </summary>
        /// <returns>라이트맵 장수와 메모리 사용량을 담은 문자열입니다.</returns>
        private static string DescribeMemory()
        {
            return $"라이트맵 {LightmapSettings.lightmaps.Length}장 / " +
                $"관리 메모리 {ToMegabytes(Profiler.GetMonoUsedSizeLong()):F1}MB / " +
                $"총 할당 {ToMegabytes(Profiler.GetTotalAllocatedMemoryLong()):F1}MB";
        }

        /// <summary>
        /// 바이트 수를 메가바이트로 바꿉니다.
        /// </summary>
        /// <param name="bytes">바꿀 바이트 수입니다.</param>
        /// <returns>메가바이트 단위 값입니다.</returns>
        private static float ToMegabytes(long bytes)
        {
            return bytes / (1024f * 1024f);
        }

        /// <summary>
        /// 디버그 콘솔에서 맵 재생성 측정을 시작합니다.
        /// </summary>
        /// <param name="cycleCount">다시 만들 횟수입니다.</param>
        [SWCommand("map.soak", "맵을 지정한 횟수만큼 다시 만들며 메모리 증가를 측정합니다.", "백룸")]
        private void SoakCommand(int cycleCount)
        {
            StartSoak(cycleCount);
        }

        /// <summary>
        /// 인스펙터에서 기본 횟수로 측정을 시작합니다.
        /// </summary>
        [SWButton("재생성 메모리 측정 시작")]
        private void StartSoakFromInspector()
        {
            if (Application.isPlaying == false)
            {
                SWLog.LogWarning($"[{nameof(BackroomsSoakRunner)}] 측정은 플레이 중에만 할 수 있습니다.");
                return;
            }

            StartSoak(defaultCycleCount);
        }
        #endregion // 함수
    }
}
