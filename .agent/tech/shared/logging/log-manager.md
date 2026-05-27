# LogManager
> **源文件**: `Assets/Scripts/Utility/Logging/LogManager.cs`

静态类，管理 LogChannel 注册、缓存和消息分发到所有 ILogAppender。

## 调用链

```
被谁调:
  BaseService 构造时        → GetChannel(GetType().Name, logLevel)
  LogChannel.Emit()         → EmitInternal()
  LogChannel.Always()       → EmitInternal(bypass: true)  ← 不检查级别
  任意代码                  → GetChannel(name) 获取频道
  任意代码                  → AddAppender / RemoveAppender 管理输出器
  ConsoleAppender           → (通过 EmitInternal 接收消息)

调谁:
  EmitInternal()            → 遍历 appenders → 各 ILogAppender.Append()
  静态构造函数               → new ConsoleAppender(LogLevel.Trace)  ← 默认注册
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | 所有模块 | 通过 GetChannel 获取日志频道 |
| 被依赖 | LogChannel | EmitInternal 是频道的最终输出终点 |
| 依赖 | ConsoleAppender | 静态构造默认注册；运行时通过 EmitInternal 回调 |
| 依赖 | ILogAppender | 持有 ILogAppender 列表，遍历分发 |

## 公开属性

```csharp
public static LogLevel GlobalThreshold { get; set; }   // 默认 LogLevel.Info
```
- **用途**: 全局日志级别下限，低于此级别的日志被所有频道过滤
- **备注**: 频道的 IsEnabled() 检查 `level < LogManager.GlobalThreshold` 时返回 false

## 方法

### GetChannel()
```csharp
public static LogChannel GetChannel(string name, LogLevel? defaultThreshold = null)
```
- **用途**: 获取或创建指定名称的日志频道
- **参数**:
  - `name` — 频道名称（如 "GameContext", "CharacterActor"）
  - `defaultThreshold` — 可选，首次创建时的默认级别，null 则使用 GlobalThreshold
- **返回**: 现有或新建的 LogChannel 实例
- **调用者**: 所有需要日志的模块，BaseService 构造时自动调用
- **备注**: 已有缓存时不检查 defaultThreshold，保留首次设置的级别

### AddAppender()
```csharp
public static void AddAppender(ILogAppender appender)
```
- **用途**: 向管理器注册一个输出器
- **参数**: `appender` — ILogAppender 实现实例
- **调用者**: 系统初始化代码，第三方扩展

### RemoveAppender()
```csharp
public static void RemoveAppender(ILogAppender appender)
```
- **用途**: 从管理器中移除已注册的输出器
- **参数**: `appender` — 要移除的 ILogAppender 实例
- **调用者**: 系统清理代码

### EmitInternal()
```csharp
internal static void EmitInternal(LogLevel level, string channel, string message, bool bypass)
```
- **用途**: 内部消息分发入口，遍历所有 appender 并调用 Append
- **参数**:
  - `level` — 日志级别
  - `channel` — 频道名称字符串
  - `message` — 日志消息文本
  - `bypass` — 为 true 时跳过 level 门槛检查 (Always 方法使用)
- **调用者**: `LogChannel.Emit()` 和 `LogChannel.Always()`
- **备注**: `internal` 可见性，外部只能通过 LogChannel 方法间接调用；bypass 时仍检查 `level >= appender.Threshold`

## 内部机制

### 静态构造函数
- `appenders.Add(new ConsoleAppender(LogLevel.Trace))` — 开箱即用，默认注册一个最低级别的控制台输出器

### 私有字段
```csharp
private static readonly Dictionary<string, LogChannel> channels = new();  // 频道缓存
private static readonly List<ILogAppender> appenders = new();            // 输出器列表
```
- channels 以频道名称为 Key，GetChannel 优先查缓存
- appenders 有序列表，EmitInternal 依次遍历每个 appender，当 `bypass || level >= appender.Threshold` 时调用 Append

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 线程安全支持 — 多线程并发 Enqueue | 远期 | 旧 logging-system.md |
| 配置文件支持 — 运行时调整 GlobalThreshold | 待做 | 旧 logging-system.md |
