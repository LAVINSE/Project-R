using System;
using System.Collections.Generic;

using SW.Util;

namespace ProjectR.Backrooms.Generation
{
    /// <summary>
    /// 생성기와 보정기, 검증기를 순서대로 돌려 쓸 수 있는 미로를 만들어 내는 조립자입니다.
    /// </summary>
    /// <remarks>
    /// 난수는 시드로만 만들어 쓰므로 같은 시드에서는 항상 같은 결과가 나옵니다.
    /// 전역 난수인 SWRandom을 쓰지 않는 이유는, 다른 시스템이 난수를 뽑으면
    /// 같은 시드에서도 미로가 달라져 생성 버그를 재현할 수 없기 때문입니다.
    /// </remarks>
    public class MazeFactory
    {
        #region 필드
        /// <summary>미로 뼈대를 만들 생성기입니다.</summary>
        private readonly IMazeGenerator generator;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>사용 중인 생성 방식의 이름입니다.</summary>
        public string GeneratorName => generator.DisplayName;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 생성기를 지정해 조립자를 만듭니다.
        /// </summary>
        /// <param name="generator">사용할 미로 생성기입니다.</param>
        /// <exception cref="ArgumentNullException">생성기가 null일 때 발생합니다.</exception>
        public MazeFactory(IMazeGenerator generator)
        {
            this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
        }

        /// <summary>
        /// 기본 생성기인 재귀적 백트래킹으로 조립자를 만듭니다.
        /// </summary>
        public MazeFactory() : this(new RecursiveBacktrackerGenerator())
        {
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 시드에서 미로를 만들고 보정과 검증까지 마칩니다.
        /// </summary>
        /// <param name="settings">생성에 사용할 설정입니다.</param>
        /// <param name="seed">재현에 사용할 시드입니다.</param>
        /// <returns>생성 결과입니다. 재시도 상한까지 검증에 실패하면 실패 결과를 반환합니다.</returns>
        /// <exception cref="ArgumentNullException">설정이 null일 때 발생합니다.</exception>
        public MazeBuildResult Build(MazeGenerationSettings settings, int seed)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            MazeBuildResult lastResult = null;
            int attemptLimit = Math.Max(1, settings.MaximumAttemptCount);

            for (int attempt = 1; attempt <= attemptLimit; attempt += 1)
            {
                lastResult = BuildOnce(settings, seed, attempt);
                if (lastResult.IsSuccess) return lastResult;

                SWLog.LogWarning($"[{nameof(MazeFactory)}] 검증에 실패해 다시 생성합니다. " +
                    $"({attempt}/{attemptLimit}) 사유: {lastResult.FailureReason}");
            }

            SWLog.LogError($"[{nameof(MazeFactory)}] 재시도 상한 {attemptLimit}회를 넘겨 생성을 포기합니다. " +
                $"시드: {seed}");

            return lastResult;
        }

        /// <summary>
        /// 한 번 생성해 보고 결과를 만듭니다.
        /// </summary>
        /// <param name="settings">생성에 사용할 설정입니다.</param>
        /// <param name="seed">재현에 사용할 시드입니다.</param>
        /// <param name="attempt">몇 번째 시도인지입니다. 1부터 시작합니다.</param>
        /// <returns>이번 시도의 생성 결과입니다.</returns>
        /// <remarks>
        /// 시도마다 난수 씨앗을 다르게 하되 시드에서 결정되도록 해서 재현성을 지킵니다.
        /// </remarks>
        private MazeBuildResult BuildOnce(MazeGenerationSettings settings, int seed, int attempt)
        {
            Random random = new(unchecked(seed * 397 + attempt));

            MazeGrid grid = generator.Generate(settings, random);
            MazeRoomCarver.Carve(grid, random, settings);
            MazeDeadEndReducer.Reduce(grid, random, settings.MaximumDeadEndRatio);
            MazeLoopCarver.Carve(grid, random, settings.MinimumLoopCount);

            MazeCoordinate start = new(random.Next(grid.Width), random.Next(grid.Height));
            MazeCoordinate exit = MazePathValidator.FindFarthest(grid, start);
            MazeStatistics statistics = MazeStatistics.Measure(grid);

            // 어두운 구역은 구조를 바꾸지 않으므로 검증이 끝난 격자 위에 마지막으로 고릅니다.
            HashSet<MazeCoordinate> darkCells = MazeDarkZoneCarver.Carve(grid, random, settings, start, exit);

            string failureReason = Validate(settings, statistics, grid, start, exit);

            return new MazeBuildResult(string.IsNullOrEmpty(failureReason), grid, seed,
                start, exit, statistics, attempt, failureReason, darkCells);
        }

        /// <summary>
        /// 생성 결과가 필수 조건을 만족하는지 확인합니다.
        /// </summary>
        /// <param name="settings">생성에 사용한 설정입니다.</param>
        /// <param name="statistics">생성 결과의 통계입니다.</param>
        /// <param name="grid">생성된 격자입니다.</param>
        /// <param name="start">시작 칸의 좌표입니다.</param>
        /// <param name="exit">탈출 지점 칸의 좌표입니다.</param>
        /// <returns>문제가 없으면 빈 문자열을, 있으면 사유 문자열을 반환합니다.</returns>
        private static string Validate(MazeGenerationSettings settings, MazeStatistics statistics,
            MazeGrid grid, MazeCoordinate start, MazeCoordinate exit)
        {
            if (MazePathValidator.IsReachable(grid, start, exit) == false)
                return "시작 지점에서 탈출 지점에 도달할 수 없습니다.";

            if (statistics.LoopCount < settings.MinimumLoopCount)
                return $"순환로가 {statistics.LoopCount}개로 최소치 {settings.MinimumLoopCount}개에 못 미칩니다.";

            if (statistics.DeadEndRatio > settings.MaximumDeadEndRatio)
                return $"막다른 길 비율이 {statistics.DeadEndRatio:P1}로 상한 " +
                    $"{settings.MaximumDeadEndRatio:P1}을 넘습니다.";

            if (statistics.LargestOpenAreaCellCount < settings.MinimumLargestOpenAreaCellCount)
                return $"가장 넓은 트인 구역이 {statistics.LargestOpenAreaCellCount}칸으로 최소치 " +
                    $"{settings.MinimumLargestOpenAreaCellCount}칸에 못 미칩니다.";

            return string.Empty;
        }
        #endregion // 함수
    }
}
