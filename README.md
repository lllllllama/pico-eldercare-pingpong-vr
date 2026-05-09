# PICO ElderCare PingPong VR/MR



面向 PICO 设备的乒乓球 VR/MR 训练 Demo。项目目标是在养老康复、轻运动和互动娱乐场景中提供一个轻量、可维护、可实机验证的乒乓球体验。



当前版本已经融合参考项目 `VRTableTennis` 的表、球拍、球、音效等可复用资产，并保留本项目自己的 PICO/XR 交互、物理、发球和场景构建逻辑。项目支持两种运行形态：



- VR 模式：虚拟房间背景 + 虚拟球桌、球、球拍和 UI。

- MR/XR 模式：真实房间透视背景 + 虚拟球桌、球、球拍和 UI，并支持把球桌摆放到真实空间中。



## 🧭 项目信息



- Unity: `2022.3.62f3`

- 目标设备: PICO 4 Ultra / PICO XR 设备

- PICO SDK: `com.unity.xr.picoxr`，来自 `PICO-Unity-Integration-SDK` 的 `release_3.4.0`

- 主场景: `Assets/_Project/Scenes/01_PingPongDemo.unity`

- 第三方说明: 见 `THIRD_PARTY_NOTICES.md`



## ✨ 当前核心功能



- 🎮 PICO 控制器交互：右手球拍击球，左手抓球和摆放球桌。

- 🏓 发球系统：自动发球，支持基础球、上旋、下旋、侧旋和随机混合发球。

- ⚙️ 乒乓球物理：桌面、球网、球拍、地面使用明确的 `PingPongSurface` 类型和反弹参数。

- 🌪️ 空气动力学：球体支持阻力和 Magnus 效应，高速旋转不会被 Unity 默认角速度上限截断。

- 📦 VRTableTennis 资产适配：保留当前项目脚本和 PICO 设置，清理旧 XR/Oculus/SteamVR/Photon 依赖。

- 🟡 桌子拖拽摆放：左手靠近黄色把手并按住 Grip，可拖动球桌。

- 🔒 桌子被动锁定：非拖动状态下锁住桌子，避免玩家或控制器把桌子推走。

- 🪟 MR 透视模式：启用 PICO 视频透视，隐藏虚拟地面/墙面，用真实房间作为背景。

- 📐 MR 地面对齐：通过 Plane Detection 查找真实地面，并同步球桌高度、发球高度和手柄防穿桌高度。

- 🔁 VR/MR 双构建路径：可以在普通 VR Demo 和 Mixed Reality Demo 之间切换构建。



## 🚀 快速开始



1. 使用 Unity Hub 打开项目，确认编辑器版本为 `2022.3.62f3`。

2. 等待 Unity 完成 Package 导入和脚本编译。

3. 打开场景：



```text

Assets/_Project/Scenes/01_PingPongDemo.unity

```



4. 根据目标模式运行顶部菜单。



普通 VR 场景：



```text

Tools/PICO ElderCare/Build PingPong Demo Scene

```



Mixed Reality 场景：



```text

Tools/PICO ElderCare/Build PingPong Mixed Reality Scene

```



5. 将场景加入 Build Settings，然后 Build And Run 到 PICO 设备。



## 🛠️ Unity 菜单



```text

Tools/PICO ElderCare/Build PingPong Demo Scene

```



生成普通 VR Demo。该路径会关闭 PICO MR 项目开关，移除 MR 运行对象，恢复不透明主相机，并重新启用虚拟地面和背景墙。



```text

Tools/PICO ElderCare/Build PingPong Mixed Reality Scene

```



生成 MR/XR Demo。该路径会启用 PICO MR、视频透视、Plane Detection、Spatial Mesh 等设置，隐藏虚拟房间表面，并创建 MR 管理器和空间感知辅助对象。



```text

Tools/PICO ElderCare/Build VRTableTennis Adapted Assets

```



从本地复制的 VRTableTennis 原始资源生成清理后的可用 prefab，包括球桌、球网、球拍和球。



```text

Tools/PICO ElderCare/Repair PingPong Demo Scene Objects

```



修复已有场景对象，适合脚本更新后快速同步组件绑定。



## ✅ 推荐验证流程



MR 版本：



```powershell

& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PingPongDemoSceneBuilder.BuildMixedRealityDemoScene -logFile 'Logs\unity_mr_build.log'

```



普通 VR 版本：



```powershell

& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PingPongDemoSceneBuilder.BuildDemoScene -logFile 'Logs\unity_vr_build.log'

```



物理自测：



```powershell

& 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe' -batchmode -quit -projectPath . -executeMethod PingPongPhysicsSelfTests.RunAll -logFile 'Logs\unity_physics_tests.log'

```



日志中应能看到：



```text

PingPong physics self tests passed.

```



如果 batchmode 提示 `No valid Unity Editor license found`，说明 Unity 授权未激活，先在 Unity Hub 中登录并激活许可证。



## 🪟 MR/XR 使用方式



1. 运行 `Build PingPong Mixed Reality Scene`。

2. Build And Run 到支持视频透视的 PICO 设备。

3. 进入后真实房间作为背景，虚拟球桌、球、球拍和 UI 叠加在真实空间中。

4. 左手靠近球桌左前方黄色把手 `LeftTableDragHandle`。

5. 按住左手 Grip 并移动手柄，调整球桌位置。

6. 松开 Grip 后位置会保存，下次进入 MR 场景时会尝试加载保存位置。

7. 如果检测到真实地面，`RoomPlaneAligner` 会把球桌高度对齐到地面，并同步发球、反弹和控制器限位高度。

## 🧩 关键脚本



- `Assets/_Project/Scripts/Editor/PingPongDemoSceneBuilder.cs`

  - 场景、prefab、VR/MR 模式、PICO 项目设置的核心生成器。

- `Assets/_Project/Scripts/Editor/PingPongPhysicsSelfTests.cs`

  - 编辑器批处理物理自测。

- `Assets/_Project/Scripts/PingPong/MR/PingPongMixedRealityManager.cs`

  - MR 透视相机、虚拟环境隐藏和 PICO 视频透视开关。

- `Assets/_Project/Scripts/PingPong/MR/PingPongRoomPlaneAligner.cs`

  - MR 地面检测、球桌高度对齐和高度相关数据同步。

- `Assets/_Project/Scripts/PingPong/Interaction/TableDragHandle.cs`

  - 左手拖拽球桌、保存摆放位置、同步发球点和桌面高度参数。

- `Assets/_Project/Scripts/PingPong/Interaction/TablePassiveMotionLock.cs`

  - 锁定球桌，防止被非主动交互推动。

- `Assets/_Project/Scripts/PingPong/Interaction/ControllerTableCollisionLimiter.cs`

  - 限制手柄/球拍视觉穿入桌面。

- `Assets/_Project/Scripts/PingPong/Ball/BallSpawner.cs`

  - 自动发球、发球轨迹、旋转发球和球 prefab 配置。

- `Assets/_Project/Scripts/PingPong/Ball/PingPongBall.cs`

  - 球体物理、空气动力学、碰撞回弹和扫掠 fallback。

- `Assets/_Project/Scripts/PingPong/PingPongGeometry.cs`

  - 球桌、球网、球、球拍的统一尺寸常量。



## 📁 资源目录



- `Assets/_Project/External/VRTableTennis/Original`

  - 从参考项目复制的原始模型、材质、贴图、音频和许可证文件。

- `Assets/_Project/External/VRTableTennis/Adapted`

  - 清理后的本项目可用 prefab。

- `Assets/_Project/Materials/PingPong`

  - 本项目生成或维护的乒乓球材质，包括 MR 平面/网格可视化材质。

- `Assets/_Project/Docs`

  - 资产复用、绑定和迁移说明。



## 📋 实机检查清单



- 初始视角是否正对球桌。

- 右手球拍是否跟随控制器。

- 击球方向和力量是否符合挥拍动作。

- 左手 Grip 是否可以抓球、释放球。

- 发球是否能过网并落到桌面。

- 上旋、下旋、侧旋是否有明显轨迹差异。

- 球是否不再穿透桌面或球拍。

- 球网是否没有明显空气墙。

- 左右手和球拍是否不会明显穿进桌体。

- 拖拽球桌后，发球点、目标点、UI 和碰撞限位是否同步。

- MR 模式下真实房间是否可见，虚拟地面/背景墙是否隐藏。

- MR 地面对齐后，球桌高度和发球高度是否合理。

## ❓ 常见问题



### 🧱 Unity 打不开或包导入失败



确认使用 `2022.3.62f3`，并让 Package Manager 完整拉取 PICO SDK。PICO SDK 来自 Git URL，首次导入需要网络。



### 🧪 batchmode 直接退出



如果日志出现：



```text

No valid Unity Editor license found

```



先在 Unity Hub 激活许可证，再重新运行 batchmode 命令。



### 🎮 右手球拍不跟随



选择 `PingPong/Paddle_Right`，在 `PaddleFollower.controllerTransform` 手动绑定 XR Origin 的右手控制器 Transform。



### 🟡 左手无法拖拽球桌



确认场景里存在 `LeftTableDragHandle`，并且左手控制器靠近黄色球形把手时按住 Grip。



### 🪟 MR 模式仍显示虚拟地面或背景墙



重新运行：



```text

Tools/PICO ElderCare/Build PingPong Mixed Reality Scene

```



MR 构建路径会禁用 `Floor` 和 `BackWall`，并启用透明相机。



### 🔁 普通 VR 模式仍开启透视



重新运行：



```text

Tools/PICO ElderCare/Build PingPong Demo Scene

```



VR 构建路径会移除 MR 管理对象，关闭 PICO MR 设置，并恢复主相机背景。



## 🧰 开发注意事项



- 不要直接导入参考项目完整场景，避免旧 XR/Oculus/SteamVR/Photon 依赖污染当前 PICO 项目。

- 修改场景生成逻辑后，优先运行对应 Builder 菜单，而不是手工改大量场景对象。

- 新增 Unity 资源时必须同时提交 `.meta` 文件。

- 提交前建议运行 `git diff --check`，避免 YAML/meta 尾随空格造成 review 噪声。

- 当前 MR 相关设置会写入 `Assets/Resources/PXR_ProjectSetting.asset`，切换 VR/MR 场景前后应确认目标模式。



## 📜 第三方与许可



本项目参考并复用了以下项目的设计或资源方向：



- `kushal-goenka/VRTableTennis`

- `tomgoddard/PingPang`

- `Pico-Developer/InteractionSample-Unity`



详细说明见 `THIRD_PARTY_NOTICES.md`。重新分发构建包或源代码前，请确认复制资源的原始授权边界。

