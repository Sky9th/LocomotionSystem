# Property System — 实现计划

> 2026-06-09 · 五阶段实施，每阶段可独立测试

## 零、文件结构

```
Assets/Scripts/Services/Modules/L3_Properties/
├── PropertyType.cs                # enum
├── PropertyValue.cs               # struct
├── Definition/
│   ├── PropertyDefSO.cs           # ScriptableObject
│   └── PropertyDefinitionRegistry.cs  # 全局注册表
├── Tree/
│   ├── PropertyNode.cs            # [Serializable] 树节点
│   ├── PropertyTreeContainer.cs   # [Serializable] JSON 序列化包装
│   └── PropertyTreeSO.cs          # ScriptableObject 模板
├── Instance/
│   └── ResolvedPropertyBag.cs     # 值对象
└── Editor/
    └── PropertyTreeEditorWindow.cs

Assets/Data/Properties/
├── Definitions/                   # PropertyDefSO 资产
│   ├── ATK.asset
│   ├── Weight.asset
│   ├── Icon.asset
│   └── ...
└── Trees/                         # PropertyTreeSO 资产
    ├── WeaponBase.asset
    ├── Firearm.asset
    ├── Pistol.asset
    └── ...
```

## 一、Phase 1：核心类型（无 Unity 依赖）

### 1.1 PropertyType.cs

```csharp
namespace RedDust.Properties
{
    public enum PropertyType
    {
        Float,
        Int,
        Bool,
        String,
        GameplayTag,
        GameplayTagList,
        AssetRef,
        AssetRefList
    }
}
```

### 1.2 PropertyValue.cs

```csharp
namespace RedDust.Properties
{
    [Serializable]
    public struct PropertyValue
    {
        public PropertyType Type;
        public string SerializedValue;

        public bool HasValue => !string.IsNullOrEmpty(SerializedValue);
        public static PropertyValue None => new() { SerializedValue = null };
    }
}
```

**验证**：编译通过。两个类型无外部依赖。

---

## 二、Phase 2：定义层

### 2.1 PropertyDefSO.cs

```csharp
namespace RedDust.Properties
{
    [CreateAssetMenu(menuName = "RedDust/Properties/Property Definition")]
    public class PropertyDefSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public PropertyType Type;
        public bool IsDeprecated;

        [Header("Float")]
        public float Min;
        public float Max = 100f;
        public float DefaultFloat = 100f;

        [Header("Int")]
        public int MinInt;
        public int MaxInt = 100;
        public int DefaultInt;

        [Header("Bool")]
        public bool DefaultBool;

        [Header("String")]
        public string DefaultString;

        [Header("AssetRef")]
        public string DefaultAssetGUID;
        public string AssetTypeConstraint; // "UnityEngine.Sprite" etc.
    }
}
```

**按 Type 切换 Inspector 字段组**：Editor 用 `[ShowIf]` 或手写 `PropertyDrawer`。这是编辑器表现层，先略。

### 2.2 PropertyDefinitionRegistry.cs

```csharp
namespace RedDust.Properties
{
    public class PropertyDefinitionRegistry
    {
        private Dictionary<string, PropertyDefSO> dict = new();

        public PropertyDefSO FindById(string id) => dict.TryGetValue(id, out var def) ? def : null;
        public bool Contains(string id) => dict.ContainsKey(id);

        #if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void BuildIndex()
        {
            var guids = AssetDatabase.FindAssets("t:PropertyDefSO", new[] { "Assets/Data/Properties/Definitions" });
            // 构建字典...
        }
        #endif
    }
}
```

**注意**：Registry 仅编辑器用。运行时 Resolve 已完成，不需要它。

**验证**：手动创建 3 个 PropertyDefSO（Float, GameplayTag, AssetRef），Registry 扫描成功。

---

## 三、Phase 3：模板层

### 3.1 PropertyNode.cs

```csharp
namespace RedDust.Properties
{
    [Serializable]
    public class PropertyNode
    {
        public string NodeId;       // 树内唯一 Id。叶子可自定义，如 "Combat_ATK"
        public string ParentId;     // ""=根
        public string DefId;        // PropertyDefSO.Id。""=文件夹
    }
}
```

**无 IsFolder**：`string.IsNullOrEmpty(DefId)` 即文件夹。
**无 IsOverride**：不覆盖，纯增量。
**无 IsEnabled**：不隐藏。
**无 Depth**：合并时不需要优先级。

### 3.2 PropertyTreeContainer.cs

```csharp
namespace RedDust.Properties
{
    [Serializable]
    public class PropertyTreeContainer
    {
        public List<PropertyNode> Nodes = new();
    }
}
```

### 3.3 PropertyTreeSO.cs

```csharp
namespace RedDust.Properties
{
    [CreateAssetMenu(menuName = "RedDust/Properties/Property Tree")]
    public class PropertyTreeSO : ScriptableObject
    {
        public PropertyTreeSO InheritsFrom;
        [TextArea(3, 20)] public string treeJson;

        /// <summary>
        /// Resolve 沿 InheritsFrom 链收集所有层，取并集，构建属性集合（纯结构，不含值）。
        /// 同 NodeId 保留祖先版本，子级冲突告警跳过。
        /// </summary>
        public Dictionary<string, PropertyDefSO> ResolveStructure()
        {
            var layers = new List<PropertyTreeContainer>();
            CollectLayers(this, layers, new HashSet<PropertyTreeSO>());

            var merged = new Dictionary<string, PropertyNode>();
            foreach (var container in layers)
            {
                foreach (var node in container.Nodes)
                {
                    if (merged.ContainsKey(node.NodeId))
                    {
                        Debug.LogWarning($"NodeId 冲突: {node.NodeId}，保留祖先");
                        continue;
                    }
                    merged[node.NodeId] = node;
                }
            }

            var roots = merged.Values.Where(n => string.IsNullOrEmpty(n.ParentId));
            var result = new Dictionary<string, PropertyDefSO>();
            foreach (var root in roots)
                BuildPath(root, merged, "", result, Registry);

            return result;
        }

        void BuildPath(PropertyNode node, Dictionary<string, PropertyNode> all,
                       string parentPath, Dictionary<string, PropertyDefSO> result,
                       PropertyDefinitionRegistry registry)
        {
            var path = string.IsNullOrEmpty(parentPath) ? node.NodeId : $"{parentPath}/{node.NodeId}";

            if (!string.IsNullOrEmpty(node.DefId)) // 叶子节点
            {
                var def = registry.FindById(node.DefId);
                if (def != null) result[path] = def;
            }

            var children = all.Values.Where(n => n.ParentId == node.NodeId);
            foreach (var child in children)
                BuildPath(child, all, path, result, registry);
        }
    }
}
```

**关键**：`ResolveStructure()` 产出 `Dictionary<string, PropertyDefSO>`（Path → Def 映射）。不含值。

**验证**：手建 WeaponBase.asset（3 节点）+ Firearm.asset（InheritsFrom=WeaponBase，+3 新增节点 + 1 同名冲突）。验证合并结果正确，同名冲突告警。

---

## 四、Phase 4：实例层

### 4.1 ResolvedPropertyBag.cs

```csharp
namespace RedDust.Properties
{
    public class ResolvedPropertyBag
    {
        // 按类型分桶存原生值
        private Dictionary<string, float> floats = new();
        private Dictionary<string, int> ints = new();
        private Dictionary<string, bool> bools = new();
        private Dictionary<string, string> strings = new();
        private Dictionary<string, GameplayTag> tags = new();
        private Dictionary<string, GameplayTag[]> tagLists = new();
        private Dictionary<string, UnityEngine.Object> assetRefs = new();
        private Dictionary<string, UnityEngine.Object[]> assetRefLists = new();

        public float GetFloat(string path) => floats.TryGetValue(path, out var v) ? v : 0f;
        // ... 其他 GetXxx 同理

        public bool TryGet(string path) => /* 检查所有桶 */;

        // --- 构造（由实例聚合根调用） ---

        public static ResolvedPropertyBag Build(
            Dictionary<string, PropertyDefSO> structure,
            string overridesJson)
        {
            var bag = new ResolvedPropertyBag();
            var overrides = ParseOverrides(overridesJson); // raw string → string dict

            foreach (var (path, def) in structure)
            {
                if (overrides.TryGetValue(path, out var rawValue))
                {
                    bag.SetFromRaw(path, def, rawValue); // 校验 + 解析
                }
                else
                {
                    bag.SetDefault(path, def); // 取 Def.DefaultXxx
                }
            }
            return bag;
        }

        // SetFromRaw: 根据 def.Type 做类型校验 + string→原生值 转换
        // SetDefault: 取 def.DefaultFloat / DefaultString / DefaultAssetGUID 等
    }
}
```

**注意**：AssetRef 在编辑器下 `Build()` 时通过 `AssetDatabase.GUIDToAssetPath` + `AssetDatabase.LoadAssetAtPath` 解析为 Object 引用存入 `assetRefs`。运行时直接返回。

**验证**：构造一个 ResolvedPropertyBag，测试 GetFloat / GetAsset / GetTag 返回值正确。

---

## 五、Phase 5：编辑器工具

### 5.1 PropertyTreeEditorWindow.cs

基础功能：
- 选择 PropertyTreeSO → 显示合并后的属性集合（Path → Def）
- 按 Depth 缩进（从 InheritsFrom 链长计算，不存 Depth 字段）
- 编辑本层节点：增删 Node、选 Def、设 ParentId
- 继承查看：灰显祖先属性，亮显本层新增
- 校验按钮：运行 ResolveStructure() 并显示告警

### 5.2 PropertyImportExport.cs

参照 `StatImportExport.cs` 五阶段：

```
Phase 1 — 创建 PropertyDefSO 资产
Phase 2 — （跳过。Properties 用全局 Id，无 defRefs 索引）
Phase 3 — 创建 PropertyTreeSO 资产，写 treeJson
Phase 4 — 链接 InheritsFrom
Phase 5 — 持久化 AssetDatabase.SaveAssets()
```

---

## 六、依赖关系

```
Phase 1 ──→ Phase 2 ──→ Phase 3 ──→ Phase 4 ──→ Phase 5
(零外部)   (依赖1)     (依赖2)     (依赖3)     (依赖4)
                          │                         │
                          └── GameplayTag 系统       └── 旧的 StatsTreeEditorWindow 仅供参考
                              (仅 GameplayTag 类型校验时引用)
```

Phase 1-3 与现有 Stats 系统零耦合。Phase 4 的 AssetRef 解析依赖 `AssetDatabase`（仅编辑器）。Phase 5 复用 StatsTreeEditorWindow 的 UI 模式但不依赖其代码。

---

## 七、不做的事（留待后续）

- PropertyType 扩展（Vector3、Color、Curve）→ 有需求再加
- Runtime Registry → 编辑时 Resolve 完成后不需要
- 多实例聚合规则（武器组装累加）→ 属于装备系统
- PropertyTreeSO 的 JSON diff 工具 → 有版本控制冲突时再做
- GearDefSO 迁移 → Properties 核心完成后单独处理
