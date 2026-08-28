using System;
using System.Diagnostics;

using UnityEngine;
using UnityEngine.AI;

using Unity.AI.Navigation;

using SW.Attributes;
using SW.Base;
using SW.Util;

using ProjectR.Backrooms.Assembly;
using ProjectR.Backrooms.Generation;

namespace ProjectR.Backrooms.Monster
{
    /// <summary>
    /// 맵 조립이 끝난 뒤 NavMesh를 한 번 굽는 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// 맵이 실행 중에 만들어지므로 미리 구워 둘 수 없습니다.
    /// 굽기는 비싸므로 조립이 완전히 끝난 뒤 딱 한 번만 합니다.
    /// 몬스터를 놓는 쪽은 맵이 아니라 이 컴포넌트의 완료 알림을 기다립니다.
    /// 맵 조립 알림을 함께 구독하면 굽기보다 먼저 놓일 수 있기 때문입니다.
    /// 다시 구울 때 이전 NavMesh 데이터를 직접 지웁니다. 실행 중에 만든 것은
    /// 저절로 사라지지 않아 재생성마다 쌓입니다.
    /// </remarks>
    [RequireComponent(typeof(NavMeshSurface))]
    public class BackroomsNavMeshBaker : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("대상")]
        [SerializeField, Tooltip("생성 결과를 받아 올 맵 조립 컴포넌트입니다.")]
        private BackroomsMapBuilder mapBuilder;

        /// <summary>NavMesh를 굽는 컴포넌트입니다.</summary>
        private NavMeshSurface navMeshSurface;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>NavMesh를 굽는 데 걸린 시간(밀리초)입니다.</summary>
        public double LastBakeMilliseconds { get; private set; }

        /// <summary>NavMesh가 준비되어 있는지 여부입니다.</summary>
        public bool IsBaked => navMeshSurface != null && navMeshSurface.navMeshData != null;
        #endregion // 프로퍼티

        #region 이벤트
        /// <summary>NavMesh 굽기가 끝났을 때 생성 결과와 함께 발생합니다.</summary>
        public event Action<MazeBuildResult> NavMeshBuilt;
        #endregion // 이벤트

        #region 함수
        /// <summary>
        /// 굽기 컴포넌트를 캐싱하고 타일만 모으도록 설정합니다.
        /// </summary>
        private void Awake()
        {
            navMeshSurface = GetComponent<NavMeshSurface>();

            // 조립이 끝난 타일은 정적 배칭으로 합쳐지지만 충돌체는 그대로 남습니다.
            // 합쳐진 메시 대신 충돌체를 모아야 굽기 결과가 실제 벽과 어긋나지 않습니다.
            navMeshSurface.collectObjects = CollectObjects.Children;
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        }

        /// <summary>
        /// 맵 조립 완료 알림을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            if (mapBuilder == null)
            {
                SWLog.LogError($"[{nameof(BackroomsNavMeshBaker)}] 맵 조립 컴포넌트가 비어 있습니다.");
                return;
            }

            mapBuilder.MapBuilt += HandleMapBuilt;
        }

        /// <summary>
        /// 맵 조립 완료 알림을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            if (mapBuilder == null) return;

            mapBuilder.MapBuilt -= HandleMapBuilt;
        }

        /// <summary>
        /// 남아 있는 NavMesh 데이터를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            ClearNavMeshData();
        }

        /// <summary>
        /// 조립이 끝난 맵에 NavMesh를 굽고 완료를 알립니다.
        /// </summary>
        /// <param name="result">방금 만들어진 미로 결과입니다.</param>
        private void HandleMapBuilt(MazeBuildResult result)
        {
            ClearNavMeshData();

            Stopwatch bakeWatch = Stopwatch.StartNew();

            navMeshSurface.BuildNavMesh();

            bakeWatch.Stop();
            LastBakeMilliseconds = bakeWatch.Elapsed.TotalMilliseconds;

            SWLog.Log($"[{nameof(BackroomsNavMeshBaker)}] NavMesh 굽기 완료. " +
                $"{LastBakeMilliseconds:F1}ms");

            NavMeshBuilt?.Invoke(result);
        }

        /// <summary>
        /// 이전에 구워 둔 NavMesh 데이터를 등록 해제하고 지웁니다.
        /// </summary>
        private void ClearNavMeshData()
        {
            if (navMeshSurface == null || navMeshSurface.navMeshData == null) return;

            NavMeshData staleData = navMeshSurface.navMeshData;

            navMeshSurface.RemoveData();
            navMeshSurface.navMeshData = null;

            Destroy(staleData);
        }
        #endregion // 함수
    }
}
