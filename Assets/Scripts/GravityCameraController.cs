using UnityEngine;

public class GravityCameraController : MonoBehaviour
{
    public enum ViewMode { FirstPerson, ThirdPerson }

    [Header("Mode")]
    public ViewMode mode = ViewMode.FirstPerson;

    [Header("References")]
    public PlayerGravityController controller;   // drag Player
    public Transform playerBody;                 // drag Player
    public Transform cameraPivot;                // drag CameraPivot (FPS pitch)

    [Header("Mouse")]
    public float sensitivity = 2f;
    public bool lockCursor = true;

    [Header("Pitch Clamp (3rd person)")]
    public float thirdPersonPitchMin = -70f;
    public float thirdPersonPitchMax = 70f;

    [Header("Third Person Orbit")]
    public float distance = 4.5f;
    public float height = 1.6f;

    [Header("Camera Collision")]
    public float collisionRadius = 0.25f;
    public LayerMask collisionMask = ~0;

    float fpPitch;
    float tpYaw;
    float tpPitch;

    // This makes third-person orbit independent of player rotation.
    Vector3 orbitForwardRef;
    Vector3 lastUp;

    void Start()
    {
        if (playerBody == null) playerBody = transform.root;
        if (controller == null && playerBody != null) controller = playerBody.GetComponent<PlayerGravityController>();
        if (cameraPivot == null) cameraPivot = transform.parent;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (controller != null)
        {
            lastUp = controller.GetPlayerUp();
            orbitForwardRef = Vector3.ProjectOnPlane(playerBody.forward, lastUp).normalized;
            if (orbitForwardRef.sqrMagnitude < 0.0001f)
                orbitForwardRef = Vector3.ProjectOnPlane(playerBody.right, lastUp).normalized;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            mode = (mode == ViewMode.FirstPerson) ? ViewMode.ThirdPerson : ViewMode.FirstPerson;
        }

        if (controller == null || playerBody == null) return;

        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;

        Vector3 up = controller.GetPlayerUp();

        if (mode == ViewMode.FirstPerson)
        {
            // FPS: yaw rotates player
            playerBody.rotation = Quaternion.AngleAxis(mx, up) * playerBody.rotation;

            // FPS: pitch rotates pivot
            fpPitch -= my;
            fpPitch = Mathf.Clamp(fpPitch, -85f, 85f);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(fpPitch, 0f, 0f);

            // FPS movement stays camera-relative
            controller.moveReference = PlayerGravityController.MoveReference.Camera;
            controller.faceCameraWhenMoving = false; // not needed in FPS
        }
        else
        {
            // 3rd person: camera orbits only. DO NOT rotate player here.
            tpYaw += mx;
            tpPitch -= my;
            tpPitch = Mathf.Clamp(tpPitch, thirdPersonPitchMin, thirdPersonPitchMax);

            // 3rd person: movement is camera-relative, and player faces camera only when moving
            controller.moveReference = PlayerGravityController.MoveReference.Camera;
            controller.faceCameraWhenMoving = true;
        }
    }

    void LateUpdate()
    {
        if (controller == null || playerBody == null) return;

        if (mode == ViewMode.FirstPerson)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            return;
        }

        Vector3 up = controller.GetPlayerUp();

        // If gravity-up changed, re-project the orbit reference so controls stay sane.
        if (Vector3.Angle(lastUp, up) > 0.5f)
        {
            orbitForwardRef = Vector3.ProjectOnPlane(orbitForwardRef, up).normalized;
            if (orbitForwardRef.sqrMagnitude < 0.0001f)
                orbitForwardRef = Vector3.ProjectOnPlane(playerBody.forward, up).normalized;

            lastUp = up;
        }

        Vector3 pivot = playerBody.position + up * height;

        // Build orbit direction from stored reference (not playerBody.forward).
        Vector3 rightRef = Vector3.Cross(up, orbitForwardRef).normalized;

        Quaternion yawRot = Quaternion.AngleAxis(tpYaw, up);
        Vector3 rightAfterYaw = yawRot * rightRef;

        Quaternion pitchRot = Quaternion.AngleAxis(tpPitch, rightAfterYaw);

        Vector3 baseDir = -orbitForwardRef; // yaw=0 means "behind" the initial ref
        Vector3 camDir = (pitchRot * (yawRot * baseDir)).normalized;

        Vector3 desiredPos = pivot + camDir * distance;

        // Collision push-in
        Vector3 toCam = desiredPos - pivot;
        float dist = toCam.magnitude;
        if (dist > 0.001f)
        {
            Vector3 dir = toCam / dist;
            if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                desiredPos = pivot + dir * Mathf.Max(hit.distance - 0.05f, 0.3f);
            }
        }

        transform.position = desiredPos;

        // Always look at pivot
        Vector3 lookDir = (pivot - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(lookDir, up);
    }
}
