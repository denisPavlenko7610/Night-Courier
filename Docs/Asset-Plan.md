# Asset Plan

Placeholder-first art plan for Night Courier. **Right now every asset is a Unity primitive. Later, only free assets are used; nothing is purchased.** (Cube, Sphere, Capsule, Cylinder, Plane, Quad). This document lists what final assets will be needed, in what order, and how prefabs are built so that swapping art never touches gameplay code.

Design lives in [GDD.md](GDD.md); template ownership lives in [Template-Mapping.md](Template-Mapping.md); phases live in [Production-Plan.md](Production-Plan.md).

---

## 1. Principles

1. **The whole vertical slice is built on primitives.** The first job is to prove that choosing a contract → preparing → loading the van → driving → solving road problems → delivering → servicing the van → counting profit is actually interesting. Art cannot prove that.
2. **Do not import final assets now, and never buy any.** Sourcing starts only after the loop is fun (MVP, then Road Alpha), and only from free assets with a license that allows commercial use on Steam (section 6.4). The packs to look for are in section 6.
3. **Replace only the visual layer.** Gameplay components, colliders, interaction logic, and anchors live on stable roots. Art lives under a replaceable `Visual` child.
4. **Few sources, one style.** Use 3–5 large compatible free packs, ideally from the same creator family, not 15 unrelated ones. Free packs vary widely in style, so unify them with a shared material set, one palette, fog, and lighting. Consistent lighting and materials make simple models look intentional.
5. **Lighting and materials do more than models.** Good materials, fog, contrast lights, and emissive panels give more quality than expensive meshes.
6. **No asset may force a gameplay change.** If an asset does not fit the gameplay prefab, fix the asset, not the code.

### 1.1 Visual Direction

**Stylized low-poly industrial night.** Not literally giant facets; rather:

```text
simple geometry · simple PBR materials · lots of metal and concrete · night · fog
contrasting light sources · emissive panels · yellow-black industrial hazard markings
```

The light language (white / yellow / red / blue / green) is defined in [GDD.md §17.1](GDD.md) and applies to placeholders too.

---

## 2. Prefab Rule for Replaceable Art

> **All gameplay prefabs must support placeholder visuals and later art replacement without changing gameplay code.**
> Keep gameplay components, colliders, interaction logic, and important anchors on stable root objects.
> Put replaceable art under a dedicated `Visual` child.

Runtime code must never depend on:

```text
mesh names · renderer hierarchy · material names · bone names · asset-specific child names
```

unless the dependency genuinely belongs to animation or visual presentation.

### 2.1 Standard Anchors

Important positions are explicit anchor objects on the stable root:

| Anchor | Used by |
|---|---|
| `Muzzle` | Weapons, drone weapons |
| `InteractionPoint` | Anything the player interacts with |
| `CargoPoint` | Hands, van cargo slots, platforms |
| `DriverSeat` | Van |
| `CameraPoint` | Player camera, vehicle first-person camera |
| `WheelPoints` | Van wheels (visual wheels follow these) |
| `EffectPoint` | VFX and audio emitters |
| `TargetPoint` | Enemies, packages, anything aimed at or tracked |
| `IndicatorPoint` | Cargo stability and rule indicators |

### 2.2 Example Hierarchies

**Van**

```text
Van
├── VehicleController
├── Rigidbody
├── Colliders
├── Interaction
├── Anchors
│   ├── DriverSeat
│   ├── CameraPoint
│   ├── WheelPoints
│   ├── CargoSlot01
│   ├── CargoSlot02
│   ├── CargoSlot03
│   └── CargoSlot04
└── Visual
    └── PH_Van            ← later replaced by the final van model
```

**Security Drone**

```text
SecurityDrone
├── Health (HealthCarrier)
├── DetectionSensor
├── Patrol
├── Collider
├── Anchors
│   ├── TargetPoint
│   └── Muzzle
└── Visual
    └── PH_SecurityDrone
```

**Cargo**

```text
Cargo
├── Package
├── Rigidbody
├── CarryObject
├── Collider
├── Anchors
│   └── IndicatorPoint
└── Visual
    └── PH_CargoBox
```

### 2.3 Requirements from the Templates

Final assets must satisfy what the template mechanics expect. Check each against the mechanic's README before replacing.

| Mechanic | Requirement on the visual |
|---|---|
| `Door` | A separate moving `panel` transform standing in its closed pose; motion is a local-space offset (a sliding door). Hinged doors cannot be driven by `Door` as it stands and would be decorative, or need a template change (audit) |
| `Lever` | A separate handle object that tweens |
| `MovingPlatform` | A platform body separate from the static frame; correct pivot |
| `PressurePlate` | A trigger volume; the plate visual is separate |
| `BreakableObject` | An `Intact Visual` child plus an inactive `Broken Visual` child containing fragment rigidbodies. The art needs both states |
| `ExplosiveBarrel` | Root with a `HealthCarrier`; visual separate |
| `HitReaction` (`HitFeedback`) | Renderers must use a shader that exposes `_BaseColor` (or legacy `_Color`); the hit flash writes a `MaterialPropertyBlock`. Custom shaders (including the anomaly shader) must keep this property |
| `Checkpoint` | Trigger zone plus the locker visual |
| `Teleporter` | A destination transform |
| Cargo points | Anchors, not mesh positions |

Moving parts are separate objects with correct pivots. Pivots matter more than ready-made animation clips: the mechanics move parts through Tween and physics.

---

## 3. Placeholder Standard

- Use only Unity primitives and a small shared set of flat placeholder materials.
- Every placeholder object and prefab is named with the prefix **`PH_`** so that remaining placeholders can be found with a project search.
- Suggested folder: `Assets/Game/Presentation/Placeholders/` (materials, small prefabs).
- Placeholder colors follow the **light language** for world/interaction cues and the **cargo hazard classes** below for cargo bodies.
- An interactable's highlight is a separate emissive accent or outline, never the base color of the object, so a yellow crate is not mistaken for an interactable.
- Use the target lighting (night, fog, contrast lights, emissive panels) on the gray-box scenes from the start. Readable lighting is part of the placeholder.

### 3.1 Cargo Hazard Classes (Placeholder Colors)

| Color | Class |
|---|---|
| White | Ordinary |
| Yellow | Fragile |
| Blue | Electrical |
| Green | Biological |
| Red | Dangerous |
| Purple | Anomalous |

These colors are a debugging and classification aid. The in-game light language (GDD §17.1) is a different channel (lights and indicators), and the two are kept visually distinct. Final cargo uses the categories in section 6.5.

### 3.2 What Stands In For What

| Thing | Placeholder |
|---|---|
| Van body | Cube |
| Van wheels | 4 Cylinders |
| Van dashboard | Cube |
| Van cargo compartment | Cube |
| Van rear doors | 2 Cubes |
| Van steering wheel | Cylinder |
| Depot, Warehouse walls, garage, shelves, workbench, machinery | Cubes |
| Terminals | Cube + Quad (screen) |
| Cargo | Cube, Sphere, Capsule |
| Security Drone | Sphere + Cubes |
| Crawler | Capsule + Cubes |
| Security Turret | Cylinder + Cube |
| Humanoid Maintenance Robot | Capsule + Cubes |
| Wisp | Emissive Sphere |
| Stun Pistol | 2–3 Cubes |
| Shotgun | Cube + Cylinder |
| Crowbar | Cylinder |
| Repulsor | Cube + Sphere |
| Scanner | Cube + Quad |
| Flashlight | Cylinder |
| Roads | Plane + Cubes (curbs, barriers) |
| Doors, levers, buttons, plates | Cubes with separate moving parts |

Weapons attach to the camera as floating tools. There is no player body and no hands for a long time.

---

## 4. Asset Inventory by Category

Priority: **P1** needed first, **P2** needed for the Demo, **P3** only for release. Tier: the earliest milestone that needs a final version (MVP / Road Alpha / Demo / 1.0). At every tier the placeholder is acceptable until a suitable free asset is found. If no free asset fits, the placeholder or an in-house blockout stays.

### 4.1 Van

One small cargo van. Many cheap car models have a good exterior and an empty interior; because driving is first-person, an interior is required.

| Part | Need | Placeholder |
|---|---|---|
| Exterior | Required | Cube body |
| Interior cabin | Required | Cube interior |
| Cargo compartment | Required | Cube |
| Steering wheel | Required | Cylinder |
| Wheels as separate objects | Required | 4 Cylinders |
| Front/rear lights | Required | Emissive Cubes |
| Rear cargo doors | Required | 2 Cubes |
| Driver/passenger doors | Preferred | — |
| Hood, engine bay | Preferred | — |
| Separate bumpers/body parts | Later | — |
| Damage variants | Not needed for the MVP | — |

Priority P1; Tier: Road Alpha.

### 4.2 Depot and Courier Garage

The most visited location. Needs a **modular industrial/warehouse kit** that is assembled like LEGO, not a single finished building. The same kit then builds the Warehouse, the clinic maintenance zones, and tunnels.

| Category | Examples |
|---|---|
| Walls, floors, ceilings | Concrete, metal; warehouse/garage floors; industrial ceilings |
| Structural | Beams, columns |
| Doors and windows | Regular, security, rolling; industrial windows |
| Stairs, railings, platforms | Metal (railings required) |
| Pipes, ventilation | Several sizes; ducts |
| Lights | Fluorescent/industrial |
| Electrical | Panels, junction boxes |
| Storage | Shelves, racks |
| Garage | Workbench, tool cabinet |

Priority P1; Tier: MVP (placeholder) → replaced after Road Alpha.

### 4.3 Garage Props (Van Maintenance)

Most are decoration. A few are real gameplay objects.

| Decoration | Gameplay objects |
|---|---|
| Jack, repair tools, oil/fluid containers, charging cable, compressor, spare parts, cargo straps, portable generator | **Battery, Tire, Repair Kit, Cargo Stabilizer** (`CarryObject` / `Inventory` items) |

Also: toolbox, workbench, fuel/battery cell. Priority P2; Tier: Road Alpha.

### 4.4 Contract Terminal and Other Terminals

The asset provides the housing and screen; the screen shows game UI (UI Toolkit, RenderTexture, or world-space UI).

| Need | Placeholder |
|---|---|
| Industrial computer, wall terminal, touch screen, keypad, security scanner, display panel | Cube + Quad |

Priority P1; Tier: MVP (placeholder).

### 4.5 Cargo and Packages

The central part of the game. Not 30 unique models: about **8 base models + material variants**.

| Visual category | Examples |
|---|---|
| Ordinary parcel | Cardboard box |
| Secure case | Metal suitcase |
| Industrial crate | Reinforced box |
| Biological | Specimen container |
| Scientific | Glass capsule |
| Energy | Glowing power cell |
| Heavy cargo | Large metal container |
| Restricted | Black/red containment case |
| Medical | Refrigerated box |
| Strange | Abstract object/container |

Material variants: Normal, Medical, Biohazard, Classified, Electrical, Anomalous.

Priority P1; Tier: MVP (placeholder).

### 4.6 Anomalies

No large "anomaly asset pack." Take ordinary objects and make them strange through shader, audio, lighting, particles, animation, teleportation, and behavior.

Objects: statue, radio, TV, lamp, mask, sphere, cube, clock, mannequin, old computer, suitcase, doll, mirror.

Example: an ordinary low-poly TV that is closer every time the player looks away is scarier than an expensive monster. Priority P2; Tier: Demo.

### 4.7 Warehouse

Little new is needed if the Depot kit is good.

| Item | Why |
|---|---|
| Pallets | Used everywhere |
| Large shelves | Cheap maze and cover |
| Containers | Quick rooms |
| Conveyor | Fits the theme |
| Forklift, cargo carts, industrial machinery | Set dressing |
| Cranes, hooks, chains | Pendulum and crane hazards |
| Storage cages, barriers, warning signs | Set dressing |

Priority P1; Tier: MVP (placeholder).

### 4.8 Conveyor System

A modular conveyor pack: straight, corner, incline, splitter, rollers, supports, control panel. Easy to build in-house if not found. Placeholder: Cube frame + rotating Cylinders, or a moving surface. Depends on the pending `ConveyorBelt` mechanic. Priority P2; Tier: Demo.

### 4.9 Road Kit (Road Tier)

No big city pack. A **modular road kit**:

```text
straight road · curve · intersection · sidewalk · curb · guardrail · road barrier
streetlight · road signs · traffic cones · bollards · road markings
```

Several environments are built around the one road. Placeholder: Plane + Cubes. Priority P1 for the road tier; Tier: Road Alpha.

### 4.10 Industrial Roadside

Mostly background: warehouse exteriors, fences, pipes, containers, power poles, transformers, storage tanks, factory silhouettes, chimneys. Nothing here is enterable. Priority P2; Tier: Demo.

### 4.11 Old City / Residential

Only for the Apartments district. A small modular pack for **one night block**: building facades, doors, windows, stairs, balconies, street lamps, trash bins, benches, air conditioners, electric boxes, fire escapes. Do not take a large realistic city. Priority P3; Tier: Demo/1.0.

### 4.12 Underground / Tunnel

Cheap in assets; the industrial kit covers half: concrete tunnel modules, pipes, cables, lights, service doors, metal stairs, maintenance platforms, ventilation, electrical boxes. Priority P3; Tier: 1.0.

### 4.13 Clinic / Laboratory

Not for the MVP: clean modular walls, glass walls, laboratory tables, medical cabinets, containment chambers, computer terminals, specimen tanks, medical lights, hospital trolley, storage fridge. Priority P3; Tier: 1.0.

### 4.14 Doors

Because of the number of mechanics that use doors (`Door`, `ConditionLock`, `HoldInteraction`), a set is worth having:

| Type | Notes |
|---|---|
| Sliding door | The type the `Door` mechanic drives |
| Security door, heavy blast door, rolling garage door, cargo door | Drive with `Door` where the motion is a slide |
| Normal hinged door | Decorative unless the template gains rotation |

Priority P1; Tier: MVP (placeholder).

### 4.15 Interaction Props

One of the most useful small-prop packs:

```text
lever · big button · wall button · keypad · card reader · switch · fuse box
control panel · pressure plate · cargo scanner · power socket · generator · battery slot
```

Priority P1; Tier: MVP (placeholder).

### 4.16 Mechanic-Specific World Assets

| Mechanic | Asset |
|---|---|
| `PressurePlate` | Floor platform / scanner |
| `MovingPlatform` | Freight platform |
| `JumpPad` | Pneumatic or anomaly pad |
| `Teleporter` | Anomaly portal or frame |
| `Checkpoint` | Courier Emergency Locker |
| `DamageZone` | Steam, fire, electricity |
| `BreakableObject` | Crates, barricades, glass (intact + broken versions) |
| `ExplosiveBarrel` | Gas cylinder, fuel tank |
| `ForceZone` (pending) | Fan or anomaly emitter |
| `FallingPlatform` (pending) | Damaged catwalk |
| `RotatingPlatform` (pending) | Industrial turntable |
| `PhysicsButton` (pending) | Large physical switch |
| `Pendulum` (pending) | Suspended cargo / crane hook |
| `RopeSwing` (pending) | Cable / service rope |
| `HoldInteraction` (pending) | Terminal, fuse, repair point |
| `PushPullObject` | Pushable crate (flat ground only) |

On the MVP almost everything is a cube.

### 4.17 Player Body and First-Person Animation

No full-body model is needed. Later: first-person arms, gloves, courier uniform sleeves. Floating tools are fine until late production.

First-person animation set (much later, not for the MVP):

```text
idle · walk sway · pistol fire/reload · shotgun fire/reload · crowbar swing
scanner use · repair/tool use · package pickup/hold/put down
```

Priority P3; Tier: 1.0.

### 4.18 Weapons and Tools

No military pack. Stylized sci-fi/industrial tools give the game its own identity:

```text
Stun Pistol · Shotgun · Crowbar · Repulsor / energy tool · Scanner · Flashlight
Repair Tool · Grenade/EMP device · Medkit
```

Priority P2; Tier: Demo.

### 4.19 Enemies

Fits a small roster. Five visual archetypes are enough, modified through materials, lights, weapons, size, and effects.

| Enemy | Notes | Priority |
|---|---|---|
| **Security Drone** | Best first enemy: no legs, no humanoid rig, few animations. Needs body, rotors/thrusters, sensor, weapon. Procedural hover, rotation, tilt, recoil | P1 |
| **Crawler** | A small robot spider, quadruped, or maintenance robot (prefer a robot over an organic creature). Needs idle, move, attack, hit, death | P1 |
| **Security Turret** | Visual for the Watcher archetype | P2 |
| **Humanoid Maintenance Robot** | Candidate fifth archetype, decided after the Demo | P3 |
| **Anomaly / Wisp** | Emissive form with effects | P2 |

Road enemies are limited to a road drone, a stationary turret, an anomaly, or a road obstacle. No traffic, pedestrians, or police vehicles.

### 4.20 Materials

A good industrial material pack matters more than expensive models:

```text
concrete · painted metal · rusted metal · steel · aluminium · plastic · rubber
glass · asphalt · wood · cardboard · dirty wall · tile · hazard stripe
```

Priority P1.

### 4.21 Decals

One decal pack can transform the industrial kit: numbers, warning labels, arrows, floor markings, oil stains, dirt, scratches, leaks, graffiti, company logos, biohazard, electrical warning. Priority P2.

### 4.22 Signs

Make these in-house as textures/UI for a consistent style and, where text matters, localization: EXIT, AUTHORIZED PERSONNEL, DELIVERY, WAREHOUSE, CONTAINMENT, DANGER, HIGH VOLTAGE, BIOHAZARD, LEVEL 01, SECTOR B. All text is English and goes through localization keys where it is player-readable text.

### 4.23 Light Fixtures

Light sources are standard Unity Lights; fixtures need models and **emissive materials**: fluorescent lamp, ceiling lamp, streetlight, emergency lamp, industrial spotlight, vehicle headlights, warning beacon. Priority P1.

### 4.24 VFX

Here a good general free pack is worth hunting for. One sci-fi/general pack covers about 80%; the rest can be Unity particle systems made in-house.

| Effect | Use |
|---|---|
| Sparks | Electricity, repair |
| Smoke | Vehicle and world damage |
| Fire | Hazards |
| Explosion | Combat |
| Electricity | Anomalies, EMP |
| Muzzle flash, bullet hit | Weapons |
| Dust | Impacts |
| Toxic gas | `DamageZone` |
| Teleport | `Teleporter` |
| Distortion | Anomalies |
| Energy pulse | Repulsor |
| Scan effect | Scanner |
| Shield / containment | Cargo |

Priority P2.

### 4.25 Anomaly Shader

One in-house shader (distortion, dissolve, fresnel, noise, emission) turns a Cube into an anomaly, portal, energy field, or unstable object. It must keep the `_BaseColor` property so hit flashes still work (2.3). Priority P2; Tier: Demo.

### 4.26 Weather and Sky

Minimal: rain and fog. Snow and sandstorms are not needed. A night sky or procedural sky; darkness and fog hide most of the world. A wet night road under lights already looks good.

### 4.27 UI Assets

Almost nothing is sourced. UI is built in UI Toolkit in the style of a corporate terminal. Needed: icons, font, logo, cursor. Panels, buttons, meters, tables are code/USS.

- **Icons:** one monochrome SVG pack, easy to tint. Health, Ammo, Battery, Fuel, Repair, Cargo, Fragile, Light, Timer, Biohazard, Electric, Warning, Credits, Route, Weapon, Inventory, Upgrade.
- **Fonts:** at most one main UI font and one optional display/terminal font. Only English is required now; if a later localization needs Cyrillic, pick fonts that cover it.
- **Vehicle dashboard:** speedometer, vehicle condition, fuel/battery, cargo stability, headlight indicator, warning lights. Built as game UI, not sourced.

### 4.28 Audio

For this game sound matters more than many models. Priority P1 for a basic set; Tier: grows with each tier.

| Group | Needed |
|---|---|
| **Environment** | Warehouse ambience, city night, wind, rain, industrial hum, electric hum, tunnel ambience |
| **Vehicle** | Engine idle/acceleration/deceleration, starter, brakes, tire rolling, gravel/asphalt, collision, suspension impacts, doors, cargo door, horn |
| **Player** | Footsteps (concrete, metal, wood, gravel), jump/landing, equipment interaction |
| **Interaction** | Door, lever, button, keypad, scanner, terminal, repair, pickup/drop |
| **Combat** | Weapons, reload, impacts, explosion, enemy hit |
| **Anomalies** | Whispers, knocks, scratching, radio interference, low-frequency hum, distorted voices, electrical pulses |

Anomaly audio is often a rule (accelerating ticking, whisper from the left), so its authoring is a design task, not just sourcing.

### 4.29 Music

Very little: Depot ambient theme, driving ambient layer, danger/combat layer, rare anomaly music. Environment sound carries most of the time.

### 4.30 Radio and Voice

Dispatcher, corporate announcements, radio messages. Not a blocker: text on the prototype. For release choose between recorded voice, licensed text-to-speech, or a voice actor, depending on what is available.

### 4.31 Animation Props

Most of the environment needs no animation clips: `Door`, `Lever`, `MovingPlatform`, `PressurePlate`, conveyors, and `RotatingPlatform` are moved through Tween and physics. Models need correct pivots and separate moving parts.

### 4.32 Store and Marketing Assets

Steam capsule, logo, key art, screenshots, trailer, GIFs. Only after a playable vertical slice.

---

## 5. What Not to Take

- A traffic vehicle pack, a pedestrian pack, police cars.
- A military pack with dozens of weapons.
- A large realistic open-world city.
- A separate "anomaly monster" pack.
- Many unrelated environment packs in different styles.
- UI kits (the UI is built in-house).

---

## 6. Sourcing Order (Free Assets Only)

No asset is purchased. Everything is a free, ready-made asset whose license permits commercial use (section 6.4), or is made in-house, or stays a placeholder.

### 6.1 The Seven Core Needs

| Need | Covers | Free-source expectation |
|---|---|---|
| Modular Industrial / Warehouse | Depot, Warehouse, much of the interior | Usually findable as low-poly kits |
| Modular Roads / Urban | Driving | Usually findable as low-poly road kits |
| Industrial Props | Terminals, tools, crates, machinery | Usually findable |
| Cargo Van | The van | **Hardest.** Free vans often lack an interior. Fallback: a free exterior plus an in-house cab and cargo bay built from primitives, or keep the placeholder van |
| Robots / Enemies | AI | Findable; check the rig and the animation set. The drone needs none |
| General Sci-Fi / Industrial VFX | Effects | Findable; gaps are filled with in-house particle systems |
| General Industrial / Horror SFX | Audio | Findable, but licenses vary per file; filter strictly |

### 6.2 Optional, Later

Clinic/Lab kit, Apartment/City kit, first-person arms, weapon/tool kit, decals.

### 6.3 Gate

No sourcing until: the MVP loop is fun, and for the road assets (roads, van) additionally until Road Alpha proves that driving is worth building on. If a milestone fails, spend the effort on gameplay, not art.

### 6.4 Free Asset Rules

An asset may be used only if its license is clear and permits commercial use in a paid Steam game.

| License | Decision |
|---|---|
| CC0 / public domain | Preferred |
| Permissive terms that allow commercial use in a game (including the Unity Asset Store license for free assets) | Allowed. Do not redistribute the source files outside the game |
| CC BY (attribution) | Allowed; the credit goes in the register and later in the credits screen |
| CC BY-SA (share-alike) | Ask the user first |
| Any NonCommercial (NC), NoDerivatives (ND), "personal use only", unclear, or missing license | **Rejected** |

Rules:

1. Read the license on the asset's own page and save a copy of it (or its URL and date) in the register. Never assume a creator's other assets share a license.
2. Prefer one creator family for the main environment kit so the style matches.
3. Third-party packs go under `Assets/ThirdParty/<PackName>/`, untouched, with their license file. Game prefabs and variants are built under `Assets/Game/`; never edit a pack in place. Delete unused demo scenes and content to keep the repository small.
4. The pack must work with Unity 6000.6 and URP 17; convert materials to the shared URP material set; check the shader exposes `_BaseColor` (2.3).
5. Scale is 1 unit = 1 meter. Check pivots and separate moving parts (2.3) before use.
6. Binary files (models, textures, audio) are stored through Git LFS per `.gitattributes`.
7. Free packs are inconsistent. If one does not fit the style, material set, or gameplay prefab, keep the placeholder or build a simple in-house version instead of bending gameplay.

### 6.5 Where to Look

Commonly used free sources. The license of each individual asset must still be checked on its page.

| Source | Typical content | Typical license |
|---|---|---|
| Kenney | Low-poly kits, props, UI, audio | CC0 |
| Quaternius | Low-poly models, robots, props | CC0 |
| ambientCG, Poly Haven | PBR materials, textures, HDRIs | CC0 |
| Unity Asset Store (free section) | Kits, VFX, tools | Asset Store license |
| Freesound | Sound effects, ambience | Per file (CC0, CC BY, or NC; reject NC) |
| OpenGameArt, itch.io free assets, Sketchfab | Mixed | Per asset; filter strictly |
| Google Fonts | Fonts | Open font licenses |

---

## 7. Replacement Workflow

For each placeholder replaced by a final asset:

1. Confirm the prefab already follows section 2 (stable root, `Visual` child, anchors).
2. Import the asset; check scale, pivot, and moving-part separation against section 2.3.
3. Swap the content under `Visual` (`PH_` object out, final model in). Do not change gameplay components, colliders, or anchors, except to adjust collider size for the new silhouette.
4. Transfer anchor positions if the silhouette moved them.
5. Assign materials from the shared material set; check `_BaseColor` support for hit flashes.
6. Open the scene, enter Play Mode, and verify the interaction still works.
7. Record the asset in the register (section 8) and remove the `PH_` object.

Acceptance: no gameplay script changed, no console errors, the interaction behaves as before.

---

## 8. Asset Register

Keep a record of every non-primitive asset: source, license, and status. Add rows as assets arrive. The attribution column feeds the credits screen at release.

| Asset | Replaces | Author | Source URL | License | Attribution required (text) | Date checked | Status |
|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — |

Status values: `Placeholder`, `Blockout`, `Final`.

---

## 9. Notes for Agents

- Build every prefab per section 2 from the first version, even with primitives.
- Use `PH_` for all placeholder objects.
- Never purchase assets. Do not import or download third-party assets without the user's explicit instruction; when told to, use only assets that satisfy section 6.4 and record each in the register (author, URL, license, attribution, date).
- Never place a third-party asset with an unclear or NC/ND license in the project.
- Do not write gameplay code that depends on asset structure.
