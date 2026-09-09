using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SW.Attributes;
using SW.Base;

using ProjectR.Data;

namespace ProjectR.UI
{
    /// <summary>
    /// 스킬트리에 놓이는 노드 하나입니다.
    /// </summary>
    /// <remarks>
    /// 노드는 <b>자기가 어떤 상태인지 판단하지 않습니다.</b> 어떤 그림을 쓸지는 <see cref="SkillTreeUI"/>가
    /// 정해서 넘겨 줍니다. 노드가 스스로 판단하려면 보유 목록과 후원금을 알아야 하고,
    /// 그러면 화면에 놓인 노드 수만큼 같은 계산이 반복됩니다.
    /// 눌림과 마우스 올림도 알리기만 하고 처리하지 않습니다. 무엇을 열고 무엇을 살지는 트리의 몫입니다.
    /// <para>
    /// 어느 스킬인지는 프리팹에 저장해 둡니다. 실행할 때 짝지으면 프리팹만 열어 보아서는
    /// 어느 노드가 어느 스킬인지 알 수 없어, 배치를 손으로 다듬을 수가 없습니다.
    /// </para>
    /// </remarks>
    public class SkillNodeView : SWMonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region 필드
        /// <summary>이 노드가 보여 주는 스킬 정의입니다.</summary>
        [SWGroup("스킬")]
        [SerializeField, Tooltip("이 노드가 보여 주는 스킬 정의입니다.")]
        private UpgradeDefinition definition;

        /// <summary>상태에 따라 그림이 바뀌는 노드 테두리입니다.</summary>
        [SWGroup("표시")]
        [SerializeField, Tooltip("상태에 따라 그림이 바뀌는 노드 테두리입니다.")]
        private Image frameImage;

        /// <summary>스킬 아이콘을 그릴 이미지입니다.</summary>
        [SerializeField, Tooltip("스킬 아이콘을 그릴 이미지입니다.")]
        private Image iconImage;

        /// <summary>노드를 눌러 스킬을 사는 버튼입니다.</summary>
        [SWGroup("조작")]
        [SerializeField, Tooltip("노드를 눌러 스킬을 사는 버튼입니다.")]
        private Button button;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 노드가 보여 주는 스킬 정의입니다.</summary>
        public UpgradeDefinition Definition => definition;

        /// <summary>노드의 위치를 잡을 때 쓰는 사각 영역입니다.</summary>
        public RectTransform Rect => (RectTransform)transform;
        #endregion // 프로퍼티

        #region 이벤트
        /// <summary>노드를 눌렀을 때 알립니다.</summary>
        public event Action<SkillNodeView> Clicked;

        /// <summary>노드에 마우스를 올렸을 때 알립니다.</summary>
        public event Action<SkillNodeView> HoverEntered;

        /// <summary>노드에서 마우스가 벗어났을 때 알립니다.</summary>
        public event Action<SkillNodeView> HoverExited;
        #endregion // 이벤트

        #region 함수
        /// <summary>
        /// 버튼의 콜백을 이어 붙입니다.
        /// </summary>
        private void Awake()
        {
            button?.onClick.AddListener(HandleClicked);
        }

        /// <summary>
        /// 이어 붙인 콜백을 떼어 냅니다.
        /// </summary>
        private void OnDestroy()
        {
            button?.onClick.RemoveListener(HandleClicked);
        }

        /// <summary>
        /// 정의에 적힌 아이콘을 노드에 그립니다.
        /// </summary>
        /// <remarks>
        /// 프리팹을 만들 때 한 번 그려 두지만 실행할 때 다시 그립니다.
        /// 정의의 아이콘만 바꾸고 노드를 다시 만들지 않은 경우가 있기 때문입니다.
        /// </remarks>
        public void ApplyIcon()
        {
            if (iconImage == null) return;

            iconImage.sprite = definition != null ? definition.Icon : null;
            iconImage.enabled = iconImage.sprite != null;
        }

        /// <summary>
        /// 노드 테두리에 쓸 그림을 바꿉니다.
        /// </summary>
        /// <param name="sprite">테두리에 쓸 그림입니다.</param>
        public void SetFrame(Sprite sprite)
        {
            if (frameImage == null || sprite == null) return;

            frameImage.sprite = sprite;
        }

        /// <summary>
        /// 노드를 눌러 살 수 있는지 정합니다.
        /// </summary>
        /// <param name="isInteractable">누를 수 있게 하려면 true입니다.</param>
        /// <remarks>
        /// 버튼을 꺼도 마우스를 올린 것은 그대로 알립니다. 이미 산 스킬도 설명은 보여야 하고,
        /// 후원금이 모자라 못 사는 노드일수록 얼마가 필요한지 보여 주어야 하기 때문입니다.
        /// </remarks>
        public void SetInteractable(bool isInteractable)
        {
            if (button == null) return;

            button.interactable = isInteractable;
        }

        /// <summary>
        /// 마우스가 노드에 올라왔음을 알립니다.
        /// </summary>
        /// <param name="eventData">가리킨 지점의 정보입니다.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            HoverEntered?.Invoke(this);
        }

        /// <summary>
        /// 마우스가 노드에서 벗어났음을 알립니다.
        /// </summary>
        /// <param name="eventData">가리킨 지점의 정보입니다.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            HoverExited?.Invoke(this);
        }

        /// <summary>
        /// 버튼이 눌렸음을 알립니다.
        /// </summary>
        private void HandleClicked()
        {
            Clicked?.Invoke(this);
        }
        #endregion // 함수

#if UNITY_EDITOR
        #region 에디터
        /// <summary>
        /// 노드가 보여 줄 스킬을 정합니다. 노드를 만들 때 편집기가 부릅니다.
        /// </summary>
        /// <param name="target">노드가 보여 줄 스킬 정의입니다.</param>
        /// <param name="frameSprite">노드 테두리에 쓸 그림입니다.</param>
        public void EditorSetup(UpgradeDefinition target, Sprite frameSprite)
        {
            definition = target;
            name = $"SkillNode_{target.DefinitionId}";

            SetFrame(frameSprite);
            ApplyIcon();
        }
        #endregion // 에디터
#endif // UNITY_EDITOR
    }
}
