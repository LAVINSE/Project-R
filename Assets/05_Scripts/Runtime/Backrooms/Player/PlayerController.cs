using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Debugging;

namespace ProjectR.Backrooms.Player
{
    /// <summary>
    /// 1인칭 이동을 담당하는 컴포넌트입니다. 걷기, 달리기, 앉기를 처리합니다.
    /// </summary>
    /// <remarks>
    /// 시점 회전은 <see cref="PlayerCameraLook"/>이 맡고, 여기서는 좌우 회전값을 그대로 따라 이동합니다.
    /// 앉기는 누르고 있는 동안만 유지되며, 천장에 막히면 일어서지 않습니다.
    /// </remarks>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : SWMonoBehaviour
    {
        #region 필드
        /// <summary>걸을 때의 속도(m/s)입니다.</summary>
        [SWGroup("이동 속도")]
        [SerializeField, Min(0f), Tooltip("걸을 때의 속도(m/s)입니다.")]
        private float walkSpeed = 2.6f;

        /// <summary>달릴 때의 속도(m/s)입니다.</summary>
        [SerializeField, Min(0f), Tooltip("달릴 때의 속도(m/s)입니다.")]
        private float runSpeed = 4.6f;

        /// <summary>앉아서 움직일 때의 속도(m/s)입니다.</summary>
        [SerializeField, Min(0f), Tooltip("앉아서 움직일 때의 속도(m/s)입니다.")]
        private float crouchSpeed = 1.2f;

        /// <summary>목표 속도에 도달하는 빠르기입니다. 낮을수록 미끄러집니다.</summary>
        [SerializeField, Min(0.1f), Tooltip("목표 속도에 도달하는 빠르기입니다. 낮을수록 미끄러집니다.")]
        private float acceleration = 14f;

        /// <summary>서 있을 때의 키(미터)입니다.</summary>
        [SWGroup("앉기")]
        [SerializeField, Min(0.5f), Tooltip("서 있을 때의 키(미터)입니다.")]
        private float standingHeight = 1.8f;

        /// <summary>앉았을 때의 키(미터)입니다.</summary>
        [SerializeField, Min(0.5f), Tooltip("앉았을 때의 키(미터)입니다.")]
        private float crouchingHeight = 1.1f;

        /// <summary>앉고 일어서는 데 걸리는 빠르기입니다.</summary>
        [SerializeField, Min(0.1f), Tooltip("앉고 일어서는 데 걸리는 빠르기입니다.")]
        private float crouchTransitionSpeed = 9f;

        /// <summary>적용할 중력 가속도(m/s²)입니다.</summary>
        [SWGroup("중력")]
        [SerializeField, Tooltip("적용할 중력 가속도(m/s²)입니다.")]
        private float gravity = -18f;

        /// <summary>바닥에 붙어 있도록 계속 눌러 주는 속도(m/s)입니다.</summary>
        [SerializeField, Min(0f), Tooltip("바닥에 붙어 있도록 계속 눌러 주는 속도(m/s)입니다.")]
        private float groundedStickSpeed = 2f;

        /// <summary>이동을 실제로 수행하는 캐릭터 컨트롤러입니다.</summary>
        private CharacterController characterController;

        /// <summary>입력을 읽어 주는 컴포넌트입니다.</summary>
        private PlayerInputReader inputReader;

        /// <summary>현재 수평 이동 속도입니다.</summary>
        private Vector3 horizontalVelocity;

        /// <summary>현재 수직 이동 속도입니다.</summary>
        private float verticalSpeed;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>수평 이동 속력(m/s)입니다. 발소리 간격을 정할 때 씁니다.</summary>
        public float HorizontalSpeed => horizontalVelocity.magnitude;

        /// <summary>앉아 있는지 여부입니다.</summary>
        public bool IsCrouching { get; private set; }

        /// <summary>달리고 있는지 여부입니다.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>바닥에 닿아 있는지 여부입니다.</summary>
        public bool IsGrounded => characterController.isGrounded;

        /// <summary>서 있을 때의 키(미터)입니다.</summary>
        public float StandingHeight => standingHeight;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 필요한 컴포넌트를 캐싱하고 디버그 감시 항목을 등록합니다.
        /// </summary>
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputReader = GetComponent<PlayerInputReader>();

            characterController.height = standingHeight;
            characterController.center = new Vector3(0f, standingHeight * 0.5f, 0f);

            SWDebugConsole.Watch("플레이어 속력", () => $"{HorizontalSpeed:F1} m/s");
            SWDebugConsole.Watch("플레이어 자세", () => IsCrouching ? "앉음" : IsRunning ? "달림" : "섬");
        }

        /// <summary>
        /// 디버그 감시 항목을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            SWDebugConsole.Unwatch("플레이어 속력");
            SWDebugConsole.Unwatch("플레이어 자세");
        }

        /// <summary>
        /// 입력을 받아 자세와 속도를 갱신하고 실제로 이동시킵니다.
        /// </summary>
        private void Update()
        {
            UpdateStance();
            UpdateHorizontalVelocity();
            UpdateVerticalSpeed();

            Vector3 motion = horizontalVelocity + Vector3.up * verticalSpeed;

            characterController.Move(motion * Time.deltaTime);
        }

        /// <summary>
        /// 앉기 상태와 키를 갱신합니다.
        /// </summary>
        /// <remarks>천장에 막혀 있으면 앉기를 풀어도 일어서지 않습니다.</remarks>
        private void UpdateStance()
        {
            bool wantsCrouch = inputReader.IsCrouchHeld;

            if (wantsCrouch == false && IsCrouching && HasCeilingAbove()) wantsCrouch = true;

            IsCrouching = wantsCrouch;

            float targetHeight = IsCrouching ? crouchingHeight : standingHeight;
            float newHeight = Mathf.MoveTowards(characterController.height, targetHeight,
                crouchTransitionSpeed * Time.deltaTime);

            characterController.height = newHeight;
            characterController.center = new Vector3(0f, newHeight * 0.5f, 0f);
        }

        /// <summary>
        /// 입력 방향과 목표 속도로 수평 속도를 갱신합니다.
        /// </summary>
        private void UpdateHorizontalVelocity()
        {
            Vector2 input = Vector2.ClampMagnitude(inputReader.Move, 1f);
            Vector3 direction = transform.right * input.x + transform.forward * input.y;

            IsRunning = IsCrouching == false && inputReader.IsSprintHeld && input.sqrMagnitude > 0.01f;

            float targetSpeed = IsCrouching ? crouchSpeed : IsRunning ? runSpeed : walkSpeed;
            Vector3 targetVelocity = direction * targetSpeed;

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity,
                acceleration * Time.deltaTime);
        }

        /// <summary>
        /// 중력을 적용해 수직 속도를 갱신합니다.
        /// </summary>
        private void UpdateVerticalSpeed()
        {
            if (IsGrounded && verticalSpeed <= 0f)
            {
                verticalSpeed = -groundedStickSpeed;
                return;
            }

            verticalSpeed += gravity * Time.deltaTime;
        }

        /// <summary>
        /// 머리 위가 막혀 있는지 확인합니다.
        /// </summary>
        /// <returns>일어설 공간이 없으면 true를 반환합니다.</returns>
        private bool HasCeilingAbove()
        {
            float radius = characterController.radius;
            Vector3 origin = transform.position + Vector3.up * (crouchingHeight - radius);
            float distance = standingHeight - crouchingHeight + radius;

            return Physics.SphereCast(origin, radius * 0.95f, Vector3.up, out _, distance,
                ~0, QueryTriggerInteraction.Ignore);
        }
        #endregion // 함수
    }
}
