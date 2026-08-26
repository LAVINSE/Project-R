using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Debugging;
using SW.Util;

using ProjectR.Backrooms.Generation;

namespace ProjectR.Backrooms.Assembly
{
    /// <summary>
    /// 생성된 미로 데이터를 받아 실행 중에 타일 프리팹으로 조립하는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 실행 중에는 조립만 합니다. 조명은 타일 프리팹에 미리 구워 두었으므로
    /// 여기서 실시간 라이트를 새로 만들지 않습니다.
    /// 한 프레임에 전부 조립하면 눈에 띄게 멈추므로 프레임을 나눠 배치합니다.
    /// </remarks>
    public class BackroomsMapBuilder : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("생성 설정")]
        [SerializeField, Tooltip("미로 생성에 사용할 설정입니다.")]
        private MazeGenerationSettings generationSettings = new MazeGenerationSettings();

        [SWGroup("타일")]
        [SerializeField, Tooltip("칸 모양에 맞는 타일 프리팹을 찾아 주는 라이브러리입니다.")]
        private MazeTileLibrary tileLibrary;

        [SWGroup("조립")]
        [SerializeField, Range(1, 512), Tooltip("한 프레임에 배치할 타일의 최대 개수입니다.")]
        private int tilesPerFrame = 24;

        [SerializeField, Tooltip("조립이 끝난 뒤 정적 배칭으로 합쳐 드로우콜을 줄일지 여부입니다.")]
        private bool useStaticBatching = true;

        [SWGroup("시드")]
        [SerializeField, Tooltip("켜면 실행할 때마다 새 시드를 뽑고, 끄면 아래 시드를 그대로 씁니다.")]
        private bool useRandomSeed = true;

        [SerializeField, SWCondition("useRandomSeed", false), Tooltip("재현에 사용할 고정 시드입니다.")]
        private int fixedSeed;

        [SWGroup("자동 실행")]
        [SerializeField, Tooltip("씬이 시작될 때 맵을 바로 만들지 여부입니다.")]
        private bool buildOnStart = true;

        /// <summary>배치한 타일을 담아 두는 부모 오브젝트입니다.</summary>
        private Transform tileRoot;

        /// <summary>진행 중인 조립 코루틴입니다. 없으면 null입니다.</summary>
        private Coroutine buildRoutine;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>가장 최근 생성 결과입니다. 아직 생성하지 않았으면 null입니다.</summary>
        public MazeBuildResult LastResult { get; private set; }

        /// <summary>가장 최근에 사용한 시드입니다.</summary>
        public int LastSeed { get; private set; }

        /// <summary>조립이 진행 중인지 여부입니다.</summary>
        public bool IsBuilding => buildRoutine != null;

        /// <summary>미로 데이터를 만드는 데 걸린 시간(밀리초)입니다.</summary>
        public double LastGenerationMilliseconds { get; private set; }

        /// <summary>타일을 배치하는 데 걸린 시간(밀리초)입니다.</summary>
        public double LastAssemblyMilliseconds { get; private set; }
        #endregion // 프로퍼티

        #region 이벤트
        /// <summary>맵 조립이 끝났을 때 생성 결과와 함께 발생합니다.</summary>
        public event Action<MazeBuildResult> MapBuilt;
        #endregion // 이벤트

        #region 함수
        /// <summary>
        /// 타일을 담을 부모 오브젝트를 준비하고 디버그 감시 항목을 등록합니다.
        /// </summary>
        private void Awake()
        {
            tileRoot = new GameObject("TileRoot").transform;
            tileRoot.SetParent(transform, false);

            SWDebugConsole.RegisterInstance(this);
            SWDebugConsole.Watch("백룸 시드", () => LastSeed.ToString());
            SWDebugConsole.Watch("백룸 타일 수", () => LastResult == null ? "0" : LastResult.Grid.CellCount.ToString());
        }

        /// <summary>
        /// 설정에 따라 씬 시작과 함께 맵을 만듭니다.
        /// </summary>
        private void Start()
        {
            if (buildOnStart) Build();
        }

        /// <summary>
        /// 디버그 감시 항목을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            SWDebugConsole.Unwatch("백룸 시드");
            SWDebugConsole.Unwatch("백룸 타일 수");
            SWDebugConsole.UnregisterInstance(this);
        }

        /// <summary>
        /// 설정에 따라 시드를 정하고 맵을 만듭니다.
        /// </summary>
        public void Build()
        {
            Build(useRandomSeed ? Environment.TickCount : fixedSeed);
        }

        /// <summary>
        /// 지정한 시드로 맵을 만듭니다.
        /// </summary>
        /// <param name="seed">재현에 사용할 시드입니다.</param>
        public void Build(int seed)
        {
            if (tileLibrary == null)
            {
                SWLog.LogError($"[{nameof(BackroomsMapBuilder)}] 타일 라이브러리가 비어 있어 조립할 수 없습니다.");
                return;
            }

            if (IsBuilding)
            {
                StopCoroutine(buildRoutine);
                buildRoutine = null;
            }

            LastSeed = seed;
            buildRoutine = StartCoroutine(BuildRoutine(seed));
        }

        /// <summary>
        /// 칸 좌표에 해당하는 월드 위치를 구합니다.
        /// </summary>
        /// <param name="coordinate">변환할 칸 좌표입니다.</param>
        /// <returns>타일 바닥 중앙의 월드 위치입니다.</returns>
        public Vector3 GetWorldPosition(MazeCoordinate coordinate)
        {
            float cellSize = tileLibrary != null ? tileLibrary.CellSize : 1f;

            return tileRoot.position + new Vector3(coordinate.X * cellSize, 0f, coordinate.Y * cellSize);
        }

        /// <summary>
        /// 미로를 만들고 타일을 프레임에 나누어 배치합니다.
        /// </summary>
        /// <param name="seed">재현에 사용할 시드입니다.</param>
        /// <returns>코루틴 열거자입니다.</returns>
        private IEnumerator BuildRoutine(int seed)
        {
            ClearTiles();

            Stopwatch generationWatch = Stopwatch.StartNew();
            MazeFactory factory = new MazeFactory();
            MazeBuildResult result = factory.Build(generationSettings, seed);
            generationWatch.Stop();

            LastResult = result;
            LastGenerationMilliseconds = generationWatch.Elapsed.TotalMilliseconds;

            Stopwatch assemblyWatch = Stopwatch.StartNew();
            int placedCount = 0;

            foreach (MazeCoordinate coordinate in result.Grid.EnumerateCoordinates())
            {
                PlaceTile(result.Grid, coordinate);
                placedCount += 1;

                if (placedCount % tilesPerFrame != 0) continue;

                assemblyWatch.Stop();
                yield return null;
                assemblyWatch.Start();
            }

            if (useStaticBatching) StaticBatchingUtility.Combine(tileRoot.gameObject);

            assemblyWatch.Stop();
            LastAssemblyMilliseconds = assemblyWatch.Elapsed.TotalMilliseconds;
            buildRoutine = null;

            SWLog.Log($"[{nameof(BackroomsMapBuilder)}] 맵 조립 완료. {result.ToSummary()}");
            SWLog.Log($"[{nameof(BackroomsMapBuilder)}] 생성 {LastGenerationMilliseconds:F1}ms / " +
                $"조립 {LastAssemblyMilliseconds:F1}ms / 타일 {placedCount}개");

            MapBuilt?.Invoke(result);
        }

        /// <summary>
        /// 칸 하나에 맞는 타일을 배치합니다.
        /// </summary>
        /// <param name="grid">배치 기준이 되는 격자입니다.</param>
        /// <param name="coordinate">배치할 칸의 좌표입니다.</param>
        private void PlaceTile(MazeGrid grid, MazeCoordinate coordinate)
        {
            EMazeDirection openings = EMazeDirection.All & ~grid.GetWalls(coordinate);

            if (tileLibrary.TryGetTile(openings, out GameObject tilePrefab, out int rotationSteps) == false) return;

            Instantiate(tilePrefab, GetWorldPosition(coordinate),
                Quaternion.Euler(0f, 90f * rotationSteps, 0f), tileRoot);
        }

        /// <summary>
        /// 배치해 둔 타일을 모두 지웁니다.
        /// </summary>
        private void ClearTiles()
        {
            List<GameObject> tiles = new List<GameObject>(tileRoot.childCount);

            for (int index = 0; index < tileRoot.childCount; index += 1)
                tiles.Add(tileRoot.GetChild(index).gameObject);

            for (int index = 0; index < tiles.Count; index += 1)
                Destroy(tiles[index]);
        }

        /// <summary>
        /// 디버그 콘솔에서 지정한 시드로 맵을 다시 만듭니다.
        /// </summary>
        /// <param name="seed">재현에 사용할 시드입니다.</param>
        [SWCommand("map.regenerate", "지정한 시드로 맵을 다시 만듭니다.", "백룸")]
        private void RegenerateWithSeed(int seed)
        {
            Build(seed);
        }

        /// <summary>
        /// 디버그 콘솔에서 현재 시드와 통계를 출력합니다.
        /// </summary>
        [SWCommand("map.seed", "현재 맵의 시드와 통계를 출력합니다.", "백룸")]
        private void PrintSeedCommand()
        {
            PrintLastResult();
        }

        /// <summary>
        /// 인스펙터에서 맵을 다시 만듭니다.
        /// </summary>
        [SWButton("맵 재생성")]
        private void RebuildFromInspector()
        {
            if (Application.isPlaying == false)
            {
                SWLog.LogWarning($"[{nameof(BackroomsMapBuilder)}] 조립은 프레임을 나눠 진행하므로 플레이 중에만 됩니다.");
                return;
            }

            Build();
        }

        /// <summary>
        /// 인스펙터에서 마지막 생성 결과를 로그로 출력합니다.
        /// </summary>
        [SWButton("현재 시드와 통계 출력")]
        private void PrintLastResult()
        {
            if (LastResult == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsMapBuilder)}] 아직 생성된 맵이 없습니다.");
                return;
            }

            SWLog.Log($"[{nameof(BackroomsMapBuilder)}] {LastResult.ToSummary()}");
        }
        #endregion // 함수
    }
}
