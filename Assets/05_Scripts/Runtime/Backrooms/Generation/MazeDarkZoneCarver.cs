using System;
using System.Collections.Generic;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 격자에서 전등이 달려 있지 않을 칸을 덩어리 단위로 골라 내는 보정기입니다.
    /// </summary>
    /// <remarks>
    /// 모든 칸에 형광등이 켜져 있으면 손전등을 켤 이유가 없습니다.
    /// 어두운 칸을 무작위로 흩뿌리면 밝은 칸과 어두운 칸이 한 칸씩 번갈아 나와 구역으로 읽히지 않으므로,
    /// 씨앗 칸에서 통로를 따라 번져 나가게 해 걸어 들어갈 수 있는 덩어리로 만듭니다.
    /// 시작 칸과 탈출 칸은 어두워지지 않습니다. 시작하자마자 아무것도 보이지 않거나
    /// 탈출 지점이 어둠에 묻혀 버리면 길 잃음이 긴장이 아니라 짜증이 됩니다.
    /// </remarks>
    public static class MazeDarkZoneCarver
    {
        #region 함수
        /// <summary>
        /// 설정한 비율만큼의 칸을 어두운 덩어리로 고릅니다.
        /// </summary>
        /// <param name="grid">기준이 되는 격자입니다.</param>
        /// <param name="random">덩어리 위치를 고를 난수 발생기입니다.</param>
        /// <param name="settings">어두운 칸 비율과 덩어리 개수가 담긴 설정입니다.</param>
        /// <param name="start">어둡게 하지 않을 시작 칸입니다.</param>
        /// <param name="exit">어둡게 하지 않을 탈출 칸입니다.</param>
        /// <returns>어두운 칸의 좌표 집합입니다. 어두운 구역이 없으면 빈 집합을 반환합니다.</returns>
        public static HashSet<MazeCoordinate> Carve(MazeGrid grid, Random random,
            MazeGenerationSettings settings, MazeCoordinate start, MazeCoordinate exit)
        {
            HashSet<MazeCoordinate> darkCells = new();

            if (grid == null || random == null || settings == null) return darkCells;
            if (settings.DarkZoneCount <= 0 || settings.DarkCellRatio <= 0f) return darkCells;

            int targetCount = (int)(grid.CellCount * settings.DarkCellRatio);
            if (targetCount <= 0) return darkCells;

            int zoneCount = Math.Min(settings.DarkZoneCount, targetCount);
            int cellsPerZone = Math.Max(1, targetCount / zoneCount);

            for (int zone = 0; zone < zoneCount; zone += 1)
                GrowZone(grid, random, darkCells, cellsPerZone, start, exit);

            return darkCells;
        }

        /// <summary>
        /// 씨앗 칸 하나를 골라 통로를 따라 번져 나가며 덩어리 하나를 만듭니다.
        /// </summary>
        /// <param name="grid">기준이 되는 격자입니다.</param>
        /// <param name="random">씨앗 칸과 번져 나갈 순서를 고를 난수 발생기입니다.</param>
        /// <param name="darkCells">지금까지 고른 어두운 칸 집합입니다. 여기에 덧붙입니다.</param>
        /// <param name="cellsPerZone">덩어리 하나가 차지할 목표 칸 수입니다.</param>
        /// <param name="start">어둡게 하지 않을 시작 칸입니다.</param>
        /// <param name="exit">어둡게 하지 않을 탈출 칸입니다.</param>
        private static void GrowZone(MazeGrid grid, Random random, HashSet<MazeCoordinate> darkCells,
            int cellsPerZone, MazeCoordinate start, MazeCoordinate exit)
        {
            if (TryFindSeed(grid, random, darkCells, start, exit, out MazeCoordinate seed) == false) return;

            List<MazeCoordinate> frontier = new() { seed };
            darkCells.Add(seed);

            int grownCount = 1;

            while (grownCount < cellsPerZone && frontier.Count > 0)
            {
                int pickIndex = random.Next(frontier.Count);
                MazeCoordinate current = frontier[pickIndex];

                // 이웃을 모두 써 버린 칸은 다시 뽑히지 않도록 목록에서 빼고 넘어갑니다.
                if (TryTakeNeighbor(grid, random, darkCells, current, start, exit,
                    out MazeCoordinate neighbor) == false)
                {
                    frontier.RemoveAt(pickIndex);
                    continue;
                }

                darkCells.Add(neighbor);
                frontier.Add(neighbor);
                grownCount += 1;
            }
        }

        /// <summary>
        /// 아직 어둡지 않은 칸 중에서 덩어리를 시작할 씨앗 칸을 고릅니다.
        /// </summary>
        /// <param name="grid">기준이 되는 격자입니다.</param>
        /// <param name="random">씨앗 칸을 고를 난수 발생기입니다.</param>
        /// <param name="darkCells">이미 고른 어두운 칸 집합입니다.</param>
        /// <param name="start">고르면 안 되는 시작 칸입니다.</param>
        /// <param name="exit">고르면 안 되는 탈출 칸입니다.</param>
        /// <param name="seed">찾은 씨앗 칸입니다. 찾지 못하면 (0, 0)입니다.</param>
        /// <returns>씨앗 칸을 찾았으면 true를 반환합니다.</returns>
        private static bool TryFindSeed(MazeGrid grid, Random random, HashSet<MazeCoordinate> darkCells,
            MazeCoordinate start, MazeCoordinate exit, out MazeCoordinate seed)
        {
            int attemptLimit = grid.CellCount;

            for (int attempt = 0; attempt < attemptLimit; attempt += 1)
            {
                MazeCoordinate candidate = new(
                    random.Next(grid.Width), random.Next(grid.Height));

                if (IsUsable(darkCells, candidate, start, exit) == false) continue;

                seed = candidate;
                return true;
            }

            seed = new MazeCoordinate(0, 0);
            return false;
        }

        /// <summary>
        /// 칸에서 통로로 이어진 이웃 중 아직 어둡지 않은 칸 하나를 고릅니다.
        /// </summary>
        /// <param name="grid">기준이 되는 격자입니다.</param>
        /// <param name="random">이웃을 고를 난수 발생기입니다.</param>
        /// <param name="darkCells">이미 고른 어두운 칸 집합입니다.</param>
        /// <param name="coordinate">이웃을 찾을 기준 칸입니다.</param>
        /// <param name="start">고르면 안 되는 시작 칸입니다.</param>
        /// <param name="exit">고르면 안 되는 탈출 칸입니다.</param>
        /// <param name="neighbor">찾은 이웃 칸입니다. 찾지 못하면 (0, 0)입니다.</param>
        /// <returns>쓸 수 있는 이웃을 찾았으면 true를 반환합니다.</returns>
        private static bool TryTakeNeighbor(MazeGrid grid, Random random, HashSet<MazeCoordinate> darkCells,
            MazeCoordinate coordinate, MazeCoordinate start, MazeCoordinate exit, out MazeCoordinate neighbor)
        {
            List<MazeCoordinate> candidates = grid.GetConnectedNeighbors(coordinate);

            for (int index = candidates.Count - 1; index >= 0; index -= 1)
            {
                if (IsUsable(darkCells, candidates[index], start, exit) == false) candidates.RemoveAt(index);
            }

            if (candidates.Count == 0)
            {
                neighbor = new MazeCoordinate(0, 0);
                return false;
            }

            neighbor = candidates[random.Next(candidates.Count)];
            return true;
        }

        /// <summary>
        /// 어둡게 만들어도 되는 칸인지 확인합니다.
        /// </summary>
        /// <param name="darkCells">이미 고른 어두운 칸 집합입니다.</param>
        /// <param name="coordinate">확인할 칸입니다.</param>
        /// <param name="start">어둡게 하면 안 되는 시작 칸입니다.</param>
        /// <param name="exit">어둡게 하면 안 되는 탈출 칸입니다.</param>
        /// <returns>어둡게 만들어도 되면 true를 반환합니다.</returns>
        private static bool IsUsable(HashSet<MazeCoordinate> darkCells, MazeCoordinate coordinate,
            MazeCoordinate start, MazeCoordinate exit)
        {
            if (coordinate == start || coordinate == exit) return false;

            return darkCells.Contains(coordinate) == false;
        }
        #endregion // 함수
    }
}
