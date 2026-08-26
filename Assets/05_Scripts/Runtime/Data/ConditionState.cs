using System;

using UnityEngine;

namespace ProjectR.Data
{
    /// <summary>
    /// 스트리머의 현재 컨디션 네 수치를 보관하는 상태 객체입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 5장 기준으로 수면, 배고픔, 기분, 피로도를 갖습니다.
    /// 피로도가 나머지 수치의 상한을 눌러 내리는 계산은 1일 시연판 단계에서 추가합니다.
    /// </remarks>
    [Serializable]
    public class ConditionState
    {
        #region 상수
        /// <summary>각 컨디션 수치의 최소값입니다.</summary>
        public const int MinValue = 0;

        /// <summary>각 컨디션 수치의 최대값입니다.</summary>
        public const int MaxValue = 100;
        #endregion // 상수

        #region 필드
        [SerializeField] private int sleep = MaxValue;
        [SerializeField] private int hunger = MaxValue;
        [SerializeField] private int mood = MaxValue;
        [SerializeField] private int fatigue = MinValue;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>수면 수치입니다. 낮을수록 백룸에서 시야가 흐려집니다.</summary>
        public int Sleep => sleep;

        /// <summary>배고픔 수치입니다. 낮을수록 달리기 지속 시간이 줄어듭니다.</summary>
        public int Hunger => hunger;

        /// <summary>기분 수치입니다. 낮을수록 리액션 판정이 둔해집니다.</summary>
        public int Mood => mood;

        /// <summary>피로도 수치입니다. 높을수록 나머지 수치의 회복 상한이 낮아집니다.</summary>
        public int Fatigue => fatigue;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 변동량을 적용합니다. 결과는 최소값과 최대값 사이로 잘립니다.
        /// </summary>
        /// <param name="delta">적용할 컨디션 변동량입니다.</param>
        public void Apply(ConditionDelta delta)
        {
            sleep = Clamp(sleep + delta.Sleep);
            hunger = Clamp(hunger + delta.Hunger);
            mood = Clamp(mood + delta.Mood);
            fatigue = Clamp(fatigue + delta.Fatigue);
        }

        /// <summary>
        /// 값을 컨디션 수치 범위 안으로 잘라 냅니다.
        /// </summary>
        /// <param name="value">잘라 낼 원본 값입니다.</param>
        /// <returns>최소값과 최대값 사이로 보정된 값입니다.</returns>
        private static int Clamp(int value)
        {
            return Mathf.Clamp(value, MinValue, MaxValue);
        }
        #endregion // 함수
    }
}
