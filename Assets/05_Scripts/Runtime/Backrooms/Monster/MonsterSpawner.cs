using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

using SW.Attributes;
using SW.BehaviourTree;
using SW.Base;
using SW.Debugging;
using SW.Util;

using ProjectR.Backrooms.Assembly;
using ProjectR.Backrooms.Generation;

namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// NavMesh가 준비된 뒤 몬스터를 맵에 놓는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 몬스터를 시작 칸 근처에 놓으면 탐험을 시작하자마자 쫓기게 되어
    /// 길을 잃는 긴장과 쫓기는 긴장이 겹쳐 버립니다. 그래서 시작 칸에서 충분히 떨어진 칸에 놓습니다.
    /// 놓을 자리는 맵 시드로 정해지므로 같은 시드에서는 같은 자리에서 시작합니다.
    /// 매복 자리는 탈출 지점으로 잡습니다. 플레이어가 반드시 한 번은 가야 하는 곳이라
    /// 기다리고 있었다는 인상이 가장 강하게 남습니다.
    /// </remarks>
    public class MonsterSpawner : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("대상")]
        [SerializeField, Tooltip("NavMesh 굽기 완료를 알려 줄 컴포넌트입니다.")]
        private BackroomsNavMeshBaker navMeshBaker;

        [SerializeField, Tooltip("칸 좌표를 월드 위치로 바꿔 줄 맵 조립 컴포넌트입니다.")]
        private BackroomsMapBuilder mapBuilder;

        [SWGroup("몬스터")]
        [SerializeField, Tooltip("맵에 놓을 몬스터 프리팹입니다.")]
        private GameObject monsterPrefab;

        [SWGroup("배치")]
        [SerializeField, Min(1), Tooltip("시작 칸에서 최소 몇 칸 떨어진 곳에 놓을지입니다.")]
        private int minimumStartCellDistance = 10;

        [SerializeField, Min(1f), Tooltip("놓을 자리를 NavMesh 위로 끌어올 때 허용할 거리(미터)입니다.")]
        private float navMeshSampleDistance = 4f;

        /// <summary>맵에 놓아 둔 몬스터입니다. 없으면 null입니다.</summary>
        private GameObject monsterInstance;

        /// <summary>놓아 둔 몬스터의 몸입니다. 없으면 null입니다.</summary>
        private MonsterAgent monsterAgent;

        /// <summary>놓아 둔 몬스터의 Behaviour Tree 실행기입니다. 없으면 null입니다.</summary>
        private SWBehaviourTreeRunner monsterRunner;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>맵에 몬스터가 놓여 있는지 여부입니다.</summary>
        public bool HasMonster => monsterInstance != null;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 디버그 명령과 감시 항목을 등록합니다.
        /// </summary>
        private void Awake()
        {
            SWDebugConsole.RegisterInstance(this);
            SWDebugConsole.Watch("몬스터 실행 노드", GetRunningNodeText);
            SWDebugConsole.Watch("몬스터 Blackboard", GetBlackboardText);
        }

        /// <summary>
        /// NavMesh 굽기 완료 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            if (navMeshBaker == null)
            {
                SWLog.LogError($"[{nameof(MonsterSpawner)}] NavMesh 굽기 컴포넌트가 비어 있습니다.");
                return;
            }

            navMeshBaker.NavMeshBuilt += HandleNavMeshBuilt;
        }

        /// <summary>
        /// NavMesh 굽기 완료 알림을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (navMeshBaker == null) return;

            navMeshBaker.NavMeshBuilt -= HandleNavMeshBuilt;
        }

        /// <summary>
        /// 디버그 명령과 감시 항목을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            SWDebugConsole.Unwatch("몬스터 실행 노드");
            SWDebugConsole.Unwatch("몬스터 Blackboard");
            SWDebugConsole.UnregisterInstance(this);
        }

        /// <summary>
        /// NavMesh가 준비되면 몬스터를 놓습니다.
        /// </summary>
        /// <param name="result">방금 만들어진 미로 결과입니다.</param>
        private void HandleNavMeshBuilt(MazeBuildResult result)
        {
            if (monsterPrefab == null)
            {
                SWLog.LogWarning($"[{nameof(MonsterSpawner)}] 몬스터 프리팹이 비어 있어 놓지 못했습니다.");
                return;
            }

            MazeCoordinate coordinate = PickSpawnCoordinate(result);
            Vector3 position = mapBuilder.GetWorldPosition(coordinate);

            PlaceMonster(position);
            SetAmbushPosition(mapBuilder.GetWorldPosition(result.ExitCoordinate));

            SWLog.Log($"[{nameof(MonsterSpawner)}] 몬스터를 {coordinate}에 놓았습니다. " +
                $"매복 자리는 탈출 칸 {result.ExitCoordinate}입니다.");
        }

        /// <summary>
        /// 시작 칸에서 충분히 떨어진 칸을 하나 고릅니다.
        /// </summary>
        /// <param name="result">칸을 고를 미로 결과입니다.</param>
        /// <returns>몬스터를 놓을 칸의 좌표입니다.</returns>
        /// <remarks>조건을 만족하는 칸이 없으면 가장 멀리 떨어진 칸을 씁니다.</remarks>
        private MazeCoordinate PickSpawnCoordinate(MazeBuildResult result)
        {
            List<MazeCoordinate> candidates = new List<MazeCoordinate>();
            MazeCoordinate farthest = result.StartCoordinate;
            int farthestDistance = -1;

            foreach (MazeCoordinate coordinate in result.Grid.EnumerateCoordinates())
            {
                if (coordinate == result.ExitCoordinate) continue;

                int distance = Mathf.Abs(coordinate.X - result.StartCoordinate.X)
                    + Mathf.Abs(coordinate.Y - result.StartCoordinate.Y);

                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthest = coordinate;
                }

                if (distance >= minimumStartCellDistance) candidates.Add(coordinate);
            }

            if (candidates.Count == 0) return farthest;

            System.Random random = new System.Random(result.Seed);

            return candidates[random.Next(candidates.Count)];
        }

        /// <summary>
        /// 몬스터를 지정한 위치에 놓습니다. 이미 있으면 자리만 옮깁니다.
        /// </summary>
        /// <param name="position">놓을 월드 위치입니다.</param>
        private void PlaceMonster(Vector3 position)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleDistance,
                    NavMesh.AllAreas))
            {
                position = hit.position;
            }

            if (monsterInstance == null)
            {
                monsterInstance = Instantiate(monsterPrefab, position, Quaternion.identity, transform);
                monsterAgent = monsterInstance.GetComponent<MonsterAgent>();
                monsterRunner = monsterInstance.GetComponent<SWBehaviourTreeRunner>();
            }
            else
            {
                MoveMonster(position);
            }

            if (monsterAgent != null) monsterAgent.SetAnchor(position);
        }

        /// <summary>
        /// 이미 놓여 있는 몬스터를 다른 자리로 옮깁니다.
        /// </summary>
        /// <param name="position">옮길 월드 위치입니다.</param>
        /// <remarks>길찾기 컴포넌트는 자기 위치를 스스로 관리하므로 Warp로 옮겨야 합니다.</remarks>
        private void MoveMonster(Vector3 position)
        {
            NavMeshAgent navMeshAgent = monsterInstance.GetComponent<NavMeshAgent>();

            if (navMeshAgent != null && navMeshAgent.isOnNavMesh) navMeshAgent.Warp(position);
            else monsterInstance.transform.position = position;
        }

        /// <summary>
        /// 매복 자리를 Blackboard에 적습니다.
        /// </summary>
        /// <param name="position">매복할 월드 위치입니다.</param>
        private void SetAmbushPosition(Vector3 position)
        {
            if (monsterRunner == null) return;

            monsterRunner.SetBlackboardValue(MonsterBlackboardKeys.AmbushPosition, position);
            monsterRunner.SetBlackboardValue(MonsterBlackboardKeys.HasAmbushPosition, true);
        }

        /// <summary>
        /// 지금 실행 중인 행동 노드의 이름을 구합니다.
        /// </summary>
        /// <returns>실행 중인 노드 이름입니다. 몬스터가 없으면 안내 문구를 반환합니다.</returns>
        private string GetRunningNodeText()
        {
            if (monsterRunner == null || monsterRunner.RuntimeTree == null) return "없음";

            IReadOnlyList<SWBehaviourNode> nodes = monsterRunner.RuntimeTree.Nodes;

            for (int index = 0; index < nodes.Count; index += 1)
            {
                if (nodes[index] is not SWBehaviourActionNode) continue;
                if (nodes[index].Status != SWBehaviourStatus.Running) continue;

                return nodes[index].DisplayName;
            }

            return "대기";
        }

        /// <summary>
        /// 몬스터의 판단에 쓰이는 Blackboard 값을 한 줄로 구합니다.
        /// </summary>
        /// <returns>모드와 감지 상태를 담은 문자열입니다.</returns>
        private string GetBlackboardText()
        {
            if (monsterRunner == null || monsterAgent == null) return "없음";

            bool canSeePlayer = monsterRunner.GetBlackboardValue(
                MonsterBlackboardKeys.CanSeePlayer, false);
            bool hasLastSeen = monsterRunner.GetBlackboardValue(
                MonsterBlackboardKeys.HasLastSeen, false);
            bool hasHeard = monsterRunner.GetBlackboardValue(MonsterBlackboardKeys.HasHeard, false);

            return $"{monsterAgent.Mode} / 시야 {(canSeePlayer ? "O" : "X")} / " +
                $"목격 {(hasLastSeen ? "O" : "X")} / 소리 {(hasHeard ? "O" : "X")}";
        }

        /// <summary>
        /// 디버그 콘솔에서 몬스터를 플레이어 근처로 불러옵니다.
        /// </summary>
        /// <param name="distance">플레이어에게서 떨어뜨릴 거리(미터)입니다.</param>
        [SWCommand("monster.spawn", "몬스터를 플레이어에게서 지정한 거리에 불러옵니다.", "몬스터")]
        private void SpawnNearPlayer(float distance)
        {
            if (navMeshBaker == null || navMeshBaker.IsBaked == false)
            {
                SWLog.LogWarning($"[{nameof(MonsterSpawner)}] NavMesh가 아직 없어 부를 수 없습니다.");
                return;
            }

            if (monsterPrefab == null)
            {
                SWLog.LogWarning($"[{nameof(MonsterSpawner)}] 몬스터 프리팹이 비어 있습니다.");
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                SWLog.LogWarning($"[{nameof(MonsterSpawner)}] 플레이어를 찾지 못했습니다.");
                return;
            }

            Vector2 offset = Random.insideUnitCircle.normalized * Mathf.Max(1f, distance);
            Vector3 position = player.transform.position + new Vector3(offset.x, 0f, offset.y);

            PlaceMonster(position);

            SWLog.Log($"[{nameof(MonsterSpawner)}] 몬스터를 플레이어에게서 {distance:F0}m 떨어진 곳에 불렀습니다.");
        }

        /// <summary>
        /// 디버그 콘솔에서 몬스터를 멈춰 세우거나 다시 움직이게 합니다.
        /// </summary>
        /// <param name="frozen">1이면 멈추고 0이면 다시 움직입니다.</param>
        [SWCommand("monster.freeze", "몬스터를 멈춰 세웁니다. 1이면 정지, 0이면 해제입니다.", "몬스터")]
        private void FreezeMonster(int frozen)
        {
            if (monsterAgent == null)
            {
                SWLog.LogWarning($"[{nameof(MonsterSpawner)}] 몬스터가 없어 멈출 수 없습니다.");
                return;
            }

            monsterAgent.SetFrozen(frozen != 0);

            SWLog.Log($"[{nameof(MonsterSpawner)}] 몬스터를 {(frozen != 0 ? "멈췄습니다" : "다시 움직입니다")}.");
        }
        #endregion // 함수
    }
}
