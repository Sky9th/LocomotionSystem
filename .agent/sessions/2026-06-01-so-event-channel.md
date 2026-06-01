# 2026-06-01 — SO Event Channel 架构落成

## 做了什么

引入 SO Event Channel 事件架构，替代旧的 EventDispatcherService + InputActionHandler 模式。

- L1 基础设施：EventChannelBase、GameEvent<T>、EventChannels（MB 组件）、IEventListener 接口
- L2 输入事件：InputEvent<T> 泛型基类（Unity Input System 集成）、6 个具体输入事件类型
- L4 重构：PlayerInput 纯类（订阅+缓存）、PlayerDirector 纯类（只算 Intent）
- 删除旧代码：InputActionHandler + 14 个 IAPlayer* + PlayerInputReceiver（含 PutAction boxing）

## 关键设计决策

1. **EventChannels 是 GameObject 上唯一的事件资产持有者** — 所有模块通过 `Get<T>()` 获取引用
2. **IEventListener 是订阅约定** — 纯类实现此接口，由 EventChannels.OnEnable 驱动订阅
3. **PlayerDirector 回归纯计算** — 不碰事件、不碰 Dispatcher
4. **InputEvent<T> 代表"Unity 是发布者"** — 事件通道直接承接 Input System 回调
5. **新旧共存→全量替换** — 先建新系统并行运行，验证后删除旧代码

## 已知问题

- TEMP：`PlayerInput` 中鼠标位置仍通过 EventDispatcherService 订阅 SCameraSnapshot，待 CameraService 也改为 SO Event Channel
- StepRelay.asset 残留在 Data/Events/ 中，后续清理
