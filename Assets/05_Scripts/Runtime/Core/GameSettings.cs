using UnityEngine;

using SW.Data;
using SW.Util;

namespace ProjectR.Core
{
    /// <summary>
    /// 플레이어가 옵션에서 고른 값을 저장하고 되살리는 설정 보관소입니다.
    /// </summary>
    /// <remarks>
    /// 옵션 팝업은 UI 계층에 있고 값을 실제로 쓰는 쪽은 백룸 계층에 있어 서로를 참조할 수 없습니다.
    /// 그래서 값을 이 클래스가 혼자 들고 있고, 바뀌었다는 사실만 <see cref="SWEventBus"/>로 알립니다.
    /// 저장은 <see cref="SWPlayerPrefs"/>에 맡기므로 여기서 파일을 직접 다루지 않습니다.
    /// </remarks>
    public static class GameSettings
    {
        #region 상수
        /// <summary>전체 음량을 저장할 키입니다.</summary>
        private const string MasterVolumeKey = "Settings.MasterVolume";

        /// <summary>음소거 여부를 저장할 키입니다.</summary>
        private const string MutedKey = "Settings.Muted";

        /// <summary>마우스 감도를 저장할 키입니다.</summary>
        private const string MouseSensitivityKey = "Settings.MouseSensitivity";

        /// <summary>전체 음량의 기본값입니다.</summary>
        public const float DefaultMasterVolume = 0.8f;

        /// <summary>마우스 감도의 기본값입니다.</summary>
        public const float DefaultMouseSensitivity = 0.08f;

        /// <summary>마우스 감도로 고를 수 있는 가장 낮은 값입니다.</summary>
        public const float MinimumMouseSensitivity = 0.01f;

        /// <summary>마우스 감도로 고를 수 있는 가장 높은 값입니다.</summary>
        public const float MaximumMouseSensitivity = 0.4f;
        #endregion // 상수

        #region 프로퍼티
        /// <summary>음소거를 풀었을 때 적용될 전체 음량입니다.</summary>
        public static float MasterVolume { get; private set; } = DefaultMasterVolume;

        /// <summary>소리를 꺼 두었는지 여부입니다.</summary>
        public static bool IsMuted { get; private set; }

        /// <summary>시점 회전 감도입니다.</summary>
        public static float MouseSensitivity { get; private set; } = DefaultMouseSensitivity;

        /// <summary>저장해 둔 값을 한 번이라도 읽었는지 여부입니다.</summary>
        public static bool IsLoaded { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 저장해 둔 값을 읽어 오고 소리에 반영합니다. 이미 읽었으면 아무것도 하지 않습니다.
        /// </summary>
        /// <remarks>설정을 읽는 쪽이 순서를 신경 쓰지 않아도 되도록 여러 번 불러도 안전하게 두었습니다.</remarks>
        public static void EnsureLoaded()
        {
            if (IsLoaded) return;

            MasterVolume = Mathf.Clamp01(SWPlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));
            IsMuted = SWPlayerPrefs.GetBool(MutedKey, false);
            MouseSensitivity = Mathf.Clamp(
                SWPlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity),
                MinimumMouseSensitivity, MaximumMouseSensitivity);

            IsLoaded = true;

            ApplyVolume();
            Publish();
        }

        /// <summary>
        /// 전체 음량을 바꾸고 저장합니다.
        /// </summary>
        /// <param name="volume">0에서 1 사이의 전체 음량입니다.</param>
        public static void SetMasterVolume(float volume)
        {
            EnsureLoaded();

            MasterVolume = Mathf.Clamp01(volume);
            SWPlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            SWPlayerPrefs.Save();

            ApplyVolume();
            Publish();
        }

        /// <summary>
        /// 소리를 끄거나 켜고 저장합니다.
        /// </summary>
        /// <param name="isMuted">소리를 끌지 여부입니다.</param>
        /// <remarks>음소거를 풀었을 때 원래 음량으로 돌아가도록 음량 자체는 건드리지 않습니다.</remarks>
        public static void SetMuted(bool isMuted)
        {
            EnsureLoaded();

            IsMuted = isMuted;
            SWPlayerPrefs.SetBool(MutedKey, IsMuted);
            SWPlayerPrefs.Save();

            ApplyVolume();
            Publish();
        }

        /// <summary>
        /// 마우스 감도를 바꾸고 저장합니다.
        /// </summary>
        /// <param name="sensitivity">시점 회전 감도입니다.</param>
        public static void SetMouseSensitivity(float sensitivity)
        {
            EnsureLoaded();

            MouseSensitivity = Mathf.Clamp(sensitivity, MinimumMouseSensitivity, MaximumMouseSensitivity);
            SWPlayerPrefs.SetFloat(MouseSensitivityKey, MouseSensitivity);
            SWPlayerPrefs.Save();

            Publish();
        }

        /// <summary>
        /// 지금 값을 소리에 반영합니다.
        /// </summary>
        /// <remarks>
        /// <see cref="AudioListener"/>의 음량을 씁니다. 이것만이 재생 경로와 상관없이 모든 소리에 걸립니다.
        /// 발소리·형광등 웅웅거림·몬스터 소리는 각 컴포넌트가 자기 <see cref="AudioSource"/>로 직접 재생하므로
        /// <see cref="SWAudioManager"/>의 마스터 음량을 낮춰도 그대로 들립니다.
        /// 매니저 쪽 음량까지 함께 낮추면 매니저를 거친 소리만 두 번 깎이므로 여기서는 건드리지 않습니다.
        /// </remarks>
        private static void ApplyVolume()
        {
            AudioListener.volume = IsMuted ? 0f : MasterVolume;
        }

        /// <summary>
        /// 값이 바뀌었음을 알립니다.
        /// </summary>
        private static void Publish()
        {
            SWEventBus.Publish(new GameSettingsChangedEvent(MasterVolume, IsMuted, MouseSensitivity));
        }
        #endregion // 함수
    }

    /// <summary>
    /// 설정값이 바뀌었음을 알리는 이벤트입니다.
    /// </summary>
    public readonly struct GameSettingsChangedEvent
    {
        #region 프로퍼티
        /// <summary>음소거를 풀었을 때 적용될 전체 음량입니다.</summary>
        public float MasterVolume { get; }

        /// <summary>소리를 꺼 두었는지 여부입니다.</summary>
        public bool IsMuted { get; }

        /// <summary>시점 회전 감도입니다.</summary>
        public float MouseSensitivity { get; }
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 바뀐 설정값을 담아 이벤트를 만듭니다.
        /// </summary>
        /// <param name="masterVolume">전체 음량입니다.</param>
        /// <param name="isMuted">소리를 꺼 두었는지 여부입니다.</param>
        /// <param name="mouseSensitivity">시점 회전 감도입니다.</param>
        public GameSettingsChangedEvent(float masterVolume, bool isMuted, float mouseSensitivity)
        {
            MasterVolume = masterVolume;
            IsMuted = isMuted;
            MouseSensitivity = mouseSensitivity;
        }
        #endregion // 생성자
    }
}
