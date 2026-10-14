# Native playtest — 0.3.0

The macOS application was launched and tested through native keyboard input,
in addition to the Unity editor's full physics/race checks.

Verified in the rendered application:
- Enter starts a race; W accelerates the player car.
- Steering input responds; S brakes and subsequently engages reverse.
- C changes the driving camera; Escape opens pause.
- F11 changes the fullscreen application into a 1600 × 900 window.
- Garage Left/Right changes the selected model and its profile description.
- Up/Down changes circuit; M changes event; V changes weather; L changes lap count.
- A Club Sport / Highland Loop / wet / one-lap solo configuration renders correctly.
- The native solo event reached results: 68.97 seconds total, 67.08 seconds clean lap, zero penalty.
- Final polish hides the driving guide in the garage, uses singular lap labels and renders button captions independently from native button styles.
- G returns from results to the garage; the original Nova R / Palm Coast / dry / three-lap circuit configuration was restored.

The input tool supplies short key presses. These were input and UI smoke checks,
not a claim of a complete human-controlled race or a handling-quality sign-off.
The repeatable physical regression races were driven by the shared AI controller in the Unity checks,
including every sector, final classification and record eligibility.

The final button-caption change was verified with Unity ScreenCapture in
`../Screenshots/garage.png`. Desktop capture service startup failures prevented
a reliable final native screenshot; the final native build includes the caption
change and started without runtime exceptions.
