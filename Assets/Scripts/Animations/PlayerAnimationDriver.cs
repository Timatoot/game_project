using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAnimationDriver : MonoBehaviour
{
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int LandHash = Animator.StringToHash("Land");

    [SerializeField] private Animator animator;
    [SerializeField] private PlayerGravityController gravityController;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GravityInput input;

    [SerializeField] private float speedDampTime = 0.08f;

    private bool wasGrounded;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (gravityController == null) gravityController = GetComponentInParent<PlayerGravityController>();
        if (rb == null && gravityController != null) rb = gravityController.GetComponent<Rigidbody>();
        if (input == null && gravityController != null) input = gravityController.GetComponent<GravityInput>();
    }

    void Update()
    {
        if (animator == null || gravityController == null || rb == null) return;

        Vector3 up = gravityController.GetPlayerUp();
        Vector3 v = rb.linearVelocity;

        float verticalSpeed = Vector3.Dot(v, up);
        Vector3 horizontal = v - up * verticalSpeed;
        float speed = horizontal.magnitude;

        Vector2 moveInput = (input != null) ? input.Move : Vector2.zero;

        //normalize so diagonals don’t push magnitude above 1
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

        bool grounded = gravityController.IsGrounded;
        bool sprinting = input != null && input.SprintHeld;

        animator.SetBool("IsGrounded", grounded);
        animator.SetFloat("VerticalSpeed", verticalSpeed);
        animator.SetBool("IsSprinting", sprinting);
        animator.SetFloat("Speed", speed, speedDampTime, Time.deltaTime);
        animator.SetFloat("MoveX", moveInput.x, 0.08f, Time.deltaTime);
        animator.SetFloat("MoveY", moveInput.y, 0.08f, Time.deltaTime);

        wasGrounded = grounded;
    }

    private void HandleJump()
    {
        if (animator != null) animator.SetTrigger(JumpHash);
    }

    private void HandleLand()
    {
        if (animator != null) animator.SetTrigger(LandHash);
    }

    void OnEnable()
    {
        if (gravityController == null) gravityController = GetComponentInParent<PlayerGravityController>();
        if (gravityController != null)
        {
            gravityController.Jumped += HandleJump;
            gravityController.Landed += HandleLand;
        }
    }

    void OnDisable()
    {
        if (gravityController != null)
        {
            gravityController.Jumped -= HandleJump;
            gravityController.Landed -= HandleLand;
        }
    }
}
