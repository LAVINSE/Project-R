using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Debugging;

namespace ProjectR.Backrooms.Player
{
    /// <summary>
    /// 플레이어가 얼마나 눈에 띄고 얼마나 시끄러운지를 한곳에서 정하는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 은신 규칙을 몬스터 쪽에 두면 몬스터가 늘어날 때마다 같은 규칙을 다시 쓰게 됩니다.
    /// 그래서 "앉으면 조용하고 잘 안 보인다"는 규칙은 플레이어 쪽에 한 벌만 둡니다.
    /// 오브젝트 뒤에 숨는 것과 시야 차단은 규칙이 아니라 실제 지형이 막는 것이므로
    /// 몬스터의 시선 판정이 처리하고, 여기서는 판정에 쓸 몸의 표본 지점만 넘겨 줍니다.
    /// </remarks>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerStealth : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("소음 반경")]
        [SerializeField, Min(0f), Tooltip("걸을 때 발소리가 들리는 반경(미터)입니다.")]
        private float walkNoiseRadius = 12f;

        [SerializeField, Min(0f), Tooltip("달릴 때 발소리가 들리는 반경(미터)입니다.")]
        private float runNoiseRadius = 22f;

        [SerializeField, Min(0f), Tooltip("앉아서 움직일 때 발소리가 들리는 반경(미터)입니다. 0이면 무음입니다.")]
        private float crouchNoiseRadius = 0f;

        [SWGroup("눈에 띄는 정도")]
        [SerializeField, Range(0.1f, 1f), Tooltip("앉았을 때 몬스터의 시야 거리에 곱할 값입니다.")]
        private float crouchSightMultiplier = 0.55f;

        [SerializeField, Min(0f), Tooltip("서 있을 때 눈높이(미터)입니다. 시선 판정의 가장 높은 표본입니다.")]
        private float standingEyeHeight = 1.65f;

        [SerializeField, Min(0f), Tooltip("앉았을 때 눈높이(미터)입니다.")]
        private float crouchingEyeHeight = 1.0f;

        /// <summary>자세를 읽어 올 이동 컴포넌트입니다.</summary>
        private PlayerController playerController;

        /// <summary>시선 판정에 넘겨 줄 표본 지점 버퍼입니다. 매번 새로 만들지 않으려고 들고 있습니다.</summary>
        private readonly Vector3[] visibilityPoints = new Vector3[3];
        #endregion // 필드

        #region 프로퍼티
        /// <summary>앉아서 몸을 숨기고 있는지 여부입니다.</summary>
        public bool IsCrouching => playerController.IsCrouching;

        /// <summary>지금 자세에서 몬스터의 시야 거리에 곱할 값입니다.</summary>
        public float SightRangeMultiplier => IsCrouching ? crouchSightMultiplier : 1f;

        /// <summary>지금 자세에서 눈높이(미터)입니다.</summary>
        public float EyeHeight => IsCrouching ? crouchingEyeHeight : standingEyeHeight;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 필요한 컴포넌트를 캐싱하고 디버그 감시 항목을 등록합니다.
        /// </summary>
        private void Awake()
        {
            playerController = GetComponent<PlayerController>();

            SWDebugConsole.Watch("플레이어 소음 반경", () => $"{GetNoiseRadius():F0} m");
        }

        /// <summary>
        /// 디버그 감시 항목을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            SWDebugConsole.Unwatch("플레이어 소음 반경");
        }

        /// <summary>
        /// 지금 자세에서 한 걸음이 들리는 반경을 구합니다.
        /// </summary>
        /// <returns>소리가 들리는 반경(미터)입니다. 0이면 들리지 않습니다.</returns>
        public float GetNoiseRadius()
        {
            if (IsCrouching) return crouchNoiseRadius;

            return playerController.IsRunning ? runNoiseRadius : walkNoiseRadius;
        }

        /// <summary>
        /// 몬스터의 시선 판정에 쓸 몸의 표본 지점을 구합니다.
        /// </summary>
        /// <returns>월드 좌표 표본 지점 배열입니다. 호출한 쪽은 값을 바꾸지 않아야 합니다.</returns>
        /// <remarks>
        /// 한 점만 보면 벽 모서리에 걸쳐 있을 때 판정이 딱딱 끊깁니다.
        /// 머리, 가슴, 발을 함께 보되 앉으면 몸이 낮아지므로 표본도 함께 낮아집니다.
        /// </remarks>
        public Vector3[] GetVisibilityPoints()
        {
            Vector3 origin = transform.position;
            float eyeHeight = EyeHeight;

            visibilityPoints[0] = origin + Vector3.up * eyeHeight;
            visibilityPoints[1] = origin + Vector3.up * (eyeHeight * 0.6f);
            visibilityPoints[2] = origin + Vector3.up * 0.25f;

            return visibilityPoints;
        }
        #endregion // 함수
    }
}
