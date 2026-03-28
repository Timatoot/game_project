using UnityEngine;

public class AsteroidBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    public float minSpeed = 50f;
    public float maxSpeed = 150f;
    
    [Header("Aim Settings")]
    public float centerVariance = 100f; 

    private float speed;
    private Vector3 moveDirection;

    void Start()
    {
        speed = Random.Range(minSpeed, maxSpeed);
        
        // Aim at (0,0,0) in LOCAL Canvas space (dead center), plus the variance
        Vector3 targetLocalPos = new Vector3(Random.Range(-centerVariance, centerVariance), Random.Range(-centerVariance, centerVariance), 0f);
        
        // Calculate direction using localPosition
        moveDirection = (targetLocalPos - transform.localPosition).normalized;

        Destroy(gameObject, 40f);
    }

    void Update()
    {
        // Move forward using localPosition
        transform.localPosition += moveDirection * speed * Time.deltaTime;
    }
}