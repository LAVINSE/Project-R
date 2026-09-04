using UnityEngine;
using UnityEngine.UI;

using SW.Attributes;
using SW.Base;

using ProjectR.Backrooms.Collect;

namespace ProjectR.UI.Backrooms
{
    /// <summary>
    /// 탐험 중에 화면에 계속 떠 있는 표시입니다.
    /// </summary>
    /// <remarks>
    /// 조준선이 있어야 무엇을 겨누고 있는지 알 수 있습니다.
    /// 바닥에 놓인 작은 물건은 조준선 없이는 안내 문구가 왜 안 뜨는지조차 알 수 없습니다.
    /// 가방에 무엇이 들었는지는 <see cref="ProjectR.UI.Inventory.InventoryPanel"/>이 따로 보여 줍니다.
    /// </remarks>
    public class BackroomsHud : SWMonoBehaviour
    {
        #region 필드
        /// <summary>바라보는 대상을 읽어 올 줍기 컴포넌트입니다.</summary>
        [SWGroup("대상")]
        [SerializeField, Tooltip("바라보는 대상을 읽어 올 줍기 컴포넌트입니다.")]
        private PlayerPickupInteractor pickupInteractor;

        /// <summary>화면 가운데에 둘 조준선입니다.</summary>
        [SWGroup("표시")]
        [SerializeField, Tooltip("화면 가운데에 둘 조준선입니다.")]
        private Graphic crosshair;

        /// <summary>주울 수 있는 물건을 알릴 글상자입니다.</summary>
        [SerializeField, Tooltip("주울 수 있는 물건을 알릴 글상자입니다.")]
        private Text pickupPromptText;

        /// <summary>평소 조준선 색입니다.</summary>
        [SWGroup("색")]
        [SerializeField, Tooltip("평소 조준선 색입니다.")]
        private Color idleColor = new(1f, 1f, 1f, 0.35f);

        /// <summary>주울 수 있는 것을 겨눴을 때의 조준선 색입니다.</summary>
        [SerializeField, Tooltip("주울 수 있는 것을 겨눴을 때의 조준선 색입니다.")]
        private Color focusedColor = new(1f, 1f, 1f, 0.95f);

        /// <summary>가방이 꽉 찼을 때의 색입니다.</summary>
        [SerializeField, Tooltip("가방이 꽉 찼을 때의 색입니다.")]
        private Color fullColor = new(1f, 0.35f, 0.3f);
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 조준선과 안내 문구를 매 프레임 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (pickupInteractor == null) return;

            AnomalyPickup focused = pickupInteractor.FocusedPickup;

            UpdateCrosshair(focused);
            UpdatePrompt(focused);
        }

        /// <summary>
        /// 겨누고 있는 것에 따라 조준선 색을 바꿉니다.
        /// </summary>
        /// <param name="focused">지금 겨누고 있는 상자입니다. 없으면 null입니다.</param>
        private void UpdateCrosshair(AnomalyPickup focused)
        {
            if (crosshair == null) return;

            if (focused == null)
            {
                crosshair.color = idleColor;
                return;
            }

            crosshair.color = pickupInteractor.IsBackpackFull ? fullColor : focusedColor;
        }

        /// <summary>
        /// 주울 수 있는 물건이 있는지 알립니다.
        /// </summary>
        /// <param name="focused">지금 겨누고 있는 상자입니다. 없으면 null입니다.</param>
        private void UpdatePrompt(AnomalyPickup focused)
        {
            if (pickupPromptText == null) return;

            if (focused == null)
            {
                pickupPromptText.text = string.Empty;
                return;
            }

            pickupPromptText.text = pickupInteractor.IsBackpackFull
                ? "가방에 자리가 없습니다"
                : $"[E] {focused.Definition.DisplayName} 줍기";

            pickupPromptText.color = pickupInteractor.IsBackpackFull ? fullColor : focusedColor;
        }
        #endregion // 함수
    }
}
