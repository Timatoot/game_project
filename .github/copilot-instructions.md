# GitHub Copilot Instructions (Unity 3D – Gravity-Shift Project)

This repository is a Unity 3D (Unity **6000.3**) third-person traversal game built around **dynamic gravity manipulation**.
The player can aim at a surface, “pull” toward it, and have their gravity reorient so that the target surface becomes the new ground.

Copilot: follow the rules and conventions below when generating or editing code for this project.

## Non-negotiables

1. **Do not use `CharacterController`.** Player movement is **Rigidbody + CapsuleCollider** only.
2. **Do not use Unity global gravity** (`Physics.gravity` / `rb.useGravity=true`) for the player. The project uses **custom gravity**.
3. **Root motion stays off.** Animation is **driven by physics state**, not the other way around.
4. **Keep responsibilities split.** Each script should own one job (input, movement, pull, camera, UI, animation, pickups).
5. **Physics work happens in `FixedUpdate`.** Input sampling and state toggles happen in `Update`. Camera positioning happens in `LateUpdate`.

## Current architecture (what exists today)

### Core scripts (do not break these contracts)

- **`PlayerGravityController`**
  - Owns: `playerUp`, custom gravity, grounding model, movement, jump, landing stabilization.
  - Exposes: `IsGrounded`, `GetPlayerUp()`, `SetPlayerUp(Vector3)`, events `Jumped` / `Landed`.
  - Movement is always along the plane perpendicular to `playerUp`.

- **`GravityPullGun`**
  - Owns: pull/gravity-shift ability, targeting, hop/detach, pulling forces, finalize gravity.
  - Uses: raycast selection from camera forward, damp sideways velocity, reorients gravity while airborne.
  - Must preserve: “hop before pull” behavior to avoid scraping/dragging on the old surface.

- **`GravityCameraController`**
  - Owns: stable aiming and orbit camera while gravity changes.
  - Must preserve: **no harsh snaps** during gravity shifts; stabilize on land.
  - Runs final camera positioning in `LateUpdate`.

- **`GravityInput`**
  - Wraps Unity Input System (`PlayerInput` + `.inputactions`).
  - Other scripts should read inputs **through `GravityInput`**, not by touching `InputAction` directly.

- **`PlayerAnimationDriver`**
  - Reads physics state and input to set animator parameters.
  - Uses `Jumped` / `Landed` events for immediate triggers.
  - Parameters currently in use include: `Speed`, `MoveX`, `MoveY`, `IsGrounded`, `VerticalSpeed`, `IsSprinting`, triggers `Jump`, `Land`.

### UI & pickups (current direction)

- UI is a **single Screen Space Overlay Canvas** (crosshair + HUD).
- Key pickup is `GravityKey` with a **trigger collider**, storing a `keyID` string to `PlayerInventory`.
- Inventory UI should display collected keys (prefer icons, not IDs).
- Preferred pattern for key icons is a **ScriptableObject key→sprite library** (one-time setup), so per-level you only change `keyID`.

## Coding conventions

### Style
- Prefer **explicit, readable code** over clever code.
- Use `[Header]`, `[SerializeField]` for inspector-exposed private fields.
- Use `GetComponentInParent` / `GetComponent` caching in `Awake()` like the existing scripts do.
- Avoid allocations in per-frame code:
  - Do not use LINQ in `Update`/`FixedUpdate`.
  - Avoid `new` per frame where possible.
- Use Unity’s `Rigidbody.linearVelocity` (Unity 6) to match existing code.

### Null safety
- Assume scenes can be rearranged. Always protect against missing references.
- Provide `EnsureReferences()` helpers where appropriate and call them in `Awake()`.
- If a script depends on a component, use `[RequireComponent]` when safe.

### Physics rules
- Never rotate the Rigidbody via physics constraints; rotation is manually aligned to gravity.
- Avoid direct `transform.position` changes for the player body during physics. Use Rigidbody forces/velocity and controlled alignment.
- Use `QueryTriggerInteraction.Ignore` for gameplay raycasts unless you explicitly need triggers.

## Prefabs & scene rules

### Player prefab
- Player is the “truth” object: Rigidbody + CapsuleCollider + controller scripts.
- Visual model lives under a `Visuals` child with the Animator. No physics components on the model.
- Camera and camera pivot are children of Player (third-person orbit pivot + MainCamera).
- UI Canvas can be:
  - a child of Player **if** Player is instantiated per scene, or
  - a separate `UIRoot` prefab marked `DontDestroyOnLoad` **if** Player/UI persist across scenes.
- If you introduce persistence (`DontDestroyOnLoad`), avoid duplicate Players/UIs and re-bind camera references on scene load.

### Key pickup prefab
- `GravityKey` should be a reusable prefab:
  - Root has: collider (IsTrigger), optional kinematic Rigidbody for reliable triggers, `GravityKey` script.
  - Visual as child.
- Per level, the only required change should be **`keyID`**.
- Never hardcode per-level behavior in the key prefab. Use the ID + external lookup/config.

## When implementing new features, prioritize these constraints

### Readability and feel
- Gravity shifts must remain controllable while airborne.
- Camera must remain aimable and stable; avoid sudden orientation snaps.
- Animations must reflect physics state immediately (event-driven jump/land; instant fall when ungrounded).

### Maintainability
- Prefer adding a new small script over bloating an existing one.
- Keep cross-script communication minimal:
  - Use events (`Jumped`, `Landed`, inventory events) or explicit references.
  - Avoid hard coupling UI scripts to gameplay scripts beyond a small public interface.

## Common implementation patterns (Copilot should follow)

### “Add a new HUD element”
- Put it under the existing HUD Canvas.
- Reuse the same Canvas unless you have a strong reason (performance isolation / different render mode).
- Anchor properly (top-right, center, etc.) and use Canvas Scaler “Scale With Screen Size”.

### “Show keys as images”
- Keep inventory as IDs (`string keyID`).
- UI resolves icons via a `KeyIconLibrary : ScriptableObject` or an addressable/resources lookup.
- UI updates via events (`KeyAdded`) rather than polling each frame.

### “Level-specific content”
- Level scenes should contain environment + placed pickups + objectives.
- Do not duplicate core systems per level; use prefabs (Player, UIRoot, key pickup).

## Testing expectations for any change
When you modify core scripts, verify at minimum:
- Gravity shift: pull to walls/ceilings/angled surfaces, no scrape/drag before detach.
- Landing: stable grounding; no micro-sliding off edges.
- Camera: no harsh snap mid-air; stable aim direction during gravity change.
- Animation: jump triggers immediately; falling begins when ungrounded; landing triggers on contact.

## What Copilot should NOT do
- Do not introduce `CharacterController`.
- Do not rework the system to “animation-driven movement” or root motion locomotion.
- Do not add big dependencies (new packages) without a clear reason.
- Do not refactor multiple scripts at once unless explicitly requested.
