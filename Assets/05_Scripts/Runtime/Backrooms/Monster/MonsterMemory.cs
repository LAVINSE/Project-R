using System.Collections.Generic;

using UnityEngine;

using SW.Attributes;
using SW.Base;

namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// 몬스터가 플레이어에 대해 "기억하는 것"을 담아 두는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 플레이어는 몬스터의 내부 판단을 볼 수 없으므로, 진짜 학습이 아니라
    /// 알고 있는 것처럼 보이는 최소한의 기록만 둡니다.
    /// 자주 앉아 숨은 자리를 세어 두었다가 수색할 때 먼저 확인하면
    /// 플레이어는 "여기도 알고 있다"고 느낍니다. 실제로는 세어 둔 값 하나뿐입니다.
    /// </remarks>
    public class MonsterMemory : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("은신처 기억")]
        [SerializeField, Range(1, 12), Tooltip("기억해 둘 은신처의 최대 개수입니다.")]
        private int maximumSpotCount = 6;

        [SerializeField, Min(0.5f), Tooltip("같은 은신처로 묶을 거리(미터)입니다.")]
        private float sameSpotDistance = 4f;

        [SerializeField, Min(1f), Tooltip("수색할 때 은신처를 떠올릴 최대 거리(미터)입니다.")]
        private float recallDistance = 18f;

        [SWGroup("수색 가중치")]
        [SerializeField, Range(0f, 1f), Tooltip("수색 지점을 고를 때 기억해 둔 은신처를 고를 확률입니다.")]
        private float recallChance = 0.45f;

        [SerializeField, Range(1f, 4f), Tooltip("클수록 수색 지점이 마지막 목격 위치에 붙습니다.")]
        private float centerBias = 2f;

        /// <summary>기억해 둔 은신처 목록입니다.</summary>
        private readonly List<HidingSpot> hidingSpots = new List<HidingSpot>();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>기억해 둔 은신처의 개수입니다.</summary>
        public int HidingSpotCount => hidingSpots.Count;
        #endregion // 프로퍼티

        #region 데이터
        /// <summary>
        /// 플레이어가 몸을 숨겼던 자리 하나의 기록입니다.
        /// </summary>
        private struct HidingSpot
        {
            /// <summary>숨었던 자리의 월드 위치입니다.</summary>
            public Vector3 Position;

            /// <summary>그 자리에서 숨은 것을 몇 번 봤는지입니다.</summary>
            public int UseCount;
        }
        #endregion // 데이터

        #region 함수
        /// <summary>
        /// 플레이어가 몸을 숨긴 자리를 기억합니다.
        /// </summary>
        /// <param name="position">숨은 자리의 월드 위치입니다.</param>
        /// <remarks>가까운 자리는 같은 은신처로 묶어 횟수만 올립니다.</remarks>
        public void RecordHidingSpot(Vector3 position)
        {
            for (int index = 0; index < hidingSpots.Count; index += 1)
            {
                if (Vector3.Distance(hidingSpots[index].Position, position) > sameSpotDistance) continue;

                HidingSpot known = hidingSpots[index];
                known.UseCount += 1;
                hidingSpots[index] = known;

                return;
            }

            if (hidingSpots.Count >= maximumSpotCount) RemoveLeastUsedSpot();

            hidingSpots.Add(new HidingSpot { Position = position, UseCount = 1 });
        }

        /// <summary>
        /// 다음에 뒤져 볼 수색 지점을 고릅니다.
        /// </summary>
        /// <param name="lastSeenPosition">플레이어를 마지막으로 본 위치입니다.</param>
        /// <param name="searchRadius">마지막 목격 위치에서 퍼져 나갈 최대 거리(미터)입니다.</param>
        /// <returns>뒤져 볼 월드 위치입니다.</returns>
        /// <remarks>
        /// 기억해 둔 은신처가 근처에 있으면 확률에 따라 그쪽을 먼저 봅니다.
        /// 그렇지 않으면 마지막 목격 위치에 가까운 쪽이 더 자주 뽑히도록 치우쳐 고릅니다.
        /// </remarks>
        public Vector3 GetSearchPoint(Vector3 lastSeenPosition, float searchRadius)
        {
            if (Random.value < recallChance &&
                TryGetFavouriteSpot(lastSeenPosition, out Vector3 remembered))
            {
                return remembered;
            }

            float distance = searchRadius * Mathf.Pow(Random.value, centerBias);
            Vector2 direction = Random.insideUnitCircle.normalized;

            return lastSeenPosition + new Vector3(direction.x, 0f, direction.y) * distance;
        }

        /// <summary>
        /// 기준 위치 근처에서 가장 자주 쓰인 은신처를 찾습니다.
        /// </summary>
        /// <param name="nearPosition">기준이 될 월드 위치입니다.</param>
        /// <param name="position">찾은 은신처의 위치입니다. 없으면 기준 위치를 그대로 돌려줍니다.</param>
        /// <returns>은신처를 찾았으면 true를 반환합니다.</returns>
        public bool TryGetFavouriteSpot(Vector3 nearPosition, out Vector3 position)
        {
            int bestIndex = -1;
            int bestUseCount = 0;

            for (int index = 0; index < hidingSpots.Count; index += 1)
            {
                if (Vector3.Distance(hidingSpots[index].Position, nearPosition) > recallDistance) continue;
                if (hidingSpots[index].UseCount <= bestUseCount) continue;

                bestIndex = index;
                bestUseCount = hidingSpots[index].UseCount;
            }

            position = bestIndex >= 0 ? hidingSpots[bestIndex].Position : nearPosition;

            return bestIndex >= 0;
        }

        /// <summary>
        /// 가장 적게 쓰인 은신처를 목록에서 지웁니다.
        /// </summary>
        private void RemoveLeastUsedSpot()
        {
            int worstIndex = 0;

            for (int index = 1; index < hidingSpots.Count; index += 1)
            {
                if (hidingSpots[index].UseCount < hidingSpots[worstIndex].UseCount) worstIndex = index;
            }

            hidingSpots.RemoveAt(worstIndex);
        }
        #endregion // 함수
    }
}
