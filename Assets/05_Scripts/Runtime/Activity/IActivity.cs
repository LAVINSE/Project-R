using ProjectR.Data;

namespace ProjectR.Activity
{
    /// <summary>
    /// 방송 시간을 소비하는 모든 활동이 따르는 공통 규격입니다.
    /// </summary>
    /// <remarks>
    /// 백룸 탐험도 여러 활동 중 하나로 취급합니다.
    /// 백룸을 게임 전체로 만들면 이후 미니게임과 알바를 붙일 때 반드시 다시 짜게 됩니다.
    /// </remarks>
    public interface IActivity
    {
        #region 프로퍼티
        /// <summary>이 활동에 들어갈 때 소비하는 방송 시간(분)입니다.</summary>
        /// <remarks>방송 시간은 활동에 들어가는 순간 한 번에 빠집니다. 탐험 도중에 줄어들지 않습니다.</remarks>
        int BroadcastCost { get; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 현재 게임 상태에서 이 활동에 진입할 수 있는지 판정합니다.
        /// </summary>
        /// <param name="state">판정에 사용할 게임 상태입니다.</param>
        /// <returns>진입할 수 있으면 true를 반환합니다.</returns>
        bool CanEnter(GameState state);

        /// <summary>
        /// 활동을 시작합니다.
        /// </summary>
        /// <param name="state">활동이 참조할 게임 상태입니다.</param>
        void Begin(GameState state);

        /// <summary>
        /// 활동을 끝내고 결과를 만들어 돌려줍니다.
        /// </summary>
        /// <returns>게임 상태에 반영할 활동 결과입니다.</returns>
        ActivityResult End();
        #endregion // 함수
    }
}
