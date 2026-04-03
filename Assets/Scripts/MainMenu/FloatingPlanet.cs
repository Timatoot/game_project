using UnityEngine;

public class FloatingPlanet : MonoBehaviour
{
    public float floatSpeed = 1f; // How fast it bobs
    public float floatDistance = 10f; // How far it moves

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Calculates a new Y position using a Sine wave
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed)/12 * floatDistance;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}