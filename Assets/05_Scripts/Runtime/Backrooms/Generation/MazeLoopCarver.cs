using System;
using System.Collections.Generic;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 안쪽 벽을 무작위로 허물어 순환로를 만드는 보정기입니다.
    /// </summary>
    /// <remarks>
    /// 순환로가 없는 완전 미로는 한쪽 벽만 짚고 걸으면 공략되어 버립니다.
    /// 모든 칸이 이어진 상태에서 안쪽 벽 하나를 허물면 순환로가 정확히 하나 늘어납니다.
    /// </remarks>
    public static class MazeLoopCarver
    {
        #region 함수
        /// <summary>
        /// 순환로 개수가 최소치에 이를 때까지 안쪽 벽을 무작위로 허뭅니다.
        /// </summary>
        /// <param name="grid">보정할 격자입니다.</param>
        /// <param name="random">허물 벽을 고를 난수 발생기입니다.</param>
        /// <param name="minimumLoopCount">확보해야 할 최소 순환로 개수입니다.</param>
        /// <returns>실제로 허문 벽의 개수입니다.</returns>
        public static int Carve(MazeGrid grid, Random random, int minimumLoopCount)
        {
            if (grid == null || random == null) return 0;

            int currentLoopCount = MazeStatistics.Measure(grid).LoopCount;
            int neededCount = minimumLoopCount - currentLoopCount;
            if (neededCount <= 0) return 0;

            List<MazeWallReference> removableWalls = CollectRemovableWalls(grid);
            Shuffle(removableWalls, random);

            int carvedCount = 0;

            for (int index = 0; index < removableWalls.Count && carvedCount < neededCount; index += 1)
            {
                MazeWallReference wall = removableWalls[index];
                if (grid.CarvePassage(wall.Coordinate, wall.Direction)) carvedCount += 1;
            }

            return carvedCount;
        }

        /// <summary>
        /// 허물 수 있는 안쪽 벽을 모두 모읍니다.
        /// </summary>
        /// <param name="grid">확인할 격자입니다.</param>
        /// <returns>허물 수 있는 벽 목록입니다. 같은 벽이 두 번 담기지 않습니다.</returns>
        /// <remarks>북쪽과 동쪽만 훑으면 이웃한 두 칸이 공유하는 벽을 한 번씩만 담게 됩니다.</remarks>
        private static List<MazeWallReference> CollectRemovableWalls(MazeGrid grid)
        {
            List<MazeWallReference> walls = new List<MazeWallReference>();

            foreach (MazeCoordinate coordinate in grid.EnumerateCoordinates())
            {
                TryAddWall(grid, walls, coordinate, EMazeDirection.North);
                TryAddWall(grid, walls, coordinate, EMazeDirection.East);
            }

            return walls;
        }

        /// <summary>
        /// 허물 수 있는 벽이면 목록에 담습니다.
        /// </summary>
        /// <param name="grid">확인할 격자입니다.</param>
        /// <param name="walls">결과를 담을 목록입니다.</param>
        /// <param name="coordinate">기준 칸의 좌표입니다.</param>
        /// <param name="direction">확인할 방향입니다.</param>
        private static void TryAddWall(MazeGrid grid, List<MazeWallReference> walls,
            MazeCoordinate coordinate, EMazeDirection direction)
        {
            if (grid.HasWall(coordinate, direction) == false) return;
            if (grid.IsInside(coordinate + MazeDirections.Offset(direction)) == false) return;

            walls.Add(new MazeWallReference(coordinate, direction));
        }

        /// <summary>
        /// 목록의 순서를 무작위로 섞습니다.
        /// </summary>
        /// <param name="walls">섞을 벽 목록입니다.</param>
        /// <param name="random">사용할 난수 발생기입니다.</param>
        private static void Shuffle(List<MazeWallReference> walls, Random random)
        {
            for (int index = walls.Count - 1; index > 0; index -= 1)
            {
                int swapIndex = random.Next(index + 1);
                MazeWallReference temporary = walls[index];
                walls[index] = walls[swapIndex];
                walls[swapIndex] = temporary;
            }
        }
        #endregion // 함수

        /// <summary>
        /// 어느 칸의 어느 방향 벽인지를 가리키는 참조입니다.
        /// </summary>
        private readonly struct MazeWallReference
        {
            #region 프로퍼티
            /// <summary>벽이 붙어 있는 칸의 좌표입니다.</summary>
            public MazeCoordinate Coordinate { get; }

            /// <summary>칸에서 벽이 있는 방향입니다.</summary>
            public EMazeDirection Direction { get; }
            #endregion // 프로퍼티

            #region 함수
            /// <summary>
            /// 벽 참조를 만듭니다.
            /// </summary>
            /// <param name="coordinate">벽이 붙어 있는 칸의 좌표입니다.</param>
            /// <param name="direction">칸에서 벽이 있는 방향입니다.</param>
            public MazeWallReference(MazeCoordinate coordinate, EMazeDirection direction)
            {
                Coordinate = coordinate;
                Direction = direction;
            }
            #endregion // 함수
        }
    }
}
