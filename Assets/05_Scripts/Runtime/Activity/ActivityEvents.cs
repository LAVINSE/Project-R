using ProjectR.Data;

namespace ProjectR.Activity
{
    /// <summary>
    /// 활동이 시작되었음을 알리는 이벤트입니다.
    /// </summary>
    public readonly struct ActivityBeganEvent
    {
        #region 프로퍼티
        /// <summary>시작된 활동입니다.</summary>
        public IActivity Activity { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 시작된 활동을 담아 이벤트를 만듭니다.
        /// </summary>
        /// <param name="activity">시작된 활동입니다.</param>
        public ActivityBeganEvent(IActivity activity)
        {
            Activity = activity;
        }
        #endregion // 함수
    }

    /// <summary>
    /// 활동 결과가 게임 상태에 반영되기 직전임을 알리는 이벤트입니다.
    /// </summary>
    /// <remarks>
    /// 활동 바깥에서 결과에 몫을 더할 자리입니다. 방송이 굴린 시청자와 후원이 여기서 합쳐집니다.
    /// <b>결과 객체를 그대로 넘기므로 듣는 쪽이 값을 더할 수 있습니다.</b>
    /// 활동이 방송에게 물어보게 하면 백룸이 방송을 참조해야 하는데, 그것이 체크리스트 1.7절이
    /// 금지한 유일한 방향입니다. 활동이 결과를 들고 와서 "더할 것 있으면 더하라"고 알리면
    /// 그 방향이 생기지 않습니다.
    /// 듣는 쪽은 값을 더하기만 하고 덮어쓰지 않습니다. 덮어쓰면 활동이 만든 몫이 사라집니다.
    /// </remarks>
    public readonly struct ActivitySettlingEvent
    {
        #region 프로퍼티
        /// <summary>끝난 활동입니다.</summary>
        public IActivity Activity { get; }

        /// <summary>아직 반영되지 않은 활동 결과입니다. 듣는 쪽이 몫을 더할 수 있습니다.</summary>
        public ActivityResult Result { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 끝난 활동과 아직 반영되지 않은 결과를 담아 이벤트를 만듭니다.
        /// </summary>
        /// <param name="activity">끝난 활동입니다.</param>
        /// <param name="result">아직 반영되지 않은 활동 결과입니다.</param>
        public ActivitySettlingEvent(IActivity activity, ActivityResult result)
        {
            Activity = activity;
            Result = result;
        }
        #endregion // 함수
    }

    /// <summary>
    /// 활동이 끝나고 결과가 게임 상태에 반영되었음을 알리는 이벤트입니다.
    /// </summary>
    public readonly struct ActivityEndedEvent
    {
        #region 프로퍼티
        /// <summary>끝난 활동입니다.</summary>
        public IActivity Activity { get; }

        /// <summary>게임 상태에 반영된 활동 결과입니다.</summary>
        public ActivityResult Result { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 끝난 활동과 그 결과를 담아 이벤트를 만듭니다.
        /// </summary>
        /// <param name="activity">끝난 활동입니다.</param>
        /// <param name="result">게임 상태에 반영된 활동 결과입니다.</param>
        public ActivityEndedEvent(IActivity activity, ActivityResult result)
        {
            Activity = activity;
            Result = result;
        }
        #endregion // 함수
    }

    /// <summary>
    /// 하루가 마감되고 다음 날로 넘어갔음을 알리는 이벤트입니다.
    /// </summary>
    /// <remarks>
    /// 마감 화면과 마을 화면이 이 알림으로 갱신됩니다.
    /// 매니저를 직접 물어보게 두면 화면이 늘어날 때마다 매니저를 참조하는 곳이 늘어납니다.
    /// </remarks>
    public readonly struct DayEndedEvent
    {
        #region 프로퍼티
        /// <summary>넘어간 뒤의 날짜입니다.</summary>
        public int Day { get; }

        /// <summary>반영된 하루 마감 결과입니다.</summary>
        public DayEndResult Result { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 넘어간 날짜와 마감 결과를 담아 이벤트를 만듭니다.
        /// </summary>
        /// <param name="day">넘어간 뒤의 날짜입니다.</param>
        /// <param name="result">반영된 하루 마감 결과입니다.</param>
        public DayEndedEvent(int day, DayEndResult result)
        {
            Day = day;
            Result = result;
        }
        #endregion // 함수
    }
}
