using System;

using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

namespace ProjectR.Core
{
    /// <summary>
    /// 로딩 씬에 배치되어 대기 중인 목적지 씬을 이어서 불러오는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 진행률은 <see cref="ProgressChanged"/>로만 알리고 화면 표현은 UI 계층이 담당합니다.
    /// </remarks>
    public class LoadingSceneRunner : SWMonoBehaviour
    {
        #region 필드
        /// <summary>목적지 씬을 불러오기 전에 대기할 최소 시간(초)입니다.</summary>
        [SWGroup("로딩 설정")]
        [SerializeField, Tooltip("목적지 씬을 불러오기 전에 대기할 최소 시간(초)입니다.")]
        private float minimumDelaySeconds = 0.2f;

        /// <summary>Start에서 확정한 목적지 씬 이름입니다.</summary>
        private string targetSceneName;
        #endregion // 필드

        #region 이벤트
        /// <summary>로딩 진행률(0~1)이 바뀔 때마다 발생합니다.</summary>
        public event Action<float> ProgressChanged;
        #endregion // 이벤트

        #region 함수
        /// <summary>
        /// 대기 중인 목적지 씬을 확인하고 최소 대기 시간 뒤에 로드를 시작합니다.
        /// </summary>
        private void Start()
        {
            targetSceneName = SceneFlow.ConsumePendingSceneName();

            if (string.IsNullOrEmpty(targetSceneName))
            {
                SWLog.LogError($"[{nameof(LoadingSceneRunner)}] 대기 중인 목적지 씬이 없어 관리 화면으로 돌아갑니다.");
                targetSceneName = SceneNames.Home;
            }

            Invoke(nameof(LoadTargetScene), minimumDelaySeconds);
        }

        /// <summary>
        /// 대기 시간이 지난 뒤 목적지 씬을 비동기로 불러옵니다.
        /// </summary>
        private void LoadTargetScene()
        {
            SWLog.Log($"[{nameof(LoadingSceneRunner)}] 목적지 씬을 불러옵니다: {targetSceneName}");
            SWSceneLoader.Instance.LoadScene(targetSceneName, onProgress: HandleProgress);
        }

        /// <summary>
        /// SWSceneLoader의 진행률을 외부로 중계합니다.
        /// </summary>
        /// <param name="progress">0에서 1 사이의 진행률입니다.</param>
        private void HandleProgress(float progress)
        {
            ProgressChanged?.Invoke(progress);
        }
        #endregion // 함수
    }
}
