# Module Lifecycle · 统一生命周期约定

> `L1_Core/Modules/` — 树形模块的生命周期时序

## 生命周期

```
Unity 阶段               ModuleHub (父)                  ModuleChildMono / ModuleChild (子)
────────────────         ──────────────                  ──────────────────────────────

Awake                    [子类] new C# 子 (构造 Register)
                         [基类] GetComponentsInChildren<ModuleChildMono>
                                  判主: GetComponentInParent<ModuleHub> == this
                                  真 → Register
                         [基类] Registry.OnAssembleAll()
                                  遍历快照 ─────────────────→ OnAssemble()
                                                               收集引用, 创建孙子


OnEnable                 [基类] 不干预子模块                OnEnable()
                                                           启用自身运行态
                                                           不重新收集引用/订阅事件


Start                    [基类] Registry.OnWireAll() ────→ OnWire()
                         [子类] 自身收尾                     跨模块连线, 事件订阅


OnDisable                [基类] 不干预子模块                重置运行时状态
                                                           不取消事件订阅


OnDestroy                [基类] 不干预子模块                取消事件订阅, 释放资源
```

> "不干预" = 基类不遍历子模块统一清理。Hub 子类自身资源不受限。

## 时序保证

| # | 保证 |
|---|------|
| 1 | OnAssemble 在 OnEnable 之前 |
| 2 | OnWire 在 OnAssemble 之后 |

## 约束

| # | 约束 | 原因 |
|---|------|------|
| 1 | ModuleChildMono 不在 Awake 做初始化，放 OnAssemble | Hub.Awake 末尾调 OnAssembleAll，同 GO 兄弟 Awake 顺序不确定——OnAssemble 可能在自身 Awake 之前被调用，此时字段为 null |
| 2 | 同级子不互相依赖 OnAssemble 顺序 | C# 子在 base.Awake 之前注册，MB 子在 GetComponentsInChildren 中注册。C# 先于 MB，且 MB 间顺序也不可控 |
| 3 | OnAssemble 中创建孙子不保证其 OnAssemble 被调用 | OnAssembleAll 遍历的是调用前的注册快照，遍历期间新注册的不在本轮迭代 |
| 4 | ModuleChildMono 起始 enabled | Unity 对 disabled 组件跳过 OnEnable，OnWire 将先于 OnEnable 触发 |
| 5 | Hub GO 起始 active | inactive 的 GO 其 Start 被推迟到激活时，OnWire 无限延后 |

## 职责边界

每个生命周期阶段允许和禁止的操作：

| 阶段 | 职责 | 允许 | 禁止 |
|------|------|------|------|
| **Awake** (Hub) | 扫描注册 | `GetComponentsInChildren` 扫描 MB 子节点 → Register → `OnAssembleAll` | — |
| **Awake** (Hub 子类 pre-base) | 前置组装 | 创建 C# 子模块（构造自注册）、`AddComponent` MB 子组件 | — |
| **Awake** (ChildMono) | 无需使用 | 基类为空，初始化放 OnAssemble | 在此做逻辑初始化（放 OnAssemble） |
| **OnAssemble** | 组装 | 收集自身引用（GetComponent）、创建孙子模块、注册到全局容器 | 解析其他模块（TryResolveService）、订阅事件、执行业务逻辑 |
| **Start** (Hub 子类 pre-base) | 前置连线 | 构建子 OnWire 依赖的共享资源 | — |
| **Start** (Hub 子类 post-base) | 启动 | Publish 初始状态 | — |
| **OnWire** | 连线 | 解析其他模块、订阅事件 | Publish 初始状态（放 Start） |
| **OnEnable** | 激活 | 启用自身运行态 | 收集引用、订阅事件 |
| **OnDisable** | 休眠 | 重置自身运行时状态 | 取消事件订阅（放 OnDestroy） |
| **OnDestroy** | 销毁 | 取消所有事件订阅、释放资源 | — |
