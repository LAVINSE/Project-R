using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Backrooms.Generation;

namespace ProjectR.Backrooms.Assembly
{
    /// <summary>
    /// 칸의 통로 모양에 맞는 타일 프리팹과 회전을 찾아 주는 에셋입니다.
    /// </summary>
    /// <remarks>
    /// 통로 조합은 열여섯 가지지만 회전을 쓰면 프리팹 다섯 종으로 전부 덮을 수 있습니다.
    /// 프리팹 수를 줄이면 미리 구워 둘 라이트맵 장수도 함께 줄어듭니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "MazeTileLibrary", menuName = "프로젝트R/백룸/타일 라이브러리")]
    public class MazeTileLibrary : SWScriptableObject
    {
        #region 필드
        [SWGroup("타일 프리팹")]
        [SerializeField, Tooltip("북쪽 한 방향만 열린 막다른 길 타일입니다.")]
        private GameObject deadEndTile;

        [SerializeField, Tooltip("북쪽과 남쪽이 열린 직선 복도 타일입니다.")]
        private GameObject straightTile;

        [SerializeField, Tooltip("북쪽과 동쪽이 열린 모퉁이 타일입니다.")]
        private GameObject cornerTile;

        [SerializeField, Tooltip("남쪽만 막힌 삼거리 타일입니다.")]
        private GameObject tJunctionTile;

        [SerializeField, Tooltip("네 방향이 모두 열린 사거리 타일입니다.")]
        private GameObject crossTile;

        [SWGroup("배치")]
        [SerializeField, Min(0.1f), Tooltip("타일 한 칸의 한 변 길이(미터)입니다. 프리팹 크기와 반드시 같아야 합니다.")]
        private float cellSize = 4f;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>타일 한 칸의 한 변 길이(미터)입니다.</summary>
        public float CellSize => cellSize;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 통로 조합에 맞는 타일 프리팹과 회전 단계를 찾습니다.
        /// </summary>
        /// <param name="openings">칸에서 열려 있는 방향의 조합입니다.</param>
        /// <param name="tilePrefab">찾은 타일 프리팹입니다. 찾지 못하면 null입니다.</param>
        /// <param name="rotationSteps">시계 방향으로 몇 번 90도 돌려야 하는지입니다.</param>
        /// <returns>맞는 타일을 찾았으면 true를 반환합니다.</returns>
        public bool TryGetTile(EMazeDirection openings, out GameObject tilePrefab, out int rotationSteps)
        {
            if (TryMatch(deadEndTile, EMazeDirection.North, openings, out rotationSteps))
            {
                tilePrefab = deadEndTile;
                return true;
            }

            if (TryMatch(straightTile, EMazeDirection.North | EMazeDirection.South, openings, out rotationSteps))
            {
                tilePrefab = straightTile;
                return true;
            }

            if (TryMatch(cornerTile, EMazeDirection.North | EMazeDirection.East, openings, out rotationSteps))
            {
                tilePrefab = cornerTile;
                return true;
            }

            if (TryMatch(tJunctionTile,
                EMazeDirection.North | EMazeDirection.East | EMazeDirection.West, openings, out rotationSteps))
            {
                tilePrefab = tJunctionTile;
                return true;
            }

            if (TryMatch(crossTile, EMazeDirection.All, openings, out rotationSteps))
            {
                tilePrefab = crossTile;
                return true;
            }

            SWLog.LogError($"[{nameof(MazeTileLibrary)}] 통로 조합에 맞는 타일이 없습니다: {openings}");

            tilePrefab = null;
            rotationSteps = 0;

            return false;
        }

        /// <summary>
        /// 타일의 기준 통로 조합을 돌려서 목표 조합과 맞는지 확인합니다.
        /// </summary>
        /// <param name="tilePrefab">확인할 타일 프리팹입니다. null이면 맞지 않은 것으로 봅니다.</param>
        /// <param name="canonicalOpenings">타일이 돌아가지 않은 상태에서 열려 있는 방향입니다.</param>
        /// <param name="targetOpenings">맞춰야 할 통로 조합입니다.</param>
        /// <param name="rotationSteps">맞는 회전 단계입니다. 맞지 않으면 0입니다.</param>
        /// <returns>회전으로 맞출 수 있으면 true를 반환합니다.</returns>
        private static bool TryMatch(GameObject tilePrefab, EMazeDirection canonicalOpenings,
            EMazeDirection targetOpenings, out int rotationSteps)
        {
            rotationSteps = 0;

            if (tilePrefab == null) return false;

            for (int steps = 0; steps < 4; steps += 1)
            {
                if (RotateClockwise(canonicalOpenings, steps) != targetOpenings) continue;

                rotationSteps = steps;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 방향 조합을 시계 방향으로 90도씩 돌립니다.
        /// </summary>
        /// <param name="directions">돌릴 방향 조합입니다.</param>
        /// <param name="steps">90도를 몇 번 돌릴지입니다.</param>
        /// <returns>돌린 뒤의 방향 조합입니다.</returns>
        /// <remarks>Y축으로 +90도 돌리면 타일의 북쪽이 월드의 동쪽을 향하므로 북 → 동 → 남 → 서 순서로 돕니다.</remarks>
        private static EMazeDirection RotateClockwise(EMazeDirection directions, int steps)
        {
            EMazeDirection rotated = directions;

            for (int step = 0; step < steps; step += 1)
            {
                EMazeDirection next = EMazeDirection.None;

                if ((rotated & EMazeDirection.North) != 0) next |= EMazeDirection.East;
                if ((rotated & EMazeDirection.East) != 0) next |= EMazeDirection.South;
                if ((rotated & EMazeDirection.South) != 0) next |= EMazeDirection.West;
                if ((rotated & EMazeDirection.West) != 0) next |= EMazeDirection.North;

                rotated = next;
            }

            return rotated;
        }
        #endregion // 함수
    }
}
