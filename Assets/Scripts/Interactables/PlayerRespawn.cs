using UnityEngine;

[DisallowMultipleComponent]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn")]
    public Transform respawnPoint;

    [Header("References")]
    public Rigidbody playerRigidbody;
    public PlayerGravityController gravityController;
    public GravityPullGun gravityPullGun;
    public GravityCameraController gravityCameraController;

    public void Respawn()
    {
        if (respawnPoint == null)
        {
            Debug.LogWarning("PlayerRespawn: No respawn point assigned.");
            return;
        }

        if (gravityPullGun != null)
            gravityPullGun.CancelPull();

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        transform.position = respawnPoint.position;

        if (gravityController != null)
        {
            gravityController.ForceRespawnState(
                respawnPoint.up,
                respawnPoint.forward,
                playerRigidbody
            );
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(respawnPoint.forward, respawnPoint.up);
        }

        if (gravityCameraController != null)
            gravityCameraController.ForceRespawnCamera(respawnPoint.up);

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        Debug.Log("Player respawned cleanly.");
    }

    public void SetRespawnPoint(Transform newPoint)
    {
        if (newPoint != null)
            respawnPoint = newPoint;
    }
}