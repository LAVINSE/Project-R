using System;

using UnityEngine;

using SW.BehaviourTree;

namespace ProjectR.Backrooms.Monster.Nodes
{
    /// <summary>
    /// 보이는 플레이어를 쫓고, 놓친 뒤에도 잠시 더 쫓는 Action 노드입니다.
    /// </summary>
    /// <remarks>
    /// 시야에서 사라지자마자 멈추면 모퉁이 하나로 추격이 끝나 긴장이 남지 않습니다.
    /// 놓친 뒤에도 마지막으로 본 자리까지는 밀어붙여야 "따돌렸다"가 결과로 느껴집니다.
    /// 이 트리는 실행 중인 노드를 바깥에서 끊지 않으므로, 물러날 때가 되면
    /// 노드 스스로 Success를 반환해 Selector가 다시 판단하게 합니다.
    /// </remarks>
    [Serializable]
    [SWBehaviourNodeCategory("몬스터/행동")]
    public sealed class MonsterChaseNode : MonsterNodeBase
    {
        #region 필드
        /// <summary>플레이어를 놓친 뒤 흐른 시간입니다.</summary>
        [NonSerialized] private float lostSeconds;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 추격을 시작하며 놓친 시간을 초기화합니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        protected override void OnStart(SWBehaviourContext context)
        {
            base.OnStart(context);

            lostSeconds = 0f;
        }

        /// <summary>
        /// 플레이어 또는 마지막 목격 위치를 향해 계속 움직입니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        /// <param name="tree">이 노드가 속한 트리입니다.</param>
        /// <returns>쫓는 동안 Running, 물러날 때 Success, 몸이 없으면 Failure입니다.</returns>
        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            if (Agent == null || Agent.IsReady == false) return SWBehaviourStatus.Failure;

            bool canSeePlayer = context.Blackboard.GetValue(MonsterBlackboardKeys.CanSeePlayer, false);
            Vector3 target;

            if (canSeePlayer)
            {
                lostSeconds = 0f;
                target = context.Blackboard.GetValue(MonsterBlackboardKeys.PlayerPosition, Vector3.zero);
            }
            else
            {
                lostSeconds += context.DeltaTime;

                float graceSeconds = context.Blackboard.GetValue(
                    MonsterBlackboardKeys.ChaseGraceSeconds, 2.5f);

                if (lostSeconds >= graceSeconds) return SWBehaviourStatus.Success;

                target = context.Blackboard.GetValue(MonsterBlackboardKeys.LastSeenPosition, Vector3.zero);
            }

            float speed = context.Blackboard.GetValue(MonsterBlackboardKeys.ChaseSpeed, 3.9f);

            Agent.MoveTo(target, speed);

            return SWBehaviourStatus.Running;
        }
        #endregion // 함수
    }
}
