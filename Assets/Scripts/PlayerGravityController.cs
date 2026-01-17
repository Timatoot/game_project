using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerGravityController : MonoBehaviour
{
    public enum MoveReference { Camera, Player }

    [Header("Movement Reference")]
    public MoveReference moveReference = MoveReference.Camera;

    [Header("Turn Toward Movement (3rd person feel)")]
    public bool rotateTowardMoveDirection = false;
    public float turnSpeed = 10f;

    [Header("Gravity")]
    public float gravityStrength = 20f;
    public float alignToGravitySpeed = 12f;

    [Header("Auto Snap Gravity")]
    public bool snapGravityOnImpact = true;
    public float minSnapSpeed = 1.0f;      // must be moving at least this fast
    public float snapSlerpSpeed = 25f;     // how fast gravity "up" turns to surface normal

    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.6f;
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Air Control")]
    public float airControl = 0.5f;

    [Header("Jump")]
    public float jumpSpeed = 7f;
    public float groundedDot = 0.55f; // how aligned contact normal must be with playerUp (0.55 ~ 56 degrees)

    [Header("Third Person Facing")]
    public bool faceCameraWhenMoving = false;
    public float faceTurnSpeed = 12f;
    public float moveDeadzone = 0.05f;

    [Header("Idle Rotation Lock")]
    public bool lockYawWhenIdle = true;

    public Transform cameraTransform;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector3 playerUp = Vector3.up;

    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    private bool hasIdleLock;
    private Quaternion idleLockRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void FixedUpdate()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        bool idle = (x * x + z * z) < (moveDeadzone * moveDeadzone);

        if (lockYawWhenIdle && faceCameraWhenMoving && idle)
        {
            if (!hasIdleLock)
            {
                hasIdleLock = true;
                idleLockRotation = transform.rotation;
            }
        }
        else
        {
            hasIdleLock = false;
        }

        // Reset each physics step; collisions will set grounded
        isGrounded = false;
        groundNormal = playerUp;

        ApplyCustomGravity();
        AlignToGravity();
        Move();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // Remove downward velocity so jump always pops
            Vector3 v = rb.linearVelocity;
            float down = Vector3.Dot(v, -playerUp);
            if (down > 0f) v += playerUp * down;

            rb.linearVelocity = v;
            rb.AddForce(playerUp * jumpSpeed, ForceMode.VelocityChange);
        }
    }

    void ApplyCustomGravity()
    {
        rb.AddForce(-playerUp * gravityStrength, ForceMode.Acceleration);
    }

    void AlignToGravity()
    {
        // Choose a forward vector to preserve.
        Vector3 forwardToKeep;

        if (hasIdleLock)
        {
            // Use locked rotation forward so camera movement can't influence it.
            forwardToKeep = Vector3.ProjectOnPlane(idleLockRotation * Vector3.forward, playerUp);
        }
        else
        {
            forwardToKeep = Vector3.ProjectOnPlane(transform.forward, playerUp);
        }

        if (forwardToKeep.sqrMagnitude < 0.0001f)
            forwardToKeep = Vector3.ProjectOnPlane(transform.right, playerUp);

        Quaternion targetRot = Quaternion.LookRotation(forwardToKeep.normalized, playerUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, alignToGravitySpeed * Time.fixedDeltaTime);
    }

    void Move()
    {
        if (cameraTransform == null && moveReference == MoveReference.Camera) return;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Transform refT = (moveReference == MoveReference.Camera) ? cameraTransform : transform;

        Vector3 refForward = Vector3.ProjectOnPlane(refT.forward, playerUp).normalized;
        Vector3 refRight   = Vector3.ProjectOnPlane(refT.right, playerUp).normalized;

        float speed = moveSpeed * (Input.GetKey(sprintKey) ? sprintMultiplier : 1f);

        Vector3 inputDir = (refRight * x + refForward * z);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        Vector3 desired = inputDir * speed;

        Vector3 vel = rb.linearVelocity;
        Vector3 vertical = Vector3.Project(vel, -playerUp);
        Vector3 horizontal = vel - vertical;

        float control = isGrounded ? 1f : airControl;
        Vector3 newHorizontal = Vector3.Lerp(horizontal, desired, control * 10f * Time.fixedDeltaTime);

        rb.linearVelocity = newHorizontal + vertical;

        // In 3rd person we usually want the character to face where they're moving,
        // but NOT where the camera is looking.
        if (moveReference == MoveReference.Player && rotateTowardMoveDirection)
        {
            if (Mathf.Abs(z) > 0.1f && inputDir.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(inputDir, playerUp);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.fixedDeltaTime);
            }
        }

        Vector2 moveInput = new Vector2(x, z);
        bool hasMoveInput = moveInput.sqrMagnitude > moveDeadzone * moveDeadzone;

        if (faceCameraWhenMoving && hasMoveInput && cameraTransform != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, playerUp).normalized;

            if (camForward.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(camForward, playerUp);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, faceTurnSpeed * Time.fixedDeltaTime);
            }
        }

    }


    void OnCollisionStay(Collision collision)
    {
        // Choose best contact for grounding + snapping
        Vector3 bestGroundNormal = groundNormal;
        float bestGroundDot = -1f;

        // For snapping, pick the contact we're moving into
        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        Vector3 incoming = speed > 0.001f ? (-v / speed) : Vector3.zero;

        Vector3 bestImpactNormal = playerUp;
        float bestImpactDot = -1f;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;

            // Grounding check: normal should be reasonably aligned with our current up
            float gDot = Vector3.Dot(n, playerUp);
            if (gDot > bestGroundDot)
            {
                bestGroundDot = gDot;
                bestGroundNormal = n;
            }

            // Impact check: which normal are we heading into?
            if (speed > 0.001f)
            {
                float iDot = Vector3.Dot(n, incoming);
                if (iDot > bestImpactDot)
                {
                    bestImpactDot = iDot;
                    bestImpactNormal = n;
                }
            }
        }

        // Set grounded if we're standing on something relative to current up
        if (bestGroundDot > groundedDot)
        {
            isGrounded = true;
            groundNormal = bestGroundNormal;

            // Lock gravity to what you’re standing on
            playerUp = Vector3.Slerp(playerUp, groundNormal.normalized, snapSlerpSpeed * Time.fixedDeltaTime);
        }

        // Snap gravity to what we actually hit while moving into it
        if (snapGravityOnImpact && speed > minSnapSpeed && bestImpactDot > 0.35f)
        {
            playerUp = Vector3.Slerp(playerUp, bestImpactNormal.normalized, snapSlerpSpeed * Time.fixedDeltaTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!snapGravityOnImpact) return;

        // Use impact direction to decide which surface we actually hit.
        Vector3 v = rb.linearVelocity;
        if (v.sqrMagnitude < 0.01f) return;

        Vector3 incoming = -v.normalized; // direction we're moving into
        Vector3 bestNormal = playerUp;
        float bestDot = -1f;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;
            float d = Vector3.Dot(n, incoming);
            if (d > bestDot)
            {
                bestDot = d;
                bestNormal = n;
            }
        }

        // Don’t overthink it: if we hit it, adopt it.
        playerUp = bestNormal.normalized;
        // Kill sideways velocity relative to the surface we landed on
        Vector3 normal = playerUp;
        Vector3 tangent = v - Vector3.Project(v, normal); // velocity along surface
        rb.linearVelocity = v - tangent * 0.6f; // remove surface sliding immediately
    }

    public void SetPlayerUp(Vector3 newUp)
    {
        if (newUp.sqrMagnitude < 0.0001f) return;
        playerUp = newUp.normalized;
    }

    public Vector3 GetPlayerUp() => playerUp;
}
