using System.Collections.Generic;

using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Data;

namespace ProjectR.UI
{
    /// <summary>
    /// 산 스킬의 이웃만 드러나는 스킬트리 화면입니다.
    /// </summary>
    /// <remarks>
    /// 트리 전체를 처음부터 보여 주지 않습니다. 지금 갖고 있는 스킬과 <b>거기에 바로 이어진 스킬</b>만
    /// 보이고, 하나를 사면 그 옆이 새로 드러납니다. 전부 펼쳐 두면 어디로 갈지 고르는 일이
    /// 첫 화면에서 끝나 버리고, 그다음부터는 정해 둔 순서를 따라가는 일만 남습니다.
    /// <para>
    /// 어느 노드가 보이는지는 <see cref="UpgradeDefinition.IsUnlockedBy"/>가 정합니다. 구매 판정과
    /// 같은 함수입니다. 여기서 따로 판정하면 한쪽만 고쳤을 때 살 수 있는데 안 보이는 노드가 생깁니다.
    /// </para>
    /// <para>
    /// <b>노드와 선은 프리팹에 실제로 놓여 있습니다.</b> 실행할 때 만들지 않습니다.
    /// 만들어 버리면 게임을 켜기 전에는 트리가 어떻게 생겼는지 볼 수 없어 배치를 손으로 다듬을 수 없고,
    /// 인스펙터에서 노드 하나만 다르게 꾸미는 일도 못 합니다.
    /// 대신 놓는 일은 손으로 하지 않습니다. 인스펙터의 <b>스킬 노드 생성</b> 버튼이 정의 목록을 읽어
    /// 한 번에 놓아 주고, 그 뒤에 옮긴 자리는 <b>노드 위치 저장</b>이 정의 에셋에 되돌려 적습니다.
    /// </para>
    /// </remarks>
    public class SkillTreeUI : SWMonoBehaviour
    {
        #region 필드
        /// <summary>스킬 정의를 모아 둔 데이터베이스입니다.</summary>
        [SWGroup("데이터")]
        [SerializeField, Tooltip("스킬 정의를 모아 둔 데이터베이스입니다.")]
        private SWIODatabase upgradeDatabase;

        /// <summary>노드와 연결선이 놓이는 영역입니다. 스크롤 뷰의 Content입니다.</summary>
        [SWGroup("배치")]
        [SerializeField, Tooltip("노드와 연결선이 놓이는 영역입니다. 스크롤 뷰의 Content입니다.")]
        private RectTransform nodeRoot;

        /// <summary>노드 하나를 그릴 프리팹입니다.</summary>
        [SerializeField, Tooltip("노드 하나를 그릴 프리팹입니다.")]
        private SkillNodeView nodePrefab;

        /// <summary>노드 사이를 잇는 선을 그릴 프리팹입니다.</summary>
        [SerializeField, Tooltip("노드 사이를 잇는 선을 그릴 프리팹입니다.")]
        private SkillLinkView linkPrefab;

        /// <summary>연결선의 굵기(픽셀)입니다.</summary>
        [SerializeField, Min(1f), Tooltip("연결선의 굵기(픽셀)입니다.")]
        private float linkThickness = 6f;

        /// <summary>가장자리 노드가 화면 끝에 붙지 않도록 트리 바깥에 둘 여백(픽셀)입니다.</summary>
        [SerializeField, Min(0f), Tooltip("가장자리 노드가 화면 끝에 붙지 않도록 트리 바깥에 둘 여백(픽셀)입니다.")]
        private float contentPadding = 300f;

        /// <summary>이미 산 스킬에 쓸 노드 그림입니다.</summary>
        [SWGroup("상태 그림")]
        [SerializeField, Tooltip("이미 산 스킬에 쓸 노드 그림입니다.")]
        private Sprite ownedSprite;

        /// <summary>아직 사지 않은 스킬에 쓸 노드 그림입니다.</summary>
        [SerializeField, Tooltip("아직 사지 않은 스킬에 쓸 노드 그림입니다.")]
        private Sprite purchasableSprite;

        /// <summary>마우스를 올린 노드에 쓸 그림입니다.</summary>
        [SerializeField, Tooltip("마우스를 올린 노드에 쓸 그림입니다.")]
        private Sprite hoveredSprite;

        /// <summary>노드에 마우스를 올렸을 때 띄울 설명입니다.</summary>
        [SWGroup("설명")]
        [SerializeField, Tooltip("노드에 마우스를 올렸을 때 띄울 설명입니다.")]
        private SkillInfoPopup infoPopup;

        /// <summary>프리팹에 놓여 있는 노드 목록입니다.</summary>
        private readonly List<SkillNodeView> nodes = new();

        /// <summary>프리팹에 놓여 있는 연결선 목록입니다.</summary>
        private readonly List<SkillLinkView> links = new();

        /// <summary>지금 마우스를 올려 둔 노드입니다. 없으면 null입니다.</summary>
        private SkillNodeView hoveredNode;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 놓여 있는 노드를 찾아 이어 붙이고 지금 상태를 반영합니다.
        /// </summary>
        private void Start()
        {
            infoPopup?.Hide();

            Bind();
            Refresh();
        }

        /// <summary>
        /// 이어 붙인 콜백을 떼어 냅니다.
        /// </summary>
        private void OnDestroy()
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                nodes[index].Clicked -= HandleNodeClicked;
                nodes[index].HoverEntered -= HandleNodeHoverEntered;
                nodes[index].HoverExited -= HandleNodeHoverExited;
            }
        }

        /// <summary>
        /// 놓여 있는 노드와 연결선을 찾아 알림을 이어 붙입니다.
        /// </summary>
        private void Bind()
        {
            if (nodeRoot == null)
            {
                SWLog.LogError($"[{nameof(SkillTreeUI)}] 배치 영역이 비어 있어 트리를 준비하지 못했습니다.");
                return;
            }

            nodeRoot.GetComponentsInChildren(true, nodes);
            nodeRoot.GetComponentsInChildren(true, links);

            for (int index = nodes.Count - 1; index >= 0; index--)
            {
                if (nodes[index].Definition == null)
                {
                    SWLog.LogWarning($"[{nameof(SkillTreeUI)}] 스킬이 정해지지 않은 노드라 건너뜁니다: " +
                        $"{nodes[index].name}");

                    nodes[index].gameObject.SetActive(false);
                    nodes.RemoveAt(index);
                    continue;
                }

                nodes[index].ApplyIcon();

                nodes[index].Clicked += HandleNodeClicked;
                nodes[index].HoverEntered += HandleNodeHoverEntered;
                nodes[index].HoverExited += HandleNodeHoverExited;
            }

            WarnMissingNodes();
        }

        /// <summary>
        /// 데이터베이스에 있는데 화면에 놓이지 않은 스킬을 알립니다.
        /// </summary>
        /// <remarks>
        /// 스킬을 새로 만들고 노드를 다시 놓지 않으면 그 스킬은 트리에 영영 나타나지 않습니다.
        /// 아무 일도 일어나지 않는 것처럼 보이므로 여기서 이름을 찍어 둡니다.
        /// </remarks>
        private void WarnMissingNodes()
        {
            if (upgradeDatabase == null) return;

            for (int index = 0; index < upgradeDatabase.Count; index++)
            {
                if (upgradeDatabase[index] is not UpgradeDefinition definition) continue;
                if (FindNode(definition) != null) continue;

                SWLog.LogWarning($"[{nameof(SkillTreeUI)}] 화면에 놓이지 않은 스킬이 있습니다: " +
                    $"{definition.DefinitionId}. 인스펙터의 스킬 노드 생성을 다시 누르세요.");
            }
        }

        /// <summary>
        /// 정의로 노드를 찾습니다.
        /// </summary>
        /// <param name="definition">찾을 스킬 정의입니다.</param>
        /// <returns>찾은 노드입니다. 없으면 null을 반환합니다.</returns>
        private SkillNodeView FindNode(UpgradeDefinition definition)
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                if (nodes[index].Definition == definition) return nodes[index];
            }

            return null;
        }

        /// <summary>
        /// 보유 상태를 다시 읽어 노드와 연결선의 표시를 맞춥니다.
        /// </summary>
        public void Refresh()
        {
            StreamerProgress streamer = GameManager.Instance.State?.ActiveStreamer;
            int donation = GameManager.Instance.State?.Donation ?? 0;

            for (int index = 0; index < nodes.Count; index++)
            {
                UpgradeDefinition definition = nodes[index].Definition;
                bool isOwned = IsOwned(streamer, definition);
                bool isVisible = isOwned || definition.IsUnlockedBy(streamer);

                nodes[index].gameObject.SetActive(isVisible);

                if (isVisible == false) continue;

                nodes[index].SetFrame(isOwned ? ownedSprite : purchasableSprite);
                nodes[index].SetInteractable(isOwned == false && donation >= definition.Cost);
            }

            for (int index = 0; index < links.Count; index++)
            {
                links[index].gameObject.SetActive(IsLinkVisible(links[index]));
            }
        }

        /// <summary>
        /// 연결선을 보여 줄지 정합니다.
        /// </summary>
        /// <param name="link">확인할 연결선입니다.</param>
        /// <returns>보여 주어야 하면 true를 반환합니다.</returns>
        /// <remarks>양쪽 노드가 모두 드러나 있을 때만 긋습니다. 한쪽이 아직 없으면 선이 허공으로 뻗습니다.</remarks>
        private static bool IsLinkVisible(SkillLinkView link)
        {
            if (link.From == null || link.To == null) return false;

            return link.From.gameObject.activeSelf && link.To.gameObject.activeSelf;
        }

        /// <summary>
        /// 스킬을 이미 갖고 있는지 확인합니다.
        /// </summary>
        /// <param name="streamer">보유 목록을 읽을 스트리머 진행도입니다.</param>
        /// <param name="definition">확인할 스킬 정의입니다.</param>
        /// <returns>갖고 있으면 true를 반환합니다.</returns>
        private static bool IsOwned(StreamerProgress streamer, UpgradeDefinition definition)
        {
            return streamer != null && streamer.HasUpgrade(definition.DefinitionId);
        }

        /// <summary>
        /// 노드를 눌러 스킬을 삽니다.
        /// </summary>
        /// <param name="node">눌린 노드입니다.</param>
        /// <remarks>
        /// 샀으면 트리를 다시 그립니다. 방금 산 노드의 이웃이 이때 드러납니다.
        /// 마우스는 아직 노드 위에 있으므로 설명도 새 상태로 다시 띄웁니다.
        /// </remarks>
        private void HandleNodeClicked(SkillNodeView node)
        {
            if (GameManager.Instance.TryPurchaseUpgrade(node.Definition.DefinitionId) == false) return;

            Refresh();

            HandleNodeHoverEntered(node);
        }

        /// <summary>
        /// 노드에 마우스를 올렸을 때 설명을 띄웁니다.
        /// </summary>
        /// <param name="node">마우스를 올린 노드입니다.</param>
        private void HandleNodeHoverEntered(SkillNodeView node)
        {
            hoveredNode = node;

            node.SetFrame(hoveredSprite);

            StreamerProgress streamer = GameManager.Instance.State?.ActiveStreamer;

            infoPopup?.Show(
                node.Definition,
                IsOwned(streamer, node.Definition),
                GameManager.Instance.State?.Donation ?? 0,
                node.Rect);
        }

        /// <summary>
        /// 노드에서 마우스가 벗어났을 때 설명을 닫습니다.
        /// </summary>
        /// <param name="node">마우스가 벗어난 노드입니다.</param>
        /// <remarks>
        /// 벗어난 노드가 지금 보고 있던 노드일 때만 닫습니다. 노드 사이를 빠르게 지나가면
        /// 다음 노드에 올라간 알림이 먼저 오고 이전 노드에서 벗어난 알림이 뒤에 올 수 있는데,
        /// 그때 닫아 버리면 방금 띄운 설명이 사라집니다.
        /// </remarks>
        private void HandleNodeHoverExited(SkillNodeView node)
        {
            StreamerProgress streamer = GameManager.Instance.State?.ActiveStreamer;

            node.SetFrame(IsOwned(streamer, node.Definition) ? ownedSprite : purchasableSprite);

            if (ReferenceEquals(hoveredNode, node) == false) return;

            hoveredNode = null;

            infoPopup?.Hide();
        }
        #endregion // 함수

#if UNITY_EDITOR
        #region 에디터
        /// <summary>
        /// 데이터베이스를 읽어 노드와 연결선을 프리팹에 놓습니다.
        /// </summary>
        /// <remarks>
        /// 이미 놓여 있던 노드와 선은 지우고 다시 놓습니다. 그래서 손으로 옮긴 자리는 사라집니다.
        /// 그 자리를 지키려면 먼저 <b>노드 위치 저장</b>을 눌러 정의 에셋에 적어 두어야 합니다.
        /// 자리의 원본은 정의 에셋이고 프리팹은 그것을 펼쳐 놓은 결과입니다.
        /// </remarks>
        [SWButton("스킬 노드 생성", 12f)]
        private void EditorBuildNodes()
        {
            if (upgradeDatabase == null || nodeRoot == null || nodePrefab == null || linkPrefab == null)
            {
                SWLog.LogError($"[{nameof(SkillTreeUI)}] 데이터베이스와 배치 참조를 먼저 채우세요.");
                return;
            }

            // 씬에 있는 물건을 끼워 두면 프리팹 인스턴스가 아니라 null이 만들어져 조용히 실패합니다.
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(nodePrefab.gameObject) == false
                || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(linkPrefab.gameObject) == false)
            {
                SWLog.LogError($"[{nameof(SkillTreeUI)}] 노드와 연결선은 프리팹 에셋을 끼워야 합니다.");
                return;
            }

            EditorClearNodes();

            List<UpgradeDefinition> definitions = new();
            List<SkillNodeView> created = new();

            for (int index = 0; index < upgradeDatabase.Count; index++)
            {
                if (upgradeDatabase[index] is not UpgradeDefinition definition) continue;

                definitions.Add(definition);
                created.Add(EditorCreateNode(definition));
            }

            EditorCreateLinks(definitions, created);
            EditorFitContent(created);
            EditorMarkDirty();

            SWLog.Log($"[{nameof(SkillTreeUI)}] 스킬 노드 {created.Count}개를 놓았습니다.");
        }

        /// <summary>
        /// 프리팹에서 옮긴 노드의 자리를 정의 에셋에 되돌려 적습니다.
        /// </summary>
        /// <remarks>
        /// 프리팹을 저장할 때도 자동으로 불립니다. 그래야 눈으로 맞춘 배치와 그래프 창이 어긋나지 않습니다.
        /// 정의 에셋은 바뀐 것으로만 표시하고 여기서 파일에 쓰지는 않습니다.
        /// 프로젝트를 저장할 때 함께 기록됩니다.
        /// </remarks>
        [SWButton("노드 위치 저장")]
        private void EditorSaveNodePositionsButton()
        {
            EditorSaveNodePositions(true);
        }

        /// <summary>
        /// 프리팹에서 옮긴 노드의 자리를 정의 에셋에 되돌려 적습니다.
        /// </summary>
        /// <param name="markDirty">프리팹을 바뀐 것으로 표시할지 여부입니다.</param>
        /// <remarks>
        /// 프리팹을 저장하는 도중에 부를 때는 표시하지 않습니다. 저장이 끝난 직후에 다시
        /// 바뀐 것으로 표시하면 방금 저장한 프리팹에 별표가 남아, 저장이 안 된 것처럼 보입니다.
        /// </remarks>
        public void EditorSaveNodePositions(bool markDirty)
        {
            if (nodeRoot == null) return;

            List<SkillNodeView> placed = new();
            List<SkillLinkView> placedLinks = new();

            nodeRoot.GetComponentsInChildren(true, placed);
            nodeRoot.GetComponentsInChildren(true, placedLinks);

            int saved = 0;

            for (int index = 0; index < placed.Count; index++)
            {
                if (placed[index].Definition == null) continue;

                Vector2 anchored = placed[index].Rect.anchoredPosition;
                UnityEditor.SerializedObject serialized = new(placed[index].Definition);

                serialized.FindProperty("treePosition").vector2Value = new Vector2(anchored.x, -anchored.y);
                serialized.ApplyModifiedProperties();

                saved++;
            }

            for (int index = 0; index < placedLinks.Count; index++)
                placedLinks[index].EditorLayout(linkThickness);

            EditorFitContent(placed);

            if (markDirty) EditorMarkDirty();

            SWLog.Log($"[{nameof(SkillTreeUI)}] 노드 {saved}개의 자리를 정의에 적었습니다.");
        }

        /// <summary>
        /// 놓여 있던 노드와 연결선을 모두 지웁니다.
        /// </summary>
        private void EditorClearNodes()
        {
            for (int index = nodeRoot.childCount - 1; index >= 0; index--)
            {
                GameObject child = nodeRoot.GetChild(index).gameObject;

                if (child.GetComponent<SkillNodeView>() == null
                    && child.GetComponent<SkillLinkView>() == null)
                    continue;

                UnityEditor.Undo.DestroyObjectImmediate(child);
            }
        }

        /// <summary>
        /// 정의 하나를 그릴 노드를 놓습니다.
        /// </summary>
        /// <param name="definition">노드가 보여 줄 스킬 정의입니다.</param>
        /// <returns>놓인 노드입니다.</returns>
        /// <remarks>
        /// 세로를 뒤집어 놓습니다. 자리를 정하는 그래프 창은 아래로 갈수록 y가 커지고
        /// 화면은 위로 갈수록 y가 커지므로, 뒤집지 않으면 트리가 위아래로 뒤집혀 나옵니다.
        /// </remarks>
        private SkillNodeView EditorCreateNode(UpgradeDefinition definition)
        {
            GameObject instance =
                (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(nodePrefab.gameObject, nodeRoot);
            SkillNodeView node = instance.GetComponent<SkillNodeView>();

            node.Rect.anchorMin = new Vector2(0.5f, 0.5f);
            node.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            node.Rect.anchoredPosition = new Vector2(definition.TreePosition.x, -definition.TreePosition.y);

            node.EditorSetup(definition, purchasableSprite);

            UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "스킬 노드 생성");

            return node;
        }

        /// <summary>
        /// 선행 관계대로 노드를 잇는 선을 놓습니다.
        /// </summary>
        /// <param name="definitions">놓은 노드에 대응하는 스킬 정의 목록입니다.</param>
        /// <param name="created">놓은 노드 목록입니다.</param>
        /// <remarks>
        /// 선은 놓자마자 맨 뒤로 보냅니다. 나중에 놓은 것이 위에 그려지므로 그대로 두면
        /// 선이 노드를 가로질러 지나갑니다.
        /// </remarks>
        private void EditorCreateLinks(List<UpgradeDefinition> definitions, List<SkillNodeView> created)
        {
            for (int index = 0; index < definitions.Count; index++)
            {
                IReadOnlyList<string> required = definitions[index].RequiredUpgradeCodes;

                for (int codeIndex = 0; codeIndex < required.Count; codeIndex++)
                {
                    SkillNodeView from = EditorFindNode(definitions, created, required[codeIndex]);

                    if (from == null)
                    {
                        SWLog.LogWarning($"[{nameof(SkillTreeUI)}] {definitions[index].DefinitionId}의 " +
                            $"선행 스킬을 찾지 못해 선을 긋지 않습니다: {required[codeIndex]}");
                        continue;
                    }

                    GameObject instance =
                        (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(linkPrefab.gameObject, nodeRoot);
                    SkillLinkView link = instance.GetComponent<SkillLinkView>();

                    instance.transform.SetAsFirstSibling();
                    link.EditorConnect(from, created[index], linkThickness);

                    UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "스킬 연결선 생성");
                }
            }
        }

        /// <summary>
        /// 식별자로 놓인 노드를 찾습니다.
        /// </summary>
        /// <param name="definitions">놓은 노드에 대응하는 스킬 정의 목록입니다.</param>
        /// <param name="created">놓은 노드 목록입니다.</param>
        /// <param name="definitionId">찾을 스킬의 식별자입니다.</param>
        /// <returns>찾은 노드입니다. 없으면 null을 반환합니다.</returns>
        private static SkillNodeView EditorFindNode(
            List<UpgradeDefinition> definitions, List<SkillNodeView> created, string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId)) return null;

            for (int index = 0; index < definitions.Count; index++)
            {
                if (definitions[index].DefinitionId != definitionId) continue;

                return created[index];
            }

            return null;
        }

        /// <summary>
        /// 노드가 전부 들어가도록 배치 영역의 크기를 맞춥니다.
        /// </summary>
        /// <param name="placed">놓여 있는 노드 목록입니다.</param>
        /// <remarks>
        /// 원점을 한가운데에 두고 좌우와 위아래로 같은 크기를 잡습니다. 노드가 몰려 있는 쪽에 맞추면
        /// 노드를 옮길 때마다 한가운데가 움직여, 저장하고 다시 놓을 때 자리가 조금씩 밀립니다.
        /// </remarks>
        private void EditorFitContent(List<SkillNodeView> placed)
        {
            if (placed.Count == 0) return;

            Vector2 reach = Vector2.zero;

            for (int index = 0; index < placed.Count; index++)
            {
                Vector2 anchored = placed[index].Rect.anchoredPosition;

                reach = Vector2.Max(reach, new Vector2(Mathf.Abs(anchored.x), Mathf.Abs(anchored.y)));
            }

            nodeRoot.anchorMin = new Vector2(0.5f, 0.5f);
            nodeRoot.anchorMax = new Vector2(0.5f, 0.5f);
            nodeRoot.pivot = new Vector2(0.5f, 0.5f);
            nodeRoot.anchoredPosition = Vector2.zero;
            nodeRoot.sizeDelta = reach * 2f + Vector2.one * (contentPadding * 2f);
        }

        /// <summary>
        /// 바뀐 내용을 저장 대상으로 표시합니다.
        /// </summary>
        private void EditorMarkDirty()
        {
            UnityEditor.EditorUtility.SetDirty(this);

            if (Application.isPlaying) return;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        #endregion // 에디터
#endif // UNITY_EDITOR
    }
}
