# Validation evidence

Reports in this folder come from the actual Unity 6000.3.25f1 editor, using the
physical WheelCollider vehicles. The driver does not teleport around the track.

| Scenario | Track | Surface | Coverage |
|---|---|---|---|
| `palm-coast-dry.json` | Palm Coast | Dry | Resource loading, suspension, acceleration, braking, steering, nitro, solid collisions, pause, restart and a three-lap race |
| `highland-loop-wet.json` | Highland Loop | Wet | The same physical sequence on the second circuit and lower-grip weather preset |

Each report records the measured speeds, steering response, remaining boost,
completed laps, recovery count and individual checks. Values are smoke-test
measurements on this Mac, rather than a guarantee of identical results on other
hardware. The sixteen track/race-rule checks also passed in the Unity editor.

The full-race test temporarily gives the player car the same AI controller as
the rivals and restores the saved garage settings afterward. It does not save
best laps. Screenshots in `../Screenshots` are captures of the real game.

Run both validation presets after handling, track or race-rule changes. The
repository GitHub workflow checks checkout integrity and licenses; running
Unity physics in hosted CI still requires a configured Unity license.
