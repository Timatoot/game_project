using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMovementAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGravityController gravityController;
    [SerializeField] private GravityPullGun pullGun;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private AudioSource whooshSource;

    [Header("Clips")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landingClip;
    [SerializeField] private AudioClip airWhooshClip;

    [Header("Volumes")]
    [SerializeField] [Range(0f, 1f)] private float jumpVolume = 0.9f;
    [SerializeField] [Range(0f, 1f)] private float landingVolume = 0.95f;
    [SerializeField] [Range(0f, 1f)] private float whooshVolume = 0.6f;

    [Header("Whoosh")]
    [SerializeField] private float minPullDistanceForWhoosh = 6f;
    [SerializeField] private float minWhooshSpeed = 7f;
    [SerializeField] private float whooshFadeOutSpeed = 8f;

    private float currentPullDistance;
    private bool whooshActive;

    private bool whooshArmed;

    private void Awake()
    {
        if (gravityController == null) gravityController = GetComponent<PlayerGravityController>();
        if (pullGun == null) pullGun = GetComponentInChildren<GravityPullGun>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        if (oneShotSource == null || whooshSource == null)
        {
            var sources = GetComponents<AudioSource>();
            if (sources.Length > 0 && oneShotSource == null) oneShotSource = sources[0];
            if (sources.Length > 1 && whooshSource == null) whooshSource = sources[1];
        }

        if (oneShotSource != null)
        {
            oneShotSource.playOnAwake = false;
            oneShotSource.loop = false;
            oneShotSource.spatialBlend = 1f;
        }

        if (whooshSource != null)
        {
            whooshSource.playOnAwake = false;
            whooshSource.loop = true;
            whooshSource.spatialBlend = 1f;
            whooshSource.clip = airWhooshClip;
            whooshSource.volume = 0f;
        }
    }

    private void OnEnable()
    {
        if (gravityController != null)
        {
            gravityController.Jumped += HandleJumped;
            gravityController.Landed += HandleLanded;
        }

        if (pullGun != null)
        {
            pullGun.PullStarted += HandlePullStarted;
            pullGun.PullEnded += HandlePullEnded;
        }
    }

    private void OnDisable()
    {
        if (gravityController != null)
        {
            gravityController.Jumped -= HandleJumped;
            gravityController.Landed -= HandleLanded;
        }

        if (pullGun != null)
        {
            pullGun.PullStarted -= HandlePullStarted;
            pullGun.PullEnded -= HandlePullEnded;
        }
    }

    private void Update()
    {
        UpdateWhoosh();
    }

    private void HandleJumped()
    {
        if (jumpClip != null && oneShotSource != null)
            oneShotSource.PlayOneShot(jumpClip, jumpVolume);
    }

    private void HandleLanded()
    {
        StopWhooshImmediate();

        if (landingClip != null && oneShotSource != null)
            oneShotSource.PlayOneShot(landingClip, landingVolume);
    }

    private void HandlePullStarted(float pullDistance)
    {
        currentPullDistance = pullDistance;

        if (whooshSource == null || airWhooshClip == null)
            return;

        whooshArmed = pullDistance >= minPullDistanceForWhoosh;

        if (!whooshArmed)
            return;

        whooshSource.clip = airWhooshClip;
        whooshSource.loop = true;

        if (!whooshSource.isPlaying)
            whooshSource.Play();
    }

    private void HandlePullEnded()
    {
        currentPullDistance = 0f;
    }

    private void UpdateWhoosh()
    {
        if (whooshSource == null || gravityController == null)
            return;

        float targetVolume = 0f;

        if (whooshArmed && !gravityController.IsGrounded)
        {
            targetVolume = whooshVolume;

            if (!whooshSource.isPlaying)
                whooshSource.Play();
        }

        whooshSource.volume = Mathf.MoveTowards(
            whooshSource.volume,
            targetVolume,
            whooshFadeOutSpeed * Time.deltaTime
        );

        if (whooshSource.isPlaying && targetVolume <= 0f && whooshSource.volume <= 0.001f)
            whooshSource.Stop();
    }

    private void StopWhooshImmediate()
    {
        whooshArmed = false;
        currentPullDistance = 0f;

        if (whooshSource != null)
        {
            whooshSource.volume = 0f;
            if (whooshSource.isPlaying)
                whooshSource.Stop();
        }
    }
}
