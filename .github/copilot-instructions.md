# CISC226 Gravity Game - AI Agent Instructions

## Project Overview
A gravity-shifting platformer game built in Unity where the player can pull themselves to any surface and reorient gravity. The game explores multi-directional gravity mechanics across different planetary levels (FallingBuilding starter level, expanding to more planets).

## Architecture & Core Systems

### Player Movement System
The player controller is divided into three interconnected components:

1. **PlayerGravityController** (`Assets/Scripts/PlayerGravityController.cs`)
   - Core physics: applies custom gravity, ground detection, movement
   - Manages player's "up" direction via `SetPlayerUp(Vector3)` 
   - Publishes events: `Jumped`, `Landed` for animation/feedback
   - **Critical**: Uses `rb.useGravity = false` and applies custom gravity via `AddForce`
   - Ground detection via raycasts at capsule bottom, uses `groundedDot = 0.55f` threshold

2. **GravityInput** (`Assets/Scripts/GravityInput.cs`)
   - Input abstraction using Unity's new Input System (PlayerInput component)
   - Action map: "Gameplay" with actions: Move, Look, Jump, Sprint, Pull, ToggleView
   - Must match `.inputactions` file names exactly
   - Provides properties like `PullPressed`, `JumpPressed`, `SprintHeld` (no polling)

3. **GravityCameraController** (`Assets/Scripts/GravityCameraController.cs`)
   - Supports FirstPerson and ThirdPerson modes (toggle via ToggleView input)
   - Rotates camera with gravity (uses `playerUp` from controller)
   - Third person: orbit around player with shoulder offset, collision detection
   - Pitch clamp differs per mode (FP: -80/80°, TP: -70/70°)

### Platform Interaction System
**GravityPullGun** (`Assets/Scripts/GravityPullGun.cs`) - the "pull to platform" mechanic:
- Raycasts forward to find target surfaces (respects LayerMask `aimMask`)
- Filters out: self, current platform (if grounded), nearby same-surface targets
- **Pull behavior**: accelerates player toward target, rotates player orientation to target surface normal
- **Pull hop**: instant upward velocity before pull starts (prevents sliding)
- **Targeting feedback**: changes target GameObject layer to outline layer (7) for visual highlighting

### Event-Driven Animation
**PlayerAnimationDriver** (`Assets/Scripts/PlayerAnimationDriver.cs`):
- Subscribes to `PlayerGravityController.Jumped` and `Landed` events
- Drives animator with: Speed, VerticalSpeed, MoveX/Y, IsGrounded, IsSprinting
- Uses damped float animation parameters for smooth transitions
- **Pattern**: Direct subscription in `OnEnable`, cleanup in `OnDisable`

## Key Development Patterns

### Component Auto-Initialization
Prefab setup pattern - all scripts use defensive null-checks with fallback resolution:
```csharp
void Awake() {
    if (controller == null) controller = GetComponentInParent<PlayerGravityController>();
    if (rb == null && controller != null) rb = controller.GetComponent<Rigidbody>();
    if (input == null && controller != null) input = controller.GetComponent<GravityInput>();
}
```
**Why**: Allows flexible prefab composition and easy scene testing without full hierarchy setup.

### Physics Configuration
- Rigidbody: `useGravity = false`, `constraints = FreezeRotation`, `CollisionDetectionMode.Continuous`, `Interpolation.Interpolate`
- Capsule collider on player (automatic via `[RequireComponent]`)
- Physics gravity applied manually each frame for custom direction

### Input System Integration
- Always access input through `GravityInput` component, never `Input` class
- Input actions defined in `.inputactions` files (see `GravityActions.inputactions`)
- Properties use `WasPressedThisFrame()` for discrete actions, `IsPressed()` for held input

### Serialization & Headers
Use `[Header()]` attributes extensively to organize inspector fields by category (References, Movement, Gravity, etc.). Makes tuning on-site physics obvious and traceable.

## Workflow Notes

### Level Structure
- Scenes in `Assets/Scenes/PrototypeLevels/` (FallingBuilding, CircularPlanets, OriginalPrototypeLevel)
- New level template: create empty scene, add Camera + Directional Light, build platforms with colliders, assign to built scenes in EditorBuildSettings

### Extending Player Mechanics
1. New abilities should inherit player state (grounded, up direction) from `PlayerGravityController`
2. Add input handling to `GravityInput` (map action in both code and `.inputactions`)
3. Subscribe to controller events for feedback (Jumped, Landed)
4. Use `SetPlayerUp()` to reorient player when latching to new surface

### Physics Tuning
Most gameplay feel resides in `PlayerGravityController` floats (moveSpeed, jumpSpeed, gravityStrength). Test changes by playing in Unity editor and adjusting via Inspector.

## Tools & Dependencies
- **Input System**: new Input System (PlayerInput + InputActions)
- **Physics**: built-in Rigidbody + Colliders
- **Animation**: Animator component with parameters (not visual scripting)
- **MCP Available**: Unity MCP tools for programmatic scene/GameObject creation if batch operations needed
