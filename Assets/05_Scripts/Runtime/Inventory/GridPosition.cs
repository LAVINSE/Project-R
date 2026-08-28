using System;

namespace ProjectR.Inventory
{
    /// <summary>
    /// 격자 인벤토리 안의 칸 좌표입니다.
    /// </summary>
    /// <remarks>
    /// 계산부를 유니티와 분리해 두기 위해 Vector2Int 대신 이 구조체를 씁니다.
    /// 왼쪽 위가 (0, 0)이고 X는 오른쪽, Y는 아래쪽으로 늘어납니다.
    /// </remarks>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        #region 프로퍼티
        /// <summary>가로 방향 칸 번호입니다.</summary>
        public int X { get; }

        /// <summary>세로 방향 칸 번호입니다.</summary>
        public int Y { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 칸 좌표를 만듭니다.
        /// </summary>
        /// <param name="x">가로 방향 칸 번호입니다.</param>
        /// <param name="y">세로 방향 칸 번호입니다.</param>
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 다른 좌표와 같은 칸인지 확인합니다.
        /// </summary>
        /// <param name="other">비교할 좌표입니다.</param>
        /// <returns>같은 칸이면 true를 반환합니다.</returns>
        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        /// 다른 객체와 같은 칸인지 확인합니다.
        /// </summary>
        /// <param name="obj">비교할 객체입니다.</param>
        /// <returns>같은 칸이면 true를 반환합니다.</returns>
        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
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
        /// 좌표를 읽기 좋은 문자열로 만듭니다.
        /// </summary>
        /// <returns>(X, Y) 형태의 문자열입니다.</returns>
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
        #endregion // 함수
    }
}
