using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;

using UnityEngine;

using SW.Base;
using SW.EditorTools.Util;
using SW.Util;

using ProjectR.Data;
using ProjectR.Inventory;

namespace ProjectR.Editor.Data
{
    /// <summary>
    /// 이상물체 정의를 만들고 고치고 지우는 창입니다.
    /// </summary>
    /// <remarks>
    /// 정의를 하나 늘리려면 에셋 생성, 값 입력, 데이터베이스 등록을 모두 거쳐야 하고
    /// 그중 하나만 빠져도 실행해 봐야 드러납니다. 이 창은 그 과정을 한 화면에 모읍니다.
    /// 왼쪽에 만들어 둔 정의를 늘어놓고 오른쪽에 고른 것의 인스펙터를 띄우는 배치는
    /// SWUtils의 Stat System 창과 같습니다. 이미 손에 익은 조작을 그대로 쓰기 위해서입니다.
    /// 입력란을 따로 만들지 않고 인스펙터를 그대로 띄우는 이유는,
    /// 정의에 필드를 더할 때마다 이 창의 입력란까지 같이 고치는 일을 없애기 위해서입니다.
    /// 경로와 표시 방식은 EditorPrefs에 남겨 두어 창을 닫았다 열어도 그대로입니다.
    /// </remarks>
    public class AnomalyMakerWindow : EditorWindow
    {
        #region 상수
        /// <summary>EditorPrefs에 설정을 남길 때 앞에 붙이는 말입니다.</summary>
        private const string PreferenceKeyPrefix = "ProjectR.AnomalyMaker.";

        /// <summary>정의 에셋을 만들 기본 폴더입니다.</summary>
        private const string DefaultDefinitionFolder = "Assets/02_Res/Anomalies";

        /// <summary>등록할 데이터베이스 에셋의 기본 경로입니다.</summary>
        private const string DefaultDatabasePath = "Assets/02_Res/Anomalies/AnomalyDatabase.asset";

        /// <summary>에셋 이름 앞에 붙일 기본값입니다.</summary>
        private const string DefaultAssetPrefix = "Anomaly_";

        /// <summary>새로 만드는 정의에 붙일 임시 이름입니다.</summary>
        private const string NewAssetName = "New";

        /// <summary>이상물체 목록의 기본 너비입니다.</summary>
        private const float DefaultListWidth = 300f;

        /// <summary>이상물체 목록 행의 기본 높이입니다.</summary>
        private const float DefaultListRowHeight = 24f;

        /// <summary>이상물체 목록 아이콘의 기본 크기입니다.</summary>
        private const float DefaultListIconSize = 20f;

        /// <summary>이상물체 목록 이름의 기본 글자 크기입니다.</summary>
        private const int DefaultListLabelFontSize = 12;

        /// <summary>목록 행 안쪽 여백입니다.</summary>
        private const float ListRowPadding = 2f;

        /// <summary>세로 스크롤바에 가리지 않도록 목록 행 오른쪽에 비워 두는 폭입니다.</summary>
        private const float ListRowRightSafePadding = 18f;

        /// <summary>목록 행에 표시할 삭제 버튼의 너비입니다.</summary>
        private const float DeleteButtonWidth = 22f;

        /// <summary>목록 행에 표시할 삭제 버튼의 높이입니다.</summary>
        private const float DeleteButtonHeight = 18f;

        /// <summary>편집 창의 탭에 표시할 이름 목록입니다.</summary>
        private static readonly string[] TabNames = { "이상물체", "설정" };

        /// <summary>목록 정렬 방식의 표시 이름입니다.</summary>
        private static readonly string[] SortModeNames = { "코드명순", "표시명순", "ID순" };

        /// <summary>목록에서 사용할 이름 표시 방식입니다.</summary>
        private static readonly string[] LabelModeNames = { "코드명", "표시명", "에셋 이름" };
        #endregion // 상수

        #region 필드
        /// <summary>지금 보고 있는 탭입니다.</summary>
        private int tabIndex;

        /// <summary>프로젝트에서 모아 온 정의 목록입니다.</summary>
        private readonly List<AnomalyDefinition> definitions = new();

        /// <summary>목록에서 고른 정의입니다. 고른 것이 없으면 null입니다.</summary>
        private AnomalyDefinition selectedDefinition;

        /// <summary>고른 정의의 인스펙터입니다.</summary>
        private UnityEditor.Editor cachedEditor;

        /// <summary>이상물체 목록의 스크롤 위치입니다.</summary>
        private Vector2 listScrollPosition;

        /// <summary>선택한 이상물체 인스펙터의 스크롤 위치입니다.</summary>
        private Vector2 inspectorScrollPosition;

        /// <summary>편집 창 설정 화면의 스크롤 위치입니다.</summary>
        private Vector2 settingsScrollPosition;

        /// <summary>목록을 걸러 낼 검색어입니다.</summary>
        private string searchText = string.Empty;

        /// <summary>고른 행을 칠할 때 쓰는 그림입니다.</summary>
        private Texture2D selectedRowTexture;

        /// <summary>고른 행을 칠할 때 쓰는 모양입니다.</summary>
        private GUIStyle selectedRowStyle;

        /// <summary>이상물체 정의를 검색하고 생성할 폴더입니다.</summary>
        private string definitionFolder = DefaultDefinitionFolder;

        /// <summary>이상물체를 등록할 데이터베이스 에셋 경로입니다.</summary>
        private string databasePath = DefaultDatabasePath;

        /// <summary>생성할 에셋 이름 앞에 붙이는 문자열입니다.</summary>
        private string assetPrefix = DefaultAssetPrefix;

        /// <summary>수정한 에셋을 자동으로 저장할지 여부입니다.</summary>
        private bool autoSaveAssets = true;

        /// <summary>이상물체 목록의 너비입니다.</summary>
        private float listWidth = DefaultListWidth;

        /// <summary>이상물체 목록 행의 높이입니다.</summary>
        private float listRowHeight = DefaultListRowHeight;

        /// <summary>이상물체 목록 아이콘의 크기입니다.</summary>
        private float listIconSize = DefaultListIconSize;

        /// <summary>이상물체 목록 이름의 글자 크기입니다.</summary>
        private int listLabelFontSize = DefaultListLabelFontSize;

        /// <summary>이상물체 목록에 적용할 정렬 방식의 번호입니다.</summary>
        private int sortMode;

        /// <summary>이상물체 목록에 적용할 이름 표시 방식의 번호입니다.</summary>
        private int labelMode;
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 창을 엽니다.
        /// </summary>
        [MenuItem("Project R/데이터/이상물체", priority = 20)]
        public static void Open()
        {
            AnomalyMakerWindow window = GetWindow<AnomalyMakerWindow>();

            SWEditorUtils.SetupWindow(window, "이상물체 데이터", minWidth: 720f, minHeight: 480f);

            // SetupWindow가 쓰는 FindTexture는 내장 아이콘 이름을 찾지 못해 탭이 그림 없이 뜹니다.
            window.titleContent.image = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image;

            window.Show();
        }

        /// <summary>
        /// 설정을 읽어 오고 목록을 채웁니다.
        /// </summary>
        private void OnEnable()
        {
            SetupStyle();
            LoadSettings();
            RefreshDefinitions();
        }

        /// <summary>
        /// 설정을 남기고 만들어 둔 것을 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            SaveSettings();

            DestroyImmediate(cachedEditor);
            DestroyImmediate(selectedRowTexture);
        }

        /// <summary>
        /// 고른 행을 칠할 모양을 준비합니다.
        /// </summary>
        private void SetupStyle()
        {
            selectedRowTexture = new Texture2D(1, 1);
            selectedRowTexture.SetPixel(0, 0, new Color(0.31f, 0.40f, 0.50f));
            selectedRowTexture.Apply();

            // 플레이 모드를 오갈 때 함께 지워지지 않도록 저장 대상에서 뺍니다.
            selectedRowTexture.hideFlags = HideFlags.DontSave;

            selectedRowStyle = new GUIStyle();
            selectedRowStyle.normal.background = selectedRowTexture;
        }
        #endregion // 초기화

        #region 설정 저장
        /// <summary>
        /// EditorPrefs에서 설정을 읽어 옵니다.
        /// </summary>
        private void LoadSettings()
        {
            definitionFolder = SWEditorUtils.LoadPref($"{PreferenceKeyPrefix}Folder", DefaultDefinitionFolder);
            databasePath = SWEditorUtils.LoadPref($"{PreferenceKeyPrefix}Database", DefaultDatabasePath);
            assetPrefix = SWEditorUtils.LoadPref($"{PreferenceKeyPrefix}Prefix", DefaultAssetPrefix);
            autoSaveAssets = SWEditorUtils.LoadPref($"{PreferenceKeyPrefix}AutoSave", true);
            listWidth = SWEditorUtils.LoadPref($"{PreferenceKeyPrefix}ListWidth", DefaultListWidth);
            listRowHeight = SWEditorUtils.LoadPref($"{PreferenceKeyPrefix}ListRowHeight", DefaultListRowHeight);
            listIconSize = SWEditorUtils.LoadPref($"{PreferenceKeyPrefix}ListIconSize", DefaultListIconSize);
            listLabelFontSize = SWEditorUtils.LoadPref($"{PreferenceKeyPrefix}ListLabelFontSize", DefaultListLabelFontSize);
            sortMode = SWEditorUtils.LoadPref($"{PreferenceKeyPrefix}SortMode", 0);
            labelMode = SWEditorUtils.LoadPref($"{PreferenceKeyPrefix}LabelMode", 0);
        }

        /// <summary>
        /// EditorPrefs에 설정을 남깁니다.
        /// </summary>
        private void SaveSettings()
        {
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}Folder", definitionFolder);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}Database", databasePath);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}Prefix", assetPrefix);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}AutoSave", autoSaveAssets);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}ListWidth", listWidth);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}ListRowHeight", listRowHeight);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}ListIconSize", listIconSize);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}ListLabelFontSize", listLabelFontSize);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}SortMode", sortMode);
            SWEditorUtils.SavePref($"{PreferenceKeyPrefix}LabelMode", labelMode);
        }

        /// <summary>
        /// 설정을 기본값으로 되돌립니다.
        /// </summary>
        private void ResetSettings()
        {
            definitionFolder = DefaultDefinitionFolder;
            databasePath = DefaultDatabasePath;
            assetPrefix = DefaultAssetPrefix;
            autoSaveAssets = true;
            listWidth = DefaultListWidth;
            listRowHeight = DefaultListRowHeight;
            listIconSize = DefaultListIconSize;
            listLabelFontSize = DefaultListLabelFontSize;
            sortMode = 0;
            labelMode = 0;

            SaveSettings();
        }
        #endregion // 설정 저장

        #region 그리기
        /// <summary>
        /// 창 내용을 그립니다.
        /// </summary>
        private void OnGUI()
        {
            tabIndex = SWEditorUtils.DrawTabBar(tabIndex, TabNames);

            if (tabIndex == 1)
            {
                DrawSettingsTab();
                return;
            }

            DrawDataTab();
        }

        /// <summary>
        /// 목록과 인스펙터를 나란히 그립니다.
        /// </summary>
        private void DrawDataTab()
        {
            AssetPreview.SetPreviewTextureCacheSize(Mathf.Max(32, 32 + definitions.Count));

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(listWidth));
                {
                    DrawListToolButtons();

                    EditorGUILayout.Space(4f);

                    searchText = GUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
                    DrawSortToolbar();

                    listScrollPosition = EditorGUILayout.BeginScrollView(
                        listScrollPosition, false, true,
                        GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none);
                    {
                        DrawDefinitionList();
                    }
                    EditorGUILayout.EndScrollView();

                    DrawProblemNotice();
                }
                EditorGUILayout.EndVertical();

                if (selectedDefinition != null)
                {
                    inspectorScrollPosition = EditorGUILayout.BeginScrollView(inspectorScrollPosition);
                    {
                        EditorGUILayout.Space(2f);
                        DrawSelectedHeader();

                        UnityEditor.Editor.CreateCachedEditor(selectedDefinition, null, ref cachedEditor);
                        cachedEditor.OnInspectorGUI();
                    }
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.BeginVertical();
                    SWEditorUtils.DrawEmptyNotice("왼쪽 목록에서 이상물체를 고르거나 새로 만들어 주세요.", MessageType.Info);
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 목록 위쪽의 만들기·삭제·새로고침 버튼을 그립니다.
        /// </summary>
        private void DrawListToolButtons()
        {
            using (new SWEditorUtils.GUIBgColorScope(new Color(0.6f, 1f, 0.6f)))
            {
                if (GUILayout.Button("새 이상물체", GUILayout.Height(24f))) CreateDefinition();
            }

            EditorGUILayout.BeginHorizontal();

            using (new SWEditorUtils.GUIEnabledScope(selectedDefinition != null))
            using (new SWEditorUtils.GUIBgColorScope(new Color(1f, 0.6f, 0.6f)))
            {
                if (GUILayout.Button("선택 삭제", GUILayout.Height(20f))) DeleteDefinition(selectedDefinition);
            }

            if (GUILayout.Button("새로고침", GUILayout.Height(20f))) RefreshDefinitions();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 정렬 기준을 바로 바꾸는 줄을 그립니다.
        /// </summary>
        private void DrawSortToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("정렬", GUILayout.Width(30f));
            DrawSortShortcutButton("코드명", 0);
            DrawSortShortcutButton("표시명", 1);
            DrawSortShortcutButton("ID", 2);

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 정렬 기준 하나를 고르는 버튼을 그립니다.
        /// </summary>
        /// <param name="label">버튼에 적을 말입니다.</param>
        /// <param name="targetSortMode">이 버튼이 고르는 정렬 기준입니다.</param>
        private void DrawSortShortcutButton(string label, int targetSortMode)
        {
            Color buttonColor = sortMode == targetSortMode ? new Color(0.55f, 0.75f, 1f) : Color.white;

            using (new SWEditorUtils.GUIBgColorScope(buttonColor))
            {
                if (GUILayout.Button(label, EditorStyles.toolbarButton)) SetSortMode(targetSortMode);
            }
        }

        /// <summary>
        /// 정의 목록을 그립니다.
        /// </summary>
        private void DrawDefinitionList()
        {
            if (definitions.Count == 0)
            {
                SWEditorUtils.DrawEmptyNotice("만들어 둔 이상물체가 없습니다.", MessageType.None);
                return;
            }

            float drawRowHeight = GetListRowDrawHeight();

            for (int index = 0; index < definitions.Count; index++)
            {
                AnomalyDefinition definition = definitions[index];

                if (definition == null) continue;

                string label = GetListLabel(definition);

                if (string.IsNullOrEmpty(searchText) == false
                    && SWEditorUtils.MatchesFilter(label, searchText) == false
                    && SWEditorUtils.MatchesFilter(definition.CodeName, searchText) == false)
                {
                    continue;
                }

                Rect rowRectangle = GUILayoutUtility.GetRect(0f, drawRowHeight, GUILayout.ExpandWidth(true));
                string idText = definition.ID != 0 ? $"[{definition.ID}] " : string.Empty;

                bool isDeleteClicked = DrawListRow(
                    rowRectangle,
                    $"{idText}{label}",
                    selectedDefinition == definition,
                    definition,
                    out Rect deleteButtonRectangle);

                if (isDeleteClicked)
                {
                    DeleteDefinition(definition);
                    break;
                }

                if (Event.current.type != EventType.MouseDown) continue;
                if (rowRectangle.Contains(Event.current.mousePosition) == false) continue;
                if (deleteButtonRectangle.Contains(Event.current.mousePosition)) continue;

                Select(definition);
                Event.current.Use();
            }
        }

        /// <summary>
        /// 목록 행 하나를 그립니다.
        /// </summary>
        /// <param name="rowRectangle">행이 차지할 자리입니다.</param>
        /// <param name="label">행에 적을 말입니다.</param>
        /// <param name="isSelected">지금 고른 행인지 여부입니다.</param>
        /// <param name="definition">행이 가리키는 정의입니다.</param>
        /// <param name="deleteButtonRectangle">삭제 버튼이 차지한 자리입니다.</param>
        /// <returns>삭제 버튼을 눌렀으면 true를 반환합니다.</returns>
        private bool DrawListRow(Rect rowRectangle, string label, bool isSelected,
            AnomalyDefinition definition, out Rect deleteButtonRectangle)
        {
            if (isSelected) GUI.Box(rowRectangle, GUIContent.none, selectedRowStyle);

            Rect iconRectangle = new(
                rowRectangle.x + ListRowPadding,
                rowRectangle.y + ((rowRectangle.height - listIconSize) * 0.5f),
                listIconSize,
                listIconSize);

            if (definition.Icon != null) SWEditorUtils.DrawSpriteIcon(iconRectangle, definition.Icon);
            else EditorGUI.DrawRect(iconRectangle, definition.DisplayColor);

            deleteButtonRectangle = new Rect(
                rowRectangle.xMax - ListRowRightSafePadding - DeleteButtonWidth - ListRowPadding,
                rowRectangle.y + ((rowRectangle.height - DeleteButtonHeight) * 0.5f),
                DeleteButtonWidth,
                DeleteButtonHeight);

            Rect labelRectangle = new(
                iconRectangle.xMax + 4f,
                rowRectangle.y + ListRowPadding,
                Mathf.Max(1f, deleteButtonRectangle.x - iconRectangle.xMax - 8f),
                rowRectangle.height - (ListRowPadding * 2f));

            GUIStyle labelStyle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = listLabelFontSize
            };

            EditorGUI.LabelField(labelRectangle, label, labelStyle);

            using (new SWEditorUtils.GUIBgColorScope(new Color(1f, 0.6f, 0.6f)))
            {
                GUIStyle deleteButtonStyle = new(GUI.skin.button) { fontSize = listLabelFontSize };

                return GUI.Button(deleteButtonRectangle, "x", deleteButtonStyle);
            }
        }

        /// <summary>
        /// 고른 정의의 이름 바꾸기와 위치 확인 도구를 그립니다.
        /// </summary>
        private void DrawSelectedHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            string changedName = EditorGUILayout.DelayedTextField("에셋 이름", selectedDefinition.name);

            if (EditorGUI.EndChangeCheck()) RenameDefinition(selectedDefinition, changedName);

            if (GUILayout.Button("Ping", GUILayout.Width(45f))) SWEditorUtils.PingAndSelect(selectedDefinition);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"형태 {selectedDefinition.Shape}", EditorStyles.miniLabel);
            DrawShapePreview(selectedDefinition);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 형태가 격자에서 어떻게 보이는지 그려 줍니다.
        /// </summary>
        /// <remarks>숫자만 보면 2x3과 3x2를 자주 뒤바꿉니다.</remarks>
        /// <param name="definition">형태를 그릴 정의입니다.</param>
        private void DrawShapePreview(AnomalyDefinition definition)
        {
            const float CellSize = 16f;

            InventoryShape shape = definition.Shape;
            Rect area = GUILayoutUtility.GetRect(shape.Width * CellSize, shape.Height * CellSize,
                GUILayout.ExpandWidth(false));

            for (int y = 0; y < shape.Height; y++)
            {
                for (int x = 0; x < shape.Width; x++)
                {
                    Rect cell = new(
                        area.x + (x * CellSize),
                        area.y + (y * CellSize),
                        CellSize - 1f,
                        CellSize - 1f);

                    EditorGUI.DrawRect(cell, definition.DisplayColor);
                }
            }
        }

        /// <summary>
        /// 그대로 두면 실행해 봐야 드러나는 문제를 목록 아래에 알립니다.
        /// </summary>
        /// <remarks>
        /// 코드명이 겹치면 <see cref="AnomalyDefinition.DefinitionId"/>로 찾을 때 엉뚱한 것이 나오고,
        /// 데이터베이스에 없으면 만들어 두고도 백룸에 나오지 않습니다. 둘 다 눈으로는 보이지 않습니다.
        /// </remarks>
        private void DrawProblemNotice()
        {
            SWIODatabase database = LoadDatabase();

            int missingArtCount = 0;
            int unregisteredCount = 0;
            HashSet<string> seenCodeNames = new();
            HashSet<string> duplicatedCodeNames = new();

            for (int index = 0; index < definitions.Count; index++)
            {
                AnomalyDefinition definition = definitions[index];

                if (definition == null) continue;

                if (definition.Icon == null || definition.WorldPrefab == null) missingArtCount++;
                if (database != null && database.Contains(definition) == false) unregisteredCount++;
                if (seenCodeNames.Add(definition.DefinitionId) == false) duplicatedCodeNames.Add(definition.DefinitionId);
            }

            if (database == null)
            {
                EditorGUILayout.HelpBox($"데이터베이스를 찾지 못했습니다: {databasePath}", MessageType.Error);
                return;
            }

            if (duplicatedCodeNames.Count > 0)
            {
                EditorGUILayout.HelpBox($"코드명이 겹칩니다: {string.Join(", ", duplicatedCodeNames)}", MessageType.Error);
            }

            if (unregisteredCount > 0)
            {
                EditorGUILayout.HelpBox($"데이터베이스에 등록되지 않은 정의 {unregisteredCount}개가 있습니다. " +
                    "등록하지 않으면 백룸에 나오지 않습니다.", MessageType.Warning);

                if (GUILayout.Button("빠진 것 모두 등록")) RegisterMissing(database);
            }

            if (missingArtCount > 0)
            {
                EditorGUILayout.HelpBox($"아이콘이나 모델이 비어 있는 정의 {missingArtCount}개가 있습니다. " +
                    "비어 있으면 형태대로 만든 상자가 대신 나옵니다.", MessageType.Info);
            }
        }

        /// <summary>
        /// 설정 탭을 그립니다.
        /// </summary>
        private void DrawSettingsTab()
        {
            settingsScrollPosition = EditorGUILayout.BeginScrollView(settingsScrollPosition);

            SWEditorUtils.DrawHeader("에셋 생성 설정");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            definitionFolder = EditorGUILayout.TextField("생성 폴더", definitionFolder);

            if (GUILayout.Button("선택", GUILayout.Width(44f)))
            {
                string pickedPath = PickProjectFolder(definitionFolder);

                if (string.IsNullOrEmpty(pickedPath) == false) definitionFolder = pickedPath;
            }
            EditorGUILayout.EndHorizontal();

            if (IsProjectPath(definitionFolder) == false)
                EditorGUILayout.HelpBox("경로는 Assets/ 로 시작해야 합니다.", MessageType.Warning);

            assetPrefix = EditorGUILayout.TextField("파일 이름 접두사", assetPrefix);

            EditorGUILayout.BeginHorizontal();
            databasePath = EditorGUILayout.TextField("데이터베이스", databasePath);

            using (new SWEditorUtils.GUIEnabledScope(LoadDatabase() != null))
            {
                if (GUILayout.Button("Ping", GUILayout.Width(44f))) SWEditorUtils.PingAssetAtPath(databasePath);
            }
            EditorGUILayout.EndHorizontal();

            if (LoadDatabase() == null)
                EditorGUILayout.HelpBox("이 경로에 데이터베이스가 없습니다.", MessageType.Warning);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);
            SWEditorUtils.DrawHeader("동작 설정");

            autoSaveAssets = EditorGUILayout.ToggleLeft("만들거나 지운 뒤 바로 저장", autoSaveAssets);

            EditorGUILayout.Space(6f);
            SWEditorUtils.DrawHeader("표시 설정");

            listWidth = EditorGUILayout.Slider("목록 넓이", listWidth, 200f, 450f);
            listRowHeight = EditorGUILayout.Slider("목록 행 높이", listRowHeight, 20f, 48f);
            listIconSize = EditorGUILayout.Slider("목록 아이콘 크기", listIconSize, 16f, 40f);
            listLabelFontSize = EditorGUILayout.IntSlider("목록 글자 크기", listLabelFontSize, 10, 18);
            labelMode = EditorGUILayout.Popup("목록 표시 이름", labelMode, LabelModeNames);

            int changedSortMode = EditorGUILayout.Popup("정렬 기준", sortMode, SortModeNames);

            if (changedSortMode != sortMode) SetSortMode(changedSortMode);

            EditorGUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("설정 저장", GUILayout.Height(24f)))
            {
                SaveSettings();
                ShowNotification(new GUIContent("설정을 저장했습니다."));
            }

            if (GUILayout.Button("기본값 복원", GUILayout.Height(24f))
                && EditorUtility.DisplayDialog("설정 초기화", "모든 설정을 기본값으로 되돌릴까요?", "복원", "취소"))
            {
                ResetSettings();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 표시 설정을 모두 담을 수 있는 행 높이를 구합니다.
        /// </summary>
        /// <returns>실제로 그릴 행 높이입니다.</returns>
        private float GetListRowDrawHeight()
        {
            return Mathf.Max(listRowHeight, listIconSize + (ListRowPadding * 2f));
        }

        /// <summary>
        /// 설정에 따라 목록에 적을 이름을 고릅니다.
        /// </summary>
        /// <param name="definition">이름을 구할 정의입니다.</param>
        /// <returns>목록에 적을 이름입니다.</returns>
        private string GetListLabel(AnomalyDefinition definition)
        {
            return labelMode switch
            {
                1 => definition.DisplayName,
                2 => definition.name,
                _ => string.IsNullOrEmpty(definition.CodeName) ? definition.name : definition.CodeName,
            };
        }
        #endregion // 그리기

        #region 에셋 관리
        /// <summary>
        /// 프로젝트에서 정의를 다시 모아 정렬합니다.
        /// </summary>
        private void RefreshDefinitions()
        {
            definitions.Clear();

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(AnomalyDefinition)}");

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                AnomalyDefinition definition = AssetDatabase.LoadAssetAtPath<AnomalyDefinition>(path);

                if (definition != null) definitions.Add(definition);
            }

            SortDefinitions();

            if (definitions.Contains(selectedDefinition) == false) Select(null);
        }

        /// <summary>
        /// 설정한 기준으로 목록을 정렬합니다.
        /// </summary>
        private void SortDefinitions()
        {
            switch (sortMode)
            {
                case 1:
                    definitions.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal));
                    break;

                case 2:
                    definitions.Sort((left, right) => left.ID.CompareTo(right.ID));
                    break;

                default:
                    definitions.Sort((left, right) => string.Compare(left.CodeName, right.CodeName, StringComparison.Ordinal));
                    break;
            }
        }

        /// <summary>
        /// 정렬 기준을 바꾸고 목록을 다시 정렬합니다.
        /// </summary>
        /// <param name="changedSortMode">새로 고른 정렬 기준입니다.</param>
        private void SetSortMode(int changedSortMode)
        {
            if (sortMode == changedSortMode) return;

            sortMode = changedSortMode;

            SortDefinitions();
            SaveSettings();
        }

        /// <summary>
        /// 목록에서 고른 것을 바꿉니다.
        /// </summary>
        /// <param name="definition">새로 고를 정의입니다. 없으면 null입니다.</param>
        private void Select(AnomalyDefinition definition)
        {
            selectedDefinition = definition;
            inspectorScrollPosition = Vector2.zero;

            GUI.FocusControl(null);
        }

        /// <summary>
        /// 빈 정의를 만들고 데이터베이스에 등록합니다.
        /// </summary>
        /// <remarks>값은 오른쪽 인스펙터에서 채웁니다. 코드명은 파일 이름과 같게 시작해 겹치지 않습니다.</remarks>
        private void CreateDefinition()
        {
            if (IsProjectPath(definitionFolder) == false)
            {
                EditorUtility.DisplayDialog("만들지 못했습니다",
                    $"생성 폴더가 올바르지 않습니다:\n{definitionFolder}\n\n설정 탭에서 Assets/ 로 시작하는 경로를 지정해 주세요.", "확인");
                return;
            }

            SWIODatabase database = LoadDatabase();

            if (database == null)
            {
                EditorUtility.DisplayDialog("만들지 못했습니다",
                    $"데이터베이스를 찾지 못했습니다:\n{databasePath}", "확인");
                return;
            }

            EnsureFolderExists(definitionFolder);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{definitionFolder.TrimEnd('/')}/{assetPrefix}{NewAssetName}.asset");
            AnomalyDefinition definition = CreateInstance<AnomalyDefinition>();

            AssetDatabase.CreateAsset(definition, assetPath);

            SerializedObject serialized = new(definition);
            serialized.FindProperty("codeName").stringValue = Path.GetFileNameWithoutExtension(assetPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // ID가 0이면 데이터베이스가 다음 번호를 붙여 줍니다.
            database.Add(definition);

            EditorUtility.SetDirty(definition);

            if (autoSaveAssets) AssetDatabase.SaveAssets();

            RefreshDefinitions();
            Select(definition);

            SWLog.Log($"[{nameof(AnomalyMakerWindow)}] 이상물체를 만들고 등록했습니다: {assetPath}");
        }

        /// <summary>
        /// 정의를 데이터베이스에서 빼고 에셋을 지웁니다.
        /// </summary>
        /// <param name="definition">지울 정의입니다.</param>
        private void DeleteDefinition(AnomalyDefinition definition)
        {
            if (definition == null) return;

            string assetPath = AssetDatabase.GetAssetPath(definition);

            if (EditorUtility.DisplayDialog("이상물체 삭제",
                $"'{definition.DisplayName}' 을(를) 지울까요?\n{assetPath}\n\n되돌릴 수 없습니다.", "삭제", "취소") == false)
            {
                return;
            }

            SWIODatabase database = LoadDatabase();

            if (database != null) database.Remove(definition);

            if (selectedDefinition == definition) Select(null);

            AssetDatabase.DeleteAsset(assetPath);

            if (autoSaveAssets) AssetDatabase.SaveAssets();

            RefreshDefinitions();

            SWLog.Log($"[{nameof(AnomalyMakerWindow)}] 이상물체를 지웠습니다: {assetPath}");
        }

        /// <summary>
        /// 정의의 에셋 이름을 바꿉니다.
        /// </summary>
        /// <param name="definition">이름을 바꿀 정의입니다.</param>
        /// <param name="changedName">새 이름입니다.</param>
        private void RenameDefinition(AnomalyDefinition definition, string changedName)
        {
            changedName = string.IsNullOrWhiteSpace(changedName) ? definition.name : changedName.Trim();

            if (changedName == definition.name) return;

            if (changedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                EditorUtility.DisplayDialog("이름을 바꾸지 못했습니다", "파일 이름으로 쓸 수 없는 글자가 들어 있습니다.", "확인");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(definition);
            string error = AssetDatabase.RenameAsset(assetPath, changedName);

            if (string.IsNullOrEmpty(error) == false)
            {
                EditorUtility.DisplayDialog("이름을 바꾸지 못했습니다", error, "확인");
                return;
            }

            if (autoSaveAssets) AssetDatabase.SaveAssets();

            RefreshDefinitions();
            Select(definition);
        }

        /// <summary>
        /// 데이터베이스에 빠져 있는 정의를 모두 등록합니다.
        /// </summary>
        /// <param name="database">등록할 데이터베이스입니다.</param>
        private void RegisterMissing(SWIODatabase database)
        {
            for (int index = 0; index < definitions.Count; index++)
            {
                if (definitions[index] == null) continue;
                if (database.Contains(definitions[index])) continue;

                database.Add(definitions[index]);
            }

            if (autoSaveAssets) AssetDatabase.SaveAssets();

            RefreshDefinitions();
        }

        /// <summary>
        /// 설정한 경로에서 데이터베이스를 읽어 옵니다.
        /// </summary>
        /// <returns>데이터베이스입니다. 없으면 null입니다.</returns>
        private SWIODatabase LoadDatabase()
        {
            return AssetDatabase.LoadAssetAtPath<SWIODatabase>(databasePath);
        }
        #endregion // 에셋 관리

        #region 경로
        /// <summary>
        /// 경로가 프로젝트 안쪽 경로인지 확인합니다.
        /// </summary>
        /// <param name="path">확인할 경로입니다.</param>
        /// <returns>Assets 아래의 경로이면 true를 반환합니다.</returns>
        private static bool IsProjectPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) == false
                && (path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal));
        }

        /// <summary>
        /// 폴더 선택 창을 열고 프로젝트 기준 경로로 바꿔 돌려줍니다.
        /// </summary>
        /// <param name="currentPath">지금 설정된 경로입니다.</param>
        /// <returns>고른 폴더의 경로입니다. 고르지 않았으면 null입니다.</returns>
        private static string PickProjectFolder(string currentPath)
        {
            string startFolder = IsProjectPath(currentPath) && AssetDatabase.IsValidFolder(currentPath)
                ? currentPath
                : "Assets";

            string absolutePath = EditorUtility.OpenFolderPanel("이상물체를 만들 폴더", startFolder, string.Empty);

            if (string.IsNullOrEmpty(absolutePath)) return null;

            string projectRoot = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
            absolutePath = absolutePath.Replace('\\', '/');

            if (string.IsNullOrEmpty(projectRoot) || absolutePath.StartsWith(projectRoot, StringComparison.Ordinal) == false)
            {
                EditorUtility.DisplayDialog("경로 오류", "프로젝트 안쪽(Assets 아래) 폴더만 고를 수 있습니다.", "확인");
                return null;
            }

            return absolutePath.Substring(projectRoot.Length + 1);
        }

        /// <summary>
        /// 폴더가 없으면 위에서부터 차례로 만듭니다.
        /// </summary>
        /// <param name="folderPath">있어야 하는 폴더 경로입니다.</param>
        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string nextPath = $"{currentPath}/{parts[index]}";

                if (AssetDatabase.IsValidFolder(nextPath) == false) AssetDatabase.CreateFolder(currentPath, parts[index]);

                currentPath = nextPath;
            }
        }
        #endregion // 경로
    }
}
