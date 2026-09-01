using System;

using ProjectR.Activity;

namespace ProjectR.Broadcast
{
    /// <summary>
    /// 방송이 도는 동안 시청자 수와 후원금을 굴리는 계산기입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 6.1절의 "시청자 수 × 방송 재미 = 후원금"을 시간에 따라 굴리는 자리입니다.
    /// 방송 재미는 따로 든 수치가 아니라 지금 어떤 상황을 내보내고 있는지(<see cref="EBroadcastState"/>)로 대신합니다.
    /// 위험한 상황일수록 시청자도 후원도 커지므로, 안전하게 플레이하면 수익이 낮아집니다(설계 원칙 2번).
    /// <para>
    /// 소수점을 버리지 않고 <b>남는 몫으로 들고 있습니다.</b> 1초마다 잘라 버리면
    /// 분당 2명씩 들어오기로 한 유입이 초당 0.033명이라 매 틱 0으로 잘려 영영 늘지 않습니다.
    /// 이런 종류의 버그는 "왜 시청자가 안 늘지"로 보일 뿐 원인이 드러나지 않습니다.
    /// </para>
    /// <para>
    /// 유니티 타입을 쓰지 않으므로 씬도 플레이 모드도 없이 시험할 수 있습니다.
    /// 이 어셈블리는 백룸을 참조하지 않습니다. 백룸에서 벌어진 일은 이벤트로만 들어옵니다.
    /// </para>
    /// </remarks>
    public class BroadcastMeter
    {
        #region 필드
        private readonly BroadcastSettings settings;
        private readonly int startViewerCount;

        private float viewerRemainder;
        private float donationRemainder;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>지금 내보내고 있는 방송 상황입니다.</summary>
        public EBroadcastState State { get; private set; }

        /// <summary>지금 시청자 수입니다.</summary>
        public int ViewerCount { get; private set; }

        /// <summary>방송이 시작된 뒤로 모인 후원금입니다.</summary>
        public int Donation { get; private set; }

        /// <summary>방송이 시작될 때에 견준 시청자 수 변동량입니다.</summary>
        /// <remarks>활동 결과에 넣을 값입니다. 활동 결과는 절대값이 아니라 변동량을 받습니다.</remarks>
        public int ViewerDelta => ViewerCount - startViewerCount;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 시작 시청자 수와 조정값을 지정해 계산기를 만듭니다.
        /// </summary>
        /// <param name="settings">상황마다의 증감 규칙입니다.</param>
        /// <param name="startViewerCount">방송을 시작할 때의 시청자 수입니다.</param>
        public BroadcastMeter(BroadcastSettings settings, int startViewerCount)
        {
            this.settings = settings;
            this.startViewerCount = Math.Max(0, startViewerCount);

            ViewerCount = this.startViewerCount;
            State = EBroadcastState.Exploring;
        }

        /// <summary>
        /// 내보내고 있는 방송 상황을 바꿉니다.
        /// </summary>
        /// <param name="state">바꿀 방송 상황입니다.</param>
        public void SetState(EBroadcastState state)
        {
            State = state;
        }

        /// <summary>
        /// 흐른 시간만큼 시청자 수와 후원금을 굴립니다.
        /// </summary>
        /// <param name="deltaMinutes">흐른 시간(분)입니다. 0 이하이면 아무것도 하지 않습니다.</param>
        /// <remarks>
        /// 후원금을 먼저 셉니다. 시청자를 먼저 늘리면 그 분에 아직 들어오지도 않은 사람이 후원을 냅니다.
        /// </remarks>
        public void Tick(float deltaMinutes)
        {
            if (deltaMinutes <= 0f) return;

            BroadcastStateRate rate = settings.GetRate(State);

            AccrueDonation(rate, deltaMinutes);
            AccrueViewers(rate, deltaMinutes);
        }

        /// <summary>
        /// 흐른 시간만큼 후원금을 모읍니다.
        /// </summary>
        /// <param name="rate">지금 상황의 증감 규칙입니다.</param>
        /// <param name="deltaMinutes">흐른 시간(분)입니다.</param>
        private void AccrueDonation(BroadcastStateRate rate, float deltaMinutes)
        {
            donationRemainder += ViewerCount * rate.DonationPerViewerPerMinute * deltaMinutes;

            int earned = (int)donationRemainder;

            if (earned <= 0) return;

            donationRemainder -= earned;
            Donation += earned;
        }

        /// <summary>
        /// 흐른 시간만큼 시청자 수를 움직입니다.
        /// </summary>
        /// <param name="rate">지금 상황의 증감 규칙입니다.</param>
        /// <param name="deltaMinutes">흐른 시간(분)입니다.</param>
        private void AccrueViewers(BroadcastStateRate rate, float deltaMinutes)
        {
            float change = (rate.ViewerFlatPerMinute + ViewerCount * rate.ViewerRatePerMinute) * deltaMinutes;

            viewerRemainder += change;

            int moved = (int)viewerRemainder;

            if (moved == 0) return;

            viewerRemainder -= moved;
            ViewerCount = Math.Max(0, ViewerCount + moved);
        }
        #endregion // 함수
    }
}
