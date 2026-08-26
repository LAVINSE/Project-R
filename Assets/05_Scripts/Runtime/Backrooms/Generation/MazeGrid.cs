using System;
using System.Collections.Generic;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 칸마다 네 방향 벽 정보를 갖는 격자 미로 데이터입니다.
    /// </summary>
    /// <remarks>
    /// 벽은 두 칸이 공유하므로 벽을 허물 때 양쪽 칸을 함께 갱신합니다.
    /// UnityEngine 타입을 쓰지 않으므로 단위 테스트에서 그대로 검증할 수 있습니다.
    /// </remarks>
    public class MazeGrid
    {
        #region 필드
        /// <summary>칸마다의 벽 정보입니다. 인덱스는 y * Width + x 입니다.</summary>
        private readonly EMazeDirection[] cellWalls;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>격자의 가로 칸 수입니다.</summary>
        public int Width { get; }

        /// <summary>격자의 세로 칸 수입니다.</summary>
        public int Height { get; }

        /// <summary>격자의 전체 칸 수입니다.</summary>
        public int CellCount => Width * Height;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 모든 벽이 세워진 격자를 만듭니다.
        /// </summary>
        /// <param name="width">가로 칸 수입니다. 1 이상이어야 합니다.</param>
        /// <param name="height">세로 칸 수입니다. 1 이상이어야 합니다.</param>
        /// <exception cref="ArgumentOutOfRangeException">가로 또는 세로 칸 수가 1보다 작을 때 발생합니다.</exception>
        public MazeGrid(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), "가로 칸 수는 1 이상이어야 합니다.");
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), "세로 칸 수는 1 이상이어야 합니다.");

            Width = width;
            Height = height;
            cellWalls = new EMazeDirection[width * height];

            for (int index = 0; index < cellWalls.Length; index += 1)
                cellWalls[index] = EMazeDirection.All;
        }

        /// <summary>
        /// 좌표가 격자 안에 있는지 확인합니다.
        /// </summary>
        /// <param name="coordinate">확인할 좌표입니다.</param>
        /// <returns>격자 안이면 true를 반환합니다.</returns>
        public bool IsInside(MazeCoordinate coordinate)
        {
            return coordinate.X >= 0 && coordinate.X < Width
                && coordinate.Y >= 0 && coordinate.Y < Height;
        }

        /// <summary>
        /// 칸의 벽 조합을 구합니다.
        /// </summary>
        /// <param name="coordinate">벽을 확인할 칸의 좌표입니다.</param>
        /// <returns>해당 칸에 남아 있는 벽 조합입니다. 격자 밖이면 네 방향 전부를 반환합니다.</returns>
        public EMazeDirection GetWalls(MazeCoordinate coordinate)
        {
            if (IsInside(coordinate) == false) return EMazeDirection.All;

            return cellWalls[ToIndex(coordinate)];
        }

        /// <summary>
        /// 칸의 특정 방향에 벽이 있는지 확인합니다.
        /// </summary>
        /// <param name="coordinate">확인할 칸의 좌표입니다.</param>
        /// <param name="direction">확인할 방향입니다.</param>
        /// <returns>벽이 있으면 true를 반환합니다.</returns>
        public bool HasWall(MazeCoordinate coordinate, EMazeDirection direction)
        {
            return (GetWalls(coordinate) & direction) != 0;
        }

        /// <summary>
        /// 두 칸 사이의 벽을 허물어 통로를 만듭니다.
        /// </summary>
        /// <param name="coordinate">기준 칸의 좌표입니다.</param>
        /// <param name="direction">통로를 낼 방향입니다.</param>
        /// <returns>실제로 벽을 허물었으면 true를 반환합니다.</returns>
        /// <remarks>격자 밖으로 향하는 바깥 벽은 허물 수 없습니다.</remarks>
        public bool CarvePassage(MazeCoordinate coordinate, EMazeDirection direction)
        {
            MazeCoordinate neighbor = coordinate + MazeDirections.Offset(direction);

            if (IsInside(coordinate) == false) return false;
            if (IsInside(neighbor) == false) return false;
            if (HasWall(coordinate, direction) == false) return false;

            cellWalls[ToIndex(coordinate)] &= ~direction;
            cellWalls[ToIndex(neighbor)] &= ~MazeDirections.Opposite(direction);

            return true;
        }

        /// <summary>
        /// 칸에서 통로로 이어진 이웃 칸의 좌표를 모두 구합니다.
        /// </summary>
        /// <param name="coordinate">기준 칸의 좌표입니다.</param>
        /// <returns>통로로 이어진 이웃 좌표 목록입니다. 없으면 빈 목록을 반환합니다.</returns>
        public List<MazeCoordinate> GetConnectedNeighbors(MazeCoordinate coordinate)
        {
            List<MazeCoordinate> neighbors = new List<MazeCoordinate>(4);

            for (int index = 0; index < MazeDirections.All.Count; index += 1)
            {
                EMazeDirection direction = MazeDirections.All[index];
                if (HasWall(coordinate, direction)) continue;

                MazeCoordinate neighbor = coordinate + MazeDirections.Offset(direction);
                if (IsInside(neighbor)) neighbors.Add(neighbor);
            }

            return neighbors;
        }

        /// <summary>
        /// 격자의 모든 좌표를 순서대로 열거합니다.
        /// </summary>
        /// <returns>왼쪽 아래에서 오른쪽 위 순서의 좌표 열거자입니다.</returns>
        public IEnumerable<MazeCoordinate> EnumerateCoordinates()
        {
            for (int y = 0; y < Height; y += 1)
            {
                for (int x = 0; x < Width; x += 1)
                    yield return new MazeCoordinate(x, y);
            }
        }

        /// <summary>
        /// 좌표를 내부 배열 인덱스로 바꿉니다.
        /// </summary>
        /// <param name="coordinate">변환할 좌표입니다.</param>
        /// <returns>내부 배열 인덱스입니다.</returns>
        private int ToIndex(MazeCoordinate coordinate)
        {
            return coordinate.Y * Width + coordinate.X;
        }
        #endregion // 함수
    }
}
