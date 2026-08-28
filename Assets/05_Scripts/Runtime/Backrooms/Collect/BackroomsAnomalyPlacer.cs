using System.Collections.Generic;

using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Backrooms.Assembly;
using ProjectR.Backrooms.Generation;
using ProjectR.Data;

namespace ProjectR.Backrooms.Collect
{
    /// <summary>
    /// 맵을 구역으로 나눠 구역마다 이상물체를 하나씩 놓는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 무작위로 흩뿌리면 한쪽에만 몰려 어떤 구역은 아무것도 없이 지나가게 됩니다.
    /// 고장 난 등과 같은 방식으로 구역마다 하나를 보장하고, 배치는 맵 시드로 정해집니다.
    /// 버린 물건을 월드에 되돌려 놓는 것도 여기서 처리합니다.
    /// 상자를 만드는 자리가 두 곳으로 갈라지면 프리팹 참조도 두 곳이 되기 때문입니다.
    /// </remarks>
    public class BackroomsAnomalyPlacer : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("대상")]
        [SerializeField, Tooltip("생성 결과를 받아 올 맵 조립 컴포넌트입니다.")]
        private BackroomsMapBuilder mapBuilder;

        [SWGroup("에셋")]
        [SerializeField, Tooltip("놓을 수 있는 이상물체 정의를 모아 둔 데이터베이스입니다.")]
        private SWIODatabase anomalyDatabase;

        [SerializeField, Tooltip("월드에 놓을 이상물체 상자 프리팹입니다.")]
        private AnomalyPickup pickupPrefab;

        [SWGroup("배치")]
        [SerializeField, Range(2, 16), Tooltip("이상물체를 하나씩 놓을 구역의 한 변 칸 수입니다.")]
        private int regionSize = 5;

        [SerializeField, Min(0f), Tooltip("칸 가운데에서 얼마나 떨어진 곳까지 놓을지(미터)입니다.")]
        private float placementJitter = 1.2f;

        /// <summary>지금 월드에 놓여 있는 상자 목록입니다.</summary>
        private readonly List<AnomalyPickup> spawnedPickups = new List<AnomalyPickup>();

        /// <summary>버린 물건을 되돌려 놓을 기준이 되는 플레이어입니다.</summary>
        private Transform playerTransform;

        /// <summary>버림 알림을 이어 둔 탐험입니다. 없으면 null입니다.</summary>
        private BackroomsActivity subscribedActivity;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>지금 월드에 놓여 있는 상자 개수입니다.</summary>
        public int SpawnedCount => spawnedPickups.Count;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 맵 조립 완료 알림과 버림 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            if (mapBuilder == null)
            {
                SWLog.LogError($"[{nameof(BackroomsAnomalyPlacer)}] 맵 조립 컴포넌트가 비어 있습니다.");
                return;
            }

            mapBuilder.MapBuilt += HandleMapBuilt;

            if (GameManager.Instance.CurrentActivity is BackroomsActivity activity)
            {
                subscribedActivity = activity;
                subscribedActivity.AnomalyDropped += HandleAnomalyDropped;
            }
        }

        /// <summary>
        /// 구독을 모두 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (mapBuilder != null) mapBuilder.MapBuilt -= HandleMapBuilt;

            if (subscribedActivity == null) return;

            subscribedActivity.AnomalyDropped -= HandleAnomalyDropped;
            subscribedActivity = null;
        }

        /// <summary>
        /// 버린 물건을 놓을 기준이 되는 플레이어를 찾아 둡니다.
        /// </summary>
        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsAnomalyPlacer)}] 플레이어를 찾지 못해 버린 물건을 되돌려 놓을 수 없습니다.");
                return;
            }

            playerTransform = player.transform;
        }

        /// <summary>
        /// 구역마다 이상물체를 하나씩 놓습니다.
        /// </summary>
        /// <param name="result">방금 만들어진 미로 결과입니다.</param>
        private void HandleMapBuilt(MazeBuildResult result)
        {
            ClearSpawned();

            if (anomalyDatabase == null || anomalyDatabase.Count == 0)
            {
                SWLog.LogWarning($"[{nameof(BackroomsAnomalyPlacer)}] 이상물체 데이터베이스가 비어 있어 놓지 않았습니다.");
                return;
            }

            System.Random random = new System.Random(result.Seed);

            for (int regionY = 0; regionY < result.Grid.Height; regionY += regionSize)
            {
                for (int regionX = 0; regionX < result.Grid.Width; regionX += regionSize)
                {
                    PlaceInRegion(result, random, regionX, regionY);
                }
            }

            SWLog.Log($"[{nameof(BackroomsAnomalyPlacer)}] 이상물체 {spawnedPickups.Count}개를 놓았습니다.");
        }

        /// <summary>
        /// 구역 하나에 이상물체를 하나 놓습니다.
        /// </summary>
        /// <param name="result">배치 기준이 되는 미로 결과입니다.</param>
        /// <param name="random">놓을 칸과 종류를 정할 난수기입니다.</param>
        /// <param name="regionX">구역의 시작 X 칸입니다.</param>
        /// <param name="regionY">구역의 시작 Y 칸입니다.</param>
        private void PlaceInRegion(MazeBuildResult result, System.Random random, int regionX, int regionY)
        {
            int width = Mathf.Min(regionSize, result.Grid.Width - regionX);
            int height = Mathf.Min(regionSize, result.Grid.Height - regionY);

            MazeCoordinate coordinate = new MazeCoordinate(
                regionX + random.Next(width), regionY + random.Next(height));

            // 시작 칸에 놓으면 나가기도 전에 주워지고, 탈출 칸에 놓으면 주우려다 나가집니다.
            if (coordinate == result.StartCoordinate || coordinate == result.ExitCoordinate) return;

            AnomalyDefinition definition =
                anomalyDatabase[random.Next(anomalyDatabase.Count)] as AnomalyDefinition;

            if (definition == null) return;

            Vector3 position = mapBuilder.GetWorldPosition(coordinate)
                + new Vector3(NextOffset(random), 0f, NextOffset(random));

            Spawn(definition, position);
        }

        /// <summary>
        /// 이상물체 상자를 지정한 자리에 놓습니다.
        /// </summary>
        /// <param name="definition">놓을 이상물체의 정의입니다.</param>
        /// <param name="position">놓을 월드 좌표입니다.</param>
        /// <returns>놓인 상자입니다. 프리팹이 없으면 null을 반환합니다.</returns>
        public AnomalyPickup Spawn(AnomalyDefinition definition, Vector3 position)
        {
            if (pickupPrefab == null)
            {
                SWLog.LogError($"[{nameof(BackroomsAnomalyPlacer)}] 이상물체 상자 프리팹이 비어 있습니다.");
                return null;
            }

            AnomalyPickup pickup = Instantiate(pickupPrefab, position, Quaternion.identity, transform);

            pickup.Setup(definition);
            spawnedPickups.Add(pickup);

            return pickup;
        }

        /// <summary>
        /// 주워 간 상자를 목록에서 지우고 없앱니다.
        /// </summary>
        /// <param name="pickup">주워 간 상자입니다.</param>
        public void Despawn(AnomalyPickup pickup)
        {
            if (pickup == null) return;

            spawnedPickups.Remove(pickup);
            Destroy(pickup.gameObject);
        }

        /// <summary>
        /// 가방에서 버린 물건을 플레이어 발밑에 되돌려 놓습니다.
        /// </summary>
        /// <param name="definition">버린 물건의 정의입니다.</param>
        private void HandleAnomalyDropped(AnomalyDefinition definition)
        {
            if (playerTransform == null) return;

            Spawn(definition, playerTransform.position + (playerTransform.forward * 0.8f));

            SWLog.Log($"[{nameof(BackroomsAnomalyPlacer)}] {definition.DisplayName}을(를) 발밑에 내려놓았습니다.");
        }

        /// <summary>
        /// 놓여 있던 상자를 모두 치웁니다.
        /// </summary>
        /// <remarks>맵을 다시 만들면 타일과 함께 상자도 의미가 없어지므로 함께 치웁니다.</remarks>
        private void ClearSpawned()
        {
            for (int i = 0; i < spawnedPickups.Count; i++)
            {
                if (spawnedPickups[i] != null) Destroy(spawnedPickups[i].gameObject);
            }

            spawnedPickups.Clear();
        }

        /// <summary>
        /// 칸 가운데에서 벗어날 거리를 뽑습니다.
        /// </summary>
        /// <param name="random">거리를 뽑을 난수기입니다.</param>
        /// <returns>좌우로 흔들 거리(미터)입니다.</returns>
        private float NextOffset(System.Random random)
        {
            return (float)((random.NextDouble() * 2.0) - 1.0) * placementJitter;
        }
        #endregion // 함수
    }
}
