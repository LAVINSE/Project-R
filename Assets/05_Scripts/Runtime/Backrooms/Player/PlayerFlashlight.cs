using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Debugging;
using SW.Util;

namespace ProjectR.Backrooms.Player
{
    /// <summary>
    /// 손전등을 켜고 끄고, 배터리를 소모시키고, 시점보다 조금 늦게 따라오도록 흔드는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 이 손전등이 이 프로젝트에서 유일하게 실행 중에 켜지는 실시간 라이트입니다.
    /// 맵 조명은 전부 프리팹에 구워 두었으므로, 실시간 라이트를 여기서 더 늘리지 않습니다.
    /// 배터리 잔량은 UI 없이 밝기로만 알립니다. 잔량이 줄면 빛이 어두워지고 흔들림이 커집니다.
    /// </remarks>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerFlashlight : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("대상")]
        [SerializeField, Tooltip("켜고 끌 스포트라이트입니다. 카메라의 자식으로 두지 않습니다.")]
        private Light spotLight;

        [SerializeField, Tooltip("손전등이 따라갈 시점 기준입니다. 보통 카메라를 넣습니다.")]
        private Transform aimTransform;

        [SWGroup("배터리")]
        [SerializeField, Min(1f), Tooltip("가득 찼을 때 켜 둘 수 있는 시간(초)입니다.")]
        private float fullBatterySeconds = 600f;

        [SerializeField, Tooltip("씬을 시작할 때 손전등을 켜 둘지 여부입니다.")]
        private bool startTurnedOn = true;

        [SWGroup("밝기")]
        [SerializeField, Min(0f), Tooltip("배터리가 가득 찼을 때의 밝기입니다.")]
        private float fullIntensity = 3.2f;

        [SerializeField, Range(0f, 1f), Tooltip("이 잔량 아래로 내려가면 밝기가 줄기 시작합니다.")]
        private float dimStartRatio = 0.25f;

        [SerializeField, Range(0f, 1f), Tooltip("배터리가 바닥났을 때 남는 밝기 비율입니다.")]
        private float minimumIntensityRatio = 0.25f;

        [SWGroup("흔들림")]
        [SerializeField, Min(0.1f), Tooltip("시점을 따라오는 빠르기입니다. 낮을수록 크게 흔들립니다.")]
        private float followSpeed = 11f;

        [SerializeField, Min(0f), Tooltip("배터리가 바닥났을 때 따라오는 빠르기에 곱할 값입니다.")]
        private float lowBatteryFollowScale = 0.55f;

        /// <summary>입력을 읽어 주는 컴포넌트입니다.</summary>
        private PlayerInputReader inputReader;

        /// <summary>남은 배터리 시간(초)입니다.</summary>
        private float remainingBatterySeconds;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>손전등이 켜져 있는지 여부입니다.</summary>
        public bool IsTurnedOn => spotLight != null && spotLight.enabled;

        /// <summary>남은 배터리 비율입니다. 0에서 1 사이입니다.</summary>
        public float BatteryRatio => fullBatterySeconds > 0f
            ? Mathf.Clamp01(remainingBatterySeconds / fullBatterySeconds)
            : 0f;

        /// <summary>배터리가 남아 있는지 여부입니다.</summary>
        public bool HasBattery => remainingBatterySeconds > 0f;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 필요한 컴포넌트를 캐싱하고 배터리를 채웁니다.
        /// </summary>
        private void Awake()
        {
            inputReader = GetComponent<PlayerInputReader>();
            remainingBatterySeconds = fullBatterySeconds;

            if (spotLight == null)
            {
                SWLog.LogError($"[{nameof(PlayerFlashlight)}] 스포트라이트가 비어 있어 손전등을 켤 수 없습니다.");
                return;
            }

            spotLight.enabled = startTurnedOn;

            SWDebugConsole.RegisterInstance(this);
            SWDebugConsole.Watch("손전등 배터리", () => $"{BatteryRatio:P0} ({(IsTurnedOn ? "켬" : "끔")})");
        }

        /// <summary>
        /// 디버그 등록을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            SWDebugConsole.Unwatch("손전등 배터리");
            SWDebugConsole.UnregisterInstance(this);
        }

        /// <summary>
        /// 입력을 받아 켜고 끄고, 배터리를 소모시키고, 밝기를 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (spotLight == null) return;

            if (inputReader.IsFlashlightPressed) Toggle();

            if (IsTurnedOn == false) return;

            remainingBatterySeconds = Mathf.Max(0f, remainingBatterySeconds - Time.deltaTime);

            if (HasBattery == false)
            {
                spotLight.enabled = false;
                SWLog.Log($"[{nameof(PlayerFlashlight)}] 배터리가 바닥나 손전등이 꺼졌습니다.");
                return;
            }

            spotLight.intensity = fullIntensity * GetIntensityRatio();
        }

        /// <summary>
        /// 시점보다 조금 늦게 따라오도록 손전등을 돌립니다.
        /// </summary>
        private void LateUpdate()
        {
            if (spotLight == null || aimTransform == null) return;

            float scale = Mathf.Lerp(lowBatteryFollowScale, 1f, GetIntensityRatio());
            float speed = followSpeed * scale;

            spotLight.transform.position = aimTransform.position;
            spotLight.transform.rotation = Quaternion.Slerp(spotLight.transform.rotation,
                aimTransform.rotation, 1f - Mathf.Exp(-speed * Time.deltaTime));
        }

        /// <summary>
        /// 손전등을 켜거나 끕니다. 배터리가 없으면 켜지지 않습니다.
        /// </summary>
        [SWCommand("player.flashlight", "손전등을 켜거나 끕니다.", "플레이어")]
        public void Toggle()
        {
            if (spotLight == null) return;

            if (spotLight.enabled)
            {
                spotLight.enabled = false;
                return;
            }

            if (HasBattery == false)
            {
                SWLog.LogWarning($"[{nameof(PlayerFlashlight)}] 배터리가 없어 손전등을 켤 수 없습니다.");
                return;
            }

            spotLight.enabled = true;
        }

        /// <summary>
        /// 배터리를 가득 채웁니다.
        /// </summary>
        [SWCommand("player.battery", "손전등 배터리를 가득 채웁니다.", "플레이어")]
        private void RefillBattery()
        {
            remainingBatterySeconds = fullBatterySeconds;
            SWLog.Log($"[{nameof(PlayerFlashlight)}] 배터리를 가득 채웠습니다.");
        }

        /// <summary>
        /// 남은 배터리에 따른 밝기 비율을 구합니다.
        /// </summary>
        /// <returns>0에서 1 사이의 밝기 비율입니다.</returns>
        private float GetIntensityRatio()
        {
            if (dimStartRatio <= 0f) return 1f;

            float dimProgress = Mathf.Clamp01(BatteryRatio / dimStartRatio);

            return Mathf.Lerp(minimumIntensityRatio, 1f, dimProgress);
        }
        #endregion // 함수
    }
}
