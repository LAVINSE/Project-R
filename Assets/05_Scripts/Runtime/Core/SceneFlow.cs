using SW.Util;

namespace ProjectR.Core
{
    /// <summary>
    /// 로딩 씬을 거쳐 목적지 씬으로 이동하는 씬 전환 진입점입니다.
    /// </summary>
    /// <remarks>
    /// 실제 비동기 로드는 SWUtils의 <see cref="SWSceneLoader"/>가 담당하며,
    /// 이 클래스는 "로딩 씬을 반드시 경유한다"는 흐름만 감싸서 제공합니다.
    /// SWUtils 원본을 수정하지 않기 위해 상속 대신 감싸는 방식을 씁니다.
    /// </remarks>
    public static class SceneFlow
    {
        #region 프로퍼티
        /// <summary>로딩 씬이 끝난 뒤 이동할 목적지 씬 이름입니다. 대기 중인 요청이 없으면 null입니다.</summary>
        public static string PendingSceneName { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 로딩 씬을 거쳐 목적지 씬으로 이동을 요청합니다.
        /// </summary>
        /// <param name="targetSceneName">최종적으로 이동할 씬의 이름입니다.</param>
        public static void ChangeScene(string targetSceneName)
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                SWLog.LogError("[SceneFlow] 목적지 씬 이름이 비어 있어 전환을 중단합니다.");
                return;
            }

            PendingSceneName = targetSceneName;
            SWLog.Log($"[SceneFlow] 씬 전환을 요청합니다: {targetSceneName}");
            SWSceneLoader.Instance.LoadScene(SceneNames.Loading);
        }

        /// <summary>
        /// 대기 중인 목적지 씬 이름을 꺼내면서 대기 상태를 비웁니다.
        /// </summary>
        /// <returns>대기 중이던 목적지 씬 이름입니다. 없으면 null을 반환합니다.</returns>
        public static string ConsumePendingSceneName()
        {
            string pendingSceneName = PendingSceneName;
            PendingSceneName = null;

            return pendingSceneName;
        }
        #endregion // 함수
    }
}
