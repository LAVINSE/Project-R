using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine.TestTools;

using ProjectR.Backrooms.Generation;

namespace ProjectR.Tests
{
    /// <summary>
    /// 맵 생성 계산부가 필수 조건을 지키는지 확인하는 단위 테스트입니다.
    /// </summary>
    /// <remarks>
    /// 맵 생성은 한 시드에서 통과했다고 안심할 수 없는 영역이므로
    /// 여러 시드를 돌려 통계로 확인합니다.
    /// </remarks>
    public class MazeGenerationTests
    {
        #region 상수
        /// <summary>통계 확인에 사용할 시드의 개수입니다.</summary>
        private const int SampleSeedCount = 30;
        #endregion // 상수

        #region 함수
        /// <summary>
        /// 테스트에 사용할 기본 설정을 만듭니다.
        /// </summary>
        /// <returns>16 x 16 크기의 기본 생성 설정입니다.</returns>
        private static MazeGenerationSettings CreateSettings()
        {
            return new MazeGenerationSettings(16, 16, 8, 0.1f, 5, 3, 3, 4, 9);
        }

        /// <summary>
        /// 순환로가 최소 개수 이상 만들어지는지 확인합니다.
        /// </summary>
        [Test]
        public void 생성_결과는_최소_순환로_개수를_만족합니다()
        {
            MazeGenerationSettings settings = CreateSettings();
            MazeFactory factory = new MazeFactory();

            for (int seed = 0; seed < SampleSeedCount; seed += 1)
            {
                MazeBuildResult result = factory.Build(settings, seed);

                Assert.IsTrue(result.IsSuccess, $"시드 {seed} 생성 실패: {result.FailureReason}");
                Assert.GreaterOrEqual(result.Statistics.LoopCount, settings.MinimumLoopCount,
                    $"시드 {seed}의 순환로가 최소치에 못 미칩니다.");
            }
        }

        /// <summary>
        /// 막다른 길 비율이 상한을 넘지 않는지 확인합니다.
        /// </summary>
        [Test]
        public void 생성_결과는_막다른_길_비율_상한을_지킵니다()
        {
            MazeGenerationSettings settings = CreateSettings();
            MazeFactory factory = new MazeFactory();

            for (int seed = 0; seed < SampleSeedCount; seed += 1)
            {
                MazeBuildResult result = factory.Build(settings, seed);

                Assert.LessOrEqual(result.Statistics.DeadEndRatio, settings.MaximumDeadEndRatio,
                    $"시드 {seed}의 막다른 길 비율이 상한을 넘습니다.");
            }
        }

        /// <summary>
        /// 시작 지점에서 탈출 지점까지 실제로 갈 수 있는지 확인합니다.
        /// </summary>
        [Test]
        public void 생성_결과에는_탈출_경로가_존재합니다()
        {
            MazeGenerationSettings settings = CreateSettings();
            MazeFactory factory = new MazeFactory();

            for (int seed = 0; seed < SampleSeedCount; seed += 1)
            {
                MazeBuildResult result = factory.Build(settings, seed);

                Assert.IsTrue(
                    MazePathValidator.IsReachable(result.Grid, result.StartCoordinate, result.ExitCoordinate),
                    $"시드 {seed}에서 탈출 지점에 도달할 수 없습니다.");
                Assert.AreNotEqual(result.StartCoordinate, result.ExitCoordinate,
                    $"시드 {seed}의 탈출 지점이 시작 지점과 같습니다.");
            }
        }

        /// <summary>
        /// 같은 시드로 생성하면 완전히 같은 결과가 나오는지 확인합니다.
        /// </summary>
        [Test]
        public void 같은_시드는_같은_미로를_만듭니다()
        {
            MazeGenerationSettings settings = CreateSettings();
            MazeFactory factory = new MazeFactory();

            for (int seed = 0; seed < SampleSeedCount; seed += 1)
            {
                MazeBuildResult first = factory.Build(settings, seed);
                MazeBuildResult second = factory.Build(settings, seed);

                Assert.AreEqual(first.StartCoordinate, second.StartCoordinate, $"시드 {seed} 시작 지점 불일치");
                Assert.AreEqual(first.ExitCoordinate, second.ExitCoordinate, $"시드 {seed} 탈출 지점 불일치");
                CollectionAssert.AreEqual(ToWallList(first.Grid), ToWallList(second.Grid),
                    $"시드 {seed}의 벽 배치가 서로 다릅니다.");
            }
        }

        /// <summary>
        /// 다른 시드는 서로 다른 미로를 만드는지 확인합니다.
        /// </summary>
        [Test]
        public void 다른_시드는_다른_미로를_만듭니다()
        {
            MazeGenerationSettings settings = CreateSettings();
            MazeFactory factory = new MazeFactory();

            List<EMazeDirection> firstWalls = ToWallList(factory.Build(settings, 1).Grid);
            List<EMazeDirection> secondWalls = ToWallList(factory.Build(settings, 2).Grid);

            CollectionAssert.AreNotEqual(firstWalls, secondWalls, "서로 다른 시드가 같은 미로를 만들었습니다.");
        }

        /// <summary>
        /// 모든 칸이 하나로 이어져 고립된 구역이 없는지 확인합니다.
        /// </summary>
        [Test]
        public void 생성_결과에_고립된_구역이_없습니다()
        {
            MazeGenerationSettings settings = CreateSettings();
            MazeFactory factory = new MazeFactory();

            for (int seed = 0; seed < SampleSeedCount; seed += 1)
            {
                MazeBuildResult result = factory.Build(settings, seed);

                Assert.AreEqual(1, result.Statistics.RegionCount,
                    $"시드 {seed}에 고립된 구역이 있습니다.");
            }
        }

        /// <summary>
        /// 조건을 만족할 수 없는 설정에서 무한 재시도에 빠지지 않는지 확인합니다.
        /// </summary>
        [Test]
        public void 만족할_수_없는_설정은_재시도_상한에서_실패로_끝납니다()
        {
            // 재시도 상한을 넘기면 MazeFactory가 의도적으로 에러 로그를 남기므로 실패로 세지 않습니다.
            LogAssert.ignoreFailingMessages = true;

            MazeGenerationSettings impossibleSettings = new MazeGenerationSettings(4, 4, 1000, 0f, 3, 0, 2, 2, 0);
            MazeFactory factory = new MazeFactory();

            MazeBuildResult result = factory.Build(impossibleSettings, 12345);

            Assert.IsFalse(result.IsSuccess, "만족할 수 없는 설정인데 성공으로 처리되었습니다.");
            Assert.AreEqual(3, result.AttemptCount, "재시도 상한만큼만 시도해야 합니다.");
            Assert.IsNotEmpty(result.FailureReason, "실패 사유가 비어 있습니다.");
        }

        /// <summary>
        /// 시야가 트인 넓은 홀이 반드시 만들어지는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 홀이 없으면 체크리스트가 요구하는 최악 구간 프레임레이트를 잴 대상 자체가 없습니다.
        /// </remarks>
        [Test]
        public void 생성_결과에는_넓은_홀이_존재합니다()
        {
            MazeGenerationSettings settings = CreateSettings();
            MazeFactory factory = new MazeFactory();

            for (int seed = 0; seed < SampleSeedCount; seed += 1)
            {
                MazeBuildResult result = factory.Build(settings, seed);

                Assert.GreaterOrEqual(result.Statistics.LargestOpenAreaCellCount,
                    settings.MinimumLargestOpenAreaCellCount,
                    $"시드 {seed}의 가장 넓은 트인 구역이 최소치에 못 미칩니다.");
            }
        }

        /// <summary>
        /// 격자의 벽 배치를 비교하기 쉬운 목록으로 만듭니다.
        /// </summary>
        /// <param name="grid">변환할 격자입니다.</param>
        /// <returns>칸 순서대로 담긴 벽 조합 목록입니다.</returns>
        private static List<EMazeDirection> ToWallList(MazeGrid grid)
        {
            List<EMazeDirection> walls = new List<EMazeDirection>(grid.CellCount);

            foreach (MazeCoordinate coordinate in grid.EnumerateCoordinates())
                walls.Add(grid.GetWalls(coordinate));

            return walls;
        }
        #endregion // 함수
    }
}
