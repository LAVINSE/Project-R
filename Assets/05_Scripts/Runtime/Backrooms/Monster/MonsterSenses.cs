using UnityEngine;

using SW.Attributes;
using SW.BehaviourTree;
using SW.Base;
using SW.Util;

using ProjectR.Backrooms.Player;

namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// 몬스터의 눈과 귀입니다. 감지 결과를 Blackboard에 적어 둡니다.
    /// </summary>
    /// <remarks>
    /// 감지는 추격 중에도 계속 돌아야 하므로 Behaviour Tree 노드가 아니라 컴포넌트로 두었습니다.
    /// 노드는 Blackboard에 적힌 결과만 읽습니다. 이렇게 두면 감지 방식이 바뀌어도 트리는 그대로입니다.
    /// 감지 거리와 시야각은 Blackboard에서 읽으므로 몬스터 유형별 차이는 Override로만 처리됩니다.
    /// 매 프레임 광선을 쏘면 비싸므로 정해진 간격으로만 확인합니다.
    /// </remarks>
    [RequireComponent(typeof(SWBehaviourTreeRunner))]
    [RequireComponent(typeof(MonsterMemory))]
    public class MonsterSenses : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("대상")]
        [SerializeField, Tooltip("감지 대상으로 삼을 태그입니다.")]
        private string playerTag = "Player";

        [SWGroup("감지")]
        [SerializeField, Min(0.02f), Tooltip("감지를 다시 확인하는 간격(초)입니다.")]
        private float checkInterval = 0.1f;

        [SerializeField, Min(0f), Tooltip("몬스터의 눈높이(미터)입니다.")]
        private float eyeHeight = 1.7f;

        [SerializeField, Tooltip("시선을 막는 것으로 볼 레이어입니다.")]
        private LayerMask obstacleMask = ~0;

        [SWGroup("기억")]
        [SerializeField, Min(1f), Tooltip("숨는 것을 봤다고 인정할 최대 거리(미터)입니다.")]
        private float hidingRecordDistance = 20f;

        /// <summary>감지 결과를 적어 둘 Behaviour Tree 실행기입니다.</summary>
        private SWBehaviourTreeRunner runner;

        /// <summary>기억을 맡길 컴포넌트입니다.</summary>
        private MonsterMemory memory;

        /// <summary>감지 대상 플레이어입니다. 찾지 못하면 null입니다.</summary>
        private Transform playerTransform;

        /// <summary>자세별 은신 규칙을 알려 주는 플레이어 컴포넌트입니다.</summary>
        private PlayerStealth playerStealth;

        /// <summary>시선 판정에서 플레이어 자신을 걸러 내기 위한 뿌리 오브젝트입니다.</summary>
        private Transform playerRoot;

        /// <summary>다음 감지까지 남은 시간입니다.</summary>
        private float checkCooldown;

        /// <summary>직전 감지에서 플레이어가 숨어 있었는지 여부입니다.</summary>
        private bool wasHidden;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>가장 최근 감지에서 플레이어가 보였는지 여부입니다.</summary>
        public bool CanSeePlayer { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 필요한 컴포넌트를 캐싱합니다.
        /// </summary>
        private void Awake()
        {
            runner = GetComponent<SWBehaviourTreeRunner>();
            memory = GetComponent<MonsterMemory>();
        }

        /// <summary>
        /// 감지 대상 플레이어를 찾아 둡니다.
        /// </summary>
        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);

            if (player == null)
            {
                SWLog.LogWarning($"[{nameof(MonsterSenses)}] 태그 {playerTag}인 플레이어를 찾지 못했습니다.");
                return;
            }

            playerTransform = player.transform;
            playerRoot = player.transform.root;
            playerStealth = player.GetComponent<PlayerStealth>();

            if (playerStealth == null)
            {
                SWLog.LogWarning($"[{nameof(MonsterSenses)}] 플레이어에 {nameof(PlayerStealth)}가 없어 " +
                    "은신이 적용되지 않습니다.");
            }
        }

        /// <summary>
        /// 소리 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            SWEventBus.Subscribe<NoiseEmittedEvent>(HandleNoiseEmitted);
        }

        /// <summary>
        /// 소리 알림을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            SWEventBus.Unsubscribe<NoiseEmittedEvent>(HandleNoiseEmitted);
        }

        /// <summary>
        /// 정해진 간격마다 시야를 확인합니다.
        /// </summary>
        private void Update()
        {
            if (playerTransform == null) return;

            checkCooldown -= Time.deltaTime;

            if (checkCooldown > 0f) return;

            checkCooldown = checkInterval;

            UpdateSight();
        }

        /// <summary>
        /// 시야 판정을 한 번 수행하고 결과를 Blackboard에 적습니다.
        /// </summary>
        private void UpdateSight()
        {
            Vector3 playerPosition = playerTransform.position;

            runner.SetBlackboardValue(MonsterBlackboardKeys.PlayerPosition, playerPosition);

            CanSeePlayer = IsPlayerVisible(playerPosition);

            runner.SetBlackboardValue(MonsterBlackboardKeys.CanSeePlayer, CanSeePlayer);

            if (CanSeePlayer)
            {
                runner.SetBlackboardValue(MonsterBlackboardKeys.LastSeenPosition, playerPosition);
                runner.SetBlackboardValue(MonsterBlackboardKeys.HasLastSeen, true);
            }

            UpdateHidingMemory(playerPosition);
        }

        /// <summary>
        /// 지금 플레이어가 보이는지 판정합니다.
        /// </summary>
        /// <param name="playerPosition">플레이어의 현재 위치입니다.</param>
        /// <returns>거리, 각도, 시선이 모두 통과하면 true를 반환합니다.</returns>
        private bool IsPlayerVisible(Vector3 playerPosition)
        {
            float sightRange = runner.GetBlackboardValue(MonsterBlackboardKeys.SightRange, 14f);

            if (playerStealth != null) sightRange *= playerStealth.SightRangeMultiplier;

            Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
            Vector3 toPlayer = playerPosition - eyePosition;

            if (toPlayer.sqrMagnitude > sightRange * sightRange) return false;

            float sightAngle = runner.GetBlackboardValue(MonsterBlackboardKeys.SightAngle, 110f);
            Vector3 flatDirection = new Vector3(toPlayer.x, 0f, toPlayer.z);

            if (Vector3.Angle(transform.forward, flatDirection) > sightAngle * 0.5f) return false;

            return HasLineOfSight(eyePosition);
        }

        /// <summary>
        /// 눈에서 플레이어 몸의 표본 지점 중 하나라도 시선이 닿는지 확인합니다.
        /// </summary>
        /// <param name="eyePosition">몬스터의 눈 위치입니다.</param>
        /// <returns>한 곳이라도 닿으면 true를 반환합니다.</returns>
        private bool HasLineOfSight(Vector3 eyePosition)
        {
            if (playerStealth == null)
                return IsPointVisible(eyePosition, playerTransform.position + Vector3.up);

            Vector3[] points = playerStealth.GetVisibilityPoints();

            for (int index = 0; index < points.Length; index += 1)
            {
                if (IsPointVisible(eyePosition, points[index])) return true;
            }

            return false;
        }

        /// <summary>
        /// 눈에서 한 지점까지 시선이 막히지 않았는지 확인합니다.
        /// </summary>
        /// <param name="eyePosition">몬스터의 눈 위치입니다.</param>
        /// <param name="targetPosition">확인할 몸의 표본 지점입니다.</param>
        /// <returns>막히지 않았으면 true를 반환합니다.</returns>
        /// <remarks>
        /// 표본 지점은 플레이어 몸 안에 있으므로 광선은 플레이어 충돌체에 먼저 맞습니다.
        /// 그래서 아무것도 맞지 않았거나 맞은 것이 플레이어일 때만 보이는 것으로 봅니다.
        /// </remarks>
        private bool IsPointVisible(Vector3 eyePosition, Vector3 targetPosition)
        {
            if (Physics.Linecast(eyePosition, targetPosition, out RaycastHit hit,
                    obstacleMask, QueryTriggerInteraction.Ignore) == false)
            {
                return true;
            }

            return hit.transform.root == playerRoot;
        }

        /// <summary>
        /// 플레이어가 앉아서 시야 밖으로 사라진 자리를 기억해 둡니다.
        /// </summary>
        /// <param name="playerPosition">플레이어의 현재 위치입니다.</param>
        private void UpdateHidingMemory(Vector3 playerPosition)
        {
            if (playerStealth == null) return;

            bool isHidden = playerStealth.IsCrouching && CanSeePlayer == false &&
                Vector3.Distance(transform.position, playerPosition) <= hidingRecordDistance;

            // 숨어 있는 내내 세면 한 자리의 횟수만 부풀어 오릅니다. 숨기 시작한 순간에만 한 번 셉니다.
            if (isHidden && wasHidden == false) memory.RecordHidingSpot(playerPosition);

            wasHidden = isHidden;
        }

        /// <summary>
        /// 들린 소리를 Blackboard에 적습니다.
        /// </summary>
        /// <param name="noiseEvent">소리가 난 위치와 반경입니다.</param>
        private void HandleNoiseEmitted(NoiseEmittedEvent noiseEvent)
        {
            float hearingRange = runner.GetBlackboardValue(MonsterBlackboardKeys.HearingRange, 22f);
            float audibleRange = Mathf.Min(noiseEvent.Radius, hearingRange);
            float distance = Vector3.Distance(transform.position, noiseEvent.Position);

            if (distance > audibleRange) return;

            runner.SetBlackboardValue(MonsterBlackboardKeys.HeardPosition, noiseEvent.Position);
            runner.SetBlackboardValue(MonsterBlackboardKeys.HasHeard, true);
        }
        #endregion // 함수
    }
}
