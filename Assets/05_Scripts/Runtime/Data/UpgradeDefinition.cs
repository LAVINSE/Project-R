using System.Collections.Generic;

using UnityEngine;

using SW.Attributes;
using SW.Base;

namespace ProjectR.Data
{
    /// <summary>
    /// 업그레이드 한 종류의 정의 에셋입니다. 스킬트리에서는 노드 하나가 됩니다.
    /// </summary>
    /// <remarks>
    /// 기획서 9.1절의 성장 네 축을 스탯 하나에 얹는 보너스로 표현합니다.
    /// 업그레이드는 <b>스탯 값을 직접 바꾸지 않고 보너스로 얹습니다.</b>
    /// 직접 바꾸면 어느 업그레이드가 얼마를 올렸는지 되짚을 수 없어, 되돌리거나 다시 계산할 수 없습니다.
    /// 보너스로 얹으면 보유 목록만 저장해 두고 불러올 때 다시 얹으면 됩니다.
    /// <para>
    /// <see cref="AnomalyDefinition"/>과 같은 방식으로 <see cref="SWIdentifiedObject"/>를 상속하고
    /// <see cref="SWIODatabase"/>에 모읍니다. 업그레이드를 늘릴 때 코드를 고치지 않습니다.
    /// </para>
    /// <para>
    /// 설계 원칙 1번은 "업그레이드는 반드시 백룸 플레이 감각을 바꿔야 한다"입니다.
    /// 그래서 대상 스탯이 수치판에만 보이는 값이면 그 업그레이드는 만들지 않습니다.
    /// 가방 용량을 첫 업그레이드로 고른 이유가 그것입니다(기획서 7.6절).
    /// </para>
    /// <para>
    /// 트리 좌표와 아이콘은 화면에만 쓰이는 값이라 원래는 데이터 정의가 아니라 별도 배치 에셋에
    /// 있어야 합니다. 그러지 않은 이유는 <b>배치와 정의를 따로 두면 둘이 어긋날 수 있기 때문</b>입니다.
    /// 스킬을 하나 지웠는데 배치 에셋에 좌표가 남으면 빈 자리에 노드가 서고, 그 어긋남은 실행해 봐야
    /// 드러납니다. 한 에셋에 두면 스킬이 사라질 때 좌표도 같이 사라집니다.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(fileName = "Upgrade", menuName = "프로젝트R/업그레이드 정의")]
    public class UpgradeDefinition : SWIdentifiedObject
    {
        #region 필드
        /// <summary>보너스를 얹을 스탯의 코드명입니다. StatKeys를 참고합니다.</summary>
        [SWGroup("효과")]
        [SerializeField, Tooltip("보너스를 얹을 스탯의 코드명입니다. StatKeys를 참고합니다.")]
        private string targetStatCode;

        /// <summary>대상 스탯에 더할 값입니다.</summary>
        [SerializeField, Tooltip("대상 스탯에 더할 값입니다.")]
        private float amount = 1f;

        /// <summary>사는 데 드는 후원금입니다.</summary>
        [SWGroup("조건")]
        [SerializeField, Min(0), Tooltip("사는 데 드는 후원금입니다.")]
        private int cost = 1000;

        /// <summary>먼저 갖고 있어야 하는 업그레이드의 코드명 목록입니다. 비우면 조건이 없습니다.</summary>
        [SerializeField, Tooltip("먼저 갖고 있어야 하는 업그레이드의 코드명 목록입니다. 비우면 조건이 없습니다.")]
        private List<string> requiredUpgradeCodes = new();

        /// <summary>스킬트리 노드에 그릴 아이콘입니다.</summary>
        [SWGroup("스킬트리")]
        [SerializeField, Tooltip("스킬트리 노드에 그릴 아이콘입니다.")]
        private Sprite icon;

        /// <summary>스킬트리에서 노드를 놓을 자리입니다. 그래프 편집 창이 채웁니다.</summary>
        [SerializeField, Tooltip("스킬트리에서 노드를 놓을 자리입니다. 그래프 편집 창이 채웁니다.")]
        private Vector2 treePosition;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>보유 목록과 저장 데이터가 이 업그레이드를 가리킬 때 쓰는 식별자입니다.</summary>
        /// <remarks>코드명을 비워 두면 에셋 이름을 대신 씁니다.</remarks>
        public string DefinitionId => string.IsNullOrEmpty(CodeName) ? name : CodeName;

        /// <summary>보너스를 얹을 스탯의 코드명입니다.</summary>
        public string TargetStatCode => targetStatCode;

        /// <summary>대상 스탯에 더할 값입니다.</summary>
        public float Amount => amount;

        /// <summary>사는 데 드는 후원금입니다.</summary>
        public int Cost => cost;

        /// <summary>먼저 갖고 있어야 하는 업그레이드의 코드명 목록입니다.</summary>
        public IReadOnlyList<string> RequiredUpgradeCodes => requiredUpgradeCodes;

        /// <summary>선행 업그레이드가 필요한지 여부입니다.</summary>
        public bool HasRequirement => requiredUpgradeCodes != null && requiredUpgradeCodes.Count > 0;

        /// <summary>스킬트리 노드에 그릴 아이콘입니다. 없으면 null입니다.</summary>
        public Sprite Icon => icon;

        /// <summary>스킬트리에서 노드를 놓을 자리입니다.</summary>
        public Vector2 TreePosition => treePosition;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 이 업그레이드의 선행 조건이 채워졌는지 확인합니다.
        /// </summary>
        /// <param name="streamer">보유 업그레이드를 읽을 스트리머 진행도입니다.</param>
        /// <returns>선행 조건을 채웠으면 true를 반환합니다.</returns>
        /// <remarks>
        /// 선행이 여럿일 때는 <b>하나라도</b> 갖고 있으면 열립니다. 트리에서 가지가 다시 합쳐지는
        /// 자리를 "둘 다 찍어야 하는 관문"이 아니라 "어느 쪽으로 와도 닿는 길"로 두기 위해서입니다.
        /// 전부 갖춰야 하는 규칙으로 바꾸려면 아래 반복문에서 찾은 즉시 참을 돌려주는 대신
        /// 하나라도 없으면 거짓을 돌려주면 됩니다.
        /// <para>
        /// 구매 판정과 화면 표시가 같은 규칙을 봐야 하므로 규칙을 정의 쪽에 둡니다.
        /// 부르는 쪽마다 따로 적으면 한쪽만 고쳤을 때 "살 수 있는데 안 보이는" 노드가 생깁니다.
        /// </para>
        /// </remarks>
        public bool IsUnlockedBy(StreamerProgress streamer)
        {
            if (HasRequirement == false) return true;
            if (streamer == null) return false;

            for (int index = 0; index < requiredUpgradeCodes.Count; index++)
            {
                if (string.IsNullOrEmpty(requiredUpgradeCodes[index])) continue;
                if (streamer.HasUpgrade(requiredUpgradeCodes[index])) return true;
            }

            return false;
        }
        #endregion // 함수
    }
}
