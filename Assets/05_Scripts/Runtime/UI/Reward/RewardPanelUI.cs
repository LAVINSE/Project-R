using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

using SW.Base;

using ProjectR.Enum;

namespace ProjectR.UI.Reward
{
    /// <summary>
    /// 지정한 재화 종류의 보상을 표시하는 패널입니다.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "ProjectR.UI", sourceAssembly: "ProjectR.UI", sourceClassName: "RewardPanelUI")]
    public class RewardPanelUI : SWMonoBehaviour
    {
        #region 필드
        /// <summary>패널에서 표시할 보상 재화의 종류입니다.</summary>
        [SerializeField] private ERewardType rewardType;
        #endregion // 필드
    }
}
