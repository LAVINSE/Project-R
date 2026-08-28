using System.Collections.Generic;

using ProjectR.Data;

namespace ProjectR.Activity
{
    /// <summary>
    /// 활동이 실패로 끝났을 때 무엇을 잃는지 정하는 규칙입니다.
    /// </summary>
    /// <remarks>
    /// 전부 잃는 것이 너무 무겁다는 반응이 나오면 구현체를 갈아 끼워 완화합니다.
    /// 기획서 7.7절의 완화 순서(방송 사고 클립 보상 → 회수 기회 → 보험 → 일부 소실)가
    /// 전부 이 규칙 하나를 바꾸는 것으로 처리되도록 결과 전체를 넘겨 받습니다.
    /// </remarks>
    public interface ILossPolicy
    {
        #region 함수
        /// <summary>
        /// 실패로 끝난 활동의 결과에 손실을 적용합니다.
        /// </summary>
        /// <param name="result">손실을 적용할 활동 결과입니다.</param>
        /// <param name="carriedItems">활동 도중 들고 있던 물건 목록입니다.</param>
        void ApplyFailure(ActivityResult result, IReadOnlyList<ItemInstance> carriedItems);
        #endregion // 함수
    }
}
