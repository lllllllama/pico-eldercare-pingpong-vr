# PICO ElderCare PingPong VR

Unity PICO VR 乒乓球体验 Demo，用于养老康复/互动娱乐情景中的轻量化乒乓练习。项目重点包括 PICO 控制器交互、右手球拍击球、左手抓球、标准化球桌/球网物理表现，以及可主动拖拽摆放的球桌。

## 当前核心功能

- **PICO VR 交互**：面向 PICO 头显和手柄测试。
- **右手球拍**：右手绑定球拍，用于击球训练。
- **左手抓球**：左手显示为手部模型，可通过 Grip 抓取和释放乒乓球。
- **标准化球桌**：球桌、球网、桌腿和网柱由程序化几何体生成，避免导入模型视觉与碰撞不一致。
- **球网触发器**：球网碰撞为 trigger，避免形成明显“空气墙”。
- **桌面物理反弹**：桌面使用明确尺寸的 BoxCollider，配合球体物理和表面逻辑处理反弹。
- **黄色拖拽把手**：左手靠近黄色球体并按住 Grip，可主动拖动球桌。
- **桌子被动锁定**：非拖拽状态下，球桌会锁定位置，避免体验者向前走或手柄碰撞把桌子越推越远。
- **手部防穿模**：左右手/右手球拍通过限制器减少穿透桌子的视觉问题。

## 主要 Unity 菜单

在 Unity 顶部菜单中使用：

```text
Tools/PICO ElderCare/Build PingPong Demo Scene
```

用于完整生成/重建乒乓球 Demo 场景对象。

```text
Tools/PICO ElderCare/Repair PingPong Demo Scene Objects
```

用于修复已有场景对象，适合脚本更新后快速同步新组件。

```text
Tools/PICO ElderCare/Build VRTableTennis Adapted Assets
```

用于生成适配后的乒乓球桌、球拍、球等 prefab 资源。

## 推荐使用流程

1. 打开 Unity 项目。
2. 等待 Unity 编译完成。
3. 清空 Console。
4. 执行：

```text
Tools/PICO ElderCare/Build PingPong Demo Scene
```

5. 确认 Hierarchy 中存在：

```text
PingPong
├── Table
│   └── LeftTableDragHandle
├── TablePlayerBlocker
├── Paddle_Right
├── Left_GrabHand
├── BallSpawnPoint
└── BallTargetPoint
```

6. 连接 PICO 设备进行实机测试。

## 桌子拖拽方式

1. 用左手靠近桌子左前侧的黄色球体 `LeftTableDragHandle`。
2. 按住左手 Grip。
3. 左右或前后移动左手。
4. 松开 Grip 后桌子停止并锁定。

拖动时会同步更新：

- `Table`
- `BallSpawnPoint`
- `BallTargetPoint`
- `TablePlayerBlocker`
- `BallSpawner.netWorldZ`
- `BallSpawner.tableBounceWorldY`
- `BallSpawner.tableBounceWorldZ`

## 关键脚本

- `Assets/_Project/Scripts/Editor/PingPongDemoSceneBuilder.cs`
  - Demo 场景和 prefab 的核心生成器。
- `Assets/_Project/Scripts/PingPong/Interaction/TableDragHandle.cs`
  - 左手黄色把手拖拽球桌逻辑。
- `Assets/_Project/Scripts/PingPong/Interaction/TablePassiveMotionLock.cs`
  - 防止球桌被体验者被动推动。
- `Assets/_Project/Scripts/PingPong/Interaction/ControllerTableCollisionLimiter.cs`
  - 限制左右手/球拍视觉穿透桌子。
- `Assets/_Project/Scripts/PingPong/Interaction/ControllerBallGrabber.cs`
  - 左手抓球逻辑。
- `Assets/_Project/Scripts/PingPong/Interaction/GrabHandPoseAnimator.cs`
  - 左手抓取动画姿态。
- `Assets/_Project/Scripts/PingPong/Interaction/PlayerTableBoundary.cs`
  - 桌子区域检测，可跟随桌子位置。
- `Assets/_Project/Scripts/PingPong/Ball/BallSpawner.cs`
  - 乒乓球生成和发球逻辑。
- `Assets/_Project/Scripts/PingPong/PingPongGeometry.cs`
  - 乒乓球桌、球网、球和球拍标准尺寸配置。

## PICO 实机测试清单

- **初始视角**：进入后是否正对球桌。
- **右手击球**：击球方向是否符合球拍接触位置。
- **击球力度**：轻挥和重挥是否有明显区别。
- **左手显示**：左手是否为手部模型，而不是第二个球拍。
- **左手抓球**：左手 Grip 是否能抓球、松开后是否释放。
- **球网表现**：球网是否不再出现明显空气墙。
- **桌面反弹**：球是否不再穿透桌面。
- **桌子锁定**：体验者向前走或手柄顶桌子时，桌子是否不会被动推走。
- **拖拽桌子**：左手抓黄色把手后，桌子是否能左右和前后移动。
- **手部防穿模**：左右手和右手球拍是否不再明显穿进桌体。

## 注意事项

- 如果修改脚本后已有场景对象没有同步，请先执行 `Repair PingPong Demo Scene Objects`。
- 如果场景对象混乱或残留旧对象较多，请执行完整的 `Build PingPong Demo Scene`。
- 如果 Unity Console 出现红色报错，应优先解决第一条编译错误。
- 当前不再依赖导入的 `PingPongTable.fbx` 作为球桌视觉主体，避免视觉高度和碰撞高度不一致。
