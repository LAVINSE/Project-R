using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;

using SW.Util;

using ProjectR.Backrooms.Lighting;

namespace ProjectR.Editor.Backrooms
{
    /// <summary>
    /// 타일 프리팹별로 조명을 굽고 그 결과를 프리팹에 저장하는 에디터 도구입니다.
    /// </summary>
    /// <remarks>
    /// 실행 중에 맵을 조립하면 씬 라이트맵을 쓸 수 없으므로,
    /// 전용 굽기 씬에 타일을 하나씩 늘어놓고 구운 뒤 그 결과를 프리팹에 옮겨 담습니다.
    /// 메뉴는 따로 두지 않습니다. <see cref="TileLightingTunerWindow"/>에서 호출합니다.
    /// </remarks>
    public static class TileLightmapBaker
    {
        #region 상수
        /// <summary>타일 프리팹이 들어 있는 폴더 경로입니다.</summary>
        private const string TilePrefabFolder = "Assets/04_Prefabs/Backrooms/Tiles";

        /// <summary>굽기 전용 씬의 경로입니다.</summary>
        private const string BakeScenePath = "Assets/01_Scenes/Bake/TileLightmapBakeScene.unity";

        /// <summary>타일을 늘어놓을 때 서로 떨어뜨릴 거리입니다. 빛이 옆 타일로 새지 않게 넉넉히 둡니다.</summary>
        private const float TileSpacing = 30f;
        #endregion // 상수

        #region 함수
        /// <summary>
        /// 타일 프리팹을 늘어놓은 굽기 전용 씬을 만듭니다.
        /// </summary>
        public static void CreateBakeScene()
        {
            string[] prefabPaths = FindTilePrefabPaths();

            if (prefabPaths.Length == 0)
            {
                SWLog.LogError($"[{nameof(TileLightmapBaker)}] 타일 프리팹을 찾지 못했습니다: {TilePrefabFolder}");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            for (int index = 0; index < prefabPaths.Length; index += 1)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[index]);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.position = new Vector3(index * TileSpacing, 0f, 0f);
            }

            ApplyBakeLightingSettings();
            EnsureFolder("Assets/01_Scenes", "Bake");
            EditorSceneManager.SaveScene(scene, BakeScenePath);

            SWLog.Log($"[{nameof(TileLightmapBaker)}] 굽기 씬을 만들었습니다. 타일 {prefabPaths.Length}종: {BakeScenePath}");
        }

        /// <summary>
        /// 굽기 씬을 엽니다. 아직 없으면 새로 만듭니다.
        /// </summary>
        /// <remarks>
        /// 이미 구워 둔 라이트맵을 잃지 않도록, 씬이 있으면 다시 만들지 않고 열기만 합니다.
        /// </remarks>
        public static void OpenOrCreateBakeScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BakeScenePath) == null)
            {
                CreateBakeScene();
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;

            EditorSceneManager.OpenScene(BakeScenePath);
        }

        /// <summary>
        /// 열려 있는 굽기 씬의 굽기 결과를 각 타일 프리팹에 저장합니다.
        /// </summary>
        public static void StoreBakedDataIntoPrefabs()
        {
            Scene scene = SceneManager.GetActiveScene();

            if (scene.path != BakeScenePath)
            {
                SWLog.LogError($"[{nameof(TileLightmapBaker)}] 굽기 씬이 열려 있지 않습니다. 먼저 {BakeScenePath}를 여세요.");
                return;
            }

            LightmapData[] sceneLightmaps = LightmapSettings.lightmaps;

            if (sceneLightmaps.Length == 0)
            {
                SWLog.LogError($"[{nameof(TileLightmapBaker)}] 구워진 라이트맵이 없습니다. 라이팅 굽기를 먼저 실행하세요.");
                return;
            }

            int storedCount = 0;

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (StoreOne(rootObject, sceneLightmaps)) storedCount += 1;
            }

            AssetDatabase.SaveAssets();
            SWLog.Log($"[{nameof(TileLightmapBaker)}] 타일 {storedCount}종에 라이트맵 정보를 저장했습니다.");
        }

        /// <summary>
        /// 굽기 씬을 열어 조명을 굽고 그 결과를 프리팹에 저장하는 것까지 한 번에 처리합니다.
        /// </summary>
        /// <remarks>
        /// 타일의 조명 값을 바꾼 뒤 백룸 씬에서 확인하려면 매번 이 과정을 거쳐야 합니다.
        /// 굽기가 끝날 때까지 에디터가 멈추며, 타일 5종 기준으로 6초 안팎이 걸립니다.
        /// </remarks>
        public static void BakeAndStore()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BakeScenePath) == null)
            {
                SWLog.LogWarning($"[{nameof(TileLightmapBaker)}] 굽기 씬이 없어 새로 만듭니다.");
                CreateBakeScene();
            }

            Scene scene = SceneManager.GetActiveScene();

            if (scene.path != BakeScenePath)
            {
                // 열려 있던 씬을 말없이 버리지 않도록 저장 여부를 먼저 묻습니다.
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;

                scene = EditorSceneManager.OpenScene(BakeScenePath);
            }

            Lightmapping.Bake();
            EditorSceneManager.SaveScene(scene);

            StoreBakedDataIntoPrefabs();
        }

        /// <summary>
        /// 굽기 씬의 인스턴스 하나에서 라이트맵 정보를 읽어 원본 프리팹에 저장합니다.
        /// </summary>
        /// <param name="instance">굽기 씬에 놓인 프리팹 인스턴스입니다.</param>
        /// <param name="sceneLightmaps">굽기 씬이 사용하는 라이트맵 목록입니다.</param>
        /// <returns>저장했으면 true를 반환합니다.</returns>
        private static bool StoreOne(GameObject instance, LightmapData[] sceneLightmaps)
        {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance);
            if (string.IsNullOrEmpty(prefabPath)) return false;

            MeshRenderer[] instanceRenderers = instance.GetComponentsInChildren<MeshRenderer>(true);
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                MeshRenderer[] prefabRenderers = prefabContents.GetComponentsInChildren<MeshRenderer>(true);

                if (prefabRenderers.Length != instanceRenderers.Length)
                {
                    SWLog.LogError($"[{nameof(TileLightmapBaker)}] 렌더러 개수가 달라 저장을 건너뜁니다: {prefabPath}");
                    return false;
                }

                List<RendererLightmapInfo> infos = new List<RendererLightmapInfo>(prefabRenderers.Length);
                List<Texture2D> colors = new List<Texture2D>();
                List<Texture2D> directions = new List<Texture2D>();

                for (int index = 0; index < instanceRenderers.Length; index += 1)
                {
                    int sceneIndex = instanceRenderers[index].lightmapIndex;
                    if (sceneIndex < 0 || sceneIndex >= sceneLightmaps.Length) continue;

                    Texture2D color = sceneLightmaps[sceneIndex].lightmapColor;
                    int localIndex = colors.IndexOf(color);

                    if (localIndex < 0)
                    {
                        colors.Add(color);
                        directions.Add(sceneLightmaps[sceneIndex].lightmapDir);
                        localIndex = colors.Count - 1;
                    }

                    infos.Add(new RendererLightmapInfo
                    {
                        Renderer = prefabRenderers[index],
                        LightmapIndex = localIndex,
                        LightmapScaleOffset = instanceRenderers[index].lightmapScaleOffset,
                    });
                }

                BakedLightmapData bakedData = prefabContents.GetComponent<BakedLightmapData>();
                if (bakedData == null) bakedData = prefabContents.AddComponent<BakedLightmapData>();

                bakedData.StoreBakedData(infos.ToArray(), colors.ToArray(), directions.ToArray());
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);

                SWLog.Log($"[{nameof(TileLightmapBaker)}] {System.IO.Path.GetFileNameWithoutExtension(prefabPath)}: " +
                    $"렌더러 {infos.Count}개 / 라이트맵 {colors.Count}장");

                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        /// <summary>
        /// 굽기 씬에 쓸 라이팅 설정을 적용합니다.
        /// </summary>
        /// <remarks>
        /// 형광등 외의 빛이 섞이지 않도록 환경광을 거의 0으로 두고 실시간 GI를 끕니다.
        /// </remarks>
        private static void ApplyBakeLightingSettings()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.fog = false;

            LightingSettings settings = new LightingSettings
            {
                name = "TileBakeSettings",
                bakedGI = true,
                realtimeGI = false,
                lightmapResolution = 12f,
                lightmapPadding = 4,
                lightmapMaxSize = 1024,
                directionalityMode = LightmapsMode.NonDirectional,
                ao = false,
                compressLightmaps = true,
                indirectScale = 0.25f,
                maxBounces = 2,
            };

            Lightmapping.lightingSettings = settings;
        }

        /// <summary>
        /// 폴더가 없으면 만듭니다.
        /// </summary>
        /// <param name="parentFolder">부모 폴더 경로입니다.</param>
        /// <param name="folderName">만들 폴더 이름입니다.</param>
        private static void EnsureFolder(string parentFolder, string folderName)
        {
            if (AssetDatabase.IsValidFolder($"{parentFolder}/{folderName}")) return;

            AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        /// <summary>
        /// 타일 프리팹 경로를 이름순으로 모두 찾습니다.
        /// </summary>
        /// <returns>타일 프리팹의 에셋 경로 목록입니다. 없으면 빈 배열을 반환합니다.</returns>
        private static string[] FindTilePrefabPaths()
        {
            if (AssetDatabase.IsValidFolder(TilePrefabFolder) == false) return new string[0];

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { TilePrefabFolder });
            List<string> paths = new List<string>(guids.Length);

            for (int index = 0; index < guids.Length; index += 1)
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[index]));

            paths.Sort();

            return paths.ToArray();
        }
        #endregion // 함수
    }
}
