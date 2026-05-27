# LogChannel · 日志频道

> `Utility/Logging/LogChannel.cs` — 带级别过滤的命名日志频道，每个模块/系统独立实例

## 调用链

```
被谁调:
  BaseService.Log.Debug("...") / Info(...) / Error(...)    ← 各模块的日志输出
  BaseService.Log.Always("...")                            ← 绕过级别过滤
  LogManager.GetChannel()                                   ← 间接调构造
  channel.GetChild("sub")                                  ← 父子频道

调谁:
  IsEnabled(level)         → 对比 this.Threshold + LogManager.GlobalThreshold
  Emit()                   → LogManager.EmitInternal(level, Name, message, bypass)
  Always()                 → LogManager.EmitInternal(..., bypass: true)
  GetChild()               → LogManager.GetChannel($"{Name}.{childName}")
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 被依赖 | 所有模块的日志输出 | 各模块持有 LogChannel 实例调用输出方法 |
| 依赖 | LogManager | Emit() 和 Always() 最终分发到 LogManager.EmitInternal |
| 依赖 | LogManager | GetChild 委托 LogManager.GetChannel |

## 公开属性

```csharp
public string Name { get; }              // 频道名称 (如 "GameContext", "CharacterActor")
public LogLevel Threshold { get; set; }  // 频道级别下限，可运行时调整
```

## 方法

### 构造函数
```csharp
internal LogChannel(string name, LogLevel threshold)
```
- **用途**: 创建命名频道
- **参数**: `name` — 频道名称；`threshold` — 初始日志级别
- **访问**: `internal` — 只能通过 `LogManager.GetChannel()` 创建
- **备注**: 不对外公开构造，确保频道统一由 LogManager 管理

### IsEnabled()
```csharp
public bool IsEnabled(LogLevel level)
```
- **用途**: 判断指定级别的日志是否应该输出
- **参数**: `level` — 要检查的日志级别
- **返回**: true 表示可以输出
- **逻辑**:
  - `level >= LogLevel.Error` → 始终 true (错误不拦截)
  - `level < Threshold` → false (频道级别过滤)
  - `level < LogManager.GlobalThreshold` → false (全局级别过滤)
  - 否则 → true
- **调用者**: `Emit()` 方法每次输出前调用

### GetChild()
```csharp
public LogChannel GetChild(string childName)
```
- **用途**: 创建或获取子频道，命名格式为 `{ParentName}.{ChildName}`
- **参数**: `childName` — 子频道名称 (如 "Input", "Movement")
- **返回**: 子频道 LogChannel 实例，继承父频道的默认 Threshold
- **调用者**: 模块内部子系统的日志需求
- **示例**: `Log.GetChild("Movement")` → 频道名 "GameContext.Movement"

### 便捷输出方法
```csharp
public void Trace(string message)
public void Debug(string message)
public void Info(string message)
public void Warning(string message)
public void Error(string message)
public void Fatal(string message)
```
- **用途**: 按级别输出日志，每个方法对应一个 LogLevel
- **参数**: `message` — 日志文本
- **调用者**: 各模块直接调用
- **备注**: 全部委托给私有的 `Emit()` 方法

### Always()
```csharp
public void Always(string message)
```
- **用途**: 绕过级别过滤强制输出 (Info 级别)
- **参数**: `message` — 日志文本
- **调用者**: 关键事件标记 (系统启动、关闭等)
- **备注**: 直接调 `LogManager.EmitInternal(..., bypass: true)`，不经过 IsEnabled 检查

## 内部方法

### Emit()
```csharp
private void Emit(LogLevel level, string message)
```
- **用途**: 内部输出核心，先 IsEnabled 检查再委托 LogManager
- **参数**: `level` — 日志级别；`message` — 日志文本
- **备注**: Trace/Debug/Info/Warning/Error/Fatal 六个公开方法都委托至此
