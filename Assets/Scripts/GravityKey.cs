using UnityEngine;

public class GravityKey : MonoBehaviour
{
    [Header("Settings")]
    public string keyID = "Level1_MainKey"; // Useful if you have multiple keys
    public float rotationSpeed = 50f;

    [Header("Floating Animation")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;
    private Vector3 startPos;

    void Start() {
        startPos = transform.localPosition;
    }

    private void Update() {
        // Rotation logic
        transform.Rotate(new Vector3(15, 30, 45) * Time.deltaTime);

        // Floating logic using a Sine wave
        float newY = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = startPos + new Vector3(0, newY, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that touched the ball is the player
        // This assumes your Player object has the PlayerGravityController script
        if (other.GetComponentInParent<PlayerGravityController>() != null)
        {
            Collect(other.gameObject);
        }
    }

    private void Collect(GameObject player)
    {
        Debug.Log("key grabbed");

        // Find a "Key Inventory" on the player and register this key
        PlayerInventory inv = player.GetComponentInParent<PlayerInventory>();
        if (inv != null)
        {
            inv.AddKey(keyID);
        }

        // Destroy the blue ball in the scene
        Destroy(gameObject);
    }
}