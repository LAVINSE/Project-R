using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Backrooms.Assembly;
using ProjectR.Backrooms.Generation;

namespace ProjectR.Backrooms.Exploration
{
    /// <summary>
    /// 맵 조립이 끝나면 플레이어를 시작 칸에 놓고 탈출 지점을 배치하는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 조립을 맡은 <see cref="BackroomsMapBuilder"/>는 타일만 놓습니다.
    /// 무엇을 그 위에 올릴지는 이 컴포넌트가 정하므로, 맵 조립과 탐험 준비가 서로 얽히지 않습니다.
    /// </remarks>
    public class BackroomsMapPopulator : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("대상")]
        [SerializeField, Tooltip("생성 결과를 받아 올 맵 조립 컴포넌트입니다.")]
        private BackroomsMapBuilder mapBuilder;

        [SerializeField, Tooltip("시작 칸으로 옮길 플레이어입니다.")]
        private Transform playerTransform;

        [SWGroup("탈출 지점")]
        [SerializeField, Tooltip("탈출 칸에 놓을 프리팹입니다.")]
        private GameObject exitPrefab;

        [SerializeField, Min(0f), Tooltip("탈출 지점을 바닥에서 얼마나 띄울지(미터)입니다.")]
        private float exitHeight = 0f;

        /// <summary>배치해 둔 탈출 지점입니다. 없으면 null입니다.</summary>
        private GameObject exitInstance;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 맵 조립 완료 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            if (mapBuilder == null)
            {
                SWLog.LogError($"[{nameof(BackroomsMapPopulator)}] 맵 조립 컴포넌트가 비어 있습니다.");
                return;
            }

            mapBuilder.MapBuilt += HandleMapBuilt;
        }

        /// <summary>
        /// 맵 조립 완료 알림을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (mapBuilder == null) return;

            mapBuilder.MapBuilt -= HandleMapBuilt;
        }

        /// <summary>
        /// 조립이 끝난 맵에 플레이어와 탈출 지점을 놓습니다.
        /// </summary>
        /// <param name="result">방금 만들어진 미로 결과입니다.</param>
        private void HandleMapBuilt(MazeBuildResult result)
        {
            MovePlayerToStart(result);
            PlaceExit(result);
        }

        /// <summary>
        /// 플레이어를 시작 칸의 바닥으로 옮기고 열린 통로 쪽을 보게 합니다.
        /// </summary>
        /// <param name="result">시작 좌표를 읽을 미로 결과입니다.</param>
        private void MovePlayerToStart(MazeBuildResult result)
        {
            if (playerTransform == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsMapPopulator)}] 플레이어가 비어 있어 시작 위치를 옮기지 못했습니다.");
                return;
            }

            EMazeDirection openings = EMazeDirection.All & ~result.Grid.GetWalls(result.StartCoordinate);

            // 캐릭터 컨트롤러는 자기 위치를 스스로 관리하므로, 끄지 않고 옮기면 원래 자리로 되돌아갑니다.
            CharacterController controller = playerTransform.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;

            if (wasEnabled) controller.enabled = false;

            playerTransform.SetPositionAndRotation(mapBuilder.GetWorldPosition(result.StartCoordinate),
                Quaternion.Euler(0f, GetYaw(openings), 0f));

            if (wasEnabled) controller.enabled = true;

            SWLog.Log($"[{nameof(BackroomsMapPopulator)}] 플레이어를 시작 칸 {result.StartCoordinate}에 놓았습니다.");
        }

        /// <summary>
        /// 탈출 지점을 탈출 칸에 놓습니다. 이미 있으면 위치만 옮깁니다.
        /// </summary>
        /// <param name="result">탈출 좌표를 읽을 미로 결과입니다.</param>
        private void PlaceExit(MazeBuildResult result)
        {
            if (exitPrefab == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsMapPopulator)}] 탈출 지점 프리팹이 비어 있어 배치하지 못했습니다.");
                return;
            }

            Vector3 position = mapBuilder.GetWorldPosition(result.ExitCoordinate) + Vector3.up * exitHeight;

            if (exitInstance == null) exitInstance = Instantiate(exitPrefab, transform);

            exitInstance.transform.SetPositionAndRotation(position, Quaternion.identity);
            exitInstance.SetActive(true);

            SWLog.Log($"[{nameof(BackroomsMapPopulator)}] 탈출 지점을 {result.ExitCoordinate}에 놓았습니다.");
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
        #endregion // 함수
    }
}
