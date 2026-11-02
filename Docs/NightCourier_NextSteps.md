# Night Courier: Next Steps

Ordered work after the reliability and clarity pass. Design lives in [GDD.md](GDD.md) (section 0 has the status and the
M0–M4 roadmap); phases and constraints live in [Production-Plan.md](Production-Plan.md). Each task has a size, its
dependencies, and an acceptance check that is a run, not a code reading.

Sizes: S under half a day, M about a day, L several days.

## Done in the last pass (M0, minimal M1, part of M2)

- Reliability: refused abort during settlement, rejected repeated delivery callbacks, settlement and save failures still
  end in a usable panel, the Depot recovers from a stuck run, a double click on Accept is ignored.
- Light field: range, spot cone, and shadow-aware occlusion in `LightReach` (the Warehouse behaves as before).
- Clarity: briefing with rules, time limit, bonuses, and threats; a breaker-panel hint that says why; scan-state and
  "scan complete" hints; one-line rule announcements; a refused delivery explains itself; damage notices name the cause.
- Result report: violations, integrity, property damage, deaths, and up to three improvement lines.

## Tasks

| # | Task | Size | Depends on | Acceptance |
|---|---|---|---|---|
| 1 | First-time-player playtest of contracts 01–03 | M | — | Three people who have not seen the game play Standard Delivery, Fragile, and Lights Out without help. Each can say, unprompted, what the rule was, why power was needed, and what the scanner did. Record the time to the first delivery and every place a player stood still for more than 20 s. Fix the worst blocker found, or file it here |
| 2 | Tune Express urgency | S | 1 (timings) | Timed human runs of both routes (safe: breaker, maintenance door, lift; short: barricade, strip, drone) give a safe-route time `S` and a short-route time `R`. Set the deadline and the on-time bonus so that `R` earns the bonus and `S` does not, for at least two of three testers. Data change only (`Contract04_Express.asset`); the EditMode content tests still pass |
| 3 | Highlight the delivery terminal and the scanner plate | S | — | The delivery point and the scanner plate carry an emissive accent that follows the light language (green for destination, yellow for interactables, GDD 17.1); a tester finds the delivery room without the objective line. Placeholder visuals only, on a `Visual` child |
| 4 | Depot lost-package safety | S | — | Check whether the package can leave the Depot floor or be thrown out of reach. Either give the Depot pause menu a "Cancel Contract" action (a confirmation, then `ContractBoard.Fail`) or respawn the package at its spawn point. A Play Mode run where the package is lost still lets the player continue |
| 5 | Settlement failure recovery | S | decision | Today a failed grant leaves the quest completed without payout (the run is abandoned so the Depot starts). Decide between retrying the grant on the failure panel and restoring the campaign from the last save; implement it with an EditMode test using a failing reward handler |
| 6 | What happens after contract five | M | decision | The user picks one: repeatable contracts with diminishing pay, or new contracts by combining existing states, rules, and encounters (the combination test, GDD 22). A finished campaign no longer ends at "No contracts are available". New content is data only |
| 7 | Rank unlocks | S | 6 | A minimum courier rank on a contract (data field on `ContractDefinition`, checked in `ContractBoard.IsAvailable`), shown in the briefing. EditMode tests for locked, unlocked, and saved-rank cases |
| 8 | Fix the clipped rank label in the Depot header | S | — | At 2560×1440 and 1920×1080 the rank text "Rank: Trainee (0 / 100 XP)" is fully visible (the screenshot in this pass showed it cut at the right edge) |
| 9 | Play Mode regression test for the whole loop | M | — | A PlayMode test assembly runs New Game → accept → pickup → van → Warehouse → power → scan → deliver → result → Depot through the real scene entries and asserts credits, rank, and the available contracts; a second test covers failure and retry; both run in the Unity test runner without manual steps |
| 10 | Real-input soak | S | 9 | A person plays contracts 01, 03, and 04 with keyboard and mouse, then with a gamepad, for 30 minutes. No stuck input context, no double payout, no console errors beyond the known `PickupItem.Drop` kinematic warning |

## Mechanic integration queue (not part of this pass)

`HoldInteraction` is already used by the delivery terminal. The other reserved mechanics (ConveyorBelt, ThrowableObject,
ForceZone, FallingPlatform, RotatingPlatform, DisappearingPlatform, PhysicsButton, Pendulum, RopeSwing) wait for content
that needs them (Template-Mapping.md section 4). Do not start one without reading its README and demo first.

## Decisions waiting for the user

- Task 5: retry the grant or restore from the save.
- Task 6: repeatable contracts or contracts by combination.
- Vehicle ownership for the road tier (Production-Plan.md, Open Decisions); the road tier is blocked until the MVP loop is
  fun, which tasks 1 and 2 measure.
