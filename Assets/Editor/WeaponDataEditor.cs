using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor
{
    private bool statsFoldout = true;
    private bool dashFoldout = true;
    private bool comboFoldout = true;
    private bool secondaryFoldout = true;
    private bool cooldownFoldout = true;
    private bool wandFoldout = true;
    private bool gridFoldout = true;
    private int selectedComboIndex = 0;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("passiveData"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animatorOverrideController"));
        EditorGUILayout.Space();

        statsFoldout = EditorGUILayout.Foldout(statsFoldout, "Stats", true, EditorStyles.foldoutHeader);
        if (statsFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("health"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("damage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("critChance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("critDamage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("evadeChance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("damageTaken"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        dashFoldout = EditorGUILayout.Foldout(dashFoldout, "Dash", true, EditorStyles.foldoutHeader);
        if (dashFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dashSpeed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dashDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dashCooldown"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        comboFoldout = EditorGUILayout.Foldout(comboFoldout, "Primary Combo", true, EditorStyles.foldoutHeader);
        if (comboFoldout)
        {
            EditorGUI.indentLevel++;
            var comboProp = serializedObject.FindProperty("combo");

            if (comboProp.arraySize > 0)
            {
                EditorGUILayout.BeginHorizontal();
                string[] tabs = new string[comboProp.arraySize];
                for (int i = 0; i < comboProp.arraySize; i++)
                {
                    var triggerProp = comboProp.GetArrayElementAtIndex(i).FindPropertyRelative("animationTrigger");
                    string triggerName = triggerProp != null && !string.IsNullOrEmpty(triggerProp.stringValue)
                        ? triggerProp.stringValue : $"Hit {i}";
                    tabs[i] = triggerName;
                }
                selectedComboIndex = Mathf.Clamp(selectedComboIndex, 0, comboProp.arraySize - 1);
                selectedComboIndex = GUILayout.Toolbar(selectedComboIndex, tabs);

                if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.Height(20)))
                {
                    comboProp.arraySize++;
                    selectedComboIndex = comboProp.arraySize - 1;
                }
                GUILayout.Space(-4);
                if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.Height(20)) && comboProp.arraySize > 0)
                {
                    comboProp.arraySize--;
                    selectedComboIndex = Mathf.Clamp(selectedComboIndex, 0, comboProp.arraySize - 1);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(4);

                var selectedHit = comboProp.GetArrayElementAtIndex(selectedComboIndex);
                selectedHit.isExpanded = true;
                var child = selectedHit.Copy();
                var end = selectedHit.GetEndProperty();
                child.NextVisible(true);
                while (!SerializedProperty.EqualContents(child, end))
                {
                    EditorGUILayout.PropertyField(child, true);
                    child.NextVisible(false);
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("No combo hits.", MessageType.Info);
                if (GUILayout.Button("+", GUILayout.Width(25)))
                    comboProp.arraySize++;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        secondaryFoldout = EditorGUILayout.Foldout(secondaryFoldout, "Secondary Attack", true, EditorStyles.foldoutHeader);
        if (secondaryFoldout)
        {
            EditorGUI.indentLevel++;
            var secondaryProp = serializedObject.FindProperty("secondaryAttack");
            secondaryProp.isExpanded = true;
            var child = secondaryProp.Copy();
            var end = secondaryProp.GetEndProperty();
            child.NextVisible(true);
            while (!SerializedProperty.EqualContents(child, end))
            {
                EditorGUILayout.PropertyField(child, true);
                child.NextVisible(false);
            }
            EditorGUI.indentLevel--;
        }

        cooldownFoldout = EditorGUILayout.Foldout(cooldownFoldout, "Attack Cooldowns", true, EditorStyles.foldoutHeader);
        if (cooldownFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("comboCooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("secondaryCooldown"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("comboResetTime"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        var weaponTypeProp = serializedObject.FindProperty("weaponType");
        if (weaponTypeProp.enumValueIndex == (int)WeaponType.Wand)
        {
            wandFoldout = EditorGUILayout.Foldout(wandFoldout, "Wand Projectile", true, EditorStyles.foldoutHeader);
            if (wandFoldout)
            {
                EditorGUI.indentLevel++;
                var wandProp = serializedObject.FindProperty("wandProjectile");
                EditorGUILayout.PropertyField(wandProp, true);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        gridFoldout = EditorGUILayout.Foldout(gridFoldout, "Grid", true, EditorStyles.foldoutHeader);
        if (gridFoldout)
        {
            EditorGUI.indentLevel++;
            var gridProp = serializedObject.FindProperty("gridSizePerLevel");
            for (int i = 0; i < gridProp.arraySize; i++)
            {
                var element = gridProp.GetArrayElementAtIndex(i);
                var xProp = element.FindPropertyRelative("x");
                var yProp = element.FindPropertyRelative("y");
                var centeredField = new GUIStyle(EditorStyles.numberField) { alignment = TextAnchor.MiddleCenter };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Level {i + 1}", GUILayout.Width(70));
                EditorGUILayout.LabelField("Row", GUILayout.Width(40));
                GUILayout.Space(-10);
                yProp.intValue = EditorGUILayout.IntField(yProp.intValue, centeredField, GUILayout.Width(40));
                EditorGUILayout.LabelField("Col", GUILayout.Width(40));
                GUILayout.Space(-10);
                xProp.intValue = EditorGUILayout.IntField(xProp.intValue, centeredField, GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}