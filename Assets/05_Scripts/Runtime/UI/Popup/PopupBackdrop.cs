using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

using SW.Attributes;
using SW.Base;
using SW.Popup;
using SW.Util;

namespace ProjectR.UI.Popup
{
    /// <summary>
    /// 팝업이 떠 있는 동안 화면 전체를 덮어 뒤쪽 클릭을 막는 판입니다.
    /// </summary>
    /// <remarks>
    /// 체크리스트 2.3절이 짚어 둔 자리입니다. <c>EventSystem</c>은 위에 덮인 것이
    /// <b>레이캐스트 대상일 때만</b> 아래를 막습니다. 옵션 팝업은 화면 일부만 덮는 패널이라
    /// 그대로 두면 팝업 뒤의 마을 건물이 눌립니다.
    /// <para>
    /// 마을 건물이 <see cref="SpriteRenderer"/>와 <c>Physics2DRaycaster</c>로 입력을 받게 되면서
    /// 실제로 새는 자리가 되었습니다. 그전에는 관리 화면이 Canvas 버튼뿐이라 드러나지 않았습니다.
    /// </para>
    /// <para>
    /// SWUtils 원본을 고치지 않고 알림만 듣습니다(체크리스트 1.2절).
    /// 팝업 하나를 만들 때마다 챙기지 않아도 되도록, 팝업 캔버스에 이 판 하나만 두면 끝입니다.
    /// 색은 완전히 투명해도 됩니다. <c>Image</c>는 알파가 0이어도 레이캐스트를 막습니다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Image))]
    [MovedFrom(true, sourceNamespace: "ProjectR.UI", sourceAssembly: "ProjectR.UI", sourceClassName: "PopupBackdrop")]
    public class PopupBackdrop : SWMonoBehaviour
    {
        #region 필드
        /// <summary>팝업이 떠 있을 때 화면을 덮을 색입니다. 알파가 0이어도 클릭은 막힙니다.</summary>
        [SWGroup("표시")]
        [SerializeField, Tooltip("팝업이 떠 있을 때 화면을 덮을 색입니다. 알파가 0이어도 클릭은 막힙니다.")]
        private Color coverColor = new(0f, 0f, 0f, 0.5f);

        /// <summary>화면을 덮는 이미지입니다.</summary>
        private Image cover;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 이미지를 캐싱하고 처음에는 꺼 둡니다.
        /// </summary>
        private void Awake()
        {
            cover = GetComponent<Image>();
            cover.color = coverColor;
            cover.raycastTarget = true;

            cover.enabled = false;
        }

        /// <summary>
        /// 팝업이 열리고 닫히는 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            SWPopupManager.Instance.PopupShown += HandlePopupChanged;
            SWPopupManager.Instance.PopupHidden += HandlePopupChanged;

            ApplyState();
        }

        /// <summary>
        /// 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (SWPopupManager.HasInstance == false) return;

            SWPopupManager.Instance.PopupShown -= HandlePopupChanged;
            SWPopupManager.Instance.PopupHidden -= HandlePopupChanged;
        }

        /// <summary>
        /// 팝업이 열리거나 닫히면 덮개를 다시 맞춥니다.
        /// </summary>
        /// <param name="popup">열리거나 닫힌 팝업입니다.</param>
        private void HandlePopupChanged(SWPopupBase popup)
        {
            ApplyState();
        }

        /// <summary>
        /// 떠 있는 팝업이 있으면 덮개를 켜고, 없으면 끕니다.
        /// </summary>
        /// <remarks>
        /// 열린 개수를 세지 않고 하나라도 있는지만 봅니다.
        /// 팝업이 겹쳐 떠도 덮개는 하나면 되고, 맨 아래 한 겹만 막으면 됩니다.
        /// </remarks>
        private void ApplyState()
        {
            bool hasPopup = SWPopupManager.Instance.ActivePopupCount > 0;

            if (cover.enabled == hasPopup) return;

            cover.enabled = hasPopup;

            SWLog.Log($"[{nameof(PopupBackdrop)}] 팝업 뒤 덮개를 {(hasPopup ? "켰습니다" : "껐습니다")}.");
        }
        #endregion // 함수
    }
}
