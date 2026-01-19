using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class GravityInput : MonoBehaviour
{
    [Header("Action Map + Action Names (must match .inputactions)")]
    [SerializeField] private string actionMapName = "Gameplay";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string pullActionName = "Pull";
    [SerializeField] private string toggleViewActionName = "ToggleView";

    private PlayerInput playerInput;
    private InputAction moveAction, lookAction, jumpAction, sprintAction, pullAction, toggleViewAction;

    private bool loggedMissingActions;

    public Vector2 Move => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public Vector2 Look => lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

    public bool JumpPressed => jumpAction != null && jumpAction.WasPressedThisFrame();
    public bool SprintHeld => sprintAction != null && sprintAction.IsPressed();
    public bool PullPressed => pullAction != null && pullAction.WasPressedThisFrame();
    public bool ToggleViewPressed => toggleViewAction != null && toggleViewAction.WasPressedThisFrame();

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        CacheActions();
    }

    void OnEnable()
    {
        CacheActions();
        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        pullAction?.Enable();
        toggleViewAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        pullAction?.Disable();
        toggleViewAction?.Disable();
    }

    private void CacheActions()
    {
        if (playerInput == null) return;

        var map = playerInput.actions.FindActionMap(actionMapName, throwIfNotFound: false);
        if (map == null)
        {
            if (!loggedMissingActions)
            {
                Debug.LogWarning($"GravityInput: Action map '{actionMapName}' not found on PlayerInput.", this);
                loggedMissingActions = true;
            }
            return;
        }

        moveAction = map.FindAction(moveActionName, false);
        lookAction = map.FindAction(lookActionName, false);
        jumpAction = map.FindAction(jumpActionName, false);
        sprintAction = map.FindAction(sprintActionName, false);
        pullAction = map.FindAction(pullActionName, false);
        toggleViewAction = map.FindAction(toggleViewActionName, false);

        if (!loggedMissingActions)
        {
            if (moveAction == null || lookAction == null || jumpAction == null || sprintAction == null || pullAction == null || toggleViewAction == null)
            {
                Debug.LogWarning("GravityInput: One or more actions were not found. Check .inputactions names.", this);
                loggedMissingActions = true;
            }
        }
    }
}
