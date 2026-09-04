using System;

using UnityEngine;

namespace ProjectR.Data
{
    /// <summary>
    /// 스트리머의 현재 컨디션 네 수치를 보관하는 상태 객체입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 5장 기준으로 수면, 배고픔, 기분, 피로도를 갖습니다.
    /// 피로도만 성격이 다릅니다. 나머지 셋은 각자 백룸에 개입하지만,
    /// 피로도는 <b>나머지 셋의 최대치를 눌러 내리는 것</b>으로 개입합니다.
    /// 그래서 피로도가 높으면 수면·배고픔·기분을 아무리 채워도 채워지지 않습니다.
    /// 이 규칙이 "쉬면 채널이 죽고, 안 쉬면 몸이 죽는다"(기획서 설계 원칙 3번)를 만듭니다.
    /// 눌러 내리는 데에 하한선을 둔 것은 설계 원칙 4번(모든 불이익에 하한선을 둔다) 때문입니다.
    /// 하한선이 없으면 피로도가 최대일 때 아무것도 회복할 수 없어 실패가 실패를 부릅니다.
    /// 이 클래스는 저장 파일에 그대로 들어가는 데이터라 인스펙터 필드를 가질 수 없습니다.
    /// 그래서 눌러 내리는 폭을 상수로 두었습니다. 스트리머마다 다르게 만들 때가 오면
    /// 스트리머 정의 에셋으로 옮깁니다(체크리스트 5.1절).
    /// </remarks>
    [Serializable]
    public class ConditionState
    {
        #region 상수
        /// <summary>각 컨디션 수치의 최소값입니다.</summary>
        public const int MinimumValue = 0;

        /// <summary>각 컨디션 수치의 최대값입니다.</summary>
        public const int MaximumValue = 100;

        /// <summary>피로도가 최대일 때에도 남겨 두는 수면·배고픔·기분의 상한입니다.</summary>
        /// <remarks>
        /// 하한선입니다. 이 값이 0이면 피로도가 최대일 때 아무것도 회복할 수 없게 되어
        /// 실패가 실패를 부르는 구조가 됩니다(기획서 설계 원칙 4번).
        /// </remarks>
        public const int MinimumUpperLimitValue = 40;
        #endregion // 상수

        #region 필드
        /// <summary>수면 수치입니다. 낮을수록 백룸에서 시야가 흐려집니다.</summary>
        [SerializeField] private int sleep = MaximumValue;

        /// <summary>배고픔 수치입니다. 낮을수록 달리기 지속 시간이 줄어듭니다.</summary>
        [SerializeField] private int hunger = MaximumValue;

        /// <summary>기분 수치입니다. 낮을수록 리액션 판정이 둔해집니다.</summary>
        [SerializeField] private int mood = MaximumValue;

        /// <summary>피로도 수치입니다. 높을수록 나머지 세 수치의 상한이 낮아집니다.</summary>
        [SerializeField] private int fatigue = MinimumValue;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>수면 수치입니다. 낮을수록 백룸에서 시야가 흐려집니다.</summary>
        public int Sleep => sleep;

        /// <summary>배고픔 수치입니다. 낮을수록 달리기 지속 시간이 줄어듭니다.</summary>
        public int Hunger => hunger;

        /// <summary>기분 수치입니다. 낮을수록 리액션 판정이 둔해집니다.</summary>
        public int Mood => mood;

        /// <summary>피로도 수치입니다. 높을수록 나머지 세 수치의 상한이 낮아집니다.</summary>
        public int Fatigue => fatigue;

        /// <summary>지금 피로도에서 수면·배고픔·기분이 올라갈 수 있는 상한입니다.</summary>
        /// <remarks>
        /// 피로도 0이면 <see cref="MaximumValue"/>이고, 피로도가 최대면 <see cref="MinimumUpperLimitValue"/>입니다.
        /// 그 사이는 선형입니다. 피로도 50이면 상한이 70입니다.
        /// 곡선을 쓰지 않은 것은 플레이어가 "피로도를 절반 풀면 절반 돌아온다"고 예상할 수 있어야
        /// 휴방을 며칠 할지 스스로 계산할 수 있기 때문입니다.
        /// </remarks>
        public int EffectiveMaximum
        {
            get
            {
                int pressure = (MaximumValue - MinimumUpperLimitValue) * fatigue / MaximumValue;

                return MaximumValue - pressure;
            }
        }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 변동량을 적용합니다. 결과는 최소값과 지금 상한 사이로 잘립니다.
        /// </summary>
        /// <param name="delta">적용할 컨디션 변동량입니다.</param>
        /// <remarks>
        /// 피로도를 먼저 반영한 뒤 나머지 셋을 자릅니다. 순서를 바꾸면
        /// 같은 호출 안에서 피로도가 오르고 수면도 오르는 경우에 새 상한이 적용되지 않습니다.
        /// </remarks>
        public void Apply(ConditionDelta delta)
        {
            fatigue = Mathf.Clamp(fatigue + delta.Fatigue, MinimumValue, MaximumValue);

            sleep = ClampToUpperLimit(sleep + delta.Sleep);
            hunger = ClampToUpperLimit(hunger + delta.Hunger);
            mood = ClampToUpperLimit(mood + delta.Mood);
        }

        /// <summary>
        /// 값을 최소값과 지금 상한 사이로 잘라 냅니다.
        /// </summary>
        /// <param name="value">잘라 낼 원본 값입니다.</param>
        /// <returns>최소값과 <see cref="EffectiveMaximum"/> 사이로 보정된 값입니다.</returns>
        private int ClampToUpperLimit(int value)
        {
            return Mathf.Clamp(value, MinimumValue, EffectiveMaximum);
        }
        #endregion // 함수
    }
}
