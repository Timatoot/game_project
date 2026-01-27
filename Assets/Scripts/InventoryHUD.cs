using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventoryHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform listRoot;
    [SerializeField] private TMP_Text rowTemplate;

    [Header("Display")]
    [SerializeField] private bool prettifyKeyIDs = true;

    private readonly Dictionary<string, TMP_Text> rows = new Dictionary<string, TMP_Text>();

    private void Awake()
    {
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();

        if (listRoot == null)
            Debug.LogError("InventoryHUD: listRoot is not assigned.", this);

        if (rowTemplate == null)
            Debug.LogError("InventoryHUD: rowTemplate is not assigned.", this);

        // We keep a template object disabled and clone it for each key.
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (inventory == null) return;

        inventory.KeyAdded += HandleKeyAdded;
        inventory.KeysCleared += RebuildAll;
    }

    private void Start()
    {
        RebuildAll();
    }

    private void OnDisable()
    {
        if (inventory == null) return;

        inventory.KeyAdded -= HandleKeyAdded;
        inventory.KeysCleared -= RebuildAll;
    }

    private void RebuildAll()
    {
        foreach (var kv in rows)
        {
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        }
        rows.Clear();

        if (inventory == null || listRoot == null || rowTemplate == null) return;

        foreach (var key in inventory.CollectedKeys)
        {
            CreateRow(key);
        }
    }

    private void HandleKeyAdded(string keyID)
    {
        if (rows.ContainsKey(keyID)) return;
        CreateRow(keyID);
    }

    private void CreateRow(string keyID)
    {
        if (rowTemplate == null || listRoot == null) return;

        var row = Instantiate(rowTemplate, listRoot);
        row.gameObject.SetActive(true);
        row.text = prettifyKeyIDs ? Prettify(keyID) : keyID;

        rows[keyID] = row;
    }

    private string Prettify(string id)
    {
        // "Level1_MainKey" -> "Level 1 Main Key"
        var s = id.Replace('_', ' ');

        var sb = new System.Text.StringBuilder(s.Length * 2);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (i > 0 && char.IsUpper(c) && char.IsLower(s[i - 1])) sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
