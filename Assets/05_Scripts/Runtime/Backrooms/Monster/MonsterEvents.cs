using ProjectR.Enum;

namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// 몬스터의 행동 모드가 바뀌었음을 알리는 이벤트입니다.
    /// </summary>
    /// <remarks>
    /// 소리 재생은 <see cref="MonsterVoice"/>가 맡지만, 모드 변화는 방송 연출이나 시청자 반응 같은
    /// 다른 시스템도 쓸 수 있는 정보라 이벤트로도 함께 알립니다.
    /// </remarks>
    public readonly struct MonsterModeChangedEvent
    {
        #region 프로퍼티
        /// <summary>바뀌기 전의 모드입니다.</summary>
        public EMonsterMode PreviousMode { get; }

        /// <summary>바뀐 뒤의 모드입니다.</summary>
        public EMonsterMode CurrentMode { get; }
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 바뀌기 전후의 모드를 담아 이벤트를 만듭니다.
        /// </summary>
        /// <param name="previousMode">바뀌기 전의 모드입니다.</param>
        /// <param name="currentMode">바뀐 뒤의 모드입니다.</param>
        public MonsterModeChangedEvent(EMonsterMode previousMode, EMonsterMode currentMode)
        {
            PreviousMode = previousMode;
            CurrentMode = currentMode;
        }
        #endregion // 생성자
    }
}
