# PlayerInput · 玩家输入聚合器

> **Last Verified**: 2026-06-22 | **Verification**: All referenced files exist, signatures match code

> `Character/Director/Player/PlayerInput.cs` — 纯类，IEventListener 实现，通过 EventHub 订阅事件，缓存帧状态。

## 调用链

```
被谁调:
  PlayerDirector.OnWire()      → BindEvents()
  PlayerDirector 析构/Unwire   → UnbindEvents()
  PlayerDirector.Evaluate()    → 读取帧状态属性

调谁:
  EventHub.Get<T>()            → 获取事件通道引用
  EventChannelBase.OnRaised    → 订阅/取消事件
  EventDispatcherService       → TEMP: SCameraSnapshot 订阅 (待迁移至 EventHub)
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | PlayerDirector | 每帧读取输入状态 |
| 依赖 | EventHub | 获取事件通道 |
| 依赖 | CrouchInputEventSO / SprintInputEventSO / ProneInputEventSO / StandInputEventSO | 姿态事件 |
| 依赖 | SecondaryInteractInputEventSO | 右键移动事件 |
| 依赖 | Skill1InputEventSO / Skill2InputEventSO | 技能事件 |
| 依赖 | Equip1InputEventSO / Equip2InputEventSO / Equip3InputEventSO | 装备事件 |
| 依赖（TEMP） | EventDispatcherService | SCameraSnapshot 订阅 |

## 公开属性（帧状态）

```csharp
internal bool SecondaryRequested { get; set; }  // 右键移动
internal bool SprintRequested { get; set; }     // Shift 冲刺
internal bool CrouchRequested { get; set; }     // 蹲下
internal bool ProneRequested { get; set; }      // 趴下
internal bool StandRequested { get; set; }      // 站立
internal bool FirstSkillRequested { get; set; }  // Skill1
internal bool SecondSkillRequested { get; set; } // Skill2
internal bool Equip1Requested { get; set; }     // Equip1
internal bool Equip2Requested { get; set; }     // Equip2
internal bool Equip3Requested { get; set; }     // Equip3
internal Vector3 MouseGroundPosition { get; }   // 鼠标地面坐标 (TEMP)
internal bool HasMouseGround { get; }            // 鼠标是否有效 (TEMP)
```

## 方法

### BindEvents()
```csharp
public void BindEvents()
```
- **用途**: 从 EventHub 获取所有事件通道，订阅 OnRaised
- **调用者**: `PlayerDirector.OnWire()`
- **备注**: 事件缺失时 NRE（fail-fast）

### UnbindEvents()
```csharp
public void UnbindEvents()
```
- **用途**: 取消所有事件订阅，释放引用
- **调用者**: ModuleChild 生命周期（OnDisable 路径）

### ClearFrameSignals()
```csharp
internal void ClearFrameSignals()
```
- **用途**: 帧末重置所有边沿信号为 false
- **调用者**: `PlayerDirector.Evaluate()` 末尾

### IEventListener 实现
```csharp
void IEventListener.BindEvents()   → 委托给 public BindEvents()
void IEventListener.UnbindEvents() → 委托给 public UnbindEvents()
```
- **备注**: IEventListener 接口由 EventHub.OnEnable/OnDisable 驱动

## Event Handler 模式

所有事件 handler 遵循相同模式：
```csharp
private void OnEquip1() => Equip1Requested = equip1Event.IsRequested;
```
- 按钮型事件读取 `IsRequested`（边沿触发）
- 无业务逻辑——只做帧状态缓存，由 PlayerDirector 消费

## 设计决策

| 决策 | 原因 |
|------|------|
| 纯类（非 MonoBehaviour） | 不依赖 GameObject 生命周期，PlayerDirector 控制创建/销毁 |
| IEventListener 实现 | EventHub.OnEnable 生命周期驱动订阅，统一入口 |
| 帧状态缓存模式 | 事件在输入线程触发，缓存在主线程 Evaluate 消费 |
| SCameraSnapshot 仍用 EventDispatcher | CameraService 尚未迁移到 SO Event Channel |
| 无空值守卫（fail-fast） | 事件缺失时 NRE 提前暴露配置问题 |

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| SCameraSnapshot → EventHub | 待做 | CameraService SO 化后 |
| EventDispatcherService 完全移除 | 待做 | CameraService 迁移完成后 |
