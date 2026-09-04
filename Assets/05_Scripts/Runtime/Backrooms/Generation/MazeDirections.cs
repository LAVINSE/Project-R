using System.Collections.Generic;

using ProjectR.Enum;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// <see cref="EMazeDirection"/>를 다루는 보조 함수 모음입니다.
    /// </summary>
    public static class MazeDirections
    {
        #region 필드
        /// <summary>네 방향을 북, 동, 남, 서 순서로 나열한 배열입니다.</summary>
        private static readonly EMazeDirection[] all =
        {
            EMazeDirection.North,
            EMazeDirection.East,
            EMazeDirection.South,
            EMazeDirection.West,
        };
        #endregion // 필드

        #region 프로퍼티
        /// <summary>네 방향을 북, 동, 남, 서 순서로 담은 목록입니다.</summary>
        public static IReadOnlyList<EMazeDirection> All => all;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 방향의 반대 방향을 구합니다.
        /// </summary>
        /// <param name="direction">반대 방향을 구할 방향입니다.</param>
        /// <returns>반대 방향입니다. 단일 방향이 아니면 <see cref="EMazeDirection.None"/>을 반환합니다.</returns>
        public static EMazeDirection Opposite(EMazeDirection direction)
        {
            switch (direction)
            {
                case EMazeDirection.North: return EMazeDirection.South;
                case EMazeDirection.East: return EMazeDirection.West;
                case EMazeDirection.South: return EMazeDirection.North;
                case EMazeDirection.West: return EMazeDirection.East;
                default: return EMazeDirection.None;
            }
        }

        /// <summary>
        /// 방향에 해당하는 격자 좌표 이동량을 구합니다.
        /// </summary>
        /// <param name="direction">이동량을 구할 방향입니다.</param>
        /// <returns>X와 Y 이동량입니다.</returns>
        public static MazeCoordinate Offset(EMazeDirection direction)
        {
            switch (direction)
            {
                case EMazeDirection.North: return new MazeCoordinate(0, 1);
                case EMazeDirection.East: return new MazeCoordinate(1, 0);
                case EMazeDirection.South: return new MazeCoordinate(0, -1);
                case EMazeDirection.West: return new MazeCoordinate(-1, 0);
                default: return new MazeCoordinate(0, 0);
            }
        }

        /// <summary>
        /// 벽 조합에 포함된 벽의 개수를 셉니다.
        /// </summary>
        /// <param name="walls">개수를 셀 벽 조합입니다.</param>
        /// <returns>포함된 벽의 개수입니다. 0에서 4 사이입니다.</returns>
        public static int CountWalls(EMazeDirection walls)
        {
            int count = 0;

            for (int index = 0; index < all.Length; index += 1)
            {
                if ((walls & all[index]) != 0) count += 1;
            }

            return count;
        }
        #endregion // 함수
    }
}
