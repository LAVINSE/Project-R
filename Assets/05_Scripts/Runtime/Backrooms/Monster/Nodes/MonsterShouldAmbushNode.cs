using System;

using UnityEngine;

using SW.BehaviourTree;

namespace ProjectR.Backrooms.Monster.Nodes
{
    /// <summary>
    /// 헛수고로 끝난 수색 뒤에 매복으로 넘어갈지 정하는 Action 노드입니다.
    /// </summary>
    /// <remarks>
    /// 매복은 수색이 실패한 직후에만 한 번 시도합니다. 확률만으로 판정하면
    /// 배회 중에도 계속 매복으로 빠져 몬스터가 통로에서 사라져 버립니다.
    /// 그래서 수색 노드가 켜 둔 표식을 소비하는 방식으로 한 번만 굴립니다.
    /// </remarks>
    [Serializable]
    [SWBehaviourNodeCategory("몬스터/감지")]
    public sealed class MonsterShouldAmbushNode : SWBehaviourActionNode
    {
        #region 함수
        /// <summary>
        /// 매복 표식을 소비하고 확률에 따라 성공을 반환합니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        /// <param name="tree">이 노드가 속한 트리입니다.</param>
        /// <returns>매복하기로 정했으면 Success, 아니면 Failure입니다.</returns>
        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            if (context.Blackboard.GetValue(MonsterBlackboardKeys.AmbushArmed, false) == false)
                return SWBehaviourStatus.Failure;

            context.Blackboard.SetValue(MonsterBlackboardKeys.AmbushArmed, false);

            if (context.Blackboard.GetValue(MonsterBlackboardKeys.HasAmbushPosition, false) == false)
                return SWBehaviourStatus.Failure;

            float chance = context.Blackboard.GetValue(MonsterBlackboardKeys.AmbushChance, 0.45f);

            return UnityEngine.Random.value < chance ? SWBehaviourStatus.Success : SWBehaviourStatus.Failure;
        }
        #endregion // 함수
    }
}
