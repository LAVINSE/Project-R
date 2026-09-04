using System.Collections.Generic;

using ProjectR.Enum;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 생성된 미로의 구조를 수치로 요약한 통계입니다.
    /// </summary>
    /// <remarks>
    /// 맵 생성은 한 번 통과했다고 안심할 수 없는 영역이므로 시드를 여러 개 돌려 이 통계로 확인합니다.
    /// </remarks>
    public readonly struct MazeStatistics
    {
        #region 프로퍼티
        /// <summary>전체 칸 수입니다.</summary>
        public int CellCount { get; }

        /// <summary>통로가 하나뿐인 막다른 길 칸의 개수입니다.</summary>
        public int DeadEndCount { get; }

        /// <summary>전체 칸 대비 막다른 길의 비율입니다.</summary>
        public float DeadEndRatio { get; }

        /// <summary>이웃한 두 칸을 잇는 통로의 개수입니다.</summary>
        public int PassageCount { get; }

        /// <summary>서로 오갈 수 있는 칸 덩어리의 개수입니다. 1이면 전부 이어져 있습니다.</summary>
        public int RegionCount { get; }

        /// <summary>독립된 순환로의 개수입니다. 0이면 벽 짚기로 공략되는 완전 미로입니다.</summary>
        public int LoopCount { get; }

        /// <summary>가장 넓은 트인 구역의 칸 수입니다. 최악 구간 성능 측정 대상이 됩니다.</summary>
        /// <remarks>통로가 셋 이상인 칸끼리 이어진 덩어리를 트인 구역으로 봅니다.</remarks>
        public int LargestOpenAreaCellCount { get; }
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 통계 값을 지정해 만듭니다.
        /// </summary>
        /// <param name="cellCount">전체 칸 수입니다.</param>
        /// <param name="deadEndCount">막다른 길 칸의 개수입니다.</param>
        /// <param name="passageCount">통로의 개수입니다.</param>
        /// <param name="regionCount">칸 덩어리의 개수입니다.</param>
        /// <param name="largestOpenAreaCellCount">가장 넓은 트인 구역의 칸 수입니다.</param>
        private MazeStatistics(int cellCount, int deadEndCount, int passageCount, int regionCount,
            int largestOpenAreaCellCount)
        {
            CellCount = cellCount;
            DeadEndCount = deadEndCount;
            PassageCount = passageCount;
            RegionCount = regionCount;
            LargestOpenAreaCellCount = largestOpenAreaCellCount;
            DeadEndRatio = cellCount > 0 ? (float)deadEndCount / cellCount : 0f;
            LoopCount = passageCount - cellCount + regionCount;
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 격자를 훑어 통계를 계산합니다.
        /// </summary>
        /// <param name="grid">통계를 낼 격자입니다.</param>
        /// <returns>계산된 통계입니다.</returns>
        public static MazeStatistics Measure(MazeGrid grid)
        {
            if (grid == null) return new MazeStatistics(0, 0, 0, 0, 0);

            int deadEndCount = 0;
            int openingCount = 0;

            foreach (MazeCoordinate coordinate in grid.EnumerateCoordinates())
            {
                int openings = grid.GetConnectedNeighbors(coordinate).Count;
                openingCount += openings;

                if (openings == 1) deadEndCount += 1;
            }

            return new MazeStatistics(grid.CellCount, deadEndCount, openingCount / 2,
                CountRegions(grid), MeasureLargestOpenArea(grid));
        }

        /// <summary>
        /// 가장 넓은 트인 구역의 칸 수를 셉니다.
        /// </summary>
        /// <param name="grid">확인할 격자입니다.</param>
        /// <returns>가장 넓은 트인 구역의 칸 수입니다. 트인 구역이 없으면 0을 반환합니다.</returns>
        /// <remarks>
        /// 벽이 하나도 없는 2 x 2 덩어리에 속한 칸만 트인 칸으로 봅니다.
        /// 통로가 여럿인 삼거리도 폭은 한 칸이라 시야가 트이지 않으므로 트인 칸으로 세지 않습니다.
        /// </remarks>
        private static int MeasureLargestOpenArea(MazeGrid grid)
        {
            HashSet<MazeCoordinate> openCells = CollectOpenCells(grid);
            HashSet<MazeCoordinate> visited = new();
            int largest = 0;

            foreach (MazeCoordinate coordinate in openCells)
            {
                if (visited.Add(coordinate) == false) continue;

                int size = 0;
                Queue<MazeCoordinate> frontier = new();
                frontier.Enqueue(coordinate);

                while (frontier.Count > 0)
                {
                    MazeCoordinate current = frontier.Dequeue();
                    size += 1;

                    List<MazeCoordinate> neighbors = grid.GetConnectedNeighbors(current);

                    for (int index = 0; index < neighbors.Count; index += 1)
                    {
                        if (openCells.Contains(neighbors[index]) == false) continue;
                        if (visited.Add(neighbors[index]) == false) continue;

                        frontier.Enqueue(neighbors[index]);
                    }
                }

                if (size > largest) largest = size;
            }

            return largest;
        }

        /// <summary>
        /// 벽이 하나도 없는 2 x 2 덩어리에 속한 칸을 모두 모읍니다.
        /// </summary>
        /// <param name="grid">확인할 격자입니다.</param>
        /// <returns>트인 칸의 좌표 집합입니다. 없으면 빈 집합을 반환합니다.</returns>
        private static HashSet<MazeCoordinate> CollectOpenCells(MazeGrid grid)
        {
            HashSet<MazeCoordinate> openCells = new();

            for (int y = 0; y + 1 < grid.Height; y += 1)
            {
                for (int x = 0; x + 1 < grid.Width; x += 1)
                {
                    MazeCoordinate leftBottom = new(x, y);
                    MazeCoordinate rightBottom = new(x + 1, y);
                    MazeCoordinate leftTop = new(x, y + 1);
                    MazeCoordinate rightTop = new(x + 1, y + 1);

                    if (grid.HasWall(leftBottom, EMazeDirection.East)) continue;
                    if (grid.HasWall(leftBottom, EMazeDirection.North)) continue;
                    if (grid.HasWall(rightTop, EMazeDirection.West)) continue;
                    if (grid.HasWall(rightTop, EMazeDirection.South)) continue;

                    openCells.Add(leftBottom);
                    openCells.Add(rightBottom);
                    openCells.Add(leftTop);
                    openCells.Add(rightTop);
                }
            }

            return openCells;
        }

        /// <summary>
        /// 서로 오갈 수 있는 칸 덩어리의 개수를 셉니다.
        /// </summary>
        /// <param name="grid">확인할 격자입니다.</param>
        /// <returns>칸 덩어리의 개수입니다.</returns>
        private static int CountRegions(MazeGrid grid)
        {
            HashSet<MazeCoordinate> visited = new();
            int regionCount = 0;

            foreach (MazeCoordinate coordinate in grid.EnumerateCoordinates())
            {
                if (visited.Contains(coordinate)) continue;

                regionCount += 1;
                MazePathValidator.CollectReachable(grid, coordinate, visited);
            }

            return regionCount;
        }
        #endregion // 함수
    }
}
