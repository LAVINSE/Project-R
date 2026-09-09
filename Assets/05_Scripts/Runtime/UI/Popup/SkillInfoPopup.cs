using TMPro;

using UnityEngine;
using UnityEngine.UI;

using SW.Attributes;
using SW.Base;

using ProjectR.Data;

namespace ProjectR.UI
{
    /// <summary>
    /// 스킬트리에서 노드에 마우스를 올렸을 때 옆에 붙어 뜨는 설명입니다.
    /// </summary>
    /// <remarks>
    /// 이름은 팝업이지만 <see cref="SW.Popup.SWPopupManager"/>가 다루는 팝업이 아닙니다.
    /// 그쪽은 열고 닫을 때마다 프리팹을 만들고 없애며 팝업 스택을 쌓는데, 마우스가 노드를
    /// 지나갈 때마다 그 일이 벌어지면 ESC로 닫는 순서까지 이 설명이 끼어듭니다.
    /// 그래서 트리 화면이 직접 켜고 끄는 자리에 둡니다.
    /// <para>
    /// 마우스 입력을 받지 않습니다(<see cref="CanvasGroup"/>). 받으면 설명이 노드를 덮은 순간
    /// 노드에서 마우스가 벗어난 것으로 처리되어 설명이 닫히고, 닫히면 다시 노드에 올라간 것이 되어
    /// 깜빡입니다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(CanvasGroup))]
    public class SkillInfoPopup : SWMonoBehaviour
    {
        #region 상수
        /// <summary>이미 산 스킬에 적을 문구입니다.</summary>
        private const string OwnedText = "습득 완료";

        /// <summary>지금 살 수 있는 스킬에 적을 문구입니다.</summary>
        private const string PurchasableText = "습득 가능";

        /// <summary>후원금이 모자라 못 사는 스킬에 적을 문구입니다.</summary>
        private const string NotEnoughDonationText = "후원금 부족";
        #endregion // 상수

        #region 필드
        /// <summary>스킬 이름을 적을 글상자입니다.</summary>
        [SWGroup("내용")]
        [SerializeField, Tooltip("스킬 이름을 적을 글상자입니다.")]
        private TMP_Text skillNameText;

        /// <summary>스킬 설명을 적을 글상자입니다.</summary>
        [SerializeField, Tooltip("스킬 설명을 적을 글상자입니다.")]
        private TMP_Text skillDescriptionText;

        /// <summary>필요한 후원금을 적을 글상자입니다.</summary>
        [SerializeField, Tooltip("필요한 후원금을 적을 글상자입니다.")]
        private TMP_Text priceText;

        /// <summary>지금 살 수 있는지를 적을 글상자입니다.</summary>
        [SerializeField, Tooltip("지금 살 수 있는지를 적을 글상자입니다.")]
        private TMP_Text stateText;

        /// <summary>스킬 아이콘을 그릴 이미지입니다.</summary>
        [SerializeField, Tooltip("스킬 아이콘을 그릴 이미지입니다.")]
        private Image iconImage;

        /// <summary>노드와 설명 사이를 띄울 간격(픽셀)입니다.</summary>
        [SWGroup("위치")]
        [SerializeField, Tooltip("노드와 설명 사이를 띄울 간격(픽셀)입니다.")]
        private Vector2 nodeOffset = new(24f, 0f);

        /// <summary>화면 좌표를 옮길 때 쓰는 캔버스입니다.</summary>
        private Canvas canvas;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 마우스 입력을 받지 않도록 하고 좌표를 옮길 캔버스를 찾아 둡니다.
        /// </summary>
        /// <remarks>
        /// 여기서 <see cref="Hide"/>를 부르지 않습니다. 꺼져 있던 설명은 <see cref="Show"/>가 켜는
        /// 순간에야 Awake가 돌기 때문에, 여기서 끄면 켜자마자 다시 꺼집니다.
        /// 처음 상태는 트리 화면이 준비하면서 정합니다.
        /// </remarks>
        private void Awake()
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            canvas = GetComponentInParent<Canvas>();
        }

        /// <summary>
        /// 스킬 설명을 채우고 노드 옆에 띄웁니다.
        /// </summary>
        /// <param name="definition">보여 줄 스킬 정의입니다.</param>
        /// <param name="isOwned">이미 산 스킬인지 여부입니다.</param>
        /// <param name="donation">지금 갖고 있는 후원금입니다.</param>
        /// <param name="nodeRect">설명을 붙일 노드의 사각 영역입니다.</param>
        public void Show(UpgradeDefinition definition, bool isOwned, int donation, RectTransform nodeRect)
        {
            if (definition == null) return;

            if (skillNameText != null) skillNameText.text = definition.DisplayName;
            if (skillDescriptionText != null) skillDescriptionText.text = definition.Description;
            if (priceText != null) priceText.text = $"${definition.Cost:N0}";

            if (stateText != null)
            {
                if (isOwned) stateText.text = OwnedText;
                else if (donation < definition.Cost) stateText.text = NotEnoughDonationText;
                else stateText.text = PurchasableText;
            }

            if (iconImage != null)
            {
                iconImage.sprite = definition.Icon;
                iconImage.enabled = definition.Icon != null;
            }

            gameObject.SetActive(true);

            MoveNextTo(nodeRect);
        }

        /// <summary>
        /// 설명을 닫습니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 노드 옆으로 자리를 옮기되 화면 밖으로 나가지 않게 잡아 둡니다.
        /// </summary>
        /// <param name="nodeRect">설명을 붙일 노드의 사각 영역입니다.</param>
        /// <remarks>
        /// 트리는 밀어서 볼 수 있으므로 가장자리 노드는 화면 끝에 놓입니다. 잡아 두지 않으면
        /// 그 노드의 설명이 화면 밖에 떠서 아무것도 읽을 수 없습니다.
        /// </remarks>
        private void MoveNextTo(RectTransform nodeRect)
        {
            RectTransform rect = (RectTransform)transform;

            if (nodeRect == null || rect.parent is not RectTransform parent) return;

            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, nodeRect.position);

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, screenPoint, eventCamera, out Vector2 local) == false)
                return;

            local += new Vector2(nodeRect.rect.width * 0.5f + nodeOffset.x, nodeOffset.y);

            Rect bounds = parent.rect;
            Vector2 size = rect.rect.size;

            local.x = Mathf.Clamp(local.x,
                bounds.xMin + size.x * rect.pivot.x,
                bounds.xMax - size.x * (1f - rect.pivot.x));
            local.y = Mathf.Clamp(local.y,
                bounds.yMin + size.y * rect.pivot.y,
                bounds.yMax - size.y * (1f - rect.pivot.y));

            // 앵커가 어디에 붙어 있든 같은 자리에 놓이도록 앵커 기준점을 빼고 넣습니다.
            Vector2 anchorReference = new(
                bounds.xMin + rect.anchorMin.x * bounds.width,
                bounds.yMin + rect.anchorMin.y * bounds.height);

            rect.anchoredPosition = local - anchorReference;
        }
        #endregion // 함수
    }
}
