using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DebugUtility.WatchEntry))]
public class WatchEntryDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var labelProp = property.FindPropertyRelative("Label");
        var valueProp = property.FindPropertyRelative("Value");

        float half = position.width * 0.45f;
        var labelRect = new Rect(position.x, position.y, half, position.height);
        var valueRect = new Rect(position.x + half + 4, position.y, position.width - half - 4, position.height);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.LabelField(labelRect, labelProp.stringValue);
            EditorGUI.LabelField(valueRect, valueProp.stringValue, EditorStyles.textField);
        }
    }
}