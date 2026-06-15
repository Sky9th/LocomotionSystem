# CharacterActor · 角色主控

> `Character/Actor/CharacterActor.cs` — MonoBehaviour，角色组合根，每帧流水线入口

## 调用链

```
被谁调:
  Unity 生命周期 → Awake/OnEnable/Update/OnDisable

调谁:
  Awake:
    ├── Instantiate(modelPrefab, transform)        → 运行时实例化 Model（触发 AnimationBrain.Awake）
    ├── modelRoot = model?.transform                → 或回退到 serialized / transform
    ├── GetComponentInChildren<AnimationBrain>()    → 获取动画组件
    ├── new CharacterRig(transform, modelRoot)      → modelRoot 统一为 Model transform
    ├── characterAnimation?.SetRig(rig)             → 注入 Rig 到 Animation
    ├── GetComponent<EventHub>()                    → 事件总线
    ├── GetComponent<PathfindingAgent>()            → 寻路
    ├── new PlayerDirector(eventHub, modelRoot, this) → Director (L4_Director)
    ├── new CharacterKinematic(transform, modelRoot, rig)  → Kinematic 模块
    ├── new GroundLocomotion()                      → Locomotion 模块
    ├── GetComponent<PropertyAgent>()               → 属性
    ├── GetComponent<AbilityExecutor/Reactor>()    → 技能
    └── new CharacterCombat(...)                    → 战斗

  Update:
    ├── new CharacterFrameContext()                 → 创建帧上下文
    ├── director.Evaluate() → SCharacterIntent      → 取得意图
    ├── ctx.Intent = intent                         → 写入帧上下文
    ├── characterKinematic.Evaluate(...)  → ctx.Kinematic
    ├── locomotionSimulator.Simulate(...)  → ctx.Motor/Discrete
    ├── PlanarSpeed + LastKinematic/LastMotor/LastDiscrete 缓存
    ├── characterAnimation?.Apply(in ctx)           → 动画应用
    └── pathfindingAgent?.SyncLocomotion(...)       → 寻路同步
```

## 序列化字段

| 分组 | 字段 | 说明 |
|------|------|------|
| Identity | `isPlayer` | 是否玩家 |
| Config | `characterProfile` | CharacterProfileSO — locomotion + kinematic |
| Locomotion | `locomotionAnimationProfile` | LocomotionAnimationConfigSO |
| Ability | `skillSlot1/2` | 临时技能槽位 |
| **Audio** | `characterAudioConfig` | CharacterAudioConfigSO（从 CharacterAudio 迁入） |
| **Model** | `modelPrefab` | 运行时实例化的模型 Prefab |
| **Animation** | `animationAliasProfile` | AnimationClipSetSO（从 AnimationBrain 迁入） |
| | `forwardRootMotion` | Root motion 开关（从 AnimationBrain 迁入） |
| | `applyRootMotionRotation` | Root motion 旋转（从 AnimationBrain 迁入） |
| | `autoMatchAnimationSpeed` | 自动匹配动画速度（从 AnimationBrain 迁入） |
| **Animation Masks** | `upperBodyMask` 等 5 个 | AvatarMask（从 AnimationBrain 迁入） |
| Hierarchy | `modelRoot` | Model 的 Transform（保留 serialized 作为回退） |

> **v0.14.11**: AnimationBrain / LocomotionDriver / TraversalDriver / CharacterAudio 的配置资产全部集中到 CharacterActor。AnimationBrain 保留在 Model 子节点（需 OnAnimatorMove），但零序列化字段，运行时从 CharacterActor 读取。

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | CharacterRig | 构造注入，物理实体写入入口 |
| 依赖 | CharacterKinematic | Evaluate() 每帧计算运动学 |
| 依赖 | ILocomotionSimulator | Simulate() 每帧仿真移动 |
| 依赖 | AnimationBrain | Model 子节点上的运行时组件，从 CharacterActor 读配置 |
| 依赖 | PropertyAgent | 属性数值 |
| 依赖 | AbilityExecutor/Reactor | 技能执行 |
| 依赖 | CharacterAudio | 从 CharacterActor 读 audioConfig |
| 依赖 | ICharacterDirector (L4_Director) | Evaluate() 产出 SCharacterIntent |
| 依赖 | LocomotionDriver / TraversalDriver | 从 CharacterActor 读动画配置 |
| 被读取 | AnimationBrain / Drivers / Audio | 通过 internal accessor 读取配置字段 |

### Awake()
- **用途**: 实例化 Model → 构造所有子模块 → 建立引用链
- **调用者**: Unity 生命周期
- **备注**: modelPrefab 非 null 时 Instantiate 创建 Model 子节点（触发 AnimationBrain.Awake），否则回退到 serialized modelRoot

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| **Character 模块生命周期管理** — Awake 堆砌依赖 Unity 隐式顺序不可靠，需显式分阶段（Phase 0 预配置 / Phase 1 服务就绪 / Phase 2 子模块注入） | TODO | [CharacterActor.cs](../../../../../../Assets/Scripts/Services/Modules/L3_Character/Actor/CharacterActor.cs) |
| modelPrefab 需干净的单个角色 Prefab（当前 Soldier_Male_01 是 30 角色的全家桶） | TODO | 本次会话 |
| PropertyAgent._def 保持独立于 CharacterActor | ✅ 决策不迁移 | 本次会话 |
| 非玩家角色 (NPC) 支持 — AICharacterDirector | 远期 | L4_Director/AI/ 占位 |
| isPlayer 身份标记抽象 | TODO | 架构讨论待解决 |
