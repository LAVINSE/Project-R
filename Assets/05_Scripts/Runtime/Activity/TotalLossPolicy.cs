using System.Collections.Generic;

using SW.Util;

using ProjectR.Data;

namespace ProjectR.Activity
{
    /// <summary>
    /// 실패하면 들고 있던 물건을 전부 잃는 규칙입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 7.7절의 기준값입니다. 이 규칙으로 먼저 테스트하고,
    /// 다시 하겠다가 아니라 그만두겠다는 반응이 나오면 그때 완화 구현체로 바꿉니다.
    /// </remarks>
    public class TotalLossPolicy : ILossPolicy
    {
        #region 함수
        /// <summary>
        /// 들고 있던 물건과 그 물건으로 벌어들일 수치를 결과에서 모두 지웁니다.
        /// </summary>
        /// <param name="result">손실을 적용할 활동 결과입니다.</param>
        /// <param name="carriedItems">활동 도중 들고 있던 물건 목록입니다.</param>
        public void ApplyFailure(ActivityResult result, IReadOnlyList<ItemInstance> carriedItems)
        {
            if (result == null) return;

            result.Items.Clear();
            result.DonationDelta = 0;
            result.ViewerDelta = 0;

            SWLog.Log($"[{nameof(TotalLossPolicy)}] 실패로 이상물체 {carriedItems?.Count ?? 0}개를 모두 잃었습니다.");
        }
        #endregion // 함수
    }
}
