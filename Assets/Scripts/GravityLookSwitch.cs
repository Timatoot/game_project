using UnityEngine;

public class GravityLookSwitch : MonoBehaviour
{
    public PlayerGravityController player;
    public float maxDistance = 100f;
    public KeyCode switchKey = KeyCode.E;

    void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                // We want player's UP to become the hit normal
                // That makes gravity pull toward the surface
                player.SetPlayerUp(hit.normal);

                // Optional: damp existing velocity so you don't slingshot
                player.GetComponent<Rigidbody>().linearVelocity *= 0.5f;
            }
        }
    }
}
