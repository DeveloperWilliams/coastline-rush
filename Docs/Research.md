# Research and sources

Reviewed 2026-10-14. These sources informed design choices; no third-party
controller code was copied into this project.

- Unity 6.3: [Create a car with Wheel colliders](https://docs.unity.com/en-us/engine/6000.3/manual/physics-section/physics-overview/collision-section/collider-shapes/wheel-colliders/wheel-collider-tutorial).
  Rigidbody setup, wheel radius, suspension positions, motor/brake torque and
  GetWorldPose for wheel animation.
- Unity: [WheelCollider API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/WheelCollider.html).
  Slip-based friction, vehicle substeps, contact queries and material-specific grip.
- GDC 2019, Hamish Young: [Vehicle Physics and Tire Dynamics in Just Cause 4](https://gdcvault.com/play/1026468/Vehicle-Physics-and-Tire-Dynamics).
  The talk description discusses balancing believability and fun and choosing a
  designer-friendly tire model. This supports keeping an approachable parameter
  set rather than claiming a full simulation.
- GDC, Matthew Harris: [Vehicle Feel Masterclass](https://gdcvault.com/play/1025295/).
  The session description emphasizes camera behavior and handling assists as part
  of vehicle feel. Camera lag, speed FOV, brake feedback and optional steering
  assistance were considered together with the physical controller.
- [Arcade Car Physics](https://github.com/Saarg/Arcade_Car_Physics), MIT.
  Reviewed the project overview as an example of a configurable WheelCollider
  package. It was not imported; Coastline's controller is original code.
- Unity: [Shader.Find and build inclusion](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Shader.Find.html).
  Runtime material shaders need explicit build references. Standard surface
  material and the original unlit/sky shaders are retained in Resources.
- Kenney [asset support/licensing](https://kenney.nl/support).
  CC0 asset redistribution and the importance of preserving the included license.

Downloaded assets, versions, source links and original license files are listed
in [ThirdPartyNotices.md](../ThirdPartyNotices.md).
