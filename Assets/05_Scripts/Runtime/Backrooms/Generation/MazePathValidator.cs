using System.Collections.Generic;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 미로에서 특정 지점 사이를 실제로 오갈 수 있는지 검증하는 유틸리티입니다.
    /// </summary>
    /// <remarks>
    /// 생성 방식을 WFC나 BSP로 바꿔도 그대로 쓸 수 있도록 격자 데이터만 보고 판정합니다.
    /// </remarks>
    public static class MazePathValidator
    {
        #region 함수
        /// <summary>
        /// 출발 지점에서 도착 지점까지 갈 수 있는지 확인합니다.
        /// </summary>
        /// <param name="grid">확인할 격자입니다.</param>
        /// <param name="from">출발 지점의 좌표입니다.</param>
        /// <param name="to">도착 지점의 좌표입니다.</param>
        /// <returns>갈 수 있으면 true를 반환합니다.</returns>
        public static bool IsReachable(MazeGrid grid, MazeCoordinate from, MazeCoordinate to)
        {
            if (grid == null) return false;
            if (grid.IsInside(from) == false || grid.IsInside(to) == false) return false;

            HashSet<MazeCoordinate> reachable = new();
            CollectReachable(grid, from, reachable);

            return reachable.Contains(to);
        }

        /// <summary>
        /// 출발 지점에서 갈 수 있는 모든 칸을 모읍니다.
        /// </summary>
        /// <param name="grid">확인할 격자입니다.</param>
        /// <param name="from">출발 지점의 좌표입니다.</param>
        /// <param name="reachable">결과를 담을 집합입니다. 기존 내용은 지우지 않습니다.</param>
        public static void CollectReachable(MazeGrid grid, MazeCoordinate from, HashSet<MazeCoordinate> reachable)
        {
            if (grid == null || reachable == null) return;
            if (grid.IsInside(from) == false) return;
            if (reachable.Add(from) == false) return;

            Queue<MazeCoordinate> frontier = new();
            frontier.Enqueue(from);

            while (frontier.Count > 0)
            {
                MazeCoordinate current = frontier.Dequeue();
                List<MazeCoordinate> neighbors = grid.GetConnectedNeighbors(current);

                for (int index = 0; index < neighbors.Count; index += 1)
                {
                    if (reachable.Add(neighbors[index]) == false) continue;

                    frontier.Enqueue(neighbors[index]);
                }
            }
        }

        /// <summary>
        /// 출발 지점에서 통로를 따라 가장 멀리 떨어진 칸을 찾습니다.
        /// </summary>
        /// <param name="grid">확인할 격자입니다.</param>
        /// <param name="from">출발 지점의 좌표입니다.</param>
        /// <returns>가장 먼 칸의 좌표입니다. 격자가 비어 있으면 출발 지점을 그대로 반환합니다.</returns>
        /// <remarks>탈출 지점을 출발 지점에서 충분히 떨어뜨리기 위해 사용합니다.</remarks>
        public static MazeCoordinate FindFarthest(MazeGrid grid, MazeCoordinate from)
        {
            if (grid == null || grid.IsInside(from) == false) return from;

            HashSet<MazeCoordinate> visited = new() { from };
            Queue<MazeCoordinate> frontier = new();
            frontier.Enqueue(from);

            MazeCoordinate farthest = from;

            while (frontier.Count > 0)
            {
                MazeCoordinate current = frontier.Dequeue();
                farthest = current;

                List<MazeCoordinate> neighbors = grid.GetConnectedNeighbors(current);

                for (int index = 0; index < neighbors.Count; index += 1)
                {
                    if (visited.Add(neighbors[index]) == false) continue;

                    frontier.Enqueue(neighbors[index]);
                }
            }

            return farthest;
        }
        #endregion // 함수
    }
}
