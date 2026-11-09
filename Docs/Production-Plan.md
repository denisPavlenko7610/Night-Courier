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

The current ordered task list, with acceptance checks, is [Next-Sprint.md](Next-Sprint.md); the
status and M0–M4 roadmap are in [GDD.md](GDD.md) section 0. The last pass added the settlement guards, the shadow-aware
light field (`LightReach`), the contract briefing, the scan and delivery hints, cause-naming damage notices, and the
improvement lines on the result screen.

All modules and mechanics are installed (eight RD packages: Foundation and seven kits, see
[Template-Packages.md](Template-Packages.md); Haptics sits inside the Presentation kit and stays unused); integration
still goes one feature at a time.
The Warehouse map, the package rules and ladder, the Lights Out battery, player health with respawn and checkpoints,
the two enemies, and the player's Stun Pistol and Crowbar all work in stepped Play Mode.

1. **Phase 13 has five configured MVP contracts.** Standard Delivery and Lights Out (the revised battery route) were
   played dock to result with synthetic keyboard and mouse events on 2026-11-09 (`RealInputPlayModeTests`). Restricted
   Cargo still needs a run with live threats. Express runs on a 45 s limit with a 22 s bonus window; both routes take
   about the same time ([Next-Sprint.md](Next-Sprint.md)). The enemies react to being shot.
2. **Phase 10–11 remaining.** The upgrades that need new hooks (carry move speed needs a carry slowdown; weapon damage
   and
   reload speed need runtime modifiers on `HitscanWeapon` and `AmmoReload`) and anything the rank should unlock. The
   Upgrade Terminal, the Equipment Locker, the deduction lines, the shift rating and courier rank, and the save of all
   of
   them already work, so the first-milestone path (purchase, save, restart, restore) is playable.
3. **Phase 2b, remaining UI modules.** The Depot terminals and the result screen as screens or windows, a rebinding
   screen, and last-used-device prompt glyphs. The main menu, the pause menu, and the confirmation popups already use
   `RD.Menus`, `ScreenNavigation`, and `Tween`; sound goes through the template `AudioService` (see Phase 2b).

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

**Done on UI Toolkit.** The main menu (Main, Settings) and the pause menu (Main, Settings, Controls) are `RD.Menus`
views (`MenuView`, `SettingsPageView`) inside `ScreenNavigation` screens (`UiScreen`, with a `Tween` fade on unscaled
time so it runs while the game is paused); each menu owns a `ScreenNavigator`. Settings are one `SettingsPage` built by
`SettingsStep` and exposed as `GameServices.SettingsPage` (graphics rows from `UnitySettingRows`; master volume and
mouse sensitivity are game rows whose hooks write `ISettingsService`, sharing the `nightcourier.` PlayerPrefs keys). The
confirmation popup is `ConfirmDialog`, a UXML overlay (New Game over a save, Abort Contract, Main Menu). Escape closes
the popup first, then pops a sub-screen, then closes the pause menu; Back (UI Toolkit navigation) and the Cancel action
can fire in the same frame, so Back is deduplicated per frame. The skin is the game USS theme layered over `Menus.uss`;
the hero main menu is game UXML. Verified in Play Mode: the UI layout audit at seven resolutions (overflow, overlap and
off-screen text for uGUI and UI Toolkit), `MenuPlayModeTests` (Continue state, New Game confirmation, Apply and Back,
pause navigation and Escape order) and the abort path in `GameLoopPlayModeTests`. Not verified: a real Escape key press
(the tests call the cancel handler), mouse clicks (the tests send navigation submit events), and gamepad navigation.

**HUD on UI Toolkit.** The `PlayerHud` prefab is one `UIDocument` (`Content/UI/Hud/PlayerHud.uxml`) driven by
`PlayerVitalsView` (health and the death overlay), `WeaponHudView`, `MedkitHudView`, `ObjectiveView` and
`InteractionPromptView` (key glyph, lock icon, hold bar). `DepotHud.uxml` (`DepotHudView`: title, credits, rank) and
`WarehouseHud.uxml` (`WarehouseHudView` and `PackageHudView`: location, contract, timer, package rules and integrity,
notices) are scene documents on the HUD `PanelSettings` (sort order below the menus). Every HUD element ignores picking,
and a covered HUD is hidden with `display: none` on its `hud-group`. The UI layout audit now measures UI Toolkit text
(off-screen, overflow, overlap) and caught a real overlap between the info column and the objective plate. Not verified:
gamepad prompts and a locale change at run time (the views refresh on `LocaleChanged`, but only English exists).

**Depot terminals on UI Toolkit.** One `DepotTerminals.uxml` document holds the contract terminal, the upgrade terminal and the equipment locker as three overlays (`ContractTerminalView`, `UpgradeTerminalView`, `EquipmentLockerView`; the rows are UXML templates driven by the plain classes `ContractSlot`, `UpgradeSlot` and `LockerRow`). `DepotEntry` keeps the contract rules and talks to its view through three events (selected, accepted, close requested). Each terminal pushes a UI input context, focuses its first usable button when it opens, and binds its own `UiSounds` to its own panel so a click plays one cue. Briefing text keeps coming from `ContractBriefing` as rich text. The UI layout audit reads the terminals at seven resolutions; `UiLayoutPlayModeTests` opens and closes each of them through real button events.

**Result panel on UI Toolkit.** The Warehouse result and failure panel is `ResultPanel.uxml` driven by `ResultPanelView` (`Bind`, `Open(credits)`, `SetReturnEnabled`, `ReturnRequested`), a scene document with its own `UiSounds` binding. Its rich text is composed in code and set on UI Toolkit labels.

**No uGUI is left.** `LocalizedLabel`, `PanelScreen`, `ConfirmWindow`, `SettingsPanelView` and the uGUI paths of `UiSounds` are deleted, and `UnityEngine.UI` is no longer referenced by the game or PlayMode test assemblies. New UI must stay on UI Toolkit (AGENTS.md rule).

**Still to integrate:** a rebinding screen on top of `Input.Rebinding` (a `SettingsPage` action row), and
last-used-device prompt glyphs (`InputDeviceTracker`).

**Audio (done).** `AudioStep` builds the template `AudioService` once and `GameServices.Audio` hands it to the scenes;
42
`AudioCue` assets in `Assets/Game/Content/Audio/Cues` are assigned to small listener components (`PlayerAudio`,
`EnemyAudio`, `WarehouseAudio`, `UiSounds`) that subscribe to events the mechanics already raise. The Depot and
Warehouse
ambience loops are generated in-house; every other clip is Kenney CC0 (register in `Docs/Asset-Plan.md` section 8).
Verified in stepped Play Mode: menu and pause clicks, Depot ambience, footsteps, terminal open and accept, pickup and
drop, pistol shot, crowbar swing, lever, generator, lift motor, door, violation buzz, unstable, alarm, hazard, player
hurt,
death and respawn, enemy hit, die, crawler attack, drone hum and bolt, and contract failure. Not exercised: scanner
beep,
battery insert, barricade break, checkpoint locker, delivery jingle, purchase and error UI sounds. Added events:
`ConditionDoor.Opened/Closed`, `FreightLift.RideStarted/RideCompleted`, `CargoScanner.ScanStarted`,
`SecurityDroneBrain.BoltFired`.

**Visual replacement, stage 1 (done).** One style family: Kenney. The Depot and Warehouse cubes use Kenney Prototype
Textures (later replaced by flat Kenney-palette colors, the pack removed) through `Env_*` materials in
`Assets/Game/Presentation/Materials`; the cube meshes were replaced by generated
box meshes (`PH_BoxMeshes.asset`) with world-scale UVs, so the texel density is the same everywhere. Every `Text` used
Kenney Future Narrow, later replaced by the built-in UI font and the font file removed (sizes scaled by 0.8, best fit on
five labels), and all buttons and the dialog, contract, upgrade and
locker panels use the UI Pack Sci-Fi sprites. Hazard, interactable, delivery, and van placeholders keep their flat
colors so the light language stays readable. Verified in stepped Play Mode with camera captures of the Depot, Warehouse,
main menu, and contract terminal; EditMode 189 of 189. Not done: weapons, props and structure, effects, enemies, van
(later stages of the replacement plan).

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
(a small `PickupDrop` item on the conveyor, carried at the player's `CargoPoint`, loaded into the van, picked up again
at
the Warehouse dock, consumed on delivery) works, and every package rule violation reaches
`ContractRun.RegisterViolation`.

The violation ladder is per-contract data (`ContractSpec.Escalation`, authored as `EscalationEntry` rows on the
definition): at violation N the Warehouse applies `PackageUnstable` (the package pulses red and takes double impact
damage, with a HUD notice), `SpawnEnemy`, `PackageHazardous`, or `FailContract` (the failure panel). The shipped ladders
are the GDD default (1 unstable, 2 enemy, 3 hazardous, 4 fail) for Fragile, Lights Out, and Restricted Cargo, and none
for Standard Delivery and Express.
Property damage (the barricade) counts toward the no-violations bonus through `RegisterPropertyDamage` but not toward
the ladder. Verified in stepped Play Mode on Fragile (four floor drops: unstable at the first, failed at the fourth).
Violation 2 now spawns an enemy: `EscalationEffect.SpawnEnemy` makes `EscalationResponder` ask a scene `Spawner` (the
Crawler
prefab, three alarm points on the NavMesh, at most 2 alive) for a Crawler and report the player to its
`ChaseTargetMemory`, with a HUD notice. Verified in stepped Play Mode on Fragile (violation 2 spawns a chasing Crawler).
The dock bay is a separate NavMesh region, so an alarm Crawler cannot reach a player still in the dock bay (its path is
partial) and waits at the nearest reachable point.

Violation 3 makes the package hazardous: `EscalationEffect.PackageHazardous` activates a `PackageHazardEffect` on the
package, which shows a translucent green aura (`HazardAura`, `PH_Toxic`) and, every 0.4 s, applies the `contamination`
effect of the `DamageOverTime` mechanic on the player's `HealthCarrier` while the player is within 3.5 m of the package
(holding it counts). The effect is data on the Player prefab: 3 damage per 0.4 s tick, 1.2 s duration, damage type
`hazard`, so a refreshed effect keeps ticking about 7.5 damage per second and expires about a second after the player
leaves. Verified in stepped Play Mode: 100 to 79 health in 3 s while holding the package, no damage at 9 m, damage again
within 2 m. The ladder was widened for Restricted Cargo (it used to fail at violation 3), so all three ladders now match
the GDD table; with Blackout's dark-time violations that gives about 40 s of darkness before failure.
The violation buzz, unstable, and hazard cues are in; still to build: per-package Behavior components.
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

Blackout adds a battery puzzle: the breaker lever is dead until a battery (a `CarryObject` at the far end of the dock)
is carried into the generator in the dock bay. The battery was moved from behind the barricade; the revised route was
played with real keyboard and mouse events on 2026-11-09 (battery in both hands, generator, lever, delivery, payout 260,
no property fine).
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
  child, `Patrol` along its route, `DetectionSensor`, `ChaseTargetMemory`, `Projectile` bolts (`Cooldown`
  1.1 s, only with line of sight; the bolt damage is 10), `HealthCarrier` (45), `HitReaction`. On detection it disables
  `Patrol` and takes over
  with a `NavMeshAgent` keeping a 6 m standoff; after the memory expires it navigates to the nearest route waypoint and
  hands control back to `Patrol`.
- Both die through `EnemyDeath` (stop the behaviours, disable colliders, shrink with Tween, destroy).
- Both react to being shot: `EnemyAlert` listens to `HealthCarrier.DamageResolved` and, when the damage carried an
  incoming
  direction (hitscan, melee, and projectile hits do; the electric floor and damage over time do not), reports the player
  to the enemy's `ChaseTargetMemory`. The Warehouse binds the player to every alert in the encounter groups and to the
  alarm enemy it spawns. Verified in stepped Play Mode: a drone shot from behind turned and shot back, and a Crawler hit
  from 17 m, outside its 12 m notice range, started chasing. This closed the "shoot the drone in the back for free" gap
  found in the Express playtest; the drone was also toughened (45 health, 10 damage bolts every 1.1 s).

The game-side brains only route the template events (`TargetDetected` to `ReportTarget`, `StateChanged` to movement)
and own movement; the AI state logic stays in `ChaseTargetMemory`. The player moved to a new `Player` layer and enemies
to an `Enemy` layer, so sensors, melee, and the NavMesh bake (Default layer only) target the right things. The Warehouse
has a baked `NavMeshSurface` (`Scenes/Warehouse/NavMesh-Warehouse.asset`); the barricade and closed doors are baked as
obstacles, so enemies do not use the shortcut or powered doors. Enemies appear per contract through
`ContractSpec.Encounters` and `EncounterGroup`: Lights Out has Crawlers, Express a Security Drone, Restricted Cargo
both.
Verified: encounter activation, detection, chase, melee hit with knockback, drone patrol, pursuit, shooting, killing the
player, searching and returning to route, and enemy death. Not covered: a Spawner-driven wave, enemies hitting a
package,
enemies on the electric floor, and fairness tuning.

### Phase 9 — Player Combat (MVP weapons done, verified in stepped Play Mode)

- **Stun Pistol** (`StunPistol`): `HitscanWeapon` (15 damage, 3.5 shots/s, damage type `stun`), `AmmoReload` (8 rounds,
  24
  reserve, 1.4 s reload; an empty magazine reloads on the next trigger pull), `Recoil` (applied to the camera by
  `PlayerMotor`), `WeaponSpread` (1 degree). The aim point comes from the camera ray and the shot leaves the muzzle. A
  `LineRenderer` tracer shows the shot.
- **Crowbar** (`Crowbar`): `MeleeAttack` (18 damage, 0.55 s swing with a 0.15–0.3 s hit window) plus `Knockback` (7 m/s)
  and a Tween swing of the placeholder model. Enemies receive the push through the game-side `EnemyKnockbackReceiver`
  (a `NavMeshAgent` move that decays), the same pattern as the player's `PlayerKnockbackReceiver`.
- **Input and switching:** `PlayerWeapons` binds Fire, Reload, SwitchWeapon, Weapon1, and Weapon2 from
  `PlayerInputActions`
  (mouse, R, Q, 1, 2; gamepad right trigger, X, Y, D-pad). `WeaponSwitching` holds the two slots; switching cancels a
  reload. A small `WeaponHudView` shows the weapon name and the magazine/reserve count in both the Depot and the
  Warehouse.
- **Hands restriction:** `WeaponRules.CanUse` is the single rule. A held package blocks the Crowbar (the pistol is
  one-handed); a heavy carry blocks every weapon. A refused use raises `UseRefused` and a HUD notice.
- **Ammo and the result:** `ContractRun.RegisterShot` counts shots only in transit (`ShotsFired` in `ContractOutcome`).
  The Emergency Lockers add 8 reserve rounds on activation. The ammo expense settlement line itself is Phase 10.
- Verified with injected input: fire, recoil, reload, auto-reload on an empty magazine, 1/2/Q switching, three pistol
  shots kill a Crawler (40 health), two kill a Security Drone (30), the Crowbar knocks a Crawler back about 1 m, three
  Crowbar hits break the barricade, the package blocks the Crowbar but not the pistol, the battery blocks both, locker
  refill and the HUD update, and the shot count reaches the active run. Not covered: a gamepad, `HitFeedback` visuals on
  a
  pistol hit beyond the existing hit flash, and the recoil-to-package stability event (design in GDD 7.6).

The player's carried items now come from the Equipment Locker (see Phase 10), and the ammo expense is a settlement line.

Later: Shotgun, Repulsor (`ChargeWeapon` + `Projectile` + `Knockback`), HomingProjectile, Grenade, ExplosiveBarrel,
DamageOverTime, DamageZone. No new weapon framework.

### Phase 10 — Economy and Progression (remaining)

Contract settlement, `Rewards`, and `Wallet` credits work. Remaining: minimal courier progression using `Progression`,
`Stats`, `Equipment`; MVP upgrades (+MaxHealth, +CarryMoveSpeed, +ScannerRange or a weapon stat), bought with credits;
no large skill trees.

Settlement deductions are done: net payout = base + bonuses − property damage − ammo expense − death penalty, floored at
₡0 (`EconomyRules`: ₡3 per shot, ₡20 per property damage event, ₡25 per death; `SettlementLineKind.Deduction`). The run
counts shots, property damage, and deaths in transit, and the result screen lists each non-zero deduction. From the road
tier the net payout also loses a cargo damage penalty and optional vehicle repair.

**Shift rating and courier rank (done).** `ShiftRatingCalculator` grades four axes from the contract outcome on an
S–A–B–C–D–F scale and averages them (rounded half up) into an overall grade: delivery time (the share of the deadline
used,
skipped for contracts without a deadline), cargo condition (package integrity at delivery, recorded through
`ContractRun.RecordCargoIntegrity`), protocol compliance (package rule violations), and property damage (events).
`ContractSettlement.Rating` carries it, and the grade does not change the payout. `CourierRank` wraps a
`ProgressionService`
track (`courier.rank`, thresholds 100, 300, and 600 XP: Trainee, Courier, Senior Courier, Hazard Courier); an overall S,
A,
B, C, or D grants 120, 100, 70, 40, or 15 XP and F grants none. The Warehouse applies the rank after settlement, before
the
save, and the result screen shows the rating, the four axes, the XP gained, and a promotion line; the Depot header shows
the rank and progress. Verified in stepped Play Mode: a clean Standard Delivery rated S and promoted to Courier (+120
XP,
20 of 200 into the level), a delivery with property damage and two violations rated B (Property B, Protocol C), and
Continue
restored the rank. Nothing is unlocked by rank yet (contracts stay gated by prerequisites); candidates are a minimum
rank on
a contract, upgrade levels, and equipment ([GDD §14](GDD.md)).

**Upgrades (done).** `UpgradeDefinition` assets (data) build immutable `UpgradeSpec`s in an `UpgradeBook`; each upgrade
targets a `Stats` stat and has up to three levels with a price per level. `CourierStats` owns a `StatCollection`
(`courier.max_health`, `courier.move_speed`, `courier.reserve_ammo`) and rebuilds one modifier per upgrade from its
level;
`CourierUpgrades` spends credits through the `Wallet`, tracks levels, and captures/restores them. The three MVP upgrades
are Reinforced Vest (+25 max health per level), Courier Boots (+8% move speed per level), and Ammo Pouch (+8 reserve
pistol rounds per level); prices are 100/200/350, 120/240/400, and 80/160/300. `PlayerSession.ApplyStats` pushes the
stats
to `PlayerVitals` (`HealthPool.SetMaximum`), `PlayerMotor` (speed multiplier), and `PlayerWeapons` (reserve bonus,
applied
as a delta so a purchase in the Depot is not double counted). The Depot has a physical Upgrade Terminal
(`PH_UpgradeTerminal`
with an `Interactable`, `UpgradeTerminalView`, three `UpgradeSlot` rows) that saves the campaign after every purchase.
Verified in stepped Play Mode: buying the vest through the terminal (E, then the Buy button) raised max health to 125,
wrote `campaign.json`, and a restart through Continue restored credits, the level, and the stats; boots and pouch
applied
in the Warehouse (speed 1.08, reserve 40 of 24 base); the result screen showed `Base pay 150, Ammo expense -9, Property
damage -20, Recovery team fee -25, Total 96`. The first MVP list in the GDD (+Max Health, +Carry Move Speed, +Scanner
Range or a weapon stat) is adapted: there is no carry slowdown or scanner yet, and the library has no runtime damage or
reload modifier, so move speed and reserve ammo stand in.

**Equipment Locker (done).** `CourierLoadout` owns an `EquipmentLoadout` (slots Primary, Tool, Utility 1, Utility 2;
definitions Stun Pistol, Crowbar, Medkit) and an `InventoryContainer` (the medkit stock, up to 3 owned). The locker in
the
Depot (`PH_EquipmentLocker` with an `Interactable`, `EquipmentLockerView`, five `LockerRow`s) can stow or equip each
weapon, buy a medkit for ₡30 (`Wallet.Spend`), and move medkits between the stock and the two utility slots; every
change
saves the campaign. In the field `PlayerMedkits` handles `UseMedkit` (H, D-pad up): it consumes an equipped medkit and
heals 40, and does nothing at full health or with only stocked (unequipped) medkits. `PlayerWeapons` reads the loadout,
so
a stowed weapon cannot be selected or used and the weapon HUD shows "No weapon" when both are stowed; `MedkitHudView`
shows the carried count. Stowing the pistol is the way to a no-shots run (and no ammo expense). Verified in stepped Play
Mode: buy two, equip one, stow the pistol (current weapon fell back to the Crowbar, Weapon1 and Fire did nothing), H at
full health kept the medkit, H after 55 damage healed to 85 and consumed it, Continue restored the stowed pistol, stock,
and credits, and the Warehouse showed the HUD and healed 40 on H. The Shotgun, Repulsor, Scanner, and Portable Light
(Demo tier) will make the Primary and Tool choice real; the suit slot and the grenade wait for their items.

### Phase 11 — Persistence (remaining)

Credits, quest progress, completed contracts, the reward ledger, purchased upgrade levels, the loadout (equipment
snapshot,
medkit stock, next medkit instance), and the courier rank experience are saved (`CampaignSave` version 6; versions 2 to
5
still load with no upgrades, the default loadout, and the Trainee rank; unknown upgrade ids are ignored and levels are
clamped to the current maximum). A loadout snapshot that fails to restore falls back to the default. Remaining:
collectible
progress. Version 6 also records pending settlement totals, ratings, grant identity and rank baseline; payment is
checkpointed before grant and resumed on Continue or Return to Depot through the existing Rewards/Wallet ledgers. Do not
serialize arbitrary active scene physics. Checkpoint/Respawn handles temporary level death.

### Phase 12 — Full Module Integration

First-delivery Tutorials integration is in the MVP objective HUD, advanced by gameplay actions.

After the core game is fun, integrate deliberately: Analytics, Achievements, Audio, Addressables,
StatusEffects, Collectibles. Every integration needs an actual gameplay or product purpose; never use a module just to
claim coverage if that damages architecture.

### Phase 13 — MVP Contract Content (remaining)

Rules, ladders, and the Blackout state are configured per contract as data (Phase 4 and Phase 7). What is left, on the
single Warehouse map:

| #  | Contract          | Done                                                                                         | Still needs |
|----|-------------------|----------------------------------------------------------------------------------------------|-------------|
| 01 | Standard Delivery | Physical pickup and delivery                                                                 | —           |
| 02 | Fragile           | `DoNotDrop`, ladder 1 / 2 / 3 / 4                                                            | —           |
| 03 | Lights Out        | `KeepInLight`, Blackout, battery and generator, ladder 1 / 2 / 3 / 4, Crawlers               | —           |
| 04 | Express           | `DeliveryDeadline` (45 s, on-time bonus under 22 s), Security Drone and Crawlers, played end to end with real input                                | —           |
| 05 | Restricted Cargo  | `DoNotDrop` + `KeepInLight` in Blackout (battery puzzle), ladder 1 / 2 / 3 / 4, both enemies | —           |

All five use the same Warehouse location with different configuration or state. Do not create five maps.

**Historical scripted runs (stepped Play Mode).** These predate the battery move and do not establish human playtest
acceptance or verify the current Blackout route.

- *Express.* Route A (breaker lever, maintenance door, north corridor, freight lift, scanner, hatch, terminal) takes
  about
  24 s; the payout was 320 base + 75 on time − 9 ammo. The drone patrols the north corridor (waypoints (-16, 0.5), (-16,
  11.5), (10, 11.5)) and was shot in the back from the west aisle without ever detecting the player, so it is a
  speed bump rather than a threat (fixed afterwards: enemies now react to being shot, see Phase 8).
- *Restricted Cargo.* The package rules (Do Not Drop, Keep In Light) and both encounters load. Route B works: with the
  package held the Crowbar is refused, so the barricade takes three pistol shots (₡9 ammo plus the ₡20 property fine and
  a lost no-violations bonus); the ramp leads to the delivery floor and the scan, hatch, and terminal work. Payout in
  the
  recorded run was 450 − 54 ammo − 40 property damage − 25 recovery fee = 331.
- *Crawlers.* Two Crawlers in the open are easy for a stand-and-shoot player (8 shots, 2.2 s, no damage taken from a 10
  m
  notice distance). The danger is the electric strip beside the route: a Crawler hit knocks the player about 1.5 m, and
  the
  strip deals 30 damage per 0.5 s tick, so a fight at the strip's south edge kills in about a second and, with the
  package
  held, adds a Do Not Drop violation (unstable package) on respawn.
- Known library warning: dropping the package logs "Setting linear velocity of a kinematic body is not supported" from
  `PickupItem.Drop` (it writes `linearVelocity` on a kinematic rigidbody). It is harmless; a fix belongs in
  `UnityTemplates` and needs the owner's agreement.

**Restricted Cargo puzzle (previous layout).** The contract runs in Blackout, so Keep In Light and the Lights Out
battery puzzle
apply together with Do Not Drop and both enemies (data change only: `LocationState.Blackout`). Verified in stepped Play
Mode: barricade (three shots), battery pick-up, carry back through the gap, generator, and breaker lever restore the
lights; the fastest scripted run took about 10.5 s, and the package is dark for the first 10 s, so one Keep In Light
violation (unstable package) is close to unavoidable, the alarm Crawler at violation 2 follows after 20 s of darkness,
the
hazardous package after 30 s, and the contract fails at 40 s (the same ladder as Lights Out). The carry breaks if the
battery lags more than 2.5 m behind the carry point,
so a very fast 180-degree turn while holding it drops it; the generator must be used from about 1.5 m. The battery is
now
in the dock; the revised route and its clean payout were verified later with real input (see Phase 7).

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

| Decision                                                   | Options                                                                                                          | Notes                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
|------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Who owns vehicle driving?                                  | (a) minimal arcade controller in `Assets/Game/Gameplay/Vehicle`; (b) a new reusable mechanic in `UnityTemplates` | The library has none. (b) fits the project's purpose                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| Fuel representation                                        | `Wallet` currency committed per road segment; `Stats` value; `Stamina`                                           | Settle in the audit                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| Hinged vehicle/building doors                              | Keep `Door` slide-only; or extend the template                                                                   | `Door` only slides                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| **`SceneFlow.ChangeSceneAsync` defect** (UnityTemplates)   | Fix in the template: skip `SetActiveScene` when the loaded scene is already active                               | In Unity 6000.6 `SceneManager.SetActiveScene` returns `false` for the scene that is already active, which is the case right after a `Single` load, so `SceneFlow` throws `InvalidOperationException: Failed to set scene ... as active` although the scene loaded. Night Courier avoids it through additive swapping and does not depend on the fix, but every other consumer of `ChangeSceneAsync` is affected. Not changed here because UnityTemplates is a separate repository with work in progress |
| New Game overwrites the existing save without confirmation | Resolved: a `ConfirmDialog` asks first (see Phase 2b)                                                            | —                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |

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

| Item          | Required                                       |
|---------------|------------------------------------------------|
| Package rules | `DoNotDrop`, `KeepInLight`, `DeliveryDeadline` |
| Enemies       | `SecurityDrone`, `Crawler`                     |
| Contracts     | 5                                              |
| Locations     | 1 Warehouse                                    |

### Additional Acceptance Criteria

The slice is not done on a feature count. It is done when these also hold; each is checked by a run, not by reading
code.

| Criterion                                                                                                    | Evidence so far                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
|--------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Every contract has its own goal, risk, and at least one differing player action, not only a different payout | Verify contract data and the differing actions in Play Mode runs per contract (Phase 13); document unverified route and encounter behavior.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| Cargo damage and rule violations are reproducible, explained, and shown honestly                             | Reproducibility was checked per rule in stepped Play Mode (Phase 4). Damage notices now name the cause (impact or electric floor) and size, and the result screen shows the deduction lines, the rating and axes, the violation count, final integrity, property damage, and deaths                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| At least one contract can be finished without firing                                                         | Stowing the pistol in the Equipment Locker gives a no-shots run (Phase 10)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| Failure, death, abort, replay, and quitting never cause a double payout, a lost profile, or a blocked UI     | EditMode (`ContractBoardTests`, `CampaignFlowTests`): a failed contract returns to Available with no reward and pays once on retry, a second delivery callback and concurrent settlements pay once, an abort during settlement is refused, and a save taken mid-run restores to a clean Depot. PlayMode (`Assets/Game/Tests/PlayMode`, four tests, state-driven input): double Accept, double delivery press, double Return, payout once, Continue restoring credits, rank, and contracts, a destroyed package failing without payout and the retry paying once, abort from the pause menu, death respawn with the recovery fee. Played through editor scripts: abort ignored while delivered, the Depot recovering from a stuck run. Not exercised: real keyboard, mouse, or gamepad input (manual script in Playtest-Checklist.md), a failed disk write |
| The result screen tells the player what to change next time, not only the numbers                            | Up to three improvement lines from `SettlementAdvice` cover broken rules, overdue delivery, cargo, property damage, death, or a clean run. Verify the relevant result screens and advice in Play Mode.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |

---

## Per-Change Checklist

- The affected feature is listed in [Template-Mapping.md](Template-Mapping.md), and the listed template was used.
- No item from the strict-constraints list was introduced.
- All new text is English and goes through localization keys.
- Code compiles with zero errors and warnings; the Console is clean.
- The relevant scene was opened, entered in Play Mode, and exercised end to end.
- Save/load and scene transitions still work when touched.
- Docs are updated in the same change if behavior, mapping, or scope changed.