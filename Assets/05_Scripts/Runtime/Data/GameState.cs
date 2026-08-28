using System;
using System.Collections.Generic;

using UnityEngine;

using SW.Util;

namespace ProjectR.Data
{
    /// <summary>
    /// 씬 전환과 활동 진행에 걸쳐 유지되는 게임 진행 상태입니다.
    /// </summary>
    /// <remarks>
    /// 저장 데이터의 원본이 되는 계층이므로 시스템을 참조하지 않습니다.
    /// 채널 진행도와 스트리머별 진행도의 분리는 1일 시연판 단계에서 도입합니다.
    /// </remarks>
    [Serializable]
    public class GameState
    {
        #region 필드
        [SerializeField] private int day = 1;
        [SerializeField] private int remainingBroadcastMinutes;
        [SerializeField] private int donation;
        [SerializeField] private int viewerCount;
        [SerializeField] private ConditionState condition = new ConditionState();
        [SerializeField] private List<ItemInstance> items = new List<ItemInstance>();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 며칠째인지를 나타냅니다.</summary>
        public int Day => day;

        /// <summary>오늘 남아 있는 방송 시간(분)입니다.</summary>
        /// <remarks>
        /// 활동에 들어갈 때 소비되는 행동력입니다. 탐험 도중에 줄어들지는 않습니다.
        /// 다 쓰면 그날은 더 방송할 수 없습니다.
        /// </remarks>
        public int RemainingBroadcastMinutes => remainingBroadcastMinutes;

        /// <summary>보유 후원금입니다.</summary>
        public int Donation => donation;

        /// <summary>현재 시청자 수입니다.</summary>
        public int ViewerCount => viewerCount;

        /// <summary>현재 컨디션 상태입니다.</summary>
        public ConditionState Condition => condition;

        /// <summary>보유 중인 이상물체 목록입니다. 없으면 빈 목록을 반환합니다.</summary>
        public IReadOnlyList<ItemInstance> Items => items;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 하루의 방송 시간을 설정합니다.
        /// </summary>
        /// <param name="minutes">오늘 사용할 수 있는 방송 시간(분)입니다.</param>
        public void ResetBroadcastTime(int minutes)
        {
            remainingBroadcastMinutes = Mathf.Max(0, minutes);
        }

        /// <summary>
        /// 활동에 들어가며 방송 시간을 소비합니다.
        /// </summary>
        /// <param name="minutes">소비할 방송 시간(분)입니다.</param>
        /// <remarks>
        /// 방송 시간은 활동을 도중에 그만두어도 되돌리지 않습니다.
        /// 되돌릴 수 있게 하면 위기 회피 수단이 되기 때문입니다.
        /// </remarks>
        public void ConsumeBroadcastTime(int minutes)
        {
            if (minutes <= 0) return;

            remainingBroadcastMinutes = Mathf.Max(0, remainingBroadcastMinutes - minutes);
        }

        /// <summary>
        /// 활동 결과를 상태에 반영합니다.
        /// </summary>
        /// <param name="result">반영할 활동 결과입니다.</param>
        public void Apply(ActivityResult result)
        {
            if (result == null)
            {
                SWLog.LogError($"[{nameof(GameState)}] 활동 결과가 null이라 반영을 건너뜁니다.");
                return;
            }

            donation = Mathf.Max(0, donation + result.DonationDelta);
            viewerCount = Mathf.Max(0, viewerCount + result.ViewerDelta);
            condition.Apply(result.Condition);

            if (result.Items != null)
                items.AddRange(result.Items);
        }

        /// <summary>
        /// 다음 날로 넘어갑니다.
        /// </summary>
        /// <param name="minutes">다음 날 사용할 수 있는 방송 시간(분)입니다.</param>
        public void AdvanceDay(int minutes)
        {
            day += 1;
            ResetBroadcastTime(minutes);
        }
        #endregion // 함수
    }
}
