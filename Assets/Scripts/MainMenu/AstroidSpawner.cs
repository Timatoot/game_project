using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Asteroid Setup")]
    public GameObject[] asteroidPrefabs; 
    public float spawnInterval = 2f;
    public Transform canvasParent; 

    [Header("Off-Screen Rectangle Boundaries")]
    public float offScreenX = 1050f; 
    public float offScreenY = 600f;  

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnAsteroid();
            timer = 0f; 
        }
    }

    void SpawnAsteroid()
    {
        if (asteroidPrefabs.Length == 0 || canvasParent == null) return;

        int randomIndex = Random.Range(0, asteroidPrefabs.Length);
        GameObject selectedAsteroid = asteroidPrefabs[randomIndex];

        Vector3 spawnLocalPos = Vector3.zero;
        int side = Random.Range(0, 4);

        if (side == 0) // TOP
        {
            spawnLocalPos.x = Random.Range(-offScreenX, offScreenX);
            spawnLocalPos.y = offScreenY;
        }
        else if (side == 1) // BOTTOM
        {
            spawnLocalPos.x = Random.Range(-offScreenX, offScreenX);
            spawnLocalPos.y = -offScreenY;
        }
        else if (side == 2) // LEFT
        {
            spawnLocalPos.x = -offScreenX;
            spawnLocalPos.y = Random.Range(-offScreenY, offScreenY);
        }
        else if (side == 3) // RIGHT
        {
            spawnLocalPos.x = offScreenX;
            spawnLocalPos.y = Random.Range(-offScreenY, offScreenY);
        }

        // 1. Spawn it temporarily at zero
        GameObject newAsteroid = Instantiate(selectedAsteroid, Vector3.zero, Quaternion.identity);
        
        // 2. Put it in the canvas (false ensures it stays flat in the UI)
        newAsteroid.transform.SetParent(canvasParent, false);
        
        // 3. Apply the off-screen starting position relative to the Canvas center!
        newAsteroid.transform.localPosition = spawnLocalPos;
    }
}