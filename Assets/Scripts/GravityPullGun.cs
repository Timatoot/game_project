using UnityEngine;

public class GravityPullGun : MonoBehaviour
{
    public PlayerGravityController controller; // drag Player here
    public Rigidbody rb;                       // drag Player's Rigidbody here (or auto-find)
    public KeyCode pullKey = KeyCode.E;

    public float maxDistance = 120f;
    public LayerMask aimMask = ~0;

    [Header("Pull Tuning")]
    public float pullAcceleration = 20f;       // how hard we pull
    public float arriveDistance = 1.2f;        // when we consider ourselves "at" the surface
    public float dampSideways = 4f;            // kills sideways sliding during pull

    private bool pulling;
    private Collider targetCollider;
    private Vector3 targetPoint;
    private Vector3 targetNormal;

    void Awake()
    {
        if (controller == null) controller = GetComponentInParent<PlayerGravityController>();
        if (rb == null && controller != null) rb = controller.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(pullKey))
        {
            TryStartPull();
        }
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

        // Strong attraction force toward the exact point you aimed at.
        rb.AddForce(pullDir * pullAcceleration, ForceMode.Acceleration);

        // Dampen sideways motion so you don't orbit or skim off.
        Vector3 v = rb.linearVelocity;
        Vector3 sideways = v - Vector3.Project(v, pullDir);
        rb.linearVelocity = v - sideways * Mathf.Clamp01(dampSideways * Time.fixedDeltaTime);

        // Optional: while pulling, rotate "up" opposite to pull direction so your gravity aligns with the pull.
        // This makes it feel like you're being reoriented toward the target.
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

        // If we're already grounded on a surface with basically the same normal, do nothing.
        if (controller.IsGrounded)
        {
            float angle = Vector3.Angle(controller.GetPlayerUp(), hit.normal);
            if (angle < 5f)
                return;
        }

        // If we're already basically at the surface, just lock gravity and don't pull.
        if (rb != null)
        {
            float dist = Vector3.Distance(rb.position, hit.point);
            if (dist < arriveDistance * 1.5f)
            {
                controller.SetPlayerUp(hit.normal);
                return;
            }
        }

        // Start pulling
        pulling = true;
        targetCollider = hit.collider;
        targetPoint = hit.point;
        targetNormal = hit.normal;
    }


    void FinishPull()
    {
        pulling = false;

        // Lock gravity to the surface you pulled onto.
        controller.SetPlayerUp(targetNormal);
    }

    // If we physically touch something during pull, prefer snapping to the thing we were pulling to.
    void OnCollisionEnter(Collision collision)
    {
        if (!pulling) return;

        if (collision.collider == targetCollider)
        {
            // We reached the intended surface.
            FinishPull();
        }
    }
}
