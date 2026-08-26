using System;
using System.Collections.Generic;

namespace ProjectR.Data
{
    /// <summary>
    /// 활동 하나가 끝났을 때 게임 상태에 반영할 결과를 담는 데이터입니다.
    /// </summary>
    /// <remarks>
    /// 백룸, 미니게임, 알바 등 모든 활동이 같은 결과 규격을 사용합니다.
    /// </remarks>
    [Serializable]
    public class ActivityResult
    {
        #region 필드
        /// <summary>후원금 변동량입니다.</summary>
        public int DonationDelta;

        /// <summary>시청자 수 변동량입니다.</summary>
        public int ViewerDelta;

        /// <summary>컨디션 네 수치의 변동량입니다.</summary>
        public ConditionDelta Condition;

        /// <summary>활동으로 얻은 이상물체 목록입니다. 없으면 빈 목록입니다.</summary>
        public List<ItemInstance> Items = new List<ItemInstance>();

        /// <summary>활동이 실패로 끝났는지 여부입니다.</summary>
        public bool IsFailure;

        /// <summary>후속 처리에 쓰이는 자유 형식 표식 목록입니다. 없으면 빈 목록입니다.</summary>
        public List<string> Flags = new List<string>();
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 아무 변동도 없는 결과를 만듭니다.
        /// </summary>
        /// <returns>변동량이 전부 0인 결과입니다.</returns>
        public static ActivityResult Empty()
        {
            return new ActivityResult();
        }
        #endregion // 함수
    }
}
