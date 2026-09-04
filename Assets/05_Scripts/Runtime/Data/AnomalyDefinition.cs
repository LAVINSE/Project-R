using UnityEngine;

using SW.Attributes;
using SW.Base;

using ProjectR.Inventory;
using ProjectR.Enum;

namespace ProjectR.Data
{
    /// <summary>
    /// 이상물체 한 종류의 정의 에셋입니다.
    /// </summary>
    /// <remarks>
    /// SWUtils의 <see cref="SWIdentifiedObject"/>가 코드명, 표시명, 설명, 카테고리를 이미 갖고 있어 그대로 상속합니다.
    /// 여러 종류를 모아 두는 목록도 SWUtils의 <see cref="SWIODatabase"/>를 그대로 씁니다.
    /// 실체를 가리키는 식별자로는 코드명을 씁니다. <see cref="ItemInstance"/>가 이 값을 들고 다닙니다.
    /// 아이콘과 모델은 비워 둘 수 있습니다. 비어 있으면 형태와 색으로 만든 상자가 대신 나오므로
    /// 아트가 준비되기 전에도 종류를 늘려 배치 규칙을 확인할 수 있습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "Anomaly", menuName = "프로젝트R/이상물체 정의")]
    public class AnomalyDefinition : SWIdentifiedObject
    {
        #region 필드
        /// <summary>인벤토리에서 차지하는 가로 칸 수입니다.</summary>
        [SWGroup("형태")]
        [SerializeField, Min(1), Tooltip("인벤토리에서 차지하는 가로 칸 수입니다.")]
        private int shapeWidth = 1;

        /// <summary>인벤토리에서 차지하는 세로 칸 수입니다.</summary>
        [SerializeField, Min(1), Tooltip("인벤토리에서 차지하는 세로 칸 수입니다.")]
        private int shapeHeight = 1;

        /// <summary>등급입니다. 인벤토리와 정산 화면에서 표시에만 쓰입니다.</summary>
        [SWGroup("표시")]
        [SerializeField, Tooltip("등급입니다. 인벤토리와 정산 화면에서 표시에만 쓰입니다.")]
        private EAnomalyGrade grade = EAnomalyGrade.Common;

        /// <summary>인벤토리 칸에 그릴 아이콘입니다. 비우면 색만 칠합니다.</summary>
        [SerializeField, Tooltip("인벤토리 칸에 그릴 아이콘입니다. 비우면 색만 칠합니다.")]
        private Sprite icon;

        /// <summary>월드에 놓을 모델 프리팹입니다. 비우면 형태대로 만든 상자를 대신 씁니다.</summary>
        [SerializeField, Tooltip("월드에 놓을 모델 프리팹입니다. 비우면 형태대로 만든 상자를 대신 씁니다.")]
        private GameObject worldPrefab;

        /// <summary>아이콘이나 모델이 없을 때 대신 칠할 색입니다.</summary>
        [SerializeField, Tooltip("아이콘이나 모델이 없을 때 대신 칠할 색입니다.")]
        private Color displayColor = Color.white;

        /// <summary>가지고 나왔을 때 더해지는 후원금입니다.</summary>
        [SWGroup("정산")]
        [SerializeField, Min(0), Tooltip("가지고 나왔을 때 더해지는 후원금입니다.")]
        private int donationBonus = 100;

        /// <summary>가지고 나왔을 때 더해지는 시청자 수입니다.</summary>
        [SerializeField, Min(0), Tooltip("가지고 나왔을 때 더해지는 시청자 수입니다.")]
        private int viewerBonus = 20;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>실체가 이 정의를 가리킬 때 쓰는 식별자입니다.</summary>
        /// <remarks>코드명을 비워 두면 에셋 이름을 대신 씁니다.</remarks>
        public string DefinitionId => string.IsNullOrEmpty(CodeName) ? name : CodeName;

        /// <summary>인벤토리에서 차지하는 형태입니다.</summary>
        public InventoryShape Shape => new InventoryShape(shapeWidth, shapeHeight);

        /// <summary>등급입니다.</summary>
        public EAnomalyGrade Grade => grade;

        /// <summary>인벤토리 칸에 그릴 아이콘입니다. 없으면 null입니다.</summary>
        public Sprite Icon => icon;

        /// <summary>월드에 놓을 모델 프리팹입니다. 없으면 null입니다.</summary>
        public GameObject WorldPrefab => worldPrefab;

        /// <summary>아이콘이나 모델이 없을 때 대신 칠할 색입니다.</summary>
        public Color DisplayColor => displayColor;

        /// <summary>가지고 나왔을 때 더해지는 후원금입니다.</summary>
        public int DonationBonus => donationBonus;

        /// <summary>가지고 나왔을 때 더해지는 시청자 수입니다.</summary>
        public int ViewerBonus => viewerBonus;
        #endregion // 프로퍼티
    }
}
