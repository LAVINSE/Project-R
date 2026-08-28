using System;

using UnityEngine;

using SW.BehaviourTree;

namespace ProjectR.Backrooms.Monster.Nodes
{
    /// <summary>
    /// 마지막 단서 주변을 정해진 시간 동안 뒤지는 Action 노드입니다.
    /// </summary>
    /// <remarks>
    /// 무작위로 헤매면 플레이어에게는 그냥 돌아다니는 것으로 보입니다.
    /// 마지막으로 본 자리에 가깝게, 그리고 전에 숨었던 자리를 우선해서 고르면
    /// 같은 무작위인데도 "내가 어디 있었는지 안다"로 읽힙니다. 고르는 일은
    /// <see cref="MonsterMemory"/>가 맡고 이 노드는 고른 자리로 가기만 합니다.
    /// </remarks>
    [Serializable]
    [SWBehaviourNodeCategory("몬스터/행동")]
    public sealed class MonsterSearchNode : MonsterNodeBase
    {
        #region 필드
        [SerializeField, Min(1f), Tooltip("마지막 단서에서 퍼져 나갈 최대 거리(미터)입니다.")]
        private float searchRadius = 10f;

        /// <summary>수색을 시작한 뒤 흐른 시간입니다.</summary>
        [NonSerialized] private float elapsedSeconds;

        /// <summary>수색의 중심이 되는 자리입니다.</summary>
        [NonSerialized] private Vector3 searchCenter;

        /// <summary>지금 향하고 있는 수색 지점입니다.</summary>
        [NonSerialized] private Vector3 currentPoint;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 단서를 소비해 수색 중심을 정하고 첫 지점을 고릅니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        protected override void OnStart(SWBehaviourContext context)
        {
            base.OnStart(context);

            elapsedSeconds = 0f;

            // 들은 소리가 더 최근 단서이므로 먼저 씁니다. 확인하러 가는 순간 단서는 소비됩니다.
            if (context.Blackboard.GetValue(MonsterBlackboardKeys.HasHeard, false))
            {
                searchCenter = context.Blackboard.GetValue(
                    MonsterBlackboardKeys.HeardPosition, Vector3.zero);
                context.Blackboard.SetValue(MonsterBlackboardKeys.HasHeard, false);
            }
            else
            {
                searchCenter = context.Blackboard.GetValue(
                    MonsterBlackboardKeys.LastSeenPosition, Vector3.zero);
            }

            currentPoint = PickNextPoint();
        }

        /// <summary>
        /// 수색 지점을 옮겨 다니다가 시간이 다 되면 물러납니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        /// <param name="tree">이 노드가 속한 트리입니다.</param>
        /// <returns>뒤지는 동안 Running, 끝났거나 플레이어를 찾으면 Success입니다.</returns>
        protected override SWBehaviourStatus OnUpdate(SWBehaviourContext context, SWBehaviourTreeAsset tree)
        {
            if (Agent == null || Agent.IsReady == false) return SWBehaviourStatus.Failure;

            if (context.Blackboard.GetValue(MonsterBlackboardKeys.CanSeePlayer, false))
                return SWBehaviourStatus.Success;

            elapsedSeconds += context.DeltaTime;

            float durationSeconds = context.Blackboard.GetValue(
                MonsterBlackboardKeys.SearchDurationSeconds, 14f);

            if (elapsedSeconds >= durationSeconds)
            {
                GiveUp(context);
                return SWBehaviourStatus.Success;
            }

            if (Agent.HasArrived()) currentPoint = PickNextPoint();

            float speed = context.Blackboard.GetValue(MonsterBlackboardKeys.SearchSpeed, 2.4f);

            Agent.MoveTo(currentPoint, speed);

            return SWBehaviourStatus.Running;
        }

        /// <summary>
        /// 다음에 뒤져 볼 지점을 고릅니다.
        /// </summary>
        /// <returns>NavMesh 위로 끌어온 수색 지점입니다.</returns>
        private Vector3 PickNextPoint()
        {
            Vector3 candidate = Memory != null
                ? Memory.GetSearchPoint(searchCenter, searchRadius)
                : searchCenter;

            Agent.TryFindPointNear(candidate, 0f, out Vector3 point);

            return point;
        }

        /// <summary>
        /// 수색을 포기하고 매복을 한 번 시도할 수 있게 표식을 켭니다.
        /// </summary>
        /// <param name="context">노드에 전달된 실행 문맥입니다.</param>
        private void GiveUp(SWBehaviourContext context)
        {
            context.Blackboard.SetValue(MonsterBlackboardKeys.HasLastSeen, false);
            context.Blackboard.SetValue(MonsterBlackboardKeys.AmbushArmed, true);
        }
        #endregion // 함수
    }
}
