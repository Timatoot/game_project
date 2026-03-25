using UnityEngine;

public class GravityCameraController : MonoBehaviour
{
    public enum ViewMode { FirstPerson, ThirdPerson }

    [Header("Mode")]
    public ViewMode mode = ViewMode.FirstPerson;

    [Header("References")]
    public PlayerGravityController controller;
    public Transform playerBody;
    public Transform cameraPivot;
    public GravityInput input;

    [Header("Look")]
    public float sensitivity = 0.08f;
    public bool invertY = false;
    public bool lockCursor = true;

    [Header("Pitch Clamp")]
    public float thirdPersonPitchMin = -70f;
    public float thirdPersonPitchMax = 70f;
    public float firstPersonPitchMin = -80f;
    public float firstPersonPitchMax = 80f;

    [Header("Third Person Orbit")]
    public float distance = 4.5f;
    public float height = 1.6f;

    [Header("Third Person Shoulder")]
    public float shoulderOffset = 0.55f;
    public float aimDistance = 50f;

    [Header("Camera Collision")]
    public float collisionRadius = 0.25f;
    public LayerMask collisionMask = ~0;

    [Header("Landing snap")]
    public float landingSnapSpeed = 6f;
    public float landingSnapDuration = 0.35f;

    private float fpPitch;
    private Vector3 tpOffset;
    private bool tpOffsetInit;

    private Vector3 lastUp;
    private bool wasGrounded;

    private float tpLandSnapTimer;
    private float fpLandSnapTimer;
    private Quaternion fpLandSnapTarget;

    void Awake()
    {
        EnsureReferences();
        ApplyCursorLock();

        if (controller != null)
        {
            lastUp = controller.GetPlayerUp();
            wasGrounded = controller.IsGrounded;
        }

        ApplyModeSettings();
    }

    void Update()
    {
        if (controller == null || playerBody == null || input == null) return;

        if (input.ToggleViewPressed)
            SetMode(mode == ViewMode.FirstPerson ? ViewMode.ThirdPerson : ViewMode.FirstPerson);

        Vector2 look = input.Look;
        float mx = look.x * sensitivity;
        float my = look.y * sensitivity * (invertY ? -1f : 1f);

        if (mode == ViewMode.FirstPerson) UpdateFirstPerson(mx, my);
        else UpdateThirdPerson(mx, my);
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

    private void SetMode(ViewMode newMode)
    {
        if (mode == newMode) return;

        mode = newMode;
        fpLandSnapTimer = 0f;
        tpLandSnapTimer = 0f;

        ApplyModeSettings();
    }

    private void ApplyModeSettings()
    {
        if (controller == null) return;

        if (mode == ViewMode.FirstPerson)
        {
            controller.moveReference = PlayerGravityController.MoveReference.Camera;
            controller.faceCameraWhenMoving = false;
        }
        else
        {
            controller.moveReference = PlayerGravityController.MoveReference.Camera;
            controller.faceCameraWhenMoving = true;
        }
    }

    private void EnsureReferences()
    {
        if (playerBody == null) playerBody = transform.root;
        if (controller == null && playerBody != null) controller = playerBody.GetComponent<PlayerGravityController>();
        if (cameraPivot == null) cameraPivot = transform.parent;
        if (input == null && playerBody != null) input = playerBody.GetComponent<GravityInput>();
    }

    private void ApplyCursorLock()
    {
        if (!lockCursor) return;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UpdateFirstPerson(float mx, float my)
    {
        Vector3 up = controller.GetPlayerUp();
        bool grounded = controller.IsGrounded;

        // Keep camera stable if gravity changes while airborne
        if (!grounded && Vector3.Angle(lastUp, up) > 0.5f && cameraPivot != null)
        {
            Quaternion r = Quaternion.FromToRotation(lastUp, up);
            cameraPivot.rotation = Quaternion.Inverse(r) * cameraPivot.rotation;
            lastUp = up;
        }

        if (grounded)
            playerBody.rotation = Quaternion.AngleAxis(mx, up) * playerBody.rotation;
        else if (cameraPivot != null)
            cameraPivot.rotation = Quaternion.AngleAxis(mx, up) * cameraPivot.rotation;

        // Pitch
        fpPitch -= my;
        fpPitch = Mathf.Clamp(fpPitch, firstPersonPitchMin, firstPersonPitchMax);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(fpPitch, 0f, 0f);

        // Landing snap
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

    private void UpdateThirdPerson(float mx, float my)
    {
        Vector3 up = controller.GetPlayerUp();

        if (!tpOffsetInit)
            return;

        tpOffset = Quaternion.AngleAxis(mx, up) * tpOffset;

        Vector3 right = Vector3.Cross(up, tpOffset).normalized;
        if (right.sqrMagnitude > 0.0001f)
            tpOffset = Quaternion.AngleAxis(my, right) * tpOffset;

        tpOffset = ClampOffsetPitch(tpOffset, up, thirdPersonPitchMin, thirdPersonPitchMax, distance);
    }

    private void LateThirdPerson()
    {
        Vector3 up = controller.GetPlayerUp();
        bool grounded = controller.IsGrounded;

        Vector3 pivot = playerBody.position + up * height;

        if (!tpOffsetInit)
        {
            tpOffset = transform.position - pivot;
            if (tpOffset.sqrMagnitude < 0.001f)
                tpOffset = -Vector3.ProjectOnPlane(playerBody.forward, up).normalized * distance;

            tpOffset = tpOffset.normalized * distance;
            tpOffset = ClampOffsetPitch(tpOffset, up, thirdPersonPitchMin, thirdPersonPitchMax, distance);
            tpOffsetInit = true;
        }

        if (grounded && !wasGrounded)
            tpLandSnapTimer = landingSnapDuration;

        if (tpLandSnapTimer > 0f)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(playerBody.forward, up).normalized;
            if (fwd.sqrMagnitude < 0.0001f)
                fwd = Vector3.ProjectOnPlane(playerBody.right, up).normalized;

            Vector3 dir = tpOffset.normalized;
            float elevation = Mathf.Asin(Mathf.Clamp(Vector3.Dot(dir, up), -1f, 1f)) * Mathf.Rad2Deg;
            elevation = Mathf.Clamp(elevation, thirdPersonPitchMin, thirdPersonPitchMax);

            Vector3 behind = (-fwd).normalized;

            Vector3 snappedDir =
                (behind * Mathf.Cos(elevation * Mathf.Deg2Rad) +
                 up * Mathf.Sin(elevation * Mathf.Deg2Rad)).normalized;

            Vector3 targetOffset = snappedDir * distance;

            tpOffset = Vector3.Slerp(tpOffset.normalized, targetOffset.normalized, landingSnapSpeed * Time.deltaTime) * distance;
            tpLandSnapTimer -= Time.deltaTime;
        }

        // Orbit base position
        Vector3 basePos = pivot + tpOffset;

        // Shoulder shift
        Vector3 orbitForward = (-tpOffset).normalized;
        Vector3 orbitRight = Vector3.Cross(up, orbitForward).normalized;
        Vector3 desiredPos = basePos + orbitRight * shoulderOffset;

        // Collision
        Vector3 toCam = desiredPos - pivot;
        float dist = toCam.magnitude;
        if (dist > 0.001f)
        {
            Vector3 dir = toCam / dist;
            if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
                desiredPos = pivot + dir * Mathf.Max(hit.distance - 0.05f, 0.3f);
        }

        transform.position = desiredPos;

        Vector3 aimPoint = pivot + orbitForward * aimDistance;
        transform.rotation = Quaternion.LookRotation((aimPoint - transform.position).normalized, up);

        wasGrounded = grounded;
        lastUp = up;
    }

    private static Vector3 ClampOffsetPitch(Vector3 offset, Vector3 up, float minPitchDeg, float maxPitchDeg, float length)
    {
        Vector3 dir = offset.normalized;

        float elevation = Mathf.Asin(Mathf.Clamp(Vector3.Dot(dir, up), -1f, 1f)) * Mathf.Rad2Deg;
        elevation = Mathf.Clamp(elevation, minPitchDeg, maxPitchDeg);

        Vector3 onPlane = Vector3.ProjectOnPlane(dir, up).normalized;
        if (onPlane.sqrMagnitude < 0.0001f)
            onPlane = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;

        Vector3 clampedDir =
            (onPlane * Mathf.Cos(elevation * Mathf.Deg2Rad) +
             up * Mathf.Sin(elevation * Mathf.Deg2Rad)).normalized;

        return clampedDir * length;
    }

    public void ForceRespawnCamera(Vector3 newUp)
    {
        lastUp = newUp.normalized;
        wasGrounded = false;

        fpLandSnapTimer = 0f;
        tpLandSnapTimer = 0f;

        fpPitch = 0f;

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.identity;

        tpOffsetInit = false;
    }
}
