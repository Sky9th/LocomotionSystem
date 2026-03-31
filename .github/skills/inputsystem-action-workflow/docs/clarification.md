# 前置确认

## 何时必须先确认

在以下信息不明确时，不应直接开始实现，而应先做最小必要确认：

- 不清楚这个 Action 属于 Player、UI 还是 System。
- 不清楚它是直接事件型，还是 Locomotion 聚合型。
- 不清楚它是一次性触发，还是按下/松开持续状态。
- 不清楚目标 Action Map 应该复用现有 map，还是需要新增 map。
- 不清楚应该支持哪些输入设备。
- 不清楚是否需要 Interaction，例如 Hold、Tap、MultiTap。
- 不清楚下游消费者是谁，导致无法判断 payload 应该长什么样。
- 不清楚这个 Action 是否需要受 EGameState 限制。

如果这些问题都能从现有代码、Input Actions 资产或用户请求中直接推断，就不要再追问。

## 推荐的最小确认问题

### 输入语义

1. 这个 Action 的目标行为是什么。
2. 它属于 Player、UI 还是 System。
3. 它是一次性触发，还是要区分按下、按住、松开。
4. 它是否需要进入 locomotion 聚合输入。

### Unity Input System 配置

1. 这个 Action 应该放进哪个 Action Map。
2. 需要支持哪些设备，例如 Keyboard&Mouse、Gamepad。
3. 是否需要 Interaction，例如 Hold 或 Tap。
4. 是否需要 composite binding。

### 运行时接线

1. 谁来消费这个 payload。
2. 是否已经存在相近的 IAction 可以复用。
3. 是否需要限制在某个 EGameState 下才生效。

## 提问策略

- 优先问能决定实现分支的问题。
- 一次只问最少数量的问题，足够开工即可。
- 能从仓库中读出来的事实，不要问用户。
- 能用默认模式解决的，不要问用户。
- 如果只是命名风格或资源放置位置，可以直接沿用项目现有约定。

## 默认假设

当用户没有额外说明且仓库内已有成熟模式时，优先采用以下默认假设：

- 玩家常规动作默认放在 Player Action Map。
- locomotion 相关输入默认按现有 LocomotionInputModule 聚合模式接入。
- Button 输入默认优先使用 context.ReadValueAsButton()。
- 一次性命令默认只处理 context.performed。
- handler 资产默认沿用现有目录结构放置。
- 游戏内控制默认只在 EGameState.Playing 下启用，除非现有同类 Action 另有模式。