# Night Courier: Next Sprint

This list replaces the task table in [Next-Steps.md](Next-Steps.md). It does not repeat finished
work: scanner identity, blocked-object prompts, the emergency lamp, dark and scan notices, test-save isolation, and the
save-failure test are done (see GDD section 0.7). The ordered tasks below each end in a run, not a code reading.

Sizes: S under half a day, M about a day.

| # | Task | Size | Acceptance |
|---|---|---|---|
| 1 | First honest playtest of Runs A and B | M | Three people who have not seen the game complete [Playtest-Checklist.md](Playtest-Checklist.md) Runs A and B with the keyboard and mouse, no teleports, no console. The session log is filled in. Record the time to the first delivery, every stall over 20 s, and the false violations. Fix the worst blocker found or file it here. Until this is done nothing in the game is "playtested" |
| 2 | Give Express a real dilemma | M | Decision first (see "Express blocker" below). After the change, timed runs of both routes by a bot or a person give a short-route time `R` and a safe-route time `S` with `R` at least 25% below `S` at the same carry state, and the deadline plus the on-time bonus sit between them (`Contract04_Express.asset`). The EditMode content tests still pass. A Play Mode test reaches a settled Express result by each route |
| 3 | Make the Warehouse night read as night | S | The default skybox is replaced (a night sky, or a ceiling plane on the `Visual` child) so the sky above the walls is not bright during a blackout. Screenshots from Play Mode at the dock and the hall, powered and unpowered, show a clear difference, and the emergency-lamp pool is visible against the dark. The ambient colour stays `Flat` |
| 4 | Highlight the scanner plate and the delivery terminal | S | The plate and the terminal carry an emissive accent that follows the light language (green for destination, yellow for interactables, GDD 17.1). A tester finds the delivery room without the objective line. Placeholder visuals only, on a `Visual` child, with the `_BaseColor` rule |
| 5 | Lights Out and Restricted Cargo end to end in Play Mode | S | `GameLoopPlayModeTests` gains two tests: Lights Out (park the package, break the barricade, battery, generator, breaker, delivery) and Restricted Cargo (same, with Do Not Drop; no violation if the package is parked on the crate, so the no-violations bonus is paid). Each reaches a settled result with asserted credits |
| 6 | Depot lost-package safety | S | Check whether the package can leave the Depot floor or be thrown out of reach. Either give the Depot pause menu a "Cancel Contract" action (confirmation, then `ContractBoard.Fail`) or respawn the package at its spawn point. A Play Mode run where the package is lost still lets the player continue |
| 7 | Settlement failure recovery | S | A decision from the user: retry the grant on the failure panel, or restore the campaign from the last save. Then implement it with an EditMode test using a failing reward handler. Today a failed grant leaves the quest completed without payout |

## Express blocker (task 2)

Measured on the baked NavMesh of the Warehouse (a static measurement, not a playthrough). The start is the dock spawn.

| Route | Walking distance to the scanner area | What stands in the way |
|---|---|---|
| Safe: breaker, maintenance door, west aisle, north aisle, freight lift | about 57 m plus the lift ride | The security drone patrols this route (Express uses the drone only) |
| Short: breaker, barricade, south floor, ramp | about 62 m plus a few crowbar hits (the barricade has 40 health, the crowbar deals 18 by the prefab data) and a ₡20 fine | Two crawlers on the south floor (added to Express by the data change below; before it, nothing) |

The "short, dangerous" route is neither shorter nor more dangerous, so no value of `_timeLimitSeconds` creates a dilemma.

**Sequencing rule.** The third contract is worked on only after a person has shown that the first two differ in play
(task 1: Run A against Run B). Today that difference is confirmed by Play Mode tests on state, not by a human. The data
half below was applied early at the user's request; reverting it is one line (`_encounters: 01000000i` in
`Contract04_Express.asset`) and nothing else depends on it.

**Chosen option 1, first half done (data only).** `Contract04_Express.asset` now lists the Crawlers and the Security
Drone, so the barricade route passes two crawlers. Verified: 221 EditMode tests pass, and a Play Mode test
(`Express_ActivatesBothThreats_AndSettlesWithTheOnTimeBonus`) shows both encounter groups active and a settled Express
result with Base pay 320. The teleporting driver does not walk the route, so the danger itself is not yet felt by a player.

**Correction to the first draft of this section.** The second half of option 1 was "open a gap in the wing wall
`PH_WingWestA` so the short route shortens by about 15 m". That number was a guess and is wrong. The barricade exit to the
ramp foot is already near a straight line (about 32.6 m of path against about 28 m straight), so a gap in that wall saves
about 4 m, and even a perfect straight line through every wall saves about 9 m of the 62. The common prefix (start to
breaker panel, about 13 m) is shared by both routes. A gap alone cannot make `R` 25% below `S`, so it is not worth a
scene edit by itself.

What can make the short route faster (pick one, each is content, not a new system; measure `R` and `S` with a bot or a
person after the change):

1. Lengthen the safe route in Express only: the lift is the single bottleneck, so a slower ride or a second stop is the
   smallest lever, but it must not slow Standard Delivery (the same lift), so it needs a per-contract value or a second
   lift console. Open question: the elevator speed and ride time have not been read yet.
2. Shorten the short route with a real shortcut piece (a second ramp or a jump over the strip into the wing) near the
   barricade exit. This is a small geometry addition and the only change that really shortens the walk.
3. Accept a risk-for-time dilemma without a time gap: both routes take about the same time, the deadline is tight (for
   example 40 s), and the crawlers plus the drone decide how many seconds each route costs. This needs timed runs first.

Do not rely on moving the drone alone: it changes only the danger, not the time.

## Decisions waiting for the user

- Task 2: which option above.
- Task 7: retry the grant or restore from the save.
- Whether finished contracts become repeatable or new contracts come from combining states, rules, and encounters (the
  combination test, GDD 22). The finished campaign still ends at "No contracts are available right now."
- Vehicle ownership for the road tier ([Production-Plan.md](Production-Plan.md), Open Decisions). The road tier stays
  blocked until the MVP loop is fun, which tasks 1 and 2 measure.
