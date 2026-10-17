# Night Courier: Rule 13 — Game Design Document

> Every package has rules. Breaking them is not recommended.

| | |
|---|---|
| **Working title** | Night Courier: Rule 13 |
| **Genre** | Night-courier job game: prepare and load cargo, drive an arcade van, deliver anomalous packages on foot. Light horror/action with immersive-sim elements |
| **Platform** | Windows, Steam. Keyboard + mouse and gamepad |
| **Players** | Single-player |
| **Engine** | Unity 6000.6, URP |
| **Session length** | 15–30 minutes for an early shift (one contract); 30–60 minutes for later multi-delivery shifts |
| **First complete version** | 2–4 hours |
| **Language** | English (source and only language until the localization phase) |
| **Status** | Pre-production. This document is the design source of truth |

Companion documents:

- [Template-Mapping.md](Template-Mapping.md) — which template module or mechanic implements each feature.
- [Production-Plan.md](Production-Plan.md) — phases, milestones, architecture constraints, and verification.
- [Asset-Plan.md](Asset-Plan.md) — placeholder-first art plan: what primitives stand in now and which assets replace them later.
- [../AGENTS.md](../AGENTS.md) — rules for AI agents and contributors working in this repository.

---

## 1. Project Goal: Template-First Development

**The main goal of this project is to validate and exercise the existing `UnityTemplates` library in a real game.**

Night Courier is built by composing the `Modules` and `Mechanics` from the sibling `UnityTemplates` repository (linked as local UPM packages, see [../TEMPLATE-PACKAGES.md](../TEMPLATE-PACKAGES.md)). The game must not grow a second framework on top of them.

Principles:

1. **Compose, do not duplicate.** `Modules` provide building blocks (Save, Health, Wallet, Conditions, …). `Mechanics` provide finished gameplay features (Door, Lever, HitscanWeapon, Patrol, …). If a template already owns a responsibility, the game uses it.
2. **Game code is glue.** Game-specific code lives in `Assets/Game` and only wires templates together and holds game-specific policy: contracts, package rules, content, balance, presentation.
3. **Use everything that fits, nothing that does not.** The aim is to use the whole library across the full game, but every use must have a real gameplay or product purpose. A module is never used just to claim coverage if that damages the architecture. Unused items are listed with a reason in [Template-Mapping.md](Template-Mapping.md). A reusable library learns more from finding out that a module is not needed than from forcing it in.
4. **One contract uses 5–10 mechanics.** The full game uses the whole library; a single contract uses a small combination of it. Variety comes from combinations, not from adding every mechanic everywhere.
5. **Templates change only for a proven deficiency.** If the game can be built through composition, the templates stay untouched. A real reusable-library deficiency is fixed in `UnityTemplates` with the smallest possible API change, never worked around with a game-side copy. Game-specific reactions are layered on top of existing events instead of extending a reusable mechanic.
6. **Pending mechanics are expected.** Ten more mechanics will be added to the library later (see section 13.4). The design already reserves their roles; the game must not build local clones of them.

Not applicable to this game: **Haptics** (Android/iOS only; this project targets Windows/Steam). See [Template-Mapping.md](Template-Mapping.md).

---

## 2. High Concept

A single strange courier company serves the city after midnight. The player is its newest night courier.

Almost none of the deliveries are ordinary. Every package ships with handling instructions:

```text
PACKAGE #1042
- Do not drop.
- Do not leave in the dark for more than 10 seconds.
- If you hear knocking from inside, do not open it.
```

### 2.1 Central Hook

> **You work as a night courier of anomalous cargo. You choose contracts, prepare your equipment and your van, physically load dangerous packages, plan a route, survive the road, obey the strange rules of the cargo, and try to finish the shift with a profit.**

Short form: every package changes the rules of the level, every mistake has physical consequences, and a contract can be completed in more than one way.

The same building plays completely differently depending on the cargo, the state of the night, and the way the player chooses to solve it.

The whole job, as one chain:

```text
PREPARE → LOAD → SERVICE THE VAN → DRIVE → ROAD INCIDENTS → DELIVER ON FOOT → RETURN → REPAIR / EXPENSES / UPGRADES
```

Preparation brings planning, the van brings a change of pace and a sense of travel, delivery brings the main systemic play, and maintenance and economy tie separate missions into long-term progression. Visually it stays cheap: one low-poly van, modular industrial/city packs, night lighting, and a limited set of creatures.

### 2.2 Design Pillars

| Pillar | Meaning |
|---|---|
| **Delivery** | The objective is always clear: take object A to place B. Difficulty comes from the journey, not from unclear rules. |
| **Package Rules** | Each package has 1–3 handling rules. Rule combinations create replayability. |
| **Cargo as a Participant** | A package is not a weight in the player's hands. It behaves, reacts, and escalates. |
| **Environmental Problems** | The route is rarely open: locks, power, platforms, barricades, anomalies. |
| **Survivable Threats** | The player is not a soldier. Combat is a tool for survival, with few but distinct weapons, and every shot has a price. |
| **Many Ways Through** | Shoot, lure, lock out, break through, or sneak around. Each way costs something different. |
| **Systemic Combinations** | New situations come from combining existing mechanics, not from adding content (see section 22). |
| **The Whole Job** | Preparation, loading, van upkeep, the road, delivery, and expenses are one connected system, not separate mini-games (see 4.4). |
| **Two Characters** | The courier and the van are both developed and both matter: the state of one changes the other. |

### 2.3 Core Fantasy

You are underpaid, under-informed, and trusted with something that should not exist. Follow the rules and get paid, or find out what the rules were hiding.

---

## 3. Scope and Targets

- First-person only. No full-body player model is required.
- **One drivable van**, with arcade handling, played on short authored road segments (2–4 minutes each) instead of an open world. The van is also a checkpoint, a safe zone, and cargo storage. Until the road tier is reached (section 3.2), the van is only a scene-transition device: select destination, doors close, scene changes.
- Small, reusable micro-locations (5–10 minutes each) replayed under many contracts. No procedural level generation.
- No crowds, no complex dialogue, no motion capture, no large animation sets.
- **Hard limits** (each would multiply the project size): no open-world city, no traffic AI, no pedestrians, no second vehicle, no cosmetic vehicle customization, no realistic car simulation, no day/night cycle simulation, no police system, no fuel stations around the city, no large garage system.
- Architecture must not prevent a later Android port, but no mobile-specific work is planned.

### 3.1 Content Targets

| Milestone | Locations | Road segments | Deliveries per shift | Contracts | Package rules | Enemies | Weapons/tools | Notes |
|---|---|---|---|---|---|---|---|---|
| **MVP** | Depot + 1 Warehouse | 0 (van is a scene transition) | 1 | 5 | 3 | 2 | 2 | 20–30 minutes of play |
| **Road Alpha** (first step of the Demo tier) | Same | 1 | 1 | Same 5 | Same | Same | Same | Drivable van, cargo loading, cargo stability, garage basics. Built only after the MVP loop is fun |
| **Steam Demo** | Depot + 2 | 2–3 | 1–2 | 10–12 | 6 | 3 | 3 | Progression, save, settings, localization hooks, achievement subset, route choice, first van upgrades; 45–60 minutes |
| **Release 1.0** | 4 | 5–6 | 2–4 | 25–30 | 10–12 | 5 | 5–6 | 15–20 upgrades (courier and van combined), 15 achievements. Do not exceed |

The weapon/tool count covers offensive and tool items. Consumables (Medkit, Battery) are not counted.

Do not start the next milestone until the current loop is actually fun. If a milestone is not fun, fix it before expanding.

### 3.2 Feature Tiers

Most of the systems below are rich on purpose; they are **not** all MVP. Each system has a tier and is built only when its tier is reached.

| System | MVP | Demo | 1.0 |
|---|---|---|---|
| Package model: Rule / Behavior / Escalation (7.1) | 3 rules | 6 rules | 10–12 rules |
| Contract types (6.6) | DELIVER | + RECOVER, DISPOSE | + TRANSFER, EXTRACT, MULTI-DROP, ESCORT, RETURN TO SENDER |
| Location states (6.7) | Normal, Blackout | + Lockdown, Security Alert | + Structural Damage, Spatial Instability, Flooded/Toxic |
| Complications (6.8) | — | Power Failure, Security Sweep, Blocked Route, Wrong Recipient | The rest |
| Recipients (6.9) | Normal | + Verification | + Missing, Hostile, Suspicious |
| Company vs. contact decisions (6.10) | — | — | A few |
| Special contracts (6.11) | — | Emergency Lockdown (optional) | No Equipment, Cleanup, Courier Hunt |
| Persistent consequences (6.12) | — | A few flags | A few more |
| Shift rating, property damage, ammo expense (14.3–14.5) | Yes | Yes | Yes |
| Hands restriction while carrying (9.7) | Yes | Yes | Yes |
| Incomplete instructions + Courier Archive (7.5) | — | Yes | Yes |
| Cargo that changes the player (7.6) | — | Heavy Mass | + others |
| Package sensors, multi-package contracts (7.7, 7.8) | — | — | Yes |
| Non-lethal tools (11.3) | — | Repulsor | + others within the 5–6 cap |
| Safe Locker checkpoints, recovery-team respawn (9.3, 13.2) | Yes | Yes | Yes |
| Van as scene transition + checkpoint (5.3) | Yes | — | — |
| Drivable arcade van, one road segment, cargo stability, collision zones (8.3, 8.9) | — | Road Alpha | Yes |
| Physical cargo loading, cargo points, securing (5.4, 5.5) | Simple pickup into the van | Road Alpha | Yes |
| Garage, persistent van condition, Van Inspection screen (5.7, 14.9) | — | Road Alpha (basic) | Yes |
| Preparation slots for personal and van loadout (5.2) | Personal only | Yes | Yes |
| Van faults and pre-trip inspection (5.6) | — | Yes | Yes |
| Route planning, junctions, road events (8.4, 8.5, 8.7) | — | Yes | Yes |
| Van upgrades, up to 3 levels per line (14.8) | — | 3 lines | All lines |
| Cargo reactions to driving (7.9) | — | Fragile, Restless | The rest |
| Road rules: Do Not Stop, Avoid Strong Impacts (7.2) | — | Yes | Yes |
| Multi-delivery shifts and loading order (4.4, 6.2) | One contract | 1–2 | 2–4 |
| Stop and repair on the road (8.8) | — | Yes | Yes |
| Return-trip contracts (6.11, 8.11) | — | — | A few |

Within the Demo tier the order is: Road Alpha → route choice and preparation slots → van upgrades → two-delivery shifts.

---

## 4. Core Loop

### 4.1 One Contract on Foot (the MVP Loop)

```text
DEPOT
  ├─ Choose contract
  ├─ Choose equipment
  └─ Collect package
        ↓
LOCATION
  ├─ Explore
  ├─ Solve access problem
  ├─ Follow package rules
  ├─ Avoid, lure, or fight threats
  ├─ Find optional loot
  └─ Deliver
        ↓
RESULT
  ├─ Base reward
  ├─ Bonuses
  ├─ Deductions (property damage, ammo, penalties)
  └─ Shift rating
        ↓
DEPOT
  ├─ Upgrades
  ├─ New equipment
  └─ Next contract
```

### 4.2 Meta Loop

Accept contract → prepare → pick up cargo → travel → reach the address while obeying rules → survive problems → deliver → receive credits and rating → buy upgrades → next contract.

### 4.3 Application Flow

```text
Startup → Main Menu → Depot → Contract Location → Contract Result → Depot → (next shift)
```

- **Startup** builds application services in dependency order (save, settings, audio, input, localization, progression, …).
- **Contract Result** is a screen shown at the end of a contract, not a separate scene.
- A new game starts in the Depot with the first contract acting as the tutorial.
- From the road tier on, a **Road** segment sits between the Depot and the Location, and between Locations when a shift has several deliveries (see 4.4). Before that tier the van is a scene transition.

### 4.4 Full Shift (Road Tier)

Early game: one contract per shift. Later: 2–4 deliveries per shift. The player loads several packages and leaves; the shift becomes route planning plus logistics.

```text
PLAN SHIFT
  ↓
LOAD VAN
  ↓
CHOOSE ROUTE
  ↓
DRIVE
  ↓
DELIVER
  ↓
DRIVE
  ↓
DELIVER
  ↓
UNEXPECTED EVENT
  ↓
DELIVER
  ↓
RETURN
  ↓
MAINTENANCE
  ↓
PROFIT
```

State flows both ways between the road and the on-foot part (see 8.10): the courier and the van are two halves of one character, and preparation, driving, and delivery are one system rather than three mini-games.

---

## 5. The Depot (Hub)

A small room: **Courier Depot**.

| Station | Purpose |
|---|---|
| **Contract Terminal** | Lists available contracts, shows details, accepts one |
| **Archive Terminal** | Corporate instructions and Courier Notes about known cargo (Demo+, see 7.5) |
| **Equipment Locker** | Choose the loadout for the shift |
| **Upgrade Terminal** | Spend credits on permanent upgrades |
| **Garage** | Inspect the van, repair, install modules, refuel/recharge (road tier, see 5.7) |
| **Package Conveyor** | Delivers the physical package for the accepted contract |
| **Van (Exit)** | Select destination, start the trip |
| **Storage** | Flavor and small unlocks; optional |

The Depot is safe: no enemies, no rule enforcement.

### 5.1 Contract Terminal Example

```text
DELIVERY #0184

Type:          DELIVER
Destination:   Blackwood Apartments
Site state:    BLACKOUT
Distance:      4.2 km (road tier)
Known threats: Security drones
Cargo:         Biological Sample
Rules:         • Do not drop
               • Keep illuminated
Expected risk: MEDIUM
Base pay:      ₡320
Perfect delivery bonus: +₡120
```

The terminal offers about three contracts at a time. The player picks one. Complications are not announced in advance. The company's "expected risk" is a guess, and it is sometimes wrong.

### 5.2 Preparation

Before leaving, the player has 2–3 minutes of preparation instead of a bare "Start Contract" button. The briefing shows the cargo, its rules, known threats, the distance (road tier), and the site state. Then the player decides what to take:

```text
Destination:   Greyline Warehouse
Cargo:         Object 031
Rules:         KEEP IN LIGHT · AVOID STRONG IMPACTS
Known threats: Security drones
Distance:      4.2 km
```

- Portable Light or not? Shotgun or Repulsor?
- Extra fuel? A repair kit? A cargo stabilizer?

This is what makes `Equipment`, `Inventory`, and `Wallet` matter.

**Limited slots are the key to interesting decisions.** The player cannot take everything.

| | Slots |
|---|---|
| **Personal** | Primary tool 1 · Secondary tool 1 · Utility 2 · Suit 1 (Demo+) |
| **Van** (road tier) | Cargo module 1 · Vehicle module 2 · Emergency slot 1 |

The flashlight is always carried. The two personal tools come from the Primary and Tool lists in any combination (for example Pistol + Scanner, or Crowbar + Repulsor).

| Contract | Good choice | What it costs |
|---|---|---|
| Fragile cargo | Cargo stabilizer, repair kit | No room for an extra battery |
| Keep In Light | Backup generator, Portable Light | The van is less protected |

A poor loadout never makes a mission impossible, but it makes it harder. See section 11.

Starting equipment: Flashlight, Stun Pistol, Crowbar, Medkit. Later: Shotgun, Scanner, Containment Grenade, Repulsor, Armor.

MVP: only the personal loadout exists; van slots arrive with the road tier.

### 5.3 The Van

The player develops two entities:

| Courier | Van |
|---|---|
| Health, Stamina, Equipment, Inventory, Weapons/Tools | Condition, Fuel, Cargo Capacity, Cargo Protection, Headlights, Engine, Tires, Body |

This is not a car RPG with twenty parts. The first version tracks four parameters:

```text
Vehicle Condition · Fuel · Cargo Stability · Headlights
```

**MVP behavior.** The van is a scene transition:

```text
Enter van → choose destination → doors close → scene transition → location
```

**Road tier behavior.** The player drives it along a short road segment (see 8.3 and 8.9) and parks at the destination.

In both tiers the van is also a temporary safe zone, a checkpoint, equipment storage, and the return point.

### 5.4 Cargo Loading

The player loads the cargo physically (road tier; before that, a package is simply picked up and put in the van):

```text
Package Conveyor → Cargo Platform → carry or push → Van
```

- Small cargo: `PickupDrop`.
- Medium and heavy cargo: `CarryObject`.
- Large crates: `PushPullObject` along flat ground. That mechanic is grounded and explicitly does not handle slopes, so there is no loading ramp; the van is loaded from a flat dock or a tail-lift (`MovingPlatform`) level with the cargo floor.
- The player opens the rear doors and places the container inside.
- `ConveyorBelt` (pending) delivers new cargo to the platform.

This gives the mechanics a purpose before the trip even starts.

### 5.5 Cargo Placement and Securing

**Cargo points.** The van has four cargo points:

```text
[ A ][ B ]
[ C ][ D ]
```

Some cargos cannot sit next to each other, which matters most in late multi-package contracts:

| Cargo | Constraint |
|---|---|
| Object 18 | Emits heat |
| Object 32 | Must stay cold |
| Object 12 | Interferes with electronics; cannot be placed next to the Backup Generator |

Adjacency checks are `Conditions` rules over the placement.

**Cargo securing.** After placing a cargo, the player secures it:

```text
Cargo Slot → Hold E — Secure Cargo
```

An unsecured cargo slides when cornering and loses stability. `HoldInteraction` is pending; until it is installed, securing is an instant interaction.

**Weight.** Cargos are Light, Medium, or Heavy. Heavy cargo lowers acceleration and lengthens braking distance. The same cargo that slowed the courier on foot (`CarryObject`) now affects the van.

**Van doors** (driver, rear left, rear right) are gameplay objects, using the `Door` mechanic if it fits (to be checked in the audit). A badly closed rear door may drop cargo on the road. The checklist is not tedious: after the first contracts, normal operations are automatic and the inspection appears only when something is wrong.

### 5.6 Pre-Trip Inspection and Van Faults

A very short procedure, not a car simulator. Sometimes before a shift one fault is present:

```text
Headlight damaged · Tire pressure low · Battery weak · Cargo door damaged
```

The player can **repair now** (money, a repair part, or time; `HoldInteraction` is pending), **replace**, or **ignore** and risk the trip.

Every fault must be felt, not shown as a percentage:

| Fault | Effect | Hurts most when |
|---|---|---|
| Headlights damaged | Light range falls | Cargo is Keep In Light |
| Engine damaged | Acceleration and top speed fall | Contract is timed |
| Tire damaged | Steering stability falls | Cargo is fragile |
| Cargo bay damaged | Cargo stability falls | Cargo is fragile |
| Battery weak | Electrical failures are likelier | Cargo is electric |

Collision damage uses three hit zones: **Front**, **Side**, **Rear**. A rear collision is the most dangerous: cargo stability damage ×2. There is no mesh deformation.

### 5.7 Garage and Maintenance

The Depot gains a second important area, the **Garage**: inspect the van, repair it, install modules, refuel or recharge.

- Damage is **not** repaired automatically after a trip. It persists between shifts (Save).
- The van keeps working at, say, 40% condition; it just performs worse. This must not become survival micromanagement.
- Each shift ends with the **Van Inspection** (see 14.9): repairs cost money, and the player decides whether to repair fully or save for an upgrade.
- Upgrades are material. The first installation of each upgrade line is physical: buy a battery, a package appears, carry it to the van, install it. Later levels apply without the ritual.
- Physical parts (battery, fuel cell, spare tire, cargo stabilizer) are `CarryObject` items.

---

## 6. Contracts

### 6.1 Contract Anatomy

| Field | Description |
|---|---|
| Id | Stable identifier |
| Title | Localization key |
| Type | DELIVER, RECOVER, … (6.6) |
| Destination | Location + delivery point |
| Road | Road segment(s) and road state; route options (road tier, 8.3) |
| Location state | Normal, Blackout, … (6.7) |
| Package definition | What is carried and how it behaves |
| Complication | Optional hidden event (6.8) |
| Recipient | Recipient type (6.9) |
| Base reward | Credits |
| Time limit | Optional |
| Package rules | 1–3 rules |
| Optional objectives | Bonus goals |
| Decision | Optional company-vs-contact choice (6.10) |
| Risk | Difficulty rating shown to the player |

### 6.2 Lifecycle

```text
Available → Accepted → PackageCollected → InTransit → Delivered → Completed
                                                     ↘ Failed
```

Player-facing steps:

```text
Accept contract → Collect package → Reach destination → Deliver package → Return / Complete → Reward
```

**Shift.** A shift wraps one or more accepted contracts. In the MVP a shift is exactly one contract; the data model must still treat a shift as a list. Later, a shift holds 2–4 deliveries: the player loads several packages, chooses the delivery order, and may be unable to take every offered contract. Example: three offers (₡200 safe, ₡350 fragile, ₡600 restricted/dangerous) with room in the van for two. Wallet, Rewards, Progression, and Equipment become one economy.

Shift states: `Planned → Loaded → InTransit → Delivering → Returning → Settled`.

### 6.3 Optional Objectives

```text
No violations · No damage · Under target time · Collect lost parcel · No kills
```

### 6.4 Reward Breakdown

```text
Base pay              300
Time bonus             75
No violations         100
Optional parcel        50
Property damage       -80
Ammo expense          -48
Cargo damage penalty  -50
Vehicle repair        -75
--------------------------
Total                 272
Shift rating            B
```

Numbers are initial tuning values. Death applies a further penalty (default −15% of payment). The net payout never drops below ₡0. Vehicle repair is optional and is paid at the Van Inspection (14.9); the player may defer it at the price of a worse van next shift. Cargo damage penalty and vehicle repair appear only from the road tier.

### 6.5 Contract = Package + Location + Location State + Complication

A contract is assembled from four independent parts, not authored as a separate level. This is preferred to procedural generation.

```text
PACKAGE  +  LOCATION  +  LOCATION STATE  +  COMPLICATION
```

Examples:

| Package | Location | State | Complication |
|---|---|---|---|
| Living Specimen | Warehouse | Blackout | Security Sweep |
| Fragile Core | Apartments | Normal | Recipient Missing |
| Restless Box | Underground Station | Lockdown | Time Limit |

Four maps can support thirty contracts because each contract changes: destination, door states, enemies, package rules, hazards, power state, optional objectives, loot, state, and complication.

Contracts are configured data applied to a location, not separate levels or separate classes. In the road tier the same scheme selects the road: **Road + Road State + Cargo + Event** (see 8.3). Valid combinations are curated by the designer: tooling may suggest combinations, but every shipped contract is reviewed and approved by hand.

AI assistance is a development-time tool only: generate many candidate combinations (cargo, route, location state, complication, reward, special rule), filter them, keep the good ones, and tune by hand. No content is generated by a language model at runtime.

### 6.6 Contract Types

Still one game about a courier; the loop shifts with the type.

| Type | Loop | Tier |
|---|---|---|
| **DELIVER** | Collect → deliver | MVP |
| **RECOVER** | Find a lost package → bring it back | Demo |
| **DISPOSE** | Deliver an object to an incinerator or destroyer | Demo |
| **TRANSFER** | Move an object between two containment zones | 1.0 |
| **EXTRACT** | Reach a dangerous object → carry it out | 1.0 |
| **MULTI-DROP** | Several small cargos → several points | 1.0 |
| **ESCORT** | The object moves on its own; the player accompanies it | 1.0 (optional) |
| **RETURN TO SENDER** | A previous delivery went wrong (see 6.12) | 1.0 |

### 6.7 Location States

Before a location loads, the contract also selects the "state of the night":

| State | Effect on the same Warehouse | Tier |
|---|---|---|
| **Normal** | Ordinary routes | MVP |
| **Blackout** | Some doors dead; battery to carry; light-dependent rules become dangerous | MVP |
| **Lockdown** | Security drones, locked doors, alternative routes | Demo |
| **Security Alert** | Patrols reinforced; sensors armed | Demo |
| **Structural Damage** | Breakable barricades, unstable platforms, detours | 1.0 |
| **Spatial Instability** | Teleporters, wrong doors, rearranged passages | 1.0 |
| **Flooded / Toxic** | Hazard zones with damage over time | 1.0 |

Four maps then offer the situational variety of 15–20 levels.

### 6.8 Complications

At most **one** major complication per short contract. Not random chaos every two minutes: the player must be able to plan.

```text
Power Failure · Wrong Recipient · Security Sweep · Package Awakening · Elevator Failure
Blocked Route · Enemy Migration · False Alarm · Second Package · Emergency Recall
```

Complications are triggered by a scripted condition, not by a dice roll.

### 6.9 Recipients

The destination itself is part of the gameplay.

| Recipient | Behavior | Tier |
|---|---|---|
| **Normal** | Open the delivery locker → done | MVP |
| **Verification** | Present keycode, ID, correct package (gated by Conditions) | Demo |
| **Missing** | The point is empty; find another terminal or new instructions | 1.0 |
| **Hostile** | The player realizes the package should not have been delivered | 1.0 |
| **Suspicious** | Offers more money to hand over the cargo to them instead of the company | 1.0 |

### 6.10 Company vs. Contact Decisions

No large branching quests. A few contracts present a single choice:

```text
COMPANY:
Deliver Object 12 to the incinerator.

UNKNOWN CONTACT:
I'll pay triple. Bring it to basement B4.
```

The outcome sets reputation flags, unlocks or locks contracts, and may feed achievements. Tier: 1.0, a handful only.

### 6.11 Special Contracts

| Contract | Concept | Tier |
|---|---|---|
| **Emergency Lockdown** | Waves of drones, crawlers, and a wisp | Demo/1.0 |
| **No Equipment** | Cargo interferes with electronics: the company confiscates the loadout. Only a flashlight and the package. The player must use doors, levers, barrels, teleporters, platforms, and AI avoidance | 1.0 |
| **Cleanup** | Something already happened. Find 3 damaged containers, stabilize, return them. Uses damage zones, damage over time, spawners, breakables, collectibles, carry objects | 1.0 |
| **Courier Hunt** | The previous courier vanished with the cargo. Follow Courier Tags, find their checkpoints, understand what happened, recover the cargo. No NPCs, no cutscenes | 1.0 |
| **Return to Depot Immediately** | After the delivery something went wrong. The player must actually drive back (see 8.11) | 1.0 |
| **Package Still in the Van** | The recipient says "this is not our package." The company stops answering. The cargo is still aboard and grows more active on the way back. A whole story mission with almost no new content | 1.0 |

### 6.12 Return to Sender and Persistent Consequences

**Return to Sender.** A previous delivery appears again as an emergency recall:

```text
EMERGENCY RECALL
OBJECT 044
BLACKWOOD CLINIC

STATUS: Containment failure.
Retrieve immediately.
```

The player returns to a familiar map in a different state: no lights, broken doors, no staff, the anomaly loose, security hostile. This costs very little content and creates the feeling of a world that remembers.

**Persistent consequences.** Not every mission resets the world. A few simple flags, stored through Save and tested through Conditions:

- A delivered unstable object makes teleporter anomalies appear in that district on later contracts.
- Knocking out the power grid forces the district onto emergency power for the next two deliveries.
- Releasing a creature means it sometimes appears in that district again.

Use sparingly: a handful of flags produces the impression of a big system without an open world.

### 6.13 Difficulty Progression

1. Early: 0–1 package rules (the first delivery is a tutorial with none), simple locations, rare enemies.
2. Mid: 2 rules.
3. Late: 3 rules + dangerous district + optional objective + complication.

### 6.14 Worked Example — "Don't Let It Sleep"

Cargo: *Specimen 029*. Rules: *Keep moving. Do not expose to red light.*

The player arrives at the warehouse. The main passage is blocked.

- **Route A:** short, lit by red emergency lights — breaks a rule.
- **Route B:** long, patrolled by security drones.

Route B contains: a locked door needing a battery (carry object), a lever restoring power, a freight platform, a drone fight, and finally a cargo scanner plate in the delivery room.

About half of the library participates in one contract, yet everything feels natural.

---

## 7. Package System

The package is the heart of the game. It is a **physical world object**, not an inventory entry, and it is a second participant in the game.

- Small cargo: picked up and dropped (`PickupDrop`).
- Heavy cargo: carried with physics (`CarryObject`), slowing the player.
- Packages never enter the inventory. Inventory holds small items (keycards, fuses, batteries, medical supplies, quest items, ammo boxes).

### 7.1 Three Layers: Rule, Behavior, Escalation

Each anomalous package has three layers:

| Layer | Meaning |
|---|---|
| **Rule** | What the player is told |
| **Behavior** | What the object actually does |
| **Escalation** | What happens after mistakes |

Example:

```text
OBJECT 071
Instruction: DO NOT SHAKE.
Real behavior: reacts to sharp accelerations.

Violation 1: knocking is heard.
Violation 2: it starts moving inside the container.
Violation 3: it periodically shoves the player.
Violation 4: the container breaks.
```

So a rule stops being `if (dropped) penalty++` and starts producing situations. A package need not use everything: each package picks the layers it needs. Layers are per-package data composed of small components; there is no god class.

### 7.2 Package Rules

| Rule | Behavior | Milestone |
|---|---|---|
| **Do Not Drop** | A hard impact or drop is a violation | MVP |
| **Keep In Light** | Without light for 10 s the package becomes unstable | MVP |
| **Delivery Deadline** | Deliver before the countdown ends | MVP |
| **Keep In Darkness** | Long exposure to bright light wakes the cargo | Demo |
| **Do Not Stand Still** | Standing still for more than 5 s is a violation | Demo |
| **Never Look Away** | Looking away makes the package relocate | 1.0 |
| **Do Not Take Damage** | Player damage is a violation | Demo |
| **No Red Light** | Red emergency light zones are a violation | Demo |
| **No Metal Contact** | Contact with metal surfaces is a violation | 1.0 |
| **Never Open** | Hold-to-open attempts are a violation | 1.0 |
| **Avoid Strong Impacts** | Collisions, hard braking, or jumps in the van drop cargo stability; below a threshold it is a violation | Road tier |
| **Do Not Stop the Vehicle** | Example: "If knocking begins, do not stop the vehicle." Stopping is a violation | Road tier |

Each rule is a small independent component with three outcomes: `RuleSatisfied`, `RuleViolated`, and the package's `ViolationCount`. A package is never a single class containing every rule.

### 7.3 Violation Ladder (Escalation)

One violation does not end the contract.

| Violation | Default consequence |
|---|---|
| 1 | Package becomes unstable (audible and visible warning) |
| 2 | An enemy is spawned |
| 3 | Package becomes hazardous (damage over time nearby) |
| 4 | Contract fails |

Thresholds and consequences are per-contract data and may be replaced by a package-specific escalation (see 7.1).

### 7.4 Cargo Types

| Cargo | Rule | Behavior |
|---|---|---|
| **Fragile Specimen** | Do Not Drop | Hard hit cracks the container; damage over time nearby |
| **Photophobic Organism** | Keep In Darkness | Too long under bright light → awakens → spawns threat |
| **Light-Dependent Package** | Keep In Light | 10 s without light → package becomes unstable |
| **Restless Box** | Do Not Stop > 5 s | Player must keep moving |
| **Hunter Container** | Never Look Away | Violation relocates the package (Teleporter-like) |
| **Resonance Core** | Do Not Take Damage | Player damage raises its charge; full charge → explosion |
| **Living Cargo** | Stay Alive | Has health; enemies attack it; deliver it alive |
| **Contaminated Shipment** | — | Emits damage over time; requires matching equipment |

MVP ships Fragile Specimen, Light-Dependent Package, and a deadline shipment.

### 7.5 Incomplete Instructions and the Courier Archive

The player can never be fully sure the instructions are complete. The company writes "Do not expose to light" but not "if it happens, do not stand nearby." Or "Do not drop" — and the player slowly discovers that a fall from height does not break the cargo, it wakes what is inside. This adds a small layer of exploration.

After the first successful delivery of a cargo type, the **Archive Terminal** records it:

```text
Known behavior · Known violations · Observed effects · Recommended equipment
```

The archive has two voices:

```text
CORPORATE INSTRUCTIONS
Avoid direct sunlight.

COURIER NOTES
Blue emergency lights apparently count as sunlight. Don't ask me why.
```

This is where humor, atmosphere, gradual teaching, and collectible lore live. `Collectible` items (Courier Tags, Black Box Logs) are fragments of this information and unlock real entries that help in future contracts (see 13.2). Unlocked knowledge is stored by Save. It gives progression without an RPG layer. Tier: Demo.

### 7.6 Cargo and the Player

**Hands.** While carrying a large cargo the primary weapon cannot be used, or only the pistol can. This makes `CarryObject` a real mechanic: put the cargo down, deal with the threat, pick it up again. With a `Do Not Drop` rule the decision becomes interesting.

**Cargo changes the player** (Demo+):

| Cargo | Effect |
|---|---|
| **Heavy Mass** | Lower move speed and jump; higher knockback resistance |
| **Neural Cargo** | Applies *Disoriented* |
| **Gravity Object** | Periodically spawns a force zone |
| **Predator Sample** | Makes noise and raises enemy detection of the player |

**Recoil vs. cargo.** Firing a powerful weapon while holding a sensitive package sends a vibration/stability event to the package. A shotgun next to a fragile cargo is a bad idea.

### 7.7 Cargo and the World

- **Cargo sensors.** The package itself can detect nearby humans (stability falls if one approaches). Surveillance devices may detect the package and raise a security alert. A small stealth layer at almost no cost. Tier: 1.0.
- **Cargo Verification Station.** The scanner platform is more than a puzzle plate: the player sets the package down, the plate activates, then a scan runs. If the scan itself breaks a rule (a bright-light scanner and a `Keep In Darkness` cargo), the player must find an alternate verification method.
- **Cargo logistics.** A large cargo cannot go up stairs. The player puts the container on a freight platform, sends it up, finds another route, and collects it upstairs. One component becomes a small puzzle.

### 7.8 Multi-Package Contracts

Later, contracts may carry two cargos whose rules interact:

| Package A | Package B |
|---|---|
| Must stay in light | Breaks nearby lights |
| Must stay cold | Produces heat |
| Attracts enemies | Damaged by movement |

The player decides: carry one at a time? leave one behind? which route? A large amount of variety with no new levels. Tier: 1.0.

### 7.9 Cargo and the Van

Driving turns the package from a weight into a mechanic. Cargo has **Stability** (100 at the start), derived from the package's health pool:

| Stability | State |
|---|---|
| 80–100 | Perfect |
| 50–79 | Minor damage |
| 20–49 | Unstable |
| 0 | Containment failure |

Collisions, hard braking, jumps, and sharp turns reduce it, so the player regulates their own driving style. This is more interesting than a plain HP bar on a box.

**Cargo changes how the van drives:**

| Cargo | Effect on driving |
|---|---|
| **Fragile** | No hard braking |
| **Restless** | Cannot drive slowly for too long |
| **Magnetic** | At high speed it pulls the van toward metal objects |
| **Gravity Core** | Periodically raises vehicle mass or spawns a force zone |
| **Living Cargo** | When frightened it moves inside the van: a small steering disturbance (no real center-of-mass change) |
| **Electric Cargo** | Periodically switches the headlights off |

**Cargo is audible from the rear.** A cheap, strong horror tool:

```text
knock → knock… knock… → something begins to scratch the door
```

The player decides whether to stop and check or to keep driving. The instructions may say "If knocking begins, do not stop the vehicle." Tier: Road tier.

**Multi-package loading.** Late shifts carry packages A, B, and C to different addresses. The player picks the order (Depot → Clinic → Warehouse → Apartments, or another), and one package may carry a time limit. This is mini-logistics with real route planning. Tier: 1.0.

---

## 8. Locations and Roads

Micro-locations, not a city. Each is a 5–10 minute traversal reused by many contracts.

| Location | Contents | Milestone |
|---|---|---|
| **Greyline Warehouse** | Loading area, warehouse floor, storage, conveyor room, maintenance, office | MVP |
| **Blackwood Apartments** | Street, lobby, stairs, basement, 4–6 apartments, roof | Demo/1.0 |
| **Underground Station** | Platform, service tunnel, maintenance rooms, electrical room | 1.0 |
| **Research Clinic** | Reception, laboratory, containment, basement, storage | 1.0 |

### 8.1 Warehouse (MVP) Requirements

Must contain: Door, ConditionLock, Lever, PressurePlate, MovingPlatform, BreakableObject, Checkpoint, PickupDrop, CarryObject. JumpPad and Teleporter are optional later.

At least two routes to the delivery room:

| | Safer / longer | Dangerous / shorter |
|---|---|---|
| Path | Maintenance, freight lift | Loading floor, barricade |
| Obstacles | Battery + locked door, lever, moving platform | Electric floor (DamageZone), breakable barricade shortcut, explosive barrels |
| Enemies | Security drone patrol | Crawlers, drone |
| Reward | Standard | Bonus loot |

The delivery room contains a **cargo scanner**: the player places the package on a pressure plate, the scan runs, and the door unlocks.

### 8.2 Warehouse by State

| State | What changes |
|---|---|
| Normal | Standard routes |
| Blackout (MVP) | Part of the doors disabled, battery carried to the generator, light-dependent rules become dangerous |
| Lockdown | Drones, condition locks, alternative routes |
| Structural Damage | Breakables, collapsing catwalk, traversal route |
| Spatial Instability | Teleporters and wrong doors |

Spatial design note: simple, identical modular rooms help the Spatial Instability state — the player begins to doubt whether they came back to the same place.

### 8.3 Road Segments (Road Tier)

There is no city. The structure is:

```text
Depot → Road Segment → Location
```

A road segment lasts **2–4 minutes**. A handful of road scenes are reused: Industrial District, Old City, Highway, Forest Road, Underground Tunnel (5–6 at release). Each contract selects **Road + Road State + Cargo + Event** by the same scheme used for locations (6.5). Road states mirror location states (clear, fog, blackout, anomaly activity).

### 8.4 Road Pacing and Incidents

The road is never just waiting. Every 20–40 seconds the player gets one small task, one at a time:

```text
Obstacle · Hazard · Anomaly · Cargo problem · Vehicle problem · Pursuer · Route decision
```

### 8.5 Route Planning and Junctions

Before departure a map shows the options:

```text
Route A   6 min   Low risk
Route B   4 min   High anomaly activity
Route C   5 min   Security checkpoint
```

`Conditions` gate routes: a Security Pass opens Route C; a courier rank unlocks a shortcut.

On the road there are a few **authored forks** (not procedural roads):

```text
LEFT    Safe             +2 min
RIGHT   Hazard zone      −1 min
```

For a timed contract the right turn is tempting; for fragile cargo the left turn is better.

### 8.6 Road Hazards

Existing mechanics apply naturally:

| Hazard | Mechanic |
|---|---|
| Toxic fog, electric road, fire | `DamageZone` |
| Barrier, wooden gate, road debris | `BreakableObject` |
| Fuel tanks | `ExplosiveBarrel` |
| Anomaly wind, gravity field | `ForceZone` (pending) |
| Spatial road anomaly | `Teleporter` |
| Vehicle ferry, industrial lift | `MovingPlatform` (the van drives on; carrying a van-sized rigidbody must be checked in the audit) |

### 8.7 Night Road Events

| Event | Composition |
|---|---|
| **Abandoned Package** | Cargo on the roadside (`Collectible` / `PickupDrop`). Taking it gives an optional reward, but it may carry unknown rules |
| **Road Drone** | `Patrol`, `DetectionSensor`, `ChaseTargetMemory` guarding a checkpoint. Slip past, show authorization, or get out and disable it |
| **Fallen Cargo** | The road is blocked. Push it (`PushPullObject`) or break it (`BreakableObject`) |
| **Spatial Loop** | A tunnel returns the player to its start (`Teleporter`). The player must notice the rule to leave |
| **Gravity Storm** | `ForceZone` (pending) pushes the van sideways |
| **Broken Bridge** | Detour, or a special industrial `MovingPlatform` |

### 8.8 Stopping and Repairs on the Road

On some roads the player can **pull over**, exit the van, and open the cargo bay to fix a problem:

```text
Cargo restraint broke → HoldInteraction → repair
Object overheating → install coolant
Engine overheating → stop, open the hood, HoldInteraction → repair
```

Stopping may itself be dangerous: a "Do Not Stop" rule, or creatures patrolling nearby. This turns repair into a tension mechanic. Breakdowns are rare and never nagging. Until `HoldInteraction` is installed, repairs are instant interactions.

### 8.9 Driving

Driving is **arcade**, not a simulator. The van should accelerate pleasantly, brake clearly, slide a little, and make collisions felt. Out of scope: gearbox, engine RPM, differentials, suspension setup, tire temperature, manual gears. The game is about delivery, not about vehicle physics.

- The camera is first-person from the driver's seat.
- Input uses a separate vehicle action context (9.2); the Input module switches contexts when the player enters or exits the van.
- Collisions use the three hit zones (5.6).
- No template owns driving; see section 23 for the decision.

### 8.10 State Carry-Over Between Road and Foot

The road, the delivery, and the return are one system:

- Arrived with broken headlights, or left a portable battery pack connected to the van: no Portable Light for the foot part.
- Used the medkit on the road: it is not available inside the building.
- Found a Fuel Cell inside: it can be used on the van.
- Left a security drone alive: it appears on the road during the return trip.
- The contract damaged the van's electronics: the way back is without lights.

This is carried by a runtime shift state (game-side), saved at the van between deliveries.

### 8.11 Return Trip

Most contracts end on the result screen followed by a scene transition to the Depot, to keep the pace. Special missions (**Return to Depot Immediately**, **Package Still in the Van**) require actually driving back, especially after a delivery that went wrong.

---

## 9. Player

First-person, single-player. Only game-specific glue is written; all behavior comes from templates.

### 9.1 Capabilities

Move, look, interact, sprint, carry, shoot, melee, switch items, use tools, take damage, die, respawn. Road tier: enter and exit the van, drive, pull over.

### 9.2 Controls (Input Actions)

| Action | Keyboard / Mouse | Gamepad |
|---|---|---|
| Move | WASD | Left stick |
| Look | Mouse | Right stick |
| Interact / hold interact | E | South button |
| Primary (fire/attack) | Left mouse | Right trigger |
| Secondary (aim/tool/brace) | Right mouse | Left trigger |
| Reload | R | West button |
| Drop | G | East button |
| Sprint | Left Shift | Left stick click |
| Carried item 1 / 2 | 1 / 2 | D-pad left / right |
| Use utility 1 / 2 | Q / C | D-pad up / North button |
| Flashlight | F | D-pad down |
| Pause | Esc | Start |

Gameplay code reads Input Actions only. No direct device polling in production code.

**Vehicle context** (road tier; a separate action context switched in when the player is in the driver's seat):

| Action | Keyboard / Mouse | Gamepad |
|---|---|---|
| Accelerate / brake-reverse | W / S | Right / left trigger |
| Steer | A / D | Left stick |
| Handbrake | Space | South button |
| Headlights | F | D-pad down |
| Look | Mouse | Right stick |
| Exit van | E | West button |
| Pause | Esc | Start |

### 9.3 Health, Death, and Recovery

Player uses `Health`. Death is part of the fiction, not a "You died" screen:

```text
Courier recovery team retrieved you.
```

After death the player respawns at the last Safe Locker (13.2) with consequences layered on top of the standard respawn: contract payment penalty (default −15%), dropped optional loot lost, cargo state possibly worsened. The campaign is never reset.

### 9.4 Stats

A small set of player stats, not an RPG sheet:

```text
MaxHealth · MoveSpeed · CarryMoveSpeed · WeaponDamage · ReloadSpeed · ScannerRange
```

### 9.5 Stamina

Sprinting and carrying heavy cargo consume stamina; it regenerates after a delay.

### 9.6 Runtime Values

- **Player health** — Health pool.
- **Package stability** — a health pool on the package.
- **Suit protection** — a stat plus a damage rule on the player's health pool.
- **Vehicle condition** — a health pool on the van (hit zones route damage to it).
- **Cargo stability** — the package's health pool, shown in bands (7.9).

### 9.7 Hands

Carrying a large cargo restricts weapons (7.6). The restriction is enforced by game-specific glue between `CarryObject` and the weapon input, not by modifying either mechanic.

---

## 10. Interaction

Interaction is simple and consistent:

- **Press Interact** — toggle door, flip lever, pick up item, use terminal.
- **Hold Interact** — timed actions (see 13.4: HoldInteraction is pending). Planned uses: open a heavy hatch, connect a battery, scan a package, repair a terminal, stabilize a package, hack a terminal, force a door, neutralize an anomaly, secure cargo in the van, repair the van, install a van module, complete a delivery.

**Design rule:** while holding, the player is vulnerable. "Hold E — 6 seconds" next to a patrolling enemy creates tension instantly.

Until HoldInteraction is installed, these actions are designed as instant interactions through existing mechanics. They are not given a local hold-timer implementation.

Interactables are readable at a glance (see section 17).

---

## 11. Equipment and Combat

Combat is a survival tool, and it is deliberately **expensive** (see 14.5), so that the environment — doors, levers, plates, barrels, teleporters, damage zones — becomes a way to solve situations.

### 11.1 Loadout Slots

| Slot | Options |
|---|---|
| **Primary** | Stun Pistol, Shotgun, Repulsor |
| **Tool** | Scanner, Crowbar, Portable Light |
| **Utility** (2 slots) | Containment Grenade, Medkit, Battery, Repair Kit |
| **Suit** | Armor variants |

Two carried items are chosen from Primary and Tool (any combination). Weapon switching is therefore part of preparation, not a permanent three-gun kit. The van has its own slots (5.2): a cargo module, two vehicle modules (for example Cargo Stabilizer, Backup Generator, Auxiliary Battery), and an emergency slot.

### 11.2 Weapons and Tools

| Item | Role | Milestone |
|---|---|---|
| **Stun Pistol** | Reliable hitscan sidearm with reload, recoil, spread | MVP |
| **Crowbar** | Melee with knockback; also breaks jammed passages and breakables (2–3 hits) | MVP |
| **Medkit** | Heals | MVP |
| **Shotgun** | Pellet weapon for close range. Fast, loud, expensive, and damaging to property | Demo |
| **Portable Light** | Lights up a cargo-light requirement | Demo |
| **Repulsor** | Charged non-lethal shot that pushes enemies and physics objects | Demo |
| **Containment Grenade** | Grenade whose effect is a short control zone (an EMP variant disables electronics) | 1.0 |
| **Scanner** | Reveals targets and cargo information | 1.0 |

Candidates that replace rather than add (the 5–6 item cap stands): *Tracking Beacon* (homing projectile attached to a creature or cargo), *Disruptor* (hitscan with a status effect that temporarily disables a robot).

Every hit has feedback: reaction flash, audio, and a short hit stop for melee.

### 11.3 Non-Lethal Identity

The arsenal favors control over killing, which gives the game its own identity while keeping the combat mechanics:

- **Repulsor** — charged projectile with knockback; little damage, large displacement.
- **Tracking Beacon** — homing projectile marking a target.
- **Disruptor** — status effect that shuts a robot down for a while.
- **Containment Grenade** — a short control zone instead of a classic explosion.

### 11.4 Weapon Systems Interacting with the World

These are game-specific orchestration over existing events, not extensions of the reusable mechanics:

- **Recoil vs. cargo:** powerful weapons send an event to a sensitive package (7.6).
- **Spread as weapon state:** a damaged weapon, or the player's *Disoriented* status, applies a temporary spread modifier.
- **Melee utility:** the crowbar breaks jammed wooden passages, not only enemies.
- **Equipment-driven switching:** only equipped items can be switched to.

### 11.5 Environmental Weapons

- **Explosive barrels** (gas cylinders, fuel barrels, power cells) — damage nearby enemies; chain reactions. Variants: an *EMP container* uses the explosion radius as an electronics effect; a *pressure tank* throws objects, can break barricades, activate physics objects, and knock enemies down.
- **Breakable objects** — barricades, crates, glass, weak doors. A shortcut through a wall is faster than finding the keycard, but it costs property damage and may attract an enemy: a low-cost moral/economic choice.

---

## 12. Enemies and AI

Very few archetypes, for scope control. Game code composes template AI mechanics.

| Enemy | Behavior | Composition |
|---|---|---|
| **Security Drone** | Patrols, spots player, chases, shoots | Patrol, DetectionSensor, ChaseTargetMemory, Projectile or hitscan, Health, HitFeedback |
| **Watcher** | Stationary or slow patrol sentry with a beam | Patrol, DetectionSensor, hitscan |
| **Crawler** | Fast melee pursuer | DetectionSensor, ChaseTargetMemory, MeleeAttack, Knockback, Health, HitFeedback |
| **Wisp** | Anomaly that launches homing orbs | Spawner, HomingProjectile |

MVP: Security Drone and Crawler. Release 1.0 adds Watcher, Wisp, and one more archetype decided after the Demo.

### 12.1 AI Rules

- **Patrol** — simple A→B→C→A routes.
- **Detection** — field of view, distance, line of sight.
- **Chase memory** — the enemy remembers the last known position, searches briefly, then returns. This makes simple AI more convincing.
- **Spawner** — enemy emergence, package-violation spawns, alarm events.
- **Wave Spawner** — only for special events, such as an *Emergency Lockdown* contract (wave 1: 2 drones; wave 2: 3 crawlers; wave 3: drone + wisp).

Existing AI mechanics own their own state logic; the game does not rewrite them to force a different state-machine usage.

### 12.2 Beyond Enemies

`Patrol` and `DetectionSensor` are not only for enemies:

- **Patrol:** security scanner, robot cleaner, moving spotlight, inspection drone. The player carries a forbidden package and slips between routes. No stealth framework is built.
- **DetectionSensor:** on the package (nearby humans), or on a surveillance camera (package seen → security alert).
- **Sound lures:** a thrown object (pending ThrowableObject) can distract AI. If `DetectionSensor` does not support sound stimuli at that point, do not extend it in advance; add the capability only when such a contract is actually being built, through the proven-need route (section 1, principle 5).

---

## 13. World, Hazards, and Level Design

### 13.1 Doors and Locks

Doors are the backbone of level design: normal, automatic, security, containment. Locks sit on top of doors and use conditions such as keycards, power, quest state, and package authorization.

Examples:

- Door requires `SecurityLevel >= 2` **and** `PowerEnabled`.
- Delivery point requires `CorrectPackage` **and** `PackageNotDestroyed`.
- Bonus requires `PackageViolationCount == 0`.

### 13.2 Industrial Elements

| Element | Use |
|---|---|
| **Lever** | Power, freight lift, emergency door, machine |
| **Pressure plate** | Cargo verification station, weight puzzles |
| **Moving platform** | Traversal and cargo logistics (freight elevators, cargo platforms) |
| **Checkpoint** | A physical **Courier Emergency Locker** (also the van and the delivery terminal). Activating it gives the checkpoint, a small ammo refill, and contract sync — once per locker |
| **Breakable object** | Barricade, wall, glass; a faster route that costs property damage |
| **Jump pad** | Industrial pneumatic lift or anomalous pad. 1–2 special locations only |
| **Teleporter** | "Impossible architecture," not a portal gun: door 204 leads to the same corridor, walk back and it is another floor |
| **Collectible** | Lost Parcels, Employee Badges, Black Box Logs, Courier Tags. Around 20 hidden items in the full game; Courier Tags unlock archive entries (7.5) |

### 13.3 Hazards

| Hazard | Source |
|---|---|
| Electric floor, fire, toxic puddle, laser, steam | Damage zones |
| Chemical exposure, burn, radiation | Damage over time |
| Poisoned, Electrified, Slowed, Disoriented, Radiated | Status effects |

### 13.4 Pending Mechanics (Not Yet Installed)

Ten mechanics will be added to the library later. Their roles are reserved; **until they are installed, content is designed to work without them and no local clones are created.**

Priority 1 (needed for the delivery fantasy):

| Mechanic | Planned use in Night Courier |
|---|---|
| **HoldInteraction** | Highest priority. Repair fuse, scan cargo, force door, stabilize package, install battery, hack terminal, revive a containment system, neutralize an anomaly, complete a delivery; secure cargo in the van, repair the van (in the garage, before a trip, and on the road), install van modules. The player stays vulnerable while holding |
| **ConveyorBelt** | Depot package conveyor and cargo platform feeding the van loading area; warehouse conveyor room. A later puzzle contract: the sorting system is broken, route a dangerous object by hand using levers and a plate that detects the cargo |
| **ThrowableObject** | Decoys: a can, tool, or small crate lures AI by sound. Crates onto pressure plates. Packages are never thrown |
| **ForceZone** | Anomalies: gravity corridor, ventilation shaft, unstable cargo, containment field; on the road, anomaly wind and gravity storms that push the van. A cargo that tries to tear itself from the player's hands. With ThrowableObject and PressurePlate, a puzzle |
| **FallingPlatform** | Emergency environment, not a platformer element: an old metal catwalk that holds a few seconds, then breaks. Dangerous shortcut route |

Priority 2:

| Mechanic | Planned use in Night Courier |
|---|---|
| **RotatingPlatform** | Mechanical equipment: turntable, cargo crane, rotating maintenance bridge; directing large cargo between warehouse sections |
| **DisappearingPlatform** | Flickering walkways in unstable anomaly zones |
| **PhysicsButton** | A large emergency button pressed by hand, by a crate, or by a thrown object — a more systemic interaction than a plain Interact |
| **Pendulum** | A crane hazard: hanging container, industrial hook, broken crane. Wait, stop the crane, or use it against enemies |
| **RopeSwing** | Lowest priority. A rare emergency: one contract with a collapsed bridge and a hanging service cable |

`PushPullObject` is a different case: it is already in the library (not yet linked into this project) and is used for crates and large cargo (5.4, 8.7).

When a pending mechanic arrives: reinstall via *Tools ▸ Unity Templates ▸ Install All Local Packages*, read its README, update [Template-Mapping.md](Template-Mapping.md), then wire it where listed above.

### 13.5 Traversal Restraint

Traversal mechanics are not used constantly. Ladders, mantling, ziplines, grapples, wall jumps, and dashes in every level would turn Night Courier into an action platformer. The library already contains `Ladder`, `LedgeGrabMantle`, `Zipline`, `GrapplingHook`, `WallJump`, `Dash`, and `ResponsiveJump` (not yet linked into this project). They are properties of specific districts or equipment, if used at all: a maintenance district with ladders and mantling, a construction site with a zipline, an experimental facility with a grappling device and force zones. `WallJump` and `Dash` do not fit the character fantasy and are not forced in. Not every mechanic is required to be used just because it exists. See [Template-Mapping.md](Template-Mapping.md) §3.7.

---

## 14. Progression and Economy

### 14.1 Courier Rank

```text
Trainee → Courier → Senior Courier → Hazard Courier → Restricted Courier → Class-X Courier
```

Rank unlocks new districts, package types, equipment, and harder contracts. Rank advances from shift ratings (14.3), not from raw XP.

### 14.2 Credits (₡)

Credits are the only currency. Payment comes from contract rewards; bonuses and deductions come from the result breakdown.

### 14.3 Shift Rating

The company grades each shift on four axes. These are results of the shift, **not** RPG stats:

```text
Delivery time          A
Cargo condition        S
Protocol compliance    C
Property damage        F

SHIFT RATING: B
```

The overall rating feeds `Progression` and unlocks. It replaces a bare "+250 XP."

### 14.4 Property Damage

The company fines destruction:

| Event | Fine (example) |
|---|---|
| Broke a door | −₡20 |
| Exploded a barrel | −₡80 |
| Smashed lab equipment | −₡150 |

The player no longer wants to shoot everything. A Crawler is in the way:

- **A:** shotgun — fast, but expensive in property damage.
- **B:** lure it into a damage zone.
- **C:** close a door on it.
- **D:** go around.

This is where the library produces emergent gameplay rather than a component demo.

### 14.5 Ammo Is an Expense

Ammunition is not infinite. It costs money, deducted on the result screen:

```text
Shotgun shell: ₡8      Contract reward: ₡300
```

Fire 25 shells and a large share of the profit is gone. This gives doors, levers, plates, barrels, teleporters, and damage zones meaning as tools. It is a balancing lever, not a survival-horror scarcity mechanic.

### 14.6 Reputation Flags

Company-vs-contact decisions (6.10) set a few reputation flags that unlock or lock contracts and feed achievements.

### 14.7 Upgrades

Small, readable upgrades; no big skill tree.

MVP:

- +Max Health
- +Carry Move Speed
- +Scanner Range or a weapon stat

Release 1.0 target: 15–20 upgrades in total, courier and van combined.

### 14.8 Van Upgrades

At most **3 levels** per line. Each affects play, so `Progression` opens real new capabilities rather than only numbers.

| Upgrade line | Effect | Tier |
|---|---|---|
| **Suspension / Cargo Stabilizer** | Impacts do less cargo damage | Demo |
| **Headlights** | Range up, reliability up | Demo |
| **Engine** | Acceleration up, top speed up | Demo |
| **Tires** | Grip up | 1.0 |
| **Armor** | Vehicle damage down | 1.0 |
| **Cargo Containment** | Anomaly effects down | 1.0 |
| **Auxiliary Battery** | Electrical failures are easier to survive | 1.0 |

Stats modifiers drive these effects; Wallet pays for them; Progression gates them; Save stores them.

### 14.9 Repairs and Expenses

After the delivery result comes the **Van Inspection**:

```text
Earned                 +420
Ammo                    -32
Cargo damage penalty    -50
Vehicle repair          -75
----------------------------
Profit                 +263
```

Money matters. Should the van be fully repaired, or should the player save for an upgrade? The van is never repaired automatically (5.7).

### 14.10 Shift Economy

With several contracts per shift (4.4), the van's capacity is a constraint: the player chooses which contracts to take, which to skip, and in what order to deliver. Wallet, Rewards, Progression, and Equipment form one economy.

---

## 15. Persistence

Saved:

```text
Courier rank · Credits · Unlocked equipment · Purchased upgrades · Completed contracts
Collectible progress · Archive knowledge · World/reputation flags
Van condition · Fuel · Installed van modules
Achievement progress · Settings · Campaign progress
```

Not saved: active physics state of a level. A mid-level death is handled by checkpoint and respawn, not by a save-state snapshot. A multi-delivery shift in progress is saved only at the van between deliveries (contracts, loaded cargo, van state), never mid-location.

---

## 16. UI and Screens

| Screen | Purpose |
|---|---|
| Main Menu | New Game, Continue, Settings, Quit |
| Depot HUD | Credits, rank, interaction prompts |
| Contract Selection | Available contracts |
| Archive | Corporate instructions and Courier Notes (Demo+) |
| Preparation | Briefing and loadout for the personal and van slots (5.2) |
| Equipment | Loadout |
| Garage | Van condition, repairs, modules, refuel/recharge (road tier) |
| Route Map | Route options, risk, duration (road tier) |
| Vehicle HUD | Speed, vehicle condition, fuel, cargo stability, headlights indicator, warning lights. Built as game UI, not as an asset |
| Upgrades | Purchases |
| Results | Reward breakdown and shift rating |
| Van Inspection | Itemized profit and repair choices (14.9) |
| Settings | Options |
| Pause | Resume, Settings, Controls, Return to Depot / Main Menu |

Windows (popups): Contract Details, Delivery Result, Equipment, Upgrades, Confirmation.

Terminal UIs animate with the project tween system.

### 16.1 Settings

Master / Music / SFX volume, Mouse Sensitivity, FOV, Graphics Quality, Resolution, Fullscreen, VSync, Language.

### 16.2 Tutorial as Corporate Training

No popup tutorials ("Press E to interact"). The company trains the courier, which doubles as lore and humor:

```text
MANDATORY TRAINING VIDEO 4-B
In the unlikely event that cargo begins whispering your legal name,
continue delivery as normal.
```

The first delivery teaches by play, without constant popups:

```text
Pick up parcel → deliver to locker → door is locked → find keycard → enemy appears → return
```

Each mechanic is introduced through play.

---

## 17. Art Direction

**Low-poly industrial horror.** Not extreme retro pixelation. Simple geometry, simple materials, strong lighting, fog, contrasting emergency lights.

### 17.1 Light as Gameplay Language

| Light / color | Meaning |
|---|---|
| White | Normal |
| Yellow | Equipment and interactable objects |
| Red | Emergency |
| Blue | Anomaly containment |
| Green | Destination |

Some contracts redefine the meaning of a light: *Object 41 becomes aggressive specifically under red emergency lighting.* Lighting is then part of the mechanics, not just decoration.

Use simple primitives until final assets exist. Development must never wait on art. Interactable objects must be immediately readable. The full placeholder scheme, the future asset list, and the prefab rules for painless replacement are in [Asset-Plan.md](Asset-Plan.md).

---

## 18. Audio

Budget horror lives on sound, and some audio is part of the rules:

```text
Ambient hum · Fluorescent lights · Distant sounds · Package sounds · Enemy cues
Door sounds · Radio · Footsteps · Weapon sounds
```

Audio as rules:

- A ticking that accelerates means stability is dropping.
- A whisper to the left means the object moved.
- A Geiger-style detector means a hazard zone is near.
- Knocking, then scratching from the cargo bay while driving is a warning and sometimes a rule (7.9).

The van needs its own sound set: engine idle/acceleration/deceleration, starter, brakes, tire rolling on asphalt and gravel, collisions, suspension impacts, doors, cargo door, horn. Music is minimal: a Depot ambient theme, a driving ambient layer, a danger layer, and rare anomaly music; environment sound carries most of the time.

For a low-budget game, sound is cheaper than complex graphics.

---

## 19. Narrative

No large script. Story arrives through contracts, package descriptions, radio, terminal messages, corporate instructions, Courier Notes, and employee logs.

The company becomes gradually suspicious. Contracts get stranger, more dangerous, and less ethical, while the company interface still prints:

```text
EXPECTED RISK: LOW
```

after half a laboratory has disappeared.

The player slowly notices that the company knows far more than it says. Final twist (working): the deliveries are not transporting anomalies — they are spreading them across the city.

---

## 20. Localization and Text Policy

- **All text is written in English.** No Cyrillic or other languages in code, assets, UI, documents, or commit messages.
- All player-facing text uses localization keys, never hardcoded strings.
- English is the source language. Additional languages (Ukrainian is the first candidate) are optional and added only through the localization module after the content is stable.
- Almost the entire narrative lives in interface text and documents (contract descriptions, package instructions, terminal messages, archive entries, subtitles), so localization carries a large share of the game. Keys follow a stable naming scheme.

---

## 21. Analytics and Achievements

### 21.1 Analytics

Collected only with consent and within the project's privacy policy. Disabled by default.

The goal is balancing contracts, not just counting launches. Most valuable data:

```text
Where the contract was abandoned · Which rule is violated most · Which route is chosen
Which equipment is taken · How much ammo is spent · Where players die · How long a contract takes
```

Plus the basics: contract accepted, completed, failed; package violation; player death; weapon used; difficulty.

### 21.2 Achievements

Achievements reinforce the game's style rather than counting kills.

| Name | Condition |
|---|---|
| FIRST NIGHT | Complete the first shift |
| PERFECT DELIVERY | Complete a contract with zero violations |
| SPECIAL HANDLING | Complete 10 anomaly deliveries |
| NO QUESTIONS ASKED | Deliver a restricted package |
| FRAGILE | Complete a fragile delivery without dropping it |
| OVERTIME | Complete an entire night shift |
| FRAGILE MEANS FRAGILE | Finish without placing cargo on the ground |
| NOT MY PROBLEM | Complete a contract without firing a weapon |
| EXPENSE REPORT | Cause more property damage than the contract pays |
| WRONG FLOOR | Use the same anomalous doorway five times |
| PROTOCOL IS OPTIONAL | Finish with 3 violations |
| PROFESSIONAL | Five perfect deliveries in a row |

Release target: 15 achievements.

---

## 22. Systemic Design Principles

**The question before adding any new content:** can this situation be produced by combining mechanics that already exist? The game is built on the number of interesting combinations, not on the amount of content.

Examples:

```text
CarryObject + Keep Moving rule + PressurePlate + Patrol
→ Put the box on the scanner; stop moving and it gets worse; a robot patrols nearby.

Keep In Light + Blackout + Carry battery + MovingPlatform
→ A different game with no new system.

Fragile + Recoil + Crawler + Breakable shortcut
→ Shoot and risk the cargo, or break the way through and run.

Fragile cargo + Do Not Stop + knocking from the cargo bay + a gravity storm
→ Keep the van steady while something scratches behind you and the road pulls sideways.

Keep In Light + broken headlights + a backup generator that costs a van slot
→ The preparation decision decides the road and the delivery.
```

Rules of thumb:

- A single contract uses 5–10 mechanics.
- Prefer a new combination, state, or rule over a new system.
- Do not extend a reusable mechanic to serve one contract; layer game-specific reactions on its events.
- A mechanic that does not fit the fantasy stays unused.

---

## 23. Technical Design Summary

Detailed mapping is in [Template-Mapping.md](Template-Mapping.md). Short version:

- **Composition, not singletons.** No GameManager, no global service locator. Application services are built by the Startup pipeline and passed explicitly to scene entry points.
- **Data vs. runtime state.** Stable definitions (contracts, packages, rules, weapons) are data. Runtime state is separate. No giant universal ScriptableObject; no MonoBehaviour per contract.
- **Events sparingly.** A few typed facts (e.g., ContractCompleted, PackageViolation, PlayerRespawned). Commands and queries stay direct calls.
- **State machines where they fit.** Contract run state; AI keeps the template's own logic.
- **Timers** for deadlines, rule grace periods, hazard durations, AI search time.
- **Tween** for doors, terminal UI, lights, and small feedback. No hand-written lerp animations per object.
- **Persistent world flags** through Save and Conditions; no world simulation.
- **Placeholder-first visuals.** Every gameplay prefab keeps logic, colliders, and anchors on a stable root and art under a replaceable `Visual` child (see [Asset-Plan.md](Asset-Plan.md)).
- **Van as two halves of the character.** The van uses the same modules as the courier: `Health` (condition, cargo stability), `Stats` (acceleration, top speed, grip, headlight range, cargo protection), `Equipment` (van slots), `Inventory` (parts), `Wallet` (repairs, upgrades, possibly fuel), `Save` (persistent condition), `Input` (vehicle context).
- **Driving has no template owner.** The library contains no vehicle controller. Options: (a) a minimal arcade controller in `Assets/Game/Vehicle` as game glue, or (b) a new reusable mechanic added to `UnityTemplates` (which fits the project's purpose). This is an open decision for the user; it is not made by an agent on its own. Either way the controller must be small and arcade (8.9), and the vehicle's health, stats, equipment, and input come from the existing modules.
- **Fuel representation** is decided in the audit (candidates: a `Wallet` currency committed once per road segment, a `Stats` value, or the `Stamina` mechanic if its API fits).

---

## 24. Risks and Non-Goals

| Risk | Mitigation |
|---|---|
| Template APIs do not fit a needed behavior | Prefer composition; fix the template minimally only after proving the need |
| Scope creep from "use every mechanic everywhere" or from the system list in this document | Tiers (3.2); each contract uses 5–10 mechanics; the full game uses the library |
| Package rules becoming a god class | One small component per rule; per-package layer data |
| Art blocks progress | Primitives and strong lighting first |
| Pending mechanics delay critical content | Design content to work without them; do not clone them locally |
| Game starts to feel like walking with a box | Cargo is a participant (7.1); states, complications, and ways-through (6.5–6.8) |
| Random complications destroy planning | At most one scripted major complication per short contract |
| The van and the road multiply the project size | Road tier only after the MVP loop is fun; one van, 5–6 short segments, arcade handling, hard limits in section 3 |
| No template owns vehicle driving | Open decision (section 23): game-side minimal controller or a new `UnityTemplates` mechanic |
| Arcade driving is not fun enough | Test it alone on a gray-box road before building loading, garage, or events on top of it |
| Preparation becomes a tedious checklist | Automate normal operations after the first contracts; show inspection only when something is wrong |

Non-goals: open world, traffic AI, pedestrians, multiple vehicles, cosmetic vehicle customization, realistic car simulation, day/night cycle simulation, police system, fuel stations around the city, a large garage system, multiplayer, procedural levels, large dialogue, deep RPG stats, mobile-specific work (for now), Haptics on this platform.
