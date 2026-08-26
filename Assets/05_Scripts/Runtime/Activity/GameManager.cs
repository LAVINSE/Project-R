using UnityEngine;

using SW.Attributes;
using SW.Util;

using ProjectR.Data;

namespace ProjectR.Activity
{
    /// <summary>
    /// 게임 상태를 보관하고 활동의 시작과 종료를 관리하는 전역 매니저입니다.
    /// </summary>
    /// <remarks>
    /// 씬을 오가도 유지되어야 하므로 <see cref="SWSingleton{T}"/>를 사용합니다.
    /// 이 프로젝트에서 계획한 전역 싱글톤은 이 매니저 하나이며,
    /// 나머지는 SWUtils가 제공하는 SWSceneLoader와 SWAudioManager를 그대로 씁니다.
    /// </remarks>
    public class GameManager : SWSingleton<GameManager>
    {
        #region 필드
        [SWGroup("하루 설정")]
        [SerializeField, Tooltip("하루에 사용할 수 있는 활동 시간대 수입니다.")]
        private int slotsPerDay = 4;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>씬 전환에 걸쳐 유지되는 게임 진행 상태입니다.</summary>
        public GameState State { get; private set; }

        /// <summary>진행 중인 활동입니다. 없으면 null입니다.</summary>
        public IActivity CurrentActivity { get; private set; }

        /// <summary>현재 활동이 진행 중인지 여부입니다.</summary>
        public bool IsActivityRunning => CurrentActivity != null;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 중복 인스턴스를 정리하고 게임 상태를 준비합니다.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            if (State != null) return;

            State = new GameState();
            State.ResetSlots(slotsPerDay);
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

            if (activity.SlotCost > State.RemainingSlots)
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 남은 시간대가 부족해 활동을 시작할 수 없습니다.");
                return false;
            }

            if (activity.CanEnter(State) == false)
            {
                SWLog.LogWarning($"[{nameof(GameManager)}] 진입 조건을 만족하지 못해 활동을 시작할 수 없습니다.");
                return false;
            }

            CurrentActivity = activity;
            State.ConsumeSlots(activity.SlotCost);
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

            SWLog.Log($"[{nameof(GameManager)}] 활동을 종료했습니다: {endedActivity.GetType().Name}");
            SWEventBus.Publish(new ActivityEndedEvent(endedActivity, result));

            return result;
        }

        /// <summary>
        /// 하루를 마치고 다음 날로 넘어갑니다.
        /// </summary>
        public void AdvanceDay()
        {
            State.AdvanceDay(slotsPerDay);
            SWLog.Log($"[{nameof(GameManager)}] {State.Day}일차로 넘어갑니다.");
        }
        #endregion // 함수
    }
}
