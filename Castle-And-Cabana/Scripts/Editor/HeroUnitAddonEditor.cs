using UnityEditor;
using UnityEngine;

//[CustomEditor(typeof(HeroUnitPrefabAddon))]
public class HeroUnitAddonEditor : Editor
{
    //// Foldout state variables
    //private bool showHeroUnitSettings = true;
    //private bool showSliders = true;
    //private bool showAnimator = true;
    //private bool showDebugging = true;
    //
    //public override void OnInspectorGUI()
    //{
    //    // Get reference to the target object
    //    HeroUnitPrefabAddon addon = (HeroUnitPrefabAddon)target;
    //
    //    // Hero Unit Settings
    //    showHeroUnitSettings = EditorGUILayout.Foldout(showHeroUnitSettings, "Hero Unit Settings", true);
    //    if (showHeroUnitSettings)
    //    {
    //        var myHeroUnitProperty = serializedObject.FindProperty("myHeroUnit");
    //        if (myHeroUnitProperty != null)
    //        {
    //            EditorGUILayout.PropertyField(myHeroUnitProperty, new GUIContent("Hero Unit"));
    //        }
    //        else
    //        {
    //            Debug.LogWarning("Property 'myHeroUnit' not found.");
    //        }
    //
    //        var myUnitInfoProperty = serializedObject.FindProperty("myUnitInfo");
    //        if (myUnitInfoProperty != null)
    //        {
    //            EditorGUILayout.PropertyField(myUnitInfoProperty, new GUIContent("Unit Info"));
    //        }
    //        else
    //        {
    //            Debug.LogWarning("Property 'myUnitInfo' not found.");
    //        }
    //    }
    //
    //
    //    // Sliders
    //    showSliders = EditorGUILayout.Foldout(showSliders, "Sliders", true);
    //    if (showSliders)
    //    {
    //        EditorGUILayout.PropertyField(serializedObject.FindProperty("healthBarSlider"), new GUIContent("Health Bar Slider"));
    //        EditorGUILayout.PropertyField(serializedObject.FindProperty("actionBarSlider"), new GUIContent("Action Bar Slider"));
    //        EditorGUILayout.PropertyField(serializedObject.FindProperty("healthSliderMaxValue"), new GUIContent("Health Slider Max Value"));
    //        EditorGUILayout.PropertyField(serializedObject.FindProperty("healthSliderValue"), new GUIContent("Health Slider Value"));
    //        EditorGUILayout.PropertyField(serializedObject.FindProperty("actionSliderMaxValue"), new GUIContent("Action Slider Max Value"));
    //        EditorGUILayout.PropertyField(serializedObject.FindProperty("actionSliderValue"), new GUIContent("Action Slider Value"));
    //    }
    //
    //    // Animator
    //    showAnimator = EditorGUILayout.Foldout(showAnimator, "Animator", true);
    //    if (showAnimator)
    //    {
    //        EditorGUILayout.PropertyField(serializedObject.FindProperty("animator"), new GUIContent("Animator"));
    //    }
    //
    //    // Debugging
    //    showDebugging = EditorGUILayout.Foldout(showDebugging, "Debugging", true);
    //    if (showDebugging)
    //    {
    //        EditorGUILayout.PropertyField(serializedObject.FindProperty("isDebugging"), new GUIContent("Enable Debugging"));
    //    }
    //
    //    // Apply changes to serialized properties
    //    serializedObject.ApplyModifiedProperties();
    //}
}
