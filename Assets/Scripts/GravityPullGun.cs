using UnityEngine;

public class GravityPullGun : MonoBehaviour
{
    [Header("References")]
    public PlayerGravityController controller;
    public Rigidbody rb;
    public GravityInput input;

    [Header("Targeting")]
    public float maxDistance = 120f;
    public LayerMask aimMask = ~0;
    public float ignoreNearFromCamera = 0.75f;

    [Header("Pull Tuning")]
    public float pullAcceleration = 20f;
    public float arriveDistance = 1.2f;
    public float dampSideways = 4f;

    [Header("Pull Hop (prevents ground sliding)")]
    public bool hopOnPull = true;
    public float hopVelocityOverride = -1f; // -1 = use PlayerGravityController.jumpSpeed
    public float hopGraceTime = 0.10f;

    [Header("Target Filtering (prevents selecting the surface you’re already on)")]
    public float ignoreSameSurfaceAngle = 15f;
    public float ignoreSameSurfaceDistance = 2.0f;

    [Header("Grav Latch Angle")]
    public float angleNeeded = 10f;

    [Header("Highlighting")]
    public int outlineLayerIndex = 7;
    private GameObject lastTarget;
    private int originalLayer;

    private bool pulling;
    private Collider targetCollider;
    private Vector3 targetPoint;
    private Vector3 targetNormal;

    private float hopTimer;

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

        if (TrySelectTarget(out RaycastHit hit))
        {
            // if grounded, unhighlight the floor we are currently on are 
            if (controller.IsGrounded && hit.collider == targetCollider)
            {
                ResetHighlight();
                return;
            }

            // call game object instead of renderer
            GameObject currentTarget = hit.collider.gameObject;

            // check the distance and see if you wanna highlight it
            float dist = Vector3.Distance(transform.position, hit.point);

            if (dist <= maxDistance)
            {
                ApplyHighlight(currentTarget);
            }
            else
            {
                // reset highlight if unable to reach
                ResetHighlight();
            }
        }
        else
        {
            ResetHighlight();
        }
    }

    private void ApplyHighlight(GameObject target)
    {
        if (target == lastTarget) return;

        ResetHighlight();

        lastTarget = target;
        originalLayer = target.layer;

        // this layer needs to be the outline layer to work
        target.layer = outlineLayerIndex;
    }

    private void ResetHighlight()
    {
        if (lastTarget != null)
        {
            lastTarget.layer = originalLayer;
            lastTarget = null;
        }
    }

    void FixedUpdate()
    {
        if (!pulling || rb == null || controller == null) return;

        UpdatePull();
    }

    private void UpdatePull()
    {
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

        if (!controller.IsGrounded && hopTimer <= 0f)
        {
            controller.SetPlayerUp(Vector3.Slerp(controller.GetPlayerUp(), -pullDir, 15f * Time.fixedDeltaTime));
        }
    }

    private void TryStartPull()
    {
        if (controller == null) return;

        if (pulling) return; //if we are mid "pull", ignore all inputs

        if (!TrySelectTarget(out RaycastHit chosen))
            return;

        float surfaceAngle = Vector3.Angle(controller.GetPlayerUp(), chosen.normal);
        // if the angle is greater than the 10 degree threshold, we can attach
        // to the same object, aka we can attach to the a diffrent side of the
        // same platform.
        if (surfaceAngle < angleNeeded)
        {
            // only block if we are on the same side of the platform
            if (controller.IsGrounded)
            {
                Debug.Log("needs to be greater than: " + angleNeeded + ". current angle is: " + surfaceAngle);
                return;
            }
        }


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
            DoPullHop();
    }

    private bool TrySelectTarget(out RaycastHit chosen)
    {
        chosen = default;

        Ray ray = new Ray(transform.position, transform.forward);
        var hits = Physics.RaycastAll(ray, maxDistance, aimMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Vector3 up = controller.GetPlayerUp();
        Vector3 playerPos = (rb != null) ? rb.position : controller.transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];

            if (h.distance < ignoreNearFromCamera)
                continue;

            // Skip self if you ever accidentally include player layer in aimMask
            if (rb != null && h.rigidbody == rb)
                continue;

            if (controller.IsGrounded)
            {
                float angle = Vector3.Angle(up, h.normal);
                float distToPlayer = Vector3.Distance(playerPos, h.point);

                if (angle < ignoreSameSurfaceAngle && distToPlayer < ignoreSameSurfaceDistance)
                    continue;
            }

            chosen = h;
            return true;
        }

        return false;
    }

    private void DoPullHop()
    {
        if (rb == null || controller == null) return;

        Vector3 up = controller.GetPlayerUp();
        float hopVel = (hopVelocityOverride > 0f) ? hopVelocityOverride : controller.jumpSpeed;

        Vector3 v = rb.linearVelocity;

        controller.Jumped?.Invoke();
        hopTimer = hopGraceTime;

        // Remove downward velocity so hop is consistent
        float down = Vector3.Dot(v, -up);
        if (down > 0f) v += up * down;

        // Ensure at least hopVel upward
        float upVel = Vector3.Dot(v, up);
        if (upVel < hopVel) v += up * (hopVel - upVel);

        rb.linearVelocity = v;
        hopTimer = hopGraceTime;
    }

    private void FinishPull()
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
