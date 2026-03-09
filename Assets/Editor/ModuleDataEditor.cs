using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ModuleData))]
public class ModuleDataEditor : Editor
{
    // Cell pixel size for the aura grid
    private const float CellSize = 28f;

    // Colours
    private static readonly Color ColBody    = new Color(0.30f, 0.60f, 1.00f); // blue
    private static readonly Color ColAura    = new Color(1.00f, 0.80f, 0.15f); // gold
    private static readonly Color ColNone    = new Color(0.20f, 0.20f, 0.20f); // dark grey
    private static readonly Color ColLabel   = Color.white;

    public override void OnInspectorGUI()
    {
        ModuleData module = (ModuleData)target;
        bool isBuff = module.moduleEffect is LevelUpBuffEffect;

        DrawPropertiesExcluding(serializedObject, "shapeGrid", "auraGrid");
        EditorGUILayout.Space();

        if (isBuff)
            DrawAuraGrid();   // one unified grid for buff modules
        else
            DrawShapeGrid();  // plain toggle grid for normal modules

        serializedObject.ApplyModifiedProperties();
    }

    // ── Normal module: toggle grid (unchanged from original) ─────────────────

    private void DrawShapeGrid()
    {
        EditorGUILayout.LabelField("Shape (5x5)", EditorStyles.boldLabel);
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
                cell.boolValue = EditorGUILayout.Toggle(cell.boolValue, GUILayout.Width(20));
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    // ── Buff module: single coloured grid ─────────────────────────────────────

    private void DrawAuraGrid()
    {
        EditorGUILayout.LabelField("Shape & Aura (5x5)", EditorStyles.boldLabel);
        DrawLegend();
        EditorGUILayout.Space(4);

        SerializedProperty grid = serializedObject.FindProperty("auraGrid");
        var cellStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle  = FontStyle.Bold,
            fontSize   = 13,
            alignment  = TextAnchor.MiddleCenter,
            fixedWidth  = CellSize,
            fixedHeight = CellSize,
        };

        for (int row = 0; row < 5; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < 5; col++)
            {
                SerializedProperty cell = grid
                    .GetArrayElementAtIndex(row)
                    .FindPropertyRelative("cells")
                    .GetArrayElementAtIndex(col);

                BuffCellType current = (BuffCellType)cell.enumValueIndex;

                // Background colour
                Color prevBg = GUI.backgroundColor;
                Color prevContent = GUI.contentColor;
                GUI.backgroundColor = current switch
                {
                    BuffCellType.Body => ColBody,
                    BuffCellType.Aura => ColAura,
                    _                 => ColNone,
                };
                GUI.contentColor = ColLabel;

                string label = current switch
                {
                    BuffCellType.Body => "■",
                    BuffCellType.Aura => "◆",
                    _                 => " ",
                };

                if (GUILayout.Button(label, cellStyle))
                    cell.enumValueIndex = ((int)current + 1) % 3; // cycle None→Body→Aura

                GUI.backgroundColor = prevBg;
                GUI.contentColor    = prevContent;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawLegend()
    {
        EditorGUILayout.BeginHorizontal();

        DrawSwatch(ColNone, "  ", "None");
        GUILayout.Space(6);
        DrawSwatch(ColBody, "■", "Body");
        GUILayout.Space(6);
        DrawSwatch(ColAura, "◆", "Aura");

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSwatch(Color bg, string icon, string label)
    {
        var swatchStyle = new GUIStyle(GUI.skin.box)
        {
            fontStyle  = FontStyle.Bold,
            fontSize   = 11,
            alignment  = TextAnchor.MiddleCenter,
            fixedWidth  = 20f,
            fixedHeight = 20f,
        };

        Color prevBg      = GUI.backgroundColor;
        Color prevContent = GUI.contentColor;
        GUI.backgroundColor = bg;
        GUI.contentColor    = ColLabel;
        GUILayout.Box(icon, swatchStyle);
        GUI.backgroundColor = prevBg;
        GUI.contentColor    = prevContent;

        GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(30));
    }
}