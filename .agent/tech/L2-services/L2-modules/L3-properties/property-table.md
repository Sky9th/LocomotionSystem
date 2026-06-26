# PropertyTable — 运行时属性平表

> `L3_Properties/Instance/PropertyTable.cs` · 技术文档 · 2026-06-26
> **Last Verified**: 2026-06-26 | **Verification**: Renamed from EntityProperties. All referenced files exist.

## 层级定位

L3 运行时层。每个实体实例的最终属性表。构造时一次性全解析（Tree 结构 + DefSO.OverridesJson → 三值合并），运行时提供 Set / Modify / Load / Tick / Snapshot / Guard / 事件。

由 PropertyComponent 内部持有，外部不可直接访问。

## 调用链

```
被谁调:
  PropertyComponent.Awake()        → new PropertyTable(_def)
  PropertyComponent.Update()       → .Tick(dt)
  PropertyComponent.Set/Modify/... → 全部代理到 PropertyTable

调谁:
  PropertyTreeSO.ResolveStructure() → 构造时获取 Path→Def 映射
  PropertyDefSO                     → 读取 Min/Max/Default/Type
  FloatState                        → 内部创建和管理
  FloatModifier                     → AddModifier/RemoveModifiers
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | PropertyTreeSO | 构造时解析结构 |
| 依赖 | PropertyDefSO | 读取类型/约束/默认值 |
| 依赖 | FloatState | 管理 Float 运行时实例 |
| 依赖 | FloatModifier | 持久修改器管理 |
| 被消费 | PropertyComponent | 唯一消费方，外部不可见 |

## 内部结构

```
PropertyTable
  ├── _structure: Dictionary<string, PropertyDefSO>     ← Path→Def
  ├── _floats / _ints / _bools / _strings / _tagLists / _assetRefs / _assetRefLists
  │                                                       ← 按类型分桶的值存储
  ├── _floatStates: Dictionary<string, FloatState>        ← Float 运行时
  ├── _guards: Dictionary<string, List<GuardEntry>>       ← 修改前拦截
  └── _modifiers: Dictionary<string, List<FloatModifier>> ← 持久修改器索引
```

## 构造

### PropertyTable(PropertyPresetSO)
```csharp
public PropertyTable(PropertyPresetSO def)
```
- **用途**: 标准构造路径。从 PropertyPresetSO 读取 Template + OverridesJson
- **调用者**: PropertyComponent.Awake()

### PropertyTable(PropertyTreeSO, string)
```csharp
public PropertyTable(PropertyTreeSO tree, string overridesJson = null)
```
- **用途**: 直接构造（无 PropertyPresetSO 时）。测试/程序化生成用
- **调用者**: 工厂、测试代码

构造流程：
1. `Tree.ResolveStructure()` → `_structure` (Path→Def)
2. 解析 OverridesJson → Path→RawString 字典
3. 遍历 _structure：override 有值则校验+存入，无则取 Def.Default
4. `InitFloatStates()`：对存在伴生 Rate 的 Float 创建 FloatState

## 公开方法

### Get — 读取

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
- **用途**: 读取属性值。GetFloat 优先返回 FloatState.Current，无则返回静态值
- **消费者**: PropertyComponent 代理

### Set / Modify / Load — 修改

```csharp
public void Set(string path, object value)
```
- **用途**: 设值，统一入口。内部按 PropertyDefSO.Type 分发校验（Float 钳制、Int 钳制、AssetRef 解析 GUID 等）
- **流程**: 取 Def → 类型校验 → Guard 拦截 → 写值 → 事件广播

```csharp
public void Modify(string path, float delta)
```
- **用途**: Float 增量快捷方式。等价于 `Set(path, GetFloat(path) + delta)`

```csharp
public void Load(Dictionary<string, object> values)
```
- **用途**: 全量设置（读档/重生）。与 Set 同一套内部分发逻辑
- **区别**: 跳过 Guard、不逐条触发事件、不清除 Modifier

### 持久修改器

```csharp
public void AddModifier(FloatModifier mod)
public void RemoveModifiers(object owner)
```
- **用途**: 注入/移除持久帧级修改器。按 Owner 批量移除
- **备注**: 首次 AddModifier 时如果 path 无 FloatState，自动懒创建

### Guard 拦截

```csharp
public void AddGuard(string path, Func<float, float, bool> validate, object owner)
public void RemoveGuards(object owner)
```
- **用途**: 注册修改前拦截器。validate(oldValue, newValue) → 返回 false 阻止修改
- **使用场景**: "禁止治疗" Debuff、"伤害免疫" Buff

### Tick

```csharp
public void Tick(float dt)
```
- **用途**: 驱动所有 FloatState 的消耗/恢复/Modifier 计算
- **调用者**: PropertyComponent.Update()

### 快照

```csharp
public Dictionary<string, FloatSnapshot> GetFloatSnapshot()
```
- **用途**: 返回所有 Float 属性的 (Current, Max) 快照。UI 轮询用
- **备注**: 包含有 FloatState 的动态属性和无 FloatState 的静态 Float

## 事件

```csharp
public event Action<string, float, float> OnFloatChanged;   // path, old, new
public event Action<string> OnZero;                          // path 到达 Min
public event Action<string> OnMax;                           // path 到达 Max
public event Action<string, object, object> OnPropertyChanged; // path, old, new (任意类型)
```
- **用途**: PropertyComponent 代理给外部订阅者

## 伴生属性推断

构造时对每个 Float 属性，检查同父文件夹下是否存在同名 + 后缀的伴生属性：

| 伴生命名 | 含义 | 作用 |
|---------|------|------|
| `{name}ConsumeRate` | 每秒消耗量 | 存在 → 创建 FloatState，isConsumable=true |
| `{name}ConsumeInterval` | 消耗间隔（秒），缺省=0 每帧 | Tick 间隔控制 |
| `{name}RestoreRate` | 每秒恢复量 | 存在 → 创建 FloatState，isRestorable=true |
| `{name}RestoreInterval` | 恢复间隔（秒），缺省=0 每帧 | Tick 间隔控制 |

两者都不存在 → 不创建 FloatState（该属性保持静态，直到首次 Modify 或 AddModifier 时懒创建）。

## 设计决策

| 决策 | 原因 |
|------|------|
| 构造时一次性全解析 | 运行时读取零开销，不回溯 Schema |
| Set 统一入口 + 内部分发 | 消费者不感知类型差异 |
| FloatState 伴生 Rate 时预创建 | Tick 驱动的属性必须每帧计算 |
| Load 跳过 Guard/事件 | 存档数据可信，不需要逐条校验和广播 |
| _resolved 按类型分桶 | 避免 object 装箱，类型安全 |

## FloatSnapshot

```csharp
public struct FloatSnapshot
{
    public float Current;
    public float Max;
    public float Normalized => Max > 0f ? Current / Max : 0f;
}
```
- **用途**: 帧级快照条目。用于 UI 显示

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| AssetRef 运行时 GUID 解析 | 仅 Editor 实现 | Addressables 或 Runtime GUID 表 | 构建管线 |
| Save/Load 序列化 | 待设计 | 存档系统 | 设计讨论 |
