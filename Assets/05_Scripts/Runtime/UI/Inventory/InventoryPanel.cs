using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using SW.Attributes;
using SW.Base;
using SW.Debugging;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Backrooms;
using ProjectR.Backrooms.Player;
using ProjectR.Data;
using ProjectR.Inventory;

namespace ProjectR.UI.Inventory
{
    /// <summary>
    /// 탐험 내내 화면에 붙어 있는 격자 가방입니다.
    /// </summary>
    /// <remarks>
    /// 팝업이 아니라 화면에 계속 떠 있습니다. 가방을 열어야만 남은 칸을 알 수 있으면
    /// 한 개만 더와 지금 나가자를 저울질하는 순간이 화면 밖으로 밀려나기 때문입니다.
    /// 평소에는 보기만 하고 이동을 막지 않습니다. 정리 키를 누르면 커서가 풀리고
    /// 그때만 끌어서 옮기거나 버릴 수 있습니다.
    /// 어디에 놓을 수 있는지는 <see cref="GridInventory"/>가 판정합니다.
    /// 이 화면은 칸을 그리고, 끌어 놓은 자리를 칸 좌표로 바꿔 넘기는 일만 합니다.
    /// </remarks>
    public class InventoryPanel : SWMonoBehaviour
    {
        #region 필드
        /// <summary>칸과 물건을 놓을 격자 영역입니다.</summary>
        [SWGroup("격자")]
        [SerializeField, Tooltip("칸과 물건을 놓을 격자 영역입니다.")]
        private RectTransform gridRoot;

        /// <summary>칸 한 개의 한 변 길이(픽셀)입니다.</summary>
        [SerializeField, Min(16f), Tooltip("칸 한 개의 한 변 길이(픽셀)입니다.")]
        private float cellSize = 56f;

        /// <summary>칸 사이를 띄워 격자선처럼 보이게 할 간격(픽셀)입니다.</summary>
        [SerializeField, Min(0f), Tooltip("칸 사이를 띄워 격자선처럼 보이게 할 간격(픽셀)입니다.")]
        private float cellGap = 2f;

        /// <summary>빈 칸을 그릴 이미지 프리팹입니다.</summary>
        [SWGroup("프리팹")]
        [SerializeField, Tooltip("빈 칸을 그릴 이미지 프리팹입니다.")]
        private Image cellPrefab;

        /// <summary>물건 하나를 그릴 표시 프리팹입니다.</summary>
        [SerializeField, Tooltip("물건 하나를 그릴 표시 프리팹입니다.")]
        private InventoryItemView itemViewPrefab;

        /// <summary>화면 전체의 진하기를 다룰 그룹입니다.</summary>
        [SWGroup("표시")]
        [SerializeField, Tooltip("화면 전체의 진하기를 다룰 그룹입니다.")]
        private CanvasGroup canvasGroup;

        /// <summary>사용 중인 칸 수를 적을 글상자입니다.</summary>
        [SerializeField, Tooltip("사용 중인 칸 수를 적을 글상자입니다.")]
        private Text usageText;

        /// <summary>정리 중에만 보여 줄 조작 안내 글상자입니다.</summary>
        [SerializeField, Tooltip("정리 중에만 보여 줄 조작 안내 글상자입니다.")]
        private Text hintText;

        /// <summary>보기만 할 때의 진하기입니다.</summary>
        [SerializeField, Range(0.1f, 1f), Tooltip("보기만 할 때의 진하기입니다.")]
        private float idleAlpha = 0.55f;

        /// <summary>정리 모드를 켜고 끄는 키입니다.</summary>
        [SWGroup("조작")]
        [SerializeField, Tooltip("정리 모드를 켜고 끄는 키입니다.")]
        private Key toggleKey = Key.I;

        /// <summary>정리 중에 이동을 막을 입력 컴포넌트입니다.</summary>
        [SerializeField, Tooltip("정리 중에 이동을 막을 입력 컴포넌트입니다.")]
        private PlayerInputReader inputReader;

        /// <summary>지금 그려 둔 물건 표시 목록입니다.</summary>
        private readonly List<InventoryItemView> itemViews = new();

        /// <summary>지금 그려 둔 빈 칸 목록입니다.</summary>
        private readonly List<Image> cellViews = new();

        /// <summary>화면이 보고 있는 탐험입니다. 없으면 null입니다.</summary>
        private BackroomsActivity activity;

        /// <summary>지금 끌고 있는 물건 표시입니다. 없으면 null입니다.</summary>
        private InventoryItemView draggedView;

        /// <summary>끌기 시작할 때 잡은 지점과 표시 왼쪽 위의 차이입니다.</summary>
        private Vector2 dragOffset;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>지금 정리 모드인지 여부입니다.</summary>
        public bool IsEditing { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 진행 중인 탐험의 가방을 그리고 알림을 구독합니다.
        /// </summary>
        private void Start()
        {
            activity = GameManager.Instance.CurrentActivity as BackroomsActivity;

            if (activity == null)
            {
                SWLog.Log($"[{nameof(InventoryPanel)}] 진행 중인 백룸 탐험이 없어 가방을 숨깁니다.");
                gameObject.SetActive(false);
                return;
            }

            activity.BackpackChanged += Refresh;

            BuildCells();
            Refresh();
            ApplyEditingState();
        }

        /// <summary>
        /// 알림 구독을 해제하고 잡고 있던 조작을 놓아 줍니다.
        /// </summary>
        private void OnDestroy()
        {
            if (IsEditing) inputReader?.SetUiFocus(false);

            if (activity == null) return;

            activity.BackpackChanged -= Refresh;
            activity = null;
        }

        /// <summary>
        /// 정리 모드 전환과 회전 입력을 확인합니다.
        /// </summary>
        private void Update()
        {
            if (Keyboard.current == null) return;

            // 디버그 콘솔이 열려 있으면 키는 콘솔 쪽 몫입니다.
            if (SWDebugConsole.IsOpen == false && Keyboard.current[toggleKey].wasPressedThisFrame)
                SetEditing(IsEditing == false);

            if (draggedView == null) return;
            if (Keyboard.current.rKey.wasPressedThisFrame == false) return;

            RotateDragged();
        }

        /// <summary>
        /// 정리 모드를 켜거나 끕니다.
        /// </summary>
        /// <param name="isEditing">정리 모드로 만들려면 true입니다.</param>
        public void SetEditing(bool isEditing)
        {
            if (IsEditing == isEditing) return;

            IsEditing = isEditing;

            if (isEditing == false) draggedView = null;

            ApplyEditingState();
        }

        /// <summary>
        /// 정리 모드 여부를 화면과 입력에 반영합니다.
        /// </summary>
        private void ApplyEditingState()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = IsEditing ? 1f : idleAlpha;
                canvasGroup.blocksRaycasts = IsEditing;
                canvasGroup.interactable = IsEditing;
            }

            if (hintText != null) hintText.gameObject.SetActive(IsEditing);

            inputReader?.SetUiFocus(IsEditing);
        }

        /// <summary>
        /// 가방 크기에 맞춰 빈 칸을 그립니다.
        /// </summary>
        private void BuildCells()
        {
            for (int index = 0; index < cellViews.Count; index++) Destroy(cellViews[index].gameObject);

            cellViews.Clear();

            if (cellPrefab == null || gridRoot == null) return;

            GridInventory backpack = activity.Backpack;

            gridRoot.sizeDelta = new Vector2(backpack.Width * cellSize, backpack.Height * cellSize);

            for (int y = 0; y < backpack.Height; y++)
            {
                for (int x = 0; x < backpack.Width; x++)
                {
                    Image cell = Instantiate(cellPrefab, gridRoot);

                    LayoutView((RectTransform)cell.transform, x, y, 1, 1);
                    cellViews.Add(cell);
                }
            }
        }

        /// <summary>
        /// 가방에 든 물건을 다시 그립니다.
        /// </summary>
        private void Refresh()
        {
            for (int index = 0; index < itemViews.Count; index++) Destroy(itemViews[index].gameObject);

            itemViews.Clear();
            draggedView = null;

            if (activity == null || itemViewPrefab == null || gridRoot == null) return;

            GridInventory backpack = activity.Backpack;

            foreach (PlacedItem item in backpack.Items)
            {
                InventoryItemView view = Instantiate(itemViewPrefab, gridRoot);

                view.Bind(this, item, activity.GetDefinition(item.InstanceId));
                LayoutView(view.Rect, item.Position.X, item.Position.Y, item.Width, item.Height);
                itemViews.Add(view);
            }

            if (usageText != null)
                usageText.text = $"가방 {backpack.OccupiedCellCount} / {backpack.CellCount}칸";
        }

        /// <summary>
        /// 표시를 칸 좌표에 맞춰 놓습니다.
        /// </summary>
        /// <param name="target">놓을 표시의 사각 영역입니다.</param>
        /// <param name="x">왼쪽 위 칸의 X 좌표입니다.</param>
        /// <param name="y">왼쪽 위 칸의 Y 좌표입니다.</param>
        /// <param name="width">차지하는 가로 칸 수입니다.</param>
        /// <param name="height">차지하는 세로 칸 수입니다.</param>
        /// <remarks>칸마다 조금씩 띄워 두면 그 틈이 격자선 역할을 합니다.</remarks>
        private void LayoutView(RectTransform target, int x, int y, int width, int height)
        {
            target.anchorMin = new Vector2(0f, 1f);
            target.anchorMax = new Vector2(0f, 1f);
            target.pivot = new Vector2(0f, 1f);
            target.sizeDelta = new Vector2((width * cellSize) - cellGap, (height * cellSize) - cellGap);
            target.anchoredPosition = new Vector2(
                (x * cellSize) + (cellGap * 0.5f), -(y * cellSize) - (cellGap * 0.5f));
        }

        /// <summary>
        /// 물건 끌기를 시작합니다.
        /// </summary>
        /// <param name="view">끌기 시작한 표시입니다.</param>
        /// <param name="eventData">끌기 입력 정보입니다.</param>
        public void BeginDrag(InventoryItemView view, PointerEventData eventData)
        {
            if (IsEditing == false) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            draggedView = view;
            dragOffset = view.Rect.anchoredPosition - GetLocalPoint(eventData);

            view.Rect.SetAsLastSibling();
        }

        /// <summary>
        /// 끌고 있는 물건을 손끝을 따라 옮깁니다.
        /// </summary>
        /// <param name="eventData">끌기 입력 정보입니다.</param>
        public void UpdateDrag(PointerEventData eventData)
        {
            if (draggedView == null) return;

            draggedView.Rect.anchoredPosition = GetLocalPoint(eventData) + dragOffset;
        }

        /// <summary>
        /// 손을 뗀 자리에 물건을 놓습니다.
        /// </summary>
        /// <param name="eventData">끌기 입력 정보입니다.</param>
        /// <remarks>놓을 수 없는 자리면 원래 자리로 되돌립니다.</remarks>
        public void EndDrag(PointerEventData eventData)
        {
            if (draggedView == null || activity == null) return;

            InventoryItemView view = draggedView;
            draggedView = null;

            Vector2 position = view.Rect.anchoredPosition;
            GridPosition target = new(
                Mathf.RoundToInt(position.x / cellSize), Mathf.RoundToInt(-position.y / cellSize));

            if (activity.TryRearrange(view.InstanceId, target, view.IsRotated) == false) Refresh();
        }

        /// <summary>
        /// 물건을 가방에서 버립니다.
        /// </summary>
        /// <param name="instanceId">버릴 물건의 실체 번호입니다.</param>
        public void DropItem(int instanceId)
        {
            if (IsEditing == false || activity == null) return;
            if (activity.TryDrop(instanceId, out AnomalyDefinition definition) == false) return;

            SWLog.Log($"[{nameof(InventoryPanel)}] {definition.DisplayName}을(를) 버렸습니다.");
        }

        /// <summary>
        /// 끌고 있는 물건을 90도 돌립니다.
        /// </summary>
        private void RotateDragged()
        {
            draggedView.ToggleRotation();

            Vector2 size = new(
                (draggedView.Shape.GetWidth(draggedView.IsRotated) * cellSize) - cellGap,
                (draggedView.Shape.GetHeight(draggedView.IsRotated) * cellSize) - cellGap);

            draggedView.Rect.sizeDelta = size;

            // 돌리면 크기가 바뀌므로 잡고 있던 자리도 함께 옮겨 손끝이 물건 가운데에 오게 맞춥니다.
            Vector2 rotatedOffset = new(-size.x * 0.5f, size.y * 0.5f);

            draggedView.Rect.anchoredPosition += rotatedOffset - dragOffset;
            dragOffset = rotatedOffset;
        }

        /// <summary>
        /// 화면 좌표를 격자 영역 안의 좌표로 바꿉니다.
        /// </summary>
        /// <param name="eventData">좌표를 읽을 입력 정보입니다.</param>
        /// <returns>격자 영역의 왼쪽 위를 원점으로 하는 좌표입니다.</returns>
        private Vector2 GetLocalPoint(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridRoot, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

            // 격자 영역의 기준점이 어디에 있든 왼쪽 위를 원점으로 맞춥니다.
            return localPoint - new Vector2(
                -gridRoot.pivot.x * gridRoot.rect.width, (1f - gridRoot.pivot.y) * gridRoot.rect.height);
        }
        #endregion // 함수
    }
}
