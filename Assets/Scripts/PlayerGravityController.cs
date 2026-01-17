using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerGravityController : MonoBehaviour
{
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

    public Transform cameraTransform;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector3 playerUp = Vector3.up;

    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

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
        // Preserve facing direction while making up = playerUp
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, playerUp);
        if (forwardOnPlane.sqrMagnitude < 0.0001f)
            forwardOnPlane = Vector3.ProjectOnPlane(transform.right, playerUp);

        Quaternion target = Quaternion.LookRotation(forwardOnPlane.normalized, playerUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, alignToGravitySpeed * Time.fixedDeltaTime);
    }

    void Move()
    {
        if (cameraTransform == null) return;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, playerUp).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, playerUp).normalized;

        float speed = moveSpeed * (Input.GetKey(sprintKey) ? sprintMultiplier : 1f);
        Vector3 desired = (camRight * x + camForward * z);
        if (desired.sqrMagnitude > 1f) desired.Normalize();
        desired *= speed;

        Vector3 vel = rb.linearVelocity;
        Vector3 vertical = Vector3.Project(vel, -playerUp);
        Vector3 horizontal = vel - vertical;

        float control = isGrounded ? 1f : airControl;
        Vector3 newHorizontal = Vector3.Lerp(horizontal, desired, control * 10f * Time.fixedDeltaTime);

        rb.linearVelocity = newHorizontal + vertical;
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
    }

    public void SetPlayerUp(Vector3 newUp)
    {
        if (newUp.sqrMagnitude < 0.0001f) return;
        playerUp = newUp.normalized;
    }

    public Vector3 GetPlayerUp() => playerUp;
}
