using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject endLevelMenuRoot;

    [Header("Gameplay References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private MonoBehaviour[] disableWhilePaused;

    [Header("Input")]
    [SerializeField] private Key pauseKey = Key.Escape;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorWhilePlaying = true;

    private bool isPaused;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (playerInput == null)
            playerInput = FindFirstObjectByType<PlayerInput>();

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (endLevelMenuRoot != null)
            endLevelMenuRoot.SetActive(false);

        SetPaused(false, force: true);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (!Keyboard.current[pauseKey].wasPressedThisFrame)
            return;

        if (endLevelMenuRoot != null && endLevelMenuRoot.activeInHierarchy)
            return;

        TogglePause();
    }

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void PauseGame()
    {
        SetPaused(true);
    }

    public void ShowEndLevelMenu()
    {
        isPaused = true;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (endLevelMenuRoot != null)
            endLevelMenuRoot.SetActive(true);

        Time.timeScale = 0f;

        if (playerInput != null)
            playerInput.enabled = false;

        if (disableWhilePaused != null)
        {
            foreach (var behaviour in disableWhilePaused)
            {
                if (behaviour != null)
                    behaviour.enabled = false;
            }
        }

        ApplyCursorState(true);
    }

    private void SetPaused(bool paused, bool force = false)
    {
        if (!force && isPaused == paused)
            return;

        isPaused = paused;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(paused);

        if (!paused && endLevelMenuRoot != null)
            endLevelMenuRoot.SetActive(false);

        Time.timeScale = paused ? 0f : 1f;

        if (playerInput != null)
            playerInput.enabled = !paused;

        if (disableWhilePaused != null)
        {
            foreach (var behaviour in disableWhilePaused)
            {
                if (behaviour != null)
                    behaviour.enabled = !paused;
            }
        }

        ApplyCursorState(paused);
    }

    private void ApplyCursorState(bool menuOpen)
    {
        if (menuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (lockCursorWhilePlaying)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnDisable()
    {
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }
}
