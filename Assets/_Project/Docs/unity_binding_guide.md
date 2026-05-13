# Unity Binding Guide (PICO PingPong Demo)

## Build the demo objects
1. Open your target scene.
2. Run `Tools/PICO ElderCare/Build PingPong Demo Scene`.
3. The tool creates/updates Environment, PingPong, Managers, and UI roots.
4. The tool first looks for cleaned VRTableTennis prefabs under:
   - `Assets/_Project/External/VRTableTennis/Adapted`
5. If adapted assets are missing or incompatible, the tool falls back to local primitive prefab+material assets under:
   - `Assets/_Project/Prefabs/PingPong`
   - `Assets/_Project/Materials/PingPong`

## VRTableTennis assets
- Original copied assets are kept under `Assets/_Project/External/VRTableTennis/Original`.
- Cleaned usable prefabs are kept under `Assets/_Project/External/VRTableTennis/Adapted`.
- You can rebuild the adapted prefabs from the copied original assets with `Tools/PICO ElderCare/Build VRTableTennis Adapted Assets` after Unity imports the FBX and audio files successfully.
- The current adapted prefabs include table, net, paddle, and ball only. They do not include old VRTableTennis scenes, Oculus, SteamVR, Photon, or legacy controller scripts.

## Replace paddle and ball models
- Put Unity Hub / Asset Store replacement models under `Assets/_Project/External/CustomPingPong/Models`.
- Paddle model names checked first: `PingPongPaddle.prefab`, `PingPongPaddle.fbx`, `Paddle.prefab`, `Paddle.fbx`, `Racket.prefab`, `Racket.fbx`.
- Ball model names checked first: `PingPongBall.prefab`, `PingPongBall.fbx`, `Ball.prefab`, `Ball.fbx`.
- Run `Tools/PICO ElderCare/Build VRTableTennis Adapted Assets` after importing or renaming the models.
- Then run `Tools/PICO ElderCare/Build PingPong Demo Scene`.
- The tool uses the replacement model as a visual child only. Project-owned physics and gameplay components stay on the adapted prefab root.

## What is auto-created
- Environment: Floor (if no obvious floor/ground exists), Directional Light (if none). Legacy `BackWall` objects are hidden so they do not render as an extra white divider.
- PingPong: Table, Net, Paddle_Right, BallSpawnPoint, BallTargetPoint, BallContainer.
- Managers: BallSpawner, ScoreManager, HitFeedbackManager.
- UI: World Space Canvas with Hit/Served/Accuracy TMP labels.
- Hit feedback uses `Original/Audio/single_bounce.mp3` when available.

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
- Adapted assets not showing: confirm the four prefabs exist in `External/VRTableTennis/Adapted`; otherwise the builder will use fallback primitives.
- Ball too fast: lower `serveSpeed`, `forwardBoost`, or `maxSpeed`.
- Hard to hit: move spawn/target points closer to paddle zone.
- Ball tunneling: keep paddle rigidbody Continuous Speculative and ball Continuous Dynamic.
- Score not changing: ensure `ScoreManager` exists and scripts compile without errors.
- UI not visible: move world-space canvas closer/in front of camera and increase scale.
