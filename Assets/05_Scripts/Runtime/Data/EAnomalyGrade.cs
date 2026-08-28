namespace ProjectR.Data
{
    /// <summary>
    /// 이상물체의 등급입니다.
    /// </summary>
    /// <remarks>
    /// 등급은 정산 수치를 만들지 않습니다. 수치는 정의 에셋에 직접 적습니다.
    /// 등급은 좋은 물건을 주웠다는 것을 한눈에 알아보게 하는 표시 용도입니다.
    /// </remarks>
    public enum EAnomalyGrade
    {
        /// <summary>흔하게 나오는 등급입니다.</summary>
        Common = 0,

        /// <summary>드물게 나오는 등급입니다.</summary>
        Rare = 1,

        /// <summary>거의 나오지 않는 등급입니다.</summary>
        Special = 2,
    }
}
