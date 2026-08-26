using System;
using System.Collections.Generic;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 격자 일부를 직사각형으로 트여 넓은 홀을 만드는 보정기입니다.
    /// </summary>
    /// <remarks>
    /// 재귀적 백트래킹만 쓰면 결과가 전부 한 칸 폭 복도라 홀과 복도의 리듬이 생기지 않습니다.
    /// 넓은 홀은 분위기 때문만이 아니라 성능 판정 때문에도 필요합니다.
    /// 시야가 트인 구간이 없으면 최악 구간 프레임레이트를 잴 대상 자체가 없습니다.
    /// </remarks>
    public static class MazeRoomCarver
    {
        #region 함수
        /// <summary>
        /// 설정한 개수만큼 직사각형 홀을 트입니다.
        /// </summary>
        /// <param name="grid">보정할 격자입니다.</param>
        /// <param name="random">홀 위치와 크기를 고를 난수 발생기입니다.</param>
        /// <param name="settings">홀 개수와 크기 범위가 담긴 설정입니다.</param>
        /// <returns>실제로 트인 홀의 개수입니다.</returns>
        public static int Carve(MazeGrid grid, Random random, MazeGenerationSettings settings)
        {
            if (grid == null || random == null || settings == null) return 0;
            if (settings.RoomCount <= 0) return 0;

            int carvedCount = 0;
            int attemptLimit = settings.RoomCount * 8;
            List<MazeCoordinate> occupied = new List<MazeCoordinate>();

            for (int attempt = 0; attempt < attemptLimit && carvedCount < settings.RoomCount; attempt += 1)
            {
                int width = random.Next(settings.MinimumRoomSize, settings.MaximumRoomSize + 1);
                int height = random.Next(settings.MinimumRoomSize, settings.MaximumRoomSize + 1);

                if (width > grid.Width || height > grid.Height) continue;

                MazeCoordinate origin = new MazeCoordinate(
                    random.Next(grid.Width - width + 1),
                    random.Next(grid.Height - height + 1));

                if (Overlaps(occupied, origin, width, height)) continue;

                CarveRectangle(grid, origin, width, height);
                MarkOccupied(occupied, origin, width, height);
                carvedCount += 1;
            }

            return carvedCount;
        }

        /// <summary>
        /// 직사각형 범위 안의 벽을 전부 허물어 트인 공간을 만듭니다.
        /// </summary>
        /// <param name="grid">보정할 격자입니다.</param>
        /// <param name="origin">직사각형의 왼쪽 아래 좌표입니다.</param>
        /// <param name="width">직사각형의 가로 칸 수입니다.</param>
        /// <param name="height">직사각형의 세로 칸 수입니다.</param>
        private static void CarveRectangle(MazeGrid grid, MazeCoordinate origin, int width, int height)
        {
            for (int offsetY = 0; offsetY < height; offsetY += 1)
            {
                for (int offsetX = 0; offsetX < width; offsetX += 1)
                {
                    MazeCoordinate coordinate = new MazeCoordinate(origin.X + offsetX, origin.Y + offsetY);

                    if (offsetX + 1 < width) grid.CarvePassage(coordinate, EMazeDirection.East);
                    if (offsetY + 1 < height) grid.CarvePassage(coordinate, EMazeDirection.North);
                }
            }
        }

        /// <summary>
        /// 이미 홀로 쓰인 칸과 겹치는지 확인합니다.
        /// </summary>
        /// <param name="occupied">이미 홀로 쓰인 칸 목록입니다.</param>
        /// <param name="origin">확인할 직사각형의 왼쪽 아래 좌표입니다.</param>
        /// <param name="width">확인할 직사각형의 가로 칸 수입니다.</param>
        /// <param name="height">확인할 직사각형의 세로 칸 수입니다.</param>
        /// <returns>한 칸이라도 겹치면 true를 반환합니다.</returns>
        /// <remarks>홀이 서로 붙으면 미로가 아니라 빈 방 하나가 되어 버리므로 겹침을 막습니다.</remarks>
        private static bool Overlaps(List<MazeCoordinate> occupied, MazeCoordinate origin, int width, int height)
        {
            for (int index = 0; index < occupied.Count; index += 1)
            {
                MazeCoordinate cell = occupied[index];

                if (cell.X < origin.X - 1 || cell.X > origin.X + width) continue;
                if (cell.Y < origin.Y - 1 || cell.Y > origin.Y + height) continue;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 직사각형 범위의 칸을 사용 중으로 표시합니다.
        /// </summary>
        /// <param name="occupied">표시를 담을 목록입니다.</param>
        /// <param name="origin">직사각형의 왼쪽 아래 좌표입니다.</param>
        /// <param name="width">직사각형의 가로 칸 수입니다.</param>
        /// <param name="height">직사각형의 세로 칸 수입니다.</param>
        private static void MarkOccupied(List<MazeCoordinate> occupied, MazeCoordinate origin, int width, int height)
        {
            for (int offsetY = 0; offsetY < height; offsetY += 1)
            {
                for (int offsetX = 0; offsetX < width; offsetX += 1)
                    occupied.Add(new MazeCoordinate(origin.X + offsetX, origin.Y + offsetY));
            }
        }
        #endregion // 함수
    }
}
