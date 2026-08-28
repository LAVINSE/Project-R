using UnityEngine;

using SW.Attributes;
using SW.BehaviourTree;
using SW.Base;
using SW.Util;

using ProjectR.Activity;
using ProjectR.Core;

namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// 몬스터가 플레이어를 붙잡았을 때 탐험을 실패로 끝내는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 조우 판정은 충돌체가 아니라 거리로 합니다. 몬스터에는 충돌체가 없어
    /// 캐릭터 컨트롤러를 밀어내지 않으며, 판정 거리는 Blackboard에서 읽으므로
    /// 몬스터 유형마다 Override로 바꿀 수 있습니다.
    /// 탈출과 마찬가지로 한 번만 처리합니다. 두 번 끝나면 시간대가 두 번 소비됩니다.
    /// </remarks>
    [RequireComponent(typeof(SWBehaviourTreeRunner))]
    public class MonsterEncounter : SWMonoBehaviour
    {
        #region 상수
        /// <summary>실패 결과에 남길 표식입니다.</summary>
        private const string FailureFlag = "몬스터 조우";
        #endregion // 상수

        #region 필드
        [SWGroup("대상")]
        [SerializeField, Tooltip("붙잡을 대상으로 삼을 태그입니다.")]
        private string playerTag = "Player";

        [SWGroup("판정")]
        [SerializeField, Min(0.02f), Tooltip("거리를 다시 확인하는 간격(초)입니다.")]
        private float checkInterval = 0.05f;

        /// <summary>판정 거리를 읽어 올 Behaviour Tree 실행기입니다.</summary>
        private SWBehaviourTreeRunner runner;

        /// <summary>붙잡을 대상 플레이어입니다. 찾지 못하면 null입니다.</summary>
        private Transform playerTransform;

        /// <summary>다음 확인까지 남은 시간입니다.</summary>
        private float checkCooldown;

        /// <summary>이미 조우 처리를 했는지 여부입니다.</summary>
        private bool hasCaught;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// Behaviour Tree 실행기를 캐싱합니다.
        /// </summary>
        private void Awake()
        {
            runner = GetComponent<SWBehaviourTreeRunner>();
        }

        /// <summary>
        /// 붙잡을 대상 플레이어를 찾아 둡니다.
        /// </summary>
        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);

            if (player == null)
            {
                SWLog.LogWarning($"[{nameof(MonsterEncounter)}] 태그 {playerTag}인 플레이어를 찾지 못했습니다.");
                return;
            }

            playerTransform = player.transform;
        }

        /// <summary>
        /// 정해진 간격마다 플레이어와의 거리를 확인합니다.
        /// </summary>
        private void Update()
        {
            if (hasCaught || playerTransform == null) return;

            checkCooldown -= Time.deltaTime;

            if (checkCooldown > 0f) return;

            checkCooldown = checkInterval;

            float catchDistance = runner.GetBlackboardValue(MonsterBlackboardKeys.CatchDistance, 1.3f);

            if (Vector3.Distance(transform.position, playerTransform.position) > catchDistance) return;

            Catch();
        }

        /// <summary>
        /// 탐험을 실패로 표시하고 관리 화면으로 돌려보냅니다.
        /// </summary>
        private void Catch()
        {
            hasCaught = true;

            SWLog.Log($"[{nameof(MonsterEncounter)}] 몬스터가 플레이어를 붙잡았습니다.");

            if (GameManager.Instance.CurrentActivity is BackroomsActivity backrooms)
                backrooms.MarkFailure(FailureFlag);

            if (GameManager.Instance.EndActivity() == null) return;

            SceneFlow.ChangeScene(SceneNames.Home);
        }
        #endregion // 함수
    }
}
