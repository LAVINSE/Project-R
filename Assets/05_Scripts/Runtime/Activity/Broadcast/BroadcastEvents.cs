namespace ProjectR.Activity
{
    /// <summary>
    /// 방송 상황이 바뀌었음을 알리는 이벤트입니다.
    /// </summary>
    /// <remarks>
    /// 백룸이 발행하고 방송이 듣습니다. 반대 방향은 없습니다.
    /// <b>백룸은 시청자 수를 모릅니다.</b> 이것이 체크리스트 1.7절이 유일한 상호 참조 금지로 못 박은 규칙이고,
    /// 어셈블리를 갈라 컴파일러가 지키게 해 둔 이유입니다.
    /// 백룸 안에서 시청자 수를 알고 싶어지는 순간이 반드시 오는데, 같은 어셈블리라면
    /// using 한 줄로 조용히 넘어가게 됩니다.
    /// </remarks>
    public readonly struct BroadcastStateChangedEvent
    {
        #region 프로퍼티
        /// <summary>바뀐 뒤의 방송 상황입니다.</summary>
        public EBroadcastState State { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 바뀐 방송 상황을 담아 이벤트를 만듭니다.
        /// </summary>
        /// <param name="state">바뀐 뒤의 방송 상황입니다.</param>
        public BroadcastStateChangedEvent(EBroadcastState state)
        {
            State = state;
        }
        #endregion // 함수
    }

    /// <summary>
    /// 방송 도중 무슨 일이 벌어졌음을 알리는 이벤트입니다.
    /// </summary>
    /// <remarks>
    /// 채팅과 후원 알림이 이 알림을 듣습니다.
    /// 지속 상태가 아니라 순간이므로, 같은 태그가 연달아 와도 그때마다 한 번씩 반응합니다.
    /// </remarks>
    public readonly struct BroadcastMomentEvent
    {
        #region 프로퍼티
        /// <summary>벌어진 일의 상황 태그입니다.</summary>
        public EBroadcastMoment Moment { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 상황 태그를 담아 이벤트를 만듭니다.
        /// </summary>
        /// <param name="moment">벌어진 일의 상황 태그입니다.</param>
        public BroadcastMomentEvent(EBroadcastMoment moment)
        {
            Moment = moment;
        }
        #endregion // 함수
    }
}
