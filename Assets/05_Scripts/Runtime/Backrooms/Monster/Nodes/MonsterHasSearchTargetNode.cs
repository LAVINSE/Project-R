using System;

using SW.BehaviourTree;

namespace ProjectR.Backrooms.Monster.Nodes
{
    /// <summary>
    /// 뒤져 볼 단서가 남아 있는지 확인하는 Action 노드입니다.
    /// </summary>
    /// <remarks>
    /// 들은 소리와 마지막 목격 위치를 함께 봅니다. 둘 중 하나라도 남아 있으면
    /// 몬스터는 아직 플레이어를 포기하지 않은 것으로 봅니다.
    /// </remarks>
    [Serializable]
    [SWBehaviourNodeCategory("몬스터/감지")]
    public sealed class MonsterHasSearchTargetNode : SWBehaviourActionNode
    {
        #region 함수
        /// <summary>
        /// 들은 소리나 마지막 목격 위치가 있으면 성공을 반환합니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        /// <param name="tree">이 노드가 속한 트리입니다.</param>
        /// <returns>단서가 있으면 Success, 없으면 Failure입니다.</returns>
        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            bool hasHeard = context.Blackboard.GetValue(MonsterBlackboardKeys.HasHeard, false);
            bool hasLastSeen = context.Blackboard.GetValue(MonsterBlackboardKeys.HasLastSeen, false);

            return hasHeard || hasLastSeen ? SWBehaviourStatus.Success : SWBehaviourStatus.Failure;
        }
        #endregion // 함수
    }
}
