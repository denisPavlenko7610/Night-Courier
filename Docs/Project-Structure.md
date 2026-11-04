# Project Structure

All game code and content lives under `Assets/Game`. Folders group responsibilities; they do not introduce new services or frameworks.

```text
Assets/Game/
  Bootstrap/
    Steps/
  Infrastructure/
    Input/
    Localization/
    Persistence/
    Settings/
  Gameplay/
    AI/
    Campaign/
    Contracts/
    Economy/
    Loadout/
    Packages/
    Player/
    Weapons/
    World/
  Features/
    MainMenu/
    Depot/
    Warehouse/
      World/
    Training/
  Presentation/
    Audio/
    Effects/
    UI/
      Common/
      Depot/
      Hud/
      Menus/
      Results/
    Materials/
    Placeholders/
  Content/
    Prefabs/
      Player/
      Enemies/
      Packages/
    UI/
  Scenes/
  Tests/
    Editor/
    PlayMode/
```

## Ownership

- `Bootstrap` owns GameHost, GameComposition, the application service bundle, scene-entry contracts, scene ids, and Startup steps. It constructs and connects existing services.
- `Infrastructure` adapts external concerns: input contexts, localization tables, settings, and campaign persistence. CampaignSave and CampaignState remain together in `Gameplay/Campaign` as the campaign data model; CampaignRepository implements storage in `Infrastructure/Persistence`.
- `Gameplay` owns game-specific policies and components composed from UnityTemplates. Each existing domain keeps its own folder. Cross-domain contracts remain in their owning domain.
- `Features` connects domains into concrete scene flows. MainMenuEntry, DepotEntry, and WarehouseEntry are composition entry points, not UI widgets. Warehouse reactions (PackageWatch and EscalationResponder) stay beside WarehouseEntry. FirstDeliveryTraining is shared by Depot and Warehouse and lives under Training.
- `Presentation` owns reusable views, HUD formatting, audio listeners, visual effects, and visual resources. Scene entry points may reference these views; views receive their dependencies explicitly. UI is grouped into Common, Depot, Hud, Menus, and Results. SettlementAdvice belongs to Results because it selects presentation copy from an already computed outcome.
- `Content` holds authored assets and prefab instances. `Scenes` holds the existing Boot, MainMenu, Depot, and Warehouse scenes. Scene paths remain stable; prefab paths are grouped by owner, with menu and HUD prefabs under Content/UI.
- `Tests` mirrors the existing EditMode and PlayMode boundaries.

The reusable framework is supplied by the linked UnityTemplates packages. Do not create a second game-side Framework layer. Add folders only when they have an actual owner and content; future vehicle glue belongs in `Gameplay/Vehicle` if that ownership is approved.

## Compatibility

This organization preserves existing namespaces (`NightCourier.App`, `NightCourier.UI`, and the domain namespaces), assembly names, script GUIDs, and asset GUIDs. Folder names describe ownership; they are not an assembly or namespace migration. NightCourier.Game remains the runtime assembly, and the two test assemblies keep their existing references. Introducing additional assemblies should follow a demonstrated need and an explicit dependency review.

The package installer remains in `Assets/Editor`: it is an editor-only package maintenance tool outside the game runtime assembly. Third-party assets, plugins, and Unity rendering settings retain their dedicated top-level folders.

Move Unity assets together with their `.meta` files. Preserve scene paths, update any literal asset paths and documentation in the same change, then compile and run the relevant Unity tests.

## Scene-specific policies

DepotObjectiveHints and WarehouseObjectiveHints select objective copy beside their scene entry points; WarehouseProgress is a separate file beside WarehouseObjectiveHints. Gameplay/Contracts retains contract execution and settlement rules.

Features/Warehouse/World owns WarehouseFacts, WarehouseConditions, WarehouseLighting, and their directly coupled components: ConditionDoor, PowerLever, FreightLift, CargoScanner, and GeneratorSocket. These components already depend on warehouse state; moving the whole group keeps that dependency within the feature. Gameplay/World retains the remaining components. CargoPackage lives in Gameplay/Packages and keeps its explicit PlayerCarry binding.

Existing namespaces are retained during these moves, including NightCourier.Contracts for objective helpers and settlement advice, NightCourier.World for warehouse components, and NightCourier.Player for CargoPackage. Type names change only where ObjectiveHints is split into the two scene-specific helpers. No save schema or localization key changes are needed.
