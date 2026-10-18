# Unity Templates

The packages are linked locally from the sibling project `../UnityTemplates` through the Unity Package Manager. Modules
are separate packages (`com.rd.save`, `com.rd.input`, and so on), and the combat, AI, and interaction mechanics are
grouped into three kits: `com.rd.combat` (Health, StatusEffects, the combat core, and the weapon, melee, damage,
checkpoint, respawn, and breakable mechanics), `com.rd.ai` (detection, chase memory, patrol, spawners), and
`com.rd.interaction` (Conditions, Tween, doors, levers, plates, pick-up and carry mechanics). Pause, Timers, StateMachine,
and the other foundation utilities now ship inside `com.rd.foundation`. The movement and traversal mechanics that were not
grouped (Dash, Stamina, MovingPlatform, JumpPad, and so on) stay separate packages. Assembly names did not change, so game
assemblies reference the same `RD.*` names as before.

To reinstall all packages: `Tools > Unity Templates > Install All Local Packages`. The installer lives in
`Assets/Editor/TemplatePackages.cs` and uses the Unity Package Manager API; it enumerates the current `package.json`
files, so run it, or edit `Packages/manifest.json` by hand, after the library layout changes.

In UnityTemplates, `python Publish/generate_upm.py` creates or updates the package descriptions after modules are added
or assembly references change. Details: [UPM.md](../UnityTemplates/UPM.md).

The installed set contains 48 RD packages: the three kits, 32 modules, the separate movement and traversal mechanics, and
two optional support packages. Haptics is intentionally excluded for this Windows/Steam project, including when running
the installer again. Kit and module packages are linked at their root, which also holds their `Demo/` and `Tests/`
folders, so the demo assemblies compile in this project. That is why the project also links `com.rd.demosupport` and
`com.rd.mechanics.demosupport` (shared demo code) and the Unity packages `com.unity.localization` and
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
