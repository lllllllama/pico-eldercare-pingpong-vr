# PICO ElderCare VR/MR

面向 PICO 设备的老年人 VR/MR 综合康养项目。当前首页采用 Figma 原型里的四模块结构：健康游戏、康复运动、VR旅游、场景视频；现阶段已内置健康游戏里的乒乓球训练功能，其余模块保留入口，后续可以继续接入独立内容。

![mode-vr](https://img.shields.io/badge/mode-VR-blue) ![mode-mr](https://img.shields.io/badge/mode-MR-orange) ![unity](https://img.shields.io/badge/Unity-2022.3.62f3-black) ![sdk](https://img.shields.io/badge/PICO%20SDK-3.4.0-green)

- **VR 模式**：虚拟首页 + 虚拟康养功能入口 + 乒乓球训练场景。
- **MR 模式**：真实房间透视背景 + 虚拟首页/球桌/球/球拍/UI，支持把球桌摆到真实空间中。

---

## 目录

- [项目信息](#项目信息)
- [核心特性](#核心特性)
- [快速开始](#快速开始)
- [Unity 菜单](#unity-菜单)
- [命令行构建与自测](#命令行构建与自测)
- [MR/XR 使用方式](#mrxr-使用方式)
- [目录结构](#目录结构)
- [关键脚本](#关键脚本)
- [实机检查清单](#实机检查清单)
- [常见问题](#常见问题)
- [开发约定](#开发约定)
- [第三方与许可](#第三方与许可)

---

## 项目信息

| 项 | 值 |
| --- | --- |
| Unity | `2022.3.62f3` |
| 目标设备 | PICO 4 Ultra / 支持视频透视的 PICO XR 设备 |
| PICO SDK | `com.unity.xr.picoxr`，来自 `PICO-Unity-Integration-SDK` 的 `release_3.4.0` |
| 主场景 | `Assets/_Project/Scenes/01_PingPongDemo.unity` |
| 自检场景 | `Assets/_Project/Scenes/00_DeviceTest.unity` |
| 第三方说明 | `THIRD_PARTY_NOTICES.md` |

---

## 核心特性

**综合首页**
- 运行后先进入 `VR康养服务` 世界空间首页，而不是直接开始乒乓球。
- 首页包含 `健康游戏`、`康复运动`、`VR旅游`、`场景视频` 四个模块入口。
- `健康游戏` 当前启动内置乒乓球训练；其它模块显示待接入状态，后续可按模块 ID 扩展。
- 首页卡片使用 VR 大字号、发光 hover、线性图标和手柄/手势选择提示。

**交互**
- 右手球拍自动跟随控制器击球，支持持球发球与自由球击打两套策略。
- 左手 Grip 抓球与释放球，松手速度自动转换为出球速度。
- 左手 Grip 拖动桌子黄色把手摆放球桌，松开可存档。

**物理**
- 桌面、球网、球拍、地面分别标注 `PingPongSurface` 类型，反弹/摩擦参数独立可调。
- 击球解算统一走 `PingPongHitSolver`，支持法向反弹、切向摩擦、自旋转移、最小闭合速度、方向约束。
- 球体自写空气动力学（阻力 + Magnus），`maxAngularVelocity` 提升到 180 rad/s，避免 Unity 默认上限截断发球旋转。
- `ContinuousDynamic` 碰撞检测 + `SphereCast` 扫掠 fallback，双重保护高速球穿桌。

**发球**
- 自动发球循环，支持 Basic / Topspin / Backspin / Sidespin / RandomMixed 五种 profile。
- 弹道求解保证过网，目标点带随机扰动，旋转方向轴从水平速度动态计算。

**MR**
- 视频透视（Video See-Through），虚拟地面/背景墙自动隐藏。
- Plane Detection 自动对齐桌面高度到真实地面。
- 桌子位置通过 `PlayerPrefs` 记忆，下次进入 MR 场景自动恢复。

**工具链**
- `PingPongDemoSceneBuilder` 一键生成 VR/MR 场景，无需手工改 `.unity`。
- `PingPongPhysicsSelfTests` 编辑器自测覆盖 Solver 关键路径，可在 batchmode 下跑回归。
- `VRTableTennis` 资产分层复用：`Original` 保原始素材，`Adapted` 保清理后的可用 prefab。

---

## 快速开始

1. 用 Unity Hub 打开本项目，编辑器版本必须是 `2022.3.62f3`。
2. 首次打开等 Package Manager 拉完依赖（PICO SDK 走 Git，需要外网）。
3. 打开主场景：

   ```text
   Assets/_Project/Scenes/01_PingPongDemo.unity
   ```

4. 根据目标模式点击 Unity 顶部菜单生成综合场景对象：
   - VR：`Tools/PICO ElderCare/Build PingPong Demo Scene`
   - MR：`Tools/PICO ElderCare/Build PingPong Mixed Reality Scene`
5. 把场景加入 Build Settings，Build And Run 到 PICO 设备。运行后先看到 `VR康养服务` 首页，选择 `健康游戏` 后进入乒乓球训练。

---

## Unity 菜单

所有菜单都在 `Tools/PICO ElderCare/` 下。

| 菜单项 | 作用 |
| --- | --- |
| `Build PingPong Demo Scene` | 生成 VR 综合首页 + 内置乒乓球训练：关闭 PICO MR 开关、移除 MR 对象、恢复不透明主相机、启用虚拟地面和背景墙。 |
| `Build PingPong Mixed Reality Scene` | 生成 MR 综合首页 + 内置乒乓球训练：启用 PICO MR、视频透视、Plane Detection、Spatial Mesh，隐藏虚拟房间，挂 MR 管理器。 |
| `Build VRTableTennis Adapted Assets` | 从 `External/VRTableTennis/Original` 的模型、音频、材质生成 `Adapted` 下的可用 prefab。 |
| `Repair PingPong Demo Scene Objects` | 脚本升级后修复已有场景对象，避免整场景重建。 |
| `Run PingPong Physics Self Tests` | 在编辑器里跑 Solver/发球/空气动力学的物理自测。 |

---

## 命令行构建与自测

适合 CI 或者本地冒烟验证。所有命令在项目根目录执行。

**构建 MR 版本**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PingPongDemoSceneBuilder.BuildMixedRealityDemoScene -logFile 'Logs\unity_mr_build.log'
```

**构建 VR 版本**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PingPongDemoSceneBuilder.BuildDemoScene -logFile 'Logs\unity_vr_build.log'
```

**跑物理自测**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PingPongPhysicsSelfTests.RunAll -logFile 'Logs\unity_physics_tests.log'
```

自测通过时日志里会出现：

```text
PingPong physics self tests passed.
```

如果 batchmode 直接退出并在日志里看到 `No valid Unity Editor license found`，说明 Unity 授权未激活，先在 Unity Hub 登录并激活许可证。

---

## MR/XR 使用方式

1. 运行 `Build PingPong Mixed Reality Scene`。
2. Build And Run 到支持视频透视的 PICO 设备。
3. 进入后真实房间作为背景，虚拟球桌、球、球拍和 UI 叠加在真实空间中。
4. 左手靠近球桌左前方黄色把手 `LeftTableDragHandle`。
5. 按住左手 Grip 并移动手柄调整球桌位置；松开 Grip 位置会保存。
6. 下次进入 MR 场景时自动加载上次保存的位置。
7. 检测到真实地面后，`RoomPlaneAligner` 会把球桌高度对齐到地面，并同步发球高度、反弹高度和控制器限位高度。

---

## 目录结构

```
Assets/
  _Project/
    Docs/               # 绑定指引 / VRTableTennis 迁移说明
    External/
      VRTableTennis/
        Original/       # 精选复制的 FBX、音频、材质、LICENSE
        Adapted/        # 清理后的 table/net/paddle/ball prefab
    Materials/PingPong/ # 项目自有 URP 材质（含 MR 可视化材质）
    Prefabs/PingPong/   # fallback 球 prefab
    Scenes/             # 01_PingPongDemo / 00_DeviceTest
    Scripts/
      Common/Events/    # PingPongEvents 事件总线
      Common/Feedback/  # HitFeedbackManager 音效/特效
      Editor/           # 场景构建器 + 物理自测
      PingPong/
        Ball/           # PingPongBall / BallSpawner / BallLifetime
        Paddle/         # PaddleFollower / PaddleVelocityTracker
        Interaction/    # 抓球、拖桌、桌子锁定、穿桌限位、视角对齐
        MR/             # MR 管理器 + 地面对齐
        UI/             # ScoreManager
        PingPongGeometry.cs / PingPongHitSolver.cs / PingPongSurface.cs
  Resources/            # PICO Debugger / ProjectSetting / PlatformSetting
  XR/ + XRI/            # XR Management 与 XRI 设置
```

---

## 关键脚本

**玩法核心**

| 脚本 | 职责 |
| --- | --- |
| `PingPong/PingPongGeometry.cs` | 球桌、球网、球、球拍的统一尺寸与物理常量。 |
| `PingPong/PingPongSurface.cs` | 表面类型（Table/Net/Paddle/Floor）标注、法线估计、PhysicMaterial 缓存。 |
| `PingPong/PingPongHitSolver.cs` | 纯函数碰撞解算器（法向反弹、切向摩擦、自旋、方向约束）。 |
| `PingPong/Ball/PingPongBall.cs` | 球物理、空气动力学、碰撞回弹、扫掠 fallback。 |
| `PingPong/Ball/BallSpawner.cs` | 自动发球、弹道求解、profile 选择、旋转轴构造。 |
| `PingPong/Ball/BallLifetime.cs` | 球体生命周期与 miss 上报。 |
| `PingPong/Paddle/PaddleFollower.cs` | 球拍跟随控制器 Transform。 |
| `PingPong/Paddle/PaddleVelocityTracker.cs` | 差分速度、角速度、接触点速度、击球点局部坐标。 |

**交互与 MR**

| 脚本 | 职责 |
| --- | --- |
| `PingPong/Interaction/ControllerBallGrabber.cs` | 左手 Grip 抓球、释放速度估计、挡出冷却。 |
| `PingPong/Interaction/TableDragHandle.cs` | 左手拖桌、PlayerPrefs 存档、同步发球点和桌面高度。 |
| `PingPong/Interaction/TablePassiveMotionLock.cs` | 非拖动期间锁定桌子。 |
| `PingPong/Interaction/ControllerTableCollisionLimiter.cs` | 限制手柄/球拍视觉穿入桌面。 |
| `PingPong/Interaction/PlayerTableBoundary.cs` | 头部/XR Rig 不进入桌内。 |
| `PingPong/Interaction/GrabHandPoseAnimator.cs` | 根据 Grip 值驱动手指张合动画。 |
| `PingPong/Interaction/VrInitialViewAligner.cs` | 进入 VR 时对齐相机朝向球桌。 |
| `PingPong/MR/PingPongMixedRealityManager.cs` | PICO 视频透视、透明主相机、隐藏虚拟环境。 |
| `PingPong/MR/PingPongRoomPlaneAligner.cs` | 地面检测、桌子对齐、高度相关数据同步。 |

**事件与反馈**

| 脚本 | 职责 |
| --- | --- |
| `Common/Events/PingPongEvents.cs` | 发球、击打、反弹、miss、训练开始/结束事件与结构体。 |
| `Common/Feedback/HitFeedbackManager.cs` | 按速度播放击打/反弹音效与特效。 |
| `PingPong/UI/ScoreManager.cs` | 命中、发球、miss、命中率、速度、旋转的 TMP 文本。 |

**工具链**

| 脚本 | 职责 |
| --- | --- |
| `Editor/PingPongDemoSceneBuilder.cs` | VR/MR 场景生成、prefab 装配、PICO 项目设置切换、修复工具。 |
| `Editor/PingPongPhysicsSelfTests.cs` | Solver、发球、空气动力学、旋转上限的批处理自测。 |

---

## 实机检查清单

**进入场景**
- 初始视角是否正对球桌。
- 右手球拍是否跟随控制器。
- 左手是否显示握持视觉并随 Grip 张合。

**击球与发球**
- 击球方向和力量是否符合挥拍动作。
- 左手 Grip 是否可以抓球、释放球。
- 发球是否能过网并落到桌面。
- 上旋、下旋、侧旋是否有明显轨迹差异。

**物理安全**
- 球是否不再穿透桌面或球拍。
- 球网是否没有明显空气墙。
- 左右手和球拍是否不会明显穿进桌体。

**MR 与摆放**
- 拖拽球桌后，发球点、目标点、UI、碰撞限位是否同步。
- MR 模式下真实房间是否可见，虚拟地面和背景墙是否隐藏。
- MR 地面对齐后，球桌高度和发球高度是否合理。
- 退出再进入 MR 场景，上次桌子位置是否正确恢复。

---

## 常见问题

**Unity 打不开或包导入失败**
确认使用 `2022.3.62f3`，让 Package Manager 完整拉取 PICO SDK。PICO SDK 来自 Git URL，首次导入需要网络。

**batchmode 直接退出**
日志出现 `No valid Unity Editor license found` 时，先在 Unity Hub 激活许可证，再重新运行 batchmode 命令。

**右手球拍不跟随**
选择 `PingPong/Paddle_Right`，把 XR Origin 的右手控制器 Transform 手动拖到 `PaddleFollower.controllerTransform`。

**左手无法拖拽球桌**
确认场景里存在 `LeftTableDragHandle`，并且左手控制器靠近黄色球形把手时按住 Grip。

**MR 模式仍显示虚拟地面或背景墙**
重新运行 `Tools/PICO ElderCare/Build PingPong Mixed Reality Scene`，MR 构建路径会禁用 `Floor` 和 `BackWall` 并启用透明相机。

**普通 VR 模式仍开启透视**
重新运行 `Tools/PICO ElderCare/Build PingPong Demo Scene`，VR 构建路径会移除 MR 对象、关闭 PICO MR 设置并恢复主相机背景。

**球太快或太难接**
调整 `Managers/BallSpawner`：
- `serveSpeed`：发球速度。
- `serveInterval`：发球间隔。
- `upwardArc`：弹道高度。
- `PingPongBall` 的 `paddleVelocityMultiplier / forwardBoost / upwardBoost / maxSpeed` 控制回球。

---

## 开发约定

- 不要直接导入参考项目的完整场景，避免旧 XR/Oculus/SteamVR/Photon 依赖污染当前 PICO 工程。
- 修改场景生成逻辑后，优先跑对应的 Builder 菜单，而不是手工批量改场景对象。
- 新增 Unity 资源时必须同时提交对应的 `.meta` 文件。
- 提交前建议跑一次 `git diff --check`，避免 YAML/meta 尾随空格产生 review 噪声。
- MR 相关设置会写入 `Assets/Resources/PXR_ProjectSetting.asset`，切换 VR/MR 场景前后确认当前目标模式。
- 物理/Solver 相关改动建议配套更新 `PingPongPhysicsSelfTests`，保留可回归覆盖。

---

## 第三方与许可

本项目参考并复用了以下项目的设计方向或资源：

- [`kushal-goenka/VRTableTennis`](https://github.com/kushal-goenka/VRTableTennis)
- [`tomgoddard/PingPang`](https://github.com/tomgoddard/PingPang)
- [`Pico-Developer/InteractionSample-Unity`](https://github.com/Pico-Developer/InteractionSample-Unity)

详细边界见 [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md)。重新分发构建包或源代码前，请确认复制资源的原始授权（特别是 `BarcadeGamesAssetPack` 相关资产可能标注 `licenseType: Store`）。
