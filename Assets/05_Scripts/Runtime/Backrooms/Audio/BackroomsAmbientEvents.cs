using UnityEngine;

using SW.Attributes;
using SW.Base;

namespace ProjectR.Backrooms.Audio
{
    /// <summary>
    /// 물방울, 먼 충격음, 환풍기 같은 미세 환경음을 가끔씩 주변에서 울리는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 계속 울리면 배경이 되어 버리므로 간격을 길게 두고 위치도 매번 바꿉니다.
    /// 소리가 어디서 났는지 모르게 만드는 것이 목적이라 플레이어에게서 떨어진 곳에 놓고 3D로 재생합니다.
    /// 가끔은 아무 소리도 내지 않는 정적 구간을 만들어 환경음까지 걷어 냅니다.
    /// 소리를 빼면 다음 소리가 훨씬 크게 들립니다.
    /// </remarks>
    [RequireComponent(typeof(BackroomsAmbience))]
    public class BackroomsAmbientEvents : SWMonoBehaviour
    {
        #region 필드
        /// <summary>환경음 사이의 최소 간격(초)입니다.</summary>
        [SWGroup("간격")]
        [SerializeField, Min(1f), Tooltip("환경음 사이의 최소 간격(초)입니다.")]
        private float minimumIntervalSeconds = 9f;

        /// <summary>환경음 사이의 최대 간격(초)입니다.</summary>
        [SerializeField, Min(1f), Tooltip("환경음 사이의 최대 간격(초)입니다.")]
        private float maximumIntervalSeconds = 26f;

        /// <summary>소리를 낼 최소 거리(미터)입니다.</summary>
        [SWGroup("위치")]
        [SerializeField, Min(1f), Tooltip("소리를 낼 최소 거리(미터)입니다.")]
        private float minimumDistance = 5f;

        /// <summary>소리를 낼 최대 거리(미터)입니다.</summary>
        [SerializeField, Min(1f), Tooltip("소리를 낼 최대 거리(미터)입니다.")]
        private float maximumDistance = 18f;

        /// <summary>환경음의 크기입니다.</summary>
        [SerializeField, Range(0f, 1f), Tooltip("환경음의 크기입니다.")]
        private float volume = 0.55f;

        /// <summary>환경음을 낼 차례에 대신 정적 구간으로 들어갈 확률입니다.</summary>
        [SWGroup("정적 구간")]
        [SerializeField, Range(0f, 1f), Tooltip("환경음을 낼 차례에 대신 정적 구간으로 들어갈 확률입니다.")]
        private float quietChance = 0.22f;

        /// <summary>정적 구간의 최소 길이(초)입니다.</summary>
        [SerializeField, Min(1f), Tooltip("정적 구간의 최소 길이(초)입니다.")]
        private float minimumQuietSeconds = 12f;

        /// <summary>정적 구간의 최대 길이(초)입니다.</summary>
        [SerializeField, Min(1f), Tooltip("정적 구간의 최대 길이(초)입니다.")]
        private float maximumQuietSeconds = 30f;

        /// <summary>정적 구간을 맡길 환경음 컴포넌트입니다.</summary>
        private BackroomsAmbience ambience;

        /// <summary>환경음을 재생할 3D 오디오 소스입니다.</summary>
        private AudioSource eventSource;

        /// <summary>재생할 환경음 클립 목록입니다.</summary>
        private AudioClip[] eventClips;

        /// <summary>다음 환경음까지 남은 시간입니다.</summary>
        private float nextEventCooldown;

        /// <summary>정적 구간이 끝날 때까지 남은 시간입니다.</summary>
        private float quietRemaining;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 환경음을 미리 합성하고 재생용 소스를 준비합니다.
        /// </summary>
        private void Awake()
        {
            ambience = GetComponent<BackroomsAmbience>();

            eventClips = new[]
            {
                ProceduralAudioBank.CreateWaterDrip(),
                ProceduralAudioBank.CreateDistantThud(),
                ProceduralAudioBank.CreateVentGust(),
            };

            GameObject sourceObject = new("AmbientEventSource");
            sourceObject.transform.SetParent(transform, false);

            eventSource = sourceObject.AddComponent<AudioSource>();
            eventSource.playOnAwake = false;
            eventSource.loop = false;
            eventSource.spatialBlend = 1f;
            eventSource.rolloffMode = AudioRolloffMode.Linear;
            eventSource.minDistance = 2f;
            eventSource.maxDistance = maximumDistance * 2f;

            nextEventCooldown = Random.Range(minimumIntervalSeconds, maximumIntervalSeconds);
        }

        /// <summary>
        /// 시간이 되면 환경음을 울리거나 정적 구간으로 들어갑니다.
        /// </summary>
        private void Update()
        {
            if (quietRemaining > 0f)
            {
                quietRemaining -= Time.deltaTime;

                if (quietRemaining <= 0f)
                {
                    ambience.SetQuiet(false);
                    nextEventCooldown = Random.Range(minimumIntervalSeconds, maximumIntervalSeconds);
                }

                return;
            }

            nextEventCooldown -= Time.deltaTime;

            if (nextEventCooldown > 0f) return;

            if (Random.value < quietChance)
            {
                BeginQuietWindow();
                return;
            }

            PlayAmbientEvent();

            nextEventCooldown = Random.Range(minimumIntervalSeconds, maximumIntervalSeconds);
        }

        /// <summary>
        /// 환경음 하나를 주변 아무 곳에서 울립니다.
        /// </summary>
        private void PlayAmbientEvent()
        {
            Vector2 offset = Random.insideUnitCircle.normalized *
                Random.Range(minimumDistance, maximumDistance);

            eventSource.transform.position = transform.position +
                new Vector3(offset.x, Random.Range(0.2f, 2.6f), offset.y);

            eventSource.PlayOneShot(eventClips[Random.Range(0, eventClips.Length)], volume);
        }

        /// <summary>
        /// 정적 구간을 시작합니다.
        /// </summary>
        private void BeginQuietWindow()
        {
            quietRemaining = Random.Range(minimumQuietSeconds, maximumQuietSeconds);
            ambience.SetQuiet(true);
        }
        #endregion // 함수
    }
}
