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
        [SWGroup("하루 설정")]
        [SerializeField, Min(0), Tooltip("하루에 사용할 수 있는 방송 시간(분)입니다.")]
        private int broadcastMinutesPerDay = 240;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>씬 전환에 걸쳐 유지되는 게임 진행 상태입니다.</summary>
        public GameState State { get; private set; }

        /// <summary>하루에 사용할 수 있는 방송 시간(분)입니다.</summary>
        public int BroadcastMinutesPerDay => broadcastMinutesPerDay;

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
                return;
            }

            State = new GameState();
            State.ResetBroadcastTime(broadcastMinutesPerDay);
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
        /// 진행 상태를 지우고 처음부터 다시 시작합니다.
        /// </summary>
        /// <remarks>테스트를 처음 상태에서 다시 하려고 만든 통로입니다.</remarks>
        public void ResetState()
        {
            GameSave.Delete();

            State = new GameState();
            State.ResetBroadcastTime(broadcastMinutesPerDay);

            SWLog.Log($"[{nameof(GameManager)}] 진행 상태를 처음부터 다시 시작합니다.");
        }

        /// <summary>
        /// 하루를 마치고 다음 날로 넘어갑니다.
        /// </summary>
        public void AdvanceDay()
        {
            State.AdvanceDay(broadcastMinutesPerDay);
            GameSave.Save(State, "하루 종료");
            SWLog.Log($"[{nameof(GameManager)}] {State.Day}일차로 넘어갑니다.");
        }
        #endregion // 함수
    }
}
