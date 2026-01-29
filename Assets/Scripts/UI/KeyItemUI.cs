/*
the ui manager for key items.
*/

using UnityEngine;
using UnityEngine.UI;

public class KeyItemUI : MonoBehaviour
{
    public PlayerInventory playerInventory; // Drag player here
    public KeyItemIconLib library;          // Drag library asset here
    public Image iconTemplate;              // Drag your "Image" child here

    private void OnEnable()
    {
        // Listen for the event from your PlayerInventory.cs
        playerInventory.KeyAdded += AddIconToUI;
    }

    private void OnDisable()
    {
        playerInventory.KeyAdded -= AddIconToUI;
    }

    private void AddIconToUI(string keyID)
    {
        // 1. Get the sprite from our new library
        Sprite keySprite = library.GetSprite(keyID);

        if (keySprite != null)
        {
            // 2. Clone the template
            Image newIcon = Instantiate(iconTemplate, transform);

            // 3. Set the image and turn it on
            newIcon.sprite = keySprite;
            newIcon.gameObject.SetActive(true);
        }
    }
}