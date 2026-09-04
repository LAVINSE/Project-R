using System;
using System.Collections.Generic;

using UnityEngine;

namespace ProjectR.Data
{
    /// <summary>
    /// 스트리머 한 명의 진행도입니다. 스트리머마다 따로 보관합니다.
    /// </summary>
    /// <remarks>
    /// 기획서 11.2절과 12.3절이 요구하는 분리입니다. DLC로 스트리머를 추가하려면
    /// 진행도가 스트리머별로 나뉘어 있어야 합니다. 나중에 나누는 것은 저장 파일 구조를 바꾸는 일이라
    /// 배포한 뒤에는 할 수 없습니다.
    /// 어느 스트리머인지는 <see cref="StreamerId"/>로만 구분합니다.
    /// 스트리머의 정의(초기 스탯·성장 계수·아바타)는 여기가 아니라 정의 에셋이 갖습니다.
    /// 진행도는 "지금 어디까지 왔는가"만 담고, "어떤 스트리머인가"는 담지 않습니다.
    /// </remarks>
    [Serializable]
    public class StreamerProgress
    {
        #region 필드
        /// <summary>진행 상태를 소유한 스트리머의 식별자입니다.</summary>
        [SerializeField] private string streamerId;

        /// <summary>현재 시청자 수입니다.</summary>
        [SerializeField] private int viewerCount;

        /// <summary>현재 컨디션 상태입니다.</summary>
        [SerializeField] private ConditionState condition = new();

        /// <summary>보유 중인 이상물체 목록입니다. 없으면 빈 목록입니다.</summary>
        [SerializeField] private List<ItemInstance> items = new();

        /// <summary>보유 중인 업그레이드의 식별자 목록입니다. 없으면 빈 목록입니다.</summary>
        [SerializeField] private List<string> upgradeIds = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>어느 스트리머의 진행도인지를 가리키는 식별자입니다.</summary>
        public string StreamerId => streamerId;

        /// <summary>현재 시청자 수입니다.</summary>
        public int ViewerCount => viewerCount;

        /// <summary>현재 컨디션 상태입니다.</summary>
        public ConditionState Condition => condition;

        /// <summary>보유 중인 이상물체 목록입니다. 없으면 빈 목록입니다.</summary>
        public IReadOnlyList<ItemInstance> Items => items;

        /// <summary>보유 중인 업그레이드의 식별자 목록입니다. 없으면 빈 목록입니다.</summary>
        /// <remarks>
        /// 스탯에 얹힌 보너스가 아니라 <b>보유 목록</b>만 저장합니다.
        /// 보너스는 불러올 때 이 목록으로 다시 계산합니다. 값을 저장해 두면
        /// 나중에 업그레이드의 효과량을 조정했을 때 이미 산 사람에게는 옛 값이 남습니다.
        /// </remarks>
        public IReadOnlyList<string> UpgradeIds => upgradeIds;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// JSON 역직렬화가 쓰는 기본 생성자입니다.
        /// </summary>
        public StreamerProgress()
        {
        }

        /// <summary>
        /// 스트리머 식별자를 지정해 새 진행도를 만듭니다.
        /// </summary>
        /// <param name="streamerId">어느 스트리머의 진행도인지를 가리키는 식별자입니다.</param>
        public StreamerProgress(string streamerId)
        {
            this.streamerId = streamerId;
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 시청자 수를 더하거나 뺍니다. 결과는 0 아래로 내려가지 않습니다.
        /// </summary>
        /// <param name="delta">더할 시청자 수입니다. 음수면 이탈입니다.</param>
        public void AddViewers(int delta)
        {
            viewerCount = Mathf.Max(0, viewerCount + delta);
        }

        /// <summary>
        /// 이상물체를 보유 목록에 넣습니다.
        /// </summary>
        /// <param name="gained">넣을 이상물체 목록입니다. null이면 아무것도 하지 않습니다.</param>
        public void AddItems(IReadOnlyList<ItemInstance> gained)
        {
            if (gained == null) return;

            for (int index = 0; index < gained.Count; index++)
            {
                if (gained[index] == null) continue;

                items.Add(gained[index]);
            }
        }

        /// <summary>
        /// 업그레이드를 이미 갖고 있는지 확인합니다.
        /// </summary>
        /// <param name="upgradeId">확인할 업그레이드의 식별자입니다.</param>
        /// <returns>갖고 있으면 true를 반환합니다.</returns>
        public bool HasUpgrade(string upgradeId)
        {
            return string.IsNullOrEmpty(upgradeId) == false && upgradeIds.Contains(upgradeId);
        }

        /// <summary>
        /// 업그레이드를 보유 목록에 넣습니다.
        /// </summary>
        /// <param name="upgradeId">넣을 업그레이드의 식별자입니다.</param>
        /// <returns>새로 넣었으면 true를 반환합니다. 이미 갖고 있었으면 false입니다.</returns>
        public bool AddUpgrade(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId)) return false;
            if (upgradeIds.Contains(upgradeId)) return false;

            upgradeIds.Add(upgradeId);

            return true;
        }
        #endregion // 함수
    }
}
