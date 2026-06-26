# 2026-06-10 — Properties 消费层设计与实现

## 做了什么

从架构层面完成了 Properties 模块的消费层：
- **PropertyPresetSO** — 实体定义抽象基类（Template + OverridesJson）
- **PropertyTable** — 统一类型分发（DoWrite），静态工厂 Create，Set/Modify/Load/Tick/Snapshot/Guard/事件
- **PropertyComponent** — MonoBehaviour 门面
- **FloatState + FloatModifier + RateContext** — Float 运行时引擎，预设频率 + 共享计时器 + 分桶存储

删除了 ResolvedPropertyBag（逻辑融入 PropertyTable）。

## 设计决策

- Schema 值永远不直接读——构造时一次性全解析
- PropertyComponent 是唯一门面
- 伴生属性推断暂不做——行为模型入口留白
- FloatState 按频率分桶（PerFrame/PerSecond/PerMinute/Custom），空桶不计时
- 两个 Agent 交叉 review 后修复 6 个 bug

## 未完成

- 消费者适配（CharacterActor、Combat、Physiology、VitalsOverlay 等）——待审核通过后实施
- 旧 Stats 代码删除——Phase 4

## 相关文件

- `Assets/Scripts/Services/Modules/L3_Properties/Definition/PropertyPresetSO.cs`
- `Assets/Scripts/Services/Modules/L3_Properties/Instance/PropertyTable.cs`
- `Assets/Scripts/Services/Modules/L3_Properties/Instance/FloatModifier.cs`
- `Assets/Scripts/Services/Modules/L3_Properties/Instance/FloatState.cs`
- `Assets/Scripts/Services/Modules/L3_Properties/PropertyComponent.cs`
