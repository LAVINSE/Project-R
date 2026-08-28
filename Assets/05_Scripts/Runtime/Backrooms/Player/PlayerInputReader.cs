using UnityEngine;
using UnityEngine.InputSystem;

using SW.Attributes;
using SW.Base;
using SW.Debugging;
using SW.Popup;
using SW.Util;

namespace ProjectR.Backrooms.Player
{
    /// <summary>
    /// 플레이어 입력을 한곳에서 읽어 다른 컴포넌트에 넘겨 주는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 이동, 시점, 손전등이 각자 입력 액션을 켜고 끄면 같은 액션 맵을 여러 번 켜게 되어
    /// 한쪽이 꺼질 때 다른 쪽 입력까지 죽습니다. 그래서 액션 맵을 켜고 끄는 책임을 이 컴포넌트가 혼자 집니다.
    /// 디버그 콘솔이나 팝업이 열려 있는 동안 입력을 막고 커서를 풀어 주는 것도 여기서 처리합니다.
    /// </remarks>
    public class PlayerInputReader : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("입력")]
        [SerializeField, Tooltip("플레이어 입력을 읽을 입력 액션 에셋입니다.")]
        private InputActionAsset inputActions;

        [SerializeField, Tooltip("사용할 액션 맵의 이름입니다.")]
        private string actionMapName = "Player";

        [SWGroup("커서")]
        [SerializeField, Tooltip("켜면 조작 중에 커서를 화면 가운데에 가둡니다.")]
        private bool lockCursor = true;

        /// <summary>사용 중인 액션 맵입니다.</summary>
        private InputActionMap actionMap;

        /// <summary>이동 입력 액션입니다.</summary>
        private InputAction moveAction;

        /// <summary>시점 입력 액션입니다.</summary>
        private InputAction lookAction;

        /// <summary>달리기 입력 액션입니다.</summary>
        private InputAction sprintAction;

        /// <summary>앉기 입력 액션입니다.</summary>
        private InputAction crouchAction;

        /// <summary>손전등 입력 액션입니다.</summary>
        private InputAction flashlightAction;

        /// <summary>줍기 입력 액션입니다.</summary>
        private InputAction interactAction;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이동 입력입니다. x가 좌우, y가 앞뒤입니다.</summary>
        public Vector2 Move { get; private set; }

        /// <summary>시점 입력입니다. 이번 프레임에 들어온 변화량입니다.</summary>
        public Vector2 Look { get; private set; }

        /// <summary>달리기 버튼을 누르고 있는지 여부입니다.</summary>
        public bool IsSprintHeld { get; private set; }

        /// <summary>앉기 버튼을 누르고 있는지 여부입니다.</summary>
        public bool IsCrouchHeld { get; private set; }

        /// <summary>이번 프레임에 손전등 버튼을 눌렀는지 여부입니다.</summary>
        public bool IsFlashlightPressed { get; private set; }

        /// <summary>이번 프레임에 줍기 버튼을 눌렀는지 여부입니다.</summary>
        public bool IsInteractPressed { get; private set; }

        /// <summary>UI가 조작을 잡고 있는지 여부입니다.</summary>
        /// <remarks>
        /// 가방을 정리하는 동안처럼 팝업이 아닌 화면이 마우스를 써야 할 때 UI 쪽에서 켜고 끕니다.
        /// 상태를 UI가 아니라 여기에 두는 이유는, 입력을 막고 커서를 푸는 책임이 원래 이 컴포넌트에 있기 때문입니다.
        /// </remarks>
        public bool HasUiFocus { get; private set; }

        /// <summary>입력을 막고 있는지 여부입니다. 디버그 콘솔, 팝업, UI 조작 중이면 true입니다.</summary>
        public bool IsInputBlocked => SWDebugConsole.IsOpen || IsPopupOpen || HasUiFocus;

        /// <summary>팝업이 하나라도 열려 있는지 여부입니다.</summary>
        private static bool IsPopupOpen =>
            SWPopupManager.HasInstance && SWPopupManager.Instance.ActivePopupCount > 0;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 액션 맵과 각 액션을 찾아 둡니다.
        /// </summary>
        private void Awake()
        {
            if (inputActions == null)
            {
                SWLog.LogError($"[{nameof(PlayerInputReader)}] 입력 액션 에셋이 비어 있어 조작할 수 없습니다.");
                return;
            }

            actionMap = inputActions.FindActionMap(actionMapName, false);

            if (actionMap == null)
            {
                SWLog.LogError($"[{nameof(PlayerInputReader)}] 액션 맵을 찾지 못했습니다: {actionMapName}");
                return;
            }

            moveAction = actionMap.FindAction("Move", false);
            lookAction = actionMap.FindAction("Look", false);
            sprintAction = actionMap.FindAction("Sprint", false);
            crouchAction = actionMap.FindAction("Crouch", false);
            flashlightAction = actionMap.FindAction("Flashlight", false);
            interactAction = actionMap.FindAction("Interact", false);
        }

        /// <summary>
        /// 액션 맵을 켜고 커서를 가둡니다.
        /// </summary>
        private void OnEnable()
        {
            actionMap?.Enable();
            ApplyCursorState(true);
        }

        /// <summary>
        /// 액션 맵을 끄고 커서를 풀어 줍니다.
        /// </summary>
        private void OnDisable()
        {
            actionMap?.Disable();
            ApplyCursorState(false);
        }

        /// <summary>
        /// 이번 프레임의 입력을 읽어 둡니다.
        /// </summary>
        private void Update()
        {
            if (IsInputBlocked)
            {
                ClearInput();
                ApplyCursorState(false);
                return;
            }

            ApplyCursorState(true);

            Move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            Look = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
            IsSprintHeld = sprintAction != null && sprintAction.IsPressed();
            IsCrouchHeld = crouchAction != null && crouchAction.IsPressed();
            IsFlashlightPressed = flashlightAction != null && flashlightAction.WasPressedThisFrame();
            IsInteractPressed = interactAction != null && interactAction.WasPressedThisFrame();
        }

        /// <summary>
        /// UI가 조작을 잡고 있는지 설정합니다.
        /// </summary>
        /// <param name="hasFocus">UI가 조작을 잡고 있으면 true입니다.</param>
        public void SetUiFocus(bool hasFocus)
        {
            HasUiFocus = hasFocus;
        }

        /// <summary>
        /// 읽어 둔 입력을 모두 비웁니다.
        /// </summary>
        private void ClearInput()
        {
            Move = Vector2.zero;
            Look = Vector2.zero;
            IsSprintHeld = false;
            IsCrouchHeld = false;
            IsFlashlightPressed = false;
            IsInteractPressed = false;
        }

        /// <summary>
        /// 커서를 가두거나 풀어 줍니다.
        /// </summary>
        /// <param name="shouldLock">가둘지 여부입니다.</param>
        private void ApplyCursorState(bool shouldLock)
        {
            if (lockCursor == false) return;

            bool wantsLock = shouldLock && Application.isFocused;

            Cursor.lockState = wantsLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = wantsLock == false;
        }
        #endregion // 함수
    }
}
