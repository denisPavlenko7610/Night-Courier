# Night Courier: Next Steps

This page is an index, not a second backlog or verification log.

- [Next-Sprint.md](Next-Sprint.md) owns the ordered work, acceptance criteria and open decisions.
- [NightCourierPlaytest.md](NightCourierPlaytest.md) owns dated execution evidence, failures and limitations. Test totals describe individual recorded runs, not a permanent project status.
- [Playtest-Checklist.md](Playtest-Checklist.md) owns ordinary-input and first-time-player acceptance.
- [GDD.md](GDD.md) owns design and feature tiers; [Production-Plan.md](Production-Plan.md) owns milestone constraints.
- [Template-Mapping.md](Template-Mapping.md) owns mechanic integration status. HoldInteraction is used for delivery and ConveyorBelt for the Depot conveyor; remaining uses follow demonstrated content needs.

## Verification boundaries

The PlayMode fixtures load real scenes but mostly arrange state, teleport the player and call interactions directly. A synthetic-keyboard regression additionally verifies forward movement, terminal focus, interaction and UI-map cancellation. A delivery-hold regression checks release, looking away, pause cancellation and a single payout. These checks do not replace a human route traversal or establish that hints and encounters are understandable.

Test saves use GameHost._saveFolder = Saves.PlayModeTests. Tests never move, back up or delete the real Saves folder. Input focus settings changed by the synthetic-keyboard test are restored afterwards.

UI-layout geometry checks and screenshots are separate from input/gameplay acceptance. Consult the dated log before claiming a clean audit: editor-package exceptions can fail an audit even when game text geometry passes.
