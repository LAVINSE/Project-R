using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Activity.Broadcast;
using ProjectR.Backrooms.Player;
using ProjectR.Enum;
using ProjectR.Inventory;

namespace ProjectR.Backrooms.Collect
{
    /// <summary>
    /// 바라보고 있는 이상물체를 주워 가방에 넣는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 판정은 조준선이 가리키는 한 줄의 광선으로 합니다.
    /// 굵은 광선은 조준이 너그러워지는 대신 화면과 판정이 어긋나고, 시작 지점이 캡슐과 겹쳐 있어
    /// 자기 몸이 언제나 거리 0으로 먼저 잡힙니다. 가는 광선은 시작 지점이 콜라이더 안이면
    /// 그 콜라이더를 무시하므로 평소에는 자기 몸이 잡히지 않습니다.
    /// 다만 캐릭터 컨트롤러를 껐다 켜서 자리를 옮긴 직후에는 물리 쪽 캡슐이 아직 따라오지 않아
    /// 자기 몸이 거리 0으로 잡히는 순간이 있습니다. 맵을 만든 뒤 시작 칸에 놓는 것이 바로 그 방식이라
    /// 탐험을 시작하자마자 주우려 하면 여기에 걸립니다. 그래서 자기 몸은 건너뛰고 그 뒤를 이어서 봅니다.
    /// </remarks>
    public class PlayerPickupInteractor : SWMonoBehaviour
    {
        #region 상수
        /// <summary>한 번에 살펴볼 최대 충돌 수입니다.</summary>
        private const int MaximumHitCount = 8;
        #endregion // 상수

        #region 필드
        /// <summary>어디를 보고 있는지 판정할 시점 카메라입니다.</summary>
        [SWGroup("대상")]
        [SerializeField, Tooltip("어디를 보고 있는지 판정할 시점 카메라입니다.")]
        private Camera viewCamera;

        /// <summary>줍기 입력을 읽어 올 입력 컴포넌트입니다.</summary>
        [SerializeField, Tooltip("줍기 입력을 읽어 올 입력 컴포넌트입니다.")]
        private PlayerInputReader inputReader;

        /// <summary>주워 간 상자를 치울 배치 컴포넌트입니다.</summary>
        [SerializeField, Tooltip("주워 간 상자를 치울 배치 컴포넌트입니다.")]
        private BackroomsAnomalyPlacer anomalyPlacer;

        /// <summary>주울 수 있는 최대 거리(미터)입니다.</summary>
        [SWGroup("판정")]
        [SerializeField, Min(0.5f), Tooltip("주울 수 있는 최대 거리(미터)입니다.")]
        private float reachDistance = 3f;

        /// <summary>줍기 판정에 포함할 레이어입니다.</summary>
        [SerializeField, Tooltip("줍기 판정에 포함할 레이어입니다.")]
        private LayerMask pickupLayers = ~0;

        /// <summary>충돌 결과를 담아 두는 버퍼입니다. 매 프레임 새로 할당하지 않으려고 들고 있습니다.</summary>
        private readonly RaycastHit[] hitBuffer = new RaycastHit[MaximumHitCount];

        /// <summary>지금 바라보고 있는 상자입니다. 없으면 null입니다.</summary>
        private AnomalyPickup focusedPickup;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>지금 바라보고 있는 상자입니다. 없으면 null입니다.</summary>
        public AnomalyPickup FocusedPickup => focusedPickup;

        /// <summary>마지막 줍기 시도가 가방이 꽉 차서 실패했는지 여부입니다.</summary>
        public bool IsBackpackFull { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 바라보는 대상을 갱신하고 줍기 입력을 처리합니다.
        /// </summary>
        private void Update()
        {
            focusedPickup = FindPickupInSight();

            if (focusedPickup == null)
            {
                IsBackpackFull = false;
                return;
            }

            if (inputReader == null || inputReader.IsInteractPressed == false) return;

            TryCollectFocused();
        }

        /// <summary>
        /// 지금 겨누고 있는 상자를 주워 가방에 넣습니다.
        /// </summary>
        /// <returns>주웠으면 true를 반환합니다.</returns>
        /// <remarks>
        /// 입력과 갈라 두었습니다. 키가 눌렸는지와 무엇을 겨누고 있는지는 서로 다른 문제이고,
        /// 이렇게 두면 디버그 명령으로 줍는 처리만 따로 확인할 수 있습니다.
        /// </remarks>
        public bool TryCollectFocused()
        {
            if (focusedPickup == null) return false;

            return Collect(focusedPickup);
        }

        /// <summary>
        /// 조준선이 가리키는 이상물체 상자를 찾습니다.
        /// </summary>
        /// <returns>바라보고 있는 상자입니다. 없으면 null을 반환합니다.</returns>
        /// <remarks>
        /// 자기 몸을 뺀 것 중 가장 가까운 것만 봅니다. 벽이 앞을 가리면 그 뒤의 상자는 줍지 못합니다.
        /// </remarks>
        private AnomalyPickup FindPickupInSight()
        {
            if (viewCamera == null) return null;

            Transform eye = viewCamera.transform;

            int hitCount = Physics.RaycastNonAlloc(eye.position, eye.forward, hitBuffer, reachDistance,
                pickupLayers, QueryTriggerInteraction.Collide);

            AnomalyPickup nearest = null;
            float nearestDistance = float.MaxValue;

            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = hitBuffer[index];

                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (hit.distance >= nearestDistance) continue;

                nearestDistance = hit.distance;
                nearest = hit.collider.GetComponentInParent<AnomalyPickup>();
            }

            return nearest;
        }

        /// <summary>
        /// 상자를 주워 가방에 넣습니다.
        /// </summary>
        /// <param name="pickup">주울 상자입니다.</param>
        /// <returns>주웠으면 true를 반환합니다.</returns>
        private bool Collect(AnomalyPickup pickup)
        {
            if (GameManager.Instance.CurrentActivity is not BackroomsActivity activity) return false;

            if (activity.TryCollect(pickup.Definition, out PlacedItem placed) == false)
            {
                IsBackpackFull = true;
                SWLog.Log($"[{nameof(PlayerPickupInteractor)}] 가방에 자리가 없어 줍지 못했습니다.");
                return false;
            }

            IsBackpackFull = false;
            focusedPickup = null;

            SWLog.Log($"[{nameof(PlayerPickupInteractor)}] {pickup.Definition.DisplayName}을(를) " +
                $"{placed.Position}에 넣었습니다.");

            if (anomalyPlacer != null) anomalyPlacer.Despawn(pickup);
            else Destroy(pickup.gameObject);

            // 방송에 알립니다. 백룸은 이 알림을 누가 듣는지 모르고, 시청자 수도 모릅니다.
            SWEventBus.Publish(new BroadcastMomentEvent(EBroadcastMoment.Collect));

            return true;
        }
        #endregion // 함수
    }
}
