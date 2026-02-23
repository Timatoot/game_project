/*
basics for every key item. later possibly add a option to change the model of the object
*/

using UnityEngine;

public class KeyItem : MonoBehaviour
{
    [Header("Settings")]
    public string keyID;
    public float rotationSpeed = 50f;
    private bool pickedUp;

    [Header("Floating Animation")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.2f;

    private Vector3 startWorldPos;
    private Vector3 bobAxis;

    private void Start()
    {
        startWorldPos = transform.position;
        bobAxis = transform.up; // lock the axis at spawn time
    }

    private void Update()
    {
        transform.Rotate(new Vector3(15f, 30f, 45f) * (rotationSpeed / 50f) * Time.deltaTime, Space.Self);

        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startWorldPos + bobAxis * offset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;

        if (other.GetComponentInParent<PlayerGravityController>() == null) return;

        pickedUp = true;

        var inv = other.GetComponentInParent<PlayerInventory>();
        if (inv != null) inv.AddKey(keyID);

        Debug.Log($"key picked up: {keyID} by {other.name}", this);

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        Destroy(gameObject);
    }
}
