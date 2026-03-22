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

    [Header("Target Filtering")]
    public float ignoreSameSurfaceAngle = 15f;
    public float ignoreSameSurfaceDistance = 1.0f;

    [Header("Grav Latch Angle")]
    public float angleNeeded = 10f;

    [Header("Highlighting")]
    public int outlineLayerIndex = 7;
    private GameObject lastTarget;
    private int originalLayer;

    [Header("Debug")]
    public bool debugTargeting;

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

        if (TrySelectVisibleTarget(out RaycastHit hit))
        {
            GameObject currentTarget = hit.collider.gameObject;
            ApplyHighlight(currentTarget);
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
        if (pulling) return;

        if (!TrySelectVisibleTarget(out RaycastHit chosen))
            return;

        float surfaceAngle = Vector3.Angle(controller.GetPlayerUp(), chosen.normal);
        if (surfaceAngle < angleNeeded && controller.IsGrounded)
        {
            if (debugTargeting)
                Debug.Log($"Blocked pull: target angle {surfaceAngle:F1} is below required {angleNeeded:F1}.");
            return;
        }

        if (controller.IsGrounded)
        {
            float angle = Vector3.Angle(controller.GetPlayerUp(), chosen.normal);
            if (angle < 5f)
                return;
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

    private bool TrySelectVisibleTarget(out RaycastHit chosen)
    {
        chosen = default;

        Ray ray = new Ray(transform.position, transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            if (debugTargeting)
                Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.red, 0.05f);
            return false;
        }

        if (debugTargeting)
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 0.05f);

        if (hit.distance < ignoreNearFromCamera)
        {
            if (debugTargeting)
                Debug.Log($"Ignoring target {hit.collider.name}: too close to camera.");
            return false;
        }

        if (rb != null && hit.rigidbody == rb)
        {
            if (debugTargeting)
                Debug.Log("Ignoring target: hit player rigidbody.");
            return false;
        }

        if (controller != null && controller.IsGrounded && IsSameSurface(hit))
        {
            if (debugTargeting)
                Debug.Log($"Ignoring target {hit.collider.name}: same grounded surface, so nothing behind it can be selected.");
            return false;
        }

        chosen = hit;
        return true;
    }

    private bool IsSameSurface(RaycastHit hit)
    {
        if (controller == null) return false;

        Vector3 up = controller.GetPlayerUp();
        Vector3 playerPos = (rb != null) ? rb.position : controller.transform.position;

        float angle = Vector3.Angle(up, hit.normal);
        float distToPlayer = Vector3.Distance(playerPos, hit.point);

        return angle < ignoreSameSurfaceAngle && distToPlayer < ignoreSameSurfaceDistance;
    }

    private void DoPullHop()
    {
        if (rb == null || controller == null) return;

        Vector3 up = controller.GetPlayerUp();
        float hopVel = (hopVelocityOverride > 0f) ? hopVelocityOverride : controller.jumpSpeed;

        Vector3 v = rb.linearVelocity;

        controller.Jumped?.Invoke();
        hopTimer = hopGraceTime;

        float down = Vector3.Dot(v, -up);
        if (down > 0f) v += up * down;

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
}
