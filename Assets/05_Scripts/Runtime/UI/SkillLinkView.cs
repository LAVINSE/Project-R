using UnityEngine;
using UnityEngine.UI;

using SW.Attributes;
using SW.Base;

namespace ProjectR.UI
{
    /// <summary>
    /// 스킬 노드 둘을 잇는 선입니다.
    /// </summary>
    /// <remarks>
    /// 이은 두 노드를 프리팹에 저장해 둡니다. 자리만 잡아 두고 누구와 누구를 이었는지는 잊어버리면,
    /// 실행할 때 어느 선을 켜고 꺼야 하는지 알 수 없습니다.
    /// 선은 마우스 입력을 받지 않습니다. 받으면 노드 위를 지나가는 선이 노드보다 먼저 마우스를 잡아
    /// 설명이 뜨지 않습니다.
    /// </remarks>
    [RequireComponent(typeof(Image))]
    public class SkillLinkView : SWMonoBehaviour
    {
        #region 필드
        /// <summary>선이 시작되는 노드입니다. 선행 스킬 쪽입니다.</summary>
        [SWGroup("연결")]
        [SerializeField, Tooltip("선이 시작되는 노드입니다. 선행 스킬 쪽입니다.")]
        private SkillNodeView from;

        /// <summary>선이 끝나는 노드입니다. 선행을 필요로 하는 쪽입니다.</summary>
        [SerializeField, Tooltip("선이 끝나는 노드입니다. 선행을 필요로 하는 쪽입니다.")]
        private SkillNodeView to;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>선이 시작되는 노드입니다.</summary>
        public SkillNodeView From => from;

        /// <summary>선이 끝나는 노드입니다.</summary>
        public SkillNodeView To => to;
        #endregion // 프로퍼티

#if UNITY_EDITOR
        #region 에디터
        /// <summary>
        /// 이을 두 노드를 정하고 자리와 길이와 기울기를 맞춥니다.
        /// </summary>
        /// <param name="fromNode">선이 시작되는 노드입니다.</param>
        /// <param name="toNode">선이 끝나는 노드입니다.</param>
        /// <param name="thickness">선의 굵기(픽셀)입니다.</param>
        public void EditorConnect(SkillNodeView fromNode, SkillNodeView toNode, float thickness)
        {
            from = fromNode;
            to = toNode;
            name = $"SkillLink_{fromNode.name}_{toNode.name}";

            GetComponent<Image>().raycastTarget = false;

            EditorLayout(thickness);
        }

        /// <summary>
        /// 이어 둔 두 노드에 맞추어 자리와 길이와 기울기를 다시 맞춥니다.
        /// </summary>
        /// <param name="thickness">선의 굵기(픽셀)입니다.</param>
        /// <remarks>노드를 손으로 옮긴 뒤에 선만 제자리에 남지 않도록 다시 부릅니다.</remarks>
        public void EditorLayout(float thickness)
        {
            if (from == null || to == null) return;

            RectTransform rect = (RectTransform)transform;
            Vector2 start = from.Rect.anchoredPosition;
            Vector2 direction = to.Rect.anchoredPosition - start;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = start + direction * 0.5f;
            rect.sizeDelta = new Vector2(direction.magnitude, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
        #endregion // 에디터
#endif // UNITY_EDITOR
    }
}
