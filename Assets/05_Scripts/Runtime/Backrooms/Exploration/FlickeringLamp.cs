using UnityEngine;

using SW.Attributes;
using SW.Base;

namespace ProjectR.Backrooms.Exploration
{
    /// <summary>
    /// 형광등이 불안정하게 깜빡이도록 밝기를 흔드는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 맵 조명은 구워 두었으므로 실제 조도는 바뀌지 않고, 등 자체의 발광 색만 바뀝니다.
    /// 그래도 시야에서 유일하게 움직이는 밝기라서 랜드마크로 아주 잘 걸립니다.
    /// 재질을 복제하면 맵을 다시 만들 때마다 재질이 쌓이므로
    /// <see cref="MaterialPropertyBlock"/>으로 이 렌더러만 값을 덮어씁니다.
    /// </remarks>
    [RequireComponent(typeof(Renderer))]
    public class FlickeringLamp : SWMonoBehaviour
    {
        #region 필드
        /// <summary>덮어쓸 발광 색 속성의 이름입니다.</summary>
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SWGroup("색")]
        [SerializeField, ColorUsage(false, true), Tooltip("가장 밝을 때의 색입니다.")]
        private Color brightColor = new Color(3.5f, 3.4f, 3.2f);

        [SerializeField, ColorUsage(false, true), Tooltip("꺼졌을 때의 색입니다.")]
        private Color dimColor = new Color(0.08f, 0.08f, 0.09f);

        [SWGroup("깜빡임")]
        [SerializeField, Min(0.01f), Tooltip("깜빡임이 이어지는 속도입니다.")]
        private float flickerSpeed = 11f;

        [SerializeField, Range(0f, 1f), Tooltip("꺼져 있는 시간의 비율입니다. 높을수록 자주 꺼집니다.")]
        private float darkRatio = 0.35f;

        [SerializeField, Min(0f), Tooltip("등마다 깜빡임이 어긋나도록 흔드는 폭(초)입니다.")]
        private float phaseSpread = 10f;

        /// <summary>색을 덮어쓸 렌더러입니다.</summary>
        private Renderer targetRenderer;

        /// <summary>렌더러에 값을 넘길 속성 블록입니다.</summary>
        private MaterialPropertyBlock propertyBlock;

        /// <summary>이 등만의 깜빡임 위상입니다.</summary>
        private float phase;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 렌더러를 캐싱하고 등마다 다른 위상을 정합니다.
        /// </summary>
        private void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            phase = Random.Range(0f, phaseSpread);
        }

        /// <summary>
        /// 밝기를 흔들어 깜빡이게 만듭니다.
        /// </summary>
        /// <remarks>
        /// 속성 블록을 덮어쓴 렌더러는 정적 배칭에서 떨어져 나와 따로 그려집니다.
        /// 보이지도 않는 등까지 매 프레임 덮어쓰면 그만큼 드로우콜이 새므로 화면에 든 등만 흔듭니다.
        /// </remarks>
        private void Update()
        {
            if (targetRenderer.isVisible == false) return;

            float wave = Mathf.PerlinNoise((Time.time + phase) * flickerSpeed, phase);
            bool isDark = wave < darkRatio;

            propertyBlock.SetColor(BaseColorId, isDark ? dimColor : brightColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
        #endregion // 함수
    }
}
