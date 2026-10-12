using UnityEngine;

/// <summary>
/// Apply alongside <c>[SerializeReference]</c> to give a polymorphic serialized-reference
/// field (or list element) an Inspector dropdown for picking the concrete subclass.
///
/// Usage:
/// <code>
/// [SerializeReference, SubclassSelector]
/// private List&lt;Ability&gt; abilities = new List&lt;Ability&gt;();
/// </code>
///
/// This is a runtime attribute (it lives outside any Editor folder); the matching
/// <c>SubclassSelectorDrawer</c> provides the editor-only UI.
/// </summary>
public class SubclassSelectorAttribute : PropertyAttribute { }