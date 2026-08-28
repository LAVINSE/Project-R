using UnityEngine;
using UnityEngine.UI;

using SW.Attributes;
using SW.Base;
using SW.Util;

using SW.Popup;

using ProjectR.Activity;
using ProjectR.Backrooms;
using ProjectR.Data;
using ProjectR.UI.Settlement;

namespace ProjectR.UI.Home
{
    /// <summary>
    /// 관리 화면에서 오늘 상태를 보여 주고 활동으로 들어가는 화면입니다.
    /// </summary>
    /// <remarks>
    /// 지금까지는 디버그 콘솔로만 백룸에 들어갈 수 있었습니다.
    /// 들어가는 문이 콘솔에만 있으면 하루가 어떻게 도는지를 개발자만 볼 수 있습니다.
    /// 활동이 늘어나면 버튼도 늘어나므로, 이 화면은 활동 목록을 놓는 자리가 됩니다.
    /// 탐험에서 돌아왔을 때 정산 화면을 띄우는 것도 여기서 합니다.
    /// 활동은 활동 씬 안에서 끝나고 곧바로 여기로 넘어오므로 종료 알림으로는 받을 수 없고,
    /// 매니저가 들고 있는 결과를 관리 화면이 가져가는 방향이 됩니다.
    /// </remarks>
    public class HomeScreen : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField, Tooltip("날짜와 남은 방송 시간을 적을 글상자입니다.")]
        private Text statusText;

        [SerializeField, Tooltip("보유 재화를 적을 글상자입니다.")]
        private Text walletText;

        [SerializeField, Tooltip("버튼을 누를 수 없는 이유를 적을 글상자입니다.")]
        private Text noticeText;

        [SWGroup("버튼")]
        [SerializeField, Tooltip("백룸 탐험으로 들어가는 버튼입니다.")]
        private Button enterBackroomsButton;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 버튼의 콜백을 이어 붙입니다.
        /// </summary>
        private void Awake()
        {
            enterBackroomsButton?.onClick.AddListener(HandleEnterBackroomsClicked);
        }

        /// <summary>
        /// 이어 붙인 콜백을 떼어 냅니다.
        /// </summary>
        private void OnDestroy()
        {
            enterBackroomsButton?.onClick.RemoveListener(HandleEnterBackroomsClicked);
        }

        /// <summary>
        /// 화면에 들어올 때 상태를 채웁니다.
        /// </summary>
        private void Start()
        {
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
        /// 정산 화면을 닫고 돌아왔을 때처럼 상태가 달라졌을 수 있으므로 다시 채웁니다.
        /// </summary>
        /// <remarks>
        /// 관리 화면에서 상태를 바꾸는 것은 아직 버튼 하나뿐이라 매 프레임 갱신해도 부담이 없습니다.
        /// 바꾸는 곳이 늘어나면 그때 알림을 받는 방식으로 바꿉니다.
        /// </remarks>
        private void Update()
        {
            Refresh();
        }

        /// <summary>
        /// 오늘 상태와 버튼을 누를 수 있는지를 화면에 반영합니다.
        /// </summary>
        private void Refresh()
        {
            GameManager manager = GameManager.Instance;
            int cost = new BackroomsActivity().BroadcastCost;
            bool canEnter = manager.IsActivityRunning == false
                && cost <= manager.State.RemainingBroadcastMinutes;

            if (statusText != null)
                statusText.text = $"{manager.State.Day}일차   방송 시간 " +
                    $"{manager.State.RemainingBroadcastMinutes} / {manager.BroadcastMinutesPerDay}분";

            if (walletText != null)
                walletText.text = $"후원금 {manager.State.Donation}   시청자 {manager.State.ViewerCount}   " +
                    $"이상물체 {manager.State.Items.Count}개";

            if (noticeText != null)
                noticeText.text = canEnter ? string.Empty : "오늘 남은 방송 시간이 부족합니다";

            if (enterBackroomsButton != null) enterBackroomsButton.interactable = canEnter;
        }

        /// <summary>
        /// 백룸 탐험을 시작합니다.
        /// </summary>
        private void HandleEnterBackroomsClicked()
        {
            if (GameManager.Instance.BeginActivity(new BackroomsActivity())) return;

            SWLog.LogWarning($"[{nameof(HomeScreen)}] 백룸 탐험을 시작하지 못했습니다.");
        }
        #endregion // 함수
    }
}
