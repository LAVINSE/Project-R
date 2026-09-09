using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;

using ProjectR.UI;

namespace ProjectR.Editor.UI
{
    /// <summary>
    /// 스킬트리 프리팹을 저장할 때 노드 자리를 정의 에셋에 되돌려 적는 연결 고리입니다.
    /// </summary>
    /// <remarks>
    /// 자리의 원본은 정의 에셋입니다. 프리팹에서 노드를 옮겨 놓고 저장만 하면 프리팹에는 새 자리가,
    /// 정의에는 옛 자리가 남아 둘이 갈라집니다. 그 상태에서 노드를 다시 놓으면 손으로 맞춘 배치가
    /// 통째로 사라집니다. 저장하는 순간에 되돌려 적어 두 자리를 항상 같게 만듭니다.
    /// <para>
    /// 저장 버튼을 따로 누르게 하지 않는 이유는, 누르는 것을 잊었을 때 잃는 것이 배치 전체이기
    /// 때문입니다. 잊을 수 있는 일은 잊어도 되게 만듭니다.
    /// </para>
    /// </remarks>
    [InitializeOnLoad]
    public static class SkillTreePrefabSaveHook
    {
        #region 생성자
        /// <summary>
        /// 프리팹 저장 알림을 구독합니다.
        /// </summary>
        /// <remarks>도메인 다시 불러오기가 꺼져 있으면 두 번 등록될 수 있으므로 먼저 떼어 냅니다.</remarks>
        static SkillTreePrefabSaveHook()
        {
            PrefabStage.prefabSaving -= HandlePrefabSaving;
            PrefabStage.prefabSaving += HandlePrefabSaving;
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 저장하려는 프리팹에 스킬트리가 있으면 노드 자리를 정의에 적습니다.
        /// </summary>
        /// <param name="prefabRoot">저장하려는 프리팹의 뿌리입니다.</param>
        private static void HandlePrefabSaving(GameObject prefabRoot)
        {
            SkillTreeUI[] trees = prefabRoot.GetComponentsInChildren<SkillTreeUI>(true);

            for (int index = 0; index < trees.Length; index++)
                trees[index].EditorSaveNodePositions(false);
        }
        #endregion // 함수
    }
}
