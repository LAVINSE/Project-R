using System;
using System.Collections.Generic;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 재귀적 백트래킹 방식으로 완전 미로를 만드는 생성기입니다.
    /// </summary>
    /// <remarks>
    /// 결과는 순환로가 하나도 없는 완전 미로이므로 막다른 길이 많습니다.
    /// 순환로 추가와 막다른 길 축소는 <see cref="MazeFactory"/>가 보정 단계에서 처리합니다.
    /// 재귀 호출 대신 명시적 스택을 써서 큰 격자에서도 스택이 넘치지 않게 했습니다.
    /// </remarks>
    public class RecursiveBacktrackerGenerator : IMazeGenerator
    {
        #region 프로퍼티
        /// <summary>로그와 통계에 표시할 생성 방식 이름입니다.</summary>
        public string DisplayName => "재귀적 백트래킹";
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 설정과 난수를 받아 완전 미로를 만듭니다.
        /// </summary>
        /// <param name="settings">생성에 사용할 설정입니다.</param>
        /// <param name="random">생성에 사용할 난수 발생기입니다.</param>
        /// <returns>모든 칸이 이어진 완전 미로입니다.</returns>
        /// <exception cref="ArgumentNullException">설정이나 난수 발생기가 null일 때 발생합니다.</exception>
        public MazeGrid Generate(MazeGenerationSettings settings, Random random)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (random == null) throw new ArgumentNullException(nameof(random));

            MazeGrid grid = new MazeGrid(settings.Width, settings.Height);
            bool[] visited = new bool[grid.CellCount];

            MazeCoordinate startCoordinate = new MazeCoordinate(
                random.Next(grid.Width), random.Next(grid.Height));

            Stack<MazeCoordinate> stack = new Stack<MazeCoordinate>(grid.CellCount);
            stack.Push(startCoordinate);
            visited[ToIndex(grid, startCoordinate)] = true;

            List<EMazeDirection> candidates = new List<EMazeDirection>(4);

            while (stack.Count > 0)
            {
                MazeCoordinate current = stack.Peek();
                CollectUnvisitedDirections(grid, visited, current, candidates);

                if (candidates.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                EMazeDirection direction = candidates[random.Next(candidates.Count)];
                MazeCoordinate next = current + MazeDirections.Offset(direction);

                grid.CarvePassage(current, direction);
                visited[ToIndex(grid, next)] = true;
                stack.Push(next);
            }

            return grid;
        }

        /// <summary>
        /// 아직 방문하지 않은 이웃으로 향하는 방향을 모읍니다.
        /// </summary>
        /// <param name="grid">대상 격자입니다.</param>
        /// <param name="visited">칸별 방문 여부입니다.</param>
        /// <param name="coordinate">기준 칸의 좌표입니다.</param>
        /// <param name="candidates">결과를 담을 목록입니다. 호출 시 비워집니다.</param>
        private static void CollectUnvisitedDirections(MazeGrid grid, bool[] visited,
            MazeCoordinate coordinate, List<EMazeDirection> candidates)
        {
            candidates.Clear();

            for (int index = 0; index < MazeDirections.All.Count; index += 1)
            {
                EMazeDirection direction = MazeDirections.All[index];
                MazeCoordinate neighbor = coordinate + MazeDirections.Offset(direction);

                if (grid.IsInside(neighbor) == false) continue;
                if (visited[ToIndex(grid, neighbor)]) continue;

                candidates.Add(direction);
            }
        }

        /// <summary>
        /// 좌표를 방문 배열의 인덱스로 바꿉니다.
        /// </summary>
        /// <param name="grid">대상 격자입니다.</param>
        /// <param name="coordinate">변환할 좌표입니다.</param>
        /// <returns>방문 배열 인덱스입니다.</returns>
        private static int ToIndex(MazeGrid grid, MazeCoordinate coordinate)
        {
            return coordinate.Y * grid.Width + coordinate.X;
        }
        #endregion // 함수
    }
}
