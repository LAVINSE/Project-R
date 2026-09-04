using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Core;

namespace ProjectR.Backrooms.Player
{
    /// <summary>
    /// 마우스 입력으로 시점을 돌리고 카메라를 눈높이에 맞춰 두는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 좌우 회전은 몸통에, 상하 회전은 카메라에 나눠 적용합니다.
    /// 몸통을 위아래로 눕히면 캐릭터 컨트롤러의 충돌 판정이 함께 기울어지기 때문입니다.
    /// </remarks>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerCameraLook : SWMonoBehaviour
    {
        #region 필드
        /// <summary>상하 회전을 적용할 카메라입니다.</summary>
        [SWGroup("대상")]
        [SerializeField, Tooltip("상하 회전을 적용할 카메라입니다.")]
        private Transform cameraTransform;

        /// <summary>자세를 읽어 눈높이를 맞출 이동 컴포넌트입니다.</summary>
        [SerializeField, Tooltip("자세를 읽어 눈높이를 맞출 이동 컴포넌트입니다.")]
        private PlayerController playerController;

        /// <summary>옵션에서 고른 값이 없을 때 쓸 시점 회전 감도입니다.</summary>
        [SWGroup("감도")]
        [SerializeField, Min(0.01f), Tooltip("옵션에서 고른 값이 없을 때 쓸 시점 회전 감도입니다.")]
        private float sensitivity = GameSettings.DefaultMouseSensitivity;

        /// <summary>위아래로 돌릴 수 있는 최대 각도입니다.</summary>
        [SerializeField, Range(30f, 89f), Tooltip("위아래로 돌릴 수 있는 최대 각도입니다.")]
        private float pitchLimit = 85f;

        /// <summary>서 있을 때 발밑에서 눈까지의 높이(미터)입니다.</summary>
        [SWGroup("눈높이")]
        [SerializeField, Min(0f), Tooltip("서 있을 때 발밑에서 눈까지의 높이(미터)입니다.")]
        private float standingEyeHeight = 1.62f;

        /// <summary>앉았을 때 발밑에서 눈까지의 높이(미터)입니다.</summary>
        [SerializeField, Min(0f), Tooltip("앉았을 때 발밑에서 눈까지의 높이(미터)입니다.")]
        private float crouchingEyeHeight = 0.95f;

        /// <summary>눈높이가 따라오는 빠르기입니다.</summary>
        [SerializeField, Min(0.1f), Tooltip("눈높이가 따라오는 빠르기입니다.")]
        private float eyeHeightSpeed = 9f;

        /// <summary>입력을 읽어 주는 컴포넌트입니다.</summary>
        private PlayerInputReader inputReader;

        /// <summary>현재 상하 회전 각도입니다.</summary>
        private float pitch;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 필요한 컴포넌트를 캐싱하고 눈높이를 맞춰 둡니다.
        /// </summary>
        private void Awake()
        {
            inputReader = GetComponent<PlayerInputReader>();

            if (cameraTransform == null)
            {
                SWLog.LogError($"[{nameof(PlayerCameraLook)}] 카메라가 비어 있어 시점을 돌릴 수 없습니다.");
                return;
            }

            cameraTransform.localPosition = new Vector3(0f, standingEyeHeight, 0f);
        }

        /// <summary>
        /// 저장해 둔 감도를 읽어 오고 옵션에서 바뀌는 것을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            GameSettings.EnsureLoaded();
            sensitivity = GameSettings.MouseSensitivity;

            SWEventBus.Subscribe<GameSettingsChangedEvent>(HandleSettingsChanged);
        }

        /// <summary>
        /// 설정 변경 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            SWEventBus.Unsubscribe<GameSettingsChangedEvent>(HandleSettingsChanged);
        }

        /// <summary>
        /// 옵션에서 고른 감도를 반영합니다.
        /// </summary>
        /// <param name="eventData">바뀐 설정값입니다.</param>
        private void HandleSettingsChanged(GameSettingsChangedEvent eventData)
        {
            sensitivity = eventData.MouseSensitivity;
        }

        /// <summary>
        /// 입력만큼 시점을 돌리고 자세에 맞춰 눈높이를 옮깁니다.
        /// </summary>
        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            Vector2 look = inputReader.Look * sensitivity;

            transform.Rotate(Vector3.up, look.x, Space.Self);

            pitch = Mathf.Clamp(pitch - look.y, -pitchLimit, pitchLimit);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            UpdateEyeHeight();
        }

        /// <summary>
        /// 앉은 자세에 맞춰 카메라 높이를 부드럽게 옮깁니다.
        /// </summary>
        private void UpdateEyeHeight()
        {
            bool isCrouching = playerController != null && playerController.IsCrouching;
            float targetHeight = isCrouching ? crouchingEyeHeight : standingEyeHeight;
            float newHeight = Mathf.MoveTowards(cameraTransform.localPosition.y, targetHeight,
                eyeHeightSpeed * Time.deltaTime);

            cameraTransform.localPosition = new Vector3(0f, newHeight, 0f);
        }
        #endregion // 함수
    }
}
