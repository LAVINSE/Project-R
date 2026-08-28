using UnityEngine;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Data;

namespace ProjectR.Backrooms.Collect
{
    /// <summary>
    /// 월드에 떨어져 있는 이상물체 하나입니다.
    /// </summary>
    /// <remarks>
    /// 종류마다 프리팹을 따로 두지 않습니다. 프리팹은 이 하나뿐이고,
    /// 무엇으로 보일지는 정의가 정합니다. 정의에 모델이 있으면 그것을 자식으로 붙이고,
    /// 없으면 격자 형태와 색으로 만든 상자를 대신 씁니다.
    /// 그래서 아트가 없는 종류를 먼저 만들어 배치 규칙만 확인할 수 있고,
    /// 나중에 모델을 채워 넣어도 이쪽은 손댈 것이 없습니다.
    /// 줍기 판정에 쓰는 충돌체는 어느 쪽이든 격자 형태를 따르므로 종류마다 조준 느낌이 같습니다.
    /// </remarks>
    [RequireComponent(typeof(BoxCollider))]
    public class AnomalyPickup : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField, Tooltip("모델이 없을 때 색을 칠해 대신 쓸 상자 렌더러입니다.")]
        private MeshRenderer boxRenderer;

        [SerializeField, Min(0.02f), Tooltip("격자 한 칸을 월드에서 몇 미터로 볼지입니다.")]
        private float metersPerCell = 0.3f;

        [SerializeField, Min(0.02f), Tooltip("상자의 높이(미터)입니다.")]
        private float boxHeight = 0.25f;

        /// <summary>색을 바꿀 때 쓰는 속성 블록입니다.</summary>
        private MaterialPropertyBlock propertyBlock;

        /// <summary>정의에서 붙여 온 모델입니다. 없으면 null입니다.</summary>
        private GameObject modelInstance;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 상자가 담고 있는 이상물체의 정의입니다.</summary>
        public AnomalyDefinition Definition { get; private set; }
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 정의에 맞춰 겉모습과 줍기 범위를 맞춥니다.
        /// </summary>
        /// <param name="definition">담을 이상물체의 정의입니다.</param>
        public void Setup(AnomalyDefinition definition)
        {
            if (definition == null)
            {
                SWLog.LogError($"[{nameof(AnomalyPickup)}] 이상물체 정의가 null이라 상자를 만들 수 없습니다.");
                return;
            }

            Definition = definition;
            name = $"Anomaly_{definition.DefinitionId}";

            Vector3 size = new Vector3(
                definition.Shape.Width * metersPerCell, boxHeight, definition.Shape.Height * metersPerCell);

            ApplyReach(size);

            if (TryApplyModel(definition, size)) return;

            ApplyBox(size, definition.DisplayColor);
        }

        /// <summary>
        /// 줍기 판정에 쓰는 충돌 범위를 맞춥니다.
        /// </summary>
        /// <param name="size">격자 형태에서 구한 크기(미터)입니다.</param>
        private void ApplyReach(Vector3 size)
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();

            boxCollider.size = size;
            boxCollider.center = new Vector3(0f, size.y * 0.5f, 0f);
        }

        /// <summary>
        /// 정의에 모델이 있으면 붙이고 격자 형태에 맞춰 크기를 맞춥니다.
        /// </summary>
        /// <param name="definition">붙일 모델을 가진 정의입니다.</param>
        /// <param name="size">격자 형태에서 구한 크기(미터)입니다.</param>
        /// <returns>모델을 붙였으면 true를 반환합니다.</returns>
        /// <remarks>
        /// 모델마다 만들어진 크기가 제각각이므로 원래 크기를 재서 격자 형태 안에 들어가도록 줄입니다.
        /// 가로세로를 따로 늘이면 모델이 찌그러지므로 한 축으로만 맞춥니다.
        /// </remarks>
        private bool TryApplyModel(AnomalyDefinition definition, Vector3 size)
        {
            if (definition.WorldPrefab == null) return false;

            if (boxRenderer != null) boxRenderer.gameObject.SetActive(false);

            if (modelInstance != null) Destroy(modelInstance);

            modelInstance = Instantiate(definition.WorldPrefab, transform);
            modelInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            modelInstance.transform.localScale = Vector3.one;

            Bounds bounds = GetLocalBounds(modelInstance);

            if (bounds.size.x <= 0f || bounds.size.z <= 0f) return true;

            float scale = Mathf.Min(size.x / bounds.size.x, size.z / bounds.size.z);

            modelInstance.transform.localScale = Vector3.one * scale;
            modelInstance.transform.localPosition = new Vector3(
                -bounds.center.x * scale, -bounds.min.y * scale, -bounds.center.z * scale);

            return true;
        }

        /// <summary>
        /// 모델이 없을 때 쓰는 상자의 크기와 색을 맞춥니다.
        /// </summary>
        /// <param name="size">상자의 가로세로높이(미터)입니다.</param>
        /// <param name="color">칠할 색입니다.</param>
        /// <remarks>
        /// 재질을 복제하면 종류마다 재질이 하나씩 늘어나므로 속성 블록으로 색만 바꿉니다.
        /// 대신 이 상자는 정적 배칭에서 빠지는데, 맵 전체에 몇 개뿐이라 문제되지 않습니다.
        /// </remarks>
        private void ApplyBox(Vector3 size, Color color)
        {
            if (boxRenderer == null) return;

            boxRenderer.gameObject.SetActive(true);
            boxRenderer.transform.localScale = size;
            boxRenderer.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);

            propertyBlock ??= new MaterialPropertyBlock();

            boxRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            boxRenderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>
        /// 모델이 실제로 차지하는 범위를 자기 기준 좌표로 구합니다.
        /// </summary>
        /// <param name="target">범위를 잴 오브젝트입니다.</param>
        /// <returns>자기 기준 좌표의 범위입니다. 렌더러가 없으면 크기 0입니다.</returns>
        private static Bounds GetLocalBounds(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            // 월드 범위를 자기 기준으로 옮깁니다. 붙인 직후라 회전과 배율이 없어 위치만 빼면 됩니다.
            bounds.center -= target.transform.position;

            return bounds;
        }
        #endregion // 함수
    }
}
