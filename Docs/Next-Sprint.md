# Night Courier: Next Sprint

Open work only, in order. Finished work is not repeated here (the last verification pass is recorded in
[GDD.md](GDD.md) section 0.6). Verify each task in Unity and record the method and limitations. External playtesters and
human acceptance are not required.

Sizes: S under half a day, M about a day.

| # | Task                         | Size | Acceptance                                                                                                                                                                                                                                                                                                                                                   |
|---|------------------------------|------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | Show the scan result in view | S    | The scan light (`CargoScanner` indicator, high on the north wall) is not in view while the player stands at the plate, and the yellow frame looks the same while scanning and after. Make the scanning and done states readable from the approach, then capture the same views again (`RealInputPlayModeTests.StandardDelivery_ScannerAndDeliveryCues_AreCapturedFromTheApproach`, output in `Temp/CueShots`) |
| 2 | Play the drone's death       | S    | `Drone.controller` has Idle, Hit and Attack only, so the `Die` trigger plays nothing before `EnemyDeath` shrinks the model. Add a state or leave it and say so in the mapping. Optional                                                                                                                                                                         |

## Express: decided and measured, 2026-11-09

The user chose the tight clock (data only, `Contract04_Express.asset`): a 45 s limit (was 300 s) and an on-time bonus
window of 22 s (was 180 s). The route hint and the overdue advice no longer claim the short route is faster
(`rule.delivery_deadline.hint`, `rule.delivery_deadline.advice`, `advice.overdue`).

Why not geometry: timed with synthetic keyboard and mouse events (`InputBot`), sprinting, with the package in hand, from
the dock spawn to the scanner approach point, the safe route (breaker panel, maintenance door, central aisle, freight
lift) took 10.5 s and the short route (barricade, south floor, ramp) 10.8 s, identical across runs. The NavMesh legs are
6.6 m spawn to panel, 2.9 m panel to door, 36.9 m door to lift (through the central aisle), 6.3 m panel to barricade and
42.7 m barricade to scanner approach, and a perfectly straight line from the barricade to the foot of the ramp saves only
about 5 m, under 11% of the trip.

Whole deliveries from the dock spawn to the opened result panel (`RealInputPlayModeTests`, the Security Drone and both
crawlers active, the lift screen below in place), bot time plus 0.5 s before the clock starts:

| Run                                       | Time   | Bonus (window 22 s)       | Health at the end |
|-------------------------------------------|--------|---------------------------|-------------------|
| Safe route, sprinting                     | 17.6 s | paid (asserted)           | 100               |
| Short route, sprinting                    | 18.0 s | paid (asserted)           | 80                |
| Short route, after shooting both crawlers | 25.7 s | lost (asserted)           | 70                |
| Safe route, walking                       | 22.6 s | at the edge, not asserted | 90                |

The walking run stays inside the 45 s limit (asserted). The bot is a faster and cleaner runner than a person, so the
bonus window leaves a person about a quarter of slack on a clean sprint; re-tune from real play if it proves too tight.
The routes still cost the same time; the choice is danger: the safe route has the drone, the short route costs a ₡20
fine, a lost no-violations bonus and crawler hits.

## Cover from the drone: the lift screen, 2026-11-09

Before it, the drone patrolled the north aisle to its east end (10, 11.5), stood off 6 m from a courier and fired
through the opening beside the lift, which looks straight down the delivery room (z about 11): the plate, the hatch and
the terminal. Sprinting bots arrived at the scanner with 20 to 30 health, and one died there. A line-of-sight probe from
54 sampled drone positions saw the lift platform, the plate and the hatch from every one of them.

The fix is geometry: `Collision_LiftScreen` and four `Wall_LiftScreen` pieces (copies of the existing partition
pieces, no new assets) form a 3.4 m by 3 m wall at x 10.4, z 9.3 to 12.7, west of the lift. The lift is entered from its
south or north side; the drone's last patrol point moved from x 10 to 8.4. The same probe now sees the lift, the plate
and the terminal stand from 1 to 6 of the 54 positions (the corners beside the screen). The NavMesh was rebaked
(`NavMesh-Warehouse.asset`). With the drone active, a walking delivery ends with 90 health (it was 20) and a sprinting
one with 100. The drone still shoots a courier who walks the aisle or boards the lift from the side, and it chases to the
delivery floor by the ramp if it loses the courier, which takes it well over 20 s.

## Decisions waiting for the user

- Whether finished contracts become repeatable or new contracts come from combining states, rules, and encounters (the
  combination test, GDD 22). The finished campaign still ends at "No contracts are available right now."
- Vehicle ownership for the road tier ([Production-Plan.md](Production-Plan.md), Open Decisions). The road tier stays
  blocked until the MVP loop is fun; the tasks above verify behavior and route timing, not enjoyment.
- Where music plays: there is no music playback system yet, only ambience cues.