using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

namespace ProjectR.Backrooms.Audio
{
    /// <summary>
    /// 형광등 환경음과 공간 바닥 소음을 깔고, 공간 크기에 맞춰 잔향을 바꾸는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 잔향을 공간 크기에 반응시키는 이유는, 복도에서 홀로 나왔을 때 눈보다 귀가 먼저 알아채게 하기 위해서입니다.
    /// 공간 크기는 사방으로 광선을 쏘아 잽니다. 매 프레임 잴 필요가 없어 주기적으로만 갱신합니다.
    /// 클립은 실행 중에 합성한 임시 소리입니다. 진짜 음원이 준비되면 SWAudioLibrary로 옮깁니다.
    /// </remarks>
    public class BackroomsAmbience : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("크기")]
        [SerializeField, Range(0f, 1f), Tooltip("형광등 환경음의 크기입니다.")]
        private float humVolume = 0.18f;

        [SerializeField, Range(0f, 1f), Tooltip("공간 바닥 소음의 크기입니다.")]
        private float roomToneVolume = 0.1f;

        [SWGroup("공간 측정")]
        [SerializeField, Min(0.1f), Tooltip("공간 크기를 다시 재는 간격(초)입니다.")]
        private float measureIntervalSeconds = 0.4f;

        [SerializeField, Min(1f), Tooltip("공간 크기를 잴 때 광선을 쏘는 최대 거리(미터)입니다.")]
        private float measureDistance = 30f;

        [SerializeField, Min(0.1f), Tooltip("이 평균 거리 이하면 가장 좁은 공간으로 봅니다.")]
        private float narrowDistance = 2.5f;

        [SerializeField, Min(0.1f), Tooltip("이 평균 거리 이상이면 가장 넓은 공간으로 봅니다.")]
        private float wideDistance = 12f;

        [SWGroup("잔향")]
        [SerializeField, Tooltip("잔향을 적용할 필터입니다. 비워 두면 오디오 리스너에 붙입니다.")]
        private AudioReverbFilter reverbFilter;

        [SerializeField, Min(0.1f), Tooltip("좁은 공간의 잔향 길이(초)입니다.")]
        private float narrowDecaySeconds = 0.7f;

        [SerializeField, Min(0.1f), Tooltip("넓은 공간의 잔향 길이(초)입니다.")]
        private float wideDecaySeconds = 2.6f;

        [SerializeField, Min(0.1f), Tooltip("잔향이 따라 변하는 빠르기입니다.")]
        private float reverbBlendSpeed = 1.5f;

        [SWGroup("정적 구간")]
        [SerializeField, Range(0f, 1f), Tooltip("정적 구간에서 환경음 크기에 곱할 값입니다.")]
        private float quietVolumeScale = 0.12f;

        [SerializeField, Min(0.01f), Tooltip("정적 구간을 오갈 때 크기가 변하는 빠르기입니다.")]
        private float quietBlendSpeed = 0.5f;

        /// <summary>형광등 환경음을 재생하는 소스입니다.</summary>
        private AudioSource humSource;

        /// <summary>공간 바닥 소음을 재생하는 소스입니다.</summary>
        private AudioSource roomToneSource;

        /// <summary>다음에 공간을 다시 잴 때까지 남은 시간입니다.</summary>
        private float measureCooldown;

        /// <summary>지금까지 잰 공간의 트인 정도입니다. 0이 가장 좁고 1이 가장 넓습니다.</summary>
        private float openness;

        /// <summary>화면에 실제로 적용 중인 트인 정도입니다.</summary>
        private float blendedOpenness;

        /// <summary>지금 목표로 하는 환경음 크기 배율입니다.</summary>
        private float targetVolumeScale = 1f;

        /// <summary>실제로 적용 중인 환경음 크기 배율입니다.</summary>
        private float currentVolumeScale = 1f;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>지금 공간이 얼마나 트여 있는지입니다. 0이 복도, 1이 넓은 홀입니다.</summary>
        public float Openness => blendedOpenness;

        /// <summary>지금 정적 구간인지 여부입니다.</summary>
        public bool IsQuiet => targetVolumeScale < 1f;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 환경음 소스를 만들고 잔향 필터를 준비합니다.
        /// </summary>
        private void Awake()
        {
            humSource = CreateLoopSource("AmbienceHum", ProceduralAudioBank.CreateFluorescentHum(), humVolume);
            roomToneSource = CreateLoopSource("AmbienceRoomTone", ProceduralAudioBank.CreateRoomTone(), roomToneVolume);

            if (reverbFilter == null) reverbFilter = FindOrCreateReverbFilter();
        }

        /// <summary>
        /// 주기적으로 공간을 다시 재고 잔향을 부드럽게 옮깁니다.
        /// </summary>
        private void Update()
        {
            measureCooldown -= Time.deltaTime;

            if (measureCooldown <= 0f)
            {
                measureCooldown = measureIntervalSeconds;
                openness = MeasureOpenness();
            }

            blendedOpenness = Mathf.MoveTowards(blendedOpenness, openness, reverbBlendSpeed * Time.deltaTime);

            ApplyReverb(blendedOpenness);
            UpdateVolumeScale();
        }

        /// <summary>
        /// 정적 구간에 들어가거나 빠져나옵니다.
        /// </summary>
        /// <param name="isQuiet">정적 구간으로 만들지 여부입니다.</param>
        /// <remarks>
        /// 소리를 빼는 것도 설계입니다. 계속 웅웅거리면 귀가 익숙해져서 무서움이 사라지므로,
        /// 가끔 환경음을 걷어 내 정적을 만들었다가 되돌립니다.
        /// </remarks>
        public void SetQuiet(bool isQuiet)
        {
            targetVolumeScale = isQuiet ? quietVolumeScale : 1f;
        }

        /// <summary>
        /// 환경음 크기를 목표 배율로 부드럽게 옮깁니다.
        /// </summary>
        private void UpdateVolumeScale()
        {
            currentVolumeScale = Mathf.MoveTowards(currentVolumeScale, targetVolumeScale,
                quietBlendSpeed * Time.deltaTime);

            humSource.volume = humVolume * currentVolumeScale;
            roomToneSource.volume = roomToneVolume * currentVolumeScale;
        }

        /// <summary>
        /// 사방으로 광선을 쏘아 공간이 얼마나 트여 있는지 잽니다.
        /// </summary>
        /// <returns>0에서 1 사이의 트인 정도입니다.</returns>
        private float MeasureOpenness()
        {
            Vector3 origin = transform.position + Vector3.up * 1.4f;
            float total = 0f;

            total += MeasureDirection(origin, Vector3.forward);
            total += MeasureDirection(origin, Vector3.back);
            total += MeasureDirection(origin, Vector3.left);
            total += MeasureDirection(origin, Vector3.right);

            float average = total * 0.25f;

            return Mathf.InverseLerp(narrowDistance, wideDistance, average);
        }

        /// <summary>
        /// 한 방향으로 광선을 쏘아 벽까지의 거리를 잽니다.
        /// </summary>
        /// <param name="origin">광선을 쏘기 시작할 위치입니다.</param>
        /// <param name="direction">광선을 쏠 방향입니다.</param>
        /// <returns>벽까지의 거리(미터)입니다. 아무것도 없으면 최대 거리를 반환합니다.</returns>
        private float MeasureDirection(Vector3 origin, Vector3 direction)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, measureDistance,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.distance;
            }

            return measureDistance;
        }

        /// <summary>
        /// 트인 정도에 맞춰 잔향 값을 적용합니다.
        /// </summary>
        /// <param name="value">0에서 1 사이의 트인 정도입니다.</param>
        private void ApplyReverb(float value)
        {
            if (reverbFilter == null) return;

            reverbFilter.decayTime = Mathf.Lerp(narrowDecaySeconds, wideDecaySeconds, value);
            reverbFilter.room = Mathf.Lerp(-1400f, -350f, value);
            reverbFilter.reflectionsDelay = Mathf.Lerp(0.007f, 0.028f, value);
            reverbFilter.reverbDelay = Mathf.Lerp(0.011f, 0.04f, value);
        }

        /// <summary>
        /// 반복 재생용 오디오 소스를 만들어 재생을 시작합니다.
        /// </summary>
        /// <param name="sourceName">만들 오브젝트의 이름입니다.</param>
        /// <param name="clip">재생할 클립입니다.</param>
        /// <param name="volume">재생 크기입니다.</param>
        /// <returns>만들어진 오디오 소스입니다.</returns>
        private AudioSource CreateLoopSource(string sourceName, AudioClip clip, float volume)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.Play();

            return source;
        }

        /// <summary>
        /// 오디오 리스너에서 잔향 필터를 찾고, 없으면 붙입니다.
        /// </summary>
        /// <returns>사용할 잔향 필터입니다. 리스너가 없으면 null을 반환합니다.</returns>
        private AudioReverbFilter FindOrCreateReverbFilter()
        {
            AudioListener listener = GetComponentInChildren<AudioListener>();

            if (listener == null) listener = FindAnyObjectByType<AudioListener>();

            if (listener == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsAmbience)}] 오디오 리스너가 없어 잔향을 적용하지 못합니다.");
                return null;
            }

            AudioReverbFilter filter = listener.GetComponent<AudioReverbFilter>();

            if (filter == null) filter = listener.gameObject.AddComponent<AudioReverbFilter>();

            filter.reverbPreset = AudioReverbPreset.User;

            return filter;
        }
        #endregion // 함수
    }
}
