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

All modules and mechanics are installed (10 RD packages: Foundation, seven kits, and two demo-support packages, see
[../TEMPLATE-PACKAGES.md](../TEMPLATE-PACKAGES.md); Haptics sits inside the Presentation kit and stays unused); integration
still goes one feature at a time.
The Warehouse map, the package rules and ladder, the Lights Out battery, player health with respawn and checkpoints,
the two enemies, and the player's Stun Pistol and Crowbar all work in stepped Play Mode.

1. **Phase 13 remaining content.** The Restricted Cargo environmental puzzle and the ladder step "package becomes
   hazardous" (needs `DamageOverTime`). A Play Mode run of Restricted Cargo and fairness tuning of the encounters now that
   the player can fight back.
2. **Phase 10–11 remaining.** The Equipment Locker (the two carried items, `Equipment` driving `WeaponSwitching`, Medkit),
   courier rank and shift rating through `Progression`, and the upgrades that need new hooks (carry move speed needs a
   carry slowdown; weapon damage and reload speed need runtime modifiers on `HitscanWeapon` and `AmmoReload`). The
   Upgrade Terminal, the deduction lines, and the save of purchased upgrades already work, so the first-milestone path
   (purchase, save, restart, restore) is playable.
3. **Phase 2b, UI modules.** `ScreenNavigation` and `PrefabWindows` replacing the plain uGUI menus, `Audio`, `Tween`.
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
Audio, Progression, Stats, Equipment, Inventory, StatusEffects, Achievements, Analytics, Tutorials, Addressables, and
every combat, AI, world, and newly installed mechanic except `HoldInteraction`, `Door`, `ConditionLock`, `Lever`,
`PressurePlate`, `MovingPlatform`, and `BreakableObject` (`Conditions` and `Health` are audited for those uses only).

### Phase 2b — UI Modules

The menus and the pause menu are plain uGUI placeholders. Still to integrate: `ScreenNavigation` and `PrefabWindows`
(replacing them, adding the New Game confirmation and a rebinding screen on top of `Input.Rebinding`), `Audio`, `Tween`,
and last-used-device prompt glyphs (`InputDeviceTracker`).

### Phase 4 — Package System

The smallest composition layer that turns existing world items into cargo. Reuse `PickupDrop`, `CarryObject`, `Health`
(destructible packages), `Conditions` (delivery acceptance: correct package, not destroyed), `Timers`, `Events`.

A package is not a giant class containing every rule. Independent rule components exist only where each is a genuinely
reusable package behavior.

Built (EditMode tests pass, 52/52): `DoNotDrop`, `KeepInLight`, `DeliveryDeadline` as `PackageRule` components on the
package prefab, activated per contract from `ContractSpec.PackageRules` by `PackageRuleSet`; package `Health` through
`HealthCarrier` with `PackageImpact` (impact damage) and `PackageIntegrity` (HUD value; destroyed fails the contract).
A release that does not land on a `PackageSafeSurface` (the scanner plate) is a `DoNotDrop` violation, because the
`PickupDrop` release always happens at hold height; a hit above 8 m/s is a violation anywhere else. `KeepInLight` runs a
10 s `CountdownTimer` while `WarehouseLighting` reports the package unlit (Blackout lights come on with the breaker
lever). `DeliveryDeadline` reports when the run's `DeadlineExpired` fires. The five contract assets carry their rules.

Verified in stepped Play Mode (state set up through `GameServices`, real clicks and keys not used): `Fragile` (a floor
drop is a violation with a HUD notice and about 7% damage, a landing on the scanner plate is not, a clean run pays the
no-violations bonus), `Lights Out` (Blackout starts dark, the 10 s rule violates once, the breaker lever restores light
and resets the timer), `Express` (the deadline violates once), integrity falling on hard impacts (a fall from 3.5 m is
a violation and damage), and a destroyed package showing the failure panel and returning to the Depot. Not covered:
`Restricted Cargo` (two rules together; only its data was checked) and the Depot-side package behavior.

Each package is modeled as three layers — Rule (what the player is told), Behavior (what the object does), Escalation
(what happens after violations) — as per-package data over small components ([GDD §7.1](GDD.md)). The physical package
(a small `PickupDrop` item on the conveyor, carried at the player's `CargoPoint`, loaded into the van, picked up again at
the Warehouse dock, consumed on delivery) works, and every package rule violation reaches
`ContractRun.RegisterViolation`.

The violation ladder is per-contract data (`ContractSpec.Escalation`, authored as `EscalationEntry` rows on the
definition): at violation N the Warehouse applies `PackageUnstable` (the package pulses red and takes double impact
damage, with a HUD notice) or `FailContract` (the failure panel). The shipped ladders are 1 unstable and 4 fail for
Fragile and Lights Out, 1 unstable and 3 fail for Restricted Cargo, and none for Standard Delivery and Express.
Property damage (the barricade) counts toward the no-violations bonus through `RegisterPropertyDamage` but not toward
the ladder. Verified in stepped Play Mode on Fragile (four floor drops: unstable at the first, failed at the fourth).
Violation 2 now spawns an enemy: `EscalationEffect.SpawnEnemy` makes `WarehouseEntry` ask a scene `Spawner` (the Crawler
prefab, three alarm points on the NavMesh, at most 2 alive) for a Crawler and report the player to its
`ChaseTargetMemory`, with a HUD notice. Fragile and Lights Out now run 1 unstable, 2 enemy, 4 fail; Restricted Cargo runs
1 unstable, 2 enemy, 3 fail. Verified in stepped Play Mode on Fragile (violation 2 spawns a chasing Crawler, 3 changes
nothing, 4 fails). The dock bay is a separate NavMesh region, so an alarm Crawler cannot reach a player still in the dock
bay (its path is partial) and waits at the nearest reachable point.
Still to build: the ladder step "package becomes hazardous" (needs damage over time), an audible warning (needs `Audio`),
and per-package Behavior components.
Package `Health` does not carry over from the Depot to the Warehouse yet (the Warehouse spawns a fresh package); revisit
with the road tier.

### Phase 5 — MVP Depot (remaining)

The Depot is walkable, with a physical Contract Terminal, a belt-fed Package Conveyor, and the van. Remaining: Equipment
Locker, Upgrade Terminal, courier rank, equipment UI. Reuse `ScreenNavigation`, `PrefabWindows`, `Tween`, `Audio`.

### Phase 6 — Player (remaining)

Movement, look, sprint, interaction, the HUD prompt, and carrying a package work. Player health works: a
`HealthCarrier` plus the `Respawn` mechanic on the Player prefab, wrapped by `PlayerVitals` (respawn protection through
a `Health` invulnerability lease and a `CountdownTimer`). On death `PlayerSession` force-drops the package (falling back
to the pick-up interactor's lifecycle when no free drop spot exists) and any heavy object, and pushes a locked input
context (`GameInput.PushLocked`: no actions, cursor stays locked);
`PlayerMotor` re-syncs the `CharacterController` on respawn (`Respawn` only moves the transform). The HUD shows health
and a fading "Courier recovery team retrieved you." overlay (`PlayerVitalsView`, Tween). Dying while carrying the
package counts as a drop. Still to add, each with the feature that needs it: `Equipment`, `Inventory`,
`Stats` for max health, move speed and upgrades, `Stamina` for sprint (Demo tier). The death penalty and cargo
worsening wait for the settlement lines (Phase 10): `ContractRun.DeathCount` is already recorded and passed in the
outcome. No new character framework. First person; no full-body model.

### Phase 7 — MVP Warehouse (remaining)

The Warehouse has two routes from the dock bay to the delivery room. After the breaker lever restores power, the safe
route goes through the maintenance door and the freight lift; the short route goes through the breakable barricade and a
ramp. In the delivery room the package is set on the cargo scanner, which opens the hatch to the delivery terminal. Both
doors are `Conditions` rules over `WarehouseFacts`. Verified in stepped Play Mode only (no real keyboard input).

Blackout adds a battery puzzle: the breaker lever is dead until a battery (a `CarryObject` on the loading floor, reachable
only through the barricade because the maintenance door is unpowered) is carried into the generator in the dock bay.
Carrying the battery takes both hands (`PlayerHeavyCarry` and `PlayerCarry` refuse each other, with a notice), and a
carried battery moves to a `Carried` layer that the interaction ray ignores. Verified in stepped Play Mode on Lights Out
(refused grab with the package in hands, carry through the barricade gap, generator, lever, lights on).

Three Courier Emergency Lockers (dock bay, loading floor, delivery room) are `Checkpoint`s in a `CheckpointSet`,
activated once each by interaction through `CourierLocker` (the dock locker is the initial checkpoint, the warehouse
feeds the active checkpoint's pose to `PlayerVitals`), and the electric floor is a `DamageZone` over `PH_DangerStrip`
(60 damage per second in 0.5 s ticks; it also damages a package lying in it). Verified in stepped Play Mode: dying in
the zone takes about 2 s (overlay, input lock, 1.5 s delay), the respawn lands at the last activated locker with
protection and a stable `CharacterController`, locker activation shows its notice and recolors the indicators, and dying
while carrying the package drops it, counts a `DoNotDrop` violation, and respawns the player. A package dropped inside
the zone on death is not covered. The held package follows the `CargoPoint` while walking; teleporting the player while
holding it (a test artifact) leaves the interpolated body behind.

Still to build: explosive barrels. Property damage is a stand-in: breaking the barricade counts against the
no-violations bonus until the settlement has a property damage line (Phase 10). The barricade is now broken with the
Crowbar (three hits). JumpPad and Teleporter are optional later.

### Phase 8 — Enemy MVP (done, verified in stepped Play Mode)

- **Crawler** (`Crawler.prefab`, `CrawlerBrain`): `DetectionSensor` on its eye, `ChaseTargetMemory`, `MeleeAttack`
  (10 damage, 1.2 s swing), `Knockback` into a game-side `PlayerKnockbackReceiver`, `HealthCarrier` (40), `HitReaction`.
  It chases and searches over a baked NavMesh, swings in range, and walks home when the memory expires.
- **SecurityDrone** (`SecurityDrone.prefab`, `SecurityDroneBrain`): a ground-level root that hovers through a `Visual`
  child, `Patrol` along its route, `DetectionSensor`, `ChaseTargetMemory`, `Projectile` bolts (8 damage, `Cooldown`
  1.4 s, only with line of sight), `HealthCarrier` (30), `HitReaction`. On detection it disables `Patrol` and takes over
  with a `NavMeshAgent` keeping a 6 m standoff; after the memory expires it navigates to the nearest route waypoint and
  hands control back to `Patrol`.
- Both die through `EnemyDeath` (stop the behaviours, disable colliders, shrink with Tween, destroy).

The game-side brains only route the template events (`TargetDetected` to `ReportTarget`, `StateChanged` to movement)
and own movement; the AI state logic stays in `ChaseTargetMemory`. The player moved to a new `Player` layer and enemies
to an `Enemy` layer, so sensors, melee, and the NavMesh bake (Default layer only) target the right things. The Warehouse
has a baked `NavMeshSurface` (`Scenes/Warehouse/NavMesh-Warehouse.asset`); the barricade and closed doors are baked as
obstacles, so enemies do not use the shortcut or powered doors. Enemies appear per contract through
`ContractSpec.Encounters` and `EncounterGroup`: Lights Out has Crawlers, Express a Security Drone, Restricted Cargo both.
Verified: encounter activation, detection, chase, melee hit with knockback, drone patrol, pursuit, shooting, killing the
player, searching and returning to route, and enemy death. Not covered: a Spawner-driven wave, enemies hitting a package,
enemies on the electric floor, and fairness tuning.

### Phase 9 — Player Combat (MVP weapons done, verified in stepped Play Mode)

- **Stun Pistol** (`StunPistol`): `HitscanWeapon` (15 damage, 3.5 shots/s, damage type `stun`), `AmmoReload` (8 rounds, 24
  reserve, 1.4 s reload; an empty magazine reloads on the next trigger pull), `Recoil` (applied to the camera by
  `PlayerMotor`), `WeaponSpread` (1 degree). The aim point comes from the camera ray and the shot leaves the muzzle. A
  `LineRenderer` tracer shows the shot.
- **Crowbar** (`Crowbar`): `MeleeAttack` (18 damage, 0.55 s swing with a 0.15–0.3 s hit window) plus `Knockback` (7 m/s)
  and a Tween swing of the placeholder model. Enemies receive the push through the game-side `EnemyKnockbackReceiver`
  (a `NavMeshAgent` move that decays), the same pattern as the player's `PlayerKnockbackReceiver`.
- **Input and switching:** `PlayerWeapons` binds Fire, Reload, SwitchWeapon, Weapon1, and Weapon2 from `PlayerInputActions`
  (mouse, R, Q, 1, 2; gamepad right trigger, X, Y, D-pad). `WeaponSwitching` holds the two slots; switching cancels a
  reload. A small `WeaponHudView` shows the weapon name and the magazine/reserve count in both the Depot and the Warehouse.
- **Hands restriction:** `WeaponRules.CanUse` is the single rule. A held package blocks the Crowbar (the pistol is
  one-handed); a heavy carry blocks every weapon. A refused use raises `UseRefused` and a HUD notice.
- **Ammo and the result:** `ContractRun.RegisterShot` counts shots only in transit (`ShotsFired` in `ContractOutcome`).
  The Emergency Lockers add 8 reserve rounds on activation. The ammo expense settlement line itself is Phase 10.
- Verified with injected input: fire, recoil, reload, auto-reload on an empty magazine, 1/2/Q switching, three pistol
  shots kill a Crawler (40 health), two kill a Security Drone (30), the Crowbar knocks a Crawler back about 1 m, three
  Crowbar hits break the barricade, the package blocks the Crowbar but not the pistol, the battery blocks both, locker
  refill and the HUD update, and the shot count reaches the active run. Not covered: a gamepad, `HitFeedback` visuals on a
  pistol hit beyond the existing hit flash, and the recoil-to-package stability event (design in GDD 7.6).

Also in the MVP, still to do in Phase 10: the player carries two items chosen at the Equipment Locker (`WeaponSwitching`
driven by `Equipment`; today both weapons are always carried), and the ammo expense line.

Later: Shotgun, Repulsor (`ChargeWeapon` + `Projectile` + `Knockback`), HomingProjectile, Grenade, ExplosiveBarrel,
DamageOverTime, DamageZone. No new weapon framework.

### Phase 10 — Economy and Progression (remaining)

Contract settlement, `Rewards`, and `Wallet` credits work. Remaining: minimal courier progression using `Progression`,
`Stats`, `Equipment`; MVP upgrades (+MaxHealth, +CarryMoveSpeed, +ScannerRange or a weapon stat), bought with credits;
no large skill trees.

Settlement deductions are done: net payout = base + bonuses − property damage − ammo expense − death penalty, floored at
₡0 (`EconomyRules`: ₡3 per shot, ₡20 per property damage event, ₡25 per death; `SettlementLineKind.Deduction`). The run
counts shots, property damage, and deaths in transit, and the result screen lists each non-zero deduction. From the road
tier the net payout also loses a cargo damage penalty and optional vehicle repair. Still to add: a shift rating
(delivery time, cargo condition, protocol compliance, property damage) that feeds `Progression` ([GDD §14](GDD.md)).

**Upgrades (done).** `UpgradeDefinition` assets (data) build immutable `UpgradeSpec`s in an `UpgradeBook`; each upgrade
targets a `Stats` stat and has up to three levels with a price per level. `CourierStats` owns a `StatCollection`
(`courier.max_health`, `courier.move_speed`, `courier.reserve_ammo`) and rebuilds one modifier per upgrade from its level;
`CourierUpgrades` spends credits through the `Wallet`, tracks levels, and captures/restores them. The three MVP upgrades
are Reinforced Vest (+25 max health per level), Courier Boots (+8% move speed per level), and Ammo Pouch (+8 reserve
pistol rounds per level); prices are 100/200/350, 120/240/400, and 80/160/300. `PlayerSession.ApplyStats` pushes the stats
to `PlayerVitals` (`HealthPool.SetMaximum`), `PlayerMotor` (speed multiplier), and `PlayerWeapons` (reserve bonus, applied
as a delta so a purchase in the Depot is not double counted). The Depot has a physical Upgrade Terminal (`PH_UpgradeTerminal`
with an `Interactable`, `UpgradeTerminalView`, three `UpgradeSlot` rows) that saves the campaign after every purchase.
Verified in stepped Play Mode: buying the vest through the terminal (E, then the Buy button) raised max health to 125,
wrote `campaign.json`, and a restart through Continue restored credits, the level, and the stats; boots and pouch applied
in the Warehouse (speed 1.08, reserve 40 of 24 base); the result screen showed `Base pay 150, Ammo expense -9, Property
damage -20, Recovery team fee -25, Total 96`. The first MVP list in the GDD (+Max Health, +Carry Move Speed, +Scanner
Range or a weapon stat) is adapted: there is no carry slowdown or scanner yet, and the library has no runtime damage or
reload modifier, so move speed and reserve ammo stand in.

### Phase 11 — Persistence (remaining)

Credits, quest progress, completed contracts, the reward ledger, and purchased upgrade levels are saved (`CampaignSave`
version 3; version 2 saves still load with no upgrades, unknown upgrade ids are ignored, and levels are clamped to the
current maximum). Remaining: rank/progression, unlocked equipment, collectible progress. Do not serialize arbitrary active scene physics. Checkpoint/Respawn
handles temporary level death.

### Phase 12 — Full Module Integration

After the core game is fun, integrate deliberately: Analytics, Achievements, Tutorials, Audio, Addressables,
StatusEffects, Collectibles. Every integration needs an actual gameplay or product purpose; never use a module just to
claim coverage if that damages architecture.

### Phase 13 — MVP Contract Content (remaining)

Rules, ladders, and the Blackout state are configured per contract as data (Phase 4 and Phase 7). What is left, on the
single Warehouse map:

| #  | Contract          | Done                                                           | Still needs                                  |
|----|-------------------|----------------------------------------------------------------|----------------------------------------------|
| 01 | Standard Delivery | Physical pickup and delivery                                   | —                                            |
| 02 | Fragile           | `DoNotDrop`, ladder 1 / 2 / 4                                  | —                                            |
| 03 | Lights Out        | `KeepInLight`, Blackout, battery and generator, ladder 1 / 2 / 4, Crawlers | —                                |
| 04 | Express           | `DeliveryDeadline` (300 s), Security Drone                     | Fairness tuning with the pistol              |
| 05 | Restricted Cargo  | `DoNotDrop` + `KeepInLight`, ladder 1 / 2 / 3, both enemies (data only; not run) | An environmental puzzle, a Play Mode run |

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