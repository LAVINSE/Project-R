using System;

using UnityEngine;
using UnityEngine.EventSystems;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Data;

namespace ProjectR.UI.Home
{
    /// <summary>
    /// 마을에 서 있는 건물 하나를 맡아 클릭과 마우스오버를 받는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 마을은 위에서 비스듬히 내려다보는 3D 공간입니다(기획서 12.7절).
    /// 건물 모델은 이 오브젝트의 자식으로 놓이고, 이 컴포넌트는 <b>모델을 만들지 않습니다.</b>
    /// 좌표와 겉모습은 씬에 두기로 했기 때문입니다(진행기록 17.1절).
    /// 정의 에셋이 아는 것은 "무엇을 하는 건물인가"뿐입니다.
    /// <para>
    /// 클릭은 카메라에 붙인 <c>PhysicsRaycaster</c>가 처리하므로,
    /// <b>이미 씬에 있는 <c>EventSystem</c>이 월드 건물과 Canvas 팝업을 같은 입력 경로로 다룹니다.</b>
    /// 건물에는 콜라이더가 하나 있으면 됩니다. 모델 조각마다 콜라이더를 달면
    /// 조각 사이의 틈으로 클릭이 새고, 조각을 바꿀 때마다 판정이 달라집니다.
    /// </para>
    /// <para>
    /// <b>들어갈 수 없는 상태를 재질로 알리지 않습니다.</b> 처음에는 <see cref="MaterialPropertyBlock"/>으로
    /// 색을 덮어썼는데, 셰이더마다 색 속성 이름이 달라 두 번 연달아 건물이 통째로 뭉갰습니다(진행기록 18.4절).
    /// 틀린 이름을 써도 예외가 나지 않고 화면만 이상해지므로 원인을 찾기도 어렵습니다.
    /// 이 마을은 에셋 팩에서 가져온 재질을 쓰고 앞으로도 늘어날 것이라,
    /// 재질에 손대지 않는 방법이 유일하게 안전합니다.
    /// 대신 <b>건물을 비추는 강조등</b>을 끄고 켭니다. 어떤 셰이더를 쓰든 똑같이 동작하고,
    /// 네온 도시라는 배경에도 재질을 눌러 납작하게 만드는 것보다 잘 맞습니다.
    /// </para>
    /// <para>
    /// 이 컴포넌트는 <b>무엇을 할지 정하지 않습니다.</b> 눌렸다는 것만 알립니다.
    /// 활동을 시작할지 방으로 들어갈지는 마을 화면이 정합니다.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public class BuildingView : SWMonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        #region 상수
        /// <summary>들어갈 수 없을 때 강조등의 세기에 곱할 값입니다.</summary>
        private const float LockedLightScale = 0.15f;

        /// <summary>마우스를 올렸을 때 강조등의 세기에 곱할 값입니다.</summary>
        private const float HoverLightScale = 1.7f;
        #endregion // 상수

        #region 필드
        [SWGroup("정의")]
        [SerializeField, Tooltip("이 자리에 세울 건물의 정의입니다.")]
        private BuildingDefinition definition;

        [SWGroup("표시")]
        [SerializeField, Tooltip("건물의 모델이 담긴 뿌리입니다. 지금은 표시용 참고 값입니다.")]
        private Transform modelRoot;

        [SerializeField, Tooltip("건물 위에 이름과 비용을 적을 글상자입니다.")]
        private UnityEngine.UI.Text label;

        [SerializeField, Tooltip("이 건물을 비추는 강조등입니다. 들어갈 수 없으면 꺼지고 마우스를 올리면 밝아집니다.")]
        private Light accentLight;

        /// <summary>강조등의 원래 세기입니다. 곱하기 전의 기준값입니다.</summary>
        private float accentBaseIntensity;

        /// <summary>마우스가 올라와 있는지 여부입니다.</summary>
        private bool isHovered;

        /// <summary>지금 들어갈 수 있는지 여부입니다.</summary>
        private bool isEnterable;
        #endregion // 필드

        #region 이벤트
        /// <summary>건물이 눌렸을 때 발생합니다.</summary>
        /// <remarks>들어갈 수 없는 건물이 눌려도 발생합니다. 이유를 알려 주는 것도 마을 화면의 몫입니다.</remarks>
        public event Action<BuildingView> Clicked;

        /// <summary>마우스가 올라오거나 벗어났을 때 발생합니다.</summary>
        public event Action<BuildingView, bool> HoverChanged;
        #endregion // 이벤트

        #region 프로퍼티
        /// <summary>이 자리에 세운 건물의 정의입니다.</summary>
        public BuildingDefinition Definition => definition;

        /// <summary>지금 들어갈 수 있는지 여부입니다.</summary>
        public bool IsEnterable => isEnterable;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 강조등의 원래 세기를 기억해 둡니다.
        /// </summary>
        /// <remarks>원래 세기를 곱해 쓰므로 건물마다 다른 밝기가 그대로 유지됩니다.</remarks>
        private void Awake()
        {
            if (accentLight != null) accentBaseIntensity = accentLight.intensity;
        }

        /// <summary>
        /// 정의에 적힌 것을 오브젝트에 옮깁니다.
        /// </summary>
        /// <remarks>이름만 맞춰 둡니다. 모델과 좌표는 씬에 있는 것을 그대로 씁니다.</remarks>
        public void ApplyDefinition()
        {
            if (definition == null) return;

            gameObject.name = $"Building_{definition.DefinitionId}";
        }

        /// <summary>
        /// 지금 상태를 보고 들어갈 수 있는지 정하고 겉모습에 반영합니다.
        /// </summary>
        /// <param name="state">확인에 쓸 게임 상태입니다.</param>
        public void Refresh(GameState state)
        {
            if (definition == null) return;

            isEnterable = string.IsNullOrEmpty(definition.GetBlockedReason(state));

            ApplyHighlight();
            ApplyLabel();
        }

        /// <summary>
        /// 들어갈 수 있는지와 마우스오버 상태를 강조등의 밝기로 나타냅니다.
        /// </summary>
        /// <remarks>
        /// 들어갈 수 없는 건물을 지우거나 끄지 않고 불만 죽입니다.
        /// 없어지면 무엇이 열릴 예정인지 알 수 없고, 마을이 자라는 것도 보이지 않습니다.
        /// 불이 꺼진 건물이 늘어서 있는 것 자체가 "아직 열리지 않았다"를 말해 줍니다.
        /// </remarks>
        private void ApplyHighlight()
        {
            if (accentLight == null) return;

            float scale = isEnterable == false ? LockedLightScale
                : isHovered ? HoverLightScale
                : 1f;

            accentLight.intensity = accentBaseIntensity * scale;
        }

        /// <summary>
        /// 건물 위에 이름과 안내를 적습니다.
        /// </summary>
        /// <remarks>
        /// 적는 것은 <b>방송 시간 비용과 이 건물이 무엇을 바꾸는지</b> 둘뿐입니다(체크리스트 2.2절).
        /// 들어갈 수 없는 이유는 여기 적지 않습니다. 건물마다 이유를 달고 있으면
        /// 마을이 경고문 판이 되므로, 이유는 마우스를 올렸을 때 화면 아래 한 줄에만 보여 줍니다.
        /// </remarks>
        private void ApplyLabel()
        {
            if (label == null) return;

            string cost = definition.HasActivity ? $"{definition.BroadcastCost}분" : string.Empty;
            string effect = definition.EffectNotice;

            string detail = string.IsNullOrEmpty(cost) ? effect
                : string.IsNullOrEmpty(effect) ? cost
                : $"{cost} · {effect}";

            label.text = string.IsNullOrEmpty(detail)
                ? definition.DisplayName
                : $"{definition.DisplayName}\n{detail}";

            label.color = isEnterable
                ? new Color(0.95f, 0.95f, 1f, 1f)
                : new Color(0.5f, 0.5f, 0.58f, 1f);
        }

        /// <summary>
        /// 건물이 눌렸음을 알립니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 정보입니다.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (definition == null)
            {
                SWLog.LogWarning($"[{nameof(BuildingView)}] 건물 정의가 비어 있어 무시합니다: {name}");
                return;
            }

            Clicked?.Invoke(this);
        }

        /// <summary>
        /// 마우스가 올라왔음을 알립니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 정보입니다.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;

            ApplyHighlight();
            HoverChanged?.Invoke(this, true);
        }

        /// <summary>
        /// 마우스가 벗어났음을 알립니다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 정보입니다.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;

            ApplyHighlight();
            HoverChanged?.Invoke(this, false);
        }
        #endregion // 함수
    }
}
