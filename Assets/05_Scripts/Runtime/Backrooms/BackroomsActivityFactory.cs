using System;

using SW.Attributes;

using ProjectR.Activity;
using ProjectR.Data;

namespace ProjectR.Backrooms
{
    /// <summary>
    /// 지금 스탯에 맞는 가방을 들려 백룸 탐험을 만듭니다.
    /// </summary>
    /// <remarks>
    /// 건물 정의가 <c>[SerializeReference]</c>로 이것을 들고 있습니다.
    /// 백룸 어셈블리에 두는 이유는 <see cref="BackroomsActivity"/>가 여기 있기 때문입니다.
    /// 활동 어셈블리에 두면 활동이 백룸을 참조해야 합니다.
    /// <para>
    /// 가방 크기를 <b>만들 때마다 다시 읽습니다.</b> 업그레이드를 산 다음 탐험에서
    /// 격자가 실제로 넓어져야 하기 때문입니다(기획서 설계 원칙 1번).
    /// 한 번 읽어 두면 그 판을 다시 시작하기 전까지 업그레이드가 반영되지 않습니다.
    /// </para>
    /// </remarks>
    [Serializable]
    [SWAddTypeMenu("백룸 탐험")]
    public class BackroomsActivityFactory : IActivityFactory
    {
        #region 프로퍼티
        /// <summary>백룸 탐험 한 번에 드는 방송 시간(분)입니다.</summary>
        public int BroadcastCost => BackroomsActivity.BroadcastMinutes;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 지금 스탯에 맞는 백룸 탐험을 만듭니다.
        /// </summary>
        /// <returns>만들어진 백룸 탐험입니다.</returns>
        public IActivity Create()
        {
            StreamerStatBoard stats = GameManager.Instance.Stats;

            if (stats == null) return new BackroomsActivity();

            int width = stats.GetIntValue(StatKeys.BackpackWidth, BackroomsActivity.DefaultBackpackWidth);
            int height = stats.GetIntValue(StatKeys.BackpackHeight, BackroomsActivity.DefaultBackpackHeight);

            return new BackroomsActivity(width, height, new TotalLossPolicy());
        }
        #endregion // 함수
    }
}
