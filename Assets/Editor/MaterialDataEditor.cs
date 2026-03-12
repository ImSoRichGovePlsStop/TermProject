using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MaterialData))]
public class MaterialDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Show only relevant fields (hide inherited module-specific ones)
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moduleName"),
                                      new GUIContent("Material Name"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rarity"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("moduleColor"),
                                      new GUIContent("Color"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStack"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shape Grid (5×5)", EditorStyles.boldLabel);

        // Legend
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.cyan;
        GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20));
        GUI.backgroundColor = Color.white;
        EditorGUILayout.LabelField("Shape", GUILayout.Width(50));
        GUI.backgroundColor = Color.gray;
        GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20));
        GUI.backgroundColor = Color.white;
        EditorGUILayout.LabelField("Empty", GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 5x5 grid
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

                GUI.backgroundColor = cell.boolValue ? Color.cyan : Color.gray;

                if (GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20)))
                    cell.boolValue = !cell.boolValue;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
