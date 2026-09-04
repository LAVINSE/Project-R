using System;

using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectR.Data
{
    /// <summary>
    /// 하루를 마감할 때 쓰는 조정값 묶음입니다.
    /// </summary>
    /// <remarks>
    /// 계산은 <see cref="DayEndCalculator"/>가 하고, 이 구조체는 그 계산이 쓰는 숫자만 들고 있습니다.
    /// 나눠 둔 이유는 계산부를 유니티 없이 시험할 수 있게 하면서도 숫자는 인스펙터에서 만지기 위해서입니다.
    /// 아래 값은 전부 첫 번째 어림값입니다. 하루 수익과 맞춰 보는 것은 실제로 돌려 본 뒤에 합니다.
    /// </remarks>
    [Serializable]
    public struct DayEndSettings
    {
        #region 필드
        /// <summary>채널 규모와 무관하게 하루마다 나가는 고정 지출입니다. 월세·생활비·식비를 하루치로 나눈 값입니다.</summary>
        [SerializeField, Min(0), Tooltip("채널 규모와 무관하게 하루마다 나가는 고정 지출입니다. 월세·생활비·식비를 하루치로 나눈 값입니다.")]
        private int baseUpkeep;

        /// <summary>시청자 한 명마다 하루에 더 나가는 지출입니다. 채널이 클수록 유지가 어려워집니다.</summary>
        [SerializeField, Min(0f), Tooltip("시청자 한 명마다 하루에 더 나가는 지출입니다. 채널이 클수록 유지가 어려워집니다.")]
        private float upkeepPerViewer;

        /// <summary>방송을 한 번도 하지 않은 날에 빠져나가는 시청자 비율입니다.</summary>
        [SerializeField, Range(0f, 1f), Tooltip("방송을 한 번도 하지 않은 날에 빠져나가는 시청자 비율입니다.")]
        private float idleChurnRate;

        /// <summary>방송을 한 날에도 자연히 빠져나가는 시청자 비율입니다.</summary>
        [SerializeField, Range(0f, 1f), Tooltip("방송을 한 날에도 자연히 빠져나가는 시청자 비율입니다.")]
        private float activeChurnRate;

        /// <summary>하루에 쓸 수 있는 기본 방송 시간(분)입니다.</summary>
        [SerializeField, Min(0), Tooltip("하루에 쓸 수 있는 기본 방송 시간(분)입니다.")]
        private int baseBroadcastMinutes;

        /// <summary>방송 시간이 한 단계 늘어나는 데 필요한 시청자 수입니다. 0이면 늘어나지 않습니다.</summary>
        [SerializeField, Min(0), Tooltip("방송 시간이 한 단계 늘어나는 데 필요한 시청자 수입니다. 0이면 늘어나지 않습니다.")]
        private int viewersPerStep;

        /// <summary>한 단계마다 늘어나는 방송 시간(분)입니다.</summary>
        [SerializeField, Min(0), Tooltip("한 단계마다 늘어나는 방송 시간(분)입니다.")]
        private int minutesPerStep;

        /// <summary>탐험이 실패로 끝난 날에 다음 날 방송 시간에서 깎는 양(분)입니다.</summary>
        [SerializeField, Min(0), Tooltip("탐험이 실패로 끝난 날에 다음 날 방송 시간에서 깎는 양(분)입니다.")]
        private int failurePenaltyMinutes;

        /// <summary>어떤 이유로도 이 아래로는 내려가지 않는 방송 시간(분)입니다.</summary>
        [FormerlySerializedAs("minBroadcastMinutes")]
        [SerializeField, Min(0), Tooltip("어떤 이유로도 이 아래로는 내려가지 않는 방송 시간(분)입니다.")]
        private int minimumBroadcastMinutes;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>채널 규모와 무관하게 하루마다 나가는 고정 지출입니다.</summary>
        public int BaseUpkeep => baseUpkeep;

        /// <summary>시청자 한 명마다 하루에 더 나가는 지출입니다.</summary>
        public float UpkeepPerViewer => upkeepPerViewer;

        /// <summary>방송을 한 번도 하지 않은 날에 빠져나가는 시청자 비율입니다.</summary>
        public float IdleChurnRate => idleChurnRate;

        /// <summary>방송을 한 날에도 자연히 빠져나가는 시청자 비율입니다.</summary>
        public float ActiveChurnRate => activeChurnRate;

        /// <summary>하루에 쓸 수 있는 기본 방송 시간(분)입니다.</summary>
        public int BaseBroadcastMinutes => baseBroadcastMinutes;

        /// <summary>방송 시간이 한 단계 늘어나는 데 필요한 시청자 수입니다.</summary>
        public int ViewersPerStep => viewersPerStep;

        /// <summary>한 단계마다 늘어나는 방송 시간(분)입니다.</summary>
        public int MinutesPerStep => minutesPerStep;

        /// <summary>탐험이 실패로 끝난 날에 다음 날 방송 시간에서 깎는 양(분)입니다.</summary>
        public int FailurePenaltyMinutes => failurePenaltyMinutes;

        /// <summary>어떤 이유로도 이 아래로는 내려가지 않는 방송 시간(분)입니다.</summary>
        public int MinimumBroadcastMinutes => minimumBroadcastMinutes;

        /// <summary>인스펙터에 값을 채우기 전에 쓰는 기본 조정값입니다.</summary>
        /// <remarks>
        /// 하루 방송 240분에 백룸 1회가 60분이므로 하루 최대 네 번 들어갈 수 있고,
        /// 한 번에 이상물체를 대여섯 개 들고 나온다고 보면 하루 수익이 수천 단위가 됩니다.
        /// 고정 지출 500은 거기에 맞춘 어림값입니다.
        /// </remarks>
        public static DayEndSettings Default => new DayEndSettings(
            baseUpkeep: 500,
            upkeepPerViewer: 0.1f,
            idleChurnRate: 0.08f,
            activeChurnRate: 0.02f,
            baseBroadcastMinutes: 240,
            viewersPerStep: 2000,
            minutesPerStep: 30,
            failurePenaltyMinutes: 60,
            minimumBroadcastMinutes: 120);
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 조정값을 전부 지정해 묶음을 만듭니다.
        /// </summary>
        /// <param name="baseUpkeep">채널 규모와 무관하게 하루마다 나가는 고정 지출입니다.</param>
        /// <param name="upkeepPerViewer">시청자 한 명마다 하루에 더 나가는 지출입니다.</param>
        /// <param name="idleChurnRate">방송을 한 번도 하지 않은 날에 빠져나가는 시청자 비율입니다.</param>
        /// <param name="activeChurnRate">방송을 한 날에도 자연히 빠져나가는 시청자 비율입니다.</param>
        /// <param name="baseBroadcastMinutes">하루에 쓸 수 있는 기본 방송 시간(분)입니다.</param>
        /// <param name="viewersPerStep">방송 시간이 한 단계 늘어나는 데 필요한 시청자 수입니다.</param>
        /// <param name="minutesPerStep">한 단계마다 늘어나는 방송 시간(분)입니다.</param>
        /// <param name="failurePenaltyMinutes">탐험이 실패로 끝난 날에 다음 날 방송 시간에서 깎는 양(분)입니다.</param>
        /// <param name="minimumBroadcastMinutes">어떤 이유로도 이 아래로는 내려가지 않는 방송 시간(분)입니다.</param>
        /// <remarks>
        /// 값이 아홉 개라 생성자가 길지만, 인스펙터에서 채우는 것이 기본 경로이고
        /// 이 생성자는 <see cref="Default"/>와 테스트가 쓰는 자리입니다.
        /// 이름 있는 인자로 부르면 어느 값을 넣는지 부르는 쪽에서 읽힙니다.
        /// </remarks>
        public DayEndSettings(int baseUpkeep, float upkeepPerViewer, float idleChurnRate,
            float activeChurnRate, int baseBroadcastMinutes, int viewersPerStep, int minutesPerStep,
            int failurePenaltyMinutes, int minimumBroadcastMinutes)
        {
            this.baseUpkeep = baseUpkeep;
            this.upkeepPerViewer = upkeepPerViewer;
            this.idleChurnRate = idleChurnRate;
            this.activeChurnRate = activeChurnRate;
            this.baseBroadcastMinutes = baseBroadcastMinutes;
            this.viewersPerStep = viewersPerStep;
            this.minutesPerStep = minutesPerStep;
            this.failurePenaltyMinutes = failurePenaltyMinutes;
            this.minimumBroadcastMinutes = minimumBroadcastMinutes;
        }
        #endregion // 생성자
    }
}
