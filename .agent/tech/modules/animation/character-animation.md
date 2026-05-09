# Character Animation System – Copilot 指南

- 该 Instruction 应保持简洁和高度概括，具体实现细节、类型定义与迁移步骤请放在项目内其他文档中。
- 本说明用于约束新的角色级动画系统，而不是继续扩展 Locomotion 子系统内部的动画实现。

## 子系统定位
- Character Animation System 是角色级表现系统，负责统一驱动角色动画表现。
- 它不属于 Locomotion 子系统，而是与 Locomotion、Ability、Interaction、Movie 等系统并列的独立子系统。
- Locomotion、Traversal、Ability、Interaction、Movie 都只是它的输入源之一。
- 该系统的目标不是维护业务规则，而是统一处理动作请求的仲裁、翻译和播放。

## 核心职责
- 接收来自多个业务子系统的角色动作请求。
- 基于请求自身提供的规则进行通道占用、可中断性、可抢占性与播放资格仲裁。
- 将高层动作请求翻译为 Animancer 可执行的播放命令。
- 统一维护当前角色动画播放状态，并向外提供只读反馈。
- 负责 FullBody、UpperBody、Additive 等动画通道的分配与执行。
- 负责与 Root Motion 的接线，但 Root Motion 是否生效应由动作请求或系统策略明确声明。

## 非职责
- 不负责输入解释。
- 不负责 Locomotion 的离散状态计算。
- 不负责 Traversal 的环境检测与请求生成。
- 不负责 Ability 的技能条件判断、资源消耗、命中逻辑与冷却逻辑。
- 不负责保存某个具体技能、Traversal 或 Movie 的业务真相。
- 不允许通过集中式硬编码表维护大量具体动作之间的业务优先级关系。

## 输入边界
- Character Animation System 的输入应来自统一的角色动作请求接口，而不是直接来自裸动画资源或内部动画状态名。
- 典型输入来源包括：
  - Locomotion
  - Traversal
  - Ability
  - Interaction
  - Movie
  - Hit React
- 每个请求应由其所属业务系统自行构建，并由请求自身声明：
  - 使用的通道
  - 是否阻塞基础移动
  - 是否允许抢占当前动作
  - 是否允许被其他动作打断
  - 完成条件或完成判定方式
  - 播放表现所需的最小参数

## 输出边界
- Character Animation System 对外提供的是只读播放结果，而不是把内部执行细节暴露给业务系统。
- 推荐输出包括：
  - 当前通道正在播放的动作标识
  - 请求是否被接受
  - 请求是否正在播放
  - 请求是否完成
  - 请求是否被打断
  - 动画层快照或表现快照

## 架构约束
- 外部业务系统不得直接传入裸 AnimationClip 并要求系统立即播放。
- 外部业务系统不得直接依赖或驱动内部 Layer 的具体状态名。
- 外部业务系统不得直接操作 AnimancerLayer、BaseLayer 或类似内部执行对象。
- 业务系统只提出请求，不直接控制动画内部实现。
- 动画系统只做通用仲裁与表现翻译，不承担具体业务规则维护。
- 同一个角色可以同时存在多个请求，但是否并行生效由通道与请求自身规则共同决定。
- 某个请求是否能打断当前动作，应优先由请求自身规则决定，而不是由动画系统维护全局业务优先级表。

## 推荐抽象方向
- 建议为所有高层动作统一抽象角色动作请求接口。
- 推荐名称优先使用 CharacterActionRequest 语义，而不是直接使用 AnimationRequest。
- 动画系统更适合作为 Character Animation Director 或 Character Animation Controller，而不是继续沿用 Locomotion Presenter 的定位。
- Director 负责收集请求、做通用裁决、选择通道并驱动 Animancer。
- Layer、State、Alias 解析等均属于动画系统内部实现细节。

## 命名空间与目录建议
- 新系统应使用独立命名空间前缀：
  - Game.CharacterAnimation
- 不建议继续放在：
  - Game.Locomotion.Animation
- 建议目录结构保持与命名空间一致，示例：
  - Assets/Scripts/CharacterAnimation/
    - Core
    - Requests
    - Channels
    - Config
    - Director
    - Animancer
    - Outputs

## 与现有系统的关系
- Locomotion 继续负责基础运动状态与移动域快照输出。
- Traversal 继续负责环境动作的业务状态与生命周期。
- Ability 继续负责技能业务语义。
- Character Animation System 统一消费这些系统提供的动作请求或状态，并负责最终表现。
- 在迁移阶段，允许旧的 Locomotion 动画实现继续存在，但新增能力应优先接入独立的 Character Animation System，而不是继续向 LocomotionSystem 内部堆积动画状态。

## 开发原则
- 优先建立统一请求接口，再做具体动作接入。
- 优先让 Traversal 成为第一个接入新接口的系统，用于验证边界是否正确。
- 在未建立统一请求接口之前，避免继续在基础 Locomotion 动画层中添加与具体业务强耦合的新状态。
- 新增动作表现能力时，优先扩展请求模型与 Director，不优先扩展 BaseLocomotion 状态机。
- 始终保持业务决策与动画执行分离。

## 当前阶段建议
- 第一阶段只定义角色动作请求接口及相关基础枚举与上下文。
- 第二阶段建立独立的 Character Animation Director。
- 第三阶段让 Traversal 接入新接口。
- 第四阶段再考虑 Ability、Interaction、Movie 等系统的接入。
- 在完成上述迁移前，现有 Locomotion 动画实现只做必要维护，不继续扩展为通用角色动作系统。