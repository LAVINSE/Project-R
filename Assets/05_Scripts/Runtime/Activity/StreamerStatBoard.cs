using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Debugging;
using SW.Stat;
using SW.Util;

using ProjectR.Data;

namespace ProjectR.Activity
{
    /// <summary>
    /// 스트리머의 스탯을 들고 있으면서 업그레이드 보너스를 얹어 주는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 스탯 계산은 SWUtils의 <see cref="SWStat"/>이 합니다. 이 컴포넌트는 두 가지만 더합니다.
    /// 코드명으로 스탯을 찾는 길과, 보유 업그레이드를 보너스로 옮기는 일입니다.
    /// <para>
    /// <b>왜 직접 만들지 않고 <see cref="SWStat"/>을 쓰는가</b> — 필요한 것이 이미 다 있습니다.
    /// 최종값이 <c>기본값 + 보너스</c>를 상한·하한으로 자른 값이고, 보너스는 <b>출처별로</b> 쌓입니다.
    /// 손전등을 바꾸면 <see cref="SWStat.RemoveBonusValue(object)"/> 한 줄로 이전 손전등 몫만 걷힙니다.
    /// 직접 만들면 결국 같은 출처별 사전을 다시 짜게 됩니다(체크리스트 1.2절).
    /// 스탯 상한(기획서 8.4절)은 <see cref="SWStat.MaxValue"/>가, 값이 바뀔 때의 화면 갱신은
    /// <c>OnValueChanged</c>가 맡습니다.
    /// </para>
    /// <para>
    /// <b>컨디션은 여기 들어오지 않습니다.</b> 컨디션은 출처가 회복 활동 하나뿐이라
    /// 출처별 보너스라는 <see cref="SWStat"/>의 본체를 쓰지 않고,
    /// <see cref="GameState"/> 안에 있어 공짜로 저장되던 것을 잃습니다(진행기록 15.2절).
    /// 컨디션은 오르내리는 상태이고, 스탯은 여러 출처가 합쳐지는 계산입니다.
    /// </para>
    /// <para>
    /// 저장에는 보유 업그레이드 목록만 들어갑니다. 보너스 값은 저장하지 않고 불러올 때 다시 얹습니다.
    /// 값을 저장하면 나중에 효과량을 조정했을 때 이미 산 사람에게 옛 값이 남습니다.
    /// </para>
    /// </remarks>
    public class StreamerStatBoard : SWMonoBehaviour
    {
        #region 상수
        /// <summary>업그레이드가 얹은 보너스를 구분하는 출처 키입니다.</summary>
        /// <remarks>
        /// 출처를 하나로 묶고 업그레이드 식별자를 세부 키로 씁니다.
        /// 그래야 <see cref="SWStat.RemoveBonusValue(object)"/> 한 번으로 업그레이드 몫만 통째로 걷을 수 있고,
        /// 나중에 장비가 얹는 보너스와 섞이지 않습니다.
        /// </remarks>
        private const string UpgradeBonusKey = "Upgrade";
        #endregion // 상수

        #region 필드
        /// <summary>런타임 스탯 복제본을 만들어 관리하는 SWUtils 컴포넌트입니다.</summary>
        [SWGroup("참조")]
        [SerializeField, Tooltip("런타임 스탯 복제본을 만들어 관리하는 SWUtils 컴포넌트입니다.")]
        private SWStats stats;

        /// <summary>코드명으로 스탯 정의를 찾을 데이터베이스입니다.</summary>
        [SerializeField, Tooltip("코드명으로 스탯 정의를 찾을 데이터베이스입니다.")]
        private SWIODatabase statDatabase;

        /// <summary>코드명으로 업그레이드 정의를 찾을 데이터베이스입니다.</summary>
        [SerializeField, Tooltip("코드명으로 업그레이드 정의를 찾을 데이터베이스입니다.")]
        private SWIODatabase upgradeDatabase;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 디버그 콘솔에 이 컴포넌트의 명령을 등록합니다.
        /// </summary>
        private void OnEnable()
        {
            SWDebugConsole.RegisterInstance(this);
        }

        /// <summary>
        /// 등록해 둔 명령을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            SWDebugConsole.UnregisterInstance(this);
        }

        /// <summary>
        /// 코드명으로 런타임 스탯을 찾습니다.
        /// </summary>
        /// <param name="statCode">찾을 스탯의 코드명입니다.</param>
        /// <returns>찾은 런타임 스탯입니다. 없으면 null을 반환합니다.</returns>
        public SWStat Find(string statCode)
        {
            if (statDatabase == null || stats == null) return null;

            SWStat definition = statDatabase.GetDataByCodeName<SWStat>(statCode);

            if (definition == null) return null;

            return stats.GetStat(definition);
        }

        /// <summary>
        /// 코드명으로 업그레이드 정의를 찾습니다.
        /// </summary>
        /// <param name="upgradeId">찾을 업그레이드의 식별자입니다.</param>
        /// <returns>찾은 업그레이드 정의입니다. 없으면 null을 반환합니다.</returns>
        public UpgradeDefinition FindUpgrade(string upgradeId)
        {
            if (upgradeDatabase == null) return null;

            return upgradeDatabase.GetDataByCodeName<UpgradeDefinition>(upgradeId);
        }

        /// <summary>
        /// 코드명으로 스탯의 최종값을 가져옵니다.
        /// </summary>
        /// <param name="statCode">값을 가져올 스탯의 코드명입니다.</param>
        /// <param name="fallback">스탯을 찾지 못했을 때 대신 돌려줄 값입니다.</param>
        /// <returns>스탯의 최종값입니다. 찾지 못하면 <paramref name="fallback"/>을 반환합니다.</returns>
        /// <remarks>
        /// 찾지 못했을 때 0을 돌려주지 않는 이유는 가방 칸 수 같은 값이 0이 되면
        /// 아무것도 주울 수 없는 상태로 조용히 진행되기 때문입니다.
        /// 부르는 쪽이 "없으면 이 값"을 정하게 하는 편이 안전합니다.
        /// </remarks>
        public float GetValue(string statCode, float fallback)
        {
            SWStat stat = Find(statCode);

            if (stat != null) return stat.Value;

            SWLog.LogWarning($"[{nameof(StreamerStatBoard)}] 스탯을 찾지 못해 기본값을 씁니다: {statCode}");

            return fallback;
        }

        /// <summary>
        /// 코드명으로 스탯의 최종값을 정수로 가져옵니다.
        /// </summary>
        /// <param name="statCode">값을 가져올 스탯의 코드명입니다.</param>
        /// <param name="fallback">스탯을 찾지 못했을 때 대신 돌려줄 값입니다.</param>
        /// <returns>내림한 스탯의 최종값입니다.</returns>
        /// <remarks>칸 수처럼 정수여야 하는 값은 내립니다. 반 칸짜리 가방은 없습니다.</remarks>
        public int GetIntValue(string statCode, int fallback)
        {
            return Mathf.FloorToInt(GetValue(statCode, fallback));
        }

        /// <summary>
        /// 보유 업그레이드를 스탯 보너스로 다시 얹습니다.
        /// </summary>
        /// <param name="streamer">보유 업그레이드를 읽을 스트리머 진행도입니다.</param>
        /// <remarks>
        /// 얹기 전에 업그레이드 몫을 통째로 걷어냅니다. 걷어내지 않으면 두 번 불렀을 때 두 배가 됩니다.
        /// 세부 키가 업그레이드 식별자이므로 같은 식별자를 다시 넣어도 교체되지만,
        /// 목록에서 빠진 업그레이드는 그대로 남습니다. 스트리머를 바꿀 때 그 일이 벌어집니다.
        /// </remarks>
        public void RebuildUpgradeBonuses(StreamerProgress streamer)
        {
            if (stats == null) return;

            ClearUpgradeBonuses();

            if (streamer == null || upgradeDatabase == null) return;

            int applied = 0;

            for (int index = 0; index < streamer.UpgradeIds.Count; index++)
            {
                UpgradeDefinition definition =
                    upgradeDatabase.GetDataByCodeName<UpgradeDefinition>(streamer.UpgradeIds[index]);

                if (definition == null)
                {
                    SWLog.LogWarning($"[{nameof(StreamerStatBoard)}] 모르는 업그레이드라 건너뜁니다: " +
                        $"{streamer.UpgradeIds[index]}");
                    continue;
                }

                SWStat stat = Find(definition.TargetStatCode);

                if (stat == null)
                {
                    SWLog.LogWarning($"[{nameof(StreamerStatBoard)}] {definition.DisplayName}의 대상 스탯을 " +
                        $"찾지 못했습니다: {definition.TargetStatCode}");
                    continue;
                }

                stat.SetBonusValue(UpgradeBonusKey, definition.DefinitionId, definition.Amount);
                applied++;
            }

            SWLog.Log($"[{nameof(StreamerStatBoard)}] 업그레이드 {applied}개를 스탯에 반영했습니다.");
        }

        /// <summary>
        /// 업그레이드가 얹은 보너스를 모든 스탯에서 걷어냅니다.
        /// </summary>
        private void ClearUpgradeBonuses()
        {
            SWStat[] all = stats.All;

            for (int index = 0; index < all.Length; index++)
                all[index]?.RemoveBonusValue(UpgradeBonusKey);
        }

        /// <summary>
        /// 지금 스탯 값을 전부 로그로 출력합니다.
        /// </summary>
        [SWButton("스탯 출력")]
        [SWCommand("stat.print", "지금 스탯 값을 전부 출력합니다.", "게임")]
        public void PrintStats()
        {
            if (stats == null || stats.IsSetup == false)
            {
                SWLog.LogWarning($"[{nameof(StreamerStatBoard)}] 스탯이 아직 준비되지 않았습니다.");
                return;
            }

            SWStat[] all = stats.All;

            for (int index = 0; index < all.Length; index++)
            {
                if (all[index] == null) continue;

                SWLog.Log($"[{nameof(StreamerStatBoard)}] {all[index].DisplayName} ({all[index].CodeName}) = " +
                    $"{all[index].Value} (기본 {all[index].DefaultValue} + 보너스 {all[index].BonusValue}, " +
                    $"상한 {all[index].MaxValue})");
            }
        }
        #endregion // 함수
    }
}
