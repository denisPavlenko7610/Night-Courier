# Night Courier: Next Steps

Background for the next work. Design lives in [GDD.md](GDD.md) (section 0 has the status); phases and constraints live in
[Production-Plan.md](Production-Plan.md). The ordered tasks are in [Next-Sprint.md](Next-Sprint.md).

## Done in the last pass (M0, minimal M1, part of M2)

- Reliability: refused abort during settlement, rejected repeated delivery callbacks, settlement and save failures still
  end in a usable panel, the Depot recovers from a stuck run, a double click on Accept is ignored.
- Light field: range, spot cone, and shadow-aware occlusion in `LightReach` (the Warehouse behaves as before).
- Clarity: briefing with rules, time limit, bonuses, and threats; a breaker-panel hint that says why; scan-state and
  "scan complete" hints; one-line rule announcements; a refused delivery explains itself; damage notices name the cause.
- Result report: violations, integrity, property damage, deaths, and up to three improvement lines.
- Tests: 217 EditMode tests and four PlayMode tests of the full loop, failure and retry, pause-menu abort, and death
  (see "Play Mode tests" below), plus a manual script for real input.

## Done in the readable-run sprint

- PlayMode saves isolated (`GameHost._saveFolder`, `Saves.PlayModeTests`); the old fixture that moved the real `Saves` folder is gone.
- Scanner identity: only the bound package, not held, starts a scan; leaving the plate aborts it with a notice.
- Blocked-object reasons on the prompt; dark and interrupted-scan notices; a package on a dead scanner asks for power.
- Blackout: an always-on work lamp and a safe crate in the dock, a flat dark ambient, a "park the package" objective.
- UI: every canvas scales with `Expand`; the HUD, terminals, pause menu, and result panel no longer overlap or clip at any tested
  aspect ratio (`UiLayoutPlayModeTests`, 7 resolutions, 18 screens); the "no violations" bonus ignores property damage, which has its own fine.
- Tests: 222 EditMode and 18 PlayMode tests (11 gameplay, 7 UI layout). Not done: a human playthrough (see [Playtest-Checklist.md](Playtest-Checklist.md)).

## Tasks

The task list moved to [Next-Sprint.md](Next-Sprint.md); it no longer repeats finished work. The manual script moved to
[Playtest-Checklist.md](Playtest-Checklist.md).

## Play Mode tests

`Assets/Game/Tests/PlayMode` (`NightCourier.Game.PlayModeTests`) loads the real `Boot` scene and drives the real scene
entries: button clicks on the menus and the contract terminal, `Interactable.Press` and hold ticks on the package, van,
breaker lever, and delivery terminal, and a teleport of the player next to each target. It does not synthesize keyboard,
mouse, or gamepad events and does not click the confirmation popup (the pause-menu abort test calls the popup's confirm
callback). This is an integration test, not a playthrough: it does not prove that the route, the hints, or the physical
interaction work for a person.

`GameLoopPlayModeTests` (five tests): the full loop with a double Accept, double Deliver, double Return and Continue; a
destroyed package failing without payout and the retry paying once; abort from the pause menu; death in transit with the
recovery fee; a malformed package prefab being rejected with a retry.

`WarehouseRulesPlayModeTests` (four tests): the scanner ignoring a stray `Cargo`-layer object, aborting when the package is
picked off the plate, and scanning the real package; Lights Out (the parked package lit by the work lamp, dark notices, the
blocked-object reasons, the barricade prompt text); Express with both threats active and a settled result; a delivery whose save fails (the exception is expected through
`LogAssert`), checking the warning, the credits kept in memory, and one payout.

**Save isolation.** `GameHost` has a serialized `_saveFolder` (default `Saves`). The fixtures set it to `Saves.PlayModeTests`
from a `sceneLoaded` callback, before the host's `Start`, and delete only that folder. The tests never move, back up, or delete
the real `Saves` folder. `PlayerPrefs` (settings and input bindings) are not touched by the tests.

Run them with the Unity Test Runner (PlayMode tab) or the MCP `run_tests` tool with `mode: PlayMode` and the full class names
(`NightCourier.PlayModeTests.GameLoopPlayModeTests`, `NightCourier.PlayModeTests.WarehouseRulesPlayModeTests`); filtering by
assembly name alone runs the template PlayMode tests instead.

## Mechanic integration queue (not part of this pass)

`HoldInteraction` is already used by the delivery terminal. The other reserved mechanics (ConveyorBelt, ThrowableObject,
ForceZone, FallingPlatform, RotatingPlatform, DisappearingPlatform, PhysicsButton, Pendulum, RopeSwing) wait for content
that needs them (Template-Mapping.md section 4). Do not start one without reading its README and demo first.

## Decisions waiting for the user

See [Next-Sprint.md](Next-Sprint.md): the Express option, the settlement failure recovery, repeatable contracts, and vehicle ownership.
