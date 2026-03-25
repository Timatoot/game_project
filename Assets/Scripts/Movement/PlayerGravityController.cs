using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerGravityController : MonoBehaviour
{
    public enum MoveReference { Camera, Player }

    public System.Action Jumped;
    public System.Action Landed;

    [Header("Input")]
    public GravityInput input;

    [Header("Movement Reference")]
    public MoveReference moveReference = MoveReference.Camera;

    [Header("Collision Masks")]
    public LayerMask groundMask = ~0;

    [Header("Gravity")]
    public float gravityStrength = 20f;
    public float alignToGravitySpeed = 12f;

    [Header("Auto Snap Gravity")]
    public bool snapGravityOnImpact = true;
    public float snapSlerpSpeed = 25f;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float sprintMultiplier = 1.6f;
    public float airControl = 0.5f;

    [Header("Sprint Direction Scaling")]
    public float backwardSprintMultiplier = 1.36f; // tuned so moveSpeed * backwardSprintMultiplier ≈ 6
    public float backwardSprintThreshold = -0.05f; // input.z less than this counts as backward
    public float forwardSprintThreshold = 0.05f;

    [Header("Jump")]
    public float jumpSpeed = 7f;
    public float groundedDot = 0.55f;

    [Header("Third Person Facing")]
    public bool faceCameraWhenMoving = false;
    public float faceTurnSpeed = 12f;
    public float moveDeadzone = 0.05f;

    [Header("Landing / Ground Stick")]
    public float landingHorizontalDamp = 0.15f;
    public float groundedFriction = 10f;

    [Header("Idle Rotation Lock")]
    public bool lockYawWhenIdle = true;

    [Header("References")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector3 playerUp = Vector3.up;

    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    private bool groundedNext;
    private Vector3 groundNormalNext = Vector3.up;

    private bool landedThisStep;

    private bool jumpedThisAir;
    private float lastMoveInputSqr;

    private bool hasIdleLock;
    private Quaternion idleLockRotation;

    public bool IsGrounded => isGrounded;
    public Vector3 GetPlayerUp() => playerUp;

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

        if (input == null)
            input = GetComponent<GravityInput>();
    }

    void FixedUpdate()
    {
        // Consume grounding results from last physics step
        isGrounded = groundedNext;
        groundNormal = groundedNext ? groundNormalNext : playerUp;

        groundedNext = false;
        groundNormalNext = playerUp;
        landedThisStep = false;

        Vector2 move = input != null ? input.Move : Vector2.zero;
        float x = move.x;
        float z = move.y;

        lastMoveInputSqr = x * x + z * z;

        UpdateIdleLock(x, z);

        ApplyCustomGravity();
        AlignToGravity();
        ApplyMovement(x, z);
        ApplyIdleGroundFriction();

        if (isGrounded)
            jumpedThisAir = false;
    }

    void Update()
    {
        if (input == null) return;
        if (!isGrounded) return;
        if (!input.JumpPressed) return;

        DoJump();
    }

    private void UpdateIdleLock(float x, float z)
    {
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
    }

    private void ApplyCustomGravity()
    {
        rb.AddForce(-playerUp * gravityStrength, ForceMode.Acceleration);
    }

    private void AlignToGravity()
    {
        Vector3 forwardToKeep = hasIdleLock
            ? Vector3.ProjectOnPlane(idleLockRotation * Vector3.forward, playerUp)
            : Vector3.ProjectOnPlane(transform.forward, playerUp);

        if (forwardToKeep.sqrMagnitude < 0.0001f)
            forwardToKeep = Vector3.ProjectOnPlane(transform.right, playerUp);

        Quaternion targetRot = Quaternion.LookRotation(forwardToKeep.normalized, playerUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, alignToGravitySpeed * Time.fixedDeltaTime);
    }

    private void ApplyMovement(float x, float z)
    {
        if (cameraTransform == null && moveReference == MoveReference.Camera) return;

        Transform refT = (moveReference == MoveReference.Camera) ? cameraTransform : transform;

        Vector3 refForward = Vector3.ProjectOnPlane(refT.forward, playerUp).normalized;
        Vector3 refRight = Vector3.ProjectOnPlane(refT.right, playerUp).normalized;

        bool sprintHeld = (input != null) && input.SprintHeld;

        float mult = 1f;

        if (sprintHeld)
        {
            if (z < backwardSprintThreshold)
            {
                mult = backwardSprintMultiplier;
            }
            else
            {
                mult = sprintMultiplier;
            }
        }

        float speed = moveSpeed * mult;


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
        Vector3 vertical = Vector3.Project(vel, playerUp);
        Vector3 horizontal = vel - vertical;

        float control = isGrounded ? 1f : airControl;
        Vector3 newHorizontal = Vector3.Lerp(horizontal, desired, control * 10f * Time.fixedDeltaTime);

        rb.linearVelocity = newHorizontal + vertical;

        bool hasMoveInput = lastMoveInputSqr > (moveDeadzone * moveDeadzone);

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

    private void ApplyIdleGroundFriction()
    {
        if (!isGrounded) return;
        if (lastMoveInputSqr >= (moveDeadzone * moveDeadzone)) return;

        Vector3 v = rb.linearVelocity;
        Vector3 vertical = Vector3.Project(v, playerUp);
        Vector3 horizontal = v - vertical;

        horizontal = Vector3.Lerp(horizontal, Vector3.zero, groundedFriction * Time.fixedDeltaTime);
        rb.linearVelocity = horizontal + vertical;
    }

    private void DoJump()
    {
        Vector3 v = rb.linearVelocity;

        float down = Vector3.Dot(v, -playerUp);
        if (down > 0f) v += playerUp * down;

        rb.linearVelocity = v;
        rb.AddForce(playerUp * jumpSpeed, ForceMode.VelocityChange);
        jumpedThisAir = true;

        Jumped?.Invoke();
    }

    void OnCollisionStay(Collision collision)
    {
        // if the object we hit is not in groundmask, ignore
        if (((1 << collision.gameObject.layer) & groundMask) == 0) return;

        if (!snapGravityOnImpact) return;

        // Find best support normal
        Vector3 bestNormal = playerUp;
        float bestDot = -999f;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;
            float d = Vector3.Dot(n, playerUp);
            if (d > bestDot)
            {
                bestDot = d;
                bestNormal = n;
            }
        }

        if (bestDot <= groundedDot) return;

        // Ensure it’s actually under you, not a side brush
        bool hasBelowContact = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            var c = collision.GetContact(i);
            if (Vector3.Dot(c.normal, bestNormal) < 0.95f) continue;

            Vector3 toContact = (c.point - transform.position).normalized;
            float below = Vector3.Dot(toContact, -playerUp);
            if (below > 0.2f)
            {
                hasBelowContact = true;
                break;
            }
        }

        if (!hasBelowContact) return;

        if (!isGrounded && !landedThisStep)
        {
            ApplyLandingBrake();
            landedThisStep = true;
            Landed?.Invoke();
        }

        groundedNext = true;
        groundNormalNext = bestNormal.normalized;

        // Smoothly align up to the surface when grounded
        float angle = Vector3.Angle(playerUp, groundNormalNext);
        if (angle > 1.0f)
            playerUp = Vector3.Slerp(playerUp, groundNormalNext, snapSlerpSpeed * Time.fixedDeltaTime);
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

        // Reduce tangent velocity on impact
        Vector3 v = rb.linearVelocity;
        Vector3 vertical = Vector3.Project(v, playerUp);
        Vector3 horizontal = v - vertical;

        rb.linearVelocity = vertical + horizontal * 0.4f;
    }

    private void ApplyLandingBrake()
    {
        Vector3 v = rb.linearVelocity;

        float into = Vector3.Dot(v, -playerUp);
        if (into > 0f) v += playerUp * into;

        Vector3 vertical = Vector3.Project(v, playerUp);
        Vector3 horizontal = v - vertical;

        horizontal *= landingHorizontalDamp;
        rb.linearVelocity = vertical + horizontal;
    }

    public void SetPlayerUp(Vector3 newUp)
    {
        if (newUp.sqrMagnitude < 0.0001f) return;
        playerUp = newUp.normalized;
    }

    public void ForceRespawnState(Vector3 newUp, Vector3 newForward, Rigidbody body)
    {
        if (newUp.sqrMagnitude > 0.0001f)
            playerUp = newUp.normalized;
        else
            playerUp = Vector3.up;

        groundedNext = false;
        groundNormalNext = playerUp;
        isGrounded = false;
        groundNormal = playerUp;
        landedThisStep = false;
        jumpedThisAir = false;
        lastMoveInputSqr = 0f;
        hasIdleLock = false;

        Vector3 projectedForward = Vector3.ProjectOnPlane(newForward, playerUp);
        if (projectedForward.sqrMagnitude < 0.0001f)
            projectedForward = Vector3.ProjectOnPlane(transform.forward, playerUp);

        if (projectedForward.sqrMagnitude < 0.0001f)
            projectedForward = Vector3.forward;

        transform.rotation = Quaternion.LookRotation(projectedForward.normalized, playerUp);

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
