# SO Event Channel — 事件架构设计

## 设计目标

用 ScriptableObject 物化的资产替代代码层的事件总线，让事件通道成为 Project 窗口里的独立文件。发布方和订阅方通过 Unity 原生引用追踪可见。

## 核心决策

### 1. 事件通道是 SO 资产，不是代码层总线

旧 EventDispatcherService 的问题是拓扑不可见——`Subscribe<T>` / `Publish<T>` 散落在代码里，改一个事件不知道影响谁。SO 资产天然可被 Find References 追踪。

### 2. Unity Input System 是真实发布者

InputEvent<T> 直接继承 EventChannelBase，内部绑定 InputAction 的 `performed` / `canceled` 回调。不需要中间 Relay 层。事件通道就是通道 + 适配器的统一体。

### 3. EventChannels 集中持有，IEventListener 分散订阅

一个 GameObject 一份 EventChannels（所有事件资产在这），任意个 IEventListener 纯类实现（各自独立订阅）。生命周期由 EventChannels.OnEnable 统一驱动。

### 4. PlayerDirector 回归纯计算

事件订阅、帧缓存移到 PlayerInput（纯类，IEventListener）。PlayerDirector 只实现 ICharacterDirector，读输入状态，算 SCharacterIntent。

## 未采用方案

| 方案 | 为什么没选 |
|------|-----------|
| static EventHub | 回到全局可变状态，退回 Dispatcher 模式 |
| 所有模块各自持有引用 | 事件数量增长后爆炸，Inspector 碎片化 |
| Zenject DI 注入 | 引入第三方框架，当前规模不需要 |

## 待解决

- SCameraSnapshot 仍通过 EventDispatcherService 传递（TEMP），待 CameraService 也改为 SO Event Channel
