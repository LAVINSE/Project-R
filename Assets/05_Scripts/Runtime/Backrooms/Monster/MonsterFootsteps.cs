using UnityEngine;

using SW.Attributes;
using SW.Base;

using ProjectR.Backrooms.Audio;

namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// 몬스터가 움직인 거리에 맞춰 발소리를 내는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 발소리가 멀어지다가 멈추는 순간이 어떤 판단 로직보다 강한 연출입니다.
    /// 그 정적을 만들려면 움직이는 동안 발소리가 반드시 들려야 하므로,
    /// 멈추면 저절로 끊기도록 시간이 아니라 실제로 나아간 거리로 걸음을 셉니다.
    /// </remarks>
    [RequireComponent(typeof(MonsterAgent))]
    public class MonsterFootsteps : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("걸음 간격")]
        [SerializeField, Min(0.1f), Tooltip("한 걸음에 나아가는 거리(미터)입니다.")]
        private float strideLength = 1.5f;

        [SWGroup("소리")]
        [SerializeField, Range(0f, 1f), Tooltip("발소리의 크기입니다.")]
        private float volume = 0.8f;

        [SerializeField, Range(1, 8), Tooltip("만들어 둘 발소리 변형의 개수입니다.")]
        private int variationCount = 4;

        [SerializeField, Range(0f, 0.3f), Tooltip("걸음마다 음높이를 흔드는 폭입니다.")]
        private float pitchVariation = 0.07f;

        [SWGroup("거리")]
        [SerializeField, Min(1f), Tooltip("소리가 줄어들기 시작하는 거리(미터)입니다.")]
        private float minimumDistance = 3f;

        [SerializeField, Min(2f), Tooltip("소리가 완전히 사라지는 거리(미터)입니다.")]
        private float maximumDistance = 35f;

        /// <summary>속력을 읽어 올 몬스터의 몸입니다.</summary>
        private MonsterAgent agent;

        /// <summary>발소리를 재생할 오디오 소스입니다.</summary>
        private AudioSource audioSource;

        /// <summary>미리 합성해 둔 발소리 변형입니다.</summary>
        private AudioClip[] footstepClips;

        /// <summary>마지막 걸음 이후 나아간 거리입니다.</summary>
        private float travelledDistance;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 발소리를 미리 합성하고 재생용 소스를 준비합니다.
        /// </summary>
        private void Awake()
        {
            agent = GetComponent<MonsterAgent>();

            footstepClips = new AudioClip[variationCount];

            for (int index = 0; index < variationCount; index += 1)
                footstepClips[index] = ProceduralAudioBank.CreateMonsterFootstep(index);

            GameObject sourceObject = new GameObject("FootstepSource");
            sourceObject.transform.SetParent(transform, false);

            audioSource = sourceObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = minimumDistance;
            audioSource.maxDistance = maximumDistance;
        }

        /// <summary>
        /// 나아간 거리를 세다가 한 걸음이 될 때마다 소리를 냅니다.
        /// </summary>
        private void Update()
        {
            float speed = agent.CurrentSpeed;

            if (speed < 0.1f)
            {
                travelledDistance = 0f;
                return;
            }

            travelledDistance += speed * Time.deltaTime;

            if (travelledDistance < strideLength) return;

            travelledDistance -= strideLength;

            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)], volume);
        }
        #endregion // 함수
    }
}
