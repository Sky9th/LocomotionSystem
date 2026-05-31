# 2026-05-31 移动速度架构重构

## 改动范围

54 个文件，覆盖 Locomotion、Animation、Pathfinding、Camera、Stats。

## 主要改动

- **LocomotionProfile** 拆分为按 gait 的速度配置（walk/run/sprint/crawl）
- **LocomotionModeProfile** 新增 `animNativeSpeed` 记录动画原生速度
- **AnimationBrain** 新增 `ApplySpeedMultiplier`，gait 变化时计算 `期望速度/动画原生` 乘积
- **PathfindingAgent** 新增 `DesiredSpeedMultiplier` 和 gait 同步
- **SCharacterIntent** 新增 `MovementSpeedMultiplier` 传递 A* 减速
- **Motor.ConvertToWorld** 修复 heading 旋转
- RootMotion 旋转关闭，转身速度提升到 720°/s
- TurnInMoving / IdleToMoving 废弃，改用即时代码旋转
- `L5_Locomotion` → `Locomotion` 目录重命名
- CameraService 生成时瞬移到玩家位置
- CharacterStats 内联 LastStats 生成

## 设计决策

- 两个 Profile 语义明确：LocomotionProfile = 角色期望移速，ModeProfile = 动画原生速度
- 乘积只在 gait 切换时计算一次，不每帧运算
- A* 减速通过 MovementSpeedMultiplier → Motor → BlendTree 处理，不通过动画倍率
- 8 方向可变速度动画本身不需要乘积（乘积仅用于 buff/减益导致的根本速度变化）

## 已知问题

- L4_Locomotion 目录结构待重新梳理
- A* 路径对角线抖动通过 SimpleSmoothModifier + pickNextWaypointDist 基本解决
