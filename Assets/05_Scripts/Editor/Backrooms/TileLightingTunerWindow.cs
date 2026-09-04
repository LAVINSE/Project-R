using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

using SW.Util;

namespace ProjectR.Editor.Backrooms
{
    /// <summary>
    /// 타일의 조명과 재질 값을 한 곳에서 조절하고 바로 다시 구워 보는 창입니다.
    /// </summary>
    /// <remarks>
    /// 타일의 Light는 Baked 전용이라 값을 바꿔도 다시 굽기 전에는 화면이 바뀌지 않습니다.
    /// 게다가 타일마다 형광등을 따로 갖고 있어 손으로 고치면 모든 타일을 맞춰야 합니다.
    /// 이 창은 프리팹에서 값을 읽어 와 보여 주고, 고친 값을 타일 폴더 전체에 한 번에 적용합니다.
    /// 어두운 타일은 형광등 자체가 없으므로 여기서 값을 적용해도 계속 어두운 채로 남습니다.
    /// 값의 원본은 어디까지나 프리팹과 재질이며 이 창은 따로 저장하지 않습니다.
    /// </remarks>
    public class TileLightingTunerWindow : EditorWindow
    {
        #region 상수
        /// <summary>타일 프리팹이 들어 있는 폴더 경로입니다.</summary>
        private const string TilePrefabFolder = "Assets/04_Prefabs/Backrooms/Tiles";

        /// <summary>재질이 들어 있는 폴더 경로입니다.</summary>
        private const string MaterialFolder = "Assets/02_Res/Backrooms/Materials";
        #endregion // 상수

        #region 필드
        /// <summary>타일 광원의 밝기입니다.</summary>
        private float lightIntensity = 18f;

        /// <summary>타일 광원이 닿는 거리입니다.</summary>
        private float lightRange = 6f;

        /// <summary>타일 광원의 색상입니다.</summary>
        private Color lightColor = Color.white;

        /// <summary>형광등 재질의 발광 색상입니다.</summary>
        private Color lampGlowColor = Color.white;

        /// <summary>벽 재질의 기본 색상입니다.</summary>
        private Color wallColor = Color.gray;

        /// <summary>바닥 재질의 기본 색상입니다.</summary>
        private Color floorColor = Color.gray;

        /// <summary>천장 재질의 기본 색상입니다.</summary>
        private Color ceilingColor = Color.gray;

        /// <summary>조절할 타일 프리팹을 불러왔는지 여부입니다.</summary>
        private bool isLoaded;

        /// <summary>조명 굽기 도구를 펼쳐 표시할지 여부입니다.</summary>
        private bool showBakeTools;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 창을 엽니다.
        /// </summary>
        [MenuItem("Project R/타일 조명 조절", priority = 0)]
        public static void Open()
        {
            TileLightingTunerWindow window = GetWindow<TileLightingTunerWindow>("타일 조명 조절");
            window.minSize = new Vector2(360f, 420f);
            window.LoadFromProject();
        }

        /// <summary>
        /// 창이 열릴 때 프리팹에서 현재 값을 읽어 옵니다.
        /// </summary>
        private void OnEnable()
        {
            LoadFromProject();
        }

        /// <summary>
        /// 창 내용을 그립니다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "타일의 Light는 Baked 전용입니다. 값을 바꾼 뒤 반드시 다시 구워야 화면이 바뀝니다.\n" +
                "형광등 밝기만 바꾸는 경우에는 굽기가 필요 없습니다.\n" +
                "어두운 타일은 형광등이 없으므로 아래 값에 영향을 받지 않습니다.",
                MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("면광원 (굽기 필요)", EditorStyles.boldLabel);
            lightIntensity = EditorGUILayout.Slider("광량", lightIntensity, 0f, 200f);
            lightRange = EditorGUILayout.Slider("도달 범위", lightRange, 1f, 20f);
            lightColor = EditorGUILayout.ColorField("빛 색", lightColor);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("재질 반사율 (굽기 필요)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("어둡게 하려면 광량보다 이쪽을 먼저 내립니다.", EditorStyles.miniLabel);
            wallColor = EditorGUILayout.ColorField("벽", wallColor);
            floorColor = EditorGUILayout.ColorField("바닥", floorColor);
            ceilingColor = EditorGUILayout.ColorField("천장", ceilingColor);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("형광등 밝기 (굽기 불필요)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1을 넘겨야 블룸이 걸려 빛나 보입니다.", EditorStyles.miniLabel);
            lampGlowColor = EditorGUILayout.ColorField(
                new GUIContent("형광등"), lampGlowColor, true, false, true);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(isLoaded == false))
            {
                if (GUILayout.Button("프리팹에서 현재 값 다시 읽기")) LoadFromProject();

                if (GUILayout.Button("적용만 하기 (굽기 없음)")) ApplyToProject();

                GUILayout.Space(6f);

                if (GUILayout.Button("적용하고 굽기까지 (약 4초)", GUILayout.Height(32f)))
                {
                    ApplyToProject();
                    TileLightmapBaker.BakeAndStore();
                }
            }

            EditorGUILayout.Space();
            DrawBakeTools();
        }

        /// <summary>
        /// 굽기 씬을 직접 다루는 버튼을 그립니다.
        /// </summary>
        /// <remarks>
        /// 값을 크게 잡는 단계에서는 굽기 씬을 열어 두고 Auto Generate로 보면서 고치는 편이 빠릅니다.
        /// </remarks>
        private void DrawBakeTools()
        {
            showBakeTools = EditorGUILayout.Foldout(showBakeTools, "굽기 씬 도구", true);
            if (showBakeTools == false) return;

            EditorGUILayout.HelpBox(
                "굽기 씬을 열고 Lighting 창의 Auto Generate를 켜면 값을 바꿀 때마다 자동으로 다시 구워져 " +
                "씬 뷰에서 바로 보입니다. 마음에 드는 값이 나오면 아래 저장 버튼으로 프리팹에 옮기세요.",
                MessageType.None);

            if (GUILayout.Button("굽기 씬 열기 (없으면 새로 만듦)")) TileLightmapBaker.OpenOrCreateBakeScene();

            if (GUILayout.Button("굽기 씬 다시 만들기 (타일 구성이 바뀐 경우)"))
            {
                if (EditorUtility.DisplayDialog("굽기 씬 다시 만들기",
                    "현재 굽기 씬을 버리고 타일 프리팹으로 새로 만듭니다. 구워 둔 라이트맵도 다시 구워야 합니다.",
                    "다시 만들기", "취소"))
                {
                    TileLightmapBaker.CreateBakeScene();
                }
            }

            if (GUILayout.Button("구운 결과만 프리팹에 저장")) TileLightmapBaker.StoreBakedDataIntoPrefabs();
        }

        /// <summary>
        /// 프리팹과 재질에서 현재 값을 읽어 창에 채웁니다.
        /// </summary>
        private void LoadFromProject()
        {
            List<GameObject> prefabs = LoadTilePrefabs();
            isLoaded = prefabs.Count > 0;

            if (isLoaded == false) return;

            // 어두운 타일에는 형광등이 아예 없으므로, 형광등을 가진 첫 타일에서 값을 읽습니다.
            Light light = null;

            for (int index = 0; index < prefabs.Count && light == null; index += 1)
                light = prefabs[index].GetComponentInChildren<Light>(true);

            if (light != null)
            {
                lightIntensity = light.intensity;
                lightRange = light.range;
                lightColor = light.color;
            }

            lampGlowColor = ReadColor("M_Backrooms_Lamp", lampGlowColor);
            wallColor = ReadColor("M_Backrooms_Wall", wallColor);
            floorColor = ReadColor("M_Backrooms_Floor", floorColor);
            ceilingColor = ReadColor("M_Backrooms_Ceiling", ceilingColor);
        }

        /// <summary>
        /// 창의 값을 형광등이 있는 타일과 재질에 적용합니다.
        /// </summary>
        /// <remarks>형광등이 없는 어두운 타일은 건너뜁니다.</remarks>
        private void ApplyToProject()
        {
            WriteColor("M_Backrooms_Lamp", lampGlowColor);
            WriteColor("M_Backrooms_Wall", wallColor);
            WriteColor("M_Backrooms_Floor", floorColor);
            WriteColor("M_Backrooms_Ceiling", ceilingColor);

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { TilePrefabFolder });
            int changedCount = 0;

            for (int index = 0; index < guids.Length; index += 1)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    Light light = contents.GetComponentInChildren<Light>(true);
                    if (light == null) continue;

                    light.intensity = lightIntensity;
                    light.range = lightRange;
                    light.color = lightColor;

                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    changedCount += 1;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            AssetDatabase.SaveAssets();
            SWLog.Log($"[{nameof(TileLightingTunerWindow)}] 타일 {changedCount}종에 조명 값을 적용했습니다.");
        }

        /// <summary>
        /// 재질의 기본 색을 읽습니다.
        /// </summary>
        /// <param name="materialName">확장자를 뺀 재질 이름입니다.</param>
        /// <param name="fallback">재질을 찾지 못했을 때 돌려줄 색입니다.</param>
        /// <returns>재질의 기본 색입니다.</returns>
        private static Color ReadColor(string materialName, Color fallback)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialFolder}/{materialName}.mat");

            if (material == null || material.HasProperty("_BaseColor") == false) return fallback;

            return material.GetColor("_BaseColor");
        }

        /// <summary>
        /// 재질의 기본 색을 씁니다.
        /// </summary>
        /// <param name="materialName">확장자를 뺀 재질 이름입니다.</param>
        /// <param name="color">적용할 색입니다.</param>
        private static void WriteColor(string materialName, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                $"{MaterialFolder}/{materialName}.mat");

            if (material == null || material.HasProperty("_BaseColor") == false) return;

            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
        }

        /// <summary>
        /// 타일 프리팹을 모두 불러옵니다.
        /// </summary>
        /// <returns>타일 프리팹 목록입니다. 없으면 빈 목록을 반환합니다.</returns>
        private static List<GameObject> LoadTilePrefabs()
        {
            List<GameObject> prefabs = new();

            if (AssetDatabase.IsValidFolder(TilePrefabFolder) == false) return prefabs;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { TilePrefabFolder });

            for (int index = 0; index < guids.Length; index += 1)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    AssetDatabase.GUIDToAssetPath(guids[index]));

                if (prefab != null) prefabs.Add(prefab);
            }

            return prefabs;
        }
        #endregion // 함수
    }
}
