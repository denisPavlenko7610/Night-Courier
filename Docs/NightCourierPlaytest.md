# Sprint 03 verification log — 2026-11-04

## Scope and evidence

This is an incomplete vertical-slice acceptance pass, not a claim of a finished or fun slice. No keyboard/mouse
playthrough was performed. Existing automation teleports, invokes buttons and presses RD Interactable directly. Root
README.md is absent. Sources inspected: GDD, Production-Plan, Template-Mapping, Template-Packages, Asset-Plan,
Next-Sprint, Playtest-Checklist, contract assets, Build Settings, game scripts and PlayMode fixtures.

The starting worktree already contained staged and unstaged UI, localization, tests and scene changes. Files continued
changing during inspection; those changes are not attributed to this pass. No scenes, GUIDs, save schema, packages or
library sources were changed here. The connected editor reports 7000.0.0a12, not the documented 6000.6. Verification on
the intended shipping editor remains required.

## Five-contract baseline

All five assets already exist under Assets/Game/Content/Contracts and target Warehouse. Common objective: collect in
Depot, depart by van, restore power, scan a physically released parcel, collect it, deliver at the terminal, settle and
return. Normal-state access uses breaker, maintenance door, west/north aisle and freight lift; the alternative uses a
breakable barricade, south floor and ramp. These routes are documented/asset-derived, not walked in this pass.

| Contract          | LocationState | PackageRules           | Encounters                                 | Limit / bonus          | Base pay | Obstacles and tactical difference                                                                                                             |
|-------------------|---------------|------------------------|--------------------------------------------|------------------------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| Standard Delivery | Normal        | None                   | None                                       | None                   | 150      | Breaker, powered door/lift, scanner and scan-locked hatch. Learn delivery without enemies                                                     |
| Fragile           | Normal        | DoNotDrop              | None initially; escalation can add threats | No violations +100     | 220      | Same route; use safe surfaces rather than dropping on floor; four-step escalation                                                             |
| Lights Out        | Blackout      | KeepInLight            | Crawlers                                   | No deadline            | 260      | Park parcel under dock lamp, fetch two-handed battery through barricade, insert generator battery, pull breaker; darkness violates every 10 s |
| Express           | Normal        | DeliveryDeadline       | Crawlers, SecurityDrone                    | 300 s; under 180 s +75 | 320      | Same geometry with threats and time pressure; expiry permits delivery, loses bonus and grades time F                                          |
| Restricted Cargo  | Blackout      | DoNotDrop, KeepInLight | Crawlers, SecurityDrone                    | No violations +100     | 450      | Park on lit crate before battery trip; preserve light and safe support, then scan/deliver                                                     |

Property damage and package-rule violations are distinct. The blackout integration helper deliberately breaks the
barricade and expects a 20-credit property fine. Restricted Cargo's current test disables encounter groups: it proves
rule/settlement plumbing, not a fair route past live enemies.

## Before → after in this pass

| Selected contract | Before                                                                                                        | After                                                                                 | Player decision / route change                |
|-------------------|---------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------|-----------------------------------------------|
| Standard Delivery | Existing introduction                                                                                         | Preserved                                                                             | None                                          |
| Lights Out        | Result recommends a free maintenance route after charging for the barricade, although the door requires power | Property advice states what caused the fine and that the maintenance door needs power | Removes misleading advice; geometry unchanged |
| Express           | Existing 300 s / 180 s thresholds and both threats                                                            | Preserved pending timed traversal                                                     | No demonstrated time/risk dilemma yet         |

Only advice.property in en.json and these documentation additions belong to this pass. Existing UI redesign is not
claimed as new work here.

## Observations and owner playtest checklist

Actual keyboard/mouse outcomes below are NOT RUN unless explicitly marked as a screenshot or automated observation.
Record seconds stalled and the exact prompt during the owner run; never replace the blank timing with a guessed value.

| Step                                        | Visible cue / expected outcome                                                | Actual evidence                                                                     | Delay / confusion                                                                                       | Priority |
|---------------------------------------------|-------------------------------------------------------------------------------|-------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|----------|
| Boot → New Game → Depot → terminal          | One available introduction, accept once, close returns gameplay input         | Existing integration cycle covers callbacks; human NOT RUN                          | Record first-task discovery time                                                                        | P1       |
| Collect → van → Warehouse                   | Parcel held; van available; arrival keeps contract active                     | Integration cycle only                                                              | Record missing pickup/depart prompts                                                                    | P1       |
| Standard: breaker → maintenance door → lift | No-power reasons before breaker; free route opens after power                 | Source/fixtures only; human NOT RUN                                                 | Check reach, collision, lift entry/exit and cargo clearance                                             | P1       |
| Put parcel on scanner → wait → collect      | Release on plate; amber then green after about 3 s; hatch opens               | Existing identity/interruption integration coverage                                 | Human must discover placement; test rotated parcel, player/other object, edge overlap and overhead pass | P1       |
| Lights Out arrival                          | Keep in light; dock lamp is safe; battery needs both hands                    | 1920x1080/09 screenshot inspected: readable objective and lamp notice               | Light boundary has no live remaining-seconds display                                                    | P1       |
| Walk parcel into dark then return           | Ten-second warning; restoration resets exposure                               | Existing light test; 1280x1024/11 inspected: readable notices, no observed clipping | Static warning is not a countdown; measure comprehension                                                | P1       |
| Park parcel → battery → generator → breaker | Parcel remains lit; two hands free; power restored                            | Integration helper breaks barricade first                                           | Mandatory fine conflicts with penalty-free route goal; not fixed                                        | P1       |
| Deliver → result → Depot → menu → Continue  | One payout, causes/deductions visible, credits and unlocks persist            | Existing full-loop integration coverage                                             | Human NOT RUN; use isolated test profile                                                                | P1       |
| Lights Out result                           | 260 base, -20 property, 240 total; zero package violations                    | 1920x1080/15 inspected; false free-route advice reproduced and text corrected       | Updated text screenshot pending                                                                         | P2       |
| Express via each route                      | Safe path slower, danger shortcut meaningfully faster                         | Human/timed traversal NOT RUN                                                       | Prior NavMesh measurements: safe ~57 m + lift; danger ~62 m + hits. Not a valid shortcut                | P1       |
| Express expiry                              | Deliver remains possible; bonus absent; deadline cause and time grade visible | New expiry scene scenario NOT RUN                                                   | Existing rule unit coverage is not scene coverage                                                       | P1       |
| Interaction cancellation                    | Release/hold, change focus, pause, death and scene unload cancel safely       | Callback-based integration is not full InteractionFocus/input coverage              | Human NOT RUN                                                                                           | P1       |
| UI at 1920x1080 and 1280x1024               | Objective, health, cargo, timer readable; covered HUD hidden                  | Three screenshots inspected; timer not visually inspected                           | Inspect all remaining screens and live input, including rebinds                                         | P2       |

Run Standard, then Fragile to preserve the real unlock chain, Lights Out, then Express. Do not unlock Express by state
mutation and call that a human campaign playthrough. Complete each selected contract through result, Depot and Continue.
Also abort once and verify the contract becomes available again. Keep this checklist separate from automated helper
results.

## Automated execution

| Check                                                  | Actual status                                                                                                                                                                                                                                               |
|--------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ContentDataTests                                       | PASS: MCP job c9790996bbc247b99d03fc5b356eb5f9 succeeded; 19 completed of 19, no reported failures; summary payload absent                                                                                                                                  |
| Observed PlayMode baseline                             | FAIL: job 3270c10f47c147f88a7b53d97b0a68b5, 23 total, 21 passed, 2 failed, 0 skipped, 144.47 s. This run also includes RD input tests; do not call all 23 game tests                                                                                        |
| UiLayout failures                                      | 2560x1440 and 2560x1080 failed on unhandled NullReferenceException in Unity.AI.Toolkit.EditorTask.DelayCall / Unity.Relay.Editor.RelayService process-exit callback. No game stack in these failures. Do not suppress the exception to turn the audit green |
| Compilation / Console                                  | Editor finished compilation and tests executed; clean-console acceptance FAIL: unityplastic.dll cannot resolve Mono.Posix. No game C# compilation errors observed                                                                                           |
| MCP execute_code                                       | NOT RUN: default backend reports Operation is not supported on this platform; Roslyn unavailable                                                                                                                                                            |
| Keyboard/mouse route, collision and balance acceptance | NOT RUN                                                                                                                                                                                                                                                     |

The PlayMode run was already active when the request returned tests_running; it was monitored to completion. A later run
request disconnected across reload; inspect current Test Runner before issuing another run. A clean rerun remains
necessary after the text edit.

Repeat in Test Runner: EditMode → NightCourier.Tests.ContentDataTests → Run Selected; PlayMode →
NightCourier.PlayModeTests.GameLoopPlayModeTests, WarehouseRulesPlayModeTests and UiLayoutPlayModeTests → Run Selected.
Export results and retain exact per-fixture counts. Resolve the editor AI Relay failure outside game code, then rerun
failed UI cases. Do not treat an external editor exception as a passing test.

## Saves and acceptance limits

Inspected GameDriver.BootToMainMenu redirects GameHost._saveFolder to Saves.PlayModeTests in sceneLoaded before startup.
Cleanup deletes only that test root and guards against the production Saves path. Failed-save coverage uses
Saves.PlayModeTests.blocked/inner and deletes its blocker file. This pass did not directly read, move, delete or write
production saves, or change persistentDataPath. Human runs must use an isolated test profile: do not click New Game
against a real campaign. Temp/UiAudit is diagnostic output only.

CargoScanner was not changed: no new reproduced geometry false positive justifies replacing AABB logic.
Rotated/edge/overhead scenarios remain unverified. No claim of complete scanner boundary coverage, full hold lifecycle,
fair enemy avoidance, penalty-free blackout, balanced Express or completed vertical-slice acceptance is made.

## Subsequent runs and shared-editor interference

- UI-only job b5b5a90b3b38493cbb416153efe877e2 completed 7/7 with one recorded failure at 2560x1440; summary payload was absent. It is not a clean seven-resolution pass.
- Isolated 2560x1440 job 35425b043227446494e8fc3734487a41: PASS, 1 passed, 0 failed, 0 skipped. Warnings about AudioListener absence and kinematic velocity remain.
- Explicit GameLoop/WarehouseRules rerun 062a62d46b3a4362ae2faa9925185fdd: FAIL/incomplete. 10 of 11 completed, one recorded failure in LightsOut_ParkedPackageStaysLit_TheDarkIsAnnounced_AndEveryBlockedObjectExplainsItself (unhandled NullReferenceException). MCP reports "Job cleared manually (stuck or orphaned)" while the scanner test was current; no final pass count exists. This agent did not request clearing that job.
- Repeated tests_running responses, unsolicited runs and ongoing file changes indicate shared-editor interference. Do not mutate live scene geometry or infer a stable post-change acceptance baseline from these overlapping runs. Repeat the named fixtures with exclusive editor control.
- JSON parsing and git diff --check succeeded after the text change. No runtime scripts were changed. Screenshot inspection of the updated advice remains pending.

## v0.4 human acceptance gate

The proposed supplement is recorded in GDD section 0.8. No new Unity run or human playthrough accompanied this documentation update. Previous automatic results remain historical evidence, including their failures and interrupted runs.

| Acceptance item | Current status | Evidence required to close |
|---|---|---|
| Start and understand the first goal without programmer coaching | NOT VERIFIED | Fresh player, keyboard/mouse, discovery time and stalls recorded |
| Complete Standard, Lights Out and Express using different tactics | NOT VERIFIED | Three full human runs, real unlocks, recorded decisions and elapsed times |
| Understand cargo rules and light safety | NOT VERIFIED | Player intentionally parks cargo safely and explains darkness feedback |
| Avoid penalties described as optional | FAIL for current documented blackout path | Reachable battery access without mandatory barricade damage; walk and verify it |
| Choose a faster dangerous route or slower safe bypass | NOT VERIFIED; known geometry blocker | Route diagram, traversal of both branches and measured times at the same carry state |
| Understand payout, deductions, expiry and bonus | NOT VERIFIED | Human result review; separately exercise overdue Express |
| Return to Depot with saved progress restored | NOT VERIFIED by human | Result ? Depot ? Main Menu ? Continue in isolated test profile; compare credits/unlocks |

Keep this table open until actual runs supply evidence. A design proposal does not turn an existing implementation into a verified feature.

## Blackout battery change after implementation authorization

This update supersedes the earlier unchanged-geometry statement and mandatory-fine baseline; those observations describe the starting layout. The entire Sprint 03 acceptance gate remains open.

| File | Change and purpose |
|---|---|
| Assets/Game/Scenes/Warehouse.unity | Existing Battery root moved from (-5, 0.4, -9.5) to (-14, 0.4, -5.5), inside the dock. No GUID, component or reference replacement; no walls added. Removes the intended dependency on destroying the barricade before restoring power |
| Assets/Game/Content/Localization/en.json | Existing objective.find_battery now names the far end of the dock; no new UI element |
| Assets/Game/Tests/PlayMode/GameDriver.cs | PowerUpTheBlackout no longer damages the barricade. Capsule casts check the dock aisle approach against Default-layer solid geometry before the existing scripted pickup. These casts are a partial clearance check, not player input or a complete movement simulation |
| Assets/Game/Tests/PlayMode/WarehouseRulesPlayModeTests.cs | Lights Out expects zero violations before scanning. Both blackout results reject a negative property-damage receipt line; Restricted Cargo still expects its clean-run bonus |
| Docs/GDD.md and Docs/Playtest-Checklist.md | Current design and human blackout steps now describe the dock battery and no mandatory fine |

Before: the battery trip deliberately incurred a 20-credit property fine. Intended after: leave cargo under the lamp, walk the dock aisle to the battery, insert it, pull the breaker, use maintenance access without damaging property. Both hands and safe cargo parking remain necessary. Standard and Express assets, timers, rewards, threats and geometry are unchanged. Restricted Cargo's automated test still disables threats and cannot prove enemy avoidance.

An editor hierarchy query and top-down render inspected the original layout before editing. Diagnostic image: Temp/UiAudit/warehouse_route_baseline.png. Dock entrance/player (-17, 0.05, -13), parcel (-15.6, 1, -11.9), safe lamp (-15.6, 2.6, -13.8), generator (-19.45, 0, -10.5), breaker (-19.7, 0, -7), maintenance door (-18, 0, -3.5), barricade (-11.5, 1.5, -8), scanner (15.8, 1.5, 11), delivery terminal (19.45, 2.4, 11). The electric strip occupies the south floor around (0.75, 0.02, -6). These are inspected transforms, not a human route trace.

Post-change compilation, Console inspection and PlayMode tests: NOT RUN / NOT VERIFIED. MCP scene reload and run_tests requests disconnected across reload, followed repeatedly by "ping not answered". The desktop helper also failed twice with "native pipe ... not found". No post-change test job id or result was returned. Earlier passing tests do not validate this change.

Required rerun after reconnect: ContentDataTests; WarehouseRulesPlayModeTests (all six); GameLoopPlayModeTests; UiLayoutPlayModeTests (all seven resolutions because objective text changed). Then perform checklist Run B with real controls: park cargo under lamp, walk around dock crates to battery, carry it to generator, restore power, walk maintenance route, scan/deliver, verify 260 base and no property deduction, return and Continue. For Restricted Cargo verify 450 +100 before optional costs and no mandatory damage. Inspect the new objective at 16:9 and 5:4. If capsule clearance fails, fix actual scene composition instead of removing the assertion.