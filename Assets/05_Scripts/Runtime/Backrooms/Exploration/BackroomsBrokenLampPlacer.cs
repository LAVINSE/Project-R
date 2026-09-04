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
    /// 맵을 구역으로 나눠 구역마다 형광등 하나를 고장 낸 것처럼 깜빡이게 만드는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 똑같은 복도만 이어지면 길을 잃은 것이 긴장이 아니라 짜증이 됩니다.
    /// 깜빡이는 등은 시야에서 유일하게 움직이는 밝기라 "여기 아까 왔던 데다"라고 스스로 알아채게 해 줍니다.
    /// 무작위로 흩뿌리면 한쪽에만 몰릴 수 있으므로 구역을 나눠 한 구역에 하나씩 보장합니다.
    /// 배치는 맵 시드로 정해지므로 같은 시드에서는 같은 등이 깜빡입니다.
    /// 구워 둔 조도는 바뀌지 않고 등의 발광 색만 바뀌므로 다시 굽지 않아도 됩니다.
    /// </remarks>
    public class BackroomsBrokenLampPlacer : SWMonoBehaviour
    {
        #region 상수
        /// <summary>타일 안에서 형광등 발광 메시가 있는 자리입니다.</summary>
        private const string LampMeshPath = "FluorescentLamp/Tube";
        #endregion // 상수

        #region 필드
        /// <summary>생성 결과를 받아 올 맵 조립 컴포넌트입니다.</summary>
        [SWGroup("대상")]
        [SerializeField, Tooltip("생성 결과를 받아 올 맵 조립 컴포넌트입니다.")]
        private BackroomsMapBuilder mapBuilder;

        /// <summary>고장 난 등을 하나씩 놓을 구역의 한 변 칸 수입니다.</summary>
        [SWGroup("배치")]
        [SerializeField, Range(2, 16), Tooltip("고장 난 등을 하나씩 놓을 구역의 한 변 칸 수입니다.")]
        private int regionSize = 6;

        /// <summary>고장 내 둔 등의 목록입니다.</summary>
        private readonly List<FlickeringLamp> brokenLamps = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>지금 깜빡이고 있는 등의 개수입니다.</summary>
        public int BrokenLampCount => brokenLamps.Count;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 맵 조립 완료 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            if (mapBuilder == null)
            {
                SWLog.LogError($"[{nameof(BackroomsBrokenLampPlacer)}] 맵 조립 컴포넌트가 비어 있습니다.");
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
        /// 구역마다 등을 하나씩 골라 고장 냅니다.
        /// </summary>
        /// <param name="result">방금 만들어진 미로 결과입니다.</param>
        private void HandleMapBuilt(MazeBuildResult result)
        {
            // 타일은 맵을 다시 만들 때 통째로 사라지므로 목록만 비우면 됩니다.
            brokenLamps.Clear();

            System.Random random = new(result.Seed);

            for (int regionY = 0; regionY < result.Grid.Height; regionY += regionSize)
            {
                for (int regionX = 0; regionX < result.Grid.Width; regionX += regionSize)
                {
                    BreakInRegion(result, random, regionX, regionY);
                }
            }

            SWLog.Log($"[{nameof(BackroomsBrokenLampPlacer)}] 등 {brokenLamps.Count}개를 고장 냈습니다.");
        }

        /// <summary>
        /// 구역 하나에서 등 하나를 골라 고장 냅니다.
        /// </summary>
        /// <param name="result">배치 기준이 되는 미로 결과입니다.</param>
        /// <param name="random">고를 칸을 정할 난수기입니다.</param>
        /// <param name="regionX">구역의 시작 X 칸입니다.</param>
        /// <param name="regionY">구역의 시작 Y 칸입니다.</param>
        private void BreakInRegion(MazeBuildResult result, System.Random random, int regionX, int regionY)
        {
            int width = Mathf.Min(regionSize, result.Grid.Width - regionX);
            int height = Mathf.Min(regionSize, result.Grid.Height - regionY);

            MazeCoordinate coordinate = new(
                regionX + random.Next(width), regionY + random.Next(height));

            // 전등이 없는 칸은 깜빡일 등이 아예 없고, 시작 칸과 탈출 칸은 그 자체로 표식이라 건드리지 않습니다.
            if (result.IsDark(coordinate)) return;
            if (coordinate == result.StartCoordinate || coordinate == result.ExitCoordinate) return;

            if (mapBuilder.TryGetPlacedTile(coordinate, out Transform tile) == false) return;

            Transform lampMesh = tile.Find(LampMeshPath);

            if (lampMesh == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsBrokenLampPlacer)}] " +
                    $"타일 {tile.name}에 {LampMeshPath}가 없어 건너뜁니다.");
                return;
            }

            brokenLamps.Add(lampMesh.gameObject.AddComponent<FlickeringLamp>());
        }
        #endregion // 함수
    }
}
