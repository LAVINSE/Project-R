using System.Text;

using UnityEngine;

namespace ProjectR.Backrooms.Lighting
{
    /// <summary>
    /// 실행 중인 맵의 라이트맵 상태를 한 번에 훑어 담은 점검 결과입니다.
    /// </summary>
    /// <remarks>
    /// 프리팹에 구워 넣은 라이트맵은 씬에 속한 텍스처가 아니라서, 빌드에서 참조가 끊기거나
    /// 스트립되면 실기에서 맵이 새까맣게 나옵니다. 에디터에서 잘 돌아간다고 빌드에서도
    /// 된다는 보장이 없으므로 실행 중에 참조가 살아 있는지 직접 세어 확인합니다.
    /// </remarks>
    public class BackroomsLightingReport
    {
        #region 프로퍼티
        /// <summary>전역 라이트맵 목록에 등록된 라이트맵 장수입니다.</summary>
        public int LightmapCount { get; private set; }

        /// <summary>등록되었지만 색상 텍스처 참조가 끊긴 라이트맵 장수입니다.</summary>
        public int MissingTextureCount { get; private set; }

        /// <summary>현재 씬에 있는 렌더러 개수입니다.</summary>
        public int RendererCount { get; private set; }

        /// <summary>라이트맵 인덱스를 실제로 받은 렌더러 개수입니다.</summary>
        public int LightmappedRendererCount { get; private set; }

        /// <summary>현재 적용된 라이트맵 방식입니다.</summary>
        public LightmapsMode Mode { get; private set; }

        /// <summary>라이트맵이 정상적으로 살아 있는지 여부입니다.</summary>
        /// <remarks>빌드에서 참조가 끊기면 등록 장수가 0이 되거나 텍스처가 비어 이 값이 false가 됩니다.</remarks>
        public bool IsHealthy => LightmapCount > 0 && MissingTextureCount == 0 && LightmappedRendererCount > 0;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 지금 이 순간의 라이트맵 상태를 훑어 점검 결과를 만듭니다.
        /// </summary>
        /// <returns>등록 장수와 렌더러 적용 현황을 담은 점검 결과입니다.</returns>
        public static BackroomsLightingReport Capture()
        {
            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            BackroomsLightingReport report = new()
            {
                LightmapCount = lightmaps.Length,
                Mode = LightmapSettings.lightmapsMode,
            };

            for (int index = 0; index < lightmaps.Length; index += 1)
            {
                if (lightmaps[index].lightmapColor == null) report.MissingTextureCount += 1;
            }

            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            report.RendererCount = renderers.Length;

            for (int index = 0; index < renderers.Length; index += 1)
            {
                if (renderers[index].lightmapIndex >= 0 && renderers[index].lightmapIndex < lightmaps.Length)
                    report.LightmappedRendererCount += 1;
            }

            return report;
        }

        /// <summary>
        /// 로그 한 줄에 담을 수 있는 요약을 만듭니다.
        /// </summary>
        /// <returns>등록 장수와 렌더러 적용 현황을 담은 요약 문자열입니다.</returns>
        public string ToSummary()
        {
            return $"라이트맵 {LightmapCount}장({Mode}) / 참조 끊김 {MissingTextureCount}장 / " +
                $"렌더러 {LightmappedRendererCount}/{RendererCount}개 적용 / " +
                $"판정 {(IsHealthy ? "정상" : "실패")}";
        }

        /// <summary>
        /// 등록된 라이트맵 텍스처를 한 장씩 나열한 상세 내용을 만듭니다.
        /// </summary>
        /// <returns>텍스처 이름과 크기, 형식을 줄마다 적은 문자열입니다.</returns>
        public string ToTextureDetail()
        {
            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            StringBuilder builder = new();

            for (int index = 0; index < lightmaps.Length; index += 1)
            {
                Texture2D lightmapColor = lightmaps[index].lightmapColor;

                if (builder.Length > 0) builder.AppendLine();

                if (lightmapColor == null)
                {
                    builder.Append($"  [{index}] 참조 끊김");
                    continue;
                }

                builder.Append($"  [{index}] {lightmapColor.name} " +
                    $"{lightmapColor.width}x{lightmapColor.height} {lightmapColor.format} " +
                    $"밉맵 {lightmapColor.mipmapCount}단계");
            }

            return builder.Length > 0 ? builder.ToString() : "  등록된 라이트맵이 없습니다.";
        }
        #endregion // 함수
    }
}
