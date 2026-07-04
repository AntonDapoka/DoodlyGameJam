# DoodlyGameJam — Agent Guide

> This file is written for AI coding agents who need to understand and modify the project. The project is a Unity game built for a game jam. The codebase is actively being refactored, so treat any summary here as a snapshot of the working tree.

## Project Overview

- **Project name:** DoodlyGameJam
- **Product name:** DoodlyGameJam
- **Company:** DefaultCompany
- **Version:** 0.1
- **Unity version:** 2022.3.62f3 (LTS)
- **Project template:** `com.unity.template.3d@8.1.3`
- **Active scene:** `Assets/Scenes/CharacterPlayground.unity`
- **Repository:** `https://github.com/AntonDapika/DoodlyGameJam.git`
- **Current branch:** `main`

This is a 3D skateboarding/graffiti game prototype. The player skates around a level, performs tricks, tags graffiti spots, and competes against an AI opponent who also reclaims graffiti spots. The project is mid-refactor: the old flat folder layout was replaced by feature-based folders, and the player movement system is being rebuilt around a modular `Rigidbody`-based architecture.

## Technology Stack

- **Engine:** Unity 2022.3.62f3 LTS
- **Render pipeline:** Universal Render Pipeline (URP) 14.0.12
- **Color space:** Linear
- **Scripting backend:** Mono (default for StandaloneWindows64)
- **API compatibility:** .NET Standard 2.1
- **C# language version:** 9.0
- **Input:** Legacy Input Manager only (`Active Input Handler: 0`)
- **Physics:** Built-in PhysX with a custom `Rigidbody`-based skateboard controller
- **Version control:** Git with visible `.meta` files and Force Text serialization

### Major Unity packages

| Package | Version | Purpose |
|---|---|---|
| `com.unity.render-pipelines.universal` | 14.0.12 | URP rendering |
| `com.unity.probuilder` | 5.2.4 | In-editor level geometry |
| `com.unity.textmeshpro` | 3.0.7 | Text rendering |
| `com.unity.ai.navigation` | 1.1.6 | NavMesh navigation |
| `com.unity.visualscripting` | 1.9.4 | Visual scripting support |
| `com.unity.test-framework` | 1.1.33 | Installed only as a dependency; unused |
| `com.unity.ads` / `com.unity.analytics` | 4.4.2 / 3.8.1 | Present but disabled in UnityConnectSettings |

### Third-party assets in `Assets/`

- **KinematicCharacterController** — full motor + examples/walkthroughs. Imported but currently unused by custom gameplay.
- **INab Studio — Advanced Edge Detection** — post-processing render-feature scripts for Built-in and URP.
- **MerryYellow Code Assist** — local editor package at `Packages/com.merry-yellow.code-assist/`.
- **Gilzoide Gesture Recognizers** — separate `Gilzoide.GestureRecognizers.csproj` present.
- **TextMesh Pro Examples & Extras** — demo scripts included in compilation.

## Project Structure

```
DoodlyGameJam/
├── Assets/
│   ├── Animation/
│   ├── INab Studio/                 # Third-party post-processing
│   ├── KinematicCharacterController/ # Third-party character controller
│   ├── Materials/                   # ~14 materials (Basic, Bricks, Doodle, Road, ...)
│   ├── Music/
│   ├── Prefabs/                     # Only SprayPaintIcon.prefab
│   ├── ProBuilder Data/
│   ├── Resources/                   # Only BillingMode.json
│   ├── Scenes/
│   │   ├── Anton's Scene.unity
│   │   └── CharacterPlayground.unity   <-- Active dev scene
│   ├── Scripts/                     # All custom gameplay code
│   ├── Shaders/
│   ├── Sounds/
│   ├── Sprites/
│   └── TextMesh Pro/                # TMP examples & extras
├── Packages/
│   ├── com.merry-yellow.code-assist/
│   ├── manifest.json
│   └── packages-lock.json
├── ProjectSettings/
├── .vscode/
├── .vsconfig
├── .gitignore
├── AGENTS.md
└── DoodlyGameJam.sln
```

## Code Organization

All custom gameplay scripts live under `Assets/Scripts/`. There are no Assembly Definition files (`.asmdef`); everything compiles into the single default `Assembly-CSharp` assembly.

The project is transitioning from a flat folder layout to a feature-based layout. The current structure is:

```
Assets/Scripts/
├── GraffitiSystem/          # Territory-tagging game loop
│   ├── Interactor/          # GraffitiScript, management, finders, hints
│   ├── Main/                # GraffitiInitializerScript
│   ├── Marker/              # GraffitiMarker
│   ├── Presenter/           # GraffitiPresenterScript
│   └── View/                # GraffitiViewScript
├── Main/                    # EnvironmentInitializer
├── NPC&PropsSystem/         # Reusable interactions, look-at-player, BPM pulse
│   └── IInteractable.cs     # Core interaction interface
├── OpponentSystem/          # AI opponent
│   ├── Interactor/
│   └── Marker/
└── PlayerSystems/           # Everything player-related
    ├── Controller/          # Input mapping, commands, input reader
    ├── MovementSystem/
    │   ├── Grinding/
    │   ├── Skating/         # Modular Rigidbody-based controller
    │   └── Tricks/
    ├── Score&StyleSystem/
    └── UI/                  # Compass and spray-paint HUD
```

### Main gameplay systems

- **Player input:** `ControlsCollection` defines hard-coded `KeyCode`s (W/A/S/D, Space, Q, E, LeftShift). `SkateInputControllerScript` polls input and dispatches `Command` instances (`PushForwardCommand`, `TurnCommand`, `JumpCommand`) against an `ISkateboardActor`.
- **Player movement:** `SkateboardMovementInteractorScript` is the main motor. It owns a separate physics body (`Rigidbody` + `SphereCollider`) and initializes/ticks modules in `FixedUpdate`:
  - `GroundingEvaluator` — sphere/raycast ground check.
  - `PushModule` — accelerates the Rigidbody with `ForceMode.Acceleration`.
  - `TurnModule` — yaw rotation and side-friction/drift adjustment.
  - `JumpModule` — handles jump requests with configurable force, coyote time, jump buffering, forward boost, ground-normal influence, and an optional speed-based force curve.
  - `AirControlModule` — applies airborne steering, yaw turning, enhanced fall/low-jump gravity, air drag, velocity alignment, auto-leveling, and pitch/roll visuals. Tracks air time for style scoring.
  - `DragModule` — empty `Tick` (removed from the controller; no longer instantiated).
- **Tricks:** `TrickType` enum defines `Kickflip`, `Ollie`, `ThreeSixty`. `TrickInteractorScript` is an empty stub and not wired into input.
- **Grinding:** `GrindableMarker` tags rail-capture triggers spawned by Unity Splines `Items To Instantiate`. `SplineGrindRailSetup` links markers to their `SplineContainer`. `GrindTriggerRelay` (added to the physics body at runtime) forwards trigger events to `GrindModule`. `GrindModule` latches the player onto the rail while airborne, preserves the landing orientation, moves them along the spline with configurable `landingBoost`, `grindAcceleration`, `maxGrindSpeed`, `uphillResistance`, `downhillAcceleration`, and `exitBoost`, and exits on jump or at the spline end.
- **Score/Style:** `ScoreManagementScript` awards score on W/A/S/D keydowns and drives `SprayPaintUIScript`. `StyleSystem` maintains static global multiplier/refill/increment values and reads the player's grounded/grinding state.
- **Graffiti:** `GraffitiInitializerScript` seeds initial opponent spots using a convex-hull (Jarvis march) perimeter check. `GraffitiManagementInteractorScript` tracks valid/active spots. `GraffitiScript` implements `IInteractable` for player reclaiming.
- **Opponent:** `OpponentInteractorScript` picks target graffiti spots and moves toward them to convert them back to opponent graffiti.
- **Interaction:** `ObjectInteractionScript` raycasts on the `E` key and calls `Interact()` on any `IInteractable`.
- **Compass HUD:** `CompassUIScript` tracks world targets marked with `GraffitiMarker` or `OpponentMarker`.

## Build and Run

### How to open the project

1. Install Unity Editor **2022.3.62f3**.
2. Open the project root folder in Unity Hub.
3. Open the active scene: `Assets/Scenes/CharacterPlayground.unity`.

### Build settings

- **Default build target:** StandaloneWindows64
- **Default resolution:** 1920 × 1080
- **Fullscreen mode:** Exclusive fullscreen (fullscreen switching allowed)
- **Quality level:** Ultra for Standalone

### Critical build issue

`ProjectSettings/EditorBuildSettings.asset` currently has **no scenes in the build list** (`m_Scenes: []`). Before producing a player build, you must add at least one scene via `File > Build Settings` in the Unity Editor. The active development scene is `CharacterPlayground.unity`; `Anton's Scene.unity` is a larger level that may be the intended main level.

### Build command

There is no CI/CD or command-line build script in the repository. Builds are produced manually through the Unity Editor (`File > Build Settings > Build`).

## Runtime Architecture

- **MonoBehaviour-heavy component architecture:** Systems are wired together via `[SerializeField]` references in the Inspector.
- **Command pattern for input:** `SkateInputControllerScript` creates `Command` objects and executes them on an `ISkateboardActor`.
- **MVP-ish naming:** The Graffiti and Opponent systems use `Interactor`/`Presenter`/`View`/`Marker` folders, but classes still inherit from `MonoBehaviour` and are coupled through serialized references rather than abstract interfaces.
- **Marker pattern:** Empty marker components (`GraffitiMarker`, `OpponentMarker`, `GrindableMarker`) tag GameObjects for queries and the compass UI.
- **Static globals:** `StyleSystem` exposes `public static` values for multiplier/refill/increment, which are shared globally.
- **Legacy input only:** No Input System package; all input is polled via `Input.GetKey` / `Input.GetKeyDown` and legacy axes.

## Development Conventions

### File and folder conventions

- Scripts are grouped by feature/system under `Assets/Scripts/`.
- Most gameplay MonoBehaviours end with `Script` (e.g., `GraffitiScript`, `SkateboardMovementInteractorScript`).
- System classes often use suffixes `InteractorScript`, `PresenterScript`, `ViewScript`, `System`, `Manager`, `Module`, or `Collection`.
- Prefabs are sparse; only one project prefab exists: `Assets/Prefabs/SprayPaintIcon.prefab`.
- Unity uses **Force Text serialization** and **Visible Meta Files**.

### C# style

- **No namespaces are used**; almost all project types live in the global namespace. The only exception is `GrindingScript`, which is inside `namespace Assets.Scripts`.
- **Naming is inconsistent:**
  - Classes use `PascalCase`.
  - Some serialized fields use `camelCase` without underscore prefix; others use `_camelCase` with underscore prefix.
  - Some `void Awake()` / `void Update()` omit `private`; others include it.
- **Access modifiers are inconsistent:** some serialized fields are `private [SerializeField]`, others are `public`.
- **Comments are sparse and informal.** Several TODOs and dead-code blocks exist.
- No `.editorconfig` is present, so formatting is not enforced automatically.

### Input key map (from `ControlsCollection`)

| Action | Key |
|---|---|
| Forward | W |
| Backward | S |
| Left | A |
| Right | D |
| Jump | Space |
| Trick 1 | Q |
| Trick 2 / Interact | E |
| Shift | LeftShift |

## Testing Instructions

- **There is no automated testing in this project.**
- `com.unity.test-framework` is installed only as a dependency of `com.unity.feature.development`; there are no `Tests/` folders, no `[Test]` fixtures, and no `EditMode`/`PlayMode` test assemblies.
- The only file matching `*Test*.cs` is third-party example code: `KinematicCharacterController/Examples/Scripts/StressTestManager.cs`.
- Manual testing workflow: open `CharacterPlayground.unity`, enter Play Mode, and test skateboard movement, tricks, graffiti tagging, and opponent behavior.

## Security Considerations

- No `.env`, `secrets.json`, credentials, or API keys were found in project scripts.
- `Assets/Resources/BillingMode.json` only contains `{"androidStore":"GooglePlay"}` and is benign.
- No network code, web requests, or cloud credentials were found in project scripts.
- Analytics, Ads, Crash Reporting, and Performance Reporting are all disabled in `ProjectSettings/UnityConnectSettings.asset`.
- `.gitignore` correctly excludes build artifacts, `Library/`, `Temp/`, `Logs/`, `UserSettings/`, and IDE caches.
- Some `Debug.Log` calls contain informal or inappropriate messages; review and clean these before release.

## Known Issues and Warnings

1. **Build list is empty** — no scenes are registered in `EditorBuildSettings`. This blocks player builds until scenes are added manually.
2. **Refactor in progress** — the working tree has many deleted/renamed files. Old flat folders were replaced by feature folders, and some stale references may remain.
3. **Stale/broken references:**
   - `StyleSystem` references `SkateboardMovementInteractorScript` via a serialized field named `playerScript`; if unassigned it will throw at runtime.
   - `ScoreManagementScript` adds score on any W/A/S/D keydown rather than on actual gameplay events.
4. **Input timing issue:** `SkateInputControllerScript` mixes immediate `Input.GetKeyDown` checks with per-frame command execution. `PushModule` also reads `Input.GetAxisRaw` internally, mixing input responsibilities.
5. **Unimplemented systems:**
   - `TrickInteractorScript`, `GraffitiHintScript`, and `GrindingScript` are empty or near-empty stubs.
   - `DragModule` is no longer used; it was removed from `SkateboardMovementInteractorScript`.
6. **Unused assets:** `KinematicCharacterController` is imported but not referenced by custom gameplay.
7. **Inappropriate debug output:** `GraffitiJarvisAlgorithmFinderScript` and `CompassUIScript` contain informal/offensive `Debug.Log` messages that should be removed before release.
8. **No CI/CD:** All builds are manual.

---

<!-- UNITY CODE ASSIST INSTRUCTIONS START -->
- Project name: DoodlyGameJam
- Unity version: Unity 2022.3.62f3
- Active scene:
  - Name: CharacterPlayground
  - Tags:
    - Untagged, Respawn, Finish, EditorOnly, MainCamera, Player, GameController
  - Layers:
    - Default, TransparentFX, Ignore Raycast, Ground, Water, UI
- Active game object:
  - Name: Graffiti Holder
  - Tag: Untagged
  - Layer: Default
<!-- UNITY CODE ASSIST INSTRUCTIONS END -->