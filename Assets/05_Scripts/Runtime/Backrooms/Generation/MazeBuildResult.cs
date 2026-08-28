using System.Collections.Generic;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 미로 한 판을 생성한 결과입니다.
    /// </summary>
    /// <remarks>
    /// 절차적 생성 버그는 재현이 되지 않으면 잡을 수 없으므로 시드를 결과에 함께 담습니다.
    /// </remarks>
    public class MazeBuildResult
    {
        #region 필드
        /// <summary>전등이 달려 있지 않은 칸의 집합입니다.</summary>
        private readonly HashSet<MazeCoordinate> darkCells;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>생성과 검증이 모두 통과했는지 여부입니다.</summary>
        public bool IsSuccess { get; }

        /// <summary>생성된 격자입니다. 실패한 경우에도 마지막 시도 결과가 담깁니다.</summary>
        public MazeGrid Grid { get; }

        /// <summary>이 결과를 그대로 재현할 수 있는 시드입니다.</summary>
        public int Seed { get; }

        /// <summary>플레이어가 시작하는 칸의 좌표입니다.</summary>
        public MazeCoordinate StartCoordinate { get; }

        /// <summary>탈출 지점이 놓인 칸의 좌표입니다.</summary>
        public MazeCoordinate ExitCoordinate { get; }

        /// <summary>생성 결과의 구조 통계입니다.</summary>
        public MazeStatistics Statistics { get; }

        /// <summary>검증을 통과하기까지 시도한 횟수입니다.</summary>
        public int AttemptCount { get; }

        /// <summary>실패한 경우의 사유입니다. 성공하면 빈 문자열입니다.</summary>
        public string FailureReason { get; }

        /// <summary>전등이 달려 있지 않은 칸의 개수입니다.</summary>
        public int DarkCellCount => darkCells.Count;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 생성 결과를 만듭니다.
        /// </summary>
        /// <param name="isSuccess">생성과 검증이 통과했는지 여부입니다.</param>
        /// <param name="grid">생성된 격자입니다.</param>
        /// <param name="seed">결과를 재현할 수 있는 시드입니다.</param>
        /// <param name="startCoordinate">시작 칸의 좌표입니다.</param>
        /// <param name="exitCoordinate">탈출 지점 칸의 좌표입니다.</param>
        /// <param name="statistics">구조 통계입니다.</param>
        /// <param name="attemptCount">시도 횟수입니다.</param>
        /// <param name="failureReason">실패 사유입니다. 성공하면 빈 문자열을 넘깁니다.</param>
        /// <param name="darkCells">전등이 달려 있지 않은 칸의 집합입니다. 없으면 null을 넘겨도 됩니다.</param>
        public MazeBuildResult(bool isSuccess, MazeGrid grid, int seed,
            MazeCoordinate startCoordinate, MazeCoordinate exitCoordinate,
            MazeStatistics statistics, int attemptCount, string failureReason,
            HashSet<MazeCoordinate> darkCells = null)
        {
            this.darkCells = darkCells ?? new HashSet<MazeCoordinate>();
            IsSuccess = isSuccess;
            Grid = grid;
            Seed = seed;
            StartCoordinate = startCoordinate;
            ExitCoordinate = exitCoordinate;
            Statistics = statistics;
            AttemptCount = attemptCount;
            FailureReason = failureReason ?? string.Empty;
        }

        /// <summary>
        /// 칸에 전등이 달려 있지 않은지 확인합니다.
        /// </summary>
        /// <param name="coordinate">확인할 칸의 좌표입니다.</param>
        /// <returns>전등이 없으면 true를 반환합니다.</returns>
        public bool IsDark(MazeCoordinate coordinate)
        {
            return darkCells.Contains(coordinate);
        }

        /// <summary>
        /// 결과를 로그에 남기기 좋은 한 줄 요약으로 만듭니다.
        /// </summary>
        /// <returns>시드와 통계를 담은 요약 문자열입니다.</returns>
        public string ToSummary()
        {
            float darkRatio = Statistics.CellCount > 0 ? (float)DarkCellCount / Statistics.CellCount : 0f;

            return $"시드 {Seed} / 시도 {AttemptCount}회 / 칸 {Statistics.CellCount} / " +
                $"순환로 {Statistics.LoopCount} / 막다른 길 {Statistics.DeadEndCount}" +
                $"({Statistics.DeadEndRatio:P1}) / 최대 트인 구역 {Statistics.LargestOpenAreaCellCount}칸 / " +
                $"어두운 칸 {DarkCellCount}({darkRatio:P1}) / " +
                $"시작 {StartCoordinate} / 탈출 {ExitCoordinate}";
        }
        #endregion // 함수
    }
}
