using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardTower.UI.Editor
{
    [CustomEditor(typeof(CardHandRoot))]
    public class CardHandRootEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(serializedObject.isEditingMultipleObjects))
            {
                if (GUILayout.Button("添加一张牌"))
                    AddOneCardInEditor();
            }
        }

        void AddOneCardInEditor()
        {
            var root = (CardHandRoot)target;
            var prefabProp = serializedObject.FindProperty("cardPrefab");
            var prefab = prefabProp.objectReferenceValue as HandCardView;
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("CardHandRoot", "请先指定 Card Prefab。", "确定");
                return;
            }

            var prefabGo = prefab.gameObject;
            GameObject instanceGo;

            if (PrefabUtility.IsPartOfPrefabAsset(prefabGo))
                instanceGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabGo, root.transform);
            else
                instanceGo = Instantiate(prefabGo, root.transform);

            Undo.RegisterCreatedObjectUndo(instanceGo, "Add Hand Card");

            var view = instanceGo.GetComponent<HandCardView>();
            if (view == null)
            {
                Undo.DestroyObjectImmediate(instanceGo);
                EditorUtility.DisplayDialog("CardHandRoot", "Card Prefab 根节点上需要 HandCardView 组件。", "确定");
                return;
            }

            root.RefreshCardListFromChildren();
            root.Reflow();

            EditorUtility.SetDirty(root);
            if (!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        }
    }
}
