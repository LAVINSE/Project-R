using UnityEngine;

using SW.Attributes;
using SW.Base;

namespace ProjectR.Data
{
    /// <summary>
    /// 업그레이드 한 종류의 정의 에셋입니다.
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
    /// </remarks>
    [CreateAssetMenu(fileName = "Upgrade", menuName = "프로젝트R/업그레이드 정의")]
    public class UpgradeDefinition : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("효과")]
        [SerializeField, Tooltip("보너스를 얹을 스탯의 코드명입니다. StatKeys를 참고합니다.")]
        private string targetStatCode;

        [SerializeField, Tooltip("대상 스탯에 더할 값입니다.")]
        private float amount = 1f;

        [SWGroup("조건")]
        [SerializeField, Min(0), Tooltip("사는 데 드는 후원금입니다.")]
        private int cost = 1000;

        [SerializeField, Tooltip("먼저 갖고 있어야 하는 업그레이드의 코드명입니다. 비우면 조건이 없습니다.")]
        private string requiredUpgradeCode;
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

        /// <summary>먼저 갖고 있어야 하는 업그레이드의 코드명입니다. 없으면 빈 문자열입니다.</summary>
        public string RequiredUpgradeCode => requiredUpgradeCode ?? string.Empty;

        /// <summary>선행 업그레이드가 필요한지 여부입니다.</summary>
        public bool HasRequirement => string.IsNullOrEmpty(requiredUpgradeCode) == false;
        #endregion // 프로퍼티
    }
}
