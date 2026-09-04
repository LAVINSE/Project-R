using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

using SW.Attributes;
using SW.Base;
using SW.Popup;

using ProjectR.Activity;
using ProjectR.Data;
using ProjectR.Enum;

namespace ProjectR.UI.Settlement
{
    /// <summary>
    /// 탐험이 끝난 뒤 무엇을 얻고 무엇을 잃었는지 보여 주는 정산 화면입니다.
    /// </summary>
    /// <remarks>
    /// 결과에는 정의를 가리키는 식별자만 담겨 있으므로 이름과 색은 데이터베이스에서 찾아 옵니다.
    /// 실패했을 때도 같은 화면을 씁니다. 성공했을 때와 같은 자리에서 빈 목록을 보게 해야
    /// 무엇을 잃었는지가 성과와 나란히 놓여 전달되기 때문입니다.
    /// </remarks>
    public class SettlementPopup : SWPopupBase
    {
        #region 필드
        /// <summary>이상물체 이름을 찾아 올 데이터베이스입니다.</summary>
        [SWGroup("에셋")]
        [SerializeField, Tooltip("이상물체 이름을 찾아 올 데이터베이스입니다.")]
        private SWIODatabase anomalyDatabase;

        /// <summary>성공과 실패를 알릴 글상자입니다.</summary>
        [SWGroup("표시")]
        [SerializeField, Tooltip("성공과 실패를 알릴 글상자입니다.")]
        private Text headlineText;

        /// <summary>가져온 이상물체를 적을 글상자입니다.</summary>
        [SerializeField, Tooltip("가져온 이상물체를 적을 글상자입니다.")]
        private Text itemsText;

        /// <summary>후원금과 시청자 변동을 적을 글상자입니다.</summary>
        [SerializeField, Tooltip("후원금과 시청자 변동을 적을 글상자입니다.")]
        private Text rewardText;

        /// <summary>성공했을 때의 제목 색입니다.</summary>
        [SWGroup("색")]
        [SerializeField, Tooltip("성공했을 때의 제목 색입니다.")]
        private Color successColor = new(0.85f, 0.9f, 1f);

        /// <summary>실패했을 때의 제목 색입니다.</summary>
        [SerializeField, Tooltip("실패했을 때의 제목 색입니다.")]
        private Color failureColor = new(1f, 0.35f, 0.3f);

        /// <summary>화면을 닫는 버튼입니다.</summary>
        [SWGroup("버튼")]
        [SerializeField, Tooltip("화면을 닫는 버튼입니다.")]
        private Button confirmButton;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 확인 버튼의 콜백을 이어 붙입니다.
        /// </summary>
        private void Awake()
        {
            confirmButton?.onClick.AddListener(HandleConfirmClicked);
        }

        /// <summary>
        /// 이어 붙인 콜백을 떼어 냅니다.
        /// </summary>
        private void OnDestroy()
        {
            confirmButton?.onClick.RemoveListener(HandleConfirmClicked);
        }

        /// <summary>
        /// 보여 줄 활동 결과를 채웁니다.
        /// </summary>
        /// <param name="result">방금 끝난 활동의 결과입니다.</param>
        public void Bind(ActivityResult result)
        {
            if (result == null) return;

            FillHeadline(result);
            FillItems(result);
            FillReward(result);
        }

        /// <summary>
        /// 성공과 실패를 알리는 제목을 채웁니다.
        /// </summary>
        /// <param name="result">보여 줄 활동 결과입니다.</param>
        private void FillHeadline(ActivityResult result)
        {
            if (headlineText == null) return;

            if (result.IsFailure == false)
            {
                headlineText.text = "방송 종료";
                headlineText.color = successColor;
                return;
            }

            string reason = result.Flags.Count > 0 ? result.Flags[0] : "탐험 실패";

            headlineText.text = $"방송 사고 — {reason}";
            headlineText.color = failureColor;
        }

        /// <summary>
        /// 가져온 이상물체 목록을 채웁니다.
        /// </summary>
        /// <param name="result">보여 줄 활동 결과입니다.</param>
        private void FillItems(ActivityResult result)
        {
            if (itemsText == null) return;

            IReadOnlyList<ItemInstance> items = result.Items;

            if (items == null || items.Count == 0)
            {
                itemsText.text = result.IsFailure
                    ? "들고 있던 이상물체를 전부 잃었습니다."
                    : "가지고 나온 이상물체가 없습니다.";
                return;
            }

            StringBuilder builder = new();

            for (int index = 0; index < items.Count; index++)
            {
                AnomalyDefinition definition = FindDefinition(items[index].ItemId);

                if (definition == null)
                {
                    builder.AppendLine(items[index].ItemId);
                    continue;
                }

                builder.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(definition.DisplayColor)}>" +
                    $"{definition.DisplayName}</color>  ({GetGradeName(definition.Grade)})");
            }

            itemsText.text = builder.ToString();
        }

        /// <summary>
        /// 후원금과 시청자 변동을 채웁니다.
        /// </summary>
        /// <param name="result">보여 줄 활동 결과입니다.</param>
        private void FillReward(ActivityResult result)
        {
            if (rewardText == null) return;

            rewardText.text = $"후원금 {result.DonationDelta:+#;-#;0}\n시청자 {result.ViewerDelta:+#;-#;0}\n" +
                $"<size=20>오늘 남은 방송 시간 {GameManager.Instance.State.RemainingBroadcastMinutes}분</size>";
        }

        /// <summary>
        /// 등급을 화면에 적을 이름으로 바꿉니다.
        /// </summary>
        /// <param name="grade">바꿀 등급입니다.</param>
        /// <returns>화면에 적을 등급 이름입니다.</returns>
        private static string GetGradeName(EAnomalyGrade grade)
        {
            switch (grade)
            {
                case EAnomalyGrade.Rare: return "희귀";
                case EAnomalyGrade.Special: return "특급";
                default: return "일반";
            }
        }

        /// <summary>
        /// 식별자로 이상물체 정의를 찾습니다.
        /// </summary>
        /// <param name="definitionId">찾을 이상물체의 식별자입니다.</param>
        /// <returns>찾은 정의입니다. 없으면 null을 반환합니다.</returns>
        /// <remarks>
        /// 코드명이 비어 있으면 에셋 이름을 식별자로 쓰므로 데이터베이스의 코드명 조회로는 찾지 못합니다.
        /// 종류가 몇 개뿐이라 정의가 스스로 말하는 식별자로 직접 훑습니다.
        /// </remarks>
        private AnomalyDefinition FindDefinition(string definitionId)
        {
            if (anomalyDatabase == null) return null;

            for (int index = 0; index < anomalyDatabase.Count; index++)
            {
                if (anomalyDatabase[index] is AnomalyDefinition definition
                    && definition.DefinitionId == definitionId) return definition;
            }

            return null;
        }

        /// <summary>
        /// 확인 버튼을 눌렀을 때 화면을 닫습니다.
        /// </summary>
        private void HandleConfirmClicked()
        {
            SWPopupManager.Instance.Hide(this);
        }
        #endregion // 함수
    }
}
