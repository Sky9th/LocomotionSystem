# IEventListener

`Assets/Scripts/L1_Core/Events/IEventListener.cs`

## 状态：预留接口，当前无实现

> **代码验证 (2026-07-04)**: 全代码库搜索 `IEventListener`，仅在定义文件本身出现。**0 个类实现此接口，0 处调用 BindEvents/UnbindEvents。**
> **状态**: 预留，等待未来集成。

## 接口定义

```csharp
namespace RedDust.Core
{
    public interface IEventListener
    {
        void BindEvents();
        void UnbindEvents();
    }
}
```

## 方法

### BindEvents()
- **设计意图**: 集中注册事件监听（如调用 `EventHub.Get<T>().Register(handler)`）
- **当前调用者**: 无

### UnbindEvents()
- **设计意图**: 集中注销事件监听
- **当前调用者**: 无

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| ← 实现 | —— | 当前无任何类实现此接口 |

## 注

- EventHub 当前不持有 IEventListener 列表，不驱动 BindEvents/UnbindEvents
- 订阅方目前直接在自己生命周期内调用 `EventHub.Get<T>().Register()` / `.Unregister()`（如 TimeService.OnWire / CharacterCombat.OnDestroy）
- 此接口保留以备未来需要批量管理监听者的场景

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 首个实现类接入 | 待需求 | —— |
