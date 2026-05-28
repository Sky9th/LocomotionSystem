# 2026-05-29 L1-L5 目录结构重构

## 概述

将 `Assets/Scripts/` 从扁平结构迁移到五层架构目录，与 `.agent/tech/` 文档对齐。

## 做了什么

- 155 个 .cs 文件从 11 个扁平目录迁移到 4 个顶层分组
- 界定了 L4/L5 的精确定义：L4=领域子系统，L5=子系统的子系统
- 将按文件类型分组的目录（Config/Structs/Data/Rules/States）去掉 L 前缀
- 将模块自身组件代码（Actor/Actions/Core/Components/HUD）去掉 L4 前缀
- 将单文件 L5 目录合并到父级（L5_UI/L5_Traversal/L5_Config）
- 清理 Windows mv 产生的幽灵 L4_* 空目录
- 反哺 tech/README.md：新增 L 层级定义表、反例说明、完整目录树

## 最终层级结构

```
L1_Core/
Services/
├── L2_EventDispatcher ... L2_CameraService  (6 简单 Service)
├── L2_Audio/  (Data/ Structs/)
├── L2_Input/  (Actions/ Structs/)
├── L2_UI/     (Core/ Components/ HUD/ Config/ MainMenu/)
└── Modules/
    ├── L3_Character/
    │   ├── Actor/ Config/ Input/           # 自身代码
    │   ├── L4_Animation/ → L5_Drivers/ → L5_Locomotion/ → States/
    │   ├── L4_Audio/ (Config/)
    │   ├── L4_Kinematic/ (Structs/)
    │   ├── L4_Locomotion/ (Structs/)
    │   └── L4_Stats/ (Rules/)
    ├── L3_Stats/ (Definition/ Tree/ Instance/ Modifier/ Interfaces/ Editor/)
    └── L3_Pathfinding/
Shared/
├── Logging/ (Appender/ Compat/)
├── Editor/ (Prototype/)
├── Utility/
└── Constants/
```

## L4/L5 界定标准

- **L4**: L3 内部的领域子系统，承担独立功能（Animation/Audio/Kinematic/Locomotion/Stats）
- **不是 L4**: 模块自身组件代码（Actor/Actions/Core/Components/HUD）
- **L5**: L4 内部的附属子系统（Drivers/Locomotion）
- **不是 L5**: 按文件类型分组（Config/Structs/Data/Rules/States/Requests）

## 已知问题

- namespace 和 using 指令尚未更新，编译可能报错
- CreateAssetMenu 路径未更新
- 旧空目录已由用户手动删除
