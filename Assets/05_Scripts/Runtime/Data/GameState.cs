using System;
using System.Collections.Generic;

using UnityEngine;

using SW.Util;

namespace ProjectR.Data
{
    /// <summary>
    /// 씬 전환과 활동 진행에 걸쳐 유지되는 게임 진행 상태이자, 저장 파일의 최상위 구조입니다.
    /// </summary>
    /// <remarks>
    /// 저장 데이터의 원본이 되는 계층이므로 시스템을 참조하지 않습니다.
    /// 안쪽은 기획서 11.2절대로 채널 진행도 하나와 스트리머별 진행도 목록으로 나뉘어 있습니다.
    /// 나누는 이유는 DLC로 스트리머를 추가하기 위해서입니다(기획서 12.3절).
    /// 바깥으로 보이는 프로퍼티는 나누기 전과 같습니다. 부르는 쪽은 진행 중인 스트리머가
    /// 누구인지 몰라도 되고, 스트리머가 여럿이 되어도 부르는 코드가 바뀌지 않습니다.
    /// <see cref="SaveVersion"/>은 초기값을 주지 않습니다. 초기값을 주면 그 필드가 없던 구버전 저장 파일이
    /// 역직렬화될 때 최신 버전으로 읽혀 구버전인 것을 알아볼 수 없게 됩니다.
    /// 새로 만들 때만 <see cref="CurrentSaveVersion"/>을 넣습니다.
    /// </remarks>
    [Serializable]
    public class GameState
    {
        #region 상수
        /// <summary>지금 이 코드가 읽고 쓰는 저장 데이터 구조의 번호입니다.</summary>
        /// <remarks>
        /// 구조를 바꿀 때마다 올립니다. 올리지 않으면 구버전 저장 파일을 새 코드가
        /// 아무 일 없다는 듯이 읽어 조용히 어긋난 상태로 진행합니다.
        /// 1번은 채널 진행도와 스트리머별 진행도를 나눈 구조입니다.
        /// </remarks>
        public const int CurrentSaveVersion = 1;
        #endregion // 상수

        #region 필드
        [SerializeField] private int saveVersion;
        [SerializeField] private string activeStreamerId;
        [SerializeField] private ChannelProgress channel = new ChannelProgress();
        [SerializeField] private List<StreamerProgress> streamers = new List<StreamerProgress>();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 저장 데이터가 만들어진 구조 번호입니다.</summary>
        public int SaveVersion => saveVersion;

        /// <summary>지금 진행 중인 스트리머의 식별자입니다. 정해지지 않았으면 빈 문자열입니다.</summary>
        public string ActiveStreamerId => activeStreamerId ?? string.Empty;

        /// <summary>스트리머가 바뀌어도 이어지는 채널 진행도입니다.</summary>
        public ChannelProgress Channel => channel;

        /// <summary>지금 진행 중인 스트리머의 진행도입니다. 정해지지 않았으면 null입니다.</summary>
        public StreamerProgress ActiveStreamer => FindStreamer(activeStreamerId);

        /// <summary>현재 며칠째인지를 나타냅니다.</summary>
        public int Day => channel.Day;

        /// <summary>오늘 남아 있는 방송 시간(분)입니다.</summary>
        /// <remarks>
        /// 활동에 들어갈 때 소비되는 행동력입니다. 탐험 도중에 줄어들지는 않습니다.
        /// 다 쓰면 그날은 더 방송할 수 없습니다.
        /// </remarks>
        public int RemainingBroadcastMinutes => channel.RemainingBroadcastMinutes;

        /// <summary>오늘 배정받은 방송 시간(분)입니다.</summary>
        public int DailyBroadcastMinutes => channel.DailyBroadcastMinutes;

        /// <summary>보유 후원금입니다.</summary>
        public int Donation => channel.Donation;

        /// <summary>현재 시청자 수입니다. 진행 중인 스트리머가 없으면 0입니다.</summary>
        public int ViewerCount => ActiveStreamer?.ViewerCount ?? 0;

        /// <summary>현재 컨디션 상태입니다. 진행 중인 스트리머가 없으면 null입니다.</summary>
        public ConditionState Condition => ActiveStreamer?.Condition;

        /// <summary>보유 중인 이상물체 목록입니다. 없으면 빈 목록을 반환합니다.</summary>
        public IReadOnlyList<ItemInstance> Items => ActiveStreamer?.Items ?? Array.Empty<ItemInstance>();
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 새 진행 상태를 만듭니다.
        /// </summary>
        /// <remarks>
        /// JSON 역직렬화도 이 생성자를 거치므로 버전은 여기서 넣지 않습니다.
        /// 넣으면 버전 필드가 없던 구버전 저장 파일까지 최신 버전으로 읽힙니다.
        /// 새 진행을 시작하는 자리에서 <see cref="MarkAsCurrentVersion"/>을 부릅니다.
        /// </remarks>
        public GameState() { }

        /// <summary>
        /// 이 진행 상태를 현재 구조 번호로 표시합니다.
        /// </summary>
        /// <remarks>새 진행을 시작할 때만 부릅니다. 불러온 진행 상태에는 부르지 않습니다.</remarks>
        public void MarkAsCurrentVersion()
        {
            saveVersion = CurrentSaveVersion;
        }

        /// <summary>
        /// 진행 중인 스트리머를 정합니다. 진행도가 없으면 새로 만듭니다.
        /// </summary>
        /// <param name="streamerId">진행할 스트리머의 식별자입니다.</param>
        /// <returns>진행 중이 된 스트리머의 진행도입니다.</returns>
        /// <remarks>
        /// 출시 스트리머가 한 명이어도 여러 명 중 하나인 것처럼 다뤄야 나중에 추가할 수 있습니다(기획서 12.3절).
        /// 그래서 이 클래스는 어떤 식별자도 알지 못하고, 부르는 쪽이 넘겨 주는 것만 받습니다.
        /// </remarks>
        public StreamerProgress SelectStreamer(string streamerId)
        {
            if (string.IsNullOrEmpty(streamerId))
            {
                SWLog.LogError($"[{nameof(GameState)}] 스트리머 식별자가 비어 있어 선택하지 않았습니다.");
                return null;
            }

            StreamerProgress found = FindStreamer(streamerId);

            if (found == null)
            {
                found = new StreamerProgress(streamerId);
                streamers.Add(found);
            }

            activeStreamerId = streamerId;

            return found;
        }

        /// <summary>
        /// 식별자로 스트리머 진행도를 찾습니다.
        /// </summary>
        /// <param name="streamerId">찾을 스트리머의 식별자입니다.</param>
        /// <returns>찾은 진행도입니다. 없으면 null을 반환합니다.</returns>
        public StreamerProgress FindStreamer(string streamerId)
        {
            if (string.IsNullOrEmpty(streamerId)) return null;

            for (int i = 0; i < streamers.Count; i++)
            {
                if (streamers[i] == null) continue;
                if (streamers[i].StreamerId != streamerId) continue;

                return streamers[i];
            }

            return null;
        }

        /// <summary>
        /// 하루의 방송 시간을 설정합니다.
        /// </summary>
        /// <param name="minutes">오늘 사용할 수 있는 방송 시간(분)입니다.</param>
        public void ResetBroadcastTime(int minutes)
        {
            channel.ResetBroadcastTime(minutes);
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
            channel.ConsumeBroadcastTime(minutes);
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

            StreamerProgress streamer = ActiveStreamer;

            if (streamer == null)
            {
                SWLog.LogError($"[{nameof(GameState)}] 진행 중인 스트리머가 없어 반영을 건너뜁니다.");
                return;
            }

            channel.AddDonation(result.DonationDelta);
            streamer.AddViewers(result.ViewerDelta);
            streamer.Condition.Apply(result.Condition);
            streamer.AddItems(result.Items);

            channel.RecordActivity(result.IsFailure);
        }

        /// <summary>
        /// 하루 마감 결과를 반영하고 다음 날로 넘어갑니다.
        /// </summary>
        /// <param name="result">반영할 하루 마감 결과입니다.</param>
        /// <remarks>
        /// 유지비와 이탈을 먼저 반영한 뒤 날짜를 넘깁니다. 순서를 바꾸면
        /// 오늘의 유지비가 내일 시청자 수로 계산됩니다.
        /// </remarks>
        public void ApplyDayEnd(DayEndResult result)
        {
            channel.AddDonation(-result.UpkeepCost);
            ActiveStreamer?.AddViewers(-result.ViewerLoss);

            channel.AdvanceDay(result.NextBroadcastMinutes);
        }
        #endregion // 함수
    }
}
