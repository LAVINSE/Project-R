using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

using SW.Base;
using SW.Util;

namespace ProjectR.Backrooms.Lighting
{
    /// <summary>
    /// 프리팹 하나에 미리 구워 둔 라이트맵 정보를 담아 두었다가 실행 중에 되살리는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 유니티의 라이트맵은 원래 씬 단위로 저장되므로, 실행 중에 조립하는 맵에는 그대로 쓸 수 없습니다.
    /// 그래서 굽기 전용 씬에서 프리팹별로 구운 결과를 이 컴포넌트에 저장해 두고,
    /// 실행 중 프리팹을 배치할 때 라이트맵 목록에 다시 등록해 인덱스를 이어 붙입니다.
    /// 실행 중에 실시간 라이트를 추가하지 않는 것이 이 방식의 목적입니다.
    /// </remarks>
    public class BakedLightmapData : SWMonoBehaviour
    {
        #region 필드
        /// <summary>굽기 시점에 저장한 렌더러별 라이트맵 정보입니다.</summary>
        [FormerlySerializedAs("rendererInfos")]
        [SerializeField, Tooltip("굽기 시점에 저장한 렌더러별 라이트맵 정보입니다.")]
        private RendererLightmapInformation[] rendererLightmapInformation = Array.Empty<RendererLightmapInformation>();

        /// <summary>이 프리팹이 사용하는 라이트맵 색상 텍스처입니다.</summary>
        [SerializeField, Tooltip("이 프리팹이 사용하는 라이트맵 색상 텍스처입니다.")]
        private Texture2D[] lightmapColors = Array.Empty<Texture2D>();

        /// <summary>이 프리팹이 사용하는 라이트맵 방향 텍스처입니다. 비어 있을 수 있습니다.</summary>
        [SerializeField, Tooltip("이 프리팹이 사용하는 라이트맵 방향 텍스처입니다. 비어 있을 수 있습니다.")]
        private Texture2D[] lightmapDirections = Array.Empty<Texture2D>();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>되살릴 라이트맵 정보가 저장되어 있는지 여부입니다.</summary>
        public bool HasBakedData => rendererLightmapInformation.Length > 0 && lightmapColors.Length > 0;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 배치되는 즉시 저장해 둔 라이트맵을 되살립니다.
        /// </summary>
        private void Awake()
        {
            Apply();
        }

        /// <summary>
        /// 저장해 둔 라이트맵을 현재 라이트맵 목록에 등록하고 렌더러에 인덱스를 이어 붙입니다.
        /// </summary>
        public void Apply()
        {
            if (HasBakedData == false) return;

            int[] remappedIndices = RegisterLightmaps();

            for (int index = 0; index < rendererLightmapInformation.Length; index += 1)
            {
                RendererLightmapInformation rendererInformation = rendererLightmapInformation[index];
                if (rendererInformation.Renderer == null) continue;
                if (rendererInformation.LightmapIndex < 0 || rendererInformation.LightmapIndex >= remappedIndices.Length) continue;

                rendererInformation.Renderer.lightmapIndex = remappedIndices[rendererInformation.LightmapIndex];
                rendererInformation.Renderer.lightmapScaleOffset = rendererInformation.LightmapScaleOffset;
            }
        }

        /// <summary>
        /// 굽기 도구가 호출해 저장할 정보를 채웁니다.
        /// </summary>
        /// <param name="rendererInformationList">렌더러별 라이트맵 정보입니다.</param>
        /// <param name="colors">라이트맵 색상 텍스처 목록입니다.</param>
        /// <param name="directions">라이트맵 방향 텍스처 목록입니다. 없으면 빈 배열을 넘깁니다.</param>
        public void StoreBakedData(RendererLightmapInformation[] rendererInformationList, Texture2D[] colors, Texture2D[] directions)
        {
            rendererLightmapInformation = rendererInformationList ?? Array.Empty<RendererLightmapInformation>();
            lightmapColors = colors ?? Array.Empty<Texture2D>();
            lightmapDirections = directions ?? Array.Empty<Texture2D>();
        }

        /// <summary>
        /// 저장해 둔 라이트맵 텍스처를 전역 라이트맵 목록에 등록합니다.
        /// </summary>
        /// <returns>저장 시점의 인덱스를 현재 목록의 인덱스로 바꿔 주는 대응표입니다.</returns>
        /// <remarks>이미 등록된 텍스처는 다시 등록하지 않으므로 목록이 무한히 늘어나지 않습니다.</remarks>
        private int[] RegisterLightmaps()
        {
            // 굽기 씬과 배치 씬의 라이트맵 방식이 다르면 셰이더가 엉뚱한 값으로 해석해 화면이 타 버립니다.
            LightmapSettings.lightmapsMode = HasDirectionTextures()
                ? LightmapsMode.CombinedDirectional
                : LightmapsMode.NonDirectional;

            LightmapData[] currentLightmaps = LightmapSettings.lightmaps;
            List<LightmapData> merged = new(currentLightmaps);
            int[] remappedIndices = new int[lightmapColors.Length];
            bool hasAdded = false;

            for (int index = 0; index < lightmapColors.Length; index += 1)
            {
                int existingIndex = FindLightmapIndex(merged, lightmapColors[index]);

                if (existingIndex < 0)
                {
                    LightmapData lightmapData = new()
                    {
                        lightmapColor = lightmapColors[index],
                        lightmapDir = index < lightmapDirections.Length ? lightmapDirections[index] : null,
                    };

                    merged.Add(lightmapData);
                    existingIndex = merged.Count - 1;
                    hasAdded = true;
                }

                remappedIndices[index] = existingIndex;
            }

            if (hasAdded)
            {
                LightmapSettings.lightmaps = merged.ToArray();
                SWLog.Log($"[{nameof(BakedLightmapData)}] 라이트맵 {merged.Count}장을 등록했습니다: {name}");
            }

            return remappedIndices;
        }

        /// <summary>
        /// 저장해 둔 라이트맵에 방향 텍스처가 들어 있는지 확인합니다.
        /// </summary>
        /// <returns>방향 텍스처가 하나라도 있으면 true를 반환합니다.</returns>
        private bool HasDirectionTextures()
        {
            for (int index = 0; index < lightmapDirections.Length; index += 1)
            {
                if (lightmapDirections[index] != null) return true;
            }

            return false;
        }

        /// <summary>
        /// 색상 텍스처가 이미 목록에 있는지 찾습니다.
        /// </summary>
        /// <param name="lightmaps">확인할 라이트맵 목록입니다.</param>
        /// <param name="lightmapColor">찾을 색상 텍스처입니다.</param>
        /// <returns>찾으면 그 인덱스를, 없으면 -1을 반환합니다.</returns>
        private static int FindLightmapIndex(List<LightmapData> lightmaps, Texture2D lightmapColor)
        {
            for (int index = 0; index < lightmaps.Count; index += 1)
            {
                if (lightmaps[index].lightmapColor == lightmapColor) return index;
            }

            return -1;
        }
        #endregion // 함수
    }

    /// <summary>
    /// 렌더러 하나가 어느 라이트맵의 어느 영역을 쓰는지를 담은 정보입니다.
    /// </summary>
    [Serializable]
    public struct RendererLightmapInformation
    {
        #region 필드
        /// <summary>라이트맵을 적용할 렌더러입니다.</summary>
        public Renderer Renderer;

        /// <summary>굽기 시점에 이 렌더러가 사용하던 라이트맵 인덱스입니다.</summary>
        public int LightmapIndex;

        /// <summary>라이트맵 안에서 이 렌더러가 차지하는 영역의 크기와 위치입니다.</summary>
        public Vector4 LightmapScaleOffset;
        #endregion // 필드
    }
}
