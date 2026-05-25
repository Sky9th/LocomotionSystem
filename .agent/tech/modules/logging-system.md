# 日志系统

日期: 2026-05-22

## 概述

`26952f5` 将分散的 `Logger.Log()` / `Debug.Log` 调用统一为 `LogChannel` 架构。

## 组件

| 文件 | 职责 |
|------|------|
| `Utility/Logging/LogLevel.cs` | 枚举：Debug / Info / Warning / Error |
| `Utility/Logging/LogChannel.cs` | 带级别的日志通道，每个 Service 独立实例 |
| `Utility/Logging/LogManager.cs` | 静态工厂，`GetChannel(name, level)` 创建/缓存通道 |
| `Utility/Logging/ILogAppender.cs` | 输出接口 |
| `Utility/Logging/ConsoleAppender.cs` | 控制台输出实现 |

## 使用

```csharp
// BaseService 自动分配
protected LogChannel Log { get; private set; }
Log.Debug("...");
Log.Warning("...");
Log.Error("...");

// 非 Service (如 GameContext)
private LogChannel Log;
void Initialize() { Log = LogManager.GetChannel(nameof(GameContext), logLevel); }
```

## 设计要点

- 每个 Service/系统有命名通道，Inspector 可设 `logLevel`
- `ConsoleAppender` 在 `LogManager` 初始化时注册
- 可通过 `ILogAppender` 扩展输出目标（文件、网络等）
