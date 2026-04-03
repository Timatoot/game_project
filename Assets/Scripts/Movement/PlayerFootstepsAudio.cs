using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepsAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGravityController controller;
    [SerializeField] private GravityInput input;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private AudioSource audioSource;

    [Header("Footstep Clips")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Timing")]
    [SerializeField] private float walkStepInterval = 0.41f;
    [SerializeField] private float sprintStepInterval = 0.29f;

    [Header("Movement Thresholds")]
    [SerializeField] private float minInputMagnitude = 0.15f;
    [SerializeField] private float minHorizontalSpeed = 1.0f;

    [Header("Sound Variation")]
    [SerializeField] private Vector2 volumeRange = new Vector2(0.85f, 1f);
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    private float stepTimer;
    private int lastClipIndex = -1;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<PlayerGravityController>();
        if (input == null) input = GetComponent<GravityInput>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        if (controller == null || input == null || rb == null || audioSource == null)
            return;

        if (footstepClips == null || footstepClips.Length == 0)
            return;

        Vector2 moveInput = input.Move;
        bool hasMoveInput = moveInput.magnitude > minInputMagnitude;
        bool grounded = controller.IsGrounded;

        Vector3 up = controller.GetPlayerUp();
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = velocity - Vector3.Project(velocity, up);
        float horizontalSpeed = horizontalVelocity.magnitude;

        bool isMovingEnough = horizontalSpeed > minHorizontalSpeed;

        if (!grounded || !hasMoveInput || !isMovingEnough)
        {
            stepTimer = 0f;
            return;
        }

        float interval = input.SprintHeld ? sprintStepInterval : walkStepInterval;
        stepTimer += Time.deltaTime;

        if (stepTimer >= interval)
        {
            PlayFootstep();
            stepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        int clipIndex = GetRandomClipIndex();
        AudioClip clip = footstepClips[clipIndex];

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, Random.Range(volumeRange.x, volumeRange.y));
    }

    private int GetRandomClipIndex()
    {
        if (footstepClips.Length == 1)
            return 0;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, footstepClips.Length);
        }
        while (newIndex == lastClipIndex);

        lastClipIndex = newIndex;
        return newIndex;
    }
}