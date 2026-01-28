using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Key Icon Library")]
public class KeyIconLibrary : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string keyID;
        public Sprite icon;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, Sprite> dict;

    private void OnEnable() => Build();

    private void Build()
    {
        dict = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.keyID) || e.icon == null) continue;
            dict[e.keyID.Trim()] = e.icon;
        }
    }

    public Sprite GetIcon(string keyID)
    {
        if (dict == null) Build();
        if (string.IsNullOrWhiteSpace(keyID)) return null;

        dict.TryGetValue(keyID.Trim(), out var s);
        return s;
    }
}
