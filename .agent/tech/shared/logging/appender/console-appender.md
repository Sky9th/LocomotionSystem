# ConsoleAppender
> **源文件**: `Assets/Scripts/Utility/Logging/ConsoleAppender.cs`

ILogAppender 的 Unity Debug.Log 实现，将日志通过 Unity Console 输出。

## 调用链

```
被谁调:
  LogManager.EmitInternal()     → appender.Append(level, channel, message)
  LogManager 静态构造            → new ConsoleAppender(LogLevel.Trace)  ← 默认注册

调谁:
  Append()                      → Logger.Log() / LogWarning() / LogError()
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | LogManager | LogManager 持有实例并在 EmitInternal 中遍历 |
| 依赖 | Logger（旧 Logger, Utility） | 将格式化委托给 Logger.Log/LogWarning/LogError |

## 公开属性

```csharp
public LogLevel Threshold { get; }  // 输出器级别门槛 (构造时传入)
```

## 方法

### 构造函数
```csharp
public ConsoleAppender(LogLevel threshold = LogLevel.Trace)
```
- **用途**: 创建控制台输出器实例
- **参数**: `threshold` — 输出级别门槛，默认 LogLevel.Trace（不过滤）
- **调用者**: `LogManager` 静态构造默认注册
- **备注**: LogManager 默认以 LogLevel.Trace 级别注册一个实例

### Append()
```csharp
public void Append(LogLevel level, string channel, string message)
```
- **用途**: 将日志通过 Unity Console 输出
- **参数**:
  - `level` — 日志级别
  - `channel` — 频道名称
  - `message` — 日志文本
- **调用者**: `LogManager.EmitInternal()`
- **逻辑**:
  - `level < Threshold` → 直接返回（第三级过滤，二次确认）
  - Trace/Debug/Info → `Logger.Log(message, channel)`
  - Warning → `Logger.LogWarning(message, channel)`
  - Error/Fatal → `Logger.LogError(message, channel)`
- **备注**: 委托给旧 `Logger` 类，旧 Logger 负责格式化（添加级别标签、频道标签）并调用 `UnityEngine.Debug.Log`

## 三级过滤在本类的位置

`ConsoleAppender.Append()` 内部做第三级过滤: `if (level < Threshold) return;`。尽管 LogManager.EmitInternal 已在遍历时检查 `level >= appender.Threshold`，Append 内仍做一次防御性判断。

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| ConsoleAppender 直接格式化，不再依赖旧 Logger | 待做 | 架构规划 |
