using System;

using SW.BehaviourTree;

namespace ProjectR.Backrooms.Monster.Nodes
{
    /// <summary>
    /// 지금 플레이어가 보이는지 확인하는 Action 노드입니다.
    /// </summary>
    /// <remarks>
    /// 판정 자체는 <see cref="MonsterSenses"/>가 정해진 간격으로 미리 해 둡니다.
    /// 노드는 그 결과만 읽으므로 트리가 몇 번 돌든 광선은 더 쏘이지 않습니다.
    /// </remarks>
    [Serializable]
    [SWBehaviourNodeCategory("몬스터/감지")]
    public sealed class MonsterSeesPlayerNode : SWBehaviourActionNode
    {
        #region 함수
        /// <summary>
        /// 플레이어가 보이면 성공을 반환합니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        /// <param name="tree">이 노드가 속한 트리입니다.</param>
        /// <returns>보이면 Success, 아니면 Failure입니다.</returns>
        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            return context.Blackboard.GetValue(MonsterBlackboardKeys.CanSeePlayer, false)
                ? SWBehaviourStatus.Success
                : SWBehaviourStatus.Failure;
        }
        #endregion // 함수
    }
}
