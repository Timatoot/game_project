using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerGravityController : MonoBehaviour
{
    public enum MoveReference { Camera, Player }

    [Header("Input")]
    public GravityInput input; // Drag Player (with GravityInput) or auto-find

    [Header("Movement Reference")]
    public MoveReference moveReference = MoveReference.Camera;

    [Header("Turn Toward Movement (3rd person feel)")]
    public bool rotateTowardMoveDirection = false;
    public float turnSpeed = 10f;

    [Header("Collision Masks")]
    public LayerMask groundMask = ~0;

    [Header("Gravity")]
    public float gravityStrength = 20f;
    public float alignToGravitySpeed = 12f;

    [Header("Auto Snap Gravity")]
    public bool snapGravityOnImpact = true;
    public float minSnapSpeed = 1.0f;
    public float snapSlerpSpeed = 25f;

    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.6f;

    [Header("Air Control")]
    public float airControl = 0.5f;

    [Header("Jump")]
    public float jumpSpeed = 7f;
    public float groundedDot = 0.55f;

    [Header("Third Person Facing")]
    public bool faceCameraWhenMoving = false;
    public float faceTurnSpeed = 12f;
    public float moveDeadzone = 0.05f;

    [Header("Landing / Ground Stick")]
    public float landingHorizontalDamp = 0.15f; // 0.15 = keep 15% of horizontal speed on landing
    public float groundedFriction = 10f;        // only used when no move input

    [Header("Idle Rotation Lock")]
    public bool lockYawWhenIdle = true;
    public Transform cameraTransform;
    private bool jumpedThisAir;
    private float airTime;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector3 playerUp = Vector3.up;

    private bool groundedLastStep;
    private bool landedThisStep;

    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    private bool hasIdleLock;
    private Quaternion idleLockRotation;

    public bool IsGrounded => isGrounded;

    private bool wasGrounded;
    private float lastMoveInputSqr;

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

        if (input == null) input = GetComponent<GravityInput>();
    }

    void FixedUpdate()
    {
        groundedLastStep = isGrounded;  // keep last step result
        isGrounded = false;             // will be set true by collision callbacks
        landedThisStep = false;

        groundNormal = playerUp;

        Vector2 move = input != null ? input.Move : Vector2.zero;
        float x = move.x;
        float z = move.y;

        lastMoveInputSqr = x * x + z * z;

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

        isGrounded = false;
        wasGrounded = false;
        groundNormal = playerUp;

        ApplyCustomGravity();
        AlignToGravity();
        Move(x, z);

        if (isGrounded && lastMoveInputSqr < (moveDeadzone * moveDeadzone))
        {
            Vector3 v = rb.linearVelocity;

            // Split into vertical (along up) and horizontal (on surface)
            Vector3 vertical = Vector3.Project(v, playerUp);
            Vector3 horizontal = v - vertical;

            horizontal = Vector3.Lerp(horizontal, Vector3.zero, groundedFriction * Time.fixedDeltaTime);
            rb.linearVelocity = horizontal + vertical;
        }

        if (isGrounded) airTime = 0f;
        else airTime += Time.fixedDeltaTime;

        if (isGrounded) jumpedThisAir = false;
    }

    void Update()
    {
        if (input != null && input.JumpPressed && isGrounded)
        {
            Vector3 v = rb.linearVelocity;
            float down = Vector3.Dot(v, -playerUp);
            if (down > 0f) v += playerUp * down;

            rb.linearVelocity = v;
            rb.AddForce(playerUp * jumpSpeed, ForceMode.VelocityChange);
            jumpedThisAir = true;
        }
    }

    void ApplyCustomGravity()
    {
        rb.AddForce(-playerUp * gravityStrength, ForceMode.Acceleration);
    }

    void AlignToGravity()
    {
        Vector3 forwardToKeep;

        if (hasIdleLock)
            forwardToKeep = Vector3.ProjectOnPlane(idleLockRotation * Vector3.forward, playerUp);
        else
            forwardToKeep = Vector3.ProjectOnPlane(transform.forward, playerUp);

        if (forwardToKeep.sqrMagnitude < 0.0001f)
            forwardToKeep = Vector3.ProjectOnPlane(transform.right, playerUp);

        Quaternion targetRot = Quaternion.LookRotation(forwardToKeep.normalized, playerUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, alignToGravitySpeed * Time.fixedDeltaTime);
    }

    void Move(float x, float z)
    {
        if (cameraTransform == null && moveReference == MoveReference.Camera) return;

        Transform refT = (moveReference == MoveReference.Camera) ? cameraTransform : transform;

        Vector3 refForward = Vector3.ProjectOnPlane(refT.forward, playerUp).normalized;
        Vector3 refRight = Vector3.ProjectOnPlane(refT.right, playerUp).normalized;

        bool sprint = (input != null) && input.SprintHeld;
        float speed = moveSpeed * (sprint ? sprintMultiplier : 1f);

        Vector3 inputDir = (refRight * x + refForward * z);
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        Vector3 desired = inputDir * speed;

        if (!isGrounded && desired.sqrMagnitude > 0.001f)
        {
            Vector3 dir = desired.normalized;

            float probeDist = 0.25f;
            float probeRadius = capsule.radius * 0.9f;
            Vector3 probeOrigin = transform.position;

            if (Physics.SphereCast(probeOrigin, probeRadius, dir, out RaycastHit hit, probeDist, groundMask, QueryTriggerInteraction.Ignore))
                desired = Vector3.ProjectOnPlane(desired, hit.normal);
        }

        Vector3 vel = rb.linearVelocity;
        Vector3 vertical = Vector3.Project(vel, -playerUp);
        Vector3 horizontal = vel - vertical;

        float control = isGrounded ? 1f : airControl;
        Vector3 newHorizontal = Vector3.Lerp(horizontal, desired, control * 10f * Time.fixedDeltaTime);

        rb.linearVelocity = newHorizontal + vertical;

        if (moveReference == MoveReference.Player && rotateTowardMoveDirection)
        {
            if (Mathf.Abs(z) > 0.1f && inputDir.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(inputDir, playerUp);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.fixedDeltaTime);
            }
        }

        bool hasMoveInput = (x * x + z * z) > (moveDeadzone * moveDeadzone);

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
        Vector3 bestGroundNormal = groundNormal;
        float bestGroundDot = -999f;

        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        Vector3 incoming = speed > 0.001f ? (-v / speed) : Vector3.zero;

        Vector3 bestImpactNormal = playerUp;
        float bestImpactDot = -1f;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;

            float gDot = Vector3.Dot(n, playerUp);
            if (gDot > bestGroundDot)
            {
                bestGroundDot = gDot;
                bestGroundNormal = n;
            }

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

        if (bestGroundDot > groundedDot)
        {
            bool hasBelowContact = false;

            for (int i = 0; i < collision.contactCount; i++)
            {
                var c = collision.GetContact(i);

                if (Vector3.Dot(c.normal, bestGroundNormal) < 0.95f)
                    continue;

                Vector3 toContact = (c.point - transform.position).normalized;
                float below = Vector3.Dot(toContact, -playerUp);

                if (below > 0.2f)
                {
                    hasBelowContact = true;
                    break;
                }
            }

            if (hasBelowContact)
            {
                if (!groundedLastStep && !landedThisStep)
                {
                    ApplyLandingBrake();
                    landedThisStep = true;
                }

                isGrounded = true;
                groundNormal = bestGroundNormal;
                wasGrounded = true;

                float angle = Vector3.Angle(playerUp, groundNormal);
                if (angle > 1.0f)
                    playerUp = Vector3.Slerp(playerUp, groundNormal.normalized, snapSlerpSpeed * Time.fixedDeltaTime);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!snapGravityOnImpact) return;
        if (!jumpedThisAir) return;

        float relSpeed = collision.relativeVelocity.magnitude;
        if (relSpeed < 0.2f) return;

        Vector3 rv = collision.relativeVelocity;

        Vector3 bestNormal = playerUp;
        float bestInto = 0f;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;
            float into = Vector3.Dot(rv, -n);
            if (into > bestInto)
            {
                bestInto = into;
                bestNormal = n;
            }
        }

        if (bestInto < 0.2f) return;

        float upDot = Vector3.Dot(bestNormal, playerUp);
        if (upDot > 0.75f || upDot < -0.75f) return;

        playerUp = bestNormal.normalized;

        Vector3 v = rb.linearVelocity;
        Vector3 tangent = v - Vector3.Project(v, playerUp);
        rb.linearVelocity = v - tangent * 0.6f;
    }

    private void ApplyLandingBrake()
    {
        Vector3 v = rb.linearVelocity;

        // remove velocity into the ground
        float into = Vector3.Dot(v, -playerUp);
        if (into > 0f) v += playerUp * into;

        // damp horizontal only
        Vector3 vertical = Vector3.Project(v, playerUp);
        Vector3 horizontal = v - vertical;

        horizontal *= landingHorizontalDamp; // e.g. 0.15f
        rb.linearVelocity = vertical + horizontal;
    }

    public void SetPlayerUp(Vector3 newUp)
    {
        if (newUp.sqrMagnitude < 0.0001f) return;
        playerUp = newUp.normalized;
    }

    public Vector3 GetPlayerUp() => playerUp;
}
