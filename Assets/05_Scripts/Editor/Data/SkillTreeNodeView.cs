using System;

using UnityEditor.Experimental.GraphView;

using UnityEngine.UIElements;

using ProjectR.Data;

namespace ProjectR.Editor.Data
{
    /// <summary>
    /// 스킬트리 그래프에서 스킬 정의 하나를 보여 주는 노드입니다.
    /// </summary>
    /// <remarks>
    /// 값을 들고 있지 않습니다. 제목과 설명은 정의 에셋에서 그때그때 읽습니다.
    /// 들고 있으면 인스펙터에서 이름을 고쳤을 때 그래프에 옛 이름이 남습니다.
    /// </remarks>
    internal sealed class SkillTreeNodeView : Node
    {
        #region 필드
        /// <summary>노드가 선택되었을 때 부를 콜백입니다.</summary>
        private readonly Action<SkillTreeNodeView> selected;

        /// <summary>코드명과 비용을 적는 글상자입니다.</summary>
        private readonly Label detailLabel;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 노드가 보여 주는 스킬 정의입니다.</summary>
        public UpgradeDefinition Definition { get; }

        /// <summary>선행 스킬에서 들어오는 연결 포트입니다.</summary>
        public Port InputPort { get; }

        /// <summary>다음 스킬로 나가는 연결 포트입니다.</summary>
        public Port OutputPort { get; }
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 스킬 정의를 보여 주는 노드를 만듭니다.
        /// </summary>
        /// <param name="definition">노드가 보여 줄 스킬 정의입니다.</param>
        /// <param name="selected">노드가 선택되었을 때 부를 콜백입니다.</param>
        public SkillTreeNodeView(UpgradeDefinition definition, Action<SkillTreeNodeView> selected)
        {
            Definition = definition;
            this.selected = selected;

            viewDataKey = definition.DefinitionId;
            userData = definition;

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "선행";
            InputPort.tooltip = "이 스킬보다 먼저 찍어야 하는 스킬에서 들어옵니다.";
            inputContainer.Add(InputPort);

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = "다음";
            OutputPort.tooltip = "이 스킬을 찍으면 열리는 스킬로 나갑니다.";
            outputContainer.Add(OutputPort);

            detailLabel = new Label();
            detailLabel.style.marginLeft = 8f;
            detailLabel.style.marginRight = 8f;
            detailLabel.style.marginBottom = 6f;
            extensionContainer.Add(detailLabel);

            Refresh();
            RefreshExpandedState();
            RefreshPorts();
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 정의 에셋을 다시 읽어 제목과 설명을 맞춥니다.
        /// </summary>
        public void Refresh()
        {
            title = Definition.DisplayName;
            detailLabel.text = $"{Definition.DefinitionId}\n{Definition.Cost:N0} 후원금";
        }

        /// <summary>
        /// 노드가 선택되었음을 알립니다.
        /// </summary>
        public override void OnSelected()
        {
            base.OnSelected();

            selected?.Invoke(this);
        }
        #endregion // 함수
    }
}
