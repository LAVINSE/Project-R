using NUnit.Framework;

using ProjectR.Data;

namespace ProjectR.Tests
{
    /// <summary>
    /// 저장 데이터의 채널·스트리머 분리와 활동 결과 반영을 검증하는 테스트입니다.
    /// </summary>
    /// <remarks>
    /// 체크리스트 5.3절의 확장성 잠금은 "어겼다는 것을 나중에 알게 되는" 종류의 규칙입니다.
    /// DLC로 스트리머를 추가할 때가 되어서야 후원금이 스트리머마다 갈라져 있다는 것을 알게 되면
    /// 이미 배포한 저장 파일 구조라 고칠 수 없습니다. 그래서 지금 테스트로 못을 박아 둡니다.
    /// </remarks>
    public class GameStateTests
    {
        #region 상수
        /// <summary>테스트에서 쓰는 첫 번째 스트리머 식별자입니다.</summary>
        private const string StreamerA = "Streamer_A";

        /// <summary>테스트에서 쓰는 두 번째 스트리머 식별자입니다.</summary>
        private const string StreamerB = "Streamer_B";
        #endregion // 상수

        #region 함수
        /// <summary>
        /// 갓 만든 진행 상태의 구조 번호가 0인지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 생성자에서 번호를 넣으면, 그 필드가 없던 구버전 저장 파일이 역직렬화될 때
        /// 최신 버전으로 읽혀 구버전인 것을 알아볼 방법이 사라집니다.
        /// JSON 역직렬화도 이 생성자를 거치므로 여기서 넣으면 안 됩니다.
        /// </remarks>
        [Test]
        public void 생성자만으로는_구조_번호가_붙지_않는다()
        {
            GameState state = new();

            Assert.AreEqual(0, state.SaveVersion);
            Assert.AreNotEqual(GameState.CurrentSaveVersion, state.SaveVersion);
        }

        /// <summary>
        /// 새 진행으로 표시하면 현재 구조 번호가 붙는지 확인합니다.
        /// </summary>
        [Test]
        public void 새_진행으로_표시하면_현재_구조_번호가_붙는다()
        {
            GameState state = new();

            state.MarkAsCurrentVersion();

            Assert.AreEqual(GameState.CurrentSaveVersion, state.SaveVersion);
        }

        /// <summary>
        /// 스트리머를 고르면 진행도가 생기고, 같은 식별자로 다시 골라도 하나만 유지되는지 확인합니다.
        /// </summary>
        [Test]
        public void 같은_스트리머를_다시_골라도_진행도는_하나다()
        {
            GameState state = new();

            StreamerProgress first = state.SelectStreamer(StreamerA);
            StreamerProgress second = state.SelectStreamer(StreamerA);

            Assert.IsNotNull(first);
            Assert.AreSame(first, second);
            Assert.AreEqual(StreamerA, state.ActiveStreamerId);
        }

        /// <summary>
        /// 활동 결과가 채널과 스트리머로 나뉘어 반영되는지 확인합니다.
        /// </summary>
        [Test]
        public void 활동_결과가_채널과_스트리머로_나뉘어_반영된다()
        {
            GameState state = new();
            state.SelectStreamer(StreamerA);

            ActivityResult result = new()
            {
                DonationDelta = 500,
                ViewerDelta = 120,
                Condition = new ConditionDelta { Sleep = -30, Fatigue = 10 },
            };
            result.Items.Add(new ItemInstance("BrokenEncoder"));

            state.Apply(result);

            Assert.AreEqual(500, state.Channel.Donation, "후원금은 채널에 쌓인다");
            Assert.AreEqual(120, state.ActiveStreamer.ViewerCount, "시청자는 스트리머에 쌓인다");
            Assert.AreEqual(70, state.Condition.Sleep, "컨디션은 스트리머에 쌓인다");
            Assert.AreEqual(1, state.Items.Count, "이상물체는 스트리머에 쌓인다");
        }

        /// <summary>
        /// 스트리머를 바꾸면 시청자·컨디션·이상물체는 갈라지고 날짜·후원금은 이어지는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 체크리스트 5.3절 확장성 잠금의 핵심이자, 기획서 11.2절이 분리를 요구한 이유 그 자체입니다.
        /// 이 테스트가 깨지면 DLC 스트리머를 추가할 수 없게 된 것입니다.
        /// </remarks>
        [Test]
        public void 스트리머를_바꾸면_진행도는_갈리고_채널은_이어진다()
        {
            GameState state = new();
            state.SelectStreamer(StreamerA);

            ActivityResult result = new() { DonationDelta = 500, ViewerDelta = 120 };
            result.Items.Add(new ItemInstance("BrokenEncoder"));
            state.Apply(result);
            state.ApplyDayEnd(new DayEndResult { NextBroadcastMinutes = 240 });

            state.SelectStreamer(StreamerB);

            Assert.AreEqual(0, state.ViewerCount, "시청자는 스트리머마다 따로다");
            Assert.AreEqual(0, state.Items.Count, "이상물체는 스트리머마다 따로다");
            Assert.AreEqual(ConditionState.MaximumValue, state.Condition.Sleep, "컨디션은 스트리머마다 따로다");
            Assert.AreEqual(500, state.Donation, "후원금은 채널이 들고 있어 이어진다");
            Assert.AreEqual(2, state.Day, "날짜는 채널이 들고 있어 이어진다");
        }

        /// <summary>
        /// 첫 스트리머로 되돌아가면 두고 간 진행도가 그대로 있는지 확인합니다.
        /// </summary>
        [Test]
        public void 되돌아가면_두고_간_진행도가_그대로다()
        {
            GameState state = new();
            state.SelectStreamer(StreamerA);
            state.Apply(new ActivityResult { ViewerDelta = 120 });

            state.SelectStreamer(StreamerB);
            state.SelectStreamer(StreamerA);

            Assert.AreEqual(120, state.ViewerCount);
        }

        /// <summary>
        /// 방송 시간을 남은 것보다 많이 소비해도 0 아래로 내려가지 않는지 확인합니다.
        /// </summary>
        [Test]
        public void 방송_시간은_0_아래로_내려가지_않는다()
        {
            GameState state = new();
            state.SelectStreamer(StreamerA);
            state.ResetBroadcastTime(60);

            state.ConsumeBroadcastTime(200);

            Assert.AreEqual(0, state.RemainingBroadcastMinutes);
        }

        /// <summary>
        /// 후원금이 모자랄 때 차감해도 0 아래로 내려가지 않는지 확인합니다.
        /// </summary>
        /// <remarks>유지비 차감이 여기에 걸립니다. 빚 개념은 두지 않습니다.</remarks>
        [Test]
        public void 후원금은_0_아래로_내려가지_않는다()
        {
            GameState state = new();
            state.SelectStreamer(StreamerA);
            state.Apply(new ActivityResult { DonationDelta = 100 });

            state.Apply(new ActivityResult { DonationDelta = -999 });

            Assert.AreEqual(0, state.Donation);
        }
        #endregion // 함수
    }
}
