/*
matches a sprite to a key item
*/

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Key Data Lib")]
public class KeyItemIconLib : ScriptableObject
{
    [System.Serializable]
    public struct KeyEntry
    {
        public string keyID;
        public Sprite icon;
    }

    public List<KeyEntry> entries;

    public Sprite GetSprite(string id)
    {
        foreach (var entry in entries)
        {
            if (entry.keyID == id) return entry.icon;
        }
        return null;
    }
}