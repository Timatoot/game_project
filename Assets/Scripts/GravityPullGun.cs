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

    [Header("Pull Hop (prevents ground sliding)")]
    public bool hopOnPull = true;
    public float hopVelocityOverride = -1f; // -1 = use PlayerGravityController.jumpSpeed
    public float hopGraceTime = 0.10f;      // small grace window after hop
    private float hopTimer;

    [Header("Target Filtering")]
    public float ignoreSameSurfaceAngle = 15f;     // degrees
    public float ignoreSameSurfaceDistance = 2.0f; // meters from player
    public float ignoreNearFromCamera = 0.75f;     // meters from camera (prevents grabbing ground right in front)
    public LayerMask playerMask = 6;

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

        if (hopTimer > 0f) hopTimer -= Time.fixedDeltaTime;

        // Important: don't steer gravity while still in contact with the old ground
        if (!controller.IsGrounded && hopTimer <= 0f)
        {
            controller.SetPlayerUp(Vector3.Slerp(controller.GetPlayerUp(), -pullDir, 15f * Time.fixedDeltaTime));
        }
    }


    void TryStartPull()
    {
        if (controller == null) return;

        Ray ray = new Ray(transform.position, transform.forward);
        var hits = Physics.RaycastAll(ray, maxDistance, aimMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Vector3 up = controller.GetPlayerUp();
        Vector3 playerPos = (rb != null) ? rb.position : controller.transform.position;

        RaycastHit chosen = default;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];

            // Skip super-near hits from the camera (usually your own platform / floor)
            if (h.distance < ignoreNearFromCamera)
                continue;

            // Skip your own colliders (if your player layer/mask setup isn’t perfect yet)
            if (rb != null && h.rigidbody == rb)
                continue;

            // If grounded, ignore “same surface + too close” hits (this is the slide problem)
            if (controller.IsGrounded)
            {
                float angle = Vector3.Angle(up, h.normal);
                float distToPlayer = Vector3.Distance(playerPos, h.point);

                if (angle < ignoreSameSurfaceAngle && distToPlayer < ignoreSameSurfaceDistance)
                    continue;
            }

            chosen = h;
            found = true;
            break;
        }

        if (!found) return;

        // Existing logic from here onward, but use `chosen` instead of `hit`
        if (controller.IsGrounded)
        {
            float angle = Vector3.Angle(controller.GetPlayerUp(), chosen.normal);
            if (angle < 5f) return;
        }

        if (rb != null)
        {
            float dist = Vector3.Distance(rb.position, chosen.point);
            if (dist < arriveDistance * 1.5f)
            {
                controller.SetPlayerUp(chosen.normal);
                return;
            }
        }

        pulling = true;
        targetCollider = chosen.collider;
        targetPoint = chosen.point;
        targetNormal = chosen.normal;

        if (hopOnPull)
        {
            DoPullHop();
        }
    }

    private void DoPullHop()
    {
        if (rb == null || controller == null) return;

        Vector3 up = controller.GetPlayerUp();

        // Use the same "takeoff speed" as your normal jump unless overridden.
        float hopVel = (hopVelocityOverride > 0f) ? hopVelocityOverride : controller.jumpSpeed;

        // Remove any downward velocity so the hop is consistent.
        Vector3 v = rb.linearVelocity;
        float down = Vector3.Dot(v, -up);
        if (down > 0f) v += up * down;

        // Ensure we have at least hopVel upward along current up (don't stack infinite boosts).
        float upVel = Vector3.Dot(v, up);
        if (upVel < hopVel) v += up * (hopVel - upVel);

        rb.linearVelocity = v;
        hopTimer = hopGraceTime;
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
