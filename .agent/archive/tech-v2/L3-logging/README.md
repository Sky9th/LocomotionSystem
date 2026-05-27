# 08-logging · 日志系统

> LogChannel 驱动的多频道日志系统，支持级别过滤和可插拔输出器 (Appender)。集中管理所有游戏模块的日志输出。

## 架构

```
调用方 (Service / Component)
        │
        ▼
  LogChannel        ←  LogManager.GetChannel() 工厂创建/缓存
        │
        ├── IsEnabled(level) → 级别过滤 (Error 始终开出)
        │
        └── Emit() → LogManager.EmitInternal()
                          │
                          ▼
                    ILogAppender[]  ← 可插拔输出器列表
                          │
                          ├── ConsoleAppender  (默认注册，Unity Debug.Log)
                          └── 未来: FileAppender / NetworkAppender / ...
```

## 调用链

```
任何代码
  │
  ├── LogManager.GetChannel(name, threshold)     ← 全局入口，获取命名频道
  │     └── new LogChannel(name, threshold)      ← 首次创建并缓存
  │
  ├── channel.Debug("...") / Info(...) / Error(...)   ← 日志输出
  │     └── LogChannel.Emit(level, message)
  │           ├── IsEnabled(level)  → false 则直接返回
  │           └── LogManager.EmitInternal(level, channel, message, bypass=false)
  │                 └── foreach appender → appender.Append(level, channel, message)
  │
  └── channel.Always("...")                    ← 绕过级别检查
        └── LogManager.EmitInternal(..., bypass=true)
              └── foreach appender → appender.Append(...)  (不检查级别)

BaseService 构造时:
  Log = LogManager.GetChannel(GetType().Name, logLevel)  ← 自动分配频道

静态构造函数:
  LogManager 静态构造 → new ConsoleAppender(LogLevel.Trace)  ← 默认注册
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| LogManager | 所有模块 | 通过 GetChannel() 获取日志频道 |
| LogManager | BaseService | BaseService 构造时自动分配 Log 属性 |
| ConsoleAppender | 旧 Logger | 在 Append() 中调用 Logger.Log/LogWarning/LogError |
| ILogAppender | 第三方扩展 | 实现接口即可接入自定义输出器 |

## 设计决策

| 决策 | 原因 |
|------|------|
| LogChannel 独立实例 + 命名缓存 | 每个模块独立控制日志级别，LogManager 集中管理生命周期 |
| Error 始终输出，不受 Threshold 控制 | 错误日志不应被过滤，确保关键信息不丢失 |
| bypass 参数绕过级别检查 | `Always()` 方法用于强制输出启动/关闭等关键标记 |
| 双层过滤 (Channel.Threshold + GlobalThreshold) | 支持全局开关 + 频道细粒度控制 |
| ConsoleAppender 在静态构造时默认注册 | 开箱即用，无需额外配置 |
| 用 internal 限制 EmitInternal 访问 | 外部只能通过 LogChannel 方法输出，保证一致性 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| FileAppender — 文件输出器 | 待做 | ILogAppender | 旧 logging-system.md |
| NetworkAppender — 网络输出器 | 远期 | 网络模块 | 旧 logging-system.md |
| LogManager 线程安全支持 | 远期 | 多线程场景 | 旧 logging-system.md |
| RichText/颜色格式化支持 | 待做 | — | 旧 logging-system.md |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [logmanager.md](logmanager.md) | 全局管理器，频道缓存，Appender 列表，Emit 分发 |
| [logchannel.md](logchannel.md) | 日志频道，级别过滤，便捷输出方法 |
| [loglevel.md](loglevel.md) | 级别枚举定义 |
| [ilogappender.md](ilogappender.md) | 输出器接口 |
| [consoleappender.md](consoleappender.md) | Unity Debug.Log 输出实现 |
