using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ComboHit))]
public class ComboHitDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var triggerProp = property.FindPropertyRelative("animationTrigger");
        string triggerName = triggerProp != null && !string.IsNullOrEmpty(triggerProp.stringValue)
            ? triggerProp.stringValue
            : "unnamed";

        string index = label.text.Replace("Element ", "");
        GUIContent newLabel = new GUIContent($"Hit {index} — {triggerName}");

        EditorGUI.PropertyField(position, property, newLabel, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}