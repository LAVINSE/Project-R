using System;

using UnityEngine;

namespace ProjectR.Data
{
    /// <summary>
    /// 스트리머가 바뀌어도 이어지는 채널 단위 진행도입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 11.2절이 저장 데이터를 채널 진행도와 스트리머별 진행도로 나누라고 정한 것을 옮긴 것입니다.
    /// 나누는 이유는 DLC로 스트리머를 추가하기 위해서입니다. 채널은 하나뿐이고 스트리머는 늘어납니다.
    /// 날짜와 자금이 여기에 있는 것은 기획서 11.2절이 그렇게 정했기 때문입니다.
    /// 하루에 방송하는 스트리머는 한 명이므로 오늘 남은 방송 시간도 채널 쪽에 둡니다.
    /// 스트리머마다 날짜가 따로 흐르면 유지비를 어느 날짜로 물릴지 정할 수 없습니다.
    /// </remarks>
    [Serializable]
    public class ChannelProgress
    {
        #region 필드
        [SerializeField] private int day = 1;
        [SerializeField] private int dailyBroadcastMinutes;
        [SerializeField] private int remainingBroadcastMinutes;
        [SerializeField] private int donation;
        [SerializeField] private int todayActivityCount;
        [SerializeField] private bool todayHasFailure;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 며칠째인지를 나타냅니다.</summary>
        public int Day => day;

        /// <summary>오늘 배정받은 방송 시간(분)입니다.</summary>
        /// <remarks>
        /// 채널이 커지면 늘고 탐험에 실패하면 줄어드는 값이라 날마다 다릅니다.
        /// 화면에 "남은 시간 / 오늘 배정" 형태로 보여 주려면 오늘 배정량이 남아 있어야 합니다.
        /// </remarks>
        public int DailyBroadcastMinutes => dailyBroadcastMinutes;

        /// <summary>오늘 남아 있는 방송 시간(분)입니다.</summary>
        public int RemainingBroadcastMinutes => remainingBroadcastMinutes;

        /// <summary>보유 후원금입니다.</summary>
        public int Donation => donation;

        /// <summary>오늘 진행한 활동 횟수입니다.</summary>
        /// <remarks>
        /// 하루 마감의 구독자 이탈 계산이 "오늘 방송을 했는가"를 이 값으로 판단합니다.
        /// 매니저가 들고 있으면 활동 도중 강제 종료했을 때 사라지므로 저장되는 쪽에 둡니다.
        /// </remarks>
        public int TodayActivityCount => todayActivityCount;

        /// <summary>오늘 실패로 끝난 활동이 있었는지 여부입니다.</summary>
        public bool TodayHasFailure => todayHasFailure;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 하루의 방송 시간을 설정합니다.
        /// </summary>
        /// <param name="minutes">오늘 사용할 수 있는 방송 시간(분)입니다.</param>
        public void ResetBroadcastTime(int minutes)
        {
            dailyBroadcastMinutes = Mathf.Max(0, minutes);
            remainingBroadcastMinutes = dailyBroadcastMinutes;
        }

        /// <summary>
        /// 오늘 활동을 하나 마쳤음을 기록합니다.
        /// </summary>
        /// <param name="isFailure">그 활동이 실패로 끝났으면 true입니다.</param>
        public void RecordActivity(bool isFailure)
        {
            todayActivityCount += 1;

            if (isFailure) todayHasFailure = true;
        }

        /// <summary>
        /// 방송 시간을 소비합니다.
        /// </summary>
        /// <param name="minutes">소비할 방송 시간(분)입니다.</param>
        public void ConsumeBroadcastTime(int minutes)
        {
            if (minutes <= 0) return;

            remainingBroadcastMinutes = Mathf.Max(0, remainingBroadcastMinutes - minutes);
        }

        /// <summary>
        /// 후원금을 더하거나 뺍니다. 결과는 0 아래로 내려가지 않습니다.
        /// </summary>
        /// <param name="delta">더할 후원금입니다. 음수면 차감입니다.</param>
        public void AddDonation(int delta)
        {
            donation = Mathf.Max(0, donation + delta);
        }

        /// <summary>
        /// 다음 날로 넘어갑니다.
        /// </summary>
        /// <param name="minutes">다음 날 사용할 수 있는 방송 시간(분)입니다.</param>
        public void AdvanceDay(int minutes)
        {
            day += 1;
            todayActivityCount = 0;
            todayHasFailure = false;

            ResetBroadcastTime(minutes);
        }
        #endregion // 함수
    }
}
