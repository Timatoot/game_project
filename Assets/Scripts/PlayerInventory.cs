using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    // A list of IDs for keys the player has collected
    private HashSet<string> collectedKeys = new HashSet<string>();

    public void AddKey(string keyID)
    {
        if (!collectedKeys.Contains(keyID))
        {
            collectedKeys.Add(keyID);
        }
    }

    public bool HasKey(string keyID)
    {
        return collectedKeys.Contains(keyID);
    }
}