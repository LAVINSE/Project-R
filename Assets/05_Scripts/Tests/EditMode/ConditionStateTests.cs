using NUnit.Framework;

using ProjectR.Data;

namespace ProjectR.Tests
{
    /// <summary>
    /// 피로도가 나머지 세 수치의 상한을 눌러 내리는 계산을 검증하는 테스트입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 5장의 "피로도가 높으면 아무리 채워도 최대치 자체가 낮아진다"가 실제로 그렇게 도는지를 봅니다.
    /// 눈으로는 확인하기 어려운 규칙입니다. 수면을 채우는 것과 채워지지 않는 것이
    /// 화면에서는 같은 동작으로 보이기 때문입니다.
    /// </remarks>
    public class ConditionStateTests
    {
        #region 함수
        /// <summary>
        /// 피로도가 없으면 상한이 최대값 그대로인지 확인합니다.
        /// </summary>
        [Test]
        public void 피로도가_0이면_상한이_최대값이다()
        {
            ConditionState condition = new();

            Assert.AreEqual(ConditionState.MaximumValue, condition.EffectiveMaximum);
        }

        /// <summary>
        /// 피로도가 최대여도 상한이 하한선 아래로 내려가지 않는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 하한선이 무너지면 피로도가 최대일 때 아무것도 회복할 수 없어
        /// 실패가 실패를 부르는 구조가 됩니다(기획서 설계 원칙 4번).
        /// </remarks>
        [Test]
        public void 피로도가_최대여도_상한이_하한선_아래로_내려가지_않는다()
        {
            ConditionState condition = new();

            condition.Apply(new ConditionDelta { Fatigue = ConditionState.MaximumValue });

            Assert.AreEqual(ConditionState.MinimumUpperLimitValue, condition.EffectiveMaximum);
        }

        /// <summary>
        /// 피로도 절반이면 눌러 내리는 폭도 절반인지 확인합니다.
        /// </summary>
        /// <remarks>선형이어야 플레이어가 휴방을 며칠 할지 스스로 계산할 수 있습니다.</remarks>
        [Test]
        public void 피로도_절반이면_눌러_내리는_폭도_절반이다()
        {
            ConditionState condition = new();

            condition.Apply(new ConditionDelta { Fatigue = 50 });

            Assert.AreEqual(70, condition.EffectiveMaximum);
        }

        /// <summary>
        /// 피로도가 오르면 이미 차 있던 수치가 새 상한까지 눌리는지 확인합니다.
        /// </summary>
        [Test]
        public void 피로도가_오르면_이미_찬_수치가_상한까지_눌린다()
        {
            ConditionState condition = new();

            Assert.AreEqual(ConditionState.MaximumValue, condition.Sleep);

            condition.Apply(new ConditionDelta { Fatigue = 100 });

            Assert.AreEqual(ConditionState.MinimumUpperLimitValue, condition.Sleep);
            Assert.AreEqual(ConditionState.MinimumUpperLimitValue, condition.Hunger);
            Assert.AreEqual(ConditionState.MinimumUpperLimitValue, condition.Mood);
        }

        /// <summary>
        /// 상한 위로는 아무리 채워도 올라가지 않는지 확인합니다.
        /// </summary>
        /// <remarks>기획서 5장이 요구하는 핵심 동작입니다.</remarks>
        [Test]
        public void 상한_위로는_아무리_채워도_올라가지_않는다()
        {
            ConditionState condition = new();

            condition.Apply(new ConditionDelta { Fatigue = 100, Sleep = -100 });

            Assert.AreEqual(0, condition.Sleep);

            condition.Apply(new ConditionDelta { Sleep = 999 });

            Assert.AreEqual(ConditionState.MinimumUpperLimitValue, condition.Sleep);
        }

        /// <summary>
        /// 같은 호출에서 피로도가 오르면 그 호출의 회복분에도 새 상한이 걸리는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 순서 실수를 잡는 테스트입니다. 나머지 셋을 먼저 자르고 피로도를 나중에 반영하면
        /// 이 호출에서만 상한을 넘긴 값이 남습니다.
        /// </remarks>
        [Test]
        public void 같은_호출에서_오른_피로도가_그_회복분에도_걸린다()
        {
            ConditionState condition = new();

            condition.Apply(new ConditionDelta { Sleep = -100 });

            Assert.AreEqual(0, condition.Sleep);

            condition.Apply(new ConditionDelta { Fatigue = 100, Sleep = 100 });

            Assert.AreEqual(ConditionState.MinimumUpperLimitValue, condition.Sleep);
        }

        /// <summary>
        /// 피로도를 풀면 상한은 돌아오지만 수치가 저절로 오르지는 않는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 휴방으로 피로도를 풀어도 잠은 따로 자야 한다는 뜻입니다.
        /// 상한이 돌아올 때 값도 함께 오르면 휴방 하나로 전부 해결되어
        /// 회복 활동을 고르는 선택이 사라집니다.
        /// </remarks>
        [Test]
        public void 피로도를_풀면_상한만_돌아오고_수치는_그대로다()
        {
            ConditionState condition = new();

            condition.Apply(new ConditionDelta { Fatigue = 100 });

            Assert.AreEqual(ConditionState.MinimumUpperLimitValue, condition.Sleep);

            condition.Apply(new ConditionDelta { Fatigue = -100 });

            Assert.AreEqual(ConditionState.MaximumValue, condition.EffectiveMaximum);
            Assert.AreEqual(ConditionState.MinimumUpperLimitValue, condition.Sleep);
        }

        /// <summary>
        /// 피로도 자체는 상한에 눌리지 않는지 확인합니다.
        /// </summary>
        /// <remarks>피로도까지 상한에 걸리면 자기가 자기를 눌러 최대까지 오르지 못합니다.</remarks>
        [Test]
        public void 피로도_자체는_상한에_눌리지_않는다()
        {
            ConditionState condition = new();

            condition.Apply(new ConditionDelta { Fatigue = ConditionState.MaximumValue });

            Assert.AreEqual(ConditionState.MaximumValue, condition.Fatigue);
        }
        #endregion // 함수
    }
}
