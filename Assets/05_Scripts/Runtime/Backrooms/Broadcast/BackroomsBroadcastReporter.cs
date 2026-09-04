using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Activity.Broadcast;
using ProjectR.Backrooms.Monster;
using ProjectR.Backrooms.Player;
using ProjectR.Enum;

namespace ProjectR.Backrooms.Broadcast
{
    /// <summary>
    /// 백룸에서 벌어진 일을 방송이 알아들을 수 있는 이벤트로 옮겨 알립니다.
    /// </summary>
    /// <remarks>
    /// <b>백룸에서 방송으로 가는 유일한 통로입니다.</b> 그리고 한 방향뿐입니다.
    /// 백룸은 시청자 수도 후원금도 모릅니다. 알아야 할 이유가 생기면 그때가 설계가 틀어지는 순간입니다.
    /// <para>
    /// 옮기는 일을 한 군데 모아 둔 이유는 백룸 곳곳에서 방송 이벤트를 흩뿌리면
    /// "지금 왜 추격 상황이지"를 추적할 자리가 없어지기 때문입니다.
    /// 상황(<see cref="EBroadcastState"/>)은 여기서만 정합니다.
    /// 순간(<see cref="EBroadcastMoment"/>)은 벌어진 자리에서 각자 알립니다. 줍기와 탈출이 그렇습니다.
    /// </para>
    /// </remarks>
    public class BackroomsBroadcastReporter : SWMonoBehaviour
    {
        #region 필드
        /// <summary>앉아 있는지를 볼 플레이어 은신 컴포넌트입니다.</summary>
        [SWGroup("참조")]
        [SerializeField, Tooltip("앉아 있는지를 볼 플레이어 은신 컴포넌트입니다.")]
        private PlayerStealth playerStealth;

        /// <summary>마지막으로 알린 방송 상황입니다.</summary>
        private EBroadcastState reportedState = EBroadcastState.Exploring;

        /// <summary>몬스터가 지금 쫓고 있는지 여부입니다.</summary>
        private bool isChased;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 몬스터 모드 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            SWEventBus.Subscribe<MonsterModeChangedEvent>(HandleMonsterModeChanged);
        }

        /// <summary>
        /// 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            SWEventBus.Unsubscribe<MonsterModeChangedEvent>(HandleMonsterModeChanged);
        }

        /// <summary>
        /// 앉기 상태가 바뀌었는지 보고 방송 상황을 다시 정합니다.
        /// </summary>
        /// <remarks>
        /// 앉기에는 알림이 없어 매 프레임 봅니다. bool 하나를 읽는 것이라 부담이 없고,
        /// 알림을 만들자고 은신 컴포넌트를 고치면 그쪽이 방송을 위해 바뀌는 셈이 됩니다.
        /// </remarks>
        private void Update()
        {
            ReportState();
        }

        /// <summary>
        /// 몬스터 모드가 바뀌면 추격 여부를 갱신하고 알립니다.
        /// </summary>
        /// <param name="changed">바뀐 몬스터 모드를 담은 이벤트입니다.</param>
        /// <remarks>추격이 시작되는 순간에만 채팅용 태그를 함께 알립니다. 이어지는 동안에는 상황이 대신합니다.</remarks>
        private void HandleMonsterModeChanged(MonsterModeChangedEvent changed)
        {
            bool wasChased = isChased;

            isChased = changed.CurrentMode == EMonsterMode.Chase;

            if (isChased && wasChased == false)
                SWEventBus.Publish(new BroadcastMomentEvent(EBroadcastMoment.Chase));

            ReportState();
        }

        /// <summary>
        /// 지금 상황을 정해 바뀌었을 때만 알립니다.
        /// </summary>
        /// <remarks>
        /// 매 프레임 알리면 듣는 쪽이 같은 값을 계속 받습니다.
        /// 바뀔 때만 알리는 것이 이벤트를 쓰는 이유입니다.
        /// </remarks>
        private void ReportState()
        {
            EBroadcastState current = ResolveState();

            if (current == reportedState) return;

            reportedState = current;

            SWEventBus.Publish(new BroadcastStateChangedEvent(current));
        }

        /// <summary>
        /// 지금 백룸의 사정을 방송 상황 하나로 옮깁니다.
        /// </summary>
        /// <returns>지금 내보내고 있다고 볼 방송 상황입니다.</returns>
        /// <remarks>
        /// 쫓기는 것이 앉아 있는 것보다 앞섭니다. 쫓기면서 앉아 있는 것은 은신이 아니라 궁지입니다.
        /// 기획서 6.1절의 "안전한 곳에 은신"은 몬스터가 물러난 뒤에야 성립합니다.
        /// </remarks>
        private EBroadcastState ResolveState()
        {
            if (isChased) return EBroadcastState.Chased;

            if (playerStealth != null && playerStealth.IsCrouching) return EBroadcastState.Hidden;

            return EBroadcastState.Exploring;
        }
        #endregion // 함수
    }
}
