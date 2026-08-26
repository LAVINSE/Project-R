using SW.Attributes;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Core;

namespace ProjectR.Backrooms
{
    /// <summary>
    /// 인게임 디버그 콘솔에 등록되는 백룸 관련 명령 모음입니다.
    /// </summary>
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
        #endregion // 함수
    }
}
