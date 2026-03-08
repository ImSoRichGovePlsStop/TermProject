using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ModuleData))]
public class ModuleDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ModuleData module = (ModuleData)target;

        DrawPropertiesExcluding(serializedObject, "shapeGrid");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shape (5×5)", EditorStyles.boldLabel);

        SerializedProperty grid = serializedObject.FindProperty("shapeGrid");

        for (int row = 0; row < 5; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < 5; col++)
            {
                SerializedProperty cell = grid
                    .GetArrayElementAtIndex(row)
                    .FindPropertyRelative("cells")
                    .GetArrayElementAtIndex(col);

                cell.boolValue = EditorGUILayout.Toggle(
                    cell.boolValue,
                    GUILayout.Width(20)
                );
            }
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }
}