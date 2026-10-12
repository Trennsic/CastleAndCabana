using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;

[System.Serializable]
public class DebugUtility
{
    [SerializeField] private bool isDebugging = false;
    public bool IsDebugging => isDebugging;

    [System.Serializable]
    public struct WatchEntry
    {
        public string Label;
        public string Value;
    }

    [SerializeField][MMReadOnly] private List<WatchEntry> watches = new List<WatchEntry>();

    public DebugUtility() { }
    public DebugUtility(bool isDebugging) { this.isDebugging = isDebugging; }

    /// <summary>
    /// Sets or updates a labeled value shown in the Inspector under this debug block.
    /// Safe to call every frame.
    /// </summary>
    public void SetWatch(string label, object value)
    {
        string v = value?.ToString() ?? "null";
        for (int i = 0; i < watches.Count; i++)
        {
            if (watches[i].Label == label)
            {
                var e = watches[i];
                e.Value = v;
                watches[i] = e;
                return;
            }
        }
        watches.Add(new WatchEntry { Label = label, Value = v });
    }

    public void ClearWatch(string label)
    {
        for (int i = 0; i < watches.Count; i++)
            if (watches[i].Label == label) { watches.RemoveAt(i); return; }
    }

    public void ClearAllWatches() => watches.Clear();

    public void Log(string message) { if (isDebugging) Debug.Log(message); }
    public void LogWarning(string message) { if (isDebugging) Debug.LogWarning(message); }
    public void LogError(string message) { if (isDebugging) Debug.LogError(message); }
}