# Production Plan

Delivery plan for the Night Courier vertical slice. Design lives in [GDD.md](GDD.md); template ownership lives in [Template-Mapping.md](Template-Mapping.md); placeholder and asset planning lives in [Asset-Plan.md](Asset-Plan.md); working rules live in [../AGENTS.md](../AGENTS.md).

**Brief:** build a small, production-quality vertical slice on top of the existing `UnityTemplates` library. This is not a new framework. The task is to test the existing `Modules` and `Mechanics` in a real game, reusing the current architecture as much as possible. Do not change existing Modules/Mechanics if the game can be built by composition. If a change is genuinely required, first identify the minimal reason, then change the existing API minimally (in the `UnityTemplates` repository).

**Do not proceed to additional districts until the full MVP loop is fun.**

---

## Strict Architecture Constraints

Do not add any of the following unless the library genuinely lacks the responsibility and the need is proven:

```text
GameManager · GlobalServiceLocator · a new EventBus · a new Inventory · a new Health system
a new Quest system · a new Reward system · a new Save system · a new StateMachine framework
a universal Weapon framework · a universal Interaction framework · a universal AI framework
```

Also do not create `SceneManager` wrappers, `UIManager`, or `SaveManager`. Prefer composition. Game-specific orchestration belongs in `Assets/Game`, not in reusable library code.

---

## Phases

### Phase 0 — Audit

Before writing game code, inspect all installed `Modules/` and `Mechanics/` and keep the feature → template mapping current ([Template-Mapping.md](Template-Mapping.md)). Pay particular attention to:

```text
Application: Addressables, Analytics, Input, Localization, Pause, Save, SceneFlow, Settings, Startup, Tutorials
Core:        Attributes, Events, Foundation, StateMachine, Timers
Gameplay:    Achievements, Conditions, Equipment, Health, Inventory, Progression, Quests, Rewards, Stats, StatusEffects, Wallet
Presentation: Audio, PrefabWindows, ScreenNavigation, Tween      (Haptics: not applicable)
```

plus every Mechanic. Do not continue until dependency reuse is understood. The first README-level pass is in the mapping file; a source-level review of each template's public API is still required before using it.

### Phase 1 — Game Structure

Create a separate game layer in `Assets/Game/`. Create folders when first needed, not in advance:

```text
Game/
├── Application/    Startup steps, composition, scene entry points
├── Contracts/      Contract definitions and runtime state
├── Packages/       Package definitions and rules
├── Player/         Player glue
├── AI/             Enemy compositions
├── World/          Level glue and configuration
├── UI/             Game-specific UI content
├── Presentation/   Feedback, lighting, audio content
├── Content/        Data assets
└── Demo/           Sandbox scenes
```

Do not put game-specific code in `UnityTemplates/Modules` or `UnityTemplates/Mechanics`.

### Phase 2 — Core Game Flow

```text
Startup → MainMenu → Depot → ContractLocation → ContractResult → Depot
```

Reuse `Startup`, `SceneFlow`, `ScreenNavigation`, `PrefabWindows`, `Save`, `Settings`, `Pause`, `Input`.

### Phase 3 — Contract Domain

A small game-specific contract model: id, title localization key, type, destination, location state, package definition, optional complication, recipient type, base reward, optional time limit, package rules, optional objectives, difficulty/risk. A contract is assembled from Package + Location + Location State + Complication ([GDD §6.5](GDD.md)). MVP supports type DELIVER, states Normal and Blackout, and no complications; a shift is modeled as a list of accepted contracts (MVP: exactly one); the model must not block the later tiers ([GDD §3.2](GDD.md)).

Lifecycle: `Available → Accepted → PackageCollected → InTransit → Delivered → Completed | Failed`.

Reuse `Quests`, `Conditions`, `Rewards`, `Wallet`, `Timers`, `StateMachine` only where their existing APIs fit naturally.

### Phase 4 — Package System

The smallest composition layer that turns existing world items into cargo. Reuse `PickupDrop`, `CarryObject`, `Health` (destructible packages), `Conditions`, `Timers`, `Events`.

A package is not a giant class containing every rule. Independent rule components exist only where each is a genuinely reusable package behavior.

Initial rules: `DoNotDrop`, `KeepInLight`, `DeliveryDeadline`. Outcomes: `RuleSatisfied`, `RuleViolated`, `ViolationCount`. One violation does not fail a contract.

Each package is modeled as three layers — Rule (what the player is told), Behavior (what the object does), Escalation (what happens after violations) — as per-package data over small components ([GDD §7.1](GDD.md)). Hands restriction while carrying large cargo is game-side glue between `CarryObject` and weapon input.

### Phase 5 — MVP Depot

One small Depot scene, primitives if final assets are unavailable. Required: Contract Terminal, Equipment Locker, Package Conveyor, Upgrade Terminal, Exit/Van. UI: available contracts, contract details, credits, courier rank, equipment. Reuse `ScreenNavigation`, `PrefabWindows`, `Tween`, `Audio`, `Localization`.

### Phase 6 — Player

Only the glue required for: movement, look, interaction, health, equipment, inventory, weapon input, carrying packages. Reuse `Input`, `Health`, `Inventory`, `Equipment`, `Stats`. No new character framework. First person; no full-body model.

### Phase 7 — MVP Warehouse

One playable Warehouse map containing: Door, ConditionLock, Lever, PressurePlate, MovingPlatform, BreakableObject, Checkpoint, PickupDrop, CarryObject. At least two routes: safer/longer and dangerous/shorter. JumpPad and Teleporter are optional later.

### Phase 8 — Enemy MVP

- **SecurityDrone:** Patrol, DetectionSensor, ChaseTargetMemory, Projectile or HitscanWeapon, Health, HitFeedback.
- **Crawler:** DetectionSensor, ChaseTargetMemory, MeleeAttack, Knockback, Health, HitFeedback.

Game code composes them. No new hard runtime dependencies between existing mechanics unless genuinely required.

### Phase 9 — Player Combat

- **Stun Pistol:** HitscanWeapon, AmmoReload, Recoil, WeaponSpread, HitFeedback.
- **Crowbar:** MeleeAttack, Knockback.

Also in the MVP: the player carries two items chosen at the Equipment Locker (`WeaponSwitching` driven by `Equipment`), and ammo use is tracked for the result screen.

Later: Shotgun, Repulsor (`ChargeWeapon` + `Projectile` + `Knockback`), HomingProjectile, Grenade, ExplosiveBarrel, DamageOverTime, DamageZone. No new weapon framework.

### Phase 10 — Economy and Progression

Contract completion produces a `Reward`; the reward grants `Wallet` currency. Minimal courier progression using `Progression`, `Stats`, `Equipment`. MVP upgrades: +MaxHealth, +CarryMoveSpeed, +ScannerRange or a weapon stat. No large skill trees.

The result screen computes the net payout: base + bonuses − property damage − ammo expense − death penalty (floored at ₡0; from the road tier also − cargo damage penalty and optional vehicle repair), plus a shift rating (delivery time, cargo condition, protocol compliance, property damage) that feeds `Progression` ([GDD §14](GDD.md)). Fines and ammo expense come from game-side listeners over existing events ([Template-Mapping.md §6](Template-Mapping.md)).

### Phase 11 — Persistence

Save: credits, rank/progression, unlocked equipment, purchased upgrades, completed contracts, collectible progress. Do not serialize arbitrary active scene physics. Checkpoint/Respawn handles temporary level death.

### Phase 12 — Full Module Integration

After the core game is fun, integrate deliberately: Analytics, Achievements, Tutorials, Localization, Audio, Addressables, StatusEffects, Collectibles. Every integration needs an actual gameplay or product purpose; never use a module just to claim coverage if that damages architecture.

### Phase 13 — Five MVP Contracts

| # | Contract | Focus |
|---|---|---|
| 01 | Standard Delivery | Learn pickup and delivery |
| 02 | Fragile | `DoNotDrop` |
| 03 | Lights Out | `KeepInLight` + power problem |
| 04 | Express | `DeliveryDeadline` + enemy route |
| 05 | Restricted Cargo | Two rules + combat + environmental puzzle |

All five use the same Warehouse location with different configuration or state. Do not create five maps.

### Phase 14 — Content Architecture

Contract content is data/config-driven where that genuinely reduces duplication. No MonoBehaviour per contract. No giant universal ScriptableObject containing every game option. Separate stable definitions from runtime state.

### Phase 15 — Presentation

Simple geometry first; never block development waiting for art. Low-poly industrial, dark, readable, limited palette, strong lighting. Interactable objects are immediately readable.

Everything is built on Unity primitives with `PH_` placeholders. **Every gameplay prefab is built from its first version with a stable root, standard anchors, and a replaceable `Visual` child**, so final art can be dropped in without touching gameplay code ([Asset-Plan.md](Asset-Plan.md) §2). No third-party assets are imported until the user decides to.

### Phase 16 — Verification (after every phase)

```text
compile · run relevant tests · open scenes · enter Play Mode · inspect Console
verify references · verify Input · verify Save · verify scene transitions
```

Use the available Unity MCP/editor tooling. Never report a scene as working merely because scripts compile.

---

## Road Tier Phases (After the MVP)

Do not start until the first-milestone definition of done below is met and the loop is fun. Each phase has its own gate; test the riskiest part first.

### Phase 17 — Road Alpha: Drive the Van

Resolve the vehicle-controller decision first (see Open Decisions). Then, on a gray-box road: an arcade van with a first-person driver's view, `Input` vehicle context, entering and exiting the van, cargo stability reacting to driving, collision hit zones, one road segment. **Test driving alone before building anything on top of it.** If arcade driving is not fun, stop and reconsider the road tier.

### Phase 18 — Loading, Preparation, and Garage

Physical cargo loading into four cargo points, securing, adjacency rules, preparation briefing with personal and van slots, Garage with persistent van condition, pre-trip faults, and the Van Inspection screen. Reuse `PickupDrop`, `CarryObject`, `PushPullObject`, `Equipment`, `Inventory`, `Stats`, `Health`, `Wallet`, `Save`.

### Phase 19 — Routes and Road Events

2–3 road segments with route planning, authored junctions, and a small set of road events and hazards from existing mechanics, one incident at a time. Roadside stop-and-repair with instant interactions until `HoldInteraction` lands.

### Phase 20 — Van Upgrades and Economy

Three upgrade lines (Cargo Stabilizer, Headlights, Engine) up to level 3, gated by `Progression`, paid with `Wallet`, installed physically the first time. Tune repair costs against payouts.

### Phase 21 — Multi-Delivery Shifts

Shift planning with two deliveries, loading order, van-capacity choice, and shift state saved at the van. Later: 2–4 deliveries, multi-package interactions, return-trip contracts.

---

## Open Decisions

Raised for the user; agents do not settle them alone.

| Decision | Options | Notes |
|---|---|---|
| Who owns vehicle driving? | (a) minimal arcade controller in `Assets/Game/Vehicle`; (b) a new reusable mechanic in `UnityTemplates` | The library has none. (b) fits the project's purpose |
| Fuel representation | `Wallet` currency committed per road segment; `Stats` value; `Stamina` | Settle in the audit |
| Hinged vehicle/building doors | Keep `Door` slide-only; or extend the template | `Door` only slides |
| Linking the eight newly added mechanics | Install All Local Packages when first needed | See Template-Mapping §3.7 |

---

## Pending-Mechanic Schedule

Ten mechanics are not yet in the library (see [Template-Mapping.md §4](Template-Mapping.md)). Order of integration when they arrive:

1. **HoldInteraction** — first. Re-evaluate hatch, battery-connect, scan, repair, and delivery-complete interactions.
2. **ConveyorBelt**, **ThrowableObject**, **ForceZone**, **FallingPlatform** — priority 1 set.
3. **RotatingPlatform**, **DisappearingPlatform**, **PhysicsButton**, **Pendulum**, **RopeSwing** — priority 2 set, added only when a location needs them.

Until then, build everything that does not depend on them. Do not stub or clone them locally.

---

## First Milestone — Definition of Done

The first playable milestone is complete only when this full path works:

```text
New Game
  → Depot
  → select contract
  → pick up physical package
  → travel to Warehouse (van scene transition, no driving)
  → encounter locked route
  → solve interaction
  → encounter enemy
  → deliver package
  → calculate reward
  → return to Depot
  → purchase one upgrade
  → save
  → restart game
  → progression restored
```

Required:

| Item | Required |
|---|---|
| Package rules | `DoNotDrop`, `KeepInLight`, `DeliveryDeadline` |
| Enemies | `SecurityDrone`, `Crawler` |
| Contracts | 5 |
| Locations | 1 Warehouse |

---

## Per-Change Checklist

- The affected feature is listed in [Template-Mapping.md](Template-Mapping.md), and the listed template was used.
- No item from the strict-constraints list was introduced.
- All new text is English and goes through localization keys.
- Code compiles with zero errors and warnings; the Console is clean.
- The relevant scene was opened, entered in Play Mode, and exercised end to end.
- Save/load and scene transitions still work when touched.
- Docs are updated in the same change if behavior, mapping, or scope changed.
