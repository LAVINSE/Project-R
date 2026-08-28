using System;

using UnityEngine;
using UnityEngine.AI;

using SW.Attributes;
using SW.Base;
using SW.Util;

namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// 몬스터의 몸입니다. 이동과 현재 행동 모드를 관리합니다.
    /// </summary>
    /// <remarks>
    /// Behaviour Tree 노드가 <see cref="NavMeshAgent"/>를 직접 만지면 노드마다 이동 규칙이 흩어집니다.
    /// 그래서 "어디로 얼마나 빠르게 간다"는 여기 한 곳에만 두고 노드는 그것을 시키기만 합니다.
    /// 충돌체는 일부러 달지 않았습니다. 몬스터는 플레이어를 거리로 붙잡고 지형은 NavMesh가 막으므로,
    /// 충돌체가 있으면 캐릭터 컨트롤러를 밀어내는 일만 생깁니다.
    /// </remarks>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterAgent : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("도착 판정")]
        [SerializeField, Min(0.1f), Tooltip("목적지에 닿았다고 볼 거리(미터)입니다.")]
        private float arrivalDistance = 1.2f;

        [SWGroup("경로 찾기")]
        [SerializeField, Min(1f), Tooltip("무작위 지점을 NavMesh 위로 끌어올 때 허용할 거리(미터)입니다.")]
        private float navMeshSampleDistance = 4f;

        /// <summary>실제 이동을 수행하는 길찾기 컴포넌트입니다.</summary>
        private NavMeshAgent navMeshAgent;

        /// <summary>배회를 시작한 기준 자리입니다.</summary>
        private Vector3 anchorPosition;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>지금 수행 중인 행동 모드입니다.</summary>
        public EMonsterMode Mode { get; private set; } = EMonsterMode.None;

        /// <summary>디버그 명령으로 멈춰 세워 두었는지 여부입니다.</summary>
        public bool IsFrozen { get; private set; }

        /// <summary>배회를 시작한 기준 자리입니다.</summary>
        public Vector3 AnchorPosition => anchorPosition;

        /// <summary>지금 이동하고 있는 속력(m/s)입니다.</summary>
        public float CurrentSpeed => navMeshAgent.velocity.magnitude;

        /// <summary>NavMesh 위에 올라가 있어 이동할 수 있는지 여부입니다.</summary>
        public bool IsReady => navMeshAgent.isOnNavMesh;
        #endregion // 프로퍼티

        #region 이벤트
        /// <summary>행동 모드가 실제로 바뀌었을 때 이전 모드와 새 모드를 담아 발생합니다.</summary>
        public event Action<EMonsterMode, EMonsterMode> ModeChanged;
        #endregion // 이벤트

        #region 함수
        /// <summary>
        /// 길찾기 컴포넌트를 캐싱하고 지금 자리를 기준 자리로 잡습니다.
        /// </summary>
        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            anchorPosition = transform.position;
        }

        /// <summary>
        /// 배회의 기준이 될 자리를 정합니다.
        /// </summary>
        /// <param name="position">기준으로 삼을 월드 위치입니다.</param>
        public void SetAnchor(Vector3 position)
        {
            anchorPosition = position;
        }

        /// <summary>
        /// 행동 모드를 바꾸고 실제로 바뀌었을 때만 알립니다.
        /// </summary>
        /// <param name="mode">새로 들어갈 행동 모드입니다.</param>
        /// <remarks>같은 모드로 다시 들어오는 것은 변화가 아니므로 알리지 않습니다.</remarks>
        public void SetMode(EMonsterMode mode)
        {
            if (Mode == mode) return;

            EMonsterMode previousMode = Mode;
            Mode = mode;

            ModeChanged?.Invoke(previousMode, mode);
            SWEventBus.Publish(new MonsterModeChangedEvent(previousMode, mode), false);
        }

        /// <summary>
        /// 지정한 속도로 목적지를 향해 움직이게 합니다.
        /// </summary>
        /// <param name="destination">향할 월드 위치입니다.</param>
        /// <param name="speed">이동 속도(m/s)입니다.</param>
        /// <returns>경로를 받아들였으면 true를 반환합니다.</returns>
        public bool MoveTo(Vector3 destination, float speed)
        {
            if (IsFrozen || navMeshAgent.isOnNavMesh == false) return false;

            navMeshAgent.speed = speed;
            navMeshAgent.isStopped = false;

            return navMeshAgent.SetDestination(destination);
        }

        /// <summary>
        /// 제자리에 멈춰 세웁니다.
        /// </summary>
        public void Stop()
        {
            if (navMeshAgent.isOnNavMesh == false) return;

            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
        }

        /// <summary>
        /// 목적지에 닿았는지 확인합니다.
        /// </summary>
        /// <returns>경로 계산이 끝났고 남은 거리가 도착 판정 안이면 true를 반환합니다.</returns>
        public bool HasArrived()
        {
            if (navMeshAgent.isOnNavMesh == false) return true;
            if (navMeshAgent.pathPending) return false;

            return navMeshAgent.remainingDistance <= arrivalDistance;
        }

        /// <summary>
        /// 지정한 자리를 중심으로 NavMesh 위의 무작위 지점을 하나 찾습니다.
        /// </summary>
        /// <param name="center">중심이 될 월드 위치입니다.</param>
        /// <param name="radius">중심에서 떨어질 최대 거리(미터)입니다.</param>
        /// <param name="point">찾은 지점입니다. 찾지 못하면 중심을 그대로 돌려줍니다.</param>
        /// <returns>지점을 찾았으면 true를 반환합니다.</returns>
        public bool TryFindPointNear(Vector3 center, float radius, out Vector3 point)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }

            point = center;
            return false;
        }

        /// <summary>
        /// 몬스터를 멈춰 세우거나 다시 움직이게 합니다.
        /// </summary>
        /// <param name="frozen">true이면 멈춰 세웁니다.</param>
        /// <remarks>몬스터를 끄지 않고 멈추기만 하므로 감지와 트리는 그대로 돌아갑니다.</remarks>
        public void SetFrozen(bool frozen)
        {
            IsFrozen = frozen;

            if (frozen) Stop();
        }
        #endregion // 함수
    }
}
