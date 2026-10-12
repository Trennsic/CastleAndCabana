using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor drawer for <see cref="SubclassSelectorAttribute"/>. Adds a type-picker
/// dropdown to <c>[SerializeReference]</c> fields and list elements so concrete
/// subclasses can be chosen in the Inspector (Unity has no built-in picker for this).
///
/// Place this file inside an <c>Editor</c> folder.
/// </summary>
[CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
public class SubclassSelectorDrawer : PropertyDrawer
{
    // Cache of base-type -> concrete subclasses so reflection runs once per type.
    private static readonly Dictionary<string, Type[]> typeCache = new Dictionary<string, Type[]>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            // Not a SerializeReference field — fall back to the default drawer.
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        // Draw the field (foldout, label, and any child fields) normally first.
        EditorGUI.PropertyField(position, property, label, true);

        // Overlay a dropdown button on the (otherwise empty) value area of the first line.
        Rect dropdownRect = new Rect(
            position.x + EditorGUIUtility.labelWidth + 2f,
            position.y,
            position.width - EditorGUIUtility.labelWidth - 2f,
            EditorGUIUtility.singleLineHeight);

        GUIContent current = new GUIContent(GetCurrentTypeDisplayName(property));
        if (EditorGUI.DropdownButton(dropdownRect, current, FocusType.Keyboard))
            ShowTypeMenu(property);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUI.GetPropertyHeight(property, label, true);

    // -----------------------------------------------------------------------
    // Type menu
    // -----------------------------------------------------------------------

    private void ShowTypeMenu(SerializedProperty property)
    {
        // Capture object + path; the menu callback fires after OnGUI, so we re-resolve
        // the property to avoid acting on a stale SerializedProperty.
        SerializedObject so = property.serializedObject;
        string path = property.propertyPath;

        Type baseType = GetManagedReferenceFieldType(property);
        Type[] types = GetDerivedTypes(baseType);

        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("(null)"), false, () => AssignType(so, path, null));

        foreach (Type t in types)
        {
            Type captured = t;
            menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(t.Name)), false,
                () => AssignType(so, path, captured));
        }

        menu.ShowAsContext();
    }

    private static void AssignType(SerializedObject so, string propertyPath, Type type)
    {
        so.Update();
        SerializedProperty p = so.FindProperty(propertyPath);
        if (p == null) return;

        p.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
        so.ApplyModifiedProperties();
    }

    // -----------------------------------------------------------------------
    // Reflection helpers
    // -----------------------------------------------------------------------

    private static string GetCurrentTypeDisplayName(SerializedProperty property)
    {
        string full = property.managedReferenceFullTypename; // "Assembly Namespace.TypeName"
        if (string.IsNullOrEmpty(full)) return "(null)";

        string[] parts = full.Split(' ');
        string typePath = parts.Length == 2 ? parts[1] : full;
        int lastDot = typePath.LastIndexOf('.');
        string name = lastDot >= 0 ? typePath.Substring(lastDot + 1) : typePath;
        return ObjectNames.NicifyVariableName(name);
    }

    private static Type GetManagedReferenceFieldType(SerializedProperty property)
        => GetTypeFromTypename(property.managedReferenceFieldTypename);

    private static Type GetTypeFromTypename(string typename)
    {
        if (string.IsNullOrEmpty(typename)) return null;

        string[] parts = typename.Split(' ');
        if (parts.Length != 2) return null;

        string assemblyName = parts[0];
        string typeName = parts[1];
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);
        return assembly?.GetType(typeName);
    }

    private static Type[] GetDerivedTypes(Type baseType)
    {
        if (baseType == null) return Array.Empty<Type>();

        string key = baseType.FullName ?? baseType.Name;
        if (typeCache.TryGetValue(key, out Type[] cached)) return cached;

        Type[] result = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .OrderBy(t => t.Name)
            .ToArray();

        typeCache[key] = result;
        return result;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
    }
}