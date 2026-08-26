using System;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 격자 미로에서 칸 하나의 위치를 나타내는 좌표입니다.
    /// </summary>
    /// <remarks>
    /// 맵 생성 계산부를 Unity와 분리해 단위 테스트하기 위해 UnityEngine 타입을 쓰지 않습니다.
    /// </remarks>
    public readonly struct MazeCoordinate : IEquatable<MazeCoordinate>
    {
        #region 프로퍼티
        /// <summary>가로 방향 좌표입니다.</summary>
        public int X { get; }

        /// <summary>세로 방향 좌표입니다.</summary>
        public int Y { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 좌표를 만듭니다.
        /// </summary>
        /// <param name="x">가로 방향 좌표입니다.</param>
        /// <param name="y">세로 방향 좌표입니다.</param>
        public MazeCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 두 좌표를 더한 좌표를 구합니다.
        /// </summary>
        /// <param name="left">왼쪽 좌표입니다.</param>
        /// <param name="right">오른쪽 좌표입니다.</param>
        /// <returns>더해진 좌표입니다.</returns>
        public static MazeCoordinate operator +(MazeCoordinate left, MazeCoordinate right)
        {
            return new MazeCoordinate(left.X + right.X, left.Y + right.Y);
        }

        /// <summary>
        /// 두 좌표가 같은지 비교합니다.
        /// </summary>
        /// <param name="left">왼쪽 좌표입니다.</param>
        /// <param name="right">오른쪽 좌표입니다.</param>
        /// <returns>같으면 true를 반환합니다.</returns>
        public static bool operator ==(MazeCoordinate left, MazeCoordinate right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 두 좌표가 다른지 비교합니다.
        /// </summary>
        /// <param name="left">왼쪽 좌표입니다.</param>
        /// <param name="right">오른쪽 좌표입니다.</param>
        /// <returns>다르면 true를 반환합니다.</returns>
        public static bool operator !=(MazeCoordinate left, MazeCoordinate right)
        {
            return left.Equals(right) == false;
        }

        /// <summary>
        /// 다른 좌표와 같은지 비교합니다.
        /// </summary>
        /// <param name="other">비교할 좌표입니다.</param>
        /// <returns>같으면 true를 반환합니다.</returns>
        public bool Equals(MazeCoordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        /// 다른 객체와 같은지 비교합니다.
        /// </summary>
        /// <param name="obj">비교할 객체입니다.</param>
        /// <returns>같은 좌표이면 true를 반환합니다.</returns>
        public override bool Equals(object obj)
        {
            return obj is MazeCoordinate other && Equals(other);
        }

        /// <summary>
        /// 해시 코드를 구합니다.
        /// </summary>
        /// <returns>좌표의 해시 코드입니다.</returns>
        public override int GetHashCode()
        {
            return (X * 397) ^ Y;
        }

        /// <summary>
        /// 좌표를 읽기 쉬운 문자열로 만듭니다.
        /// </summary>
        /// <returns>"(X, Y)" 형태의 문자열입니다.</returns>
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
        #endregion // 함수
    }
}
