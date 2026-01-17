using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerGravityController : MonoBehaviour
{
    [Header("Gravity")]
    public float gravityStrength = 20f;
    public float alignToGravitySpeed = 10f;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float airControl = 0.5f;
    public float jumpSpeed = 7f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask = ~0;

    public Transform cameraTransform;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    // "Up" for the player. Gravity pulls in -up direction.
    private Vector3 playerUp = Vector3.up;

    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void FixedUpdate()
    {
        GroundCheck();
        ApplyCustomGravity();
        AlignToGravity();
        Move();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // jump in the "up" direction
            rb.linearVelocity += playerUp * jumpSpeed;
        }
    }

    void GroundCheck()
    {
        float radius = capsule.radius * 0.95f;
        float castDistance = (capsule.height * 0.5f) - radius + groundCheckDistance;

        // Cast from center downward along -playerUp
        Vector3 origin = transform.position + playerUp * 0.1f;

        isGrounded = Physics.SphereCast(origin, radius, -playerUp, out _, castDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    void ApplyCustomGravity()
    {
        // Always accelerate toward "down"
        Vector3 gravityAccel = -playerUp * gravityStrength;
        rb.AddForce(gravityAccel, ForceMode.Acceleration);
    }

    void AlignToGravity()
    {
        // Keep current forward direction, but make it valid on the surface plane.
        Vector3 forwardOnPlane = Vector3.ProjectOnPlane(transform.forward, playerUp);

        // If we're looking almost straight into up/down, pick a fallback.
        if (forwardOnPlane.sqrMagnitude < 0.0001f)
            forwardOnPlane = Vector3.ProjectOnPlane(transform.right, playerUp);

        Quaternion targetRot = Quaternion.LookRotation(forwardOnPlane.normalized, playerUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, alignToGravitySpeed * Time.fixedDeltaTime);
    }



    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // Movement relative to camera, projected onto the "surface plane"
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, playerUp).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, playerUp).normalized;

        Vector3 desired = (camRight * x + camForward * z).normalized * moveSpeed;

        // Current velocity decomposed into vertical/horizontal relative to playerUp
        Vector3 vel = rb.linearVelocity;
        Vector3 vertical = Vector3.Project(vel, -playerUp);
        Vector3 horizontal = vel - vertical;

        float control = isGrounded ? 1f : airControl;
        Vector3 newHorizontal = Vector3.Lerp(horizontal, desired, control * 10f * Time.fixedDeltaTime);

        rb.linearVelocity = newHorizontal + vertical;
    }

    // Called by the gravity gun script
    public void SetPlayerUp(Vector3 newUp)
    {
        if (newUp.sqrMagnitude < 0.0001f) return;
        playerUp = newUp.normalized;
    }

    public Vector3 GetPlayerUp() => playerUp;
}