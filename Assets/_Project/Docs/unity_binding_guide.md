# Unity Binding Guide (PICO PingPong Demo)

## Build the demo objects
1. Open your target scene.
2. Run `Tools/PICO ElderCare/Build PingPong Demo Scene`.
3. The tool creates/updates Environment, PingPong, Managers, and UI roots.
4. The tool also creates/updates fallback prefab+material assets under:
   - `Assets/_Project/Prefabs/PingPong`
   - `Assets/_Project/Materials/PingPong`

## What is auto-created
- Environment: Floor (if no obvious floor/ground exists), Directional Light (if none), BackWall.
- PingPong: Table, Net, Paddle_Right, BallSpawnPoint, BallTargetPoint, BallContainer.
- Managers: BallSpawner, ScoreManager, HitFeedbackManager.
- UI: World Space Canvas with Hit/Served/Accuracy TMP labels.

## If right-hand controller is not auto-bound
- Select `PingPong/Paddle_Right`.
- In `PaddleFollower`, drag XR Origin's RightHand/Right Controller transform into `controllerTransform`.

## Tuning
- Ball speed: `Managers/BallSpawner -> serveSpeed`.
- Serve interval: `Managers/BallSpawner -> serveInterval`.
- Paddle size: adjust `PingPong/Paddle_Right` transform scale.
- Paddle return strength: `PingPongBall` (`paddleVelocityMultiplier`, `forwardBoost`, `upwardBoost`, `maxSpeed`).

## Build and Run to PICO
1. Keep existing PICO XR settings unchanged.
2. Add/open your demo scene in Build Settings.
3. Build And Run to connected PICO 4 Ultra.

## FAQ
- Paddle not following: assign `controllerTransform` manually.
- Ball too fast: lower `serveSpeed`, `forwardBoost`, or `maxSpeed`.
- Hard to hit: move spawn/target points closer to paddle zone.
- Ball tunneling: keep paddle rigidbody Continuous Speculative and ball Continuous Dynamic.
- Score not changing: ensure `ScoreManager` exists and scripts compile without errors.
- UI not visible: move world-space canvas closer/in front of camera and increase scale.
