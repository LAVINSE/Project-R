using NUnit.Framework;

using ProjectR.Activity;
using ProjectR.Broadcast;

namespace ProjectR.Tests
{
    /// <summary>
    /// 방송 도중 시청자 수와 후원금이 굴러가는 계산을 검증하는 테스트입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 6.1절의 수익 구조가 실제로 그 방향으로 도는지를 봅니다.
    /// 특히 소수점을 남는 몫으로 들고 가는 부분은 눈으로 확인할 수 없는 자리입니다.
    /// 잘라 버려도 화면에서는 "시청자가 안 느네"로만 보입니다.
    /// </remarks>
    public class BroadcastMeterTests
    {
        #region 함수
        /// <summary>
        /// 검산하기 쉬운 값으로 채운 조정값을 만듭니다.
        /// </summary>
        /// <returns>탐험은 분당 2명, 추격은 분당 10명 + 10%인 조정값입니다.</returns>
        private static BroadcastSettings CreateSettings()
        {
            return new BroadcastSettings(
                hidden: new BroadcastStateRate(-2f, -0.1f, 0f),
                exploring: new BroadcastStateRate(2f, 0f, 0.1f),
                mission: new BroadcastStateRate(5f, 0f, 0.2f),
                chased: new BroadcastStateRate(10f, 0.1f, 0.5f),
                revealing: new BroadcastStateRate(20f, 0.2f, 1f));
        }

        /// <summary>
        /// 탐험 중에는 시청자가 조금씩 늘어나는지 확인합니다.
        /// </summary>
        [Test]
        public void 탐험_중에는_시청자가_조금씩_는다()
        {
            BroadcastMeter meter = new BroadcastMeter(CreateSettings(), 100);

            meter.SetState(EBroadcastState.Exploring);
            meter.Tick(10f);

            Assert.AreEqual(120, meter.ViewerCount);
            Assert.AreEqual(20, meter.ViewerDelta);
        }

        /// <summary>
        /// 숨어 있으면 시청자가 빠져나가는지 확인합니다.
        /// </summary>
        /// <remarks>기획서 6.1절의 "안전한 곳에 은신 → 이탈"입니다.</remarks>
        [Test]
        public void 숨어_있으면_시청자가_빠진다()
        {
            BroadcastMeter meter = new BroadcastMeter(CreateSettings(), 100);

            meter.SetState(EBroadcastState.Hidden);
            meter.Tick(1f);

            Assert.AreEqual(88, meter.ViewerCount);
            Assert.Less(meter.ViewerDelta, 0);
        }

        /// <summary>
        /// 추격이 탐험보다 시청자와 후원을 모두 크게 만드는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 기획서 설계 원칙 2번 "살아남기와 재미있기는 충돌해야 한다"가 여기에 걸립니다.
        /// 위험한 상황에서 수익이 낮으면 플레이어가 위험을 고를 이유가 없어집니다.
        /// </remarks>
        [Test]
        public void 추격이_탐험보다_시청자와_후원을_모두_크게_만든다()
        {
            BroadcastMeter exploring = new BroadcastMeter(CreateSettings(), 100);
            BroadcastMeter chased = new BroadcastMeter(CreateSettings(), 100);

            exploring.SetState(EBroadcastState.Exploring);
            chased.SetState(EBroadcastState.Chased);

            exploring.Tick(5f);
            chased.Tick(5f);

            Assert.Greater(chased.ViewerDelta, exploring.ViewerDelta);
            Assert.Greater(chased.Donation, exploring.Donation);
        }

        /// <summary>
        /// 잘게 나눠 굴려도 한 번에 굴린 것과 같은 결과가 나오는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 소수점을 남는 몫으로 들고 가는 것을 확인하는 테스트입니다.
        /// 매 틱 잘라 버리면 초 단위로 굴릴 때 분당 2명이 영영 0명이 됩니다.
        /// </remarks>
        [Test]
        public void 잘게_나눠_굴려도_한_번에_굴린_것과_같다()
        {
            BroadcastMeter atOnce = new BroadcastMeter(CreateSettings(), 100);
            BroadcastMeter bitByBit = new BroadcastMeter(CreateSettings(), 100);

            atOnce.SetState(EBroadcastState.Exploring);
            bitByBit.SetState(EBroadcastState.Exploring);

            atOnce.Tick(10f);

            for (int i = 0; i < 600; i++) bitByBit.Tick(1f / 60f);

            Assert.AreEqual(atOnce.ViewerCount, bitByBit.ViewerCount);
        }

        /// <summary>
        /// 초 단위로 굴려도 시청자가 늘어나는지 확인합니다.
        /// </summary>
        /// <remarks>소수점을 잘라 버렸을 때 실제로 나타나는 증상을 그대로 잡는 테스트입니다.</remarks>
        [Test]
        public void 초_단위로_굴려도_시청자가_는다()
        {
            BroadcastMeter meter = new BroadcastMeter(CreateSettings(), 100);

            meter.SetState(EBroadcastState.Exploring);

            for (int i = 0; i < 60; i++) meter.Tick(1f / 60f);

            Assert.AreEqual(102, meter.ViewerCount);
        }

        /// <summary>
        /// 후원금이 시청자 수에 비례하는지 확인합니다.
        /// </summary>
        /// <remarks>기획서 6.1절의 "시청자 수 × 방송 재미 = 후원금"입니다.</remarks>
        [Test]
        public void 후원금이_시청자_수에_비례한다()
        {
            BroadcastMeter small = new BroadcastMeter(CreateSettings(), 100);
            BroadcastMeter large = new BroadcastMeter(CreateSettings(), 1000);

            small.SetState(EBroadcastState.Exploring);
            large.SetState(EBroadcastState.Exploring);

            small.Tick(1f);
            large.Tick(1f);

            Assert.AreEqual(10, small.Donation);
            Assert.AreEqual(100, large.Donation);
        }

        /// <summary>
        /// 시청자가 0 아래로 내려가지 않는지 확인합니다.
        /// </summary>
        [Test]
        public void 시청자는_0_아래로_내려가지_않는다()
        {
            BroadcastMeter meter = new BroadcastMeter(CreateSettings(), 10);

            meter.SetState(EBroadcastState.Hidden);
            meter.Tick(100f);

            Assert.AreEqual(0, meter.ViewerCount);
        }

        /// <summary>
        /// 시간이 흐르지 않으면 아무것도 움직이지 않는지 확인합니다.
        /// </summary>
        [Test]
        public void 시간이_흐르지_않으면_움직이지_않는다()
        {
            BroadcastMeter meter = new BroadcastMeter(CreateSettings(), 100);

            meter.SetState(EBroadcastState.Chased);
            meter.Tick(0f);
            meter.Tick(-5f);

            Assert.AreEqual(100, meter.ViewerCount);
            Assert.AreEqual(0, meter.Donation);
        }

        /// <summary>
        /// 후원을 먼저 세고 시청자를 늘리는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 순서가 바뀌면 그 분에 아직 들어오지도 않은 시청자가 후원을 냅니다.
        /// 시청자 100명이 분당 0.1씩 내면 1분에 10이어야 하고, 늘어난 2명은 다음 분부터 셉니다.
        /// </remarks>
        [Test]
        public void 후원은_늘어나기_전_시청자_수로_센다()
        {
            BroadcastMeter meter = new BroadcastMeter(CreateSettings(), 100);

            meter.SetState(EBroadcastState.Exploring);
            meter.Tick(1f);

            Assert.AreEqual(10, meter.Donation);
            Assert.AreEqual(102, meter.ViewerCount);
        }
        #endregion // 함수
    }
}
