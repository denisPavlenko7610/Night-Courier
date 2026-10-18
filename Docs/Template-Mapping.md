# Template Mapping

Maps every game feature to the existing `UnityTemplates` module or mechanic that owns it. This is the Phase 0 audit
deliverable and the anti-duplication reference: **if a feature appears here, it is implemented by the listed template,
not by new game code.**

> **Audit depth:** this first pass is based on the READMEs and asmdef layout of the packages, not on a full source
> review. All 92 applicable RD packages are linked, including the 18 additional mechanics in §3.7 and §4.
> Haptics is intentionally excluded for Windows/Steam. Package
> installation does not mean gameplay integration or behavioral verification. Before writing game code for a feature,
> read
> the listed template's README, public runtime API, and demo scene, then correct this file if the API differs.

Legend: **MVP** first playable · **Demo** Steam demo · **1.0** release · **Candidate** fits, not committed · **Not
planned** does not fit the design · **N/A** not applicable to the platform · **Pending integration** package installed,
gameplay use not yet implemented.

---

## 1. Responsibility Ownership

| Responsibility                                                               | Owner                                                                                                      | Never build                                        |
|------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------|----------------------------------------------------|
| Application startup and shutdown order                                       | `Startup`                                                                                                  | A bootstrap manager                                |
| Scene loading and transitions                                                | `SceneFlow`                                                                                                | A scene-manager wrapper                            |
| Persistence                                                                  | `Save`                                                                                                     | A save manager                                     |
| User settings                                                                | `Settings`                                                                                                 | A settings manager                                 |
| Pause                                                                        | `Pause`                                                                                                    | A global pause flag                                |
| Input                                                                        | `Input`                                                                                                    | Direct device polling                              |
| Health, damage, death                                                        | `Health` (+ `HealthCarrier` bridge)                                                                        | A second health system                             |
| Currency                                                                     | `Wallet`                                                                                                   | Currency math inside contracts                     |
| Reward delivery                                                              | `Rewards`                                                                                                  | A custom payout calculator that mutates the wallet |
| Goals and steps                                                              | `Quests`                                                                                                   | A custom quest system                              |
| Rules and gating                                                             | `Conditions`                                                                                               | Ad-hoc `if` chains for access                      |
| Equipment loadout                                                            | `Equipment`                                                                                                | A custom loadout class                             |
| Small items                                                                  | `Inventory`                                                                                                | A second inventory                                 |
| Ranks and XP                                                                 | `Progression`                                                                                              | A custom rank counter                              |
| Numeric stats and modifiers                                                  | `Stats`                                                                                                    | A parallel stat store                              |
| Timed effects                                                                | `StatusEffects`                                                                                            | A custom buff system                               |
| Deadlines and cooldowns                                                      | `Timers`                                                                                                   | Ad-hoc float timers                                |
| UI navigation                                                                | `ScreenNavigation`, `PrefabWindows`                                                                        | A UI manager                                       |
| Animation of values/UI/doors                                                 | `Tween`                                                                                                    | Per-object lerp code                               |
| Sound playback                                                               | `Audio`                                                                                                    | Raw `AudioSource` management                       |
| Localization                                                                 | `Localization`                                                                                             | Hardcoded strings                                  |
| Telemetry                                                                    | `Analytics`                                                                                                | A custom event logger                              |
| Achievements                                                                 | `Achievements`                                                                                             | A custom unlock tracker                            |
| Tutorial flow                                                                | `Tutorials`                                                                                                | Popup spam                                         |
| Decoupled facts                                                              | `Events`                                                                                                   | A second event bus                                 |
| Logging                                                                      | `Foundation` `UnityLogger`                                                                                 | `Debug.Log`                                        |
| Van condition, cargo stability, hit zones                                    | `Health` (+ `HealthCarrier`)                                                                               | A custom durability system                         |
| Van slots (cargo module, vehicle modules, emergency)                         | `Equipment` (a second loadout instance)                                                                    | A custom module-slot class                         |
| Van stats (acceleration, top speed, grip, headlight range, cargo protection) | `Stats` (own collection)                                                                                   | A parallel stat store                              |
| Repair parts and kits                                                        | `Inventory`, `CarryObject`                                                                                 | A parts manager                                    |
| Repair, upgrade, and fuel payments                                           | `Wallet`                                                                                                   | Inline currency math                               |
| Driving versus on-foot controls                                              | `Input` action-map contexts                                                                                | Direct device polling                              |
| Road scene transitions                                                       | `SceneFlow`, later `Addressables`                                                                          | A scene-manager wrapper                            |
| **Vehicle driving (arcade controller)**                                      | **No template owns it. Open decision: game-side minimal controller or a new mechanic in `UnityTemplates`** | A realistic car simulation                         |

### Game-owned code (new code allowed in `Assets/Game`)

- Contract domain model and result calculation.
- Package definitions and package rules.
- Depot station glue and Contract Terminal content.
- Player glue connecting Input, Health, Equipment, Inventory, and weapon mechanics.
- Enemy prefabs that compose template AI/combat mechanics.
- Location and contract configuration data.
- Game-specific UI content and presentation.
- Application composition (the Startup steps and the scene entry points).

---

## 2. Modules

### 2.1 Application

| Module           | Role in Night Courier                                                                                                                                                                     | Status                                                                                                         |
|------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------|
| **Startup**      | Dependency-ordered steps that build application services (save, settings, audio, input, localization, progression, analytics) and shut them down in reverse                               | MVP                                                                                                            |
| **SceneFlow**    | Boot → Main Menu → Depot ↔ Location transitions; the van transition; additive HUD/scenes if used                                                                                          | MVP                                                                                                            |
| **Save**         | One typed game snapshot per slot: rank, credits, unlocks, upgrades, completed contracts, collectibles, achievements; later archive knowledge and a few world/reputation flags             | MVP (core) / Demo (knowledge, flags)                                                                           |
| **Settings**     | Volume, sensitivity, FOV, graphics, resolution, fullscreen, VSync, language                                                                                                               | MVP                                                                                                            |
| **Input**        | In use: an owned clone of `NightCourier.inputactions` with `Gameplay` and `UI` maps switched through `InputMapContexts` (scenes push Gameplay, terminals/pause/result push UI, cursor lock follows the active map), saved binding overrides loaded at startup. Later: rebinding screen, last-used-device glyphs, Vehicle map in the road tier | MVP                                                                                                            |
| **Pause**        | In use: the pause menu requests a pause handle (time scale 0 through `UnityTimeScalePauseController`). Later: terminals/windows that should freeze the world, hit stop                    | MVP                                                                                                            |
| **Localization** | Provider-backed keys for contracts, rules, terminals, UI. English source                                                                                                                  | MVP (keys) / Demo (extra languages)                                                                            |
| **Tutorials**    | Persistent authored steps for the first delivery, presented as corporate training (no "Press E" popups)                                                                                   | Demo                                                                                                           |
| **Analytics**    | Consent-gated telemetry for balancing: abandoned contracts, most-violated rule, chosen route, equipment taken, ammo spent, deaths, completion time                                        | Demo                                                                                                           |
| **Addressables** | Location, enemy, package, and contract-asset loading with leases                                                                                                                          | Demo (MVP uses direct scene references). Requires `com.unity.addressables` (already resolved in the lock file) |

### 2.2 Core

| Module           | Role                                                                                                                                   | Status            |
|------------------|----------------------------------------------------------------------------------------------------------------------------------------|-------------------|
| **Foundation**   | `Result<T>`, `CompositeDisposable`, `AsyncGate`, validated ids, `UnityLogger`                                                          | MVP               |
| **Events**       | A few typed facts: `ContractCompleted`, `PackageViolation`, `PlayerRespawned`. Nothing else goes on the bus                            | MVP               |
| **DependencyInjection** | In use at the composition boundary: `GameHost`'s root container (`GameComposition.Build`) constructs `GameServices` and owns a child scope per content scene; scene-scoped services (`WarehouseFacts`, `ConditionEngine`) are resolved once by the scene entry and passed on explicitly. Components never resolve from it | MVP               |
| **StateMachine** | Not used for the contract lifecycle: `Quests` steps plus the run phase cover it. Revisit only for a behavior with real per-state logic | Not needed so far |
| **Timers**       | `CountdownTimer` for deadlines and rule countdowns; `Cooldown` for grace periods and violation cooldowns                               | MVP               |
| **Attributes**   | Inspector authoring helpers only: `[Assign]`, `[Button]`, `[Scene]` on game MonoBehaviours                                             | MVP               |
| **Extensions**   | Vector/transform/list helpers where they remove repeated code                                                                          | As needed         |

> **Correction to earlier notes:** `Attributes` is an Inspector-helper module, not a runtime value store. Runtime values
> map as follows — player health: `Health`; package stability: `Health` on the package; suit protection: `Stats` plus a
> damage rule.

### 2.3 Gameplay

| Module            | Role                                                                                                                                                                                                                                                                                                             | Status                               |
|-------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------|
| **Health**        | In use: package integrity (`HealthCarrier` on the package, hit by `PackageImpact`; a destroyed package fails the contract) and the Warehouse barricade. Later: player and enemy health; armor as damage rules; invulnerability on respawn                                                                                       | MVP                                  |
| **Quests**        | Contract lifecycle as three steps (collect, depart, deliver) with prerequisites unlocking the contract chain; a failed run abandons the quest; completed-contract history and snapshots are saved; the different contract types (RECOVER, DISPOSE, MULTI-DROP, …) are different step lists                       | MVP                                  |
| **Conditions**    | In use: the Warehouse door rules (`power on`, `cargo scanned`) are `Conditions` rules over `WarehouseFacts`, evaluated by one fact evaluator. Later: security level, delivery acceptance (correct package, not destroyed), recipient verification, bonus checks (violation count), world/reputation flag gates | MVP (doors, delivery) / Demo (flags) |
| **Rewards**       | Contract payout as a reward bundle with idempotent grant ids. The net amount (base + bonuses − property damage − ammo expense − death penalty, floored at ₡0) is computed by the game settlement calculation and granted through Rewards with a game-side `wallet.credits` handler; grant ids are per completion | MVP                                  |
| **Wallet**        | In use: the credits balance; the payout is granted through `Rewards`, and upgrades are bought with `Spend` (`CourierUpgrades`)                                                                                                                                                                                                                                                                        | MVP                                  |
| **Progression**   | Courier Rank track; shift rating feeds experience and unlocks                                                                                                                                                                                                                                                    | MVP                                  |
| **Stats**         | In use: `CourierStats` (`courier.max_health`, `courier.move_speed`, `courier.reserve_ammo`) with one upgrade modifier per level, applied to `PlayerVitals`, `PlayerMotor`, and `PlayerWeapons`. Later: `CarryMoveSpeed` (needs a carry slowdown), `WeaponDamage` and `ReloadSpeed` (need runtime modifiers on `HitscanWeapon` and `AmmoReload`), `ScannerRange`; temporary modifiers from cargo such as Heavy Mass                                                                                                                                              | MVP                                  |
| **Equipment**     | In use: `CourierLoadout` wraps an `EquipmentLoadout` (Primary, Tool, Utility 1, Utility 2) for the Stun Pistol, Crowbar, and carried medkits; `PlayerWeapons` reads it, so a stowed weapon cannot be used. Later: suit slot, grenade, shotgun and repulsor choices. Road tier: a second loadout instance for the van (cargo module, 2 vehicle modules, emergency slot) | MVP (personal) / Road Alpha (van)    |
| **Inventory**     | In use: the medkit stock (`consumable.medkit`, up to 3 owned) in an `InventoryContainer`, bought at the Equipment Locker. Later: keycards, fuses, batteries, quest items, ammo boxes | MVP                                  |
| **StatusEffects** | Poisoned, Electrified, Slowed, Disoriented, Radiated; cargo-applied effects (Neural Cargo → Disoriented); also backs `DamageOverTime`                                                                                                                                                                            | Demo                                 |
| **Achievements**  | Steam-style achievement tracking, hidden entries, snapshots; style-driven goals (see GDD §21.2)                                                                                                                                                                                                                  | Demo                                 |

### 2.4 Presentation

| Module               | Role                                                                                                                                                                                            | Status                                                                                                                                                                        |
|----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **ScreenNavigation** | Persistent screens: Main Menu, Depot HUD, Contract Selection, Results, Pause                                                                                                                    | MVP                                                                                                                                                                           |
| **PrefabWindows**    | Popups: Contract Details, Delivery Result, Equipment, Upgrades, Confirmation                                                                                                                    | MVP                                                                                                                                                                           |
| **Tween**            | Terminal UI animation, delivery locker, lights, small feedback (Door and Lever already use it)                                                                                                  | MVP                                                                                                                                                                           |
| **Audio**            | Pooled cue playback: ambience, doors, package cues, weapons, enemy cues. Some cues are rules: accelerating ticking = falling stability, whisper = object moved, detector clicks = hazard nearby | MVP                                                                                                                                                                           |
| **Haptics**          | —                                                                                                                                                                                               | **N/A.** Android/iOS only; this game targets Windows/Steam. Gamepad rumble, if ever wanted, is a separate decision through the Input System directly, not through this module |

---

## 3. Mechanics

### 3.1 Items

| Mechanic        | Role                                                                                                                      | Status |
|-----------------|---------------------------------------------------------------------------------------------------------------------------|--------|
| **PickupDrop**  | In use: the small package is a `PickupItem`; the player carries it with a `PickupInteractor` at the `CargoPoint` anchor (`PlayerCarry` wraps pick-up, drop on G, consume on delivery). World props later | MVP    |
| **CarryObject** | In use: the Lights Out battery (`PlayerHeavyCarry` wraps `CarryCarrier`; it excludes carrying the package at the same time, and the carried object moves to the `Carried` layer so it does not block the interaction ray). Later: heavy containers, crates | MVP    |
| **Collectible** | Lost parcels, employee badges, black-box logs, Courier Tags (each unlocks an archive entry that helps in later contracts) | Demo   |

### 3.2 Interaction

| Mechanic          | Role                                                                            | Status |
|-------------------|---------------------------------------------------------------------------------|--------|
| **Door**          | In use: Warehouse maintenance door and scanner hatch (slide up). Later: automatic, security, containment doors | MVP    |
| **ConditionLock** | In use: `ConditionDoor` configures one per Warehouse door with a `Conditions` rule. Later: keycard, quest state, package authorization | MVP    |
| **Lever**         | In use: the breaker panel lever restores Warehouse power (one-way). Later: emergency door | MVP    |
| **PressurePlate** | In use: the cargo scanner plate (activation mask is the `Cargo` layer; `CargoScanner` runs a 3 s `CountdownTimer` while powered and occupied). Later: weight puzzles | MVP    |

### 3.3 World and Traversal

| Mechanic                          | Role                                                                                        | Status |
|-----------------------------------|---------------------------------------------------------------------------------------------|--------|
| **MovingPlatform** (+ `Elevator`) | In use: the Warehouse freight lift (`FreightLift` console, powered). The mechanic carries only non-kinematic rigidbodies, so `PlayerMotor` follows any kinematic-rigidbody ground itself. Later: cargo logistics | MVP    |
| **BreakableObject**               | In use: the Route B barricade over a `HealthCarrier`; the Crowbar breaks it in three hits (`BreakableBarricade` only relays `Broken`), and it registers a violation until property damage fines exist. Later: crates, glass, weak doors | MVP    |
| **Checkpoint**                    | In use: the three Warehouse Courier Emergency Lockers (`CourierLocker` activates its `Checkpoint` once by interaction; the set's active pose feeds `PlayerVitals`). Later: van, delivery terminal | MVP    |
| **Teleporter**                    | "Impossible architecture" in Spatial Instability states; Hunter Container relocation        | 1.0    |
| **JumpPad**                       | Pneumatic lift or anomalous pad in 1–2 special locations                                    | 1.0    |

### 3.4 Character

| Mechanic      | Role                                                                                                                                                                     | Status                                                                                                     |
|---------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------|
| **Respawn**   | In use: on the Player prefab next to a `HealthCarrier`, wrapped by `PlayerVitals` (the spawn pose comes from the checkpoint set). It only moves the transform, so `PlayerMotor` re-syncs the `CharacterController`. Penalties (payment, lost loot, worsened cargo) are game-side, layered on top | MVP                                                                                                        |
| **Stamina**   | Sprint and heavy-cargo cost. Sprint is free until this lands                                                                                                                                              | Demo                                                                                                       |
| **DodgeRoll** | —                                                                                                                                                                        | **Not planned.** First-person horror with no full-body character; defense comes from positioning and light |

### 3.5 Combat (shared bridge + weapons)

| Mechanic              | Role                                                                                                                     | Status                                                                                                                 |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------|
| **Combat** (Core)     | `HealthCarrier`, `CombatDamage`, `HitscanRay`, `Explosion`. Every damageable object carries a `HealthCarrier`            | MVP                                                                                                                    |
| **Common** (Core)     | Collider queries, direction sampling, owned objects                                                                      | MVP                                                                                                                    |
| **HitscanWeapon**     | In use: the Stun Pistol (`StunPistol` aims from the camera ray and fires from the muzzle)                                | MVP                                                                                                                    |
| **AmmoReload**        | In use: pistol magazine/reserve (8 / 24, locker refill +8). Later: shotgun                                               | MVP                                                                                                                    |
| **Recoil**            | In use: pistol camera kick applied by `PlayerMotor`. Later: shotgun; game-side glue forwards a shot to a held sensitive package as a stability event | MVP                                                                                                                    |
| **WeaponSpread**      | In use: pistol accuracy (1 degree cone). Later: temporary modifier from a damaged weapon or *Disoriented*, applied by game-side orchestration | MVP                                                                                                                    |
| **HitFeedback**       | In use: `HitReaction` flash on the Crawler and the Security Drone. Later: props, melee hit stop                                                                           | MVP                                                                                                                    |
| **MeleeAttack**       | In use: the Crawler attack (hit mask is the `Player` layer) and the Crowbar swing (`Default` + `Enemy`; also breaks the barricade)                                                                    | MVP                                                                                                                    |
| **Knockback**         | In use: Crawler hits, received by the game-side `PlayerKnockbackReceiver` (the `CharacterController` has no rigidbody), and Crowbar hits, received by `EnemyKnockbackReceiver` (a `NavMeshAgent` move). Later: Repulsor                                                                                     | MVP                                                                                                                    |
| **WeaponSwitching**   | In use: Pistol and Crowbar slots, driven by `PlayerWeapons` (keys 1, 2, Q). Later: the two carried items chosen at the locker | MVP                                                                                                                    |
| **Shotgun**           | Close-range weapon: fast, loud, expensive, bad for property                                                              | Demo                                                                                                                   |
| **Projectile**        | In use: the Security Drone bolt (`DroneBolt.prefab`). Later: Repulsor shot                                                                                                | MVP (drone)                                                                                                            |
| **ChargeWeapon**      | Repulsor charge (the former "Pulse Launcher")                                                                            | Demo                                                                                                                   |
| **HomingProjectile**  | Wisp orbs; Tracking Beacon (candidate)                                                                                   | 1.0                                                                                                                    |
| **Grenade**           | Containment Grenade (control zone; EMP variant)                                                                          | 1.0                                                                                                                    |
| **ExplosiveBarrel**   | Gas cylinders, fuel barrels, power cells; EMP container and pressure tank variants via game-side reactions to its events | Demo                                                                                                                   |
| **DamageZone**        | In use: the Warehouse electric floor over `PH_DangerStrip`. Later: fire, toxic puddle, laser, steam           | MVP                                                                                                                    |
| **DamageOverTime**    | Chemical exposure, burn, radiation, contaminated cargo                                                                   | Demo                                                                                                                   |
| **Block**             | Crowbar brace: directional defense as a damage rule                                                                      | Candidate (1.0)                                                                                                        |
| **TargetSelection**   | Scanner target cycling                                                                                                   | Candidate (1.0)                                                                                                        |
| **Parry**             | —                                                                                                                        | **Not planned.** Skill-expression combat does not fit survival horror at this scope. Revisit only if melee needs depth |
| **ComboAttackBuffer** | —                                                                                                                        | **Not planned.** The crowbar is a single swing                                                                         |
| **LockOnTargeting**   | —                                                                                                                        | **Not planned.** First-person free aim                                                                                 |

### 3.6 AI

| Mechanic              | Role                                                                                                                                                          | Status                               |
|-----------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------|
| **Patrol**            | In use: the Security Drone route (the brain disables it during pursuit and hands control back afterwards). Later: Watcher routes; also security scanners, cleaner robots, moving spotlights, inspection drones                                                        | MVP                                  |
| **DetectionSensor**   | In use: on the Crawler and the Security Drone (target mask `Player`, obstacle mask `Default`). Field of view, distance, line of sight; also on cargo (nearby humans) and surveillance cameras (sees the package). Sound stimuli are **not** added in advance | MVP (enemies) / 1.0 (cargo, cameras) |
| **ChaseTargetMemory** | In use: Crawler and Security Drone chase, search, and give-up decisions; the brains only move over a `NavMeshAgent`                                                                                                                                    | MVP                                  |
| **Spawner**           | In use: the violation-2 alarm Crawler in the Warehouse (`WarehouseEntry`). Later: alarm-triggered enemies                                                                                                               | MVP                                  |
| **WaveSpawner**       | *Emergency Lockdown* contract only                                                                                                                            | 1.0                                  |

### 3.7 Additional Installed Mechanics

These eight mechanics are installed as individual local packages from `../UnityTemplates`. Their planned roles below
remain unchanged; installation does not promote candidates or unplanned mechanics into the game design.

| Mechanic            | Planned role                                                                                                                                     | Status                                              |
|---------------------|--------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------|
| **PushPullObject**  | Grounded crates and large cargo pushed along flat ground (loading, road obstacles, plate puzzles). It does not handle slopes, so no loading ramp | Road Alpha / Demo                                   |
| **ResponsiveJump**  | Jump feel for the first-person controller, if the player can jump                                                                                | Candidate                                           |
| **Ladder**          | Maintenance district                                                                                                                             | Candidate (1.0)                                     |
| **LedgeGrabMantle** | Maintenance district                                                                                                                             | Candidate (1.0)                                     |
| **Zipline**         | Construction-site district                                                                                                                       | Candidate (1.0)                                     |
| **GrapplingHook**   | Experimental-facility district                                                                                                                   | Candidate (1.0)                                     |
| **WallJump**        | —                                                                                                                                                | **Not planned.** Does not fit the character fantasy |
| **Dash**            | —                                                                                                                                                | **Not planned.** Same reasoning as `DodgeRoll`      |

---

## 4. Installed Mechanics Awaiting Gameplay Integration

All ten mechanics below are now installed as individual local UPM packages. Their reserved roles remain pending gameplay
integration. At installation, HoldInteraction and RopeSwing had runtime source and demo scripts but no module README;
review their completed documentation and demos before gameplay integration. Package compilation is checked separately
from behavior.

| #  | Mechanic                 | Priority        | Reserved role                                                                                                                                                                                                                                                                                   |
|----|--------------------------|-----------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | **HoldInteraction**      | **1 (highest)** | **Integrated for the delivery point** (one `HoldInteraction` per hold target, driven by `Interactable`); remaining uses below follow their content. Hold E: repair fuse, scan cargo, force door, stabilize package, install battery, hack terminal, revive containment, neutralize anomaly, complete delivery; secure cargo in the van, repair the van (garage, pre-trip, roadside), install van modules. The player stays vulnerable while holding |
| 2  | **ConveyorBelt**         | 1               | **Integrated for the Depot package conveyor** (belt plus `ConveyorPassenger` on the package, end stop at the pick-up point). Remaining: van-loading cargo platform; warehouse conveyor room; later a sorting-system puzzle (lever routes, plate detects the cargo)                                                                                                                                           |
| 3  | **ThrowableObject**      | 1               | Distraction objects (can, tool, small crate) for AI, plus crates onto plates. Needs a sound-stimulus path in AI only when such a contract is actually built                                                                                                                                     |
| 4  | **ForceZone**            | 1               | Anomalies: gravity corridor, ventilation shaft, unstable cargo, containment field; roadside anomaly wind and gravity storms that push the van; cargo that pulls out of the player's hands                                                                                                       |
| 5  | **FallingPlatform**      | 1               | Emergency environment: an old catwalk that holds a few seconds, then breaks                                                                                                                                                                                                                     |
| 6  | **RotatingPlatform**     | 2               | Turntables, cargo cranes, rotating maintenance bridges                                                                                                                                                                                                                                          |
| 7  | **DisappearingPlatform** | 2               | Flickering anomaly walkways                                                                                                                                                                                                                                                                     |
| 8  | **PhysicsButton**        | 2               | Large emergency button pressed by hand, crate, or thrown object                                                                                                                                                                                                                                 |
| 9  | **Pendulum**             | 2               | Crane hazard: hanging container, hook, broken crane                                                                                                                                                                                                                                             |
| 10 | **RopeSwing**            | 2 (lowest)      | One rare contract: collapsed bridge, hanging service cable                                                                                                                                                                                                                                      |

Rules before gameplay integration:

1. Do not build a local version of any of these.
2. Design contracts and maps so they function without them (instant-press interaction through existing mechanics for the
   HoldInteraction roles).
3. Read the completed README, runtime API, and demo scene, then add the content listed in the reserved role only at its
   planned tier and verify it in Play Mode.

---

## 5. MVP Contract Matrix

All five contracts are type DELIVER and run on the same Warehouse location with different configuration. Location
states: Normal for all except 03, which uses Blackout. No complications in the MVP.

| #  | Contract                                          | Rules                       | Core templates exercised                                                                                                                                                        |
|----|---------------------------------------------------|-----------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 01 | **Standard Delivery** — learn pickup and delivery | none (tutorial)             | `PickupDrop`, `Door`, `ConditionLock`, `Inventory` (keycard), `Checkpoint`, `Quests`, `Rewards`, `Wallet`, `Tween`, `Audio`                                                     |
| 02 | **Fragile**                                       | Do Not Drop                 | Contract 01 set + package `Health`/`HealthCarrier`, `Timers`, `Events`, `DamageZone`, `HitFeedback`                                                                             |
| 03 | **Lights Out**                                    | Keep In Light               | `Lever`, `Conditions` (power), `Door`, `CarryObject` (battery), `Timers`, Crawler (`DetectionSensor`, `ChaseTargetMemory`, `MeleeAttack`, `Knockback`)               |
| 04 | **Express**                                       | Delivery Deadline           | `Timers`, route choice (`DamageZone`, `BreakableObject` vs. `MovingPlatform`), Security Drone (`Patrol`, `DetectionSensor`, `ChaseTargetMemory`, `Projectile`), Stun Pistol set |
| 05 | **Restricted Cargo**                              | Do Not Drop + Keep In Light | Blackout: `CarryObject` (battery), `Lever`, `Conditions`; `PressurePlate` (cargo scanner), `MovingPlatform`, `ConditionLock`, both enemies, Crowbar, `Checkpoint`, `Respawn`      |

Reward and rank flow for every contract: `Quests` → `Conditions` check → result calculation (game) → `Rewards` bundle →
`Wallet` credit → `Progression` (shift rating) → `Save`.

---

## 6. Game-Side Orchestration Over Template Events

These behaviors are built by the game listening to existing events. They must **not** be implemented by extending the
reusable mechanics.

| Behavior              | Listens to                                                    | Result                                                  |
|-----------------------|---------------------------------------------------------------|---------------------------------------------------------|
| Property damage fines | `BreakableObject` / `HealthCarrier` `Died`, barrel explosions | Built for the barricade: `ContractRun.RegisterPropertyDamage` and a `Deduction` line (`EconomyRules.PropertyDamageFine`). Later: barrels, crates |
| Ammo expense          | `PlayerWeapons.ShotFired`                                     | Built: `ContractRun.RegisterShot` (in transit only) and a `Deduction` settlement line (`EconomyRules.AmmoCostPerShot`) on the result screen |
| Cargo recoil          | `Recoil` generation                                           | Stability event on the held package                     |
| Spread penalty        | `StatusEffects` changes, weapon condition                     | Temporary modifier applied to `WeaponSpread` input      |
| Explosion variants    | `ExplosiveBarrel` / `Grenade` explosion events                | EMP effect, object throw, barricade break, control zone |
| Death consequences    | `Respawn` / `HealthCarrier` `Died` via `PlayerVitals.Died`    | Built: force-drop the package and heavy object (a drop under `DoNotDrop`), lock input, `ContractRun.RegisterDeath`, and a `Deduction` line (`EconomyRules.DeathPenalty`). Later: lost optional loot, worsened cargo |
| Respawn protection    | `PlayerVitals.Respawned`                                      | A `Health` invulnerability lease held for 2 s by a `CountdownTimer` |
| Package escalation    | `ContractRun.PackageViolationRegistered`                      | `ContractSpec.Escalation` steps: unstable package (double impact damage, pulse), an enemy from the scene `Spawner` (the new Crawler is told where the player is through `ChaseTargetMemory.ReportTarget`), contract failure. Later: damage over time, shove, container failure |
| Medkit use            | `UseMedkit` input action, `CourierLoadout`                  | `PlayerMedkits` consumes an equipped medkit and heals through `HealthPool.Heal`; blocked at full health, when dead, or with no equipped medkit |
| Hands restriction     | `PlayerHeavyCarry` / `PlayerCarry` held state                 | A refused pick-up or grab raises an event and a HUD notice. `PlayerWeapons` checks `WeaponRules.CanUse` before every shot or swing: a held package blocks the Crowbar, a heavy carry blocks every weapon, and a refusal raises `UseRefused` and a notice |
| Press and hold interaction | `Interact` input action, `HoldInteraction`               | `Interactable` (prompt key plus an optional `HoldInteraction`) found by the `PlayerInteractor` raycast; scene entries subscribe to `Interacted`. Mechanics such as `Door` and `Lever` stay input-agnostic and are called from these handlers |
| Package rules         | `PickupItem` `Released`, collisions, `CountdownTimer`, `ContractRun.DeadlineExpired` | `DoNotDrop`, `KeepInLight`, `DeliveryDeadline` components report `Violated`; `PackageRuleSet` relays the kind to `ContractRun.RegisterViolation` and the HUD notice |
| Warehouse gating      | `WarehouseFacts` changes                                      | `ConditionDoor` re-evaluates its `Conditions` rule through `ConditionLock` and opens its `Door` once |
| Riding platforms      | Kinematic rigidbody under the `CharacterController`           | `PlayerMotor` applies the ground's position delta each frame |
| Enemy movement        | `DetectionSensor` `TargetDetected`/`TargetLost`, `ChaseTargetMemory` state, `MeleeAttack` `TargetHit`, `HealthCarrier` `Died` | `CrawlerBrain` and `SecurityDroneBrain` forward detection to the memory and move a `NavMeshAgent` by its state; `EnemyDeath` handles death. No new AI framework |
| Barricade debris      | `BreakableObject` `Broken`                                    | Debris colliders ignore the player collider so rubble cannot block the shortcut |

If a case turns out to need a change inside a template (for example, a sound stimulus for `DetectionSensor`), raise it
as a proven-need change in `UnityTemplates` — see [../AGENTS.md](../AGENTS.md).

---

## 7. Preparation, Van, and Road (Road Tier)

The van and the road come after the MVP loop is fun ([GDD §3.2](GDD.md)). Nothing here is built during the MVP, except
that the data model treats a shift as a list of contracts.

| Feature                                                             | Owner                                                                                                                            | Notes                                                                                                                                  |
|---------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------|
| Driving the van (arcade)                                            | **No template owner**                                                                                                            | Open decision: a minimal controller in `Assets/Game/Vehicle`, or a new mechanic added to `UnityTemplates`. Must stay arcade (GDD §8.9) |
| Vehicle condition and cargo stability                               | `Health` / `HealthCarrier`                                                                                                       | Hit zones (front, side, rear) are game-side collision classification that sends typed damage; rear multiplies cargo damage ×2          |
| Van stats and fault effects                                         | `Stats`                                                                                                                          | Acceleration, top speed, grip, headlight range, cargo protection; upgrade and fault modifiers                                          |
| Fault states (headlights damaged, weak battery, …)                  | `StatusEffects` or `Stats` modifiers                                                                                             | Decide in the audit; faults are felt through stat changes, not percentages                                                             |
| Van slots                                                           | `Equipment`                                                                                                                      | A second `EquipmentLoadout` instance                                                                                                   |
| Parts, repair kits                                                  | `Inventory` + `CarryObject`                                                                                                      | Physical objects for the first install of each upgrade line                                                                            |
| Fuel                                                                | Audit: `Wallet` currency committed per road segment, a `Stats` value, or `Stamina`                                               | One choice, recorded here after the audit                                                                                              |
| Repairs, upgrades, refuel payments                                  | `Wallet`                                                                                                                         | Atomic spends                                                                                                                          |
| Upgrade gating                                                      | `Progression` + `Conditions`                                                                                                     | Real new capabilities, not numbers only                                                                                                |
| Persistent van condition, installed modules, shift state at the van | `Save`                                                                                                                           | Shift saved only at the van between deliveries                                                                                         |
| Cargo placement adjacency (heat, cold, electronics)                 | `Conditions`                                                                                                                     | Rules over cargo-point occupancy                                                                                                       |
| Route unlocks (security pass, rank)                                 | `Conditions`                                                                                                                     |                                                                                                                                        |
| Cargo loading                                                       | `PickupDrop`, `CarryObject`, `PushPullObject`, `MovingPlatform` (tail-lift), `ConveyorBelt` (pending integration)                | Flat dock or tail-lift; no ramp                                                                                                        |
| Cargo securing, roadside and garage repair                          | `HoldInteraction` (pending integration)                                                                                          | Instant interaction until its gameplay integration is verified                                                                         |
| Van doors                                                           | `Door`                                                                                                                           | Slide-only; verify fit for a van door in the audit                                                                                     |
| Driving versus on-foot input                                        | `Input` contexts                                                                                                                 | Vehicle context                                                                                                                        |
| Road scenes                                                         | `SceneFlow`, later `Addressables`                                                                                                | Road + Road State + Cargo + Event                                                                                                      |
| Road hazards                                                        | `DamageZone`, `BreakableObject`, `ExplosiveBarrel`, `Teleporter`, `MovingPlatform`, `ForceZone` (pending integration)            | A van-sized rigidbody on `MovingPlatform` must be checked                                                                              |
| Road events                                                         | `Patrol`, `DetectionSensor`, `ChaseTargetMemory`, `Collectible`, `PickupDrop`, `PushPullObject`, `BreakableObject`, `Teleporter` | See GDD §8.7                                                                                                                           |
| Cargo reactions to driving                                          | Game-side package rules over `Stats` and van events                                                                              | Magnetic, Gravity Core, Living, Electric, Restless                                                                                     |
| Vehicle HUD and Garage UI                                           | `ScreenNavigation`, `PrefabWindows`, `Tween`                                                                                     | Game UI                                                                                                                                |
| Van audio                                                           | `Audio`                                                                                                                          | Engine, tires, collisions, doors                                                                                                       |
| Van Inspection and shift settlement                                 | `Rewards`, `Wallet`, `Quests`, `Progression`                                                                                     | Net profit per shift                                                                                                                   |
| Preparation timer                                                   | `Timers`                                                                                                                         | 2–3 minutes if timed                                                                                                                   |

**Library gaps to settle with the user:** the vehicle controller (above), the fuel representation, and whether a hinged
van door is needed (`Door` only slides).

---

## 8. Maintenance

- Update this file in the same change that adds, removes, or re-purposes a template usage.
- A template marked **Not planned** or **N/A** stays that way until the design document says otherwise.
- If a template turns out not to fit its mapped role, record the reason here before changing the mapping.