using UnityEngine;

using SW.Attributes;
using SW.Base;

using ProjectR.Backrooms.Audio;
using ProjectR.Enum;

namespace ProjectR.Backrooms.Player
{
    /// <summary>
    /// 걸은 거리에 맞춰 발소리를 내는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 시간이 아니라 걸은 거리로 걸음을 세므로, 걷기와 달리기의 간격이 저절로 달라집니다.
    /// 바닥에 <see cref="FootstepSurface"/>가 붙어 있으면 그 재질의 소리를 냅니다.
    /// 클립은 실행 중에 합성한 임시 소리입니다. 진짜 음원이 준비되면
    /// SWAudioLibrary에 등록하고 <c>SWAudioManager.PlaySfxRandomPitch</c>로 바꿉니다.
    /// </remarks>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerFootsteps : SWMonoBehaviour
    {
        #region 필드
        /// <summary>걸을 때 한 걸음에 나아가는 거리(미터)입니다.</summary>
        [SWGroup("걸음 간격")]
        [SerializeField, Min(0.1f), Tooltip("걸을 때 한 걸음에 나아가는 거리(미터)입니다.")]
        private float strideLength = 0.85f;

        /// <summary>앉아서 움직일 때 한 걸음에 나아가는 거리(미터)입니다.</summary>
        [SerializeField, Min(0.1f), Tooltip("앉아서 움직일 때 한 걸음에 나아가는 거리(미터)입니다.")]
        private float crouchStrideLength = 1.15f;

        /// <summary>걸을 때의 발소리 크기입니다.</summary>
        [SWGroup("크기")]
        [SerializeField, Range(0f, 1f), Tooltip("걸을 때의 발소리 크기입니다.")]
        private float walkVolume = 0.5f;

        /// <summary>달릴 때의 발소리 크기입니다.</summary>
        [SerializeField, Range(0f, 1f), Tooltip("달릴 때의 발소리 크기입니다.")]
        private float runVolume = 0.85f;

        /// <summary>앉아서 움직일 때의 발소리 크기입니다.</summary>
        [SerializeField, Range(0f, 1f), Tooltip("앉아서 움직일 때의 발소리 크기입니다.")]
        private float crouchVolume = 0.12f;

        /// <summary>재질마다 만들어 둘 발소리 변형의 개수입니다.</summary>
        [SWGroup("변화")]
        [SerializeField, Range(1, 8), Tooltip("재질마다 만들어 둘 발소리 변형의 개수입니다.")]
        private int variationCount = 4;

        /// <summary>걸음마다 음높이를 흔드는 폭입니다.</summary>
        [SerializeField, Range(0f, 0.3f), Tooltip("걸음마다 음높이를 흔드는 폭입니다.")]
        private float pitchVariation = 0.09f;

        /// <summary>바닥 재질을 확인할 때 아래로 쏘는 거리(미터)입니다.</summary>
        [SWGroup("바닥 판정")]
        [SerializeField, Min(0.1f), Tooltip("바닥 재질을 확인할 때 아래로 쏘는 거리(미터)입니다.")]
        private float surfaceCheckDistance = 1.5f;

        /// <summary>자세와 속력을 읽어 올 이동 컴포넌트입니다.</summary>
        private PlayerController playerController;

        /// <summary>발소리를 재생할 오디오 소스입니다.</summary>
        private AudioSource audioSource;

        /// <summary>재질별로 미리 합성해 둔 발소리입니다.</summary>
        private AudioClip[][] footstepClips;

        /// <summary>마지막 걸음 이후 나아간 거리입니다.</summary>
        private float travelledDistance;
        #endregion // 필드

        #region 이벤트
        /// <summary>한 걸음을 디딜 때마다 발생합니다. 소음 반경을 알리는 쪽이 구독합니다.</summary>
        public event System.Action Stepped;
        #endregion // 이벤트

        #region 함수
        /// <summary>
        /// 필요한 컴포넌트를 캐싱하고 발소리를 미리 합성해 둡니다.
        /// </summary>
        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

            BuildFootstepClips();
        }

        /// <summary>
        /// 나아간 거리를 세다가 한 걸음이 될 때마다 소리를 냅니다.
        /// </summary>
        private void Update()
        {
            if (playerController.IsGrounded == false) return;

            float speed = playerController.HorizontalSpeed;

            if (speed < 0.1f)
            {
                travelledDistance = 0f;
                return;
            }

            travelledDistance += speed * Time.deltaTime;

            float stride = playerController.IsCrouching ? crouchStrideLength : strideLength;

            if (travelledDistance < stride) return;

            travelledDistance -= stride;
            PlayFootstep();
        }

        /// <summary>
        /// 지금 밟고 있는 바닥에 맞는 발소리를 한 번 재생합니다.
        /// </summary>
        private void PlayFootstep()
        {
            EFootstepSurface surface = DetectSurface();
            AudioClip[] clips = footstepClips[(int)surface];

            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], GetVolume());

            Stepped?.Invoke();
        }

        /// <summary>
        /// 지금 자세에 맞는 발소리 크기를 구합니다.
        /// </summary>
        /// <returns>0에서 1 사이의 크기입니다.</returns>
        private float GetVolume()
        {
            if (playerController.IsCrouching) return crouchVolume;

            return playerController.IsRunning ? runVolume : walkVolume;
        }

        /// <summary>
        /// 발밑의 바닥 재질을 확인합니다.
        /// </summary>
        /// <returns>확인한 재질입니다. 표시가 없으면 콘크리트로 봅니다.</returns>
        private EFootstepSurface DetectSurface()
        {
            Vector3 origin = transform.position + Vector3.up * 0.3f;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, surfaceCheckDistance,
                    ~0, QueryTriggerInteraction.Ignore) == false)
            {
                return EFootstepSurface.Concrete;
            }

            FootstepSurface surface = hit.collider.GetComponentInParent<FootstepSurface>();

            return surface != null ? surface.Surface : EFootstepSurface.Concrete;
        }

        /// <summary>
        /// 재질별 발소리를 미리 합성해 둡니다.
        /// </summary>
        private void BuildFootstepClips()
        {
            int surfaceCount = System.Enum.GetValues(typeof(EFootstepSurface)).Length;

            footstepClips = new AudioClip[surfaceCount][];

            for (int surfaceIndex = 0; surfaceIndex < surfaceCount; surfaceIndex += 1)
            {
                footstepClips[surfaceIndex] = new AudioClip[variationCount];

                for (int variation = 0; variation < variationCount; variation += 1)
                {
                    footstepClips[surfaceIndex][variation] =
                        ProceduralAudioBank.CreateFootstep((EFootstepSurface)surfaceIndex, variation);
                }
            }
        }
        #endregion // 함수
    }
}
