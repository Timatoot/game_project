using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryHUD_Icons : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private KeyIconLibrary iconLibrary;

    [SerializeField] private Transform iconRoot;     // your KeyGrid
    [SerializeField] private Image iconTemplate;     // your KeyIconTemplate (disabled)

    [Header("Fallback")]
    [SerializeField] private Sprite missingIcon;

    private readonly Dictionary<string, Image> spawned = new Dictionary<string, Image>();

    private void Awake()
    {
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
        if (iconTemplate != null) iconTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (inventory == null) return;
        inventory.KeyAdded += OnKeyAdded;
        inventory.KeysCleared += Rebuild;
    }

    private void Start() => Rebuild();

    private void OnDisable()
    {
        if (inventory == null) return;
        inventory.KeyAdded -= OnKeyAdded;
        inventory.KeysCleared -= Rebuild;
    }

    private void Rebuild()
    {
        foreach (var kv in spawned)
        {
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        }
        spawned.Clear();

        if (inventory == null) return;
        foreach (var id in inventory.CollectedKeys)
        {
            CreateIcon(id);
        }
    }

    private void OnKeyAdded(string keyID)
    {
        if (spawned.ContainsKey(keyID)) return;
        CreateIcon(keyID);
    }

    private void CreateIcon(string keyID)
    {
        if (iconRoot == null || iconTemplate == null) return;

        var img = Instantiate(iconTemplate, iconRoot);
        img.gameObject.SetActive(true);

        Sprite icon = iconLibrary != null ? iconLibrary.GetIcon(keyID) : null;
        img.sprite = icon != null ? icon : missingIcon;
        img.preserveAspect = true;

        spawned[keyID] = img;
    }
}
