using System;
using System.Collections.Generic;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 막다른 길 비율이 상한을 넘지 않도록 벽을 허무는 보정기입니다.
    /// </summary>
    /// <remarks>
    /// 재귀적 백트래킹은 막다른 길을 과하게 만듭니다. 막다른 길에서 죽는 경험은
    /// 실력이 아니라 운으로 느껴지므로 비율에 상한을 둡니다.
    /// 벽을 허무는 동작은 통로만 늘리므로 새로운 막다른 길을 만들지 않고 반드시 끝납니다.
    /// </remarks>
    public static class MazeDeadEndReducer
    {
        #region 함수
        /// <summary>
        /// 막다른 길 비율이 상한 이하가 될 때까지 막다른 길의 벽을 허뭅니다.
        /// </summary>
        /// <param name="grid">보정할 격자입니다.</param>
        /// <param name="random">허물 벽을 고를 난수 발생기입니다.</param>
        /// <param name="maximumDeadEndRatio">허용하는 막다른 길의 최대 비율입니다.</param>
        /// <returns>실제로 허문 벽의 개수입니다.</returns>
        public static int Reduce(MazeGrid grid, Random random, float maximumDeadEndRatio)
        {
            if (grid == null || random == null) return 0;

            List<MazeCoordinate> deadEnds = CollectDeadEnds(grid);
            int allowedDeadEndCount = (int)(grid.CellCount * Math.Max(0f, maximumDeadEndRatio));
            int carvedCount = 0;

            Shuffle(deadEnds, random);

            for (int index = 0; index < deadEnds.Count; index += 1)
            {
                if (deadEnds.Count - carvedCount <= allowedDeadEndCount) break;

                if (CarveRandomWall(grid, deadEnds[index], random)) carvedCount += 1;
            }

            return carvedCount;
        }

        /// <summary>
        /// 통로가 하나뿐인 칸을 모두 찾습니다.
        /// </summary>
        /// <param name="grid">확인할 격자입니다.</param>
        /// <returns>막다른 길 칸의 좌표 목록입니다. 없으면 빈 목록을 반환합니다.</returns>
        private static List<MazeCoordinate> CollectDeadEnds(MazeGrid grid)
        {
            List<MazeCoordinate> deadEnds = new List<MazeCoordinate>();

            foreach (MazeCoordinate coordinate in grid.EnumerateCoordinates())
            {
                if (grid.GetConnectedNeighbors(coordinate).Count == 1) deadEnds.Add(coordinate);
            }

            return deadEnds;
        }

        /// <summary>
        /// 칸에 남아 있는 안쪽 벽 하나를 무작위로 허뭅니다.
        /// </summary>
        /// <param name="grid">보정할 격자입니다.</param>
        /// <param name="coordinate">벽을 허물 칸의 좌표입니다.</param>
        /// <param name="random">허물 벽을 고를 난수 발생기입니다.</param>
        /// <returns>허물었으면 true를 반환합니다. 격자 바깥 벽만 남았으면 false를 반환합니다.</returns>
        private static bool CarveRandomWall(MazeGrid grid, MazeCoordinate coordinate, Random random)
        {
            List<EMazeDirection> candidates = new List<EMazeDirection>(4);

            for (int index = 0; index < MazeDirections.All.Count; index += 1)
            {
                EMazeDirection direction = MazeDirections.All[index];
                if (grid.HasWall(coordinate, direction) == false) continue;
                if (grid.IsInside(coordinate + MazeDirections.Offset(direction)) == false) continue;

                candidates.Add(direction);
            }

            if (candidates.Count == 0) return false;

            return grid.CarvePassage(coordinate, candidates[random.Next(candidates.Count)]);
        }

        /// <summary>
        /// 목록의 순서를 무작위로 섞습니다.
        /// </summary>
        /// <param name="coordinates">섞을 좌표 목록입니다.</param>
        /// <param name="random">사용할 난수 발생기입니다.</param>
        private static void Shuffle(List<MazeCoordinate> coordinates, Random random)
        {
            for (int index = coordinates.Count - 1; index > 0; index -= 1)
            {
                int swapIndex = random.Next(index + 1);
                MazeCoordinate temporary = coordinates[index];
                coordinates[index] = coordinates[swapIndex];
                coordinates[swapIndex] = temporary;
            }
        }
        #endregion // 함수
    }
}
