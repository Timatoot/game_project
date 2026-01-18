using UnityEngine;

public class GravityPullGun : MonoBehaviour
{
    public PlayerGravityController controller; // drag Player here
    public Rigidbody rb;                       // drag Player's Rigidbody here (or auto-find)
    public GravityInput input;                 // drag Player (with GravityInput) or auto-find

    public float maxDistance = 120f;
    public LayerMask aimMask = ~0;

    [Header("Pull Tuning")]
    public float pullAcceleration = 20f;
    public float arriveDistance = 1.2f;
    public float dampSideways = 4f;

    private bool pulling;
    private Collider targetCollider;
    private Vector3 targetPoint;
    private Vector3 targetNormal;

    void Awake()
    {
        if (controller == null) controller = GetComponentInParent<PlayerGravityController>();
        if (rb == null && controller != null) rb = controller.GetComponent<Rigidbody>();

        if (input == null && controller != null) input = controller.GetComponent<GravityInput>();
    }

    void Update()
    {
        if (input != null && input.PullPressed)
            TryStartPull();
    }

    void FixedUpdate()
    {
        if (!pulling || rb == null) return;

        Vector3 toTarget = targetPoint - rb.position;
        float dist = toTarget.magnitude;

        if (dist < arriveDistance)
        {
            FinishPull();
            return;
        }

        Vector3 pullDir = toTarget / dist;

        rb.AddForce(pullDir * pullAcceleration, ForceMode.Acceleration);

        Vector3 v = rb.linearVelocity;
        Vector3 sideways = v - Vector3.Project(v, pullDir);
        rb.linearVelocity = v - sideways * Mathf.Clamp01(dampSideways * Time.fixedDeltaTime);

        controller.SetPlayerUp(Vector3.Slerp(controller.GetPlayerUp(), -pullDir, 15f * Time.fixedDeltaTime));
    }

    void TryStartPull()
    {
        if (controller == null) return;

        if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit,
            maxDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        if (controller.IsGrounded)
        {
            float angle = Vector3.Angle(controller.GetPlayerUp(), hit.normal);
            if (angle < 5f) return;
        }

        if (rb != null)
        {
            float dist = Vector3.Distance(rb.position, hit.point);
            if (dist < arriveDistance * 1.5f)
            {
                controller.SetPlayerUp(hit.normal);
                return;
            }
        }

        pulling = true;
        targetCollider = hit.collider;
        targetPoint = hit.point;
        targetNormal = hit.normal;
    }

    void FinishPull()
    {
        pulling = false;
        controller.SetPlayerUp(targetNormal);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!pulling) return;

        if (collision.collider == targetCollider)
            FinishPull();
    }
}
