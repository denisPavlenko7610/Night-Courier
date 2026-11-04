# Night Courier: Manual Playtest Checklist

Status: **no human has completed this checklist yet.** Every result below the "Record" headings is blank on purpose. Nothing in
this file may be reported as a confirmed playtest until a person fills it in with the keyboard and mouse (or a gamepad)
and no teleports, console, or debug calls.

## What the automated tests do and do not prove

| Check | Automated (EditMode and PlayMode suites) | Needs a person |
|---|---|---|
| Loop, payout once, Continue, abort, death, destroyed cargo, save failure | Yes: `GameLoopPlayModeTests`, `WarehouseRulesPlayModeTests` | No |
| The scanner ignores a stray object, ignores a held package, aborts when the package leaves | Yes: a real `Cargo`-layer cube and the real package on the real plate | No |
| A parked package stays lit in a blackout, the dark is announced once, blocked objects carry a reason, the barricade prompt appears when looked at | Yes (state and prompt text; the camera is turned by setting the player rotation) | Whether the text is read in time |
| The route is walkable, doors and the lift behave, interaction rays reach their targets | **No.** `GameDriver` teleports the player next to each target, calls `Interactable.Press` directly, and reads private fields by reflection | Yes |
| Whether the hints are understood, how long a run takes, where a player stalls | **No** | Yes |
| Input from a real keyboard, mouse, or gamepad, and a pause menu opened mid-action | **No** | Yes |

Static measurements only (not a playthrough): the NavMesh walking distance from the start to the scanner area is about 57 m
by the maintenance route and about 62 m by the barricade route (see "Express" in [Next-Sprint.md](Next-Sprint.md)).

## Setup

1. Open the `Boot` scene and enter Play Mode (or run a build). Start with **New Game**; if a save exists, confirm the popup.
2. Keys: move WASD, look mouse, interact E (hold where the prompt says Hold), drop G, fire left mouse, switch weapon Q / 1 / 2,
   medkit H, pause Esc. Gamepad: A interact, B drop, Start pause.
3. Keep a stopwatch. Do not give the tester any hint. Note the commit hash, the date, and the input device.
4. Test PlayMode runs use the folder `Saves.PlayModeTests`; your real `Saves` folder is never touched by them.

## Run A: Standard Delivery (the introduction, one honest trip)

Expected route (about 55 m on foot): Depot terminal → package on the conveyor → van → Warehouse dock → package on the crate →
breaker panel on the west wall → maintenance door → west aisle → north aisle → freight lift console → lift up → scanner plate →
scan → hatch → delivery terminal.

| # | Step | What the player should see | Record: minutes | Record: confusion |
|---|---|---|---|---|
| A1 | Depot: open the terminal (E), read the briefing, accept (once) | The terminal shows the work order, dispatch notes, risk estimate, base pay, site, and `Package rules: None`; the objective says to pick up the parcel | | |
| A2 | Take the package, load the van (E) | The objective changes to "Carry the parcel to the van and load it." | | |
| A3 | Warehouse: pick up the package | "Switch on the breaker panel. The scanner, lift and door run on it." | | |
| A4 | Look at the maintenance door before the power is on | The prompt reads "No power. Switch on the breaker panel." | | |
| A5 | Pull the breaker lever | The door opens; the objective says to set the package on the scanner plate | | |
| A6 | Walk to the lift; look at the console before and after the power | No-power text before; "Ride the freight lift" after | | |
| A7 | Set the package on the plate (G) | Indicator turns amber, "Scanning. Leave the package on the plate...", green after about 3 s, "Scan complete..." | | |
| A8 | Pick it up, pass the hatch, hold E at the terminal | One result panel: Base pay, total, rating, rank and XP, one improvement line | | |
| A9 | Return to the Depot (click once), then Main Menu → Continue | Credits 150, the contract no longer offered | | |

Record for the whole run: total time to the first delivery ___; every place the tester stood still for more than 20 s ___;
number of false violations (a violation the tester did not understand) ___; moments with no known goal ___; console errors ___.

## Run B: Lights Out (the dark shift)

Unlocks after Standard Delivery. The point is that the tester discovers the work lamp, the battery, and the barricade
without help.

| # | Step | What the player should see | Record: minutes | Record: confusion |
|---|---|---|---|---|
| B1 | Arrive in the blackout dock | The hint says the rule and that the work lamp is the safe spot; the package sits lit on the crate | | |
| B2 | Pick the package up and walk out of the lamp pool | "Cargo in darkness. Find light within 10 seconds." once | | |
| B3 | Try the breaker panel, then the generator, without a battery | The prompts say the panel is dead and the generator needs a battery | | |
| B4 | Find the way out | Find the battery at the far end of the dock. The barricade is optional and its prompt warns of a fine. | | |
| B5 | With the package in hand, try to grab the battery | "Your hands are full." and the objective says to set the package down on the lit crate | | |
| B6 | Leave the barricade intact, fetch the dock battery (both hands), insert it, pull the lever | Lights come on, doors unlock, no property fine appears on the result | | |
| B7 | Finish the delivery | Result lists Base pay and the ₡20 property fine | | |

Record: did the tester park the package under the lamp on purpose? ___; violations ___; minutes ___; where the tester
did not know what to do ___.

## Run C: UI readability on your own screen

Resize the window to the smallest size you would play in and to your full screen. For each screen below, nothing may overlap
or be cut off, and every line must be readable at arm's length: main menu and settings, the Depot (HUD, contract terminal
with one and with several contracts, upgrade terminal, equipment locker), the Warehouse HUD (rules, integrity, objective,
a prompt, three notices at once), the pause menu with Settings and Controls, the result and failure panels, the death
overlay. Record the window size and anything that overlaps. The automated check (`UiLayoutPlayModeTests`) covers seven
sizes but cannot judge contrast or taste.

## Negative scenarios (each once, by hand)

| # | Scenario | Expected |
|---|---|---|
| N1 | Hold E at the delivery terminal before the scan | A notice that the package has not been scanned; no result |
| N2 | Pick the package off the plate mid-scan | The indicator resets and the notice says the scan was interrupted |
| N3 | Put the package on the plate with the power off | The objective says to restore power, not "Pick up the package" |
| N4 | Hold E at the terminal, press Esc during the settlement | No abort during the settlement; one result panel |
| N5 | Walk onto the electric strip until death | Respawn at the last locker; the result shows the ₡25 fee |
| N6 | Drop the package off the plate on Fragile four times | Notices name "Do not drop", escalation, then the failure panel with no payout; the retry pays once |
| N7 | Abort from the pause menu; press Esc again in the popup | The popup closes first; the contract is offered again after the Depot loads |
| N8 | Make the save folder unwritable, then finish a delivery | The result shows "Warning: your progress could not be saved."; credits are still shown; the Depot loads |
| N9 | Double-click Accept, double-press Deliver, double-click Return to Depot | One contract, one payout, one Depot |

## Pass criteria for one honest trip

- Run A finishes without help and without the console.
- No false violations in Run A; at most one understood violation in Run B.
- No stall longer than 20 s without a visible goal (objective line or prompt).
- The tester can say, unprompted, what the rule was, why the power was needed, and what the scanner did.

## Session log

| Date | Tester | Commit | Device | Run A min | Run B min | False violations | Worst stall | Verdict |
|---|---|---|---|---|---|---|---|---|
| | | | | | | | | |
