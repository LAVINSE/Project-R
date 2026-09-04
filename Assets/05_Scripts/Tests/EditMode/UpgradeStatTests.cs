using NUnit.Framework;

using UnityEngine;

using SW.Stat;

using ProjectR.Data;

namespace ProjectR.Tests
{
    /// <summary>
    /// 업그레이드 보유 목록과, 그 목록이 기대는 스탯 보너스 규칙을 검증하는 테스트입니다.
    /// </summary>
    /// <remarks>
    /// 뒤쪽 셋은 SWUtils의 <see cref="SWStat"/>을 시험하는 것처럼 보이지만 그렇지 않습니다.
    /// <b>업그레이드 설계가 기대고 있는 동작을 못 박는 테스트입니다.</b>
    /// 보유 목록만 저장하고 보너스는 불러올 때 다시 얹는 구조는
    /// "출처별로 걷어낼 수 있다"는 성질 위에 서 있습니다. 그 성질이 깨지면 설계가 무너지는데,
    /// 그때 드러나는 증상은 "가방이 두 배가 되었다" 같은 엉뚱한 모습입니다.
    /// SWUtils를 올릴 때 이 테스트가 먼저 알려 줍니다.
    /// </remarks>
    public class UpgradeStatTests
    {
        #region 상수
        /// <summary>테스트에서 쓰는 업그레이드 출처 키입니다.</summary>
        private const string UpgradeKey = "Upgrade";

        /// <summary>테스트에서 쓰는 장비 출처 키입니다.</summary>
        private const string GearKey = "Gear";
        #endregion // 상수

        #region 함수
        /// <summary>
        /// 시험용 스탯을 만듭니다.
        /// </summary>
        /// <param name="defaultValue">기본값입니다.</param>
        /// <param name="maximumValue">상한입니다.</param>
        /// <returns>만들어진 스탯입니다.</returns>
        private static SWStat CreateStat(float defaultValue, float maximumValue)
        {
            SWStat stat = ScriptableObject.CreateInstance<SWStat>();

            stat.MinValue = 0f;
            stat.MaxValue = maximumValue;
            stat.DefaultValue = defaultValue;

            return stat;
        }

        /// <summary>
        /// 같은 업그레이드를 두 번 넣어도 하나만 남는지 확인합니다.
        /// </summary>
        [Test]
        public void 같은_업그레이드를_두_번_넣어도_하나만_남는다()
        {
            StreamerProgress streamer = new("Streamer_A");

            Assert.IsTrue(streamer.AddUpgrade("Backpack_01"));
            Assert.IsFalse(streamer.AddUpgrade("Backpack_01"));

            Assert.AreEqual(1, streamer.UpgradeIds.Count);
            Assert.IsTrue(streamer.HasUpgrade("Backpack_01"));
        }

        /// <summary>
        /// 갖지 않은 업그레이드는 갖고 있다고 하지 않는지 확인합니다.
        /// </summary>
        [Test]
        public void 갖지_않은_업그레이드는_없다고_한다()
        {
            StreamerProgress streamer = new("Streamer_A");

            Assert.IsFalse(streamer.HasUpgrade("Backpack_01"));
            Assert.IsFalse(streamer.HasUpgrade(string.Empty));
            Assert.IsFalse(streamer.HasUpgrade(null));
        }

        /// <summary>
        /// 업그레이드가 여러 개 쌓이면 보너스도 함께 쌓이는지 확인합니다.
        /// </summary>
        /// <remarks>가방 확장 네 단계를 다 사면 세로가 4칸 늘어야 합니다.</remarks>
        [Test]
        public void 업그레이드가_쌓이면_보너스도_쌓인다()
        {
            SWStat backpackHeight = CreateStat(4f, 8f);

            backpackHeight.SetBonusValue(UpgradeKey, "Backpack_01", 1f);
            backpackHeight.SetBonusValue(UpgradeKey, "Backpack_02", 1f);
            backpackHeight.SetBonusValue(UpgradeKey, "Backpack_03", 1f);
            backpackHeight.SetBonusValue(UpgradeKey, "Backpack_04", 1f);

            Assert.AreEqual(8f, backpackHeight.Value);

            Object.DestroyImmediate(backpackHeight);
        }

        /// <summary>
        /// 같은 업그레이드를 다시 얹어도 두 배가 되지 않는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 보유 목록을 다시 훑어 보너스를 얹는 구조라 같은 식별자가 두 번 얹힐 수 있습니다.
        /// 세부 키가 같으면 더해지지 않고 교체되어야 합니다.
        /// </remarks>
        [Test]
        public void 같은_업그레이드를_다시_얹어도_두_배가_되지_않는다()
        {
            SWStat backpackHeight = CreateStat(4f, 8f);

            backpackHeight.SetBonusValue(UpgradeKey, "Backpack_01", 1f);
            backpackHeight.SetBonusValue(UpgradeKey, "Backpack_01", 1f);

            Assert.AreEqual(5f, backpackHeight.Value);

            Object.DestroyImmediate(backpackHeight);
        }

        /// <summary>
        /// 업그레이드 몫만 걷어내고 장비 몫은 남는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// <b>업그레이드 설계 전체가 이 성질 위에 서 있습니다.</b>
        /// 출처별로 걷어낼 수 없으면 보유 목록을 다시 훑을 때마다
        /// 장비가 준 보너스까지 함께 날아갑니다.
        /// </remarks>
        [Test]
        public void 업그레이드_몫만_걷어내고_장비_몫은_남는다()
        {
            SWStat backpackHeight = CreateStat(4f, 20f);

            backpackHeight.SetBonusValue(UpgradeKey, "Backpack_01", 1f);
            backpackHeight.SetBonusValue(UpgradeKey, "Backpack_02", 1f);
            backpackHeight.SetBonusValue(GearKey, "BigBag", 3f);

            Assert.AreEqual(9f, backpackHeight.Value);

            backpackHeight.RemoveBonusValue(UpgradeKey);

            Assert.AreEqual(7f, backpackHeight.Value);

            Object.DestroyImmediate(backpackHeight);
        }

        /// <summary>
        /// 보너스가 아무리 쌓여도 상한을 넘지 않는지 확인합니다.
        /// </summary>
        /// <remarks>기획서 8.4절의 "스탯에 상한선을 둔다"가 여기에 걸립니다.</remarks>
        [Test]
        public void 보너스가_쌓여도_상한을_넘지_않는다()
        {
            SWStat backpackHeight = CreateStat(4f, 8f);

            backpackHeight.SetBonusValue(UpgradeKey, "Overflow", 999f);

            Assert.AreEqual(8f, backpackHeight.Value);

            Object.DestroyImmediate(backpackHeight);
        }

        /// <summary>
        /// 런타임 복제본을 만들어도 원본 에셋의 값이 바뀌지 않는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 스탯을 에셋으로 두면서 실행 중에 값을 바꿀 수 있는 이유가 이것입니다.
        /// 복제본이 아니라 원본에 보너스가 얹히면 에디터에서 한 판 돌릴 때마다
        /// 에셋 값이 바뀌어 다음 판이 달라집니다.
        /// </remarks>
        [Test]
        public void 런타임_복제본은_원본_에셋을_건드리지_않는다()
        {
            SWStat origin = CreateStat(4f, 8f);
            SWStat clone = origin.CreateRuntimeClone();

            clone.SetBonusValue(UpgradeKey, "Backpack_01", 3f);

            Assert.AreEqual(7f, clone.Value);
            Assert.AreEqual(4f, origin.Value, "원본은 그대로여야 한다");
            Assert.AreSame(origin, clone.OriginStat);

            Object.DestroyImmediate(clone);
            Object.DestroyImmediate(origin);
        }
        #endregion // 함수
    }
}
