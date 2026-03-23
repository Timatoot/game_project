using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class LevelFinish : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private GameObject endLevelScreen;

    [Header("Key Requirement")]
    [SerializeField] private bool autoCountKeysInScene = true;
    [SerializeField] private int requiredKeyCount = 0;

    [Header("Interact")]
    [SerializeField] private string interactPrompt = "Press F to finish";

    [Header("Visuals")]
    [SerializeField] private Color lockedColor = new Color(1f, 0.15f, 0.15f, 0.22f);
    [SerializeField] private Color readyColor = new Color(0.15f, 1f, 0.35f, 0.22f);
    [SerializeField] private float emissionStrength = 4f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float idleEmissionMultiplier = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onFinish;

    private PlayerInventory playerInventory;
    private GravityInput playerInput;
    private PlayerInput unityPlayerInput;
    private PauseMenuController pauseMenuController;
    private Material runtimeMaterial;
    private bool playerInside;
    private bool finished;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProp = Shader.PropertyToID("_Color");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        var trigger = GetComponent<Collider>();
        trigger.isTrigger = true;

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer != null)
            runtimeMaterial = targetRenderer.material;

        CacheRefs();

        if (autoCountKeysInScene)
            requiredKeyCount = CountUniqueKeysInScene();

        if (debugLogs)
        {
            Debug.Log($"[LevelFinish] Awake. requiredKeyCount={requiredKeyCount}, autoCount={autoCountKeysInScene}, rendererFound={(targetRenderer != null)}", this);

            if (requiredKeyCount == 0)
                Debug.LogWarning("[LevelFinish] requiredKeyCount resolved to 0. This usually means your scene keys have blank keyID values.", this);
        }

        ApplyVisual(HasAllKeys());
    }

    private void Update()
    {
        if (playerInventory == null || playerInput == null || unityPlayerInput == null || pauseMenuController == null)
            CacheRefs();

        bool canFinish = HasAllKeys();
        ApplyVisual(canFinish);

        if (!playerInside || finished || playerInput == null)
            return;

        if (playerInput.InteractPressed)
        {
            if (canFinish)
            {
                if (debugLogs) Debug.Log("[LevelFinish] Interact pressed and finish is unlocked.", this);
                FinishLevel();
            }
            else
            {
                Debug.Log($"[LevelFinish] Finish locked: {GetCollectedKeyCount()}/{requiredKeyCount} keys collected.", this);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (finished) return;

        var controller = other.GetComponentInParent<PlayerGravityController>();
        if (controller == null) return;

        playerInside = true;
        CacheRefs();

        if (debugLogs)
            Debug.Log($"[LevelFinish] Player entered trigger. {interactPrompt}", this);
    }

    private void OnTriggerExit(Collider other)
    {
        var controller = other.GetComponentInParent<PlayerGravityController>();
        if (controller == null) return;

        playerInside = false;

        if (debugLogs)
            Debug.Log("[LevelFinish] Player exited trigger.", this);
    }

    private void CacheRefs()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();

        if (playerInput == null)
            playerInput = FindFirstObjectByType<GravityInput>();

        if (unityPlayerInput == null)
            unityPlayerInput = FindFirstObjectByType<PlayerInput>();

        if (pauseMenuController == null)
            pauseMenuController = FindFirstObjectByType<PauseMenuController>();
    }

    private bool HasAllKeys()
    {
        if (requiredKeyCount <= 0) return true;
        return GetCollectedKeyCount() >= requiredKeyCount;
    }

    private int GetCollectedKeyCount()
    {
        return playerInventory != null ? playerInventory.CollectedKeys.Count : 0;
    }

    private int CountUniqueKeysInScene()
    {
        var keys = FindObjectsByType<KeyItem>(FindObjectsSortMode.None);
        HashSet<string> uniqueKeys = new();

        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key.keyID))
                uniqueKeys.Add(key.keyID);
        }

        return uniqueKeys.Count;
    }

    private void ApplyVisual(bool canFinish)
    {
        if (runtimeMaterial == null) return;

        Color stateColor = canFinish ? readyColor : lockedColor;
        float pulse = playerInside
            ? (0.65f + Mathf.Sin(Time.time * pulseSpeed) * 0.35f)
            : idleEmissionMultiplier;

        Color emission = stateColor * emissionStrength * Mathf.Max(0f, pulse);

        if (runtimeMaterial.HasProperty(BaseColor))
            runtimeMaterial.SetColor(BaseColor, stateColor);
        else if (runtimeMaterial.HasProperty(ColorProp))
            runtimeMaterial.SetColor(ColorProp, stateColor);

        if (runtimeMaterial.HasProperty(EmissionColor))
        {
            runtimeMaterial.EnableKeyword("_EMISSION");
            runtimeMaterial.SetColor(EmissionColor, emission);
        }
    }

    private void FinishLevel()
    {
        finished = true;
        Debug.Log("[LevelFinish] Level complete.", this);

        if (pauseMenuController != null)
        {
            pauseMenuController.ShowEndLevelMenu();
        }
        else
        {
            if (endLevelScreen != null)
                endLevelScreen.SetActive(true);

            Time.timeScale = 0f;

            if (unityPlayerInput != null)
                unityPlayerInput.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        onFinish?.Invoke();
    }
}
