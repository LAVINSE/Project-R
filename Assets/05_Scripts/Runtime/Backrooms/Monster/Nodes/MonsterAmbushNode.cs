using System;

using UnityEngine;

using SW.BehaviourTree;

namespace ProjectR.Backrooms.Monster.Nodes
{
    /// <summary>
    /// 탈출 지점 근처로 물러나 가만히 기다리는 Action 노드입니다.
    /// </summary>
    /// <remarks>
    /// 여기서 중요한 것은 기다리는 동안 완전히 멈춘다는 점입니다.
    /// 멀어지던 발소리가 뚝 끊기는 그 정적이 "포기했나"와 "기다리고 있나"를 동시에 만듭니다.
    /// 계속 서성이면 소리가 이어져 그 효과가 사라지므로 도착하면 반드시 세웁니다.
    /// </remarks>
    [Serializable]
    [SWBehaviourNodeCategory("몬스터/행동")]
    public sealed class MonsterAmbushNode : MonsterNodeBase
    {
        #region 필드
        /// <summary>매복 자리에 도착했는지 여부입니다.</summary>
        [NonSerialized] private bool hasArrived;

        /// <summary>매복 자리에서 기다린 시간입니다.</summary>
        [NonSerialized] private float waitedSeconds;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 매복을 시작하며 대기 상태를 초기화합니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        protected override void OnStart(SWBehaviourContext context)
        {
            base.OnStart(context);

            hasArrived = false;
            waitedSeconds = 0f;
        }

        /// <summary>
        /// 매복 자리로 물러났다가 정해진 시간 동안 기다립니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        /// <param name="tree">이 노드가 속한 트리입니다.</param>
        /// <returns>기다리는 동안 Running, 끝났거나 플레이어를 보면 Success입니다.</returns>
        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            if (Agent == null || Agent.IsReady == false) return SWBehaviourStatus.Failure;

            if (context.Blackboard.GetValue(MonsterBlackboardKeys.CanSeePlayer, false))
                return SWBehaviourStatus.Success;

            if (hasArrived == false)
            {
                Vector3 ambushPosition = context.Blackboard.GetValue(
                    MonsterBlackboardKeys.AmbushPosition, Vector3.zero);
                float speed = context.Blackboard.GetValue(MonsterBlackboardKeys.SearchSpeed, 2.4f);

                Agent.MoveTo(ambushPosition, speed);

                if (Agent.HasArrived())
                {
                    hasArrived = true;
                    Agent.Stop();
                }

                return SWBehaviourStatus.Running;
            }

            waitedSeconds += context.DeltaTime;

            float durationSeconds = context.Blackboard.GetValue(
                MonsterBlackboardKeys.AmbushDurationSeconds, 12f);

            return waitedSeconds >= durationSeconds
                ? SWBehaviourStatus.Success
                : SWBehaviourStatus.Running;
        }
        #endregion // 함수
    }
}
