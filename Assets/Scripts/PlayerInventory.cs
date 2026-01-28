using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    private readonly HashSet<string> collectedKeys = new HashSet<string>();

    public event Action<string> KeyAdded;
    public event Action KeysCleared;

    public IReadOnlyCollection<string> CollectedKeys => collectedKeys;

    public void AddKey(string keyID)
    {
        if (string.IsNullOrWhiteSpace(keyID)) return;

        if (collectedKeys.Add(keyID))
        {
            KeyAdded?.Invoke(keyID);
        }
    }

    public bool HasKey(string keyID) => collectedKeys.Contains(keyID);

    public void ClearKeys()
    {
        if (collectedKeys.Count == 0) return;
        collectedKeys.Clear();
        KeysCleared?.Invoke();
    }
}
