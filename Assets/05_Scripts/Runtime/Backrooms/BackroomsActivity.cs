using System;
using System.Collections.Generic;

using SW.Util;

using ProjectR.Activity;
using ProjectR.Core;
using ProjectR.Data;
using ProjectR.Inventory;

namespace ProjectR.Backrooms
{
    /// <summary>
    /// 백룸 탐험을 하나의 활동으로 표현하는 구현체입니다.
    /// </summary>
    /// <remarks>
    /// 이번 탐험 동안의 가방을 들고 있습니다. 가방을 씬이 아니라 활동이 들고 있어야
    /// 씬을 벗어나는 순간에도 무엇을 들고 나왔는지가 남아 정산할 수 있습니다.
    /// 정산 수치는 주울 때가 아니라 나올 때 셉니다. 버린 물건이 수치에 남으면 안 되기 때문입니다.
    /// </remarks>
    public class BackroomsActivity : IActivity
    {
        #region 상수
        /// <summary>가방 스탯을 찾지 못했을 때 쓰는 가로 칸 수입니다.</summary>
        /// <remarks>부르는 쪽이 스탯을 못 읽었을 때 넘겨 줄 값이라 공개해 둡니다.</remarks>
        public const int DefaultBackpackWidth = 6;

        /// <summary>가방 스탯을 찾지 못했을 때 쓰는 세로 칸 수입니다.</summary>
        public const int DefaultBackpackHeight = 4;

        /// <summary>백룸 탐험 한 번에 드는 방송 시간(분)입니다.</summary>
        /// <remarks>
        /// 들어가는 순간 한 번에 빠집니다. 탐험 안에는 제한 시간이 없습니다.
        /// 층이 깊어지면 더 드는 것은 층 정의가 생길 때 붙입니다.
        /// 화면이 탐험을 만들지 않고도 비용을 물어볼 수 있어야 하므로 공개해 둡니다.
        /// </remarks>
        public const int BroadcastMinutes = 60;
        #endregion // 상수

        #region 필드
        /// <summary>실패했을 때 무엇을 잃을지 정하는 규칙입니다.</summary>
        private readonly ILossPolicy lossPolicy;

        /// <summary>가방에 담긴 실체 번호마다의 이상물체 정의입니다.</summary>
        private readonly Dictionary<int, AnomalyDefinition> carriedDefinitions =
            new();

        /// <summary>이번 탐험에서 모인 결과입니다. 탐험 중에 갱신됩니다.</summary>
        private ActivityResult result;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>백룸 탐험에 들어갈 때 소비하는 방송 시간(분)입니다.</summary>
        public int BroadcastCost => BroadcastMinutes;

        /// <summary>이번 탐험에서 들고 다니는 가방입니다.</summary>
        public GridInventory Backpack { get; }

        /// <summary>이번 탐험이 이미 실패로 표시되었는지 여부입니다.</summary>
        public bool IsFailure => result != null && result.IsFailure;
        #endregion // 프로퍼티

        #region 이벤트
        /// <summary>가방에 든 것이 달라졌을 때 호출됩니다.</summary>
        public event Action BackpackChanged;

        /// <summary>가방에서 물건을 버렸을 때 호출됩니다.</summary>
        /// <remarks>버린 물건을 월드 어디에 되돌려 놓을지는 이 알림을 받은 씬이 정합니다.</remarks>
        public event Action<AnomalyDefinition> AnomalyDropped;
        #endregion // 이벤트

        #region 생성자
        /// <summary>
        /// 기본 가방 크기와 전량 소실 규칙으로 탐험을 만듭니다.
        /// </summary>
        public BackroomsActivity()
            : this(DefaultBackpackWidth, DefaultBackpackHeight, new TotalLossPolicy())
        {
        }

        /// <summary>
        /// 가방 크기와 손실 규칙을 지정해 탐험을 만듭니다.
        /// </summary>
        /// <param name="backpackWidth">가방의 가로 칸 수입니다.</param>
        /// <param name="backpackHeight">가방의 세로 칸 수입니다.</param>
        /// <param name="lossPolicy">실패했을 때 적용할 손실 규칙입니다.</param>
        /// <remarks>가방 용량은 이후 업그레이드로 늘어나므로 바깥에서 지정할 수 있게 열어 둡니다.</remarks>
        public BackroomsActivity(int backpackWidth, int backpackHeight, ILossPolicy lossPolicy)
        {
            Backpack = new GridInventory(backpackWidth, backpackHeight);
            this.lossPolicy = lossPolicy ?? new TotalLossPolicy();
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 백룸에 진입할 수 있는지 판정합니다.
        /// </summary>
        /// <param name="state">판정에 사용할 게임 상태입니다.</param>
        /// <returns>진입할 수 있으면 true를 반환합니다.</returns>
        /// <remarks>층 해금 조건과 장비 조건은 이후 단계에서 추가합니다.</remarks>
        public bool CanEnter(GameState state)
        {
            return state != null;
        }

        /// <summary>
        /// 백룸 씬으로 이동해 탐험을 시작합니다.
        /// </summary>
        /// <param name="state">활동이 참조할 게임 상태입니다.</param>
        public void Begin(GameState state)
        {
            result = ActivityResult.Empty();

            Backpack.Clear();
            carriedDefinitions.Clear();

            SWLog.Log($"[{nameof(BackroomsActivity)}] 백룸 탐험을 시작합니다.");
            SceneFlow.ChangeScene(SceneNames.Backrooms);
        }

        /// <summary>
        /// 이상물체를 주워 가방에 넣습니다.
        /// </summary>
        /// <param name="definition">주운 이상물체의 정의입니다.</param>
        /// <param name="placed">가방에 놓인 자리입니다. 실패하면 null입니다.</param>
        /// <returns>넣었으면 true를 반환합니다. 자리가 없으면 false를 반환합니다.</returns>
        public bool TryCollect(AnomalyDefinition definition, out PlacedItem placed)
        {
            placed = null;

            if (definition == null)
            {
                SWLog.LogError($"[{nameof(BackroomsActivity)}] 이상물체 정의가 null이라 주울 수 없습니다.");
                return false;
            }

            if (Backpack.TryAdd(definition.DefinitionId, definition.Shape, out placed) == false) return false;

            carriedDefinitions.Add(placed.InstanceId, definition);
            BackpackChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// 가방 안의 물건을 다른 자리로 옮기거나 돌립니다.
        /// </summary>
        /// <param name="instanceId">옮길 물건의 실체 번호입니다.</param>
        /// <param name="position">새 왼쪽 위 칸 좌표입니다.</param>
        /// <param name="isRotated">새 회전 여부입니다.</param>
        /// <returns>옮겼으면 true를 반환합니다.</returns>
        public bool TryRearrange(int instanceId, GridPosition position, bool isRotated)
        {
            if (Backpack.TryMove(instanceId, position, isRotated) == false) return false;

            BackpackChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// 가방에서 물건을 꺼내 버립니다.
        /// </summary>
        /// <param name="instanceId">버릴 물건의 실체 번호입니다.</param>
        /// <param name="definition">버린 물건의 정의입니다. 실패하면 null입니다.</param>
        /// <returns>버렸으면 true를 반환합니다.</returns>
        /// <remarks>
        /// 버린 물건을 월드 어디에 되돌려 놓을지는 씬이 정합니다.
        /// 버리면 그대로 사라지게 하면 무엇을 버릴지 고민하는 대신 아무거나 버리게 됩니다.
        /// </remarks>
        public bool TryDrop(int instanceId, out AnomalyDefinition definition)
        {
            if (carriedDefinitions.TryGetValue(instanceId, out definition) == false) return false;
            if (Backpack.Remove(instanceId) == false) return false;

            carriedDefinitions.Remove(instanceId);
            BackpackChanged?.Invoke();
            AnomalyDropped?.Invoke(definition);

            return true;
        }

        /// <summary>
        /// 가방에 담긴 물건의 정의를 찾습니다.
        /// </summary>
        /// <param name="instanceId">찾을 물건의 실체 번호입니다.</param>
        /// <returns>물건의 정의입니다. 없으면 null을 반환합니다.</returns>
        public AnomalyDefinition GetDefinition(int instanceId)
        {
            return carriedDefinitions.TryGetValue(instanceId, out AnomalyDefinition definition) ? definition : null;
        }

        /// <summary>
        /// 이번 탐험을 실패로 표시합니다.
        /// </summary>
        /// <param name="reason">실패 사유입니다. 결과의 표식으로 남습니다.</param>
        /// <remarks>
        /// 실패해도 탐험은 <see cref="End"/>로 똑같이 끝납니다.
        /// 무엇을 잃는지는 <see cref="ILossPolicy"/>가 정합니다.
        /// </remarks>
        public void MarkFailure(string reason)
        {
            result ??= ActivityResult.Empty();

            if (result.IsFailure) return;

            result.IsFailure = true;
            result.Flags.Add(reason);

            SWLog.Log($"[{nameof(BackroomsActivity)}] 탐험이 실패로 끝났습니다: {reason}");
        }

        /// <summary>
        /// 탐험을 끝내고 결과를 돌려줍니다.
        /// </summary>
        /// <returns>게임 상태에 반영할 활동 결과입니다.</returns>
        public ActivityResult End()
        {
            result ??= ActivityResult.Empty();

            Settle();

            SWLog.Log($"[{nameof(BackroomsActivity)}] 백룸 탐험을 종료합니다. " +
                $"이상물체 {result.Items.Count}개 / 후원금 {result.DonationDelta} / 시청자 {result.ViewerDelta}");

            return result;
        }

        /// <summary>
        /// 가방에 남아 있는 것을 세어 결과에 옮겨 담습니다.
        /// </summary>
        private void Settle()
        {
            List<ItemInstance> carriedItems = new();

            result.Items.Clear();
            result.DonationDelta = 0;
            result.ViewerDelta = 0;

            foreach (PlacedItem item in Backpack.Items)
            {
                AnomalyDefinition definition = GetDefinition(item.InstanceId);

                if (definition == null) continue;

                carriedItems.Add(new ItemInstance(definition.DefinitionId));
                result.Items.Add(new ItemInstance(definition.DefinitionId));
                result.DonationDelta += definition.DonationBonus;
                result.ViewerDelta += definition.ViewerBonus;
            }

            if (result.IsFailure == false) return;

            // 성공했을 때의 결과를 먼저 다 만들어 두고 손실 규칙이 그것을 깎게 합니다.
            // 완화 단계에서 일부만 남기는 규칙으로 바꿔도 여기는 그대로 둘 수 있습니다.
            lossPolicy.ApplyFailure(result, carriedItems);
        }
        #endregion // 함수
    }
}
