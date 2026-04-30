# VRTableTennis Reuse Notes

## Reuse / migration status
- Minimal local migration has been applied for ball, paddle, table, net, materials, required texture, and hit/ambience audio.
- Original selected files are stored under `Assets/_Project/External/VRTableTennis/Original`.
- Cleaned usable prefabs are stored under `Assets/_Project/External/VRTableTennis/Adapted`.
- Full scenes and legacy XR/Oculus/SteamVR/Photon scripts were not migrated.

## What is migrated now
- `Original/Models/PPPaddle.fbx`
- `Original/Models/PingPondTable.fbx`
- `Original/Models/Ball.fbx`
- `Original/Materials/PingPongTable*.mat`
- `Original/Materials/Metal.mat`
- `Original/Materials/seamless-wood-texture-free-6.*`
- `Original/Audio/single_bounce.mp3`
- `Original/Audio/ping_pong_whoosh.mp3`
- `Original/Audio/cheering.mp3`
- `Adapted/PingPongPaddle_Adapted.prefab`
- `Adapted/PingPongTable_Adapted.prefab`
- `Adapted/PingPongNet_Adapted.prefab`
- `Adapted/PingPongBall_Adapted.prefab`

## Adapted prefab cleanup
- Adapted prefabs keep MeshRenderer, MeshFilter, Material references, Collider, and Rigidbody where useful.
- Old VRTableTennis gameplay/controller scripts are not attached.
- Paddle uses the current project `PaddleFollower` and `PaddleVelocityTracker`.
- Ball uses the current project `PingPongBall` and `BallLifetime`.
- No Missing Script placeholders are intentionally present.

## What is not migrated yet
- Full `.unity` scenes.
- `Assets/Oculus` and other old XR integration folders from the reference project.
- Network/multiplayer or Photon-related code.
- Legacy scripts such as paddle controller, ball holding, or scene-specific gameplay managers from the reference project.

## Why not directly use full VRTableTennis scene
- Current project already contains working PICO/XR setup and scene bootstrap.
- Pulling in a full foreign scene risks breaking controller bindings, camera rig assumptions, or project-level settings.
- This demo therefore composes objects into current scene via Editor tool, keeping existing XR Origin and Main Camera intact.

## Current PICO demo resource usage
- `PingPongDemoSceneBuilder` now checks adapted VRTableTennis prefabs first:
  - `Assets/_Project/External/VRTableTennis/Adapted/PingPongTable_Adapted.prefab`
  - `Assets/_Project/External/VRTableTennis/Adapted/PingPongNet_Adapted.prefab`
  - `Assets/_Project/External/VRTableTennis/Adapted/PingPongPaddle_Adapted.prefab`
  - `Assets/_Project/External/VRTableTennis/Adapted/PingPongBall_Adapted.prefab`
- If any adapted prefab is unavailable, the existing primitive fallback path remains active:
  - `Assets/_Project/Prefabs/PingPong`
  - `Assets/_Project/Materials/PingPong`
- Hit feedback binds `Original/Audio/single_bounce.mp3` when it is present.

## Validation notes
- No `ProjectSettings` files were intentionally changed.
- No `.unity` scene file was edited directly.
- Unity batch generation was attempted, but this local project reports a Unity editor version mismatch and package compile issues in sample XR files before the editor method can complete. The adapted prefabs were therefore created as explicit prefab assets using the imported asset GUIDs.
- Open the project in the matching Unity editor and let it import once before using the build menu.
