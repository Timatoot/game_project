using UnityEngine;

public class GravityMouseLook : MonoBehaviour
{
    public PlayerGravityController player;
    public Transform playerBody;
    public float sensitivity = 2f;
    public float pitchClamp = 85f;

    float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody == null) playerBody = transform.parent;
        if (player == null && playerBody != null) player = playerBody.GetComponent<PlayerGravityController>();
    }

    void Update()
    {
        if (player == null || playerBody == null) return;

        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;

        Vector3 up = player.GetPlayerUp();

        // Yaw (left/right)
        playerBody.rotation = Quaternion.AngleAxis(mx, up) * playerBody.rotation;

        // Pitch (up/down)
        pitch -= my;
        pitch = Mathf.Clamp(pitch, -pitchClamp, pitchClamp);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
