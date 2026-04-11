#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BSPRoomPreset))]
public class BSPRoomPresetEditor : Editor
{
    const float CellSize    = 22f;
    const float CellPadding = 2f;

    static readonly Color VoidColor  = new Color(0.15f, 0.15f, 0.15f, 1f);
    static readonly Color FloorColor = new Color(0.25f, 0.65f, 0.55f, 1f);
    static readonly Color GridBg     = new Color(0.12f, 0.12f, 0.12f, 1f);

    public override void OnInspectorGUI()
    {
        var preset = (BSPRoomPreset)target;

        serializedObject.Update();

        EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        preset.sizeX = Mathf.Max(1, EditorGUILayout.IntField("Size X", preset.sizeX));
        preset.sizeZ = Mathf.Max(1, EditorGUILayout.IntField("Size Z", preset.sizeZ));
        if (EditorGUI.EndChangeCheck())
        {
            preset.ResetGrid();
            EditorUtility.SetDirty(preset);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Void Grid  (■ = void / blocked,  □ = floor)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Click cells to toggle. Void cells have no floor and block doors.",
            EditorStyles.miniLabel);
        EditorGUILayout.Space(4);

        if (preset.voidGrid == null || preset.voidGrid.Length != preset.sizeZ)
            preset.ResetGrid();

        DrawGrid(preset);

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();

        var allowedTypesProp = serializedObject.FindProperty("allowedTypes");
        EditorGUILayout.PropertyField(allowedTypesProp, new GUIContent("Allowed Types"), true);

        preset.variant = EditorGUILayout.IntField("Variant", preset.variant);

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(preset);
        }
    }

    void DrawGrid(BSPRoomPreset preset)
    {
        float gridW = preset.sizeX * (CellSize + CellPadding) + CellPadding;
        float gridH = preset.sizeZ * (CellSize + CellPadding) + CellPadding;

        // Reserve space for the grid
        Rect gridRect = GUILayoutUtility.GetRect(gridW, gridH,
            GUILayout.ExpandWidth(false));

        // Background
        EditorGUI.DrawRect(gridRect, GridBg);

        Event e = Event.current;

        for (int pz = 0; pz < preset.sizeZ; pz++)
        {
            for (int px = 0; px < preset.sizeX; px++)
            {
                float cellX = gridRect.x + CellPadding + px * (CellSize + CellPadding);
                float cellY = gridRect.y + CellPadding + pz * (CellSize + CellPadding);
                var cellRect = new Rect(cellX, cellY, CellSize, CellSize);

                bool isVoid = preset.IsVoid(px, pz);
                EditorGUI.DrawRect(cellRect, isVoid ? VoidColor : FloorColor);

                // Handle click
                if (e.type == EventType.MouseDown && e.button == 0 &&
                    cellRect.Contains(e.mousePosition))
                {
                    Undo.RecordObject(preset, "Toggle Void Cell");
                    preset.voidGrid[pz].cells[px] = !isVoid;
                    EditorUtility.SetDirty(preset);
                    e.Use();
                    Repaint();
                }
            }
        }

        // Labels: X axis
        Rect labelRow = new Rect(gridRect.x, gridRect.yMax + 2f, gridW, 14f);
        for (int px = 0; px < preset.sizeX; px++)
        {
            float lx = gridRect.x + CellPadding + px * (CellSize + CellPadding);
            EditorGUI.LabelField(new Rect(lx, labelRow.y, CellSize, 14f),
                px.ToString(), EditorStyles.centeredGreyMiniLabel);
        }

        GUILayoutUtility.GetRect(gridW, 16f);

        // Buttons row
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All", GUILayout.Width(90)))
        {
            Undo.RecordObject(preset, "Clear Void Grid");
            preset.ResetGrid();
            EditorUtility.SetDirty(preset);
        }
        if (GUILayout.Button("Fill All", GUILayout.Width(90)))
        {
            Undo.RecordObject(preset, "Fill Void Grid");
            foreach (var row in preset.voidGrid)
                for (int x = 0; x < row.cells.Length; x++)
                    row.cells[x] = true;
            EditorUtility.SetDirty(preset);
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif
