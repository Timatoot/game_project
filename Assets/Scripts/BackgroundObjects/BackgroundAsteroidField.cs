using System.Collections.Generic;
using UnityEngine;

public class BackgroundAsteroidField : MonoBehaviour
{
    [System.Serializable]
    private class AsteroidState
    {
        public Transform transform;
        public Vector3 startLocalPosition;
        public Quaternion startLocalRotation;
        public Vector3 amplitude;
        public Vector3 frequency;
        public Vector3 phase;
        public Vector3 rotationAxis;
        public float rotationSpeed;
    }

    [Header("Setup")]
    [SerializeField] private bool includeInactiveChildren = false;
    [SerializeField] private bool randomizeInitialYRotation = true;

    [Header("Floating Distance")]
    [SerializeField] private Vector2 xAmplitudeRange = new Vector2(0.05f, 0.25f);
    [SerializeField] private Vector2 yAmplitudeRange = new Vector2(0.03f, 0.18f);
    [SerializeField] private Vector2 zAmplitudeRange = new Vector2(0.05f, 0.25f);

    [Header("Floating Speed")]
    [SerializeField] private Vector2 frequencyRange = new Vector2(0.08f, 0.25f);

    [Header("Rotation")]
    [SerializeField] private Vector2 rotationSpeedRange = new Vector2(0.5f, 2.0f);

    [Header("Time")]
    [SerializeField] private bool useUnscaledTime = false;

    private readonly List<AsteroidState> asteroids = new List<AsteroidState>();

    private void OnEnable()
    {
        CacheChildren();
    }

    private void OnTransformChildrenChanged()
    {
        CacheChildren();
    }

    [ContextMenu("Rebuild Asteroid List")]
    public void CacheChildren()
    {
        asteroids.Clear();

        foreach (Transform child in transform)
        {
            if (!includeInactiveChildren && !child.gameObject.activeSelf)
                continue;

            if (randomizeInitialYRotation)
            {
                child.localRotation *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            AsteroidState state = new AsteroidState
            {
                transform = child,
                startLocalPosition = child.localPosition,
                startLocalRotation = child.localRotation,
                amplitude = new Vector3(
                    RandomSigned(xAmplitudeRange),
                    RandomSigned(yAmplitudeRange),
                    RandomSigned(zAmplitudeRange)
                ),
                frequency = new Vector3(
                    Random.Range(frequencyRange.x, frequencyRange.y),
                    Random.Range(frequencyRange.x, frequencyRange.y),
                    Random.Range(frequencyRange.x, frequencyRange.y)
                ),
                phase = new Vector3(
                    Random.Range(0f, Mathf.PI * 2f),
                    Random.Range(0f, Mathf.PI * 2f),
                    Random.Range(0f, Mathf.PI * 2f)
                ),
                rotationAxis = Random.onUnitSphere,
                rotationSpeed = Random.Range(rotationSpeedRange.x, rotationSpeedRange.y)
            };

            if (state.rotationAxis.sqrMagnitude < 0.0001f)
                state.rotationAxis = Vector3.up;

            asteroids.Add(state);
        }
    }

    private void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;

        foreach (AsteroidState asteroid in asteroids)
        {
            if (asteroid.transform == null)
                continue;

            Vector3 offset = new Vector3(
                Mathf.Sin(t * asteroid.frequency.x + asteroid.phase.x) * asteroid.amplitude.x,
                Mathf.Sin(t * asteroid.frequency.y + asteroid.phase.y) * asteroid.amplitude.y,
                Mathf.Sin(t * asteroid.frequency.z + asteroid.phase.z) * asteroid.amplitude.z
            );

            asteroid.transform.localPosition = asteroid.startLocalPosition + offset;
            asteroid.transform.localRotation =
                asteroid.startLocalRotation *
                Quaternion.AngleAxis(t * asteroid.rotationSpeed, asteroid.rotationAxis);
        }
    }

    private static float RandomSigned(Vector2 range)
    {
        float value = Random.Range(range.x, range.y);
        return Random.value < 0.5f ? -value : value;
    }
}