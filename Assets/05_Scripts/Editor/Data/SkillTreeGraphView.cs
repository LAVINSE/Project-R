using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.Experimental.GraphView;

using UnityEngine;
using UnityEngine.UIElements;

using SW.Base;

using ProjectR.Data;

namespace ProjectR.Editor.Data
{
    /// <summary>
    /// 스킬 정의를 노드로 늘어놓고 선행 관계를 선으로 잇는 그래프 화면입니다.
    /// </summary>
    /// <remarks>
    /// 그래프는 자기 안에 배치를 따로 보관하지 않습니다. 노드를 옮기면 그 자리에서 정의 에셋의
    /// <see cref="UpgradeDefinition.TreePosition"/>에 적고, 선을 이으면 선행 목록에 적습니다.
    /// 따로 보관하면 저장을 잊은 채 창을 닫았을 때 배치가 사라지고, 무엇보다 에셋을 직접 고친
    /// 경우와 그래프가 어긋납니다.
    /// <para>
    /// 값은 <see cref="SerializedObject"/>로 씁니다. 필드에 직접 넣으면 되돌리기가 듣지 않고
    /// 에셋이 변경된 것으로 표시되지 않아 저장되지 않습니다.
    /// </para>
    /// </remarks>
    internal sealed class SkillTreeGraphView : GraphView
    {
        #region 상수
        /// <summary>선행 목록 필드의 이름입니다.</summary>
        private const string RequiredCodesPropertyName = "requiredUpgradeCodes";

        /// <summary>트리 좌표 필드의 이름입니다.</summary>
        private const string TreePositionPropertyName = "treePosition";
        #endregion // 상수

        #region 필드
        /// <summary>식별자로 노드를 찾을 때 쓰는 표입니다.</summary>
        private readonly Dictionary<string, SkillTreeNodeView> nodeViews = new();

        /// <summary>노드가 선택되었을 때 부를 콜백입니다.</summary>
        private readonly Action<UpgradeDefinition> nodeSelected;

        /// <summary>노드를 지워도 된다고 확인받았을 때 부를 콜백입니다.</summary>
        private readonly Action<UpgradeDefinition> nodeDeleteRequested;

        /// <summary>지금 보고 있는 데이터베이스입니다.</summary>
        private SWIODatabase database;
        #endregion // 필드

        #region 생성자
        /// <summary>
        /// 스킬트리 그래프 화면을 만듭니다.
        /// </summary>
        /// <param name="nodeSelected">노드가 선택되었을 때 부를 콜백입니다.</param>
        /// <param name="nodeDeleteRequested">노드를 지워도 된다고 확인받았을 때 부를 콜백입니다.</param>
        public SkillTreeGraphView(
            Action<UpgradeDefinition> nodeSelected,
            Action<UpgradeDefinition> nodeDeleteRequested)
        {
            this.nodeSelected = nodeSelected;
            this.nodeDeleteRequested = nodeDeleteRequested;

            style.flexGrow = 1f;

            GridBackground grid = new();
            grid.StretchToParentSize();
            Insert(0, grid);

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            graphViewChanged = HandleGraphViewChanged;
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 데이터베이스를 읽어 노드와 연결선을 다시 만듭니다.
        /// </summary>
        /// <param name="target">읽어 올 스킬 데이터베이스입니다.</param>
        public void Reload(SWIODatabase target)
        {
            database = target;

            graphViewChanged = null;

            foreach (GraphElement element in graphElements.ToList())
                RemoveElement(element);

            nodeViews.Clear();

            if (database != null)
            {
                CreateNodeViews();
                CreateEdgeViews();
            }

            graphViewChanged = HandleGraphViewChanged;
        }

        /// <summary>
        /// 데이터베이스의 스킬마다 노드를 만듭니다.
        /// </summary>
        private void CreateNodeViews()
        {
            for (int index = 0; index < database.Count; index++)
            {
                if (database[index] is not UpgradeDefinition definition) continue;
                if (nodeViews.ContainsKey(definition.DefinitionId)) continue;

                SkillTreeNodeView nodeView = new(definition, HandleNodeSelected);

                nodeView.SetPosition(new Rect(definition.TreePosition, Vector2.zero));

                AddElement(nodeView);
                nodeViews.Add(definition.DefinitionId, nodeView);
            }
        }

        /// <summary>
        /// 선행 목록대로 노드를 잇습니다.
        /// </summary>
        private void CreateEdgeViews()
        {
            foreach (SkillTreeNodeView nodeView in nodeViews.Values)
            {
                IReadOnlyList<string> required = nodeView.Definition.RequiredUpgradeCodes;

                for (int index = 0; index < required.Count; index++)
                {
                    if (nodeViews.TryGetValue(required[index] ?? string.Empty,
                        out SkillTreeNodeView fromView) == false)
                        continue;

                    AddElement(fromView.OutputPort.ConnectTo(nodeView.InputPort));
                }
            }
        }

        /// <summary>
        /// 노드를 새로 만들어 그래프에 얹습니다.
        /// </summary>
        /// <param name="definition">얹을 스킬 정의입니다.</param>
        public void AddNodeView(UpgradeDefinition definition)
        {
            if (definition == null || nodeViews.ContainsKey(definition.DefinitionId)) return;

            SkillTreeNodeView nodeView = new(definition, HandleNodeSelected);

            nodeView.SetPosition(new Rect(definition.TreePosition, Vector2.zero));

            AddElement(nodeView);
            nodeViews.Add(definition.DefinitionId, nodeView);
        }

        /// <summary>
        /// 지금 보고 있는 화면의 한가운데를 그래프 좌표로 돌려줍니다.
        /// </summary>
        /// <returns>화면 한가운데의 그래프 좌표입니다.</returns>
        /// <remarks>새로 만든 스킬을 보이지 않는 곳에 놓지 않으려고 씁니다.</remarks>
        public Vector2 GetViewCenter()
        {
            return contentViewContainer.WorldToLocal(layout.center);
        }

        /// <summary>
        /// 이을 수 있는 포트만 골라 돌려줍니다.
        /// </summary>
        /// <param name="startPort">연결을 시작한 포트입니다.</param>
        /// <param name="nodeAdapter">그래프가 넘겨 주는 연결 판정기입니다.</param>
        /// <returns>이을 수 있는 포트 목록입니다.</returns>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatible = new();

            ports.ForEach(port =>
            {
                if (port == startPort) return;
                if (port.node == startPort.node) return;
                if (port.direction == startPort.direction) return;

                compatible.Add(port);
            });

            return compatible;
        }

        /// <summary>
        /// 노드가 선택되었음을 창에 알립니다.
        /// </summary>
        /// <param name="nodeView">선택된 노드입니다.</param>
        private void HandleNodeSelected(SkillTreeNodeView nodeView)
        {
            nodeSelected?.Invoke(nodeView.Definition);
        }

        /// <summary>
        /// 그래프에서 일어난 변경을 정의 에셋에 적습니다.
        /// </summary>
        /// <param name="change">그래프가 알려 온 변경 내용입니다.</param>
        /// <returns>실제로 반영할 변경 내용입니다.</returns>
        /// <remarks>
        /// 노드를 지우는 것은 막습니다. 노드는 곧 에셋이라 Delete 키 한 번으로 사라지면
        /// 되돌릴 수 없습니다. 지우는 길은 창의 삭제 버튼 하나로 두어 확인을 거치게 합니다.
        /// </remarks>
        private GraphViewChange HandleGraphViewChanged(GraphViewChange change)
        {
            if (change.movedElements != null)
            {
                for (int index = 0; index < change.movedElements.Count; index++)
                {
                    if (change.movedElements[index] is not SkillTreeNodeView nodeView) continue;

                    SaveTreePosition(nodeView.Definition, nodeView.GetPosition().position);
                }
            }

            if (change.edgesToCreate != null)
                change.edgesToCreate.RemoveAll(edge => TryConnect(edge) == false);

            if (change.elementsToRemove is { Count: > 0 })
            {
                List<SkillTreeNodeView> removedNodes = new();

                for (int index = 0; index < change.elementsToRemove.Count; index++)
                {
                    if (change.elementsToRemove[index] is SkillTreeNodeView nodeView)
                        removedNodes.Add(nodeView);
                }

                if (removedNodes.Count > 0 && ConfirmNodeDelete(removedNodes) == false)
                {
                    // 취소하면 함께 지워지려던 선도 그대로 둡니다. 노드만 남고 선이 사라지면
                    // 화면과 선행 목록이 어긋난 채로 남습니다.
                    change.elementsToRemove.Clear();
                    return change;
                }

                // 선을 먼저 끊습니다. 에셋을 지운 뒤에 끊으면 이미 없는 정의를 건드리게 됩니다.
                for (int index = 0; index < change.elementsToRemove.Count; index++)
                {
                    if (change.elementsToRemove[index] is Edge edge) Disconnect(edge);
                }

                for (int index = 0; index < removedNodes.Count; index++)
                {
                    nodeViews.Remove(removedNodes[index].Definition.DefinitionId);
                    nodeDeleteRequested?.Invoke(removedNodes[index].Definition);
                }
            }

            return change;
        }

        /// <summary>
        /// 노드를 지워도 되는지 묻습니다.
        /// </summary>
        /// <param name="removedNodes">지우려는 노드 목록입니다.</param>
        /// <returns>지워도 되면 true를 반환합니다.</returns>
        /// <remarks>노드는 곧 에셋이라 Delete 키 한 번에 사라지면 되돌릴 수 없습니다.</remarks>
        private static bool ConfirmNodeDelete(List<SkillTreeNodeView> removedNodes)
        {
            string names = removedNodes.Count == 1
                ? removedNodes[0].Definition.DisplayName
                : $"{removedNodes[0].Definition.DisplayName} 외 {removedNodes.Count - 1}개";

            return EditorUtility.DisplayDialog("스킬 삭제",
                $"{names}을(를) 지웁니다.\n에셋이 삭제되고 다른 스킬의 선행 목록에서도 빠집니다.",
                "삭제", "취소");
        }

        /// <summary>
        /// 지금 고른 노드와 선을 지웁니다.
        /// </summary>
        /// <remarks>
        /// 도구 모음의 삭제 버튼이 부릅니다. Delete 키와 같은 길을 지나가므로 확인 창과
        /// 선행 목록 정리가 한 곳에만 있습니다.
        /// </remarks>
        public void RequestDeleteSelection()
        {
            List<GraphElement> targets = new();

            for (int index = 0; index < selection.Count; index++)
            {
                if (selection[index] is GraphElement element) targets.Add(element);
            }

            if (targets.Count == 0) return;

            DeleteElements(targets);
        }

        /// <summary>
        /// 노드가 놓인 자리를 정의 에셋에 적습니다.
        /// </summary>
        /// <param name="definition">자리를 적을 스킬 정의입니다.</param>
        /// <param name="position">노드가 놓인 자리입니다.</param>
        private static void SaveTreePosition(UpgradeDefinition definition, Vector2 position)
        {
            SerializedObject serialized = new(definition);

            serialized.FindProperty(TreePositionPropertyName).vector2Value = position;
            serialized.ApplyModifiedProperties();
        }

        /// <summary>
        /// 이은 선을 선행 목록에 적습니다.
        /// </summary>
        /// <param name="edge">새로 이은 선입니다.</param>
        /// <returns>선행 목록에 적었으면 true를 반환합니다.</returns>
        private bool TryConnect(Edge edge)
        {
            if (edge.output?.node is not SkillTreeNodeView fromView) return false;
            if (edge.input?.node is not SkillTreeNodeView toView) return false;

            string requiredCode = fromView.Definition.DefinitionId;

            if (WouldMakeCycle(fromView.Definition, toView.Definition))
            {
                EditorUtility.DisplayDialog("스킬트리 그래프",
                    $"{toView.Definition.DisplayName}은(는) 이미 " +
                    $"{fromView.Definition.DisplayName}보다 앞에 있습니다.\n\n" +
                    "서로를 선행으로 두면 두 스킬 모두 영원히 열리지 않습니다.", "확인");

                return false;
            }

            SerializedObject serialized = new(toView.Definition);
            SerializedProperty codes = serialized.FindProperty(RequiredCodesPropertyName);

            if (IndexOfCode(codes, requiredCode) >= 0) return false;

            codes.arraySize++;
            codes.GetArrayElementAtIndex(codes.arraySize - 1).stringValue = requiredCode;
            serialized.ApplyModifiedProperties();

            return true;
        }

        /// <summary>
        /// 지운 선을 선행 목록에서 뺍니다.
        /// </summary>
        /// <param name="edge">지운 선입니다.</param>
        private void Disconnect(Edge edge)
        {
            if (edge.output?.node is not SkillTreeNodeView fromView) return;
            if (edge.input?.node is not SkillTreeNodeView toView) return;

            SerializedObject serialized = new(toView.Definition);
            SerializedProperty codes = serialized.FindProperty(RequiredCodesPropertyName);
            int index = IndexOfCode(codes, fromView.Definition.DefinitionId);

            if (index < 0) return;

            codes.DeleteArrayElementAtIndex(index);
            serialized.ApplyModifiedProperties();
        }

        /// <summary>
        /// 선행 목록에서 코드명이 있는 자리를 찾습니다.
        /// </summary>
        /// <param name="codes">뒤질 선행 목록입니다.</param>
        /// <param name="code">찾을 코드명입니다.</param>
        /// <returns>찾은 자리입니다. 없으면 -1을 반환합니다.</returns>
        private static int IndexOfCode(SerializedProperty codes, string code)
        {
            for (int index = 0; index < codes.arraySize; index++)
            {
                if (codes.GetArrayElementAtIndex(index).stringValue == code) return index;
            }

            return -1;
        }

        /// <summary>
        /// 선행으로 두면 서로를 기다리게 되는지 확인합니다.
        /// </summary>
        /// <param name="from">선행이 될 스킬입니다.</param>
        /// <param name="to">선행을 받을 스킬입니다.</param>
        /// <returns>고리가 생기면 true를 반환합니다.</returns>
        /// <remarks>
        /// 고리가 생겨도 에러는 나지 않습니다. 그 대신 두 스킬이 화면에 영영 나타나지 않고,
        /// 그 사실은 게임을 켜서 트리를 한참 찍어 봐야 드러납니다. 여기서 막습니다.
        /// </remarks>
        private bool WouldMakeCycle(UpgradeDefinition from, UpgradeDefinition to)
        {
            if (from == to) return true;

            // from의 선행을 거슬러 올라가다 to를 만나면, from을 to의 선행으로 두는 순간 고리가 됩니다.
            Queue<UpgradeDefinition> pending = new();
            HashSet<string> visited = new();

            pending.Enqueue(from);

            while (pending.Count > 0)
            {
                UpgradeDefinition current = pending.Dequeue();

                if (visited.Add(current.DefinitionId) == false) continue;
                if (current == to) return true;

                IReadOnlyList<string> required = current.RequiredUpgradeCodes;

                for (int index = 0; index < required.Count; index++)
                {
                    if (nodeViews.TryGetValue(required[index] ?? string.Empty,
                        out SkillTreeNodeView previousView) == false)
                        continue;

                    pending.Enqueue(previousView.Definition);
                }
            }

            return false;
        }
        #endregion // 함수
    }
}
