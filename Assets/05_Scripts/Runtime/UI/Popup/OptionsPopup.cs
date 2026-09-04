using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

using SW.Attributes;
using SW.Popup;
using SW.Util;

using ProjectR.Core;

namespace ProjectR.UI.Popup
{
    /// <summary>
    /// ESC로 여는 옵션 팝업입니다. 음량과 마우스 감도를 고치고 게임을 끌 수 있습니다.
    /// </summary>
    /// <remarks>
    /// 값의 원본은 <see cref="GameSettings"/>가 들고 있으므로 이 팝업은 읽어서 보여 주고 고쳐 넘기기만 합니다.
    /// 팝업이 열려 있는 동안 조작 입력이 막히는 것은 PlayerInputReader가 팝업 개수를 보고 알아서 처리합니다.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "ProjectR.UI", sourceAssembly: "ProjectR.UI", sourceClassName: "OptionsPopup")]
    public class OptionsPopup : SWPopupBase
    {
        #region 필드
        /// <summary>전체 음량을 고칠 슬라이더입니다.</summary>
        [SWGroup("음량")]
        [SerializeField, Tooltip("전체 음량을 고칠 슬라이더입니다.")]
        private Slider masterVolumeSlider;

        /// <summary>소리를 켜고 끌 토글입니다.</summary>
        [SerializeField, Tooltip("소리를 켜고 끌 토글입니다.")]
        private Toggle muteToggle;

        /// <summary>마우스 감도를 고칠 슬라이더입니다.</summary>
        [SWGroup("조작")]
        [SerializeField, Tooltip("마우스 감도를 고칠 슬라이더입니다.")]
        private Slider mouseSensitivitySlider;

        /// <summary>팝업을 닫는 버튼입니다.</summary>
        [SWGroup("버튼")]
        [SerializeField, Tooltip("팝업을 닫는 버튼입니다.")]
        private Button closeButton;

        /// <summary>게임을 끄는 버튼입니다.</summary>
        [SerializeField, Tooltip("게임을 끄는 버튼입니다.")]
        private Button quitButton;

        /// <summary>값을 되돌려 넣는 동안 다시 저장하지 않도록 막는 표시입니다.</summary>
        private bool isRefreshing;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 조작 UI의 콜백을 이어 붙입니다.
        /// </summary>
        private void Awake()
        {
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.minValue = GameSettings.MinimumMouseSensitivity;
                mouseSensitivitySlider.maxValue = GameSettings.MaximumMouseSensitivity;
            }

            masterVolumeSlider?.onValueChanged.AddListener(HandleMasterVolumeChanged);
            muteToggle?.onValueChanged.AddListener(HandleMuteChanged);
            mouseSensitivitySlider?.onValueChanged.AddListener(HandleMouseSensitivityChanged);
            closeButton?.onClick.AddListener(HandleCloseClicked);
            quitButton?.onClick.AddListener(HandleQuitClicked);
        }

        /// <summary>
        /// 이어 붙인 콜백을 떼어 냅니다.
        /// </summary>
        private void OnDestroy()
        {
            masterVolumeSlider?.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
            muteToggle?.onValueChanged.RemoveListener(HandleMuteChanged);
            mouseSensitivitySlider?.onValueChanged.RemoveListener(HandleMouseSensitivityChanged);
            closeButton?.onClick.RemoveListener(HandleCloseClicked);
            quitButton?.onClick.RemoveListener(HandleQuitClicked);
        }

        /// <summary>
        /// 팝업이 열릴 때 저장해 둔 값을 화면에 채웁니다.
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();
            Refresh();
        }

        /// <summary>
        /// 저장해 둔 설정값을 조작 UI에 되돌려 넣습니다.
        /// </summary>
        private void Refresh()
        {
            GameSettings.EnsureLoaded();

            isRefreshing = true;

            if (masterVolumeSlider != null) masterVolumeSlider.value = GameSettings.MasterVolume;
            if (muteToggle != null) muteToggle.isOn = GameSettings.IsMuted;
            if (mouseSensitivitySlider != null) mouseSensitivitySlider.value = GameSettings.MouseSensitivity;

            isRefreshing = false;
        }

        /// <summary>
        /// 슬라이더에서 고친 전체 음량을 반영합니다.
        /// </summary>
        /// <param name="value">고친 전체 음량입니다.</param>
        private void HandleMasterVolumeChanged(float value)
        {
            if (isRefreshing) return;

            GameSettings.SetMasterVolume(value);
        }

        /// <summary>
        /// 토글에서 고친 음소거 여부를 반영합니다.
        /// </summary>
        /// <param name="isMuted">소리를 끌지 여부입니다.</param>
        private void HandleMuteChanged(bool isMuted)
        {
            if (isRefreshing) return;

            GameSettings.SetMuted(isMuted);
        }

        /// <summary>
        /// 슬라이더에서 고친 마우스 감도를 반영합니다.
        /// </summary>
        /// <param name="value">고친 마우스 감도입니다.</param>
        private void HandleMouseSensitivityChanged(float value)
        {
            if (isRefreshing) return;

            GameSettings.SetMouseSensitivity(value);
        }

        /// <summary>
        /// 닫기 버튼을 눌렀을 때 팝업을 닫습니다.
        /// </summary>
        private void HandleCloseClicked()
        {
            SWPopupManager.Instance.Hide(this);
        }

        /// <summary>
        /// 게임 종료 버튼을 눌렀을 때 게임을 끕니다.
        /// </summary>
        /// <remarks>에디터에서는 실행 파일을 끌 수 없으므로 플레이 모드를 멈추는 것으로 대신합니다.</remarks>
        private void HandleQuitClicked()
        {
            SWLog.Log($"[{nameof(OptionsPopup)}] 게임을 종료합니다.");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        #endregion // 함수
    }
}
