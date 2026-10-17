# Repository Instructions

This file is the source of truth for AI agents and contributors working in this repository.

## Project

**Night Courier: Rule 13** — a first-person, single-player night-courier job game for Windows/Steam on Unity 6000.6 (URP, Input System). The player prepares and loads anomalous cargo, drives an arcade van on short road segments (road tier, after the MVP), and delivers on foot. Packages carry handling rules; breaking them has consequences.

The main goal of this project is to **exercise the existing `UnityTemplates` library (Modules and Mechanics) in a real game**. The game is built by composing those packages, not by creating a second framework on top of them.

Read before working:

1. [Docs/GDD.md](Docs/GDD.md) — game design.
2. [Docs/Template-Mapping.md](Docs/Template-Mapping.md) — which template owns each feature.
3. [Docs/Production-Plan.md](Docs/Production-Plan.md) — phases, constraints, definition of done, open decisions.
4. [Docs/Asset-Plan.md](Docs/Asset-Plan.md) — placeholder-first art plan and prefab rules.
5. [TEMPLATE-PACKAGES.md](TEMPLATE-PACKAGES.md) — how the template packages are linked.

## Communication

- Converse with the user in Russian.
- **Everything written into the repository is English**: documents, code identifiers, localization source strings, UI text, asset names, commit messages, and PR descriptions.
- Prefer the simplest production-quality solution that satisfies a demonstrated need. State material assumptions and trade-offs; do not hide uncertainty behind abstractions.
- Do not report a feature as working until it was actually run (see Verification).

## Template-First Rule (Most Important)

Before writing any game code for a feature:

1. Look the feature up in [Docs/Template-Mapping.md](Docs/Template-Mapping.md).
2. Read the owning template's `README.md`, public runtime API, and demo scene in `../UnityTemplates`.
3. Use the template through composition. Add only the glue and game-specific policy in `Assets/Game`.
4. If the feature is not mapped, search all installed `Modules` and `Mechanics` for an existing owner first. Add the new mapping to the file in the same change.

Use the whole library across the game, but **only where it fits**. Never force a module into a role to claim coverage if it damages the architecture. Items marked *Not planned* or *N/A* in the mapping stay unused until the design changes.

### Never add (unless the library genuinely lacks the responsibility and the need is proven)

```text
GameManager · GlobalServiceLocator · a new EventBus · a new Inventory · a new Health system
a new Quest system · a new Reward system · a new Save system · a new StateMachine framework
a universal Weapon framework · a universal Interaction framework · a universal AI framework
SceneManager wrappers · UIManager · SaveManager
```

### Modifying templates

- `UnityTemplates` is linked live (see [TEMPLATE-PACKAGES.md](TEMPLATE-PACKAGES.md)); edits there affect every consumer.
- Change a template only for a proven reusable-library deficiency that composition cannot solve. First state the minimal reason to the user and get agreement, then make the smallest API change **in the `UnityTemplates` repository**, following its `AGENTS.md`, and regenerate its package descriptions (`python Publish/generate_upm.py`).
- Never copy a template into `Assets/Game` or work around a deficiency with a game-side duplicate.
- Do not add game-specific code to `UnityTemplates`.

### Pending mechanics

Ten mechanics are not yet in the library: **HoldInteraction** (highest priority), ConveyorBelt, ThrowableObject, ForceZone, FallingPlatform, RotatingPlatform, DisappearingPlatform, PhysicsButton, Pendulum, RopeSwing.

- Do not implement, stub, or clone them. Their reserved roles are in [Docs/Template-Mapping.md](Docs/Template-Mapping.md) §4.
- Design content to function without them (instant interaction through existing mechanics where HoldInteraction is planned).
- When the user announces they were added: run *Tools ▸ Unity Templates ▸ Install All Local Packages*, read the new README and demo, update the mapping, then wire them in.

### Not applicable

- **Haptics** is Android/iOS only; this project targets Windows/Steam. Do not use it.

### Library gaps and newly added mechanics

- **Vehicle driving has no template owner.** Do not start a vehicle controller on your own. Ownership (a minimal controller in `Assets/Game/Vehicle`, or a new mechanic in `UnityTemplates`) is an open decision for the user ([Docs/Production-Plan.md](Docs/Production-Plan.md)). Whatever the controller is, it stays arcade; vehicle health, stats, equipment, and input come from the existing modules.
- The library already contains mechanics that are not linked into this project (`PushPullObject`, `ResponsiveJump`, `Ladder`, `LedgeGrabMantle`, `Zipline`, `GrapplingHook`, `WallJump`, `Dash`). Check [Docs/Template-Mapping.md](Docs/Template-Mapping.md) §3.7 for their planned roles, link a package only when it is first needed, and never recreate one locally.
- `Door` slides only. Do not fake hinged doors with a custom door script.

## Game Layer

- All game code and content live under `Assets/Game/`. Create folders when first needed (`Application`, `Contracts`, `Packages`, `Player`, `AI`, `World`, `UI`, `Presentation`, `Content`, `Demo`).
- Namespace root `NightCourier`; assembly definitions named `NightCourier.*`. The `RD` prefix belongs to the template library only.
- Game assemblies reference template assemblies explicitly. Templates never reference game code.
- **No singletons and no service locator.** The Startup pipeline builds application services; they are passed explicitly to scene entry points. Scene-local initialization stays in its scene.
- Separate stable definitions (data) from runtime state. No MonoBehaviour per contract, no giant universal ScriptableObject.
- A package is never one class holding all rules: one small component per genuinely reusable rule.
- Use `Events` only for a few decoupled facts (`ContractCompleted`, `PackageViolation`, `PlayerRespawned`). Commands and queries stay direct calls.
- Do not rewrite template mechanics' internal logic to force a different module usage (for example, leave AI state logic to the AI mechanics).
- Do not extend a reusable mechanic to serve one contract or feature (for example, a sound stimulus in `DetectionSensor`, extra systems in `WeaponSpread`, or variants inside `Explosion`). Layer game-specific reactions over the mechanic's existing events; list them in [Docs/Template-Mapping.md](Docs/Template-Mapping.md) §6. If a template change is truly required, follow "Modifying templates".
- **Combination test:** before adding content or a system, check whether the situation can be produced by combining existing mechanics, states, and rules ([Docs/GDD.md](Docs/GDD.md) §22). Prefer a new combination over a new system.
- Build features at their tier only ([Docs/GDD.md](Docs/GDD.md) §3.2). Do not implement Demo/1.0 systems while working on the MVP, but do not model data in a way that blocks them.
- Prefer simple primitives and placeholder materials over waiting for art (see Visuals and Assets).

## Visuals and Assets

The whole vertical slice is built on Unity primitives. Details and the future asset list are in [Docs/Asset-Plan.md](Docs/Asset-Plan.md).

> All gameplay prefabs must support placeholder visuals and later art replacement without changing gameplay code. Keep gameplay components, colliders, interaction logic, and important anchors on stable root objects. Put replaceable art under a dedicated `Visual` child.

- Runtime code must never depend on mesh names, renderer hierarchy, material names, bone names, or asset-specific child names, unless the dependency genuinely belongs to animation or visual presentation.
- Important positions are explicit anchors: `Muzzle`, `InteractionPoint`, `CargoPoint`, `DriverSeat`, `CameraPoint`, `WheelPoints`, `EffectPoint`, `TargetPoint`, `IndicatorPoint`.
- Moving parts (doors, levers, platforms, conveyors) are separate objects with correct pivots. Breakable props have an intact visual and an inactive broken visual with fragments.
- Materials and shaders on hit-reactive objects must expose `_BaseColor` (or legacy `_Color`), because the hit flash writes a `MaterialPropertyBlock`.
- Name every placeholder object and prefab with the prefix `PH_`.
- An interactable's highlight is a separate emissive accent or outline, never its base color.
- **Only free assets are used; never purchase anything.** Do not import or download third-party assets without the user's explicit instruction. When told to, accept only licenses that permit commercial use (CC0, CC BY, Asset Store free-asset license); reject NonCommercial, NoDerivatives, unclear, or missing licenses. Put packs under `Assets/ThirdParty/<PackName>/`, build game variants under `Assets/Game/`, and record each asset in the register in [Docs/Asset-Plan.md](Docs/Asset-Plan.md) (section 6.4).
- Player-facing signs and labels are made as textures or UI so they stay English and localizable.

## Code Conventions

Match the surrounding code and the template library's style:

- Unity 6 APIs. Use `Awaitable` for asynchronous code; do not introduce `Task` in new runtime code. `async void` only for Unity lifecycle or UI callbacks, with exceptions caught at that boundary.
- Allman braces, tabs for indentation, `PascalCase` types and members, `_camelCase` private fields.
- **No code comments or XML documentation.** Express intent through names, types, and validation.
- Log through Foundation's `UnityLogger`, never `Debug.Log*`.
- Name persistent ids, UI element names, timings, capacities, thresholds, and persistence keys with constants.
- Read input only through Input Actions; never poll `Keyboard.current` or other devices in production gameplay.
- Never use `FindObjectOfType`, `FindObjectsOfType`, `FindObjectsByType`, or `GameObject.Find` in `Update`, `LateUpdate`, or `FixedUpdate`. Cache references or use events.
- Use Tween for animations of doors, UI, and lights rather than per-object lerp code.
- Use `UnityEngine.Pool` for frequently spawned objects where it is a measurable benefit.
- Avoid per-frame allocations in hot paths.

## Text and Localization

- All player-facing text is English and goes through the Localization module via stable keys. No hardcoded UI strings.
- English is the only source language until the localization phase; extra languages are added later through the module.
- Do not write Cyrillic or other non-English text into code, assets, or docs.

## Unity Workflow and Verification

- Inspect `git status` first and preserve unrelated or pre-existing changes.
- Use the Unity MCP tooling to compile, read the Console, open scenes, enter Play Mode, and run tests. Check the Console for compile errors after every script change.
- Verify file existence before writing. Preserve GUIDs and `.meta` files when moving or renaming assets; never regenerate them without a reason.
- After each phase: compile, run relevant tests, open the scenes, enter Play Mode, inspect the Console, verify references, Input, Save, and scene transitions.
- A scene counts as working only when it was run in Play Mode and the relevant flow was exercised. Compiling is not enough. State plainly what was not verified.
- Keep Build Settings scene paths valid.
- Do not claim Play Mode coverage that was not actually run.

## Git

- Work on the current branch unless told otherwise; commit and push only when asked.
- Keep changes scoped. Do not modify unrelated files, `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or generated `*.csproj`/`*.sln` files.
- `Packages/manifest.json` and `Packages/packages-lock.json` are committed in this repository; changes to package descriptions themselves are committed in `UnityTemplates`.
- Update docs in the same change when behavior, mapping, or scope changes.

## Scope Discipline

- Do not proceed to additional districts until the MVP loop is actually fun.
- The road tier (drivable van, loading, garage, routes, multi-delivery shifts) starts only after the MVP definition of done is met. Until then the van is a scene transition, though a shift is already modeled as a list of contracts.
- Hard limits (see [Docs/GDD.md](Docs/GDD.md) §3): no open-world city, traffic AI, pedestrians, second vehicle, cosmetic vehicle customization, realistic car simulation, day/night simulation, police system, fuel stations, or large garage system.
- Each contract uses roughly 5–10 mechanics; do not push every mechanic into every contract.
- MVP content targets are in [Docs/GDD.md](Docs/GDD.md) §3.1. Do not exceed them without the user's decision.
