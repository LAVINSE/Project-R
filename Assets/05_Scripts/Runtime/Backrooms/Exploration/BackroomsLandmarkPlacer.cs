using System.Collections.Generic;

using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Backrooms.Assembly;
using ProjectR.Backrooms.Generation;

namespace ProjectR.Backrooms.Exploration
{
    /// <summary>
    /// 맵을 구역으로 나눠 구역마다 랜드마크를 하나씩 놓는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 완전히 똑같은 복도만 이어지면 길을 잃은 것이 긴장이 아니라 짜증이 됩니다.
    /// "여기 아까 왔던 데다"라고 스스로 알아채게 하려면 눈에 걸리는 표식이 골고루 흩어져 있어야 합니다.
    /// 그래서 무작위로 흩뿌리지 않고 구역을 나눠 한 구역에 하나씩 보장합니다.
    /// 배치는 맵 시드로 정해지므로 같은 시드에서는 같은 자리에 같은 랜드마크가 놓입니다.
    /// </remarks>
    public class BackroomsLandmarkPlacer : SWMonoBehaviour
    {
        #region 필드
        /// <summary>생성 결과를 받아 올 맵 조립 컴포넌트입니다.</summary>
        [SWGroup("대상")]
        [SerializeField, Tooltip("생성 결과를 받아 올 맵 조립 컴포넌트입니다.")]
        private BackroomsMapBuilder mapBuilder;

        /// <summary>구역마다 하나씩 골라 놓을 랜드마크 프리팹 목록입니다.</summary>
        [SWGroup("랜드마크")]
        [SerializeField, Tooltip("구역마다 하나씩 골라 놓을 랜드마크 프리팹 목록입니다.")]
        private GameObject[] landmarkPrefabs = new GameObject[0];

        /// <summary>랜드마크를 하나씩 놓을 구역의 한 변 칸 수입니다.</summary>
        [SerializeField, Range(2, 16), Tooltip("랜드마크를 하나씩 놓을 구역의 한 변 칸 수입니다.")]
        private int regionSize = 6;

        /// <summary>배치한 랜드마크를 담아 두는 부모 오브젝트입니다.</summary>
        private Transform landmarkRoot;

        /// <summary>배치해 둔 랜드마크 목록입니다.</summary>
        private readonly List<GameObject> placedLandmarks = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>지금 배치되어 있는 랜드마크 개수입니다.</summary>
        public int PlacedCount => placedLandmarks.Count;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 랜드마크를 담을 부모 오브젝트를 준비합니다.
        /// </summary>
        private void Awake()
        {
            landmarkRoot = new GameObject("LandmarkRoot").transform;
            landmarkRoot.SetParent(transform, false);
        }

        /// <summary>
        /// 맵 조립 완료 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            if (mapBuilder == null)
            {
                SWLog.LogError($"[{nameof(BackroomsLandmarkPlacer)}] 맵 조립 컴포넌트가 비어 있습니다.");
                return;
            }

            mapBuilder.MapBuilt += HandleMapBuilt;
        }

        /// <summary>
        /// 맵 조립 완료 알림을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (mapBuilder == null) return;

            mapBuilder.MapBuilt -= HandleMapBuilt;
        }

        /// <summary>
        /// 구역마다 랜드마크를 하나씩 놓습니다.
        /// </summary>
        /// <param name="result">방금 만들어진 미로 결과입니다.</param>
        private void HandleMapBuilt(MazeBuildResult result)
        {
            ClearLandmarks();

            if (landmarkPrefabs.Length == 0)
            {
                SWLog.LogWarning($"[{nameof(BackroomsLandmarkPlacer)}] 랜드마크 프리팹이 없어 배치를 건너뜁니다.");
                return;
            }

            System.Random random = new(result.Seed);

            for (int regionY = 0; regionY < result.Grid.Height; regionY += regionSize)
            {
                for (int regionX = 0; regionX < result.Grid.Width; regionX += regionSize)
                {
                    PlaceInRegion(result, random, regionX, regionY);
                }
            }

            SWLog.Log($"[{nameof(BackroomsLandmarkPlacer)}] 랜드마크 {placedLandmarks.Count}개를 놓았습니다.");
        }

        /// <summary>
        /// 구역 하나에 랜드마크를 하나 놓습니다.
        /// </summary>
        /// <param name="result">배치 기준이 되는 미로 결과입니다.</param>
        /// <param name="random">배치를 정할 난수기입니다.</param>
        /// <param name="regionX">구역의 시작 X 칸입니다.</param>
        /// <param name="regionY">구역의 시작 Y 칸입니다.</param>
        private void PlaceInRegion(MazeBuildResult result, System.Random random, int regionX, int regionY)
        {
            int width = Mathf.Min(regionSize, result.Grid.Width - regionX);
            int height = Mathf.Min(regionSize, result.Grid.Height - regionY);

            MazeCoordinate coordinate = new(
                regionX + random.Next(width), regionY + random.Next(height));

            // 시작 칸과 탈출 칸은 이미 그 자체로 표식이므로 랜드마크를 겹쳐 놓지 않습니다.
            if (coordinate.Equals(result.StartCoordinate) || coordinate.Equals(result.ExitCoordinate)) return;

            GameObject prefab = landmarkPrefabs[random.Next(landmarkPrefabs.Length)];

            if (prefab == null) return;

            GameObject landmark = Instantiate(prefab, mapBuilder.GetWorldPosition(coordinate),
                Quaternion.Euler(0f, 90f * random.Next(4), 0f), landmarkRoot);

            placedLandmarks.Add(landmark);
        }

        /// <summary>
        /// 배치해 둔 랜드마크를 모두 지웁니다.
        /// </summary>
        private void ClearLandmarks()
        {
            for (int index = 0; index < placedLandmarks.Count; index += 1)
            {
                if (placedLandmarks[index] != null) Destroy(placedLandmarks[index]);
            }

            placedLandmarks.Clear();
        }
        #endregion // 함수
    }
}
