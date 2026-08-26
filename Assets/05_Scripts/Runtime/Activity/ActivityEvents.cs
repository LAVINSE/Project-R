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
}
