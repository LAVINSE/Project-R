using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;

using SW.Attributes;
using SW.Debugging;
using SW.Popup;
using SW.Util;

namespace ProjectR.UI.Popup
{
    /// <summary>
    /// 정해진 키로 팝업을 열고 닫는 공용 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 팝업마다 컨트롤러를 하나씩 만들면 팝업이 늘어날 때마다 똑같은 코드가 하나씩 늘어납니다.
    /// 키와 팝업만 다르고 하는 일은 같으므로 목록으로 받아 한 컴포넌트가 전부 처리합니다.
    /// 팝업은 프리팹이 아니라 <see cref="SWPopupCatalog"/>에 등록된 키로 부릅니다.
    /// 그래야 이 컴포넌트가 프리팹 참조를 들고 다니지 않아 전역으로 하나만 두어도 됩니다.
    /// 키를 PlayerInputReader가 아니라 여기서 직접 읽는 이유는,
    /// 그쪽이 팝업이 열려 있는 동안 입력을 통째로 막아 한 번 연 팝업을 닫을 수 없게 되기 때문입니다.
    /// 씬마다 하나씩 놓아 두어도 되도록 <see cref="SWSingleton{T}"/>를 씁니다.
    /// 먼저 만들어진 것만 씬을 넘어 살아남고, 다음 씬에 놓인 것은 스스로 사라집니다.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "ProjectR.UI", sourceAssembly: "ProjectR.UI", sourceClassName: "PopupHotkeyController")]
    public class PopupHotkeyController : SWSingleton<PopupHotkeyController>
    {
        #region 데이터
        /// <summary>
        /// 키 하나와 그 키로 여는 팝업 하나의 짝입니다.
        /// </summary>
        [Serializable]
        public class Entry
        {
            #region 필드
            /// <summary>팝업을 열고 닫을 키입니다.</summary>
            [SerializeField, Tooltip("팝업을 열고 닫을 키입니다.")]
            private Key key = Key.Escape;

            /// <summary>이 키로 열 팝업의 카탈로그 키입니다.</summary>
            [SerializeField, Tooltip("이 키로 열 팝업의 카탈로그 키입니다.")]
            private string popupKey = PopupKeys.Options;
            #endregion // 필드

            #region 프로퍼티
            /// <summary>팝업을 열고 닫을 키입니다.</summary>
            public Key Key => key;

            /// <summary>이 키로 열 팝업의 카탈로그 키입니다.</summary>
            public string PopupKey => popupKey;

            /// <summary>이 짝으로 지금 열어 둔 팝업입니다. 닫혀 있으면 null입니다.</summary>
            public SWPopupBase OpenedPopup { get; set; }
            #endregion // 프로퍼티
        }
        #endregion // 데이터

        #region 필드
        /// <summary>키와 팝업 프리팹의 짝 목록입니다.</summary>
        [SWGroup("팝업")]
        [SerializeField, Tooltip("키와 팝업 프리팹의 짝 목록입니다.")]
        private List<Entry> entries = new();
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 팝업이 닫혔다는 알림을 구독합니다.
        /// </summary>
        /// <remarks>팝업 안의 닫기 버튼으로 닫은 경우에도 열림 여부를 맞춰 두어야 키가 한 번에 다시 엽니다.</remarks>
        private void OnEnable()
        {
            SWPopupManager.Instance.PopupHidden += HandlePopupHidden;
        }

        /// <summary>
        /// 팝업이 닫혔다는 알림 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (SWPopupManager.HasInstance == false) return;

            SWPopupManager.Instance.PopupHidden -= HandlePopupHidden;
        }

        /// <summary>
        /// 열어 둔 팝업이 닫혔으면 열림 여부를 지웁니다.
        /// </summary>
        /// <param name="popup">방금 닫힌 팝업입니다.</param>
        private void HandlePopupHidden(SWPopupBase popup)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (ReferenceEquals(entries[index].OpenedPopup, popup) == false) continue;

                entries[index].OpenedPopup = null;
                return;
            }
        }

        /// <summary>
        /// 등록해 둔 키가 눌렸는지 확인합니다.
        /// </summary>
        private void Update()
        {
            if (Keyboard.current == null) return;

            // 디버그 콘솔이 열려 있으면 키는 콘솔 쪽 몫입니다.
            if (SWDebugConsole.IsOpen) return;

            for (int index = 0; index < entries.Count; index++)
            {
                if (Keyboard.current[entries[index].Key].wasPressedThisFrame == false) continue;

                Toggle(entries[index]);
                return;
            }
        }

        /// <summary>
        /// 지정한 키의 팝업을 열거나 닫습니다.
        /// </summary>
        /// <param name="key">열고 닫을 팝업의 키입니다.</param>
        public void Toggle(Key key)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].Key != key) continue;

                Toggle(entries[index]);
                return;
            }

            SWLog.LogWarning($"[{nameof(PopupHotkeyController)}] {key}에 등록된 팝업이 없습니다.");
        }

        /// <summary>
        /// 짝 하나의 팝업을 열거나 닫습니다.
        /// </summary>
        /// <param name="entry">열고 닫을 짝입니다.</param>
        private void Toggle(Entry entry)
        {
            if (entry.OpenedPopup != null)
            {
                SWPopupManager.Instance.Hide(entry.OpenedPopup);
                entry.OpenedPopup = null;
                return;
            }

            if (string.IsNullOrEmpty(entry.PopupKey))
            {
                SWLog.LogError($"[{nameof(PopupHotkeyController)}] {entry.Key}에 걸린 팝업 키가 비어 있습니다.");
                return;
            }

            entry.OpenedPopup = SWPopupManager.Instance.Show(entry.PopupKey);
        }
        #endregion // 함수
    }
}
