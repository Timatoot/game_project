using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CrosshairAimFeedback : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private LayerMask mask = ~0;

    [Header("Visuals")]
    [SerializeField] private Color hitColor = Color.white;
    [SerializeField] private Color noHitColor = new Color(1f, 1f, 1f, 0.25f);

    private Image img;

    void Awake()
    {
        img = GetComponent<Image>();
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (cam == null) return;

        bool hit = Physics.Raycast(
            cam.transform.position,
            cam.transform.forward,
            maxDistance,
            mask,
            QueryTriggerInteraction.Ignore);

        img.enabled = true;
        img.color = hit ? hitColor : noHitColor;
    }
}
