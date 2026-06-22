# Module 系统 · 架构参考

> `L1_Core/Modules/` — ModuleHub / ModuleChildMono / ModuleChild / ModuleRegistry / IModuleChild

## 类型

### ModuleHub — 父节点

```csharp
public abstract class ModuleHub : MonoBehaviour
{
    private ModuleRegistry _registry;
    internal ModuleRegistry Registry => _registry ??= new ModuleRegistry();

    protected virtual void Awake()
    {
        foreach (var child in GetComponentsInChildren<ModuleChildMono>(includeInactive: true))
        {
            var owner = child.GetComponentInParent<ModuleHub>(includeInactive: true);
            if (owner == this)
                Registry.Register(child);
        }
        Registry.OnAssembleAll();
    }

    protected virtual void Start()
    {
        Registry.OnWireAll();
    }
}
```

- 持有 `Registry`（惰性创建 `??=`）
- 在 Awake 末尾搜索 ModuleChildMono 子模块、注册、调 OnAssembleAll
- 在 Start 调 OnWireAll
- **不实现** IModuleChild，没有 OnAssemble / OnWire

### ModuleChildMono — MB 子节点

```csharp
public abstract class ModuleChildMono : MonoBehaviour, IModuleChild
{
    public virtual void OnAssemble() { }
    public virtual void OnWire() { }
}
```

- 实现 IModuleChild
- Awake 无基类逻辑（留给子类做仅依赖自身序列化字段的 setup）
- 不自注册。由父 Hub 搜到并 Register

### ModuleChild — C# 子节点

```csharp
public abstract class ModuleChild : IModuleChild
{
    protected ModuleChild(ModuleRegistry parent)
    {
        parent.Register(this);
    }

    public virtual void OnAssemble() { }
    public virtual void OnWire() { }
}
```

- 实现 IModuleChild
- 构造函数自注册到父 Registry
- 没有 MonoBehaviour（无 Awake / OnEnable / OnDisable / OnDestroy）

### ModuleRegistry — 收集器

```csharp
public sealed class ModuleRegistry
{
    readonly List<IModuleChild> _modules = new();

    public int Count => _modules.Count;

    internal void Register(IModuleChild module)
    {
        if (!_modules.Contains(module))
            _modules.Add(module);
    }

    public void OnAssembleAll()
    {
        foreach (var m in _modules) m.OnAssemble();
    }

    public void OnWireAll()
    {
        foreach (var m in _modules) m.OnWire();
    }
}
```

### IModuleChild — 子模块接口

```csharp
public interface IModuleChild
{
    void OnAssemble();
    void OnWire();
}
```

位于 `L1_Core/Modules/`。

## 注册路径

```
ModuleHub.Awake
  │
  ├── [子类在 base.Awake 之前] new C# 子
  │       ModuleChild 构造 ──→ Registry.Register(this)
  │
  └── [基类 Awake 内] GetComponentsInChildren<ModuleChildMono>(includeInactive: true)
        │
        └── foreach child → GetComponentInParent<ModuleHub>(includeInactive: true)
              │
              ├── == this  → Registry.Register(child)
              └── != this  → 跳过（归属更近的 Hub）
```

## 嵌套 Hub 边界

```
CharacterActor (ModuleHub)              ← 根 Hub
├── EventHub (ModuleChildMono)          ← 归属 CharacterActor
└── AnimationBrain (ModuleHub)          ← 子 Hub，不在 CharacterActor.Registry 中
    ├── LocomotionDriver (ModuleChildMono)  ← 归属 AnimationBrain
    └── TraversalDriver (ModuleChildMono)   ← 归属 AnimationBrain
```

`GetComponentInParent<ModuleHub>` 保证每个 ModuleChildMono 归入最近的 Hub。子 Hub 不在父 Hub 的 Registry 中，两棵树生命周期独立。

## 子类用法

```
Hub 子类 Awake:
  1. SetupModel / ResolveComponents
  2. new C# 子（构造自注册）
  3. base.Awake()  ← 发现 MB 子 + OnAssembleAll

Hub 子类 Start:
  1. [可选] pre-wire（子 OnWire 依赖的共享资源）
  2. base.Start()  ← OnWireAll
  3. [可选] post-wire（依赖子 OnWire 结果的收尾）
```

## 约束

| # | 约束 |
|---|------|
| 1 | ModuleChildMono 不在 Awake 做初始化 — OnAssemble 可能在 Awake 之前调用 |
| 2 | ModuleChildMono 起始 enabled — disabled 组件 OnEnable 被跳过，OnWire 先于 OnEnable |
| 3 | Hub GO 起始 active — inactive GO 的 Start 被推迟，OnWire 延后 |
| 4 | 同级子不互相依赖 OnAssemble 顺序 — C# 先注册、MB 后注册 |
| 5 | OnAssemble 中创建孙子不保证其 OnAssemble 被调用 — OnAssembleAll 遍历快照 |

## 相关文档

- [module-lifecycle.md](module-lifecycle.md) — 生命周期时序
