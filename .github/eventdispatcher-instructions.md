# EventDispatcher 设计说明

## 核心定位
EventDispatcher 负责在各子系统之间传递解耦事件，是 GameManager 初始化阶段挂载/注册的公共服务。所有订阅者以 `Action<TPayload, MetaStruct>` 形式接收消息，第二个参数由 Dispatcher 统一生成，包含时间戳与帧索引，确保全局一致的时序信息。

## 发布/订阅约定
- `Subscribe<TPayload>(Action<TPayload, MetaStruct> handler)`：注册处理器，禁止传入 null；同一处理器不会被重复添加。
- `Unsubscribe<TPayload>(Action<TPayload, MetaStruct> handler)`：移除处理器；当该 payload 类型无监听者时从字典中删除。
- `Publish<TPayload>(TPayload payload)`：
  - 在调用帧构造新的 `MetaStruct { Timestamp = Time.time, FrameIndex = (uint)Time.frameCount }`。
  - 查找对应 payload 类型的监听者快照并逐个调用 `handler(payload, meta)`。
  - 如果 payload 类型当前无人订阅则直接返回，不产生 GC 分配。
- `Clear()`：清空内部字典，多用于 GameManager 退出或重置流程。

## 使用指南
1. **单一入口**：除 GameManager 注入外，其他脚本应通过 GameContext 或显式依赖注入获取 Dispatcher；不要在场景中额外创建实例。
2. **无状态 payload**：建议发布不可变 Struct 或 DTO，事件内不要持有场景引用；需要上下文时改为在 payload 中传递 ID 或由 GameContext 查询。
3. **线程与执行顺序**：Dispatcher 设计为主线程使用；所有回调均在调用 `Publish` 的同一帧执行，不做跨帧缓存。
4. **避免深度层级**：Subscribe/Unsubscribe 仅维护委托列表，不支持优先级。若需顺序保证，由调用方在自身层面包装。
5. **MetaStruct 信任**：消费方禁止自行生成时间戳；所有统计/测量统一依赖传入的 `MetaStruct`，以便分析工具可复用。

## 调试建议
- 在需要时可在调用 `Publish` 前添加条件编译的 `Debug.Log` 输出 payload 内容，但正式版本应关闭以避免刷屏。
- 若遇到订阅未移除导致的多次回调，可在 `Unsubscribe` 前后加 `Debug.Assert` 验证。

## 扩展方向
- 如需支持一次性订阅或优先级，可在当前实现上引入包装器（例如 `OneShotListener<T>`），但仍保持核心 API 简洁。
- 若未来接入多线程，可在外围增加队列，将 `Publish` 请求推入主线程再执行现有逻辑。
