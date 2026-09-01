namespace ProjectR.Data
{
    /// <summary>
    /// 스탯 정의 에셋의 코드명을 모아 둔 상수입니다.
    /// </summary>
    /// <remarks>
    /// 문자열 키는 오타가 실행할 때까지 드러나지 않습니다. 상수로 모으는 것이 유일한 방어입니다
    /// (체크리스트 1.4절). <c>PopupKeys</c>, <c>MonsterBlackboardKeys</c>와 같은 방식입니다.
    /// <para>
    /// 여기 적힌 값은 <c>Assets/02_Res/Stats</c>의 스탯 에셋 코드명과 같아야 합니다.
    /// <b>코드가 이름으로 찾는 스탯만 여기 둡니다.</b> 에셋만 있으면 되는 스탯은 여기 적지 않습니다.
    /// 목록이 스탯 전체와 같아지면, 스탯을 늘릴 때 코드를 고치지 않아도 된다는 이점이 사라집니다.
    /// </para>
    /// <para>
    /// 축 이름은 카테고리 에셋으로 붙입니다(기획서 9.1절의 신체 · 방송 · 장비 · 채널).
    /// 코드명에 축을 접두사로 넣지 않는 이유는 스탯이 다른 축으로 옮겨질 때
    /// 코드명까지 바뀌면 저장 데이터가 그 스탯을 못 찾기 때문입니다.
    /// </para>
    /// </remarks>
    public static class StatKeys
    {
        #region 상수
        /// <summary>가방의 가로 칸 수입니다.</summary>
        public const string BackpackWidth = "BackpackWidth";

        /// <summary>가방의 세로 칸 수입니다.</summary>
        /// <remarks>
        /// 가방 용량 업그레이드는 세로만 늘립니다. 가로를 늘리면 격자가 옆으로 자라
        /// 화면 우하단에 고정해 둔 자리(체크리스트 2.5절)를 밀어냅니다.
        /// </remarks>
        public const string BackpackHeight = "BackpackHeight";
        #endregion // 상수
    }
}
