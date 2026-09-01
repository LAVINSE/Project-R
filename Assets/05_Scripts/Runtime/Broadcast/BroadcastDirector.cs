using System;

using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Debugging;
using SW.Util;

using ProjectR.Activity;

namespace ProjectR.Broadcast
{
    /// <summary>
    /// 활동이 도는 동안 방송을 진행하며 시청자와 후원, 채팅을 굴리는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// <b>이 어셈블리는 백룸을 참조하지 않습니다.</b> 백룸에서 벌어진 일은 전부 이벤트로만 들어옵니다.
    /// 체크리스트 1.7절이 유일한 상호 참조 금지로 못 박은 규칙이고, asmdef가 이것을 컴파일로 지킵니다.
    /// <para>
    /// 전역 싱글톤으로 두지 않았습니다. 계획한 전역 객체 넷(GameManager · SWPopupManager ·
    /// PopupHotkeyController · EventSystem) 밖으로 나가지 않으려는 것이고,
    /// 방송은 활동이 도는 씬에서만 필요하므로 그 씬에 하나 놓으면 충분하기 때문입니다.
    /// 같은 씬의 화면이 이 컴포넌트를 인스펙터로 직접 물면 됩니다.
    /// </para>
    /// <para>
    /// 굴리는 시간 단위는 <b>실제 시간</b>입니다. 활동 비용의 60분은 하루치 행동력에서 깎이는 값이지
    /// 탐험이 도는 동안 흐르는 시계가 아닙니다. 백룸 안에는 제한 시간이 없습니다(진행기록 10.9절).
    /// 그래서 <see cref="BroadcastSettings"/>의 분당 수치는 실제 1분 기준입니다.
    /// </para>
    /// </remarks>
    public class BroadcastDirector : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("조정값")]
        [SerializeField, Tooltip("상황마다의 시청자·후원 증감 규칙입니다.")]
        private BroadcastSettings settings = BroadcastSettings.Default;

        [SerializeField, Min(0.05f), Tooltip("시청자와 후원을 다시 계산하는 간격(초)입니다.")]
        private float tickInterval = 0.25f;

        [SWGroup("채팅")]
        [SerializeField, Tooltip("상황 태그마다 쓸 채팅 문장을 담은 템플릿 세트입니다.")]
        private ChatTemplateSet chatTemplates;

        [SerializeField, Min(0f), Tooltip("아무 일도 없을 때 채팅이 한 줄 올라오는 간격(초)입니다. 0이면 올리지 않습니다.")]
        private float idleChatInterval = 6f;

        /// <summary>지금 방송을 굴리고 있는 계산기입니다. 방송 중이 아니면 null입니다.</summary>
        private BroadcastMeter meter;

        /// <summary>채팅 문장을 고르는 난수 생성기입니다.</summary>
        private System.Random random;

        /// <summary>마지막으로 굴린 뒤 흐른 시간(초)입니다.</summary>
        private float tickTimer;

        /// <summary>마지막으로 채팅이 올라간 뒤 흐른 시간(초)입니다.</summary>
        private float idleChatTimer;
        #endregion // 필드

        #region 이벤트
        /// <summary>채팅 한 줄이 올라올 때 발생합니다.</summary>
        /// <remarks>채팅창이 이 알림을 듣고 줄을 그립니다.</remarks>
        public event Action<ChatLine> ChatLineAdded;
        #endregion // 이벤트

        #region 프로퍼티
        /// <summary>방송이 도는 중인지 여부입니다.</summary>
        public bool IsOnAir => meter != null;

        /// <summary>지금 시청자 수입니다. 방송 중이 아니면 0입니다.</summary>
        public int ViewerCount => meter?.ViewerCount ?? 0;

        /// <summary>이번 방송에서 모인 후원금입니다. 방송 중이 아니면 0입니다.</summary>
        public int Donation => meter?.Donation ?? 0;

        /// <summary>지금 내보내고 있는 방송 상황입니다.</summary>
        public EBroadcastState State => meter?.State ?? EBroadcastState.Exploring;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 이벤트를 구독하고 디버그 명령을 등록합니다.
        /// </summary>
        private void OnEnable()
        {
            SWEventBus.Subscribe<ActivityBeganEvent>(HandleActivityBegan);
            SWEventBus.Subscribe<ActivitySettlingEvent>(HandleActivitySettling);
            SWEventBus.Subscribe<BroadcastStateChangedEvent>(HandleStateChanged);
            SWEventBus.Subscribe<BroadcastMomentEvent>(HandleMoment);

            SWDebugConsole.RegisterInstance(this);
        }

        /// <summary>
        /// 구독과 디버그 명령을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            SWEventBus.Unsubscribe<ActivityBeganEvent>(HandleActivityBegan);
            SWEventBus.Unsubscribe<ActivitySettlingEvent>(HandleActivitySettling);
            SWEventBus.Unsubscribe<BroadcastStateChangedEvent>(HandleStateChanged);
            SWEventBus.Unsubscribe<BroadcastMomentEvent>(HandleMoment);

            SWDebugConsole.UnregisterInstance(this);
        }

        /// <summary>
        /// 정해진 간격마다 시청자와 후원을 굴리고 조용할 때 채팅을 올립니다.
        /// </summary>
        private void Update()
        {
            if (IsOnAir == false) return;

            tickTimer += Time.deltaTime;

            if (tickTimer >= tickInterval)
            {
                meter.Tick(tickTimer / 60f);
                tickTimer = 0f;
            }

            TickIdleChat();
        }

        /// <summary>
        /// 활동이 시작되면 방송을 켭니다.
        /// </summary>
        /// <param name="began">시작된 활동을 담은 이벤트입니다.</param>
        /// <remarks>
        /// 시작 시청자 수는 지금 진행 중인 스트리머가 들고 있던 값입니다.
        /// 방송마다 0에서 시작하면 채널을 키운 것이 다음 방송에 아무 영향도 주지 않습니다.
        /// </remarks>
        private void HandleActivityBegan(ActivityBeganEvent began)
        {
            meter = new BroadcastMeter(settings, GameManager.Instance.State.ViewerCount);
            random = new System.Random(GameManager.Instance.State.Day);

            tickTimer = 0f;
            idleChatTimer = 0f;

            SWLog.Log($"[{nameof(BroadcastDirector)}] 방송을 시작합니다. 시청자 {meter.ViewerCount}명");
        }

        /// <summary>
        /// 활동이 정산되기 직전에 방송 몫을 결과에 더하고 방송을 끕니다.
        /// </summary>
        /// <param name="settling">정산 직전의 활동 결과를 담은 이벤트입니다.</param>
        /// <remarks>
        /// 결과를 직접 만들지 않고 이미 만들어진 결과에 더하는 이유는 방향 때문입니다.
        /// 백룸이 방송에게 물어보게 하면 백룸이 방송을 참조해야 합니다.
        /// 활동 쪽이 결과를 들고 와서 "더할 것 있으면 더하라"고 알리면 그 방향이 생기지 않습니다.
        /// </remarks>
        private void HandleActivitySettling(ActivitySettlingEvent settling)
        {
            if (IsOnAir == false) return;
            if (settling.Result == null) return;

            settling.Result.DonationDelta += meter.Donation;
            settling.Result.ViewerDelta += meter.ViewerDelta;

            SWLog.Log($"[{nameof(BroadcastDirector)}] 방송을 마칩니다. " +
                $"후원 {meter.Donation} / 시청자 {meter.ViewerDelta:+#;-#;0}명");

            meter = null;
        }

        /// <summary>
        /// 방송 상황이 바뀌면 계산기에 알립니다.
        /// </summary>
        /// <param name="changed">바뀐 방송 상황을 담은 이벤트입니다.</param>
        private void HandleStateChanged(BroadcastStateChangedEvent changed)
        {
            meter?.SetState(changed.State);
        }

        /// <summary>
        /// 무슨 일이 벌어지면 그 상황에 맞는 채팅을 올립니다.
        /// </summary>
        /// <param name="moment">벌어진 일의 상황 태그를 담은 이벤트입니다.</param>
        private void HandleMoment(BroadcastMomentEvent moment)
        {
            if (IsOnAir == false) return;

            PublishChat(moment.Moment);

            idleChatTimer = 0f;
        }

        /// <summary>
        /// 한동안 아무 일도 없으면 침묵 태그로 채팅을 한 줄 올립니다.
        /// </summary>
        /// <remarks>
        /// 아무 일도 없을 때 채팅이 멈추면 시청자가 사라진 것처럼 보입니다.
        /// 기획서 6.3절이 말한 "흐름이 반응하는 느낌"은 조용할 때도 흘러야 만들어집니다.
        /// </remarks>
        private void TickIdleChat()
        {
            if (idleChatInterval <= 0f) return;

            idleChatTimer += Time.deltaTime;

            if (idleChatTimer < idleChatInterval) return;

            idleChatTimer = 0f;

            PublishChat(EBroadcastMoment.Silence);
        }

        /// <summary>
        /// 상황 태그에 맞는 채팅 한 줄을 만들어 알립니다.
        /// </summary>
        /// <param name="moment">채팅을 고를 상황 태그입니다.</param>
        private void PublishChat(EBroadcastMoment moment)
        {
            ChatLine line = ChatLineFactory.Create(chatTemplates, moment, random);

            if (line.IsValid == false) return;

            ChatLineAdded?.Invoke(line);
        }

        /// <summary>
        /// 지금 방송 상태를 로그로 출력합니다.
        /// </summary>
        [SWButton("방송 상태 출력")]
        [SWCommand("broadcast.print", "지금 방송의 시청자와 후원을 출력합니다.", "방송")]
        public void PrintBroadcast()
        {
            if (IsOnAir == false)
            {
                SWLog.Log($"[{nameof(BroadcastDirector)}] 지금은 방송 중이 아닙니다.");
                return;
            }

            SWLog.Log($"[{nameof(BroadcastDirector)}] 상황 {meter.State} / 시청자 {meter.ViewerCount}명 " +
                $"({meter.ViewerDelta:+#;-#;0}) / 이번 방송 후원 {meter.Donation}");
        }

        /// <summary>
        /// 방송 상황을 강제로 바꿉니다.
        /// </summary>
        /// <param name="state">바꿀 방송 상황입니다.</param>
        /// <remarks>몬스터를 부르지 않고도 추격 상황의 증감을 확인하려고 둔 통로입니다.</remarks>
        [SWCommand("broadcast.state", "방송 상황을 강제로 바꿉니다.", "방송")]
        public void SetBroadcastState(EBroadcastState state)
        {
            if (IsOnAir == false)
            {
                SWLog.LogWarning($"[{nameof(BroadcastDirector)}] 방송 중이 아니라 상황을 바꾸지 않았습니다.");
                return;
            }

            meter.SetState(state);

            SWLog.Log($"[{nameof(BroadcastDirector)}] 방송 상황을 {state}로 바꿨습니다.");
        }
        #endregion // 함수
    }
}
