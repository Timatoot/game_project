using UnityEngine;
using UnityEngine.UI;

public class CrosshairAimFeedback : MonoBehaviour
{
    public Camera cam;
    public float maxDistance = 100f;
    public LayerMask mask = ~0;

    private Image img;

    void Awake()
    {
        img = GetComponent<Image>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (cam == null || img == null) return;
        bool hit = Physics.Raycast(cam.transform.position, cam.transform.forward, maxDistance, mask, QueryTriggerInteraction.Ignore);
        img.enabled = true;
        img.color = hit ? Color.white : new Color(1f, 1f, 1f, 0.25f);
    }
}
