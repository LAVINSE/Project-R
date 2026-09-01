using System;

using UnityEngine;

namespace ProjectR.Broadcast
{
    /// <summary>
    /// 방송 상황 하나에서 시청자와 후원이 어떻게 움직이는지를 적은 한 줄입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 6.1절의 수익 구조 표에서 한 행에 해당합니다.
    /// 유입을 인원과 비율 둘로 나눠 둔 이유가 있습니다.
    /// 비율만 쓰면 시청자가 0일 때 아무리 위험한 짓을 해도 영원히 0으로 남고,
    /// 인원만 쓰면 대형 채널에서 추격의 급증이 체감되지 않습니다.
    /// 초반에는 인원이, 후반에는 비율이 눈에 보이게 됩니다.
    /// </remarks>
    [Serializable]
    public struct BroadcastStateRate
    {
        #region 필드
        [SerializeField, Tooltip("이 상황에서 1분마다 들어오거나 빠지는 시청자 수입니다. 음수면 이탈입니다.")]
        private float viewerFlatPerMinute;

        [SerializeField, Tooltip("이 상황에서 1분마다 현재 시청자 수에 곱해 더하는 비율입니다. 음수면 이탈입니다.")]
        private float viewerRatePerMinute;

        [SerializeField, Min(0f), Tooltip("이 상황에서 시청자 한 명이 1분에 내는 후원금입니다.")]
        private float donationPerViewerPerMinute;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>1분마다 들어오거나 빠지는 시청자 수입니다.</summary>
        public float ViewerFlatPerMinute => viewerFlatPerMinute;

        /// <summary>1분마다 현재 시청자 수에 곱해 더하는 비율입니다.</summary>
        public float ViewerRatePerMinute => viewerRatePerMinute;

        /// <summary>시청자 한 명이 1분에 내는 후원금입니다.</summary>
        public float DonationPerViewerPerMinute => donationPerViewerPerMinute;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 한 상황의 증감 규칙을 만듭니다.
        /// </summary>
        /// <param name="viewerFlatPerMinute">1분마다 들어오거나 빠지는 시청자 수입니다.</param>
        /// <param name="viewerRatePerMinute">1분마다 현재 시청자 수에 곱해 더하는 비율입니다.</param>
        /// <param name="donationPerViewerPerMinute">시청자 한 명이 1분에 내는 후원금입니다.</param>
        public BroadcastStateRate(float viewerFlatPerMinute, float viewerRatePerMinute,
            float donationPerViewerPerMinute)
        {
            this.viewerFlatPerMinute = viewerFlatPerMinute;
            this.viewerRatePerMinute = viewerRatePerMinute;
            this.donationPerViewerPerMinute = donationPerViewerPerMinute;
        }
        #endregion // 함수
    }
}
