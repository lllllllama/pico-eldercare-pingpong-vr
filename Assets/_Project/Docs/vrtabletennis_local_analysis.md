# VRTableTennis 本地资源分析与迁移计划

生成时间：2026-04-30  
当前 PICO 项目：`D:\test_projects\VR\pico-eldercare-pingpong-vr`  
用户指定参考路径：`D:\PicoCodexWork\ReferenceProjects\VRTableTennis`

> 注意：用户指定的本地参考路径不存在。为完成分析，已将公开仓库 `kushal-goenka/VRTableTennis` 浅克隆到 `D:\tmp\codex_ref\VRTableTennis` 作为只读分析源。没有把完整项目复制进当前项目 `Assets`，也没有修改 `ProjectSettings`、`.unity` 场景或任何 C# 代码。

## VRTableTennis 项目结构概览

参考项目根目录包含完整 Unity 工程结构：

- `Assets/BarcadeGamesAssetPack/`
  - 主要可复用美术资源来源。
  - `Models/FBX/PPPaddle.fbx`：乒乓球拍模型。
  - `Models/FBX/PingPondTable.fbx`：乒乓球台模型，注意文件名是 `PingPondTable`，不是 `PingPongTable`。
  - `Models/FBX/Ball.fbx`：球模型，但从材质名 `White/Color/Number/InnerRing/Black` 判断更像台球/通用球，不是乒乓球专用。
  - `Materials/PingPongTable*.mat`：球台相关旧内置管线材质。
- `Assets/Scenes/`
  - `game__room.unity`：主要游戏房间场景，包含球台、球拍、球、音效、比分逻辑、Oculus 控制逻辑。
  - `gameroom.unity`：另一个房间场景版本。
  - `start_screen.unity`：开始界面，不适合迁移到 PICO Demo。
- `Assets/Scripts/`
  - `gameplay.cs`、`ballbounce.cs`、`playerside.cs`、`quit.cs`。
  - 这些脚本大量使用 `OVRInput` 或旧 UI/场景对象名绑定。
- `Assets/Resources/`
  - `PaddleControl.cs`、`BallHolding.cs`、`DontGoThroughThings.cs`、`Cheering.mp3`。
  - `PaddleControl.cs`、`BallHolding.cs` 依赖 Oculus `OVRInput`。
- `Assets/Oculus/`
  - 完整 Oculus SDK、SampleFramework、Avatar、Platform、Spatializer 等内容。
  - 按当前硬性限制，整体不迁移。
- 顶层音频：
  - `Assets/single_bounce.mp3`
  - `Assets/ping_pong_whoosh.mp3`

参考项目 `Packages/manifest.json` 使用旧 Unity 包，例如 `com.unity.package-manager-ui`、`com.unity.ads`、`com.unity.analytics`、TextMeshPro 1.2.4。当前 PICO 项目使用 `com.unity.xr.picoxr` 和 XR Interaction Toolkit Sample 资源，不应引入参考项目包配置。

## 可迁移资源清单

| 类别 | 参考文件 | 直接复制 | Missing Script 风险 | 旧 VR/Oculus/SteamVR 依赖 | 适合迁移到 PICO | 建议 |
| --- | --- | --- | --- | --- | --- | --- |
| 球拍模型 | `Assets/BarcadeGamesAssetPack/Models/FBX/PPPaddle.fbx` + `.meta` | 可以 | 无，FBX 本身无脚本 | 无 | 适合 | 复制到 `Assets/_Project/Models/PingPong/PPPaddle.fbx`，基于它新建 PICO 适配 prefab。 |
| 球台/球网模型 | `Assets/BarcadeGamesAssetPack/Models/FBX/PingPondTable.fbx` + `.meta` | 可以 | 无，FBX 本身无脚本 | 无 | 适合 | 球台和网在同一 FBX 中，优先迁移为 `PingPongTable_Adapted.prefab`。 |
| 球模型 | `Assets/BarcadeGamesAssetPack/Models/FBX/Ball.fbx` + `.meta` | 可复制但不推荐直接用作乒乓球 | 无，FBX 本身无脚本 | 无 | 一般 | 更像台球资产。当前项目的 Sphere 球更适合保留，只替换材质、音效和 prefab 结构。 |
| 球台材质 | `Assets/BarcadeGamesAssetPack/Materials/PingPongTable.mat`、`PingPongTable 1.mat`、`PingPongTable 2.mat`、`PingPongTable 3.mat` | 可以 | 无 | 无 | 需要适配 | 源材质使用 Built-in Standard shader；当前项目可能是 URP，应在 `Adapted` 或 `Materials/PingPong` 下重建 URP/Lit 材质。 |
| 其他材质 | `Metal.mat`、`seamless-wood-texture-free-*.mat`、`Felt.mat` | 可选 | 无 | 无 | 部分适合 | 只迁移球台/球拍实际需要的材质，避免把整个 asset pack 材质库搬入。 |
| 纹理 | `seamless-wood-texture-free-6.jpg`、`Felt.jpg` 等 | 可选 | 无 | 无 | 低优先级 | 乒乓球 Demo 的核心模型基本靠纯色材质即可，纹理不是最小迁移必需项。 |
| 反弹音效 | `Assets/single_bounce.mp3` | 可以 | 无 | 无 | 适合 | 迁移到 `Assets/_Project/Audio/PingPong/single_bounce.mp3`，用于球撞桌/球拍反馈。 |
| 挥拍音效 | `Assets/ping_pong_whoosh.mp3` | 可以 | 无 | 无 | 适合 | 可用于球拍快速挥动或击球反馈。 |
| 欢呼音效 | `Assets/Resources/Cheering.mp3` | 可以 | 无 | 无 | 可选 | Demo 初期可不接入，后续用于命中目标奖励。 |
| 嘘声音效 | `Assets/Scripts/studio audience awwww sound FX (1).mp3` | 可以但不建议优先 | 无 | 无 | 可选 | 文件位于 `Scripts` 目录，迁移时必须重命名并放入 `Audio/PingPong`。 |
| 物理参数 | `game__room.unity` 中 Ball/Paddle Rigidbody、Collider 配置 | 不直接复制场景 | 若复制场景且不复制脚本会有 Missing Script | 场景内有 Oculus 依赖脚本 | 适合“参考数值” | 当前项目已经采用相近球质量 `0.0027` 和连续碰撞，可吸收碰撞体尺寸、音效挂载思路。 |
| 场景结构 | `Assets/Scenes/game__room.unity` | 不建议直接复制 | 高 | 高，引用 `OVRInput` 脚本和 Oculus prefabs | 只适合参考 | 只参考对象布局：Table、Paddle、EnemyPaddle、Ball、playerside/enemyside。 |

补充观察：

- `game__room.unity` 中球是 Unity 内置 Sphere，而不是 `Ball.fbx`。它的 Rigidbody 质量为 `0.0027`，`m_CollisionDetection: 2`，即 ContinuousDynamic，对当前项目有参考价值。
- `game__room.unity` 中 `PPPaddle.fbx` 被实例化为 `Paddle` 和 `EnemyPaddle`，缩放约为 `4`。当前项目不需要 `EnemyPaddle`，只需要右手训练球拍。
- `game__room.unity` 中 `PingPondTable.fbx` 被实例化时缩放约为 `40`，位置约 `x=0.638, y=0.763, z=0`，旋转约 `(-90, 90, 0)`。这说明 FBX 原始单位较小，迁移后需要在 Adapted Prefab 中固定缩放。
- `PingPondTable.fbx.meta` 内部材质名包括 `Black`、`Net`、`Material.001`、`TableGreen`，说明球网属于球台模型的一部分。

## 不建议迁移的内容

- `Assets/Oculus/` 整个目录：包含 Oculus VR、Avatar、Platform、Spatializer、SampleFramework、OpenVR bridge 等大量依赖，会破坏当前 PICO/XR 配置风险，且违反“不引入 Oculus、SteamVR、Photon、Meta XR 等依赖”限制。
- `Assets/Scenes/*.unity`：不能直接修改或迁移场景。源场景绑定旧对象名、旧 UI、Oculus Avatar/OVR 脚本和大量家具场景资产，迁移成本高于收益。
- `Assets/Scripts/gameplay.cs`、`Assets/Resources/PaddleControl.cs`、`Assets/Resources/BallHolding.cs`、`Assets/Scripts/quit.cs`：直接引用 `OVRInput`。当前项目不含 Oculus SDK，复制后会编译失败。
- `Assets/Scripts/ballbounce.cs`：虽然主要是乒乓球状态机和音效逻辑，但强绑定 `UnityEngine.UI.Text`、具体对象名、`gameplay` 静态字段和源场景结构，不适合直接迁移。可作为比分/规则参考。
- `Assets/BarcadeGamesAssetPack/DemoScene.unity`：资产包演示场景，包含台球、空气曲棍球等，不是当前最小 Demo 所需。
- `Assets/Furniture_ges1/`：家具/房间装饰资源较多，会增加项目体积；当前快速 Demo 不需要。
- `ProjectSettings/`、`Packages/manifest.json`：不得迁移。参考项目是旧 Oculus 工程，当前项目必须保留 PICO / XR 配置。

## 资源迁移路径建议

建议采用“Original 只放精选原始文件，Adapted 放清理后的 Unity prefab/material”的分层方式：

- `Assets/_Project/External/VRTableTennis/Original/`
  - 放许可证和来源说明。
  - 可放少量原始文件清单，不建议把完整工程复制进来。
- `Assets/_Project/External/VRTableTennis/Original/Models/`
  - `PPPaddle.fbx`
  - `PingPondTable.fbx`
  - 暂不放 `Ball.fbx`，除非后续确认模型外观适合乒乓球。
- `Assets/_Project/External/VRTableTennis/Original/Audio/`
  - `single_bounce.mp3`
  - `ping_pong_whoosh.mp3`
  - 可选：`Cheering.mp3`
- `Assets/_Project/External/VRTableTennis/Original/Materials/`
  - 只放和球台/球拍相关的 `PingPongTable*.mat`，用于参考颜色，不建议最终 prefab 直接依赖旧材质。
- `Assets/_Project/External/VRTableTennis/Adapted/`
  - 放从原始 FBX 派生的中间 prefab 或材质替换版本。
- `Assets/_Project/Prefabs/PingPong/`
  - `PingPongTable.prefab`
  - `PingPongPaddle.prefab`
  - `PingPongBall.prefab`
  - 这些是 `PingPongDemoSceneBuilder` 最终加载的稳定入口。
- `Assets/_Project/Materials/PingPong/`
  - `TableGreen_URP.mat`
  - `NetWhite_URP.mat`
  - `PaddleRed_URP.mat`
  - `PaddleWood_URP.mat`
  - `BallWhite_URP.mat`
- `Assets/_Project/Models/PingPong/`
  - 如果不想通过 `External/.../Original` 间接引用，也可以把最终使用的 FBX 放这里。二选一即可，避免重复。
- `Assets/_Project/Textures/PingPong/`
  - 只放实际使用的木纹/球台纹理。
- `Assets/_Project/Audio/PingPong/`
  - `single_bounce.mp3`
  - `ping_pong_whoosh.mp3`
  - `cheering.mp3`

建议保留 `LICENSE` 或在 `THIRD_PARTY_NOTICES.md` 补充 MIT 许可证归属。注意 `BarcadeGamesAssetPack` 文件的 `.meta` 中 `licenseType: Store`，虽然仓库本身是 MIT，但资产来源可能来自 Unity Asset Store 包。正式商用前需要复核该资产包原始授权。

## Prefab 清理建议

### PingPongTable.prefab

- 基于 `PingPondTable.fbx` 新建，而不是复制源 `.unity` 中的实例。
- 在 Adapted prefab 上固定：
  - Transform 缩放：先参考源场景约 `40`，再按当前 Demo 桌面宽 `2.4m`、深 `1.4m` 调整。
  - MeshRenderer 材质替换为当前项目 URP/Lit 材质。
  - Collider：不要直接用复杂 MeshCollider 作为主要碰撞面。建议添加明确的 BoxCollider：
    - 桌面 collider：约 `2.4 x 0.08 x 1.4`。
    - 球网 collider：约 `2.4 x 0.25 x 0.03`，可独立 child。
  - Rigidbody：球台不需要 Rigidbody，保持静态 Collider 即可。
- 如果 FBX 内置网格的网和台面不可拆分，可在 prefab 下额外建 `NetCollider` child 专门处理碰撞。

### PingPongPaddle.prefab

- 基于 `PPPaddle.fbx` 新建，移除源场景里附加的 `PaddleControl`、`DontGoThroughThings` 等旧脚本。
- 添加当前项目脚本：
  - `PaddleFollower`
  - `PaddleVelocityTracker`
- Rigidbody：
  - `isKinematic = true`
  - `useGravity = false`
  - `collisionDetectionMode = ContinuousSpeculative`
- Collider：
  - 使用 BoxCollider 作为主要击球区域。
  - 源场景的 BoxCollider 参考尺寸约 `0.0455 x 0.008 x 0.0806`，但这是在 FBX 局部/缩放体系下的值。迁移后应在最终 prefab 尺寸下重新调。
- 不要迁移 `OVRInput.GetLocalControllerVelocity` 逻辑；当前项目已通过 `PaddleVelocityTracker` 从 Transform 差分计算速度。

### PingPongBall.prefab

- 不建议直接使用 `Ball.fbx`，因为它更像台球资产。
- 保留当前 `PingPongDemoSceneBuilder.CreateOrUpdateBallPrefab()` 生成的 Sphere 基础结构，再做资源化清理：
  - Sphere Mesh 或简单球模型。
  - `SphereCollider.radius = 0.5`，最终 Transform scale 约 `0.04`。
  - Rigidbody `mass = 0.0027`，沿用源项目真实乒乓球质量思路。
  - `collisionDetectionMode = ContinuousDynamic`。
  - `interpolation = Interpolate`。
  - 当前项目脚本 `PingPongBall`、`BallLifetime`。
  - 可加 `AudioSource`，clip 指向 `single_bounce.mp3`，但播放逻辑建议在当前 `PingPongBall` 或反馈系统里接入，而不是迁移 `ballbounce.cs`。

### Audio

- 音频文件可以直接复制到 `Assets/_Project/Audio/PingPong/`。
- 不要迁移 Oculus Spatializer 或 Oculus AudioManager。
- 先用普通 Unity `AudioSource`，必要时后续再接入当前项目自己的 `HitFeedbackManager`。

## 对当前 PingPongDemoSceneBuilder 的修改建议

当前 `Assets/_Project/Scripts/Editor/PingPongDemoSceneBuilder.cs` 的关键行为：

- 第 9-10 行只定义 `PrefabRoot` 和 `MaterialRoot`。
- 第 26-29 行直接调用 `LoadOrCreatePrefabAsset()` / `CreateOrUpdateBallPrefab()` 创建 fallback Cube/Sphere prefab。
- 第 31-33 行实例化时强制写入固定 scale，可能覆盖外部模型 prefab 的真实缩放。
- 第 173 行 `SetupPaddle()` 会给球拍添加当前项目所需 Rigidbody、Collider、`PaddleFollower`、`PaddleVelocityTracker`。
- 第 213 行 `BindRightController()` 会自动绑定右手控制器。

建议改法，不在本次执行中实际修改：

1. 增加外部资源路径常量：
   - `ExternalRoot = "Assets/_Project/External/VRTableTennis"`
   - `AdaptedRoot = "Assets/_Project/External/VRTableTennis/Adapted"`
   - `ModelRoot = "Assets/_Project/Models/PingPong"`
   - `AudioRoot = "Assets/_Project/Audio/PingPong"`
2. 新增“优先加载已适配 prefab，找不到再 fallback”的方法：
   - `LoadPreferredPrefab("PingPongTable", fallbackFactory)`
   - `LoadPreferredPrefab("PingPongPaddle", fallbackFactory)`
   - `LoadPreferredPrefab("PingPongBall", fallbackFactory)`
3. 推荐查找顺序：
   - `Assets/_Project/Prefabs/PingPong/PingPongTable.prefab`
   - `Assets/_Project/External/VRTableTennis/Adapted/PingPongTable_Adapted.prefab`
   - fallback Cube。
   - 球拍、球同理。
4. 对外部 prefab 不要强制套用 Cube/Sphere 的 scale：
   - 将 `InstantiateOrReuse(...)` 改为支持 `bool preservePrefabScale` 或 `Vector3? overrideScale`。
   - fallback 资源才使用当前硬编码 scale。
   - 外部 `PPPaddle.fbx` 和 `PingPondTable.fbx` 适配后的 prefab 应在 prefab 内固定尺寸。
5. `CreateOrUpdateBallPrefab()` 应拆成两层：
   - `LoadOrCreateBallPrefab()`：先加载 `PingPongBall.prefab`。
   - `CreateFallbackBallPrefab()`：只在没有已迁移 prefab 时创建 Sphere。
6. `SetupPaddle()` 可以保留，但要避免破坏已有 MeshCollider/BoxCollider：
   - 如果外部 prefab 已有 `BoxCollider`，不要添加新的重复 Collider。
   - 如果只有 MeshCollider，建议禁用或保留为视觉辅助，另加简化 BoxCollider。
7. 新增 `SetupBall(GameObject ballPrefabOrInstance)` 思路：
   - 确保 Rigidbody、SphereCollider、`PingPongBall`、`BallLifetime` 存在。
   - 可绑定 `single_bounce.mp3` 的 AudioSource。
8. `netPrefab` 的策略：
   - 如果迁移后的 `PingPongTable.prefab` 已含视觉球网，则不再单独实例化 fallback `Net`。
   - 如果仍需要明确碰撞网，则保留单独 `PingPongNet.prefab` 或在 table prefab 下创建 `NetCollider`。
   - Builder 可通过检查 table prefab 内是否存在名称包含 `Net` 的 child，决定是否实例化 fallback net。
9. 不要从 Builder 里导入或修改 `ProjectSettings`，也不要调用任何场景文件直接写入逻辑。继续只通过当前打开场景中的 GameObject 构建对象。

推荐的最终优先级：

1. 当前项目自有 `Assets/_Project/Prefabs/PingPong/*.prefab`
2. VRTableTennis Adapted prefab
3. VRTableTennis Original FBX 临时实例化后保存 Adapted prefab
4. 当前 Cube/Sphere fallback

## 最小迁移步骤

第一阶段只迁移美术和音效，不迁移旧脚本：

1. 复制并归档许可证：
   - `D:\tmp\codex_ref\VRTableTennis\LICENSE`
   - 目标：`Assets/_Project/External/VRTableTennis/Original/LICENSE`
2. 精选复制模型：
   - `Assets/BarcadeGamesAssetPack/Models/FBX/PPPaddle.fbx`
   - `Assets/BarcadeGamesAssetPack/Models/FBX/PingPondTable.fbx`
   - 目标：`Assets/_Project/External/VRTableTennis/Original/Models/`
3. 精选复制音效：
   - `Assets/single_bounce.mp3`
   - `Assets/ping_pong_whoosh.mp3`
   - 可选 `Assets/Resources/Cheering.mp3`
   - 目标：`Assets/_Project/Audio/PingPong/`
4. 在 Unity Editor 中创建适配 prefab：
   - `PingPongTable_Adapted.prefab`
   - `PingPongPaddle_Adapted.prefab`
   - 不修改 `.unity` 文件，手动或通过后续 Editor 工具创建 prefab asset。
5. 替换材质：
   - 用当前 URP/Lit 或 Standard fallback 重新创建材质。
   - 不直接依赖源项目旧 Standard 材质作为最终材质。
6. 给 Adapted prefab 添加当前项目脚本和简化碰撞体：
   - 球拍：`PaddleFollower`、`PaddleVelocityTracker`、Rigidbody、BoxCollider。
   - 球：继续使用当前 `PingPongBall.prefab`，只接入音效和白色材质。
   - 球台：BoxCollider 桌面，NetCollider 子物体。
7. 修改 `PingPongDemoSceneBuilder.cs`：
   - 先查找已迁移 prefab。
   - 找不到时再调用现有 fallback 创建逻辑。
8. 打开当前 PICO Demo 场景，通过菜单 `Tools/PICO ElderCare/Build PingPong Demo Scene` 生成对象，确认未破坏 XR Origin/PICO 配置。

## 风险点

- 源项目包含完整 Oculus SDK。任何批量复制 `Assets/Oculus` 都会引入大量编译冲突和 XR 设置风险。
- 源脚本使用 `OVRInput`，当前 PICO 项目没有 Oculus SDK，直接复制脚本会编译失败。
- 源场景依赖旧 GameObject 名称和旧 UI 文本，不能直接作为当前 Demo 场景。
- `PingPondTable.fbx` 文件名拼写为 `PingPond`，当前项目使用 `PingPong` 命名。迁移时建议保留原始文件名在 `Original`，适配 prefab 使用 `PingPongTable.prefab`。
- `Ball.fbx` 不一定是乒乓球。直接替换当前球可能让 Demo 视觉上变成台球，需要人工确认。
- 源材质为 Built-in Standard shader。当前项目若使用 URP，直接复制 `.mat` 可能显示粉色或效果不一致，需要重建材质。
- FBX 缩放单位不一致。源场景中 table 缩放约 `40`、paddle 缩放约 `4`，不能直接用当前 Builder 的 Cube/Sphere scale 覆盖。
- 球网与球台可能在同一 mesh 中，若只想替换 net，需要额外拆分或创建独立 `NetCollider`。
- 资产授权需要复核。仓库 LICENSE 是 MIT，但 `BarcadeGamesAssetPack` 的 `.meta` 有 `licenseType: Store`，正式发布前应确认 Asset Store 包授权。

## 下一步执行计划

1. 只复制精选资源到 `Assets/_Project/External/VRTableTennis/Original/` 和 `Assets/_Project/Audio/PingPong/`，不复制完整仓库。
2. 在 Unity 中创建 `PingPongTable_Adapted.prefab` 和 `PingPongPaddle_Adapted.prefab`，清理旧材质、添加简化碰撞。
3. 保留当前项目的 `PingPongBall`、`BallSpawner`、`PaddleFollower`、`PaddleVelocityTracker` 作为核心玩法脚本。
4. 更新 `PingPongDemoSceneBuilder.cs`，让它优先加载 `Assets/_Project/Prefabs/PingPong/` 或 `External/VRTableTennis/Adapted/` 中的 prefab，找不到再走 fallback Cube/Sphere。
5. 增加音效绑定方案：优先把 `single_bounce.mp3` 接到球碰撞反馈，把 `ping_pong_whoosh.mp3` 接到高速度挥拍或命中反馈。
6. 在 PICO/XR 当前场景中运行 Builder 验证：
   - 球台显示正确。
   - 球拍跟随右手控制器。
   - 球能生成、碰撞、反弹。
   - 没有 Oculus/SteamVR/Photon/Meta XR 依赖进入项目。
   - `ProjectSettings` 和 `.unity` 文件未被自动污染。
