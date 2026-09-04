using System;

namespace ProjectR.Data
{
    /// <summary>
    /// 하루를 마감할 때의 유지비, 구독자 이탈, 다음 날 방송 시간을 계산합니다.
    /// </summary>
    /// <remarks>
    /// 기획서 8.3절이 요구하는 것은 하나입니다. <b>성장을 멈추면 후퇴한다.</b>
    /// 엔드리스에는 마감 기한이 없으므로 유지비가 그 압박을 대신합니다.
    /// 그래서 이 계산은 두 방향으로 동시에 조입니다.
    /// 쉬면 시청자가 빠지고, 쉬지 않아도 채널이 클수록 유지비가 오릅니다.
    /// <para>
    /// 유니티 타입을 쓰지 않습니다. 상태를 받지 않고 숫자만 받아 숫자만 돌려주므로
    /// 씬도 플레이 모드도 없이 시험할 수 있습니다. 조정값은 <see cref="DayEndSettings"/>가 들고 있어
    /// 이 클래스에는 조정할 숫자가 하나도 없습니다.
    /// </para>
    /// <para>
    /// 데이터 계층에 둔 이유는 계산이 다루는 대상이 전부 이 계층의 값이기 때문입니다.
    /// 유니티와의 분리를 컴파일로 강제하는 <c>ProjectR.Inventory</c>에 넣으면
    /// 그 어셈블리가 아무것도 참조하지 않는다는 규칙(체크리스트 1.7절)을 어기게 됩니다.
    /// </para>
    /// </remarks>
    public static class DayEndCalculator
    {
        #region 함수
        /// <summary>
        /// 하루 마감 결과를 계산합니다.
        /// </summary>
        /// <param name="settings">계산에 쓸 조정값입니다.</param>
        /// <param name="viewerCount">마감 시점의 시청자 수입니다.</param>
        /// <param name="activityCount">오늘 진행한 활동 횟수입니다. 0이면 쉰 날로 봅니다.</param>
        /// <param name="hasFailure">오늘 실패로 끝난 활동이 있었는지 여부입니다.</param>
        /// <returns>오늘 나갈 유지비와 이탈, 다음 날 방송 시간입니다.</returns>
        public static DayEndResult Calculate(DayEndSettings settings, int viewerCount,
            int activityCount, bool hasFailure)
        {
            int safeViewerCount = Math.Max(0, viewerCount);

            return new DayEndResult
            {
                UpkeepCost = CalculateUpkeep(settings, safeViewerCount),
                ViewerLoss = CalculateViewerLoss(settings, safeViewerCount, activityCount),
                NextBroadcastMinutes = CalculateNextBroadcastMinutes(settings, safeViewerCount, hasFailure),
                IsPenalized = hasFailure && settings.FailurePenaltyMinutes > 0,
            };
        }

        /// <summary>
        /// 오늘 나갈 유지비를 계산합니다.
        /// </summary>
        /// <param name="settings">계산에 쓸 조정값입니다.</param>
        /// <param name="viewerCount">마감 시점의 시청자 수입니다.</param>
        /// <returns>오늘 나갈 유지비입니다.</returns>
        /// <remarks>
        /// 고정 지출에 채널 규모에 비례하는 몫을 더합니다(기획서 8.3절).
        /// 규모에 비례하는 몫이 없으면 시청자가 늘수록 게임이 쉬워지기만 해서 압박이 사라집니다.
        /// </remarks>
        private static int CalculateUpkeep(DayEndSettings settings, int viewerCount)
        {
            decimal scaled = settings.BaseUpkeep + viewerCount * (decimal)settings.UpkeepPerViewer;

            return (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 오늘 빠져나갈 시청자 수를 계산합니다.
        /// </summary>
        /// <param name="settings">계산에 쓸 조정값입니다.</param>
        /// <param name="viewerCount">마감 시점의 시청자 수입니다.</param>
        /// <param name="activityCount">오늘 진행한 활동 횟수입니다.</param>
        /// <returns>오늘 빠져나갈 시청자 수입니다.</returns>
        /// <remarks>
        /// 인원이 아니라 비율로 빼는 이유는 채널이 클수록 유지가 어려워야 하기 때문입니다.
        /// 고정 인원으로 빼면 대형 채널에서는 이탈이 없는 것과 같아집니다.
        /// 쉰 날의 이탈률이 방송한 날보다 높은 것이 "쉬면 채널이 죽는다"(설계 원칙 3번)입니다.
        /// 이탈은 올림합니다. 내림하면 시청자가 적을 때 이탈이 영영 0이 되어 압박이 사라집니다.
        /// <para>
        /// <c>decimal</c>로 옮겨 곱하는 이유는 올림 때문입니다. <c>float</c> 0.1은 실제로는
        /// 0.100000001490116이라 <c>double</c>로 넓히면 시청자 1000명에 100.0000015가 나오고,
        /// 올림하면 101이 됩니다. <c>decimal</c> 변환은 <c>float</c>의 유효자릿수만 취하므로 0.1 그대로 옮겨집니다.
        /// </para>
        /// </remarks>
        private static int CalculateViewerLoss(DayEndSettings settings, int viewerCount, int activityCount)
        {
            if (viewerCount <= 0) return 0;

            float rate = activityCount > 0 ? settings.ActiveChurnRate : settings.IdleChurnRate;

            if (rate <= 0f) return 0;

            int loss = (int)Math.Ceiling(viewerCount * (decimal)rate);

            return Math.Min(loss, viewerCount);
        }

        /// <summary>
        /// 다음 날 쓸 수 있는 방송 시간을 계산합니다.
        /// </summary>
        /// <param name="settings">계산에 쓸 조정값입니다.</param>
        /// <param name="viewerCount">마감 시점의 시청자 수입니다.</param>
        /// <param name="hasFailure">오늘 실패로 끝난 활동이 있었는지 여부입니다.</param>
        /// <returns>다음 날 쓸 수 있는 방송 시간(분)입니다.</returns>
        /// <remarks>
        /// 채널이 커지면 늘고 실패하면 줄지만, 하한선 아래로는 내려가지 않습니다.
        /// 하한선이 없으면 한 번 실패한 사람이 방송 시간이 없어 회복도 못 하는
        /// 실패가 실패를 부르는 구조가 됩니다(기획서 설계 원칙 4번).
        /// </remarks>
        private static int CalculateNextBroadcastMinutes(DayEndSettings settings, int viewerCount, bool hasFailure)
        {
            int minutes = settings.BaseBroadcastMinutes;

            if (settings.ViewersPerStep > 0)
            {
                int step = viewerCount / settings.ViewersPerStep;

                minutes += step * settings.MinutesPerStep;
            }

            if (hasFailure) minutes -= settings.FailurePenaltyMinutes;

            return Math.Max(settings.MinimumBroadcastMinutes, minutes);
        }
        #endregion // 함수
    }
}
