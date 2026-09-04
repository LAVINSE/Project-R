using System;

using UnityEngine;

using SW.BehaviourTree;

namespace ProjectR.Backrooms.Monster.Nodes
{
    /// <summary>
    /// 배회를 시작했던 자리로 돌아가는 Action 노드입니다.
    /// </summary>
    /// <remarks>
    /// 추격과 수색이 끝나면 몬스터는 맵 구석에 남아 있게 됩니다.
    /// 돌아가는 길이 없으면 한 번 따돌린 뒤로는 다시 마주칠 일이 없어져
    /// 남은 탐험이 통째로 심심해집니다.
    /// 이미 기준 자리에 있으면 할 일이 없으므로 곧바로 실패를 반환해 배회로 넘깁니다.
    /// </remarks>
    [Serializable]
    [SWBehaviourNodeCategory("몬스터/행동")]
    public sealed class MonsterReturnNode : MonsterNodeBase
    {
        #region 필드
        /// <summary>이 거리 안이면 이미 돌아온 것으로 봅니다(미터).</summary>
        [SerializeField, Min(1f), Tooltip("이 거리 안이면 이미 돌아온 것으로 봅니다(미터).")]
        private float arrivedDistance = 6f;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 기준 자리로 돌아갑니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        /// <param name="tree">이 노드가 속한 트리입니다.</param>
        /// <returns>돌아가는 동안 Running, 도착하면 Success, 이미 있으면 Failure입니다.</returns>
        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            if (Agent == null || Agent.IsReady == false) return SWBehaviourStatus.Failure;

            if (context.Blackboard.GetValue(MonsterBlackboardKeys.CanSeePlayer, false))
                return SWBehaviourStatus.Success;

            Vector3 anchorPosition = Agent.AnchorPosition;

            if (Vector3.Distance(Agent.transform.position, anchorPosition) <= arrivedDistance)
                return SWBehaviourStatus.Failure;

            float speed = context.Blackboard.GetValue(MonsterBlackboardKeys.PatrolSpeed, 1.9f);

            Agent.MoveTo(anchorPosition, speed);

            return Agent.HasArrived() ? SWBehaviourStatus.Success : SWBehaviourStatus.Running;
        }
        #endregion // 함수
    }
}
