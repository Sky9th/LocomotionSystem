# IInitializable · 递归树初始化协议

> `L1_Core/IInitializable.cs` — Unity Awake/Start 无序调用的补充，提供显式两阶段协调

## 完整生命周期（Unity 原生 + IInitializable 补充）

```

  ═══════════════════════════════════════════════════════════════════════════════
  Phase 0 — Instantiate
  ═══════════════════════════════════════════════════════════════════════════════

  GameObject.Instantiate(prefab)
    │
    └── Unity 引擎: 深度优先，实例化整棵 GameObject 树
        所有 MB 被创建，但 Awake 尚未调用


  ═══════════════════════════════════════════════════════════════════════════════
  Phase 1 — Awake（Unity 原生，深度优先，同层无序）
  ═══════════════════════════════════════════════════════════════════════════════

  ★ Unity 保证: 子节点的 Awake 在父节点 Awake 返回之前执行完毕
  ★ Unity 不保证: 同层兄弟节点之间的 Awake 顺序
  ★ 纯 C# 类不参与 —— 没有 Awake，只能被父节点手动 new

  Root.Awake()                              ◄── 引擎调用
    │
    ├── ChildA.Awake()                      ◄── 引擎自动
    │     └── GrandA1.Awake()               ◄── 引擎自动
    │
    ├── ChildB.Awake()                      ◄── 引擎自动（顺序不保证）
    │     ├── GrandB1.Awake()
    │     └── GrandB2.Awake()
    │
    ├── ChildC.Awake()
    │
    ├── SetupModel()          ─┐            ◄── 手动: 实例化 Model Prefab
    ├── ResolveComponents()   ─┤            ◄── 手动: GetComponent 收集引用
    │                          │
    └── OnAssemble()  ◄════════╣══════════════════════════════════════════
          │                    ┃            ┌─────────────────────────────┐
          │ ★ 所有子节点       ┃            │ IInitializable 补充 #1     │
          │   Awake 已完成     ┃            │                             │
          │ ★ 纯 C# 子模块     ┃            │ 父模块在 Awake 末尾显式调用  │
          │   此时安全构造      ┃            │ 深度优先递归构建子树          │
          │                    ┃            │ 返回时子树完整，引用全部非空  │
          │                    ┃            └─────────────────────────────┘
          ├── new CharacterRig(...)          ◄── 纯 C#，唯一创建点
          ├── new PlayerDirector(...)        ◄── 纯 C#
          ├── new CharacterKinematic(...)    ◄── 纯 C#
          ├── new GroundLocomotion()         ◄── 纯 C#
          ├── new CharacterCombat(...)       ◄── 纯 C#
          │
          ├── ChildA.OnAssemble()            ◄── 递归向下（如果 ChildA 是 IInitializable）
          │     └── GrandA1.OnAssemble()     ◄── 深度优先，直达叶子
          │
          ├── ChildB.OnAssemble()            ◄── 递归向下
          │
          └── SetRig → LocomotionDriver     ◄── 注入：传播到子驱动
          │
          └── return  ← 子树完整


  ═══════════════════════════════════════════════════════════════════════════════
  Phase 2 — OnEnable（Unity 原生，Awake 之后、Start 之前）
  ═══════════════════════════════════════════════════════════════════════════════

  ★ 仅在 GameObject 激活时调用
  ★ 多用于注册回调（对时序不敏感的轻量操作）

  Root.OnEnable() → ChildA.OnEnable() → ChildB.OnEnable() → ...

  ── 本次不在此阶段做任何 IInitializable 操作 ──


  ═══════════════════════════════════════════════════════════════════════════════
  Phase 3 — Start（Unity 原生，所有 Awake 完成后）
  ═══════════════════════════════════════════════════════════════════════════════

  ★ Unity 保证: 所有节点的 Awake 已全部执行完毕
  ★ Unity 不保证: Start 之间的调用顺序（和 Awake 一样无序）
  ★ 此时可以安全进行跨模块引用——对方的 Awake 肯定跑过了

  Root.Start()                              ◄── 引擎调用
    │
    ├── ChildA.Start()                      ◄── 引擎自动（无序）
    ├── ChildB.Start()                      ◄── 引擎自动
    ├── ChildC.Start()                      ◄── 引擎自动
    │
    └── OnWire()  ◄════════════════════════════════════════════════════
          │                    ┌─────────────────────────────────────────┐
          │ ★ 所有同级         │ IInitializable 补充 #2                │
          │   OnAssemble 完成  │                                         │
          │ ★ 所有 C# 子模块   │ 父模块在 Start 里显式调用                │
          │   已构造完毕        │ 深度优先递归通知子树                     │
          │                    │ 再跨同级连线                             │
          │                    │ 返回时全树可运转                          │
          │                    └─────────────────────────────────────────┘
          │
          ├── ChildA.OnWire()               ◄── 先递归通知子树
          │     └── GrandA1.OnWire()
          │
          ├── ChildB.OnWire()               ◄── 先递归通知子树
          │
          ├── combat.SubscribeEvents()      ◄── 再做自己的跨模块连线
          ├── agent.AddModifier(...)         ◄── 对方已 Wire，安全
          │
          └── return  ← 全树可运转


  ═══════════════════════════════════════════════════════════════════════════════
  Phase 4 — Update 循环（Unity 原生）
  ═══════════════════════════════════════════════════════════════════════════════

  Root.Update() → ChildA.Update() → ...

  逐帧运行。IInitializable 不参与此阶段。


  ═══════════════════════════════════════════════════════════════════════════════
  Phase 5 — OnDisable（Unity 原生——软暂停）
  ═══════════════════════════════════════════════════════════════════════════════

  ★ GameObject 被禁用或场景卸载时触发
  ★ 不是销毁！对象池 / SetActive(false) 都会走这里
  ★ 只做状态重置，不释放资源、不取消事件订阅

  Root.OnDisable()
    └── kinematic?.Reset()                  ◄── 重置运动学状态


  ═══════════════════════════════════════════════════════════════════════════════
  Phase 6 — OnDestroy（Unity 原生——硬销毁）
  ═══════════════════════════════════════════════════════════════════════════════

  ★ Unity 自动级联：父销毁 → 子全部销毁
  ★ IInitializable 不需要补充 —— 级联已经正确处理了整棵树的销毁
  ★ 会话级清理（Playing→MainMenu）走 IGameplaySessionHandler，不走这里

  Root.OnDestroy()
    ├── combat?.UnsubscribeEvents()
    ├── kinematic?.Reset()
    ├── ChildA.OnDestroy()                  ◄── Unity 自动级联
    ├── ChildB.OnDestroy()                  ◄── Unity 自动级联
    └── ChildC.OnDestroy()                  ◄── Unity 自动级联
```

## 对比：外部协调器驱动（Service）

```
  MB 模块用 Awake/Start 驱动（上图）。
  但 Service 不走这个——它由 GameService 手动协调：

  GameService.Bootstrap()
    │
    ├── foreach: service.Register(ctx)        ← 注入 GameContext
    ├── foreach: service.AttachDispatcher()   ← 注入事件总线（纯字段，无回调）
    │
    ├── foreach: service.OnAssemble()         ← ─── 递归装配
    │     └── OnDispatcherAttached() + OnSubscriptionsActivated()
    │
    ├── foreach: service.OnWire()             ← ─── 跨模块连线
    │     └── OnServicesReady()
    │
    └── Bootstrap 完成

  ▲ 没有 Awake/Start 的事——全是 GameService 显式调用，顺序 100% 可控
```

## 接口定义

```csharp
namespace RedDust.Core
{
    public interface IInitializable
    {
        void OnAssemble();   // 递归构建子树
        void OnWire();       // 跨同级连线
    }
}
```

## 调用链

```
被谁调:
  MB 模块自身       → Awake 末尾调 OnAssemble(), Start 里调 OnWire()
  GameService       → 逐个调 Service.OnAssemble(), Service.OnWire()
  父模块             → 对子模块递归调用 OnAssemble(), OnWire()

调谁:
  子模块             → 递归向下传播
  纯 C# 子模块       → OnAssemble 中 new, OnWire 中连线
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | 无 | 纯协议 |
| 被实现 | Module, ModuleBehaviour, ModuleComponent | Module 系统（L1 工具封装） |
| 被实现 | BaseService | GameService 驱动（后续 PR） |
| 被实现 | CharacterActor | 通过 ModuleBehaviour 间接实现 |
| 被实现 | 未来模块（装备、AI Director） | 统一初始化契约 |

## 与 Module 系统的关系

`IInitializable` 是协议，`Module` / `ModuleBehaviour` / `ModuleComponent` 是工具封装。一般不需要直接实现 `IInitializable`——继承对应的 Module 基类即可。详见 [module-system.md](module-system.md)。

## BaseService 映射

| IInitializable | BaseService |
|---------------|-------------|
| `OnAssemble()` | ① `OnDispatcherAttached()` + ② `OnSubscriptionsActivated()` |
| `OnWire()` | ③ `OnServicesReady()` |

注：`OnRegister(context)` 和 `AttachDispatcher()` 在 `OnAssemble()` 之前由 GameService 单独调用。

## CharacterActor 映射

| IInitializable | CharacterActor |
|---------------|---------------|
| `OnAssemble()` | `new CharacterRig` → `SetRig` → `new` 所有 C# 子系统 |
| `OnWire()` | `combat.SubscribeEvents()` + `agent.AddModifier()` |

## 不是什么

- **不是状态机** — 不追踪进度，不做运行时校验
- **不替代 Awake/Start/OnDestroy** — 是它们的补充
- **不定义 Teardown** — Unity Destroy 级联 + `IGameplaySessionHandler` 已覆盖清理
