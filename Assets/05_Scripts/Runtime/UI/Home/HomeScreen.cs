using UnityEngine;
using UnityEngine.UI;

using SW.Attributes;
using SW.Base;
using SW.Popup;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Data;
using ProjectR.UI.Settlement;

namespace ProjectR.UI.Home
{
    /// <summary>
    /// 관리 화면을 맡아 마을과 방을 오가고 활동으로 들여보내는 화면입니다.
    /// </summary>
    /// <remarks>
    /// 체크리스트 2.2절과 2.4절입니다. 관리 화면은 두 층으로 나뉩니다.
    /// 활동을 고르는 <b>마을</b>과 성장이 쌓이는 <b>방</b>입니다.
    /// <para>
    /// 둘을 씬이 아니라 루트 오브젝트 전환으로 오갑니다. 씬을 늘리면 전역 객체 네 벌과
    /// 로딩 구간이 따라붙습니다(체크리스트 2.1절).
    /// </para>
    /// <para>
    /// 활동 선택을 목록 버튼이 아니라 마을 건물로 둔 이유는 설계 원칙 5번
    /// ("성장은 눈에 보여야 한다")을 수치가 아니라 그림으로 처리하기 위해서입니다.
    /// 해금으로 건물이 늘어나면 마을이 자라는 것이 그대로 성장의 그림이 됩니다.
    /// </para>
    /// <para>
    /// 활동은 활동 씬 안에서 끝나고 곧바로 여기로 넘어오므로 종료 알림으로는 받을 수 없고,
    /// 매니저가 들고 있는 결과를 이 화면이 가져가는 방향이 됩니다.
    /// </para>
    /// </remarks>
    public class HomeScreen : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("화면")]
        [SerializeField, Tooltip("마을 화면의 루트 오브젝트입니다.")]
        private GameObject villageRoot;

        [SerializeField, Tooltip("방 화면의 루트 오브젝트입니다.")]
        private GameObject roomRoot;

        [SWGroup("건물")]
        [SerializeField, Tooltip("마을에 서 있는 건물들입니다. 씬에 놓인 순서대로 넣습니다.")]
        private BuildingView[] buildings;

        [SWGroup("표시")]
        [SerializeField, Tooltip("날짜와 남은 방송 시간을 적을 글상자입니다.")]
        private Text statusText;

        [SerializeField, Tooltip("보유 재화를 적을 글상자입니다.")]
        private Text walletText;

        [SerializeField, Tooltip("마우스를 올린 건물의 안내와 들어갈 수 없는 이유를 적을 글상자입니다.")]
        private Text noticeText;

        [SWGroup("버튼")]
        [SerializeField, Tooltip("하루를 마감하고 다음 날로 넘기는 버튼입니다.")]
        private Button endDayButton;

        [SerializeField, Tooltip("방에서 마을로 돌아가는 버튼입니다.")]
        private Button leaveRoomButton;

        /// <summary>마우스가 올라와 있는 건물입니다. 없으면 null입니다.</summary>
        private BuildingView hoveredBuilding;

        /// <summary>화면에 마지막으로 적은 안내입니다.</summary>
        private string lastNotice;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 건물과 버튼의 알림을 이어 붙입니다.
        /// </summary>
        private void Awake()
        {
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildings[i] == null) continue;

                buildings[i].ApplyDefinition();
                buildings[i].Clicked += HandleBuildingClicked;
                buildings[i].HoverChanged += HandleBuildingHoverChanged;
            }

            if (endDayButton != null) endDayButton.onClick.AddListener(HandleEndDayClicked);
            if (leaveRoomButton != null) leaveRoomButton.onClick.AddListener(ShowVillage);
        }

        /// <summary>
        /// 이어 붙인 알림을 떼어 냅니다.
        /// </summary>
        private void OnDestroy()
        {
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildings[i] == null) continue;

                buildings[i].Clicked -= HandleBuildingClicked;
                buildings[i].HoverChanged -= HandleBuildingHoverChanged;
            }

            if (endDayButton != null) endDayButton.onClick.RemoveListener(HandleEndDayClicked);
            if (leaveRoomButton != null) leaveRoomButton.onClick.RemoveListener(ShowVillage);
        }

        /// <summary>
        /// 화면에 들어올 때 마을을 보여 주고 상태를 채웁니다.
        /// </summary>
        private void Start()
        {
            ShowVillage();
            ShowPendingSettlement();
            Refresh();
        }

        /// <summary>
        /// 아직 보여 주지 않은 활동 결과가 있으면 정산 화면을 띄웁니다.
        /// </summary>
        private void ShowPendingSettlement()
        {
            if (GameManager.Instance.PendingResult == null) return;

            ActivityResult result = GameManager.Instance.ConsumePendingResult();

            SWPopupManager.Instance.Show<SettlementPopup>(PopupKeys.Settlement, popup => popup.Bind(result));
        }

        /// <summary>
        /// 마을 화면을 보여 줍니다.
        /// </summary>
        public void ShowVillage()
        {
            SetLayer(isRoom: false);

            Refresh();
        }

        /// <summary>
        /// 방 화면을 보여 줍니다.
        /// </summary>
        /// <remarks>
        /// 방은 성장이 공간으로 보이는 자리입니다(기획서 9.3절).
        /// 시그널 트로피와 방의 변화는 그 정의가 생기는 M2에서 붙입니다.
        /// </remarks>
        public void ShowRoom()
        {
            SetLayer(isRoom: true);
        }

        /// <summary>
        /// 마을과 방 중 어느 층을 보여 줄지 정하고, 그 층에서만 쓰는 버튼을 함께 켜고 끕니다.
        /// </summary>
        /// <param name="isRoom">방을 보여 주려면 true입니다.</param>
        /// <remarks>
        /// 루트 오브젝트만 바꾸면 Canvas는 화면 위에 그대로 떠 있습니다.
        /// 마을에서 "마을로 나가기"가, 방에서 "하루 마감"이 보이는 것은 둘 다 말이 되지 않습니다.
        /// <para>
        /// <b><c>?.</c>를 쓰지 않습니다.</b> 널 조건 연산자는 C#의 널만 보고 유니티가 덮어쓴
        /// <c>==</c> 연산자를 건너뜁니다. 인스펙터에서 비워 둔 필드는 C#으로는 널이 아니라
        /// "아무것도 가리키지 않는 껍데기"라서, <c>?.</c>가 그대로 통과시키고
        /// <c>UnassignedReferenceException</c>이 납니다. 실제로 그렇게 터졌습니다(진행기록 18.3절).
        /// </para>
        /// </remarks>
        private void SetLayer(bool isRoom)
        {
            if (villageRoot != null) villageRoot.SetActive(isRoom == false);
            if (roomRoot != null) roomRoot.SetActive(isRoom);

            if (endDayButton != null) endDayButton.gameObject.SetActive(isRoom == false);
            if (leaveRoomButton != null) leaveRoomButton.gameObject.SetActive(isRoom);

            if (isRoom == false) return;

            hoveredBuilding = null;
            RefreshNotice();
        }

        /// <summary>
        /// 상태 표시와 건물의 겉모습을 지금 상태에 맞춥니다.
        /// </summary>
        /// <remarks>
        /// 상태가 바뀌는 자리가 늘어나 매 프레임 갱신을 그만두었습니다.
        /// 지금은 화면에 들어올 때, 건물을 누른 뒤, 하루를 마감한 뒤에만 다시 그립니다.
        /// </remarks>
        private void Refresh()
        {
            GameState state = GameManager.Instance.State;

            if (statusText != null)
                statusText.text = $"{state.Day}일차   방송 시간 " +
                    $"{state.RemainingBroadcastMinutes} / {state.DailyBroadcastMinutes}분";

            if (walletText != null)
                walletText.text = $"후원금 {state.Donation}   시청자 {state.ViewerCount}   " +
                    $"이상물체 {state.Items.Count}개";

            for (int i = 0; i < buildings.Length; i++)
                if (buildings[i] != null) buildings[i].Refresh(state);

            if (endDayButton != null) endDayButton.interactable = GameManager.Instance.IsActivityRunning == false;

            RefreshNotice();
        }

        /// <summary>
        /// 마우스를 올린 건물에 맞춰 안내를 다시 적습니다.
        /// </summary>
        private void RefreshNotice()
        {
            if (noticeText == null) return;

            string notice = BuildNotice();

            if (notice == lastNotice) return;

            lastNotice = notice;
            noticeText.text = notice;
        }

        /// <summary>
        /// 지금 적어야 할 안내 문구를 만듭니다.
        /// </summary>
        /// <returns>적을 안내 문구입니다. 적을 것이 없으면 빈 문자열입니다.</returns>
        /// <remarks>
        /// 건물 위에 붙는 안내는 <b>방송 시간 비용과 그 건물이 무엇을 바꾸는지</b> 둘뿐입니다(체크리스트 2.2절).
        /// 들어갈 수 없으면 그 자리에 이유를 대신 적습니다. 버튼을 끄기만 하면 왜 안 되는지 알 수 없습니다.
        /// </remarks>
        private string BuildNotice()
        {
            if (hoveredBuilding == null || hoveredBuilding.Definition == null) return string.Empty;

            BuildingDefinition definition = hoveredBuilding.Definition;
            string blocked = definition.GetBlockedReason(GameManager.Instance.State);

            if (string.IsNullOrEmpty(blocked) == false)
                return $"{definition.DisplayName} — {blocked}";

            string cost = definition.HasActivity ? $"방송 시간 {definition.BroadcastCost}분" : "시간이 들지 않습니다";

            return $"{definition.DisplayName} — {cost}   {definition.EffectNotice}";
        }

        /// <summary>
        /// 건물이 눌렸을 때 활동을 시작하거나 방으로 들어갑니다.
        /// </summary>
        /// <param name="building">눌린 건물입니다.</param>
        /// <remarks>
        /// 여기 있는 분기는 "활동인가 아닌가" 하나뿐입니다. 건물 종류를 보고 나누지 않습니다.
        /// 활동을 하나 늘리는 것은 <see cref="IActivityFactory"/> 구현을 하나 더 만들고
        /// 정의 에셋에서 고르는 일로 끝납니다.
        /// </remarks>
        private void HandleBuildingClicked(BuildingView building)
        {
            BuildingDefinition definition = building.Definition;
            string blocked = definition.GetBlockedReason(GameManager.Instance.State);

            if (string.IsNullOrEmpty(blocked) == false)
            {
                SWLog.Log($"[{nameof(HomeScreen)}] {definition.DisplayName}에 들어가지 못했습니다: {blocked}");
                RefreshNotice();
                return;
            }

            if (definition.OpensRoom)
            {
                ShowRoom();
                return;
            }

            if (GameManager.Instance.BeginActivity(definition.CreateActivity()) == false)
                SWLog.LogWarning($"[{nameof(HomeScreen)}] 활동을 시작하지 못했습니다: {definition.DisplayName}");
        }

        /// <summary>
        /// 마우스가 건물에 올라오거나 벗어났을 때 안내를 바꿉니다.
        /// </summary>
        /// <param name="building">마우스가 오간 건물입니다.</param>
        /// <param name="isHovered">올라왔으면 true입니다.</param>
        private void HandleBuildingHoverChanged(BuildingView building, bool isHovered)
        {
            if (isHovered) hoveredBuilding = building;
            else if (hoveredBuilding == building) hoveredBuilding = null;

            RefreshNotice();
        }

        /// <summary>
        /// 하루를 마감하고 다음 날로 넘깁니다.
        /// </summary>
        private void HandleEndDayClicked()
        {
            DayEndResult result = GameManager.Instance.EndDay();

            SWLog.Log($"[{nameof(HomeScreen)}] 하루를 마감했습니다. 유지비 {result.UpkeepCost} / " +
                $"이탈 {result.ViewerLoss}명");

            Refresh();
        }
        #endregion // 함수
    }
}
