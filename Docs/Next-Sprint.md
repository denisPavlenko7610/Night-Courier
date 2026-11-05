# Night Courier: Next Sprint

Open work only, in order. Finished work is not repeated here. Each task ends in a run, not a code reading; scripted
scenarios do not close human acceptance.

Sizes: S under half a day, M about a day.

| # | Task | Size | Acceptance |
|---|---|---|---|
| 1 | First honest playtest of Runs A and B | M | Three people who have not seen the game complete [Playtest-Checklist.md](Playtest-Checklist.md) Runs A and B with the keyboard and mouse, no teleports, no console. The session log is filled in. Record the time to the first delivery, every stall over 20 s, and the false violations. Fix the worst blocker found or file it here. Until this is done nothing in the game is "playtested" |
| 2 | Give Express a real dilemma | M | Use the selected shortcut (see "Express blocker" below). After the change, timed runs of both routes by a bot or a person give a short-route time `R` and a safe-route time `S` with `R` at least 25% below `S` at the same carry state, and the deadline plus the on-time bonus sit between them (`Contract04_Express.asset`). The EditMode content tests still pass. A Play Mode test reaches a settled Express result by each route |
| 3 | Highlight the scanner plate and the delivery terminal | Implemented; human acceptance open | Yellow scanner frame and green terminal strips under Visual use self-lit materials with no colliders or lights. Verify that a tester finds the destination without the objective line and that carried cargo does not hide the cues |
| 4 | Drive the enemies by hand | S | A person fights a crawler and the drone in the Express Warehouse: the Walk, Attack, Hit and TurnOff animations (`EnemyAnimation`) read correctly during real play. `EnemyAnimationPlayModeTests` only proves Idle and Hit |

## Express blocker (task 2)

Measured on the baked NavMesh of the Warehouse (a static measurement, not a playthrough). The start is the dock spawn.

| Route | Walking distance to the scanner area | What stands in the way |
|---|---|---|
| Safe: breaker, maintenance door, west aisle, north aisle, freight lift | about 57 m plus the lift ride | The security drone patrols this route (Express uses the drone only) |
| Short: breaker, barricade, south floor, ramp | about 62 m plus a few crowbar hits (the barricade has 40 health, the crowbar deals 18 by the prefab data) and a ₡20 fine | Two crawlers on the south floor (`Contract04_Express.asset` lists the Crawlers and the Security Drone) |

The "short, dangerous" route is neither shorter nor more dangerous, so no value of `_timeLimitSeconds` creates a dilemma.
A gap in the wing wall alone does not help: it saves about 4 m of the 62 (about 9 m with a perfect straight line through
every wall), and the first 13 m to the breaker panel are shared by both routes.

**Sequencing rule.** The third contract is worked on only after a person has shown that the first two differ in play
(task 1: Run A against Run B). The teleporting test driver does not walk the route, so the danger itself is not yet felt
by a player.

The user selected option 2, a genuine dangerous shortcut. After human Runs A and B, measure `R` and `S` with a bot or a
person after the change. The considered options were:

1. Lengthen the safe route in Express only: the lift is the single bottleneck, so a slower ride or a second stop is the
   smallest lever, but it must not slow Standard Delivery (the same lift), so it needs a per-contract value or a second
   lift console. Open question: the elevator speed and ride time have not been read yet.
2. Shorten the short route with a real shortcut piece (a second ramp or a jump over the strip into the wing) near the
   barricade exit. This is a small geometry addition and the only change that really shortens the walk.
3. Accept a risk-for-time dilemma without a time gap: both routes take about the same time, the deadline is tight (for
   example 40 s), and the crawlers plus the drone decide how many seconds each route costs. This needs timed runs first.

Do not rely on moving the drone alone: it changes only the danger, not the time.

## Decisions waiting for the user

- Whether finished contracts become repeatable or new contracts come from combining states, rules, and encounters (the
  combination test, GDD 22). The finished campaign still ends at "No contracts are available right now."
- Vehicle ownership for the road tier ([Production-Plan.md](Production-Plan.md), Open Decisions). The road tier stays
  blocked until the MVP loop is fun, which tasks 1 and 2 measure.
- Where music plays: there is no music playback system yet, only ambience cues.
