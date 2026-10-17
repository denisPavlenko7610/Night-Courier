# Unity Templates

The packages are linked locally from the sibling project `../UnityTemplates` through the Unity Package Manager. Every module and every mechanic is a separate package: `com.rd.save`, `com.rd.input`, `com.rd.mechanics.door`, and so on. Shared dependencies, including Foundation, are linked once.

To reinstall all packages: `Tools > Unity Templates > Install All Local Packages`. The installer lives in `Assets/Editor/TemplatePackages.cs` and uses the Unity Package Manager API.

In UnityTemplates, `python Publish/generate_upm.py` creates or updates the package descriptions after modules are added or assembly references change. Details: [UPM.md](../UnityTemplates/UPM.md).

The packages include module sources together with their editor tools, and the mechanics' Runtime. Demo scenes, examples, and tests of the template stay in UnityTemplates.

These are live links to shared sources: changes in UnityTemplates also affect Night Courier. On another computer, both repositories must sit side by side with the same folder names. Changes to package descriptions must be committed in the UnityTemplates repository, while `Packages/manifest.json` and `Packages/packages-lock.json` are committed in Night Courier.

All RD packages are direct project dependencies because they are not yet available from a package registry. When reducing the set, remove dependent mechanics first, then their shared dependencies. The umbrella package `com.rd.templates` must not be installed at the same time: it contains the same assemblies.

See also [AGENTS.md](AGENTS.md) and [Docs/Template-Mapping.md](Docs/Template-Mapping.md) for how the game uses these packages.
