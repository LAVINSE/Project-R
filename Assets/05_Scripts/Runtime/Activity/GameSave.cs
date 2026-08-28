using SW.Data;
using SW.Util;

using ProjectR.Data;

namespace ProjectR.Activity
{
    /// <summary>
    /// 게임 진행 상태를 저장하고 불러오는 창구입니다.
    /// </summary>
    /// <remarks>
    /// 기획서 11.1절의 저장 방침을 그대로 옮긴 것입니다.
    /// 저장 시점은 활동 시작 직후, 활동 정산 완료 시점, 하루 종료 시점 세 곳뿐입니다.
    /// 활동 도중에는 저장하지 않으므로 강제 종료하면 활동 시작 시점으로 되돌아가고,
    /// 시간대는 활동 시작 직후에 이미 저장되어 있으므로 되돌려도 복구되지 않습니다.
    /// 위기 상황에서 강제 종료로 도망쳐도 얻는 것이 없게 만드는 것이 이 규칙의 목적입니다.
    /// 클라우드 백업은 쓰지 않습니다. 저장은 전부 로컬에서 그 자리에서 끝납니다.
    /// 채널 진행도와 스트리머별 진행도의 분리는 1일 시연판 단계에서 도입합니다.
    /// </remarks>
    public static class GameSave
    {
        #region 함수
        /// <summary>
        /// 저장해 둔 진행 상태를 불러옵니다.
        /// </summary>
        /// <param name="state">불러온 진행 상태입니다. 저장 파일이 없으면 null입니다.</param>
        /// <returns>불러왔으면 true를 반환합니다.</returns>
        /// <remarks>
        /// SWSaveDataManager는 등록된 데이터의 타입을 보고 역직렬화하므로 빈 상태를 먼저 등록합니다.
        /// 클라우드를 거치면 완료가 콜백으로 넘어오므로 로컬만 읽는 경로를 씁니다.
        /// </remarks>
        public static bool TryLoad(out GameState state)
        {
            state = null;

            if (SWSaveDataManager.HasSave() == false) return false;

            SWSaveDataManager.SetData(new GameState());

            bool isLoaded = false;
            SWSaveDataManager.LoadAll(success => isLoaded = success, null, false);

            if (isLoaded == false) return false;
            if (SWSaveDataManager.TryGetData(out GameState loaded) == false) return false;

            state = loaded;

            SWLog.Log($"[{nameof(GameSave)}] 저장해 둔 진행 상태를 불러왔습니다. {state.Day}일차");

            return true;
        }

        /// <summary>
        /// 진행 상태를 저장합니다.
        /// </summary>
        /// <param name="state">저장할 진행 상태입니다.</param>
        /// <param name="reason">어느 시점에 저장했는지를 남길 사유입니다.</param>
        public static void Save(GameState state, string reason)
        {
            if (state == null)
            {
                SWLog.LogError($"[{nameof(GameSave)}] 진행 상태가 null이라 저장하지 않았습니다.");
                return;
            }

            SWSaveDataManager.SetData(state);
            SWSaveDataManager.SaveAll(null, null, false, true, false);

            SWLog.Log($"[{nameof(GameSave)}] 진행 상태를 저장했습니다: {reason}");
        }

        /// <summary>
        /// 저장해 둔 진행 상태를 지웁니다.
        /// </summary>
        /// <returns>지웠으면 true를 반환합니다.</returns>
        /// <remarks>테스트를 처음부터 다시 하려고 만든 통로입니다.</remarks>
        public static bool Delete()
        {
            SWSaveDataManager.ClearData();

            return SWSaveDataManager.Delete();
        }
        #endregion // 함수
    }
}
