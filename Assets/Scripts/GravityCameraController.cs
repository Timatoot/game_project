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

    [Header("Landing snap")]
    public float landingSnapSpeed = 6f;      // higher = faster snap when you land
    public float landingSnapDuration = 0.35f; // seconds to keep snapping after landing

    // FPS pitch
    float fpPitch;

    // Third person state: we store a WORLD offset from pivot to camera
    private Vector3 tpOffset;
    private bool tpOffsetInit;

    // Track gravity-up changes
    private Vector3 lastUp;

    // Landing detection + snap timers
    private bool wasGrounded;
    private float tpLandSnapTimer;

    private float fpLandSnapTimer;
    private Quaternion fpLandSnapTarget;

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
            wasGrounded = controller.IsGrounded;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
            mode = (mode == ViewMode.FirstPerson) ? ViewMode.ThirdPerson : ViewMode.FirstPerson;

        if (controller == null || playerBody == null) return;

        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;

        if (mode == ViewMode.FirstPerson)
            UpdateFirstPerson(mx, my);
        else
            UpdateThirdPerson(mx, my);
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

        LateThirdPerson();
    }

    // -------------------------
    // FIRST PERSON (free-look while airborne, snap on landing)
    // -------------------------
    void UpdateFirstPerson(float mx, float my)
    {
        Vector3 up = controller.GetPlayerUp();
        bool grounded = controller.IsGrounded;

        // If gravity-up changed while airborne, cancel the parent’s “reorientation” effect on the view.
        if (!grounded && Vector3.Angle(lastUp, up) > 0.5f && cameraPivot != null)
        {
            Quaternion R = Quaternion.FromToRotation(lastUp, up);
            cameraPivot.rotation = Quaternion.Inverse(R) * cameraPivot.rotation;
            lastUp = up;
        }

        if (grounded)
        {
            // Normal FPS: yaw rotates player
            playerBody.rotation = Quaternion.AngleAxis(mx, up) * playerBody.rotation;
        }
        else
        {
            // Airborne FPS: yaw is free-look (do NOT rotate player)
            if (cameraPivot != null)
                cameraPivot.rotation = Quaternion.AngleAxis(mx, up) * cameraPivot.rotation;
        }

        // Pitch always applies to the pivot locally
        fpPitch -= my;
        fpPitch = Mathf.Clamp(fpPitch, -85f, 85f);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(fpPitch, 0f, 0f);

        controller.moveReference = PlayerGravityController.MoveReference.Camera;
        controller.faceCameraWhenMoving = false;

        // Landing snap: rotate player to match camera forward when we touch down
        if (grounded && !wasGrounded)
        {
            Vector3 camF = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            if (camF.sqrMagnitude > 0.0001f)
            {
                fpLandSnapTarget = Quaternion.LookRotation(camF, up);
                fpLandSnapTimer = landingSnapDuration;
            }
        }

        if (fpLandSnapTimer > 0f)
        {
            playerBody.rotation = Quaternion.Slerp(playerBody.rotation, fpLandSnapTarget, landingSnapSpeed * Time.deltaTime);
            fpLandSnapTimer -= Time.deltaTime;
        }

        wasGrounded = grounded;
    }

    // -------------------------
    // THIRD PERSON (free cam while airborne, snap on landing)
    // -------------------------
    void UpdateThirdPerson(float mx, float my)
    {
        Vector3 up = controller.GetPlayerUp();

        // We rotate the offset directly by mouse input (free orbit)
        if (!tpOffsetInit)
        {
            // tpOffset will initialize in LateThirdPerson once we know pivot for sure
            controller.moveReference = PlayerGravityController.MoveReference.Camera;
            controller.faceCameraWhenMoving = true;
            return;
        }

        // Yaw: rotate offset around gravity-up
        tpOffset = Quaternion.AngleAxis(mx, up) * tpOffset;

        // Pitch: rotate offset around the current right axis
        Vector3 right = Vector3.Cross(up, tpOffset).normalized;
        if (right.sqrMagnitude > 0.0001f)
            tpOffset = Quaternion.AngleAxis(my, right) * tpOffset;

        // Clamp pitch to [-70, 70]
        tpOffset = ClampOffsetPitch(tpOffset, up, thirdPersonPitchMin, thirdPersonPitchMax, distance);

        controller.moveReference = PlayerGravityController.MoveReference.Camera;
        controller.faceCameraWhenMoving = true;
    }

    void LateThirdPerson()
    {
        Vector3 up = controller.GetPlayerUp();
        bool grounded = controller.IsGrounded;

        Vector3 pivot = playerBody.position + up * height;

        // Init offset once (so camera starts where it already is)
        if (!tpOffsetInit)
        {
            tpOffset = transform.position - pivot;
            if (tpOffset.sqrMagnitude < 0.001f)
                tpOffset = -Vector3.ProjectOnPlane(playerBody.forward, up).normalized * distance;

            if (tpOffset.sqrMagnitude < 0.001f)
                tpOffset = -playerBody.forward * distance;

            tpOffset = tpOffset.normalized * distance;
            tpOffset = ClampOffsetPitch(tpOffset, up, thirdPersonPitchMin, thirdPersonPitchMax, distance);
            tpOffsetInit = true;
        }

        // Detect landing and begin snap
        if (grounded && !wasGrounded)
            tpLandSnapTimer = landingSnapDuration;

        // While airborne: do NOTHING special (free cam, no reorientation)
        // On landing: smoothly snap camera to behind player (in the new gravity frame)
        if (tpLandSnapTimer > 0f)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(playerBody.forward, up).normalized;
            if (fwd.sqrMagnitude < 0.0001f)
                fwd = Vector3.ProjectOnPlane(playerBody.right, up).normalized;

            // Preserve current elevation (pitch) while snapping yaw behind the player
            Vector3 dir = tpOffset.normalized;
            float elevation = Mathf.Asin(Mathf.Clamp(Vector3.Dot(dir, up), -1f, 1f)) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, thirdPersonPitchMin, thirdPersonPitchMax);

            Vector3 behindOnPlane = (-fwd).normalized;
            Vector3 right = Vector3.Cross(up, behindOnPlane).normalized;

            Vector3 snappedDir =
                (behindOnPlane * Mathf.Cos(elevation * Mathf.Deg2Rad) +
                 up * Mathf.Sin(elevation * Mathf.Deg2Rad)).normalized;

            Vector3 targetOffset = snappedDir * distance;

            tpOffset = Vector3.Slerp(tpOffset.normalized, targetOffset.normalized, landingSnapSpeed * Time.deltaTime) * distance;
            tpLandSnapTimer -= Time.deltaTime;
        }

        // Apply camera position from offset
        Vector3 desiredPos = pivot + tpOffset;

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

        // Look at pivot with correct up
        Vector3 lookDir = (pivot - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(lookDir, up);

        // Keep offset consistent after collision adjustment (prevents popping)
        tpOffset = (transform.position - pivot);
        if (tpOffset.sqrMagnitude > 0.001f)
            tpOffset = tpOffset.normalized * distance;

        // If gravity-up changed, don’t force the camera to rotate in-air — just update lastUp.
        if (Vector3.Angle(lastUp, up) > 0.5f)
            lastUp = up;

        wasGrounded = grounded;
    }

    static Vector3 ClampOffsetPitch(Vector3 offset, Vector3 up, float minPitchDeg, float maxPitchDeg, float length)
    {
        Vector3 dir = offset.normalized;

        float elevation = Mathf.Asin(Mathf.Clamp(Vector3.Dot(dir, up), -1f, 1f)) * Mathf.Rad2Deg;
        elevation = Mathf.Clamp(elevation, minPitchDeg, maxPitchDeg);

        Vector3 onPlane = Vector3.ProjectOnPlane(dir, up).normalized;
        if (onPlane.sqrMagnitude < 0.0001f)
        {
            // Fallback: pick any stable direction on plane
            onPlane = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
            if (onPlane.sqrMagnitude < 0.0001f)
                onPlane = Vector3.ProjectOnPlane(Vector3.right, up).normalized;
        }

        Vector3 clampedDir =
            (onPlane * Mathf.Cos(elevation * Mathf.Deg2Rad) +
             up * Mathf.Sin(elevation * Mathf.Deg2Rad)).normalized;

        return clampedDir * length;
    }
}
