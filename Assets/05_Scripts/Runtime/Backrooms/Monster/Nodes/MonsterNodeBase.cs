using System;

using UnityEngine;

using SW.BehaviourTree;

namespace ProjectR.Backrooms.Monster.Nodes
{
    /// <summary>
    /// 몬스터의 몸과 기억이 필요한 Action 노드의 공통 기반입니다.
    /// </summary>
    /// <remarks>
    /// 노드마다 <c>GetComponent</c>를 매 프레임 부르면 Update에서 컴포넌트를 찾는 셈이 됩니다.
    /// 진입할 때 한 번만 찾아 두고 그 뒤에는 캐싱해 둔 참조를 씁니다.
    /// 노드는 Behaviour Tree 에셋과 함께 복제되므로, 실행 중 참조는 직렬화하지 않습니다.
    /// </remarks>
    [Serializable]
    public abstract class MonsterNodeBase : SWBehaviourActionNode
    {
        #region 필드
        /// <summary>참조를 찾아 둔 대상 오브젝트입니다.</summary>
        [NonSerialized] private GameObject boundOwner;

        /// <summary>이동을 맡길 몬스터의 몸입니다.</summary>
        [NonSerialized] private MonsterAgent agent;

        /// <summary>수색 지점을 골라 줄 기억입니다.</summary>
        [NonSerialized] private MonsterMemory memory;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이동을 맡길 몬스터의 몸입니다. 찾지 못했으면 null입니다.</summary>
        protected MonsterAgent Agent => agent;

        /// <summary>수색 지점을 골라 줄 기억입니다. 찾지 못했으면 null입니다.</summary>
        protected MonsterMemory Memory => memory;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 노드에 진입할 때 몬스터 컴포넌트 참조를 준비합니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        protected override void OnStart(SWBehaviourContext context)
        {
            BindOwner(context);
        }

        /// <summary>
        /// 대상 오브젝트가 바뀌었을 때만 컴포넌트를 다시 찾습니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        private void BindOwner(SWBehaviourContext context)
        {
            if (boundOwner == context.Owner && agent != null) return;

            boundOwner = context.Owner;
            agent = context.GetComponent<MonsterAgent>();
            memory = context.GetComponent<MonsterMemory>();
        }
        #endregion // 함수
    }
}
