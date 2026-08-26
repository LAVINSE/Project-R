using System;

using UnityEngine;

namespace ProjectR.Data
{
    /// <summary>
    /// 백룸에서 습득한 이상물체 한 개를 나타내는 실체 데이터입니다.
    /// </summary>
    /// <remarks>
    /// 격자 크기, 등급, 판매가 같은 정의 값은 아이템 정의 에셋이 담당하며
    /// 이 클래스는 정의를 가리키는 식별자만 보관합니다.
    /// </remarks>
    [Serializable]
    public class ItemInstance
    {
        #region 필드
        [SerializeField] private string itemId;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>아이템 정의를 가리키는 식별자입니다.</summary>
        public string ItemId => itemId;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 아이템 식별자를 지정해 실체를 만듭니다.
        /// </summary>
        /// <param name="itemId">아이템 정의를 가리키는 식별자입니다.</param>
        public ItemInstance(string itemId)
        {
            this.itemId = itemId;
        }
        #endregion // 함수
    }
}
