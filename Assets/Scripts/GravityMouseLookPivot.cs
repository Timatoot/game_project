using UnityEngine;

public class GravityMouseLookPivot : MonoBehaviour
{
    public PlayerGravityController player;   // drag Player here
    public Transform playerBody;             // drag Player transform here
    public Transform cameraPivot;            // drag CameraPivot here

    public float sensitivity = 2f;
    public float pitchClamp = 85f;

    private float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody == null) playerBody = player != null ? player.transform : transform.root;
        if (player == null && playerBody != null) player = playerBody.GetComponent<PlayerGravityController>();
        if (cameraPivot == null)
        {
            // If you attach this script to the Camera, its parent should be the pivot.
            cameraPivot = transform.parent != null ? transform.parent : transform;
        }
    }

    void Update()
    {
        if (player == null || playerBody == null || cameraPivot == null) return;

        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;

        Vector3 up = player.GetPlayerUp();

        // Yaw: rotate the player around current "up"
        playerBody.rotation = Quaternion.AngleAxis(mx, up) * playerBody.rotation;

        // Pitch: rotate the pivot locally
        pitch -= my;
        pitch = Mathf.Clamp(pitch, -pitchClamp, pitchClamp);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
