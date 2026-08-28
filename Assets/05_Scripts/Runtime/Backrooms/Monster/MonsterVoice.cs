using UnityEngine;

using SW.Attributes;
using SW.Base;

using ProjectR.Backrooms.Audio;

namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// 몬스터의 행동 모드가 바뀔 때마다 그 사실을 소리로 알리는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 이 컴포넌트가 Phase 3에서 가장 중요합니다. 상태가 바뀌었는데 소리가 없으면
    /// 플레이어는 무슨 일이 일어났는지 모르고, 그러면 대처가 실력이 될 수 없습니다.
    /// 판단 로직을 정교하게 만드는 것보다 이 소리를 제대로 내는 쪽이 먼저입니다.
    /// 클립은 실행 중에 합성한 임시 소리입니다. 진짜 음원이 준비되면
    /// SWAudioLibrary에 키로 등록하고 SWAudioManager로 갈아 끼웁니다.
    /// </remarks>
    [RequireComponent(typeof(MonsterAgent))]
    public class MonsterVoice : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("크기")]
        [SerializeField, Range(0f, 1f), Tooltip("추격을 시작할 때 내는 소리의 크기입니다.")]
        private float screechVolume = 0.9f;

        [SerializeField, Range(0f, 1f), Tooltip("배회와 복귀에서 내는 소리의 크기입니다.")]
        private float growlVolume = 0.55f;

        [SerializeField, Range(0f, 1f), Tooltip("수색과 매복에서 내는 소리의 크기입니다.")]
        private float breathVolume = 0.7f;

        [SWGroup("거리")]
        [SerializeField, Min(1f), Tooltip("소리가 줄어들기 시작하는 거리(미터)입니다.")]
        private float minimumDistance = 4f;

        [SerializeField, Min(2f), Tooltip("소리가 완전히 사라지는 거리(미터)입니다.")]
        private float maximumDistance = 45f;

        /// <summary>모드 변화를 알려 줄 몬스터의 몸입니다.</summary>
        private MonsterAgent agent;

        /// <summary>모드 변화 소리를 재생할 오디오 소스입니다.</summary>
        private AudioSource voiceSource;

        /// <summary>배회와 복귀에 쓰는 그르렁 소리입니다.</summary>
        private AudioClip growlClip;

        /// <summary>추격 시작에 쓰는 날카로운 소리입니다.</summary>
        private AudioClip screechClip;

        /// <summary>수색과 매복에 쓰는 숨소리입니다.</summary>
        private AudioClip breathClip;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 소리를 미리 합성하고 재생용 소스를 준비합니다.
        /// </summary>
        private void Awake()
        {
            agent = GetComponent<MonsterAgent>();

            growlClip = ProceduralAudioBank.CreateMonsterGrowl();
            screechClip = ProceduralAudioBank.CreateMonsterScreech();
            breathClip = ProceduralAudioBank.CreateMonsterBreath();

            GameObject sourceObject = new GameObject("VoiceSource");
            sourceObject.transform.SetParent(transform, false);

            voiceSource = sourceObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 1f;
            voiceSource.rolloffMode = AudioRolloffMode.Linear;
            voiceSource.minDistance = minimumDistance;
            voiceSource.maxDistance = maximumDistance;
        }

        /// <summary>
        /// 모드 변화 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            agent.ModeChanged += HandleModeChanged;
        }

        /// <summary>
        /// 모드 변화 알림을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            agent.ModeChanged -= HandleModeChanged;
        }

        /// <summary>
        /// 바뀐 모드에 맞는 소리를 한 번 냅니다.
        /// </summary>
        /// <param name="previousMode">바뀌기 전의 모드입니다.</param>
        /// <param name="currentMode">바뀐 뒤의 모드입니다.</param>
        private void HandleModeChanged(EMonsterMode previousMode, EMonsterMode currentMode)
        {
            AudioClip clip = GetClip(currentMode, out float volume);

            if (clip == null) return;

            voiceSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// 모드에 해당하는 클립과 크기를 구합니다.
        /// </summary>
        /// <param name="mode">소리를 낼 행동 모드입니다.</param>
        /// <param name="volume">해당 소리의 크기입니다.</param>
        /// <returns>재생할 클립입니다. 낼 소리가 없으면 null을 반환합니다.</returns>
        private AudioClip GetClip(EMonsterMode mode, out float volume)
        {
            switch (mode)
            {
                case EMonsterMode.Chase:
                    volume = screechVolume;
                    return screechClip;

                case EMonsterMode.Search:
                case EMonsterMode.Ambush:
                    volume = breathVolume;
                    return breathClip;

                case EMonsterMode.Patrol:
                case EMonsterMode.Return:
                    volume = growlVolume;
                    return growlClip;

                default:
                    volume = 0f;
                    return null;
            }
        }
        #endregion // 함수
    }
}
