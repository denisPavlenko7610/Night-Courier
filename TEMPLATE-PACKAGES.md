# Unity Templates

The packages are linked locally from the sibling project `../UnityTemplates` through the Unity Package Manager. The
library ships seven kits, each one package: `com.rd.combat` (Health, StatusEffects, the combat core, and the weapon,
melee, damage, checkpoint, respawn, and breakable mechanics), `com.rd.ai` (StateMachine, detection, chase memory, patrol,
spawners), `com.rd.interaction` (Conditions, Tween, doors, levers, plates, pick-up, carry, hold, and throw mechanics),
`com.rd.platformer` (Dash, Stamina, jumps, traversal, and the moving-world mechanics such as MovingPlatform),
`com.rd.gameplay` (Wallet, Inventory, Equipment, Stats, Progression, Quests, Rewards, Achievements), `com.rd.framework`
(Startup, SceneFlow, Save, Settings, Input, Localization, Analytics, Addressables, Tutorials, Events, Attributes,
Extensions, DependencyInjection), and `com.rd.presentation` (Audio, PrefabWindows, ScreenNavigation, Haptics). Pause,
Timers, and the other foundation utilities ship inside `com.rd.foundation`, which every kit depends on. Assembly names did
not change, so game assemblies reference the same `RD.*` names as before.

To reinstall all packages: `Tools > Unity Templates > Install All Local Packages`. The installer lives in
`Assets/Editor/TemplatePackages.cs` and uses the Unity Package Manager API; it enumerates the current `package.json`
files, so run it, or edit `Packages/manifest.json` by hand, after the library layout changes.

In UnityTemplates, `python Publish/generate_upm.py` creates or updates the package descriptions after units are added or
assembly references change. The library's UPM.md still describes the older per-unit packages; the kit layout above is the
current one.

The installed set contains 10 RD packages: Foundation, the seven kits, and two optional demo-support packages. The
Presentation kit also contains Haptics, which is Android/iOS only and stays unused in this Windows/Steam project (the
code compiles but the game never references it). Kit packages are linked at their root, which also holds their `Demo/` and
`Tests/` folders, so the demo assemblies compile in this project. That is why the project also links `com.rd.demosupport`
and `com.rd.mechanics.demosupport` (shared demo code) and the Unity packages `com.unity.localization` and
`com.unity.addressables` (referenced by the Localization and Addressables demos). Game code uses none of them. Gameplay
integration remains separate from package installation.

These are live links to shared sources: changes in UnityTemplates also affect Night Courier. On another computer, both
repositories must sit side by side with the same folder names. Changes to package descriptions must be committed in the
UnityTemplates repository, while `Packages/manifest.json` and `Packages/packages-lock.json` are committed in Night
Courier.

All RD packages are direct project dependencies because they are not yet available from a package registry. The umbrella
package `com.rd.templates` must not be installed at the same time: it contains the same assemblies.

See also [AGENTS.md](AGENTS.md) and [Docs/Template-Mapping.md](Docs/Template-Mapping.md) for how the game uses these
packages.
