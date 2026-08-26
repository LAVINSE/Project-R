using SW.Util;

using ProjectR.Activity;
using ProjectR.Core;
using ProjectR.Data;

namespace ProjectR.Backrooms
{
    /// <summary>
    /// 백룸 탐험을 하나의 활동으로 표현하는 구현체입니다.
    /// </summary>
    /// <remarks>
    /// 프로토타입 단계에서는 씬 진입과 종료 흐름만 담당하고
    /// 수집물과 후원금 정산은 Phase 4에서 채웁니다.
    /// </remarks>
    public class BackroomsActivity : IActivity
    {
        #region 필드
        /// <summary>이번 탐험에서 모인 결과입니다. 탐험 중에 갱신됩니다.</summary>
        private ActivityResult result;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>백룸 탐험이 소비하는 시간대 수입니다.</summary>
        public int SlotCost => 1;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 백룸에 진입할 수 있는지 판정합니다.
        /// </summary>
        /// <param name="state">판정에 사용할 게임 상태입니다.</param>
        /// <returns>진입할 수 있으면 true를 반환합니다.</returns>
        /// <remarks>층 해금 조건과 장비 조건은 이후 단계에서 추가합니다.</remarks>
        public bool CanEnter(GameState state)
        {
            return state != null;
        }

        /// <summary>
        /// 백룸 씬으로 이동해 탐험을 시작합니다.
        /// </summary>
        /// <param name="state">활동이 참조할 게임 상태입니다.</param>
        public void Begin(GameState state)
        {
            result = ActivityResult.Empty();

            SWLog.Log($"[{nameof(BackroomsActivity)}] 백룸 탐험을 시작합니다.");
            SceneFlow.ChangeScene(SceneNames.Backrooms);
        }

        /// <summary>
        /// 탐험을 끝내고 결과를 돌려줍니다.
        /// </summary>
        /// <returns>게임 상태에 반영할 활동 결과입니다.</returns>
        public ActivityResult End()
        {
            SWLog.Log($"[{nameof(BackroomsActivity)}] 백룸 탐험을 종료합니다.");

            return result ?? ActivityResult.Empty();
        }
        #endregion // 함수
    }
}
