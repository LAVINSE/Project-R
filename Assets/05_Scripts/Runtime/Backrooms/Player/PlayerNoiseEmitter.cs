using UnityEngine;

using SW.Base;
using SW.Util;

namespace ProjectR.Backrooms.Player
{
    /// <summary>
    /// 플레이어가 낸 소리를 반경과 함께 알리는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 걸음 간격을 여기서 다시 세지 않고 <see cref="PlayerFootsteps"/>가 알려 주는 순간에 맞춥니다.
    /// 들리는 소리와 들키는 소리가 어긋나면 플레이어는 왜 들켰는지 알 수 없습니다.
    /// 반경이 0인 자세(앉기)에서는 아무것도 알리지 않아 이벤트 자체가 발생하지 않습니다.
    /// </remarks>
    [RequireComponent(typeof(PlayerFootsteps))]
    [RequireComponent(typeof(PlayerStealth))]
    public class PlayerNoiseEmitter : SWMonoBehaviour
    {
        #region 필드
        /// <summary>걸음을 알려 주는 발소리 컴포넌트입니다.</summary>
        private PlayerFootsteps footsteps;

        /// <summary>자세별 소음 반경을 정해 주는 은신 컴포넌트입니다.</summary>
        private PlayerStealth stealth;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 필요한 컴포넌트를 캐싱합니다.
        /// </summary>
        private void Awake()
        {
            footsteps = GetComponent<PlayerFootsteps>();
            stealth = GetComponent<PlayerStealth>();
        }

        /// <summary>
        /// 걸음 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            footsteps.Stepped += HandleStepped;
        }

        /// <summary>
        /// 걸음 알림을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            footsteps.Stepped -= HandleStepped;
        }

        /// <summary>
        /// 한 걸음마다 지금 자세의 반경으로 소리를 알립니다.
        /// </summary>
        private void HandleStepped()
        {
            float radius = stealth.GetNoiseRadius();

            if (radius <= 0f) return;

            // 걸음은 자주 발생하므로 이벤트 버스 로그를 끕니다. 켜 두면 로그만으로 프레임이 흔들립니다.
            SWEventBus.Publish(new NoiseEmittedEvent(transform.position, radius), false);
        }
        #endregion // 함수
    }
}
