# PropertyComponent — 属性门面

> `L3_Properties/PropertyComponent.cs` · 技术文档 · 2026-06-10

## 层级定位

L3 MonoBehaviour 门面。挂载在 GameObject 上，是所有属性操作的唯一入口。其他子系统通过 `GetComponent<PropertyComponent>()` 获取引用，不直接接触 EntityProperties、EntityDefSO、PropertyTreeSO。

## 调用链

```
被谁调:
  CharacterActor / GearInstance / BuildingInstance  → GetComponent<PropertyComponent>()
  所有消费者系统 → props.GetFloat / props.Set / props.AddModifier ...

调谁:
  EntityProperties   → 全部 API 代理（Awake 构造，Update Tick）
  FloatModifier      → AddModifier 透传
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | EntityDefSO | Inspector _def 引用 |
| 依赖 | EntityProperties | 内部持有，构造和管理 |
| 被消费 | 所有需要属性的子系统 | CharacterCombat, VitalsOverlay, Physiology, UI 等 |
| 被消费 | CharacterActor | 同 GameObject 上的兄弟组件 |

## 公开属性

### 读取
```csharp
public float GetFloat(string path)
public int GetInt(string path)
public bool GetBool(string path)
public string GetString(string path)
public string GetTag(string path)
public string[] GetTagList(string path)
public T GetAsset<T>(string path) where T : UnityEngine.Object
public float GetMin(string path)
public float GetMax(string path)
public bool Has(string path)
```
- **用途**: 读取属性值。全部代理到 EntityProperties

### 修改
```csharp
public void Set(string path, object value)
```
- **用途**: 设值，统一入口。走 Guard 拦截 + 事件广播

```csharp
public void Modify(string path, float delta)
```
- **用途**: Float 增量快捷方式

```csharp
public void Load(Dictionary<string, object> values)
```
- **用途**: 全量设置（读档用）。跳过 Guard、不触发事件

### 持久修改器
```csharp
public void AddModifier(FloatModifier mod)
public void RemoveModifiers(object owner)
```
- **用途**: 注入/移除 FloatModifier。按 Owner 批量移除

### Guard 拦截
```csharp
public void AddGuard(string path, Func<float, float, bool> validate, object owner)
public void RemoveGuards(object owner)
```
- **用途**: 注册/移除修改前拦截器

### 事件
```csharp
public event Action<string, float, float> OnFloatChanged;
public event Action<string> OnZero;
public event Action<string> OnMax;
public event Action<string, object, object> OnPropertyChanged;
```
- **用途**: 修改后广播。代理自 EntityProperties

### 快照
```csharp
public Dictionary<string, FloatSnapshot> GetFloatSnapshot()
```
- **用途**: 帧级浮点属性快照，UI 轮询用

## 内部机制

### Awake
```csharp
private void Awake()
```
- 从 `_def` (EntityDefSO) 构建 `EntityProperties`
- _def 为 null 时 LogError

### Update
```csharp
private void Update()
```
- 调用 `_props.Tick(Time.deltaTime)` 驱动 FloatState 消耗/恢复
- **不依赖外部驱动**——PropertyComponent 自管理 Tick

## 使用规则

- **同一 GameObject 只有一个 PropertyComponent**（`[DisallowMultipleComponent]`）
- **其他组件不直接引用 EntityProperties**——全部通过 PropertyComponent 的公开 API
- **不直接访问 _def / _props**——这两个字段是 private
- **事件订阅在 OnEnable 中，取消在 OnDisable 中**——避免泄漏
- **Load 方法仅用于读档/重生**——正常运行时修改走 Set/Modify

## 设计决策

| 决策 | 原因 |
|------|------|
| MonoBehaviour 而非纯 C# 类 | 挂载在 GameObject 上，生命周期由 Unity 管理，其他组件通过 GetComponent 获取 |
| 代理全部 API | EntityProperties 对子系统不可见，替换内部实现不影响消费者 |
| 自驱动 Tick | 不依赖 CharacterActor 每帧调用，降低耦合 |
| 事件双重代理（add/remove 中判 null） | _props 在 Awake 后才创建，订阅可能在此之前发生 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| 多 EntityDefSO 动态切换 | 远期 | 角色状态机 | — |
| 网络同步（RPC 桥接） | 远期 | 网络层 | — |
