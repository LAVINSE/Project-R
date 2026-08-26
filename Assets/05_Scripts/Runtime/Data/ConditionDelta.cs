using System;

namespace ProjectR.Data
{
    /// <summary>
    /// 활동 결과로 발생하는 컨디션 네 수치의 변동량입니다.
    /// </summary>
    /// <remarks>
    /// 변동량이므로 음수를 가질 수 있습니다. 실제 상한과 하한 처리는 <see cref="ConditionState"/>가 담당합니다.
    /// </remarks>
    [Serializable]
    public struct ConditionDelta
    {
        #region 필드
        /// <summary>수면 수치의 변동량입니다.</summary>
        public int Sleep;

        /// <summary>배고픔 수치의 변동량입니다.</summary>
        public int Hunger;

        /// <summary>기분 수치의 변동량입니다.</summary>
        public int Mood;

        /// <summary>피로도 수치의 변동량입니다.</summary>
        public int Fatigue;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>변동이 전혀 없는 값입니다.</summary>
        public static ConditionDelta None => default;
        #endregion // 프로퍼티
    }
}
