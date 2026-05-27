# ILogAppender · 输出器接口

> `Utility/Logging/ILogAppender.cs` — 日志输出抽象，所有输出目标 (Console/File/Network) 需实现此接口

## 调用链

```
被谁调:
  LogManager.EmitInternal()     → 遍历 appenders 调 Append()
  LogManager.AddAppender()      → 注册到 appender 列表
  LogManager.RemoveAppender()   → 从列表移除

谁实现此接口:
  ConsoleAppender               ← 当前唯一的实现
  (未来) FileAppender            ← 文件输出
  (未来) NetworkAppender         ← 网络发送
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | LogManager | LogManager 持有 ILogAppender 列表并遍历调用 |
| 依赖方 | ConsoleAppender | 实现 ILogAppender 接口 |

## 接口定义

```csharp
public interface ILogAppender
{
    LogLevel Threshold { get; }
    void Append(LogLevel level, string channel, string message);
}
```

### Threshold
- **用途**: 输出器的级别门槛，低于此级别的消息不被输出
- **备注**: 与 LogManager.GlobalThreshold + LogChannel.Threshold 构成三级过滤

### Append()
```csharp
void Append(LogLevel level, string channel, string message)
```
- **用途**: 输出一条日志消息
- **参数**:
  - `level` — 日志级别
  - `channel` — 来源频道名称 (如 "GameContext")
  - `message` — 日志文本内容
- **调用者**: `LogManager.EmitInternal()` — 遍历所有 appender 依次调用
- **备注**: 实现类应在方法内部自行检查 level 与 Threshold 的关系

## 三级过滤顺序

```
LogChannel.IsEnabled()          → 频道级过滤 (level < Threshold 则丢弃)
  └── LogManager.GlobalThreshold  → 全局过滤 (level < GlobalThreshold 则丢弃)
        └── ILogAppender.Threshold → 输出器级过滤 (level < Threshold 则丢弃)
```

前两级在 LogChannel.Emit 中完成，第三级在 ILogAppender.Append() 实现中自行判断。
