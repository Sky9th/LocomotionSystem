# LogLevel
> **源文件**: `Assets/Scripts/Utility/Logging/LogLevel.cs`

枚举，定义日志的严重性级别。

## 定义

```csharp
public enum LogLevel : byte
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5
}
```

## 级别说明

| 级别 | 值 | 用途 | 默认是否输出 |
|------|-----|------|------------|
| Trace | 0 | 最详细跟踪信息，框架内部调用细节 | 否（低于默认 GlobalThreshold=Info） |
| Debug | 1 | 开发调试信息，变量值、状态变化 | 否 |
| Info | 2 | 常规信息，系统状态变更、关键流程节点 | 是 |
| Warning | 3 | 警告，潜在问题但不影响功能 | 是 |
| Error | 4 | 错误，功能受损但系统可继续运行 | 是（不受阈值限制） |
| Fatal | 5 | 致命错误，系统不可继续运行 | 是（不受阈值限制） |

## 使用规则

- **Error 和 Fatal 不受阈值限制**: `LogChannel.IsEnabled` 中 `level >= LogLevel.Error` 始终返回 true
- **比较语义**: 数值越大越严重，`level >= Threshold` 意味着"达到或超过阈值级别才输出"
- **字节存储**: 使用 `byte` 枚举，节省内存占用

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 无具体规划。 | — | — |

## 调用者

- `LogChannel.Threshold` — 频道级别门槛
- `LogManager.GlobalThreshold` — 全局级别门槛
- `LogChannel.IsEnabled()` — 双重过滤判断
- `ConsoleAppender.Append()` — 按级别选择输出方法
- `LogManager.EmitInternal()` — 与 appender.Threshold 比较决定是否分发
