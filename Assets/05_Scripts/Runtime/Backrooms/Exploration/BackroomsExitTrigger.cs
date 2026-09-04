using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Activity.Broadcast;
using ProjectR.Core;
using ProjectR.Enum;

namespace ProjectR.Backrooms.Exploration
{
    /// <summary>
    /// 플레이어가 닿으면 탐험을 끝내고 관리 화면으로 돌려보내는 탈출 지점입니다.
    /// </summary>
    /// <remarks>
    /// 탈출은 한 번만 처리합니다. 씬이 넘어가는 사이에 두 번 닿아 활동이 두 번 종료되면
    /// 시간대가 두 번 소비되기 때문입니다.
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public class BackroomsExitTrigger : SWMonoBehaviour
    {
        #region 필드
        /// <summary>탈출로 인정할 대상의 태그입니다.</summary>
        [SWGroup("판정")]
        [SerializeField, Tooltip("탈출로 인정할 대상의 태그입니다.")]
        private string playerTag = "Player";

        /// <summary>이미 탈출 처리를 했는지 여부입니다.</summary>
        private bool hasEscaped;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 콜라이더를 트리거로 맞춰 둡니다.
        /// </summary>
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        /// <summary>
        /// 플레이어가 닿으면 탈출을 처리합니다.
        /// </summary>
        /// <param name="other">닿은 콜라이더입니다.</param>
        private void OnTriggerEnter(Collider other)
        {
            if (hasEscaped) return;
            if (other.CompareTag(playerTag) == false) return;

            hasEscaped = true;

            SWLog.Log($"[{nameof(BackroomsExitTrigger)}] 탈출 지점에 도달했습니다.");

            // 정산보다 먼저 알립니다. 정산이 끝나면 방송이 이미 꺼져 있어 채팅이 올라갈 자리가 없습니다.
            SWEventBus.Publish(new BroadcastMomentEvent(EBroadcastMoment.Escape));

            if (GameManager.Instance.EndActivity() == null) return;

            SceneFlow.ChangeScene(SceneNames.Home);
        }
        #endregion // 함수
    }
}
