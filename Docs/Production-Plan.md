# Production Plan

Remaining work for the Night Courier vertical slice. Finished work is removed from this plan. Design lives in
[GDD.md](GDD.md); template ownership lives in [Template-Mapping.md](Template-Mapping.md); placeholder and asset planning
lives in [Asset-Plan.md](Asset-Plan.md); working rules live in [../AGENTS.md](../AGENTS.md).

**Brief:** build a small, production-quality vertical slice on top of the existing `UnityTemplates` library. This is not
a new framework. The task is to test the existing `Modules` and `Mechanics` in a real game, reusing the current
architecture as much as possible. Do not change existing Modules/Mechanics if the game can be built by composition. If a
change is genuinely required, first identify the minimal reason, then change the existing API minimally (in the
`UnityTemplates` repository).

**Do not proceed to additional districts until the full MVP loop is fun.**

---

## Next Up (Recommended Order)

All modules and mechanics except Haptics are installed (92 packages); integration still goes one feature at a time.

1. **Phase 5 + Phase 4, physical part.** Package conveyor in the Depot and the package as a physical object
   (`PickupDrop` / `CarryObject`) with its hands restriction. Replaces the remaining stub Collect and Depart buttons in
   the terminal. Needs the player's carry point (`CargoPoint` anchor) and `Health` on the player.
2. **Phase 7 Warehouse.** Door, ConditionLock, Lever, PressurePlate, MovingPlatform, BreakableObject, Checkpoint, two
   routes, and the cargo scanner at the delivery point.
3. **Phase 4 rules + Phase 13 content.** `DoNotDrop`, `KeepInLight`, `DeliveryDeadline`, then per-contract configuration.
4. **Phase 8–9 Enemies and combat.** Security Drone, Crawler, Stun Pistol, Crowbar, plus player `Health`, `Equipment`,
   and weapon input.
5. **Phase 10–11 Upgrades, progression, persistence.** Then the first-milestone definition of done is met.
6. **Phase 2b, UI modules.** `ScreenNavigation` and `PrefabWindows` replacing the plain uGUI menus, `Audio`, `Tween`.
   Can be done at any point between the steps above.

---

## Strict Architecture Constraints

Do not add any of the following unless the library genuinely lacks the responsibility and the need is proven:

```text
GameManager · GlobalServiceLocator · a new EventBus · a new Inventory · a new Health system
a new Quest system · a new Reward system · a new Save system · a new StateMachine framework
a universal Weapon framework · a universal Interaction framework · a universal AI framework
```

Also do not create `SceneManager` wrappers, `UIManager`, or `SaveManager`. Prefer composition. Game-specific
orchestration belongs in `Assets/Game`, not in reusable library code.

---

## Phases

### Phase 0 — Audit (ongoing, per feature)

Before using a template, read its README, public runtime API, and demo, and keep
[Template-Mapping.md](Template-Mapping.md) current. Not yet audited at source level: ScreenNavigation, PrefabWindows,
Audio, Conditions, Progression, Stats, Equipment, Inventory, Health, StatusEffects, Achievements, Analytics, Tutorials,
Addressables, and every combat, AI, world, and newly installed mechanic except `HoldInteraction`.

### Phase 2b — UI Modules

The menus and the pause menu are plain uGUI placeholders. Still to integrate: `ScreenNavigation` and `PrefabWindows`
(replacing them, adding the New Game confirmation and a rebinding screen on top of `Input.Rebinding`), `Audio`, `Tween`,
and last-used-device prompt glyphs (`InputDeviceTracker`).

### Phase 4 — Package System

The smallest composition layer that turns existing world items into cargo. Reuse `PickupDrop`, `CarryObject`, `Health`
(destructible packages), `Conditions` (delivery acceptance: correct package, not destroyed), `Timers`, `Events`.

A package is not a giant class containing every rule. Independent rule components exist only where each is a genuinely
reusable package behavior.

Initial rules: `DoNotDrop`, `KeepInLight`, `DeliveryDeadline`. Outcomes: `RuleSatisfied`, `RuleViolated`,
`ViolationCount` (the contract run already exposes `RegisterViolation`, `ViolationCount`, and the deadline). One
violation does not fail a contract.

Each package is modeled as three layers — Rule (what the player is told), Behavior (what the object does), Escalation
(what happens after violations) — as per-package data over small components ([GDD §7.1](GDD.md)). Hands restriction
while carrying large cargo is game-side glue between `CarryObject` and weapon input. The Collect and Depart stub
buttons in the Depot terminal are replaced by the physical package and the van; delivery is already a hold interaction.

### Phase 5 — MVP Depot (remaining)

The Depot is walkable and the Contract Terminal is a physical interactable that opens the contract UI. Remaining:
Equipment Locker, Package Conveyor, Upgrade Terminal, Exit/Van, courier rank, equipment UI. Reuse `ScreenNavigation`,
`PrefabWindows`, `Tween`, `Audio`.

### Phase 6 — Player (remaining)

Movement, look, sprint, interaction, and the HUD prompt work. Still to add, each with the feature that needs it:
`Health` (with death and respawn), `Equipment` and weapon input, `Inventory`, carrying packages (`CargoPoint` anchor),
`Stats` for move speed and upgrades, `Stamina` for sprint (Demo tier). No new character framework. First person; no
full-body model.

### Phase 7 — MVP Warehouse

One playable Warehouse map containing: Door, ConditionLock, Lever, PressurePlate, MovingPlatform, BreakableObject,
Checkpoint, PickupDrop, CarryObject. At least two routes: safer/longer and dangerous/shorter. JumpPad and Teleporter are
optional later. The current Warehouse scene is a walkable blockout with a hold-to-deliver terminal.

### Phase 8 — Enemy MVP

- **SecurityDrone:** Patrol, DetectionSensor, ChaseTargetMemory, Projectile or HitscanWeapon, Health, HitFeedback.
- **Crawler:** DetectionSensor, ChaseTargetMemory, MeleeAttack, Knockback, Health, HitFeedback.

Game code composes them. No new hard runtime dependencies between existing mechanics unless genuinely required.

### Phase 9 — Player Combat

- **Stun Pistol:** HitscanWeapon, AmmoReload, Recoil, WeaponSpread, HitFeedback.
- **Crowbar:** MeleeAttack, Knockback.

Also in the MVP: the player carries two items chosen at the Equipment Locker (`WeaponSwitching` driven by `Equipment`),
and ammo use is tracked for the result screen.

Later: Shotgun, Repulsor (`ChargeWeapon` + `Projectile` + `Knockback`), HomingProjectile, Grenade, ExplosiveBarrel,
DamageOverTime, DamageZone. No new weapon framework.

### Phase 10 — Economy and Progression (remaining)

Contract settlement, `Rewards`, and `Wallet` credits work. Remaining: minimal courier progression using `Progression`,
`Stats`, `Equipment`; MVP upgrades (+MaxHealth, +CarryMoveSpeed, +ScannerRange or a weapon stat), bought with credits;
no large skill trees.

The settlement still needs its deduction lines: net payout = base + bonuses − property damage − ammo expense − death
penalty (floored at ₡0; from the road tier also − cargo damage penalty and optional vehicle repair), plus a shift rating
(delivery time, cargo condition, protocol compliance, property damage) that feeds `Progression`
([GDD §14](GDD.md)). Fines and ammo expense come from game-side listeners over existing events
([Template-Mapping.md §6](Template-Mapping.md)).

### Phase 11 — Persistence (remaining)

Credits, quest progress, completed contracts, and the reward ledger are saved. Remaining: rank/progression, unlocked
equipment, purchased upgrades, collectible progress. Do not serialize arbitrary active scene physics. Checkpoint/Respawn
handles temporary level death.

### Phase 12 — Full Module Integration

After the core game is fun, integrate deliberately: Analytics, Achievements, Tutorials, Audio, Addressables,
StatusEffects, Collectibles. Every integration needs an actual gameplay or product purpose; never use a module just to
claim coverage if that damages architecture.

### Phase 13 — MVP Contract Content (remaining)

The five contracts exist as data. What each still needs is its gameplay configuration on the single Warehouse map:

| #  | Contract          | Needs                                                 |
|----|-------------------|-------------------------------------------------------|
| 01 | Standard Delivery | Physical pickup and delivery                          |
| 02 | Fragile           | `DoNotDrop` rule                                      |
| 03 | Lights Out        | `KeepInLight` rule + Blackout state power problem     |
| 04 | Express           | `DeliveryDeadline` rule (300 s) + enemy route         |
| 05 | Restricted Cargo  | Two rules + combat + environmental puzzle             |

All five use the same Warehouse location with different configuration or state. Do not create five maps.

### Phase 15 — Presentation

Simple geometry first; never block development waiting for art. Low-poly industrial, dark, readable, limited palette,
strong lighting. Interactable objects are immediately readable.

Everything is built on Unity primitives with `PH_` placeholders. **Every gameplay prefab is built from its first version
with a stable root, standard anchors, and a replaceable `Visual` child**, so final art can be dropped in without
touching gameplay code ([Asset-Plan.md](Asset-Plan.md) §2). No third-party assets are imported until the user decides
to.

### Phase 16 — Verification (after every phase)

```text
compile · run relevant tests · open scenes · enter Play Mode · inspect Console
verify references · verify Input · verify Save · verify scene transitions
```

Use the available Unity MCP/editor tooling. Never report a scene as working merely because scripts compile.

---

## Road Tier Phases (After the MVP)

Do not start until the first-milestone definition of done below is met and the loop is fun. Each phase has its own gate;
test the riskiest part first.

### Phase 17 — Road Alpha: Drive the Van

Resolve the vehicle-controller decision first (see Open Decisions). Then, on a gray-box road: an arcade van with a
first-person driver's view, `Input` vehicle context, entering and exiting the van, cargo stability reacting to driving,
collision hit zones, one road segment. **Test driving alone before building anything on top of it.** If arcade driving
is not fun, stop and reconsider the road tier.

### Phase 18 — Loading, Preparation, and Garage

Physical cargo loading into four cargo points, securing, adjacency rules, preparation briefing with personal and van
slots, Garage with persistent van condition, pre-trip faults, and the Van Inspection screen. Reuse `PickupDrop`,
`CarryObject`, `PushPullObject`, `Equipment`, `Inventory`, `Stats`, `Health`, `Wallet`, `Save`.

### Phase 19 — Routes and Road Events

2–3 road segments with route planning, authored junctions, and a small set of road events and hazards from existing
mechanics, one incident at a time. Roadside stop-and-repair with instant interactions until the installed
`HoldInteraction` mechanic is integrated and verified.

### Phase 20 — Van Upgrades and Economy

Three upgrade lines (Cargo Stabilizer, Headlights, Engine) up to level 3, gated by `Progression`, paid with `Wallet`,
installed physically the first time. Tune repair costs against payouts.

### Phase 21 — Multi-Delivery Shifts

Shift planning with two deliveries, loading order, van-capacity choice, and shift state saved at the van. Later: 2–4
deliveries, multi-package interactions, return-trip contracts. The data model already treats a shift as a list of
contracts; the board currently allows exactly one active contract.

---

## Open Decisions

Raised for the user; agents do not settle them alone.

| Decision | Options | Notes |
|---|---|---|
| Who owns vehicle driving? | (a) minimal arcade controller in `Assets/Game/Vehicle`; (b) a new reusable mechanic in `UnityTemplates` | The library has none. (b) fits the project's purpose |
| Fuel representation | `Wallet` currency committed per road segment; `Stats` value; `Stamina` | Settle in the audit |
| Hinged vehicle/building doors | Keep `Door` slide-only; or extend the template | `Door` only slides |
| **`SceneFlow.ChangeSceneAsync` defect** (UnityTemplates) | Fix in the template: skip `SetActiveScene` when the loaded scene is already active | In Unity 6000.6 `SceneManager.SetActiveScene` returns `false` for the scene that is already active, which is the case right after a `Single` load, so `SceneFlow` throws `InvalidOperationException: Failed to set scene ... as active` although the scene loaded. Night Courier avoids it through additive swapping and does not depend on the fix, but every other consumer of `ChangeSceneAsync` is affected. Not changed here because UnityTemplates is a separate repository with work in progress |
| New Game overwrites the existing save without confirmation | Add a confirmation window when PrefabWindows is integrated | The save is only overwritten at the first progress after New Game |

---

## Mechanic Integration Schedule

Ten newer mechanics are installed but await gameplay integration (see [Template-Mapping.md §4](Template-Mapping.md)).
`HoldInteraction` is needed in step 2 above; the rest follow the content that needs them. Integration order:

1. **HoldInteraction** — first. Re-evaluate hatch, battery-connect, scan, repair, and delivery-complete interactions.
2. **ConveyorBelt**, **ThrowableObject**, **ForceZone**, **FallingPlatform** — priority 1 set.
3. **RotatingPlatform**, **DisappearingPlatform**, **PhysicsButton**, **Pendulum**, **RopeSwing** — priority 2 set,
   added only when a location needs them.

Until a mechanic is integrated and verified, build everything that does not depend on it. Do not clone it locally.

---

## First Milestone — Definition of Done

The first playable milestone is complete only when this full path works. Steps already working in the stub flow:
New Game, select contract, deliver, calculate reward, return to Depot, save, restart, progression restored (credits and
contract progress).

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