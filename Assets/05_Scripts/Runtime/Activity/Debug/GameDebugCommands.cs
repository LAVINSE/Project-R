using UnityEngine.Scripting.APIUpdating;

using SW.Attributes;
using SW.Util;

using ProjectR.Data;

namespace ProjectR.Activity.Debugging
{
    /// <summary>
    /// 인게임 디버그 콘솔에 등록되는 게임 진행 관련 명령 모음입니다.
    /// </summary>
    /// <remarks>
    /// SWDebugConsole이 SW_DEBUG_MODE 환경에서 정적 메서드를 찾아 자동으로 등록합니다.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "ProjectR.Activity", sourceAssembly: "ProjectR.Activity", sourceClassName: "GameDebugCommands")]
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
        /// 하루를 마감하고 다음 날로 넘어갑니다.
        /// </summary>
        [SWCommand("state.nextday", "하루를 마감하고 다음 날로 넘어갑니다.", "게임")]
        private static void AdvanceDay()
        {
            GameManager.Instance.EndDay();
        }

        /// <summary>
        /// 컨디션 수치 하나를 지정한 값으로 맞춥니다.
        /// </summary>
        /// <param name="kind">맞출 수치의 이름입니다. sleep, hunger, mood, fatigue 중 하나입니다.</param>
        /// <param name="value">맞출 값입니다.</param>
        /// <remarks>
        /// 값을 직접 넣지 않고 지금 값과의 차이를 적용합니다.
        /// 그래야 피로도가 눌러 내린 상한이 그대로 걸립니다.
        /// 직접 넣는 통로를 만들면 상한을 넘긴 값이 들어가 규칙이 없는 것과 같아집니다.
        /// </remarks>
        [SWCommand("condition.set", "컨디션 수치를 맞춥니다. sleep / hunger / mood / fatigue", "게임")]
        private static void SetCondition(string kind, int value)
        {
            ConditionState condition = GameManager.Instance.State.Condition;
            ConditionDelta delta = default;

            switch (kind.ToLowerInvariant())
            {
                case "sleep": delta.Sleep = value - condition.Sleep; break;
                case "hunger": delta.Hunger = value - condition.Hunger; break;
                case "mood": delta.Mood = value - condition.Mood; break;
                case "fatigue": delta.Fatigue = value - condition.Fatigue; break;
                default:
                    SWLog.LogWarning($"[{nameof(GameDebugCommands)}] 모르는 수치입니다: {kind}. " +
                        $"sleep / hunger / mood / fatigue 중에서 고릅니다.");
                    return;
            }

            condition.Apply(delta);

            SWLog.Log($"[{nameof(GameDebugCommands)}] {kind}을(를) 맞췄습니다. " +
                $"수면 {condition.Sleep} / 배고픔 {condition.Hunger} / 기분 {condition.Mood} / " +
                $"피로도 {condition.Fatigue} / 상한 {condition.EffectiveMaximum}");
        }

        /// <summary>
        /// 후원금을 더하거나 뺍니다.
        /// </summary>
        /// <param name="amount">더할 후원금입니다. 음수면 차감입니다.</param>
        [SWCommand("donation.add", "후원금을 더하거나 뺍니다. 음수면 차감입니다.", "게임")]
        private static void AddDonation(int amount)
        {
            GameManager.Instance.State.Channel.AddDonation(amount);

            SWLog.Log($"[{nameof(GameDebugCommands)}] 후원금이 {GameManager.Instance.State.Donation}이 되었습니다.");
        }

        /// <summary>
        /// 시청자 수를 지정한 값으로 맞춥니다.
        /// </summary>
        /// <param name="count">맞출 시청자 수입니다.</param>
        /// <remarks>유지비와 이탈이 규모에 비례하는지를 확인하려면 큰 수를 넣어 봐야 합니다.</remarks>
        [SWCommand("viewer.set", "시청자 수를 맞춥니다.", "게임")]
        private static void SetViewers(int count)
        {
            StreamerProgress streamer = GameManager.Instance.State.ActiveStreamer;

            if (streamer == null)
            {
                SWLog.LogWarning($"[{nameof(GameDebugCommands)}] 진행 중인 스트리머가 없어 맞추지 못했습니다.");
                return;
            }

            streamer.AddViewers(count - streamer.ViewerCount);

            SWLog.Log($"[{nameof(GameDebugCommands)}] 시청자 수가 {streamer.ViewerCount}명이 되었습니다.");
        }

        /// <summary>
        /// 비용과 선행 조건을 따지지 않고 업그레이드를 얻습니다.
        /// </summary>
        /// <param name="upgradeId">얻을 업그레이드의 코드명입니다.</param>
        [SWCommand("upgrade.grant", "비용을 내지 않고 업그레이드를 얻습니다.", "게임")]
        private static void GrantUpgrade(string upgradeId)
        {
            if (GameManager.Instance.GrantUpgrade(upgradeId)) return;

            SWLog.LogWarning($"[{nameof(GameDebugCommands)}] 업그레이드를 얻지 못했습니다: {upgradeId}");
        }

        /// <summary>
        /// 업그레이드를 사서 얻습니다.
        /// </summary>
        /// <param name="upgradeId">살 업그레이드의 코드명입니다.</param>
        /// <remarks>살 수 없으면 왜 못 사는지가 로그에 남습니다.</remarks>
        [SWCommand("upgrade.buy", "후원금을 내고 업그레이드를 삽니다.", "게임")]
        private static void PurchaseUpgrade(string upgradeId)
        {
            GameManager.Instance.TryPurchaseUpgrade(upgradeId);
        }

        /// <summary>
        /// 보유 중인 업그레이드를 출력합니다.
        /// </summary>
        [SWCommand("upgrade.print", "보유 중인 업그레이드를 출력합니다.", "게임")]
        private static void PrintUpgrades()
        {
            StreamerProgress streamer = GameManager.Instance.State.ActiveStreamer;

            if (streamer == null || streamer.UpgradeIds.Count == 0)
            {
                SWLog.Log($"[{nameof(GameDebugCommands)}] 보유 중인 업그레이드가 없습니다.");
                return;
            }

            SWLog.Log($"[{nameof(GameDebugCommands)}] 보유 업그레이드 {streamer.UpgradeIds.Count}개: " +
                $"{string.Join(", ", streamer.UpgradeIds)}");
        }

        /// <summary>
        /// 지금 값으로 하루를 마감하면 어떻게 되는지 계산만 해 봅니다.
        /// </summary>
        /// <remarks>
        /// 마감하지 않고 결과만 봅니다. 유지비 조정값을 만질 때 하루를 실제로 넘기지 않고
        /// 숫자만 확인할 수 있어야 여러 번 시험할 수 있습니다.
        /// </remarks>
        [SWCommand("state.dayend", "하루를 마감하면 어떻게 되는지 계산만 해 봅니다.", "게임")]
        private static void PreviewDayEnd()
        {
            GameManager manager = GameManager.Instance;

            DayEndResult result = DayEndCalculator.Calculate(manager.DayEndSettings,
                manager.State.ViewerCount, manager.State.Channel.TodayActivityCount,
                manager.State.Channel.TodayHasFailure);

            SWLog.Log($"[{nameof(GameDebugCommands)}] 오늘 활동 {manager.State.Channel.TodayActivityCount}회 / " +
                $"실패 {(manager.State.Channel.TodayHasFailure ? "있음" : "없음")}");
            SWLog.Log($"[{nameof(GameDebugCommands)}] 유지비 {result.UpkeepCost} / 이탈 {result.ViewerLoss}명 / " +
                $"다음 날 방송 시간 {result.NextBroadcastMinutes}분{(result.IsPenalized ? " (실패로 깎임)" : string.Empty)}");
        }
        #endregion // 함수
    }
}
