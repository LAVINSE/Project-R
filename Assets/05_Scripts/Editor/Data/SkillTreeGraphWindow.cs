using System.IO;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

using SW.Base;

using ProjectR.Data;

namespace ProjectR.Editor.Data
{
    /// <summary>
    /// 스킬트리의 배치와 선행 관계를 그래프로 잡는 창입니다.
    /// </summary>
    /// <remarks>
    /// 스킬 하나를 트리에 얹으려면 정의 에셋을 만들고, 값을 넣고, 데이터베이스에 등록하고,
    /// 화면에 놓을 자리와 선행을 정해야 합니다. 손으로 하면 그중 하나만 빠져도 게임을 켜 봐야
    /// 드러납니다. <see cref="AnomalyMakerWindow"/>가 이상물체에 대해 하는 일을 스킬에 대해 합니다.
    /// <para>
    /// 배치를 눈으로 잡아야 하므로 목록이 아니라 그래프입니다. 노드를 끌어다 놓은 자리가 곧
    /// 게임 화면에서 노드가 서는 자리이고, 노드를 이은 선이 곧 선행 조건입니다.
    /// 저장 버튼은 없습니다. 옮기거나 이은 그 자리에서 에셋에 적힙니다.
    /// </para>
    /// </remarks>
    public class SkillTreeGraphWindow : EditorWindow
    {
        #region 상수
        /// <summary>창을 여는 메뉴 경로입니다.</summary>
        private const string MenuPath = "프로젝트R/스킬트리 그래프";

        /// <summary>처음 열 때 찾아 볼 데이터베이스 에셋의 경로입니다.</summary>
        private const string DefaultDatabasePath = "Assets/02_Res/Upgrades/UpgradeDatabase.asset";

        /// <summary>새로 만드는 스킬 에셋에 붙일 이름입니다.</summary>
        private const string NewAssetName = "Upgrade_New";

        /// <summary>오른쪽 인스펙터 칸의 너비입니다.</summary>
        private const float InspectorWidth = 340f;
        #endregion // 상수

        #region 필드
        /// <summary>지금 보고 있는 스킬 데이터베이스입니다.</summary>
        private SWIODatabase database;

        /// <summary>노드를 늘어놓는 그래프 화면입니다.</summary>
        private SkillTreeGraphView graphView;

        /// <summary>오른쪽에 띄울 인스펙터가 들어가는 칸입니다.</summary>
        private VisualElement inspectorPanel;

        /// <summary>그래프에서 고른 스킬 정의입니다. 고른 것이 없으면 null입니다.</summary>
        private UpgradeDefinition selectedDefinition;

        /// <summary>고른 정의의 인스펙터입니다.</summary>
        private UnityEditor.Editor cachedEditor;
        #endregion // 필드

        #region 메뉴
        /// <summary>
        /// 스킬트리 그래프 창을 엽니다.
        /// </summary>
        [MenuItem(MenuPath)]
        public static void OpenWindow()
        {
            SkillTreeGraphWindow window = GetWindow<SkillTreeGraphWindow>();

            window.titleContent = new GUIContent("스킬트리 그래프");
            window.minSize = new Vector2(900f, 560f);
        }
        #endregion // 메뉴

        #region 함수
        /// <summary>
        /// 창의 화면을 구성합니다.
        /// </summary>
        private void CreateGUI()
        {
            rootVisualElement.Clear();

            if (database == null)
                database = AssetDatabase.LoadAssetAtPath<SWIODatabase>(DefaultDatabasePath);

            CreateToolbar();
            CreateBody();

            graphView.Reload(database);
        }

        /// <summary>
        /// 인스펙터를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            DestroyCachedEditor();
        }

        /// <summary>
        /// 데이터베이스 선택과 작업 버튼이 있는 도구 모음을 만듭니다.
        /// </summary>
        private void CreateToolbar()
        {
            Toolbar toolbar = new();

            ObjectField databaseField = new("데이터베이스")
            {
                objectType = typeof(SWIODatabase),
                value = database
            };

            databaseField.style.width = 380f;
            databaseField.RegisterValueChangedCallback(changed =>
            {
                database = changed.newValue as SWIODatabase;
                SelectDefinition(null);
                graphView.Reload(database);
            });

            toolbar.Add(databaseField);
            toolbar.Add(new ToolbarButton(CreateSkill) { text = "새 스킬" });
            toolbar.Add(new ToolbarButton(() => graphView.RequestDeleteSelection()) { text = "선택 삭제" });
            toolbar.Add(new ToolbarButton(() => graphView.Reload(database)) { text = "새로고침" });
            toolbar.Add(new ToolbarButton(() => graphView.FrameAll()) { text = "전체 보기" });

            rootVisualElement.Add(toolbar);
        }

        /// <summary>
        /// 그래프와 인스펙터가 나란히 놓이는 본문을 만듭니다.
        /// </summary>
        private void CreateBody()
        {
            VisualElement body = new();

            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;

            graphView = new SkillTreeGraphView(SelectDefinition, DeleteDefinition);
            graphView.style.flexGrow = 1f;

            inspectorPanel = new VisualElement();
            inspectorPanel.style.width = InspectorWidth;
            inspectorPanel.style.paddingLeft = 6f;
            inspectorPanel.style.paddingRight = 6f;
            inspectorPanel.style.paddingTop = 6f;
            inspectorPanel.Add(new IMGUIContainer(DrawInspector));

            body.Add(graphView);
            body.Add(inspectorPanel);

            rootVisualElement.Add(body);
        }

        /// <summary>
        /// 그래프에서 고른 스킬을 기억하고 인스펙터를 갈아 끼웁니다.
        /// </summary>
        /// <param name="definition">고른 스킬 정의입니다. 고른 것이 없으면 null입니다.</param>
        private void SelectDefinition(UpgradeDefinition definition)
        {
            if (selectedDefinition == definition) return;

            selectedDefinition = definition;

            DestroyCachedEditor();
        }

        /// <summary>
        /// 고른 스킬의 인스펙터를 그립니다.
        /// </summary>
        /// <remarks>
        /// 입력란을 따로 만들지 않고 인스펙터를 그대로 띄웁니다. 그래야 정의에 필드를 더할 때마다
        /// 이 창까지 같이 고치는 일이 없습니다(<see cref="AnomalyMakerWindow"/>와 같은 이유입니다).
        /// 값이 바뀌면 그래프를 통째로 다시 그립니다. 코드명이 바뀌면 노드를 찾는 열쇠까지 바뀌므로
        /// 제목만 고쳐서는 맞출 수 없습니다.
        /// </remarks>
        private void DrawInspector()
        {
            if (selectedDefinition == null)
            {
                EditorGUILayout.HelpBox("노드를 고르면 여기에서 값을 고칠 수 있습니다.", MessageType.Info);
                return;
            }

            UnityEditor.Editor.CreateCachedEditor(selectedDefinition, null, ref cachedEditor);

            EditorGUI.BeginChangeCheck();

            cachedEditor.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck() == false) return;

            rootVisualElement.schedule.Execute(() => graphView.Reload(database));
        }

        /// <summary>
        /// 스킬 정의 에셋을 새로 만들어 데이터베이스와 그래프에 얹습니다.
        /// </summary>
        /// <remarks>
        /// 데이터베이스가 있는 폴더에 만듭니다. 폴더를 따로 정하게 하면 데이터베이스에는 등록되었는데
        /// 엉뚱한 곳에 있는 에셋이 생깁니다.
        /// </remarks>
        private void CreateSkill()
        {
            if (database == null)
            {
                EditorUtility.DisplayDialog("스킬트리 그래프", "먼저 데이터베이스를 고르세요.", "확인");
                return;
            }

            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(database));
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{NewAssetName}.asset");
            UpgradeDefinition created = CreateInstance<UpgradeDefinition>();

            AssetDatabase.CreateAsset(created, path);

            SerializedObject serialized = new(created);

            serialized.FindProperty("codeName").stringValue = Path.GetFileNameWithoutExtension(path);
            serialized.FindProperty("treePosition").vector2Value = graphView.GetViewCenter();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            database.Add(created);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            graphView.AddNodeView(created);
            SelectDefinition(created);
        }

        /// <summary>
        /// 스킬을 데이터베이스와 다른 스킬의 선행 목록에서 지우고 에셋을 삭제합니다.
        /// </summary>
        /// <param name="definition">지울 스킬 정의입니다.</param>
        /// <remarks>
        /// 지워도 되는지 묻는 일은 그래프 쪽이 이미 마쳤습니다. 여기서 다시 묻지 않습니다.
        /// 선행 목록까지 훑는 이유는, 지운 스킬을 선행으로 걸어 둔 스킬이 남으면 그 스킬이
        /// 영영 열리지 않기 때문입니다. 에셋만 지우면 그 사실이 게임을 켜야 드러납니다.
        /// <para>
        /// 그래프를 다시 그리는 일은 미뤄 둡니다. 지금은 그래프가 변경 내용을 처리하는 도중이라,
        /// 그 자리에서 요소를 전부 갈아 끼우면 처리하던 목록이 발밑에서 사라집니다.
        /// </para>
        /// </remarks>
        private void DeleteDefinition(UpgradeDefinition definition)
        {
            if (definition == null || database == null) return;

            string path = AssetDatabase.GetAssetPath(definition);

            RemoveFromRequirements(definition.DefinitionId);

            database.Remove(definition);
            EditorUtility.SetDirty(database);

            if (selectedDefinition == definition) SelectDefinition(null);

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();

            rootVisualElement.schedule.Execute(() => graphView.Reload(database));
        }

        /// <summary>
        /// 모든 스킬의 선행 목록에서 지정한 식별자를 뺍니다.
        /// </summary>
        /// <param name="removedId">뺄 스킬의 식별자입니다.</param>
        private void RemoveFromRequirements(string removedId)
        {
            for (int index = 0; index < database.Count; index++)
            {
                if (database[index] is not UpgradeDefinition definition) continue;

                SerializedObject serialized = new(definition);
                SerializedProperty codes = serialized.FindProperty("requiredUpgradeCodes");
                bool isChanged = false;

                for (int codeIndex = codes.arraySize - 1; codeIndex >= 0; codeIndex--)
                {
                    if (codes.GetArrayElementAtIndex(codeIndex).stringValue != removedId) continue;

                    codes.DeleteArrayElementAtIndex(codeIndex);
                    isChanged = true;
                }

                if (isChanged) serialized.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 만들어 둔 인스펙터를 정리합니다.
        /// </summary>
        private void DestroyCachedEditor()
        {
            if (cachedEditor == null) return;

            DestroyImmediate(cachedEditor);
            cachedEditor = null;
        }
        #endregion // 함수
    }
}
