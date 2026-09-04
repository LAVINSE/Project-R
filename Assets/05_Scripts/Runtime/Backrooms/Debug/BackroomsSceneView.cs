using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;

using SW.Attributes;
using SW.Base;
using SW.Debugging;
using SW.Util;

using ProjectR.Backrooms.Assembly;

namespace ProjectR.Backrooms.Debugging
{
    /// <summary>
    /// 만들어진 미로를 한눈에 보려고 화면을 잠깐 바꾸는 확인 도구입니다.
    /// </summary>
    /// <remarks>
    /// 백룸은 천장이 덮여 있고 어두워서, 씬 창에서 위에서 내려다봐도 미로 모양이 보이지 않습니다.
    /// 생성 결과가 이상할 때 눈으로 확인할 방법이 없으면 시드와 통계 숫자만 보고 짐작해야 합니다.
    /// 천장을 감추고 화면을 밝게 하면 미로가 그대로 드러납니다.
    /// 밝게 하는 데 빛을 더하지 않고 후처리 노출을 올립니다.
    /// 벽과 바닥은 라이트맵을 구워 둔 정적 오브젝트라 실시간 빛을 더해도 거의 밝아지지 않고,
    /// 실행 중 실시간 라이트는 손전등 하나뿐이라는 규칙(진행기록 8.2절)도 지켜야 하기 때문입니다.
    /// 노출은 이미 그려진 그림을 밝히는 것이라 조명 구성을 건드리지 않습니다.
    /// **성능을 잴 때는 꺼야 합니다.** 후처리가 하나 더 얹혀 있기 때문입니다.
    /// 씬을 벗어나면 스스로 꺼집니다.
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "ProjectR.Backrooms.Measurement", sourceAssembly: "ProjectR.Backrooms", sourceClassName: "BackroomsSceneView")]
    public class BackroomsSceneView : SWMonoBehaviour
    {
        #region 상수
        /// <summary>타일 안에서 천장이 있는 자리의 이름입니다.</summary>
        private const string CeilingName = "Ceiling";
        #endregion // 상수

        #region 필드
        /// <summary>놓인 타일을 훑을 맵 조립 컴포넌트입니다.</summary>
        [SWGroup("대상")]
        [SerializeField, Tooltip("놓인 타일을 훑을 맵 조립 컴포넌트입니다.")]
        private BackroomsMapBuilder mapBuilder;

        /// <summary>밝게 볼 때 올릴 노출 단계입니다.</summary>
        [SWGroup("밝기")]
        [SerializeField, Range(0f, 8f), Tooltip("밝게 볼 때 올릴 노출 단계입니다.")]
        private float overviewExposure = 2.5f;

        /// <summary>밝게 보기로 켜 둔 후처리입니다. 꺼져 있으면 null입니다.</summary>
        private Volume overviewVolume;

        /// <summary>후처리에 쓰려고 만든 프로파일입니다. 에셋이 아니라 실행 중에만 있습니다.</summary>
        private VolumeProfile overviewProfile;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>천장을 감춰 둔 상태인지 여부입니다.</summary>
        public bool IsCeilingHidden { get; private set; }

        /// <summary>전체를 밝게 보고 있는지 여부입니다.</summary>
        public bool IsOverviewLit { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 디버그 콘솔에 이 컴포넌트의 명령을 등록합니다.
        /// </summary>
        /// <remarks>인스턴스 메서드의 명령은 스스로 등록해야 콘솔이 찾을 수 있습니다.</remarks>
        private void Awake()
        {
            SWDebugConsole.RegisterInstance(this);
        }

        /// <summary>
        /// 등록해 둔 명령을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            SWDebugConsole.UnregisterInstance(this);
        }

        /// <summary>
        /// 천장을 감추거나 되돌립니다.
        /// </summary>
        [SWButton("천장 감추기 / 되돌리기")]
        [SWCommand("view.ceiling", "천장을 감추거나 되돌립니다.", "백룸")]
        public void ToggleCeiling()
        {
            SetCeilingHidden(IsCeilingHidden == false);
        }

        /// <summary>
        /// 전체를 밝게 보거나 되돌립니다.
        /// </summary>
        [SWButton("밝게 보기 / 되돌리기")]
        [SWCommand("view.bright", "환경광을 올려 전체를 밝게 보거나 되돌립니다.", "백룸")]
        public void ToggleOverviewLight()
        {
            SetOverviewLit(IsOverviewLit == false);
        }

        /// <summary>
        /// 천장을 감추고 밝게 보는 것을 한 번에 켜거나 끕니다.
        /// </summary>
        [SWButton("미로 살펴보기 / 되돌리기", 8f)]
        [SWCommand("view.maze", "천장을 감추고 밝게 보는 것을 한 번에 켜거나 끕니다.", "백룸")]
        public void ToggleMazeOverview()
        {
            bool shouldEnable = IsCeilingHidden == false || IsOverviewLit == false;

            SetCeilingHidden(shouldEnable);
            SetOverviewLit(shouldEnable);
        }

        /// <summary>
        /// 천장을 감출지 정합니다.
        /// </summary>
        /// <param name="isHidden">감추려면 true입니다.</param>
        public void SetCeilingHidden(bool isHidden)
        {
            if (mapBuilder == null)
            {
                SWLog.LogWarning($"[{nameof(BackroomsSceneView)}] 맵 조립 컴포넌트가 비어 있습니다.");
                return;
            }

            IsCeilingHidden = isHidden;

            int changed = 0;

            foreach (Transform child in mapBuilder.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != CeilingName) continue;

                child.gameObject.SetActive(isHidden == false);
                changed++;
            }

            SWLog.Log($"[{nameof(BackroomsSceneView)}] 천장 {changed}개를 " +
                $"{(isHidden ? "감췄습니다" : "되돌렸습니다")}.");
        }

        /// <summary>
        /// 전체를 밝게 볼지 정합니다.
        /// </summary>
        /// <param name="isLit">밝게 보려면 true입니다.</param>
        public void SetOverviewLit(bool isLit)
        {
            if (isLit == IsOverviewLit) return;

            if (isLit) CreateOverviewVolume();
            else DestroyOverviewVolume();

            IsOverviewLit = isLit;

            SWLog.Log($"[{nameof(BackroomsSceneView)}] 밝게 보기를 {(isLit ? "켰습니다" : "껐습니다")}.");
        }

        /// <summary>
        /// 노출을 올리는 후처리를 만들어 겁니다.
        /// </summary>
        /// <remarks>
        /// 씬의 볼륨 프로파일을 고치면 에셋이 더러워지므로 실행 중에만 있는 볼륨을 따로 얹습니다.
        /// 우선순위를 높게 두어 기존 볼륨보다 나중에 적용되게 합니다.
        /// </remarks>
        private void CreateOverviewVolume()
        {
            GameObject volumeGo = new("OverviewVolume");

            volumeGo.transform.SetParent(transform, false);

            overviewProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            ColorAdjustments colorAdjustments = overviewProfile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = overviewExposure;

            overviewVolume = volumeGo.AddComponent<Volume>();
            overviewVolume.isGlobal = true;
            overviewVolume.priority = 100f;
            overviewVolume.profile = overviewProfile;
        }

        /// <summary>
        /// 걸어 둔 후처리를 치웁니다.
        /// </summary>
        private void DestroyOverviewVolume()
        {
            if (overviewVolume != null) Destroy(overviewVolume.gameObject);
            if (overviewProfile != null) Destroy(overviewProfile);

            overviewVolume = null;
            overviewProfile = null;
        }

        /// <summary>
        /// 켜 둔 채로 씬을 벗어나도 후처리가 남지 않도록 되돌립니다.
        /// </summary>
        private void OnDisable()
        {
            SetOverviewLit(false);
        }
        #endregion // 함수
    }
}
