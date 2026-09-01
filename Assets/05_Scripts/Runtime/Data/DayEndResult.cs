using System;

namespace ProjectR.Data
{
    /// <summary>
    /// 하루 마감 계산의 결과입니다.
    /// </summary>
    /// <remarks>
    /// 계산과 반영을 갈라 놓기 위한 중간 결과입니다.
    /// 계산부가 상태를 직접 고치면 하루 마감 화면에서 "얼마가 나갔는지"를 보여 줄 수 없습니다.
    /// 결과를 먼저 만들고 화면에 보여 준 뒤 반영하는 순서를 쓸 수 있게 나눠 두었습니다.
    /// </remarks>
    [Serializable]
    public struct DayEndResult
    {
        #region 필드
        /// <summary>오늘 나간 유지비입니다.</summary>
        public int UpkeepCost;

        /// <summary>오늘 빠져나간 시청자 수입니다.</summary>
        public int ViewerLoss;

        /// <summary>다음 날 쓸 수 있는 방송 시간(분)입니다.</summary>
        public int NextBroadcastMinutes;

        /// <summary>탐험 실패 때문에 다음 날 방송 시간이 깎였는지 여부입니다.</summary>
        public bool IsPenalized;
        #endregion // 필드
    }
}
