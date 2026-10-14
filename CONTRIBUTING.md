# Contributing

Use Unity 6000.3.25f1. Commit `Assets`, every asset's `.meta` file, `Packages` and
`ProjectSettings`. Do not commit `Library`, `Temp`, IDE files, build output or logs.

Before proposing a change:
1. Open the project and use **Coastline Rush > Play Race**.
2. Run the rules checks and all three physical presets under **Coastline Rush > Validation**.
3. Playtest the affected feature, including pause, restart and results.
4. Build the macOS player if the change affects runtime or resource loading.
5. Include validation results and relevant screenshots in the pull request.

Keep physics, AI, race rules and presentation separate. Use SI units. Expose
handling changes through `VehicleTuning` and explain their gameplay effect.
Do not update third-party assets without recording source/version/license changes.
Avoid regenerating existing `.meta` files: their GUIDs are scene references.

See [Design.md](Docs/Design.md) for responsibilities and upcoming work.

With the editor open, `python3 scripts/editor-command.py <command>` queues a
fixed project operation such as `rules`, `validate`, `validate-wet`, `validate-nova`
or `build`. Requests are written atomically; completion is recorded in
`Logs/coastline-command-result.txt`.
