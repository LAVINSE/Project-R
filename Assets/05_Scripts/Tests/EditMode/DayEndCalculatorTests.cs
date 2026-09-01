using NUnit.Framework;

using ProjectR.Data;

namespace ProjectR.Tests
{
    /// <summary>
    /// 하루 마감의 유지비, 구독자 이탈, 다음 날 방송 시간 계산을 검증하는 테스트입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 8.3절이 유지비에 맡긴 역할은 "성장을 멈추면 후퇴한다"입니다.
    /// 그 구조가 실제로 서 있는지는 하루를 여러 번 돌려 봐야 눈에 보이는데,
    /// 여기서는 한 번의 계산만으로 같은 것을 확인합니다.
    /// </remarks>
    public class DayEndCalculatorTests
    {
        #region 함수
        /// <summary>
        /// 테스트에서 쓸 조정값을 만듭니다.
        /// </summary>
        /// <returns>검산하기 쉬운 값으로 채운 조정값입니다.</returns>
        /// <remarks>
        /// 기본값 대신 딱 떨어지는 수를 씁니다. 기본값이 조정되면 테스트가 함께 깨지는데,
        /// 그것은 계산이 틀린 것이 아니라 숫자를 만진 것이라 깨질 이유가 없습니다.
        /// </remarks>
        private static DayEndSettings CreateSettings()
        {
            return new DayEndSettings(
                baseUpkeep: 500,
                upkeepPerViewer: 0.1f,
                idleChurnRate: 0.1f,
                activeChurnRate: 0.02f,
                baseBroadcastMinutes: 240,
                viewersPerStep: 1000,
                minutesPerStep: 30,
                failurePenaltyMinutes: 60,
                minBroadcastMinutes: 120);
        }

        /// <summary>
        /// 시청자가 없어도 고정 지출은 나가는지 확인합니다.
        /// </summary>
        [Test]
        public void 시청자가_없어도_고정_지출은_나간다()
        {
            DayEndResult result = DayEndCalculator.Calculate(CreateSettings(), 0, 1, false);

            Assert.AreEqual(500, result.UpkeepCost);
        }

        /// <summary>
        /// 채널이 커지면 유지비도 함께 커지는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 기획서 8.3절의 "규모 비례"입니다. 이것이 없으면 시청자가 늘수록 게임이
        /// 쉬워지기만 해서 엔드리스의 압박이 사라집니다.
        /// </remarks>
        [Test]
        public void 채널이_커지면_유지비도_커진다()
        {
            DayEndSettings settings = CreateSettings();

            int smallChannel = DayEndCalculator.Calculate(settings, 1000, 1, false).UpkeepCost;
            int largeChannel = DayEndCalculator.Calculate(settings, 10000, 1, false).UpkeepCost;

            Assert.AreEqual(600, smallChannel);
            Assert.AreEqual(1500, largeChannel);
            Assert.Greater(largeChannel, smallChannel);
        }

        /// <summary>
        /// 쉰 날이 방송한 날보다 많이 빠져나가는지 확인합니다.
        /// </summary>
        /// <remarks>기획서 설계 원칙 3번 "쉬면 채널이 죽는다"가 여기에 걸립니다.</remarks>
        [Test]
        public void 쉰_날이_방송한_날보다_많이_빠진다()
        {
            DayEndSettings settings = CreateSettings();

            int idleLoss = DayEndCalculator.Calculate(settings, 1000, 0, false).ViewerLoss;
            int activeLoss = DayEndCalculator.Calculate(settings, 1000, 1, false).ViewerLoss;

            Assert.AreEqual(100, idleLoss);
            Assert.AreEqual(20, activeLoss);
            Assert.Greater(idleLoss, activeLoss);
        }

        /// <summary>
        /// 시청자가 적어도 이탈이 0으로 사라지지 않는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 내림으로 계산하면 시청자 5명에 이탈률 2%가 0명이 되어 압박이 없어집니다.
        /// 올림이라 최소 1명은 빠집니다.
        /// </remarks>
        [Test]
        public void 시청자가_적어도_이탈이_0이_되지_않는다()
        {
            DayEndResult result = DayEndCalculator.Calculate(CreateSettings(), 5, 1, false);

            Assert.AreEqual(1, result.ViewerLoss);
        }

        /// <summary>
        /// 시청자가 아예 없으면 이탈도 없는지 확인합니다.
        /// </summary>
        [Test]
        public void 시청자가_없으면_이탈도_없다()
        {
            DayEndResult result = DayEndCalculator.Calculate(CreateSettings(), 0, 0, false);

            Assert.AreEqual(0, result.ViewerLoss);
        }

        /// <summary>
        /// 이탈이 가진 시청자보다 많아지지 않는지 확인합니다.
        /// </summary>
        [Test]
        public void 이탈이_가진_시청자보다_많아지지_않는다()
        {
            DayEndSettings settings = new DayEndSettings(
                baseUpkeep: 0, upkeepPerViewer: 0f, idleChurnRate: 1f, activeChurnRate: 1f,
                baseBroadcastMinutes: 240, viewersPerStep: 0, minutesPerStep: 0,
                failurePenaltyMinutes: 0, minBroadcastMinutes: 0);

            DayEndResult result = DayEndCalculator.Calculate(settings, 10, 0, false);

            Assert.AreEqual(10, result.ViewerLoss);
        }

        /// <summary>
        /// 채널이 커지면 다음 날 방송 시간이 늘어나는지 확인합니다.
        /// </summary>
        [Test]
        public void 채널이_커지면_방송_시간이_늘어난다()
        {
            DayEndSettings settings = CreateSettings();

            Assert.AreEqual(240, DayEndCalculator.Calculate(settings, 0, 1, false).NextBroadcastMinutes);
            Assert.AreEqual(240, DayEndCalculator.Calculate(settings, 999, 1, false).NextBroadcastMinutes);
            Assert.AreEqual(270, DayEndCalculator.Calculate(settings, 1000, 1, false).NextBroadcastMinutes);
            Assert.AreEqual(330, DayEndCalculator.Calculate(settings, 3500, 1, false).NextBroadcastMinutes);
        }

        /// <summary>
        /// 탐험에 실패하면 다음 날 방송 시간이 줄어드는지 확인합니다.
        /// </summary>
        [Test]
        public void 탐험에_실패하면_다음_날_방송_시간이_줄어든다()
        {
            DayEndSettings settings = CreateSettings();

            DayEndResult result = DayEndCalculator.Calculate(settings, 0, 1, true);

            Assert.AreEqual(180, result.NextBroadcastMinutes);
            Assert.IsTrue(result.IsPenalized);
        }

        /// <summary>
        /// 실패가 겹쳐도 방송 시간이 하한선 아래로 내려가지 않는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 기획서 설계 원칙 4번의 하한선입니다. 하한선이 없으면 한 번 실패한 사람이
        /// 방송 시간이 없어 회복도 못 하는 실패가 실패를 부르는 구조가 됩니다.
        /// </remarks>
        [Test]
        public void 실패해도_방송_시간이_하한선_아래로_내려가지_않는다()
        {
            DayEndSettings settings = new DayEndSettings(
                baseUpkeep: 0, upkeepPerViewer: 0f, idleChurnRate: 0f, activeChurnRate: 0f,
                baseBroadcastMinutes: 240, viewersPerStep: 0, minutesPerStep: 0,
                failurePenaltyMinutes: 999, minBroadcastMinutes: 120);

            DayEndResult result = DayEndCalculator.Calculate(settings, 0, 1, true);

            Assert.AreEqual(120, result.NextBroadcastMinutes);
        }

        /// <summary>
        /// 실패가 없으면 깎였다는 표시가 붙지 않는지 확인합니다.
        /// </summary>
        [Test]
        public void 실패가_없으면_깎였다는_표시가_붙지_않는다()
        {
            DayEndResult result = DayEndCalculator.Calculate(CreateSettings(), 1000, 2, false);

            Assert.IsFalse(result.IsPenalized);
        }
        #endregion // 함수
    }
}
