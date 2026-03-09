using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ModuleData))]
public class ModuleDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawPropertiesExcluding(serializedObject, "shapeGrid", "buffGrid");

        EditorGUILayout.Space();

        SerializedProperty isBuffAdjacent = serializedObject.FindProperty("isBuffAdjacent");
        SerializedProperty grid = serializedObject.FindProperty("shapeGrid");
        SerializedProperty buffGrid = serializedObject.FindProperty("buffGrid");

        // Legend
        if (isBuffAdjacent.boolValue)
        {
            EditorGUILayout.LabelField("Shape & Buff Grid (5×5)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.cyan;
            GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("Shape", GUILayout.Width(50));
            GUI.backgroundColor = Color.yellow;
            GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("Buff", GUILayout.Width(50));
            GUI.backgroundColor = Color.gray;
            GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20));
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("Empty", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.LabelField("Shape Grid (5×5)", EditorStyles.boldLabel);
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
        }
        EditorGUILayout.Space();

        // Grid
        for (int row = 0; row < 5; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < 5; col++)
            {
                SerializedProperty shapeCell = grid
                    .GetArrayElementAtIndex(row)
                    .FindPropertyRelative("cells")
                    .GetArrayElementAtIndex(col);

                SerializedProperty buffCell = buffGrid
                    .GetArrayElementAtIndex(row)
                    .FindPropertyRelative("cells")
                    .GetArrayElementAtIndex(col);

                if (shapeCell.boolValue)
                    GUI.backgroundColor = Color.cyan;
                else if (isBuffAdjacent.boolValue && buffCell.boolValue)
                    GUI.backgroundColor = Color.yellow;
                else
                    GUI.backgroundColor = Color.gray;

                if (GUILayout.Button("", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    bool isShape = shapeCell.boolValue;
                    bool isBuff = buffCell.boolValue;

                    if (!isShape && !isBuff)
                        shapeCell.boolValue = true;
                    else if (isShape && !isBuff)
                    {
                        if (isBuffAdjacent.boolValue)
                        {
                            shapeCell.boolValue = false;
                            buffCell.boolValue = true;
                        }
                        else
                            shapeCell.boolValue = false;
                    }
                    else if (!isShape && isBuff)
                        buffCell.boolValue = false;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }
}