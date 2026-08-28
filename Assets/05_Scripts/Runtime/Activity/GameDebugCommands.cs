using SW.Attributes;
using SW.Util;

using ProjectR.Data;

namespace ProjectR.Activity
{
    /// <summary>
    /// 인게임 디버그 콘솔에 등록되는 게임 진행 관련 명령 모음입니다.
    /// </summary>
    /// <remarks>
    /// SWDebugConsole이 SW_DEBUG_MODE 환경에서 정적 메서드를 찾아 자동으로 등록합니다.
    /// </remarks>
    public static class GameDebugCommands
    {
        #region 함수
        /// <summary>
        /// 현재 게임 상태를 로그로 출력합니다.
        /// </summary>
        [SWCommand("state.print", "현재 게임 상태를 출력합니다.", "게임")]
        private static void PrintState()
        {
            GameState state = GameManager.Instance.State;

            SWLog.Log($"[{nameof(GameDebugCommands)}] {state.Day}일차 / 남은 방송 시간 {state.RemainingBroadcastMinutes}분 / " +
                $"후원금 {state.Donation} / 시청자 {state.ViewerCount} / 보유 이상물체 {state.Items.Count}개");
            SWLog.Log($"[{nameof(GameDebugCommands)}] 수면 {state.Condition.Sleep} / 배고픔 {state.Condition.Hunger} / " +
                $"기분 {state.Condition.Mood} / 피로도 {state.Condition.Fatigue}");
        }

        /// <summary>
        /// 남은 방송 시간을 지정한 값으로 설정합니다.
        /// </summary>
        /// <param name="minutes">설정할 남은 방송 시간(분)입니다.</param>
        [SWCommand("state.time", "남은 방송 시간(분)을 설정합니다.", "게임")]
        private static void SetRemainingBroadcastTime(int minutes)
        {
            GameManager.Instance.State.ResetBroadcastTime(minutes);
            SWLog.Log($"[{nameof(GameDebugCommands)}] 남은 방송 시간을 {minutes}분으로 설정했습니다.");
        }

        /// <summary>
        /// 저장해 둔 진행 상태를 지우고 처음부터 다시 시작합니다.
        /// </summary>
        [SWCommand("state.reset", "진행 상태를 지우고 처음부터 다시 시작합니다.", "게임")]
        private static void ResetState()
        {
            GameManager.Instance.ResetState();
        }

        /// <summary>
        /// 다음 날로 넘어갑니다.
        /// </summary>
        [SWCommand("state.nextday", "다음 날로 넘어갑니다.", "게임")]
        private static void AdvanceDay()
        {
            GameManager.Instance.AdvanceDay();
        }
        #endregion // 함수
    }
}
