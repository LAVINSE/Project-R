using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SW.Attributes;
using SW.Base;

using ProjectR.Data;
using ProjectR.Inventory;

namespace ProjectR.UI.Inventory
{
    /// <summary>
    /// 격자 가방에 놓인 물건 한 개의 표시입니다.
    /// </summary>
    /// <remarks>
    /// 어디에 놓을 수 있는지는 계산부가 판정하므로, 이 표시는 끌고 다니는 동안의 겉모습만 책임집니다.
    /// 실제로 자리를 옮기는 것은 손을 뗀 순간 <see cref="InventoryPanel"/>이 한 번 처리합니다.
    /// </remarks>
    public class InventoryItemView : SWMonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler
    {
        #region 필드
        /// <summary>물건의 색을 칠할 배경 이미지입니다.</summary>
        [SWGroup("표시")]
        [SerializeField, Tooltip("물건의 색을 칠할 배경 이미지입니다.")]
        private Image background;

        /// <summary>아이콘을 그릴 이미지입니다.</summary>
        [SerializeField, Tooltip("아이콘을 그릴 이미지입니다.")]
        private Image icon;

        /// <summary>아이콘이 없을 때 이름을 적을 글상자입니다.</summary>
        [SerializeField, Tooltip("아이콘이 없을 때 이름을 적을 글상자입니다.")]
        private Text label;

        /// <summary>이 표시를 만든 가방 화면입니다.</summary>
        private InventoryPanel owner;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>표시하고 있는 물건의 실체 번호입니다.</summary>
        public int InstanceId { get; private set; }

        /// <summary>표시하고 있는 물건의 형태입니다.</summary>
        public InventoryShape Shape { get; private set; }

        /// <summary>지금 화면에서 돌려 놓은 상태인지 여부입니다.</summary>
        public bool IsRotated { get; private set; }

        /// <summary>이 표시의 사각 영역입니다.</summary>
        public RectTransform Rect { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 사각 영역을 캐싱합니다.
        /// </summary>
        private void Awake()
        {
            Rect = (RectTransform)transform;
        }

        /// <summary>
        /// 표시할 물건을 지정합니다.
        /// </summary>
        /// <param name="owner">이 표시를 만든 가방 화면입니다.</param>
        /// <param name="item">표시할 물건입니다.</param>
        /// <param name="definition">물건의 정의입니다. 없으면 null을 넘겨도 됩니다.</param>
        public void Bind(InventoryPanel owner, PlacedItem item, AnomalyDefinition definition)
        {
            this.owner = owner;

            InstanceId = item.InstanceId;
            Shape = item.Shape;
            IsRotated = item.IsRotated;

            ApplyLook(item, definition);
        }

        /// <summary>
        /// 아이콘이 있으면 아이콘을, 없으면 색과 이름을 보여 줍니다.
        /// </summary>
        /// <param name="item">표시할 물건입니다.</param>
        /// <param name="definition">물건의 정의입니다. 없으면 null입니다.</param>
        /// <remarks>
        /// 아이콘과 이름을 함께 두면 칸 안에서 서로를 가립니다.
        /// 아이콘이 있으면 그것만으로 무엇인지 알 수 있으므로 이름은 뺍니다.
        /// </remarks>
        private void ApplyLook(PlacedItem item, AnomalyDefinition definition)
        {
            // 아이콘을 그릴 자리가 없으면 아이콘이 있어도 이름으로 보여 줘야 합니다.
            Sprite sprite = icon != null && definition != null ? definition.Icon : null;

            if (icon != null)
            {
                icon.gameObject.SetActive(sprite != null);
                icon.sprite = sprite;
                icon.preserveAspect = true;
            }

            if (label != null)
            {
                label.gameObject.SetActive(sprite == null);
                label.text = definition != null ? definition.DisplayName : item.DefinitionId;
            }

            if (background == null) return;

            // 아이콘이 있으면 배경은 아이콘을 받쳐 주기만 하면 되므로 색을 죽입니다.
            background.color = sprite != null
                ? new Color(1f, 1f, 1f, 0.16f)
                : definition != null ? definition.DisplayColor : Color.gray;
        }

        /// <summary>
        /// 끌고 다니는 동안의 회전 상태를 뒤집습니다.
        /// </summary>
        public void ToggleRotation()
        {
            IsRotated = !IsRotated;
        }

        /// <summary>
        /// 끌기를 시작합니다.
        /// </summary>
        /// <param name="eventData">끌기 입력 정보입니다.</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.BeginDrag(this, eventData);
        }

        /// <summary>
        /// 끌고 있는 동안 위치를 따라 옮깁니다.
        /// </summary>
        /// <param name="eventData">끌기 입력 정보입니다.</param>
        public void OnDrag(PointerEventData eventData)
        {
            owner?.UpdateDrag(eventData);
        }

        /// <summary>
        /// 끌기를 마칩니다.
        /// </summary>
        /// <param name="eventData">끌기 입력 정보입니다.</param>
        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.EndDrag(eventData);
        }

        /// <summary>
        /// 오른쪽 버튼으로 누르면 물건을 버립니다.
        /// </summary>
        /// <param name="eventData">누름 입력 정보입니다.</param>
        /// <remarks>
        /// 격자 밖으로 끌어내는 방식은 옮기려다 손이 미끄러지면 그대로 버려집니다.
        /// 버리는 것은 되돌릴 수 없으므로 옮기기와 다른 버튼으로 갈라 둡니다.
        /// </remarks>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right) return;

            owner?.DropItem(InstanceId);
        }
        #endregion // 함수
    }
}
