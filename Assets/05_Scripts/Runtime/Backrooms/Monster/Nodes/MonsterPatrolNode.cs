using System;

using UnityEngine;

using SW.BehaviourTree;

namespace ProjectR.Backrooms.Monster.Nodes
{
    /// <summary>
    /// 목적 없이 통로를 돌아다니는 Action 노드입니다.
    /// </summary>
    /// <remarks>
    /// 기준 자리 주변만 맴돌면 플레이어가 안전 구역을 학습해 버리므로,
    /// 지금 서 있는 자리를 중심으로 다음 지점을 골라 조금씩 흘러 다니게 합니다.
    /// 배회 반경은 몬스터의 성질이 아니라 층의 통로 폭이 정하는 값이라
    /// Blackboard Key가 아니라 노드 설정으로 두었습니다.
    /// </remarks>
    [Serializable]
    [SWBehaviourNodeCategory("몬스터/행동")]
    public sealed class MonsterPatrolNode : MonsterNodeBase
    {
        #region 필드
        [SerializeField, Min(1f), Tooltip("다음 배회 지점을 고를 반경(미터)입니다.")]
        private float wanderRadius = 18f;

        /// <summary>지금 향하고 있는 배회 지점입니다.</summary>
        [NonSerialized] private Vector3 currentPoint;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 배회를 시작하며 첫 지점을 고릅니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        protected override void OnStart(SWBehaviourContext context)
        {
            base.OnStart(context);

            if (Agent != null) Agent.TryFindPointNear(Agent.transform.position, wanderRadius, out currentPoint);
        }

        /// <summary>
        /// 배회 지점을 옮겨 다니다가 단서가 생기면 물러납니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        /// <param name="tree">이 노드가 속한 트리입니다.</param>
        /// <returns>돌아다니는 동안 Running, 단서가 생기면 Success입니다.</returns>
        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            if (Agent == null || Agent.IsReady == false) return SWBehaviourStatus.Failure;

            if (context.Blackboard.GetValue(MonsterBlackboardKeys.CanSeePlayer, false) ||
                context.Blackboard.GetValue(MonsterBlackboardKeys.HasHeard, false))
            {
                return SWBehaviourStatus.Success;
            }

            if (Agent.HasArrived())
                Agent.TryFindPointNear(Agent.transform.position, wanderRadius, out currentPoint);

            float speed = context.Blackboard.GetValue(MonsterBlackboardKeys.PatrolSpeed, 1.9f);

            Agent.MoveTo(currentPoint, speed);

            return SWBehaviourStatus.Running;
        }
        #endregion // 함수
    }
}
