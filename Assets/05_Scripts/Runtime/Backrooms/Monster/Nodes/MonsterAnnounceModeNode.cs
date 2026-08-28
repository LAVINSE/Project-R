using System;

using UnityEngine;

using SW.BehaviourTree;

namespace ProjectR.Backrooms.Monster.Nodes
{
    /// <summary>
    /// 자식 행동이 실제로 시작될 때 몬스터의 행동 모드를 바꾸는 Decorator 노드입니다.
    /// </summary>
    /// <remarks>
    /// 행동 모드가 바뀌면 <see cref="MonsterAgent"/>가 알리고 <see cref="MonsterVoice"/>가 소리를 냅니다.
    /// 소리 재생을 여기 하나로 몰아 두면 행동 노드를 새로 만들 때마다 소리를 잊지 않게 됩니다.
    /// 모드를 진입 시점이 아니라 자식이 Running을 돌려준 뒤에 바꾸는 이유는,
    /// 곧바로 실패하는 행동(예: 이미 돌아와 있어 복귀할 필요가 없는 경우)까지
    /// 소리를 내면 플레이어가 있지도 않은 행동을 읽게 되기 때문입니다.
    /// </remarks>
    [Serializable]
    [SWBehaviourNodeCategory("몬스터/연출")]
    public sealed class MonsterAnnounceModeNode : SWBehaviourDecoratorNode
    {
        #region 필드
        [SerializeField, Tooltip("자식 행동이 시작될 때 알릴 행동 모드입니다.")]
        private EMonsterMode mode = EMonsterMode.Patrol;

        /// <summary>이번 진입에서 이미 알렸는지 여부입니다.</summary>
        [NonSerialized] private bool hasAnnounced;

        /// <summary>참조를 찾아 둔 대상 오브젝트입니다.</summary>
        [NonSerialized] private GameObject boundOwner;

        /// <summary>모드를 알릴 몬스터의 몸입니다.</summary>
        [NonSerialized] private MonsterAgent agent;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 알림 상태를 초기화하고 몬스터의 몸을 찾아 둡니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        protected override void OnStart(SWBehaviourContext context)
        {
            hasAnnounced = false;

            if (boundOwner == context.Owner && agent != null) return;

            boundOwner = context.Owner;
            agent = context.GetComponent<MonsterAgent>();
        }

        /// <summary>
        /// 자식을 실행하고 실제로 시작되었을 때 한 번만 모드를 바꿉니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        /// <param name="tree">이 노드가 속한 트리입니다.</param>
        /// <returns>자식의 실행 결과를 그대로 반환합니다.</returns>
        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            SWBehaviourStatus status = TickChild(context, tree);

            if (status == SWBehaviourStatus.Running && hasAnnounced == false)
            {
                hasAnnounced = true;

                if (agent != null) agent.SetMode(mode);
            }

            return status;
        }
        #endregion // 함수
    }
}
