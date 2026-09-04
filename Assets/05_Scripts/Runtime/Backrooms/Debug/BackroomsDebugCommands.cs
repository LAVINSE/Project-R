using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

using SW.Attributes;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Backrooms.Assembly;
using ProjectR.Backrooms.Collect;
using ProjectR.Backrooms.Generation;
using ProjectR.Backrooms.Lighting;
using ProjectR.Core;
using ProjectR.Inventory;

namespace ProjectR.Backrooms.Debugging
{
    /// <summary>
    /// 인게임 디버그 콘솔에 등록되는 백룸 관련 명령 모음입니다.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "ProjectR.Backrooms", sourceAssembly: "ProjectR.Backrooms", sourceClassName: "BackroomsDebugCommands")]
    public static class BackroomsDebugCommands
    {
        #region 함수
        /// <summary>
        /// 백룸 탐험 활동을 시작합니다.
        /// </summary>
        [SWCommand("backrooms.enter", "백룸 탐험을 시작합니다.", "백룸")]
        private static void EnterBackrooms()
        {
            GameManager.Instance.BeginActivity(new BackroomsActivity());
        }

        /// <summary>
        /// 백룸 탐험을 종료하고 관리 화면으로 돌아갑니다.
        /// </summary>
        [SWCommand("backrooms.exit", "백룸 탐험을 종료하고 관리 화면으로 돌아갑니다.", "백룸")]
        private static void ExitBackrooms()
        {
            if (GameManager.Instance.EndActivity() == null) return;

            SceneFlow.ChangeScene(SceneNames.Home);
        }

        /// <summary>
        /// 지금 겨누고 있는 이상물체를 줍습니다.
        /// </summary>
        /// <remarks>겨누기와 줍기 중 어느 쪽이 안 되는지 갈라 보려고 만든 통로입니다.</remarks>
        [SWCommand("pickup.take", "지금 겨누고 있는 이상물체를 줍습니다.", "백룸")]
        private static void TakeFocusedPickup()
        {
            PlayerPickupInteractor interactor = Object.FindAnyObjectByType<PlayerPickupInteractor>();

            if (interactor == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsDebugCommands)}] 줍기 컴포넌트를 찾지 못했습니다.");
                return;
            }

            if (interactor.TryCollectFocused() == false)
                SWLog.LogWarning($"[{nameof(BackroomsDebugCommands)}] 겨누고 있는 이상물체가 없습니다.");
        }

        /// <summary>
        /// 가방에 들어 있는 이상물체를 출력합니다.
        /// </summary>
        [SWCommand("backpack.print", "가방에 들어 있는 이상물체를 출력합니다.", "백룸")]
        private static void PrintBackpack()
        {
            if (GameManager.Instance.CurrentActivity is not BackroomsActivity activity)
            {
                SWLog.LogWarning($"[{nameof(BackroomsDebugCommands)}] 진행 중인 백룸 탐험이 없습니다.");
                return;
            }

            SWLog.Log($"[{nameof(BackroomsDebugCommands)}] 가방 " +
                $"{activity.Backpack.OccupiedCellCount}/{activity.Backpack.CellCount}칸");

            foreach (PlacedItem item in activity.Backpack.Items)
            {
                SWLog.Log($"[{nameof(BackroomsDebugCommands)}] {item.DefinitionId} " +
                    $"{item.Position} {item.Width}x{item.Height}");
            }
        }

        /// <summary>
        /// 플레이어를 지정한 칸 좌표로 옮깁니다.
        /// </summary>
        /// <param name="x">옮길 칸의 X 좌표입니다.</param>
        /// <param name="y">옮길 칸의 Y 좌표입니다.</param>
        [SWCommand("player.teleport", "플레이어를 지정한 칸 좌표로 옮깁니다.", "플레이어")]
        private static void TeleportPlayer(int x, int y)
        {
            BackroomsMapBuilder mapBuilder = Object.FindAnyObjectByType<BackroomsMapBuilder>();

            if (mapBuilder == null || mapBuilder.LastResult == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsDebugCommands)}] 아직 맵이 없어 옮길 수 없습니다.");
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsDebugCommands)}] 플레이어를 찾지 못했습니다.");
                return;
            }

            MazeCoordinate coordinate = new(x, y);

            if (mapBuilder.LastResult.Grid.IsInside(coordinate) == false)
            {
                SWLog.LogWarning($"[{nameof(BackroomsDebugCommands)}] 격자 밖 좌표입니다: {coordinate}");
                return;
            }

            // 캐릭터 컨트롤러는 자기 위치를 스스로 관리하므로 끄지 않고 옮기면 되돌아갑니다.
            CharacterController controller = player.GetComponent<CharacterController>();
            bool wasEnabled = controller != null && controller.enabled;

            if (wasEnabled) controller.enabled = false;

            player.transform.position = mapBuilder.GetWorldPosition(coordinate);

            if (wasEnabled) controller.enabled = true;

            SWLog.Log($"[{nameof(BackroomsDebugCommands)}] 플레이어를 {coordinate}로 옮겼습니다.");
        }

        /// <summary>
        /// 지금 등록되어 있는 라이트맵 상태를 출력합니다.
        /// </summary>
        /// <remarks>빌드에서 라이트맵 참조가 끊겼는지 확인하는 용도입니다.</remarks>
        [SWCommand("map.lightmaps", "현재 맵의 라이트맵 등록 상태를 출력합니다.", "백룸")]
        private static void PrintLightmaps()
        {
            BackroomsLightingReport report = BackroomsLightingReport.Capture();

            SWLog.Log($"[{nameof(BackroomsDebugCommands)}] {report.ToSummary()}");
            SWLog.Log($"[{nameof(BackroomsDebugCommands)}] 라이트맵 상세\n{report.ToTextureDetail()}");
        }
        #endregion // 함수
    }
}
