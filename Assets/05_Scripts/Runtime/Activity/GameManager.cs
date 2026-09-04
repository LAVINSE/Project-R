using UnityEngine;

using SW.Attributes;
using SW.Util;

using ProjectR.Core;
using ProjectR.Data;

namespace ProjectR.Activity
{
    /// <summary>
    /// 게임 상태를 보관하고 활동의 시작과 종료를 관리하는 전역 매니저입니다.
    /// </summary>
    /// <remarks>
    /// 씬을 오가도 유지되어야 하므로 <see cref="SWSingleton{T}"/>를 사용합니다.
    /// 게임 코드가 만드는 전역 싱글톤은 이 매니저와 PopupHotkeyController 둘뿐이며,
    /// 나머지는 SWUtils가 제공하는 SWSceneLoader와 SWAudioManager를 그대로 씁니다.
    /// </remarks>
    public class GameManager : SWSingleton<GameManager>
    {
        #region 필드
        /// <summary>하루를 마감할 때 쓰는 유지비·이탈·방송 시간 조정값입니다.</summary>
        [SWGroup("하루 설정")]
        [SerializeField, Tooltip("하루를 마감할 때 쓰는 유지비·이탈·방송 시간 조정값입니다.")]
        private DayEndSettings dayEndSettings = DayEndSettings.Default;

        /// <summary>진행할 스트리머의 식별자입니다. 스트리머 선택 화면이 생기기 전까지 쓰는 자리입니다.</summary>
        [SWGroup("스트리머")]
        [SerializeField, Tooltip("진행할 스트리머의 식별자입니다. 스트리머 선택 화면이 생기기 전까지 쓰는 자리입니다.")]
        private string defaultStreamerId = "Streamer_01";

        /// <summary>스탯과 업그레이드 보너스를 들고 있는 스탯판입니다.</summary>
        [SerializeField, Tooltip("스탯과 업그레이드 보너스를 들고 있는 스탯판입니다.")]
        private StreamerStatBoard statBoard;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>씬 전환에 걸쳐 유지되는 게임 진행 상태입니다.</summary>
        public GameState State { get; private set; }

        /// <summary>하루를 마감할 때 쓰는 조정값입니다.</summary>
        public DayEndSettings DayEndSettings => dayEndSettings;

        /// <summary>스탯과 업그레이드 보너스를 들고 있는 스탯판입니다.</summary>
        public StreamerStatBoard Stats => statBoard;

        /// <summary>진행 중인 활동입니다. 없으면 null입니다.</summary>
        public IActivity CurrentActivity { get; private set; }

        /// <summary>현재 활동이 진행 중인지 여부입니다.</summary>
        public bool IsActivityRunning => CurrentActivity != null;

        /// <summary>아직 정산 화면으로 보여 주지 않은 활동 결과입니다. 없으면 null입니다.</summary>
        /// <remarks>
        /// 활동은 활동 씬 안에서 끝나고 곧바로 관리 화면으로 넘어가므로,
        /// 관리 화면 쪽에서 결과를 받으려면 씬 전환을 건너뛸 자리가 필요합니다.
        /// </remarks>
        public ActivityResult PendingResult { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 중복 인스턴스를 정리하고 게임 상태를 준비합니다.
        /// </summary>
        /// <remarks>저장해 둔 진행 상태가 있으면 그것을 잇고, 없으면 새 진행을 시작합니다.</remarks>
        public override void Awake()
        {
            base.Awake();

            // 저장해 둔 옵션(음량·마우스 감도)을 어느 씬에서든 게임이 시작될 때 한 번 읽어 둡니다.
            GameSettings.EnsureLoaded();

            if (State != null) return;

            if (GameSave.TryLoad(out GameState loaded))
            {
                State = loaded;

                // 진행 중인 스트리머가 비어 있으면 채웁니다. 스트리머 선택 화면이 생기기 전까지의 자리입니다.
                if (State.ActiveStreamer == null) State.SelectStreamer(defaultStreamerId);

                return;
            }

            State = CreateNewState();
        }

        /// <summary>
        /// 보유 업그레이드를 스탯에 반영합니다.
        /// </summary>
        /// <remarks>
        /// Awake가 아니라 Start에서 합니다. 스탯 복제본은 <c>SWStats</c>가 자기 Awake에서 만드는데,
        /// 컴포넌트 사이의 Awake 순서는 정해져 있지 않아 Awake에서 부르면 스탯이 아직 없을 수 있습니다.
        /// 유니티는 모든 Awake를 끝낸 뒤에 Start를 부르므로 여기서는 반드시 준비되어 있습니다.
        /// </remarks>
        private void Start()
        {
            statBoard?.RebuildUpgradeBonuses(State.ActiveStreamer);
        }

        /// <summary>
        /// 처음 시작하는 진행 상태를 만듭니다.
        /// </summary>
        /// <returns>구조 번호와 진행할 스트리머, 오늘 방송 시간이 채워진 진행 상태입니다.</returns>
        /// <remarks>
        /// 구조 번호는 새로 만들 때만 넣습니다. 생성자에서 넣으면 그 필드가 없던 구버전 저장 파일도
        /// 최신 버전으로 읽히기 때문입니다.
        /// </remarks>
        private GameState CreateNewState()
        {
            GameState created = new();

            created.MarkAsCurrentVersion();
            created.SelectStreamer(defaultStreamerId);
            created.ResetBroadcastTime(dayEndSettings.BaseBroadcastMinutes);

            return created;
        }

        /// <summary>
        /// 활동을 시작합니다. 진입 조건을 만족하지 못하면 시작하지 않습니다.
        /// </summary>
        /// <param name="activity">시작할 활동입니다.</param>
        /// <returns>활동을 시작했으면 true를 반환합니다.</returns>
        public bool BeginActivity(IActivity activity)
        {
            if (activity == null)
            {
                SWLog.LogError($"[{nameof(GameManager)}] 활동이 null이라 시작할 수 없습니다.");
                return false;
            }

            if (IsActivityRunning)
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 이미 진행 중인 활동이 있어 시작을 거부합니다.");
                return false;
            }

            if (activity.BroadcastCost > State.RemainingBroadcastMinutes)
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 남은 방송 시간이 부족해 활동을 시작할 수 없습니다.");
                return false;
            }

            if (activity.CanEnter(State) == false)
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 진입 조건을 만족하지 못해 활동을 시작할 수 없습니다.");
                return false;
            }

            CurrentActivity = activity;
            State.ConsumeBroadcastTime(activity.BroadcastCost);

            // 방송 시간을 소비한 직후에 저장해 둡니다. 활동 도중 강제 종료하면 여기로 되돌아오므로
            // 위기 상황에서 게임을 끄고 도망쳐도 방송 시간은 이미 사라진 뒤입니다. (기획서 11.1절)
            GameSave.Save(State, $"활동 시작 - {activity.GetType().Name}");

            activity.Begin(State);

            SWLog.Log($"[{nameof(GameManager)}] 활동을 시작했습니다: {activity.GetType().Name}");
            SWEventBus.Publish(new ActivityBeganEvent(activity));

            return true;
        }

        /// <summary>
        /// 진행 중인 활동을 끝내고 결과를 게임 상태에 반영합니다.
        /// </summary>
        /// <returns>반영된 활동 결과입니다. 진행 중인 활동이 없으면 null을 반환합니다.</returns>
        public ActivityResult EndActivity()
        {
            if (IsActivityRunning == false)
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 진행 중인 활동이 없어 종료를 건너뜁니다.");
                return null;
            }

            IActivity endedActivity = CurrentActivity;
            CurrentActivity = null;

            ActivityResult result = endedActivity.End() ?? ActivityResult.Empty();

            // 반영하기 전에 바깥에서 몫을 더할 기회를 줍니다. 방송이 굴린 시청자와 후원이 여기서 합쳐집니다.
            SWEventBus.Publish(new ActivitySettlingEvent(endedActivity, result));

            State.Apply(result);
            GameSave.Save(State, $"활동 정산 - {endedActivity.GetType().Name}");

            PendingResult = result;

            SWLog.Log($"[{nameof(GameManager)}] 활동을 종료했습니다: {endedActivity.GetType().Name}");
            SWEventBus.Publish(new ActivityEndedEvent(endedActivity, result));

            return result;
        }

        /// <summary>
        /// 아직 보여 주지 않은 활동 결과를 가져오면서 비웁니다.
        /// </summary>
        /// <returns>보여 줄 활동 결과입니다. 없으면 null을 반환합니다.</returns>
        public ActivityResult ConsumePendingResult()
        {
            ActivityResult pending = PendingResult;
            PendingResult = null;

            return pending;
        }

        /// <summary>
        /// 업그레이드를 사서 보유 목록에 넣고 스탯에 반영합니다.
        /// </summary>
        /// <param name="upgradeId">살 업그레이드의 식별자입니다.</param>
        /// <returns>샀으면 true를 반환합니다.</returns>
        /// <remarks>
        /// 살 수 없는 이유마다 다른 로그를 남깁니다. 화면에서 버튼을 끄기만 하면
        /// 왜 못 사는지 알 수 없다는 것이 마을 건물에도 그대로 적용됩니다(체크리스트 2.2절).
        /// </remarks>
        public bool TryPurchaseUpgrade(string upgradeId)
        {
            if (statBoard == null) return false;

            StreamerProgress streamer = State.ActiveStreamer;
            UpgradeDefinition definition = statBoard.FindUpgrade(upgradeId);

            if (streamer == null || definition == null)
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 모르는 업그레이드라 사지 못했습니다: {upgradeId}");
                return false;
            }

            if (streamer.HasUpgrade(definition.DefinitionId))
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 이미 갖고 있습니다: {definition.DisplayName}");
                return false;
            }

            if (definition.HasRequirement && streamer.HasUpgrade(definition.RequiredUpgradeCode) == false)
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 선행 업그레이드가 없습니다: " +
                    $"{definition.RequiredUpgradeCode}");
                return false;
            }

            if (State.Donation < definition.Cost)
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 후원금이 모자랍니다. " +
                    $"{definition.Cost} 필요 / {State.Donation} 보유");
                return false;
            }

            State.Channel.AddDonation(-definition.Cost);

            GrantUpgrade(definition.DefinitionId);

            return true;
        }

        /// <summary>
        /// 비용과 선행 조건을 따지지 않고 업그레이드를 보유 목록에 넣습니다.
        /// </summary>
        /// <param name="upgradeId">넣을 업그레이드의 식별자입니다.</param>
        /// <returns>새로 넣었으면 true를 반환합니다.</returns>
        /// <remarks>디버그 명령과 구매가 함께 쓰는 자리입니다. 넣은 뒤 곧바로 스탯에 반영하고 저장합니다.</remarks>
        public bool GrantUpgrade(string upgradeId)
        {
            StreamerProgress streamer = State.ActiveStreamer;

            if (streamer == null || streamer.AddUpgrade(upgradeId) == false) return false;

            statBoard?.RebuildUpgradeBonuses(streamer);
            GameSave.Save(State, $"업그레이드 획득 - {upgradeId}");

            SWLog.Log($"[{nameof(GameManager)}] 업그레이드를 얻었습니다: {upgradeId}");

            return true;
        }

        /// <summary>
        /// 진행 상태를 지우고 처음부터 다시 시작합니다.
        /// </summary>
        /// <remarks>테스트를 처음 상태에서 다시 하려고 만든 통로입니다.</remarks>
        public void ResetState()
        {
            GameSave.Delete();

            State = CreateNewState();
            statBoard?.RebuildUpgradeBonuses(State.ActiveStreamer);

            SWLog.Log($"[{nameof(GameManager)}] 진행 상태를 처음부터 다시 시작합니다.");
        }

        /// <summary>
        /// 하루를 마감하고 다음 날로 넘어갑니다.
        /// </summary>
        /// <returns>반영된 하루 마감 결과입니다.</returns>
        /// <remarks>
        /// 유지비와 구독자 이탈이 여기서 걸립니다(기획서 8.3절).
        /// 결과를 돌려주는 것은 하루 마감 화면이 "얼마가 나갔는지"를 보여 줄 수 있어야 하기 때문입니다.
        /// 활동 중에는 마감할 수 없습니다. 활동 결과가 반영되기 전에 날짜가 넘어가기 때문입니다.
        /// </remarks>
        public DayEndResult EndDay()
        {
            if (IsActivityRunning)
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 활동이 진행 중이라 하루를 마감하지 않습니다.");
                return default;
            }

            DayEndResult result = DayEndCalculator.Calculate(dayEndSettings, State.ViewerCount,
                State.Channel.TodayActivityCount, State.Channel.TodayHasFailure);

            State.ApplyDayEnd(result);
            GameSave.Save(State, "하루 종료");

            SWLog.Log($"[{nameof(GameManager)}] {State.Day}일차로 넘어갑니다. " +
                $"유지비 {result.UpkeepCost} / 이탈 {result.ViewerLoss}명 / 방송 시간 {result.NextBroadcastMinutes}분");

            SWEventBus.Publish(new DayEndedEvent(State.Day, result));

            return result;
        }
        #endregion // 함수
    }
}
