using System;

using UnityEngine;

using SW.Attributes;

using ProjectR.Activity;

namespace ProjectR.Broadcast
{
    /// <summary>
    /// 방송 상황마다의 시청자·후원 증감 규칙을 모아 둔 조정값입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 6.1절의 표를 그대로 옮긴 것입니다.
    /// 배열이 아니라 상황마다 이름 붙은 필드로 둔 이유는, 배열이면 인스펙터에서
    /// 몇 번째 줄이 어느 상황인지 알 수 없고 상황을 하나 끼워 넣을 때 줄이 통째로 밀리기 때문입니다.
    /// </remarks>
    [Serializable]
    public struct BroadcastSettings
    {
        #region 필드
        [SWGroup("상황별 증감")]
        [SerializeField, Tooltip("안전한 곳에 숨어 있을 때입니다. 시청자가 빠지고 후원이 없습니다.")]
        private BroadcastStateRate hidden;

        [SerializeField, Tooltip("탐험 중일 때입니다. 시청자가 유지되고 후원이 소액 들어옵니다.")]
        private BroadcastStateRate exploring;

        [SerializeField, Tooltip("미션을 수행 중일 때입니다. 시청자가 오릅니다.")]
        private BroadcastStateRate mission;

        [SerializeField, Tooltip("몬스터에게 쫓기고 있을 때입니다. 시청자가 급증하고 후원이 터집니다.")]
        private BroadcastStateRate chased;

        [SerializeField, Tooltip("이상물체를 공개하고 있을 때입니다. 시청자가 급증하고 후원이 대량으로 들어옵니다.")]
        private BroadcastStateRate revealing;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>인스펙터에 값을 채우기 전에 쓰는 기본 조정값입니다.</summary>
        /// <remarks>
        /// 기획서 6.1절의 표에서 "이탈 / 유지 / 상승 / 급증 / 급증"의 순서만 지킨 어림값입니다.
        /// 실제 숫자는 하루를 여러 번 돌려 보고 맞춥니다.
        /// 위험한 상황일수록 시청자와 후원이 모두 커지는 것이 설계 원칙 2번
        /// "살아남기와 재미있기는 충돌해야 한다"를 만듭니다.
        /// </remarks>
        public static BroadcastSettings Default => new BroadcastSettings(
            hidden: new BroadcastStateRate(-2f, -0.04f, 0f),
            exploring: new BroadcastStateRate(2f, 0f, 0.02f),
            mission: new BroadcastStateRate(8f, 0.04f, 0.06f),
            chased: new BroadcastStateRate(30f, 0.15f, 0.25f),
            revealing: new BroadcastStateRate(40f, 0.20f, 0.40f));
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 상황마다의 증감 규칙을 지정해 조정값을 만듭니다.
        /// </summary>
        /// <param name="hidden">안전한 곳에 숨어 있을 때의 규칙입니다.</param>
        /// <param name="exploring">탐험 중일 때의 규칙입니다.</param>
        /// <param name="mission">미션을 수행 중일 때의 규칙입니다.</param>
        /// <param name="chased">몬스터에게 쫓기고 있을 때의 규칙입니다.</param>
        /// <param name="revealing">이상물체를 공개하고 있을 때의 규칙입니다.</param>
        /// <remarks>인스펙터에서 채우는 것이 기본 경로이고, 이 생성자는 기본값과 테스트가 쓰는 자리입니다.</remarks>
        public BroadcastSettings(BroadcastStateRate hidden, BroadcastStateRate exploring,
            BroadcastStateRate mission, BroadcastStateRate chased, BroadcastStateRate revealing)
        {
            this.hidden = hidden;
            this.exploring = exploring;
            this.mission = mission;
            this.chased = chased;
            this.revealing = revealing;
        }

        /// <summary>
        /// 방송 상황에 해당하는 증감 규칙을 가져옵니다.
        /// </summary>
        /// <param name="state">규칙을 찾을 방송 상황입니다.</param>
        /// <returns>그 상황의 증감 규칙입니다.</returns>
        /// <remarks>
        /// 알 수 없는 상황은 탐험으로 봅니다. 상황을 하나 늘리고 여기를 빠뜨렸을 때
        /// 시청자가 0으로 굳는 대신 아무 일도 없는 것처럼 도는 쪽이 덜 위험합니다.
        /// </remarks>
        public BroadcastStateRate GetRate(EBroadcastState state)
        {
            switch (state)
            {
                case EBroadcastState.Hidden: return hidden;
                case EBroadcastState.Mission: return mission;
                case EBroadcastState.Chased: return chased;
                case EBroadcastState.Revealing: return revealing;
                default: return exploring;
            }
        }
        #endregion // 함수
    }
}
