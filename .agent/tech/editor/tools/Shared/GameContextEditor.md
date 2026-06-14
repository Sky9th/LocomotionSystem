# GameContextEditor
> **源文件**: `Assets/Scripts/Editor/GameContextEditor.cs`

自定义 Inspector，运行时显示 GameContext 的 Service Registry 和 Snapshot Cache 状态。

## 调用链

```
被谁调:
  Unity Editor Inspector 面板  → OnInspectorGUI() (选中 GameContext 对象时自动调用)

调谁:
  OnInspectorGUI()              → GameContext.IsInitialized / RegisteredServiceCount / SnapshotCount
                                  GameContext.RegisteredServiceTypes / SnapshotStructTypes
  DrawTypeList()                → EditorGUILayout.Foldout / LabelField
```

## 耦合模块

| 方向 | 模块 | 关系 |
|------|------|------|
| 依赖 | GameContext (01-core) | 读取 GameContext 运行时属性用于展示 |

## 方法

### OnInspectorGUI()
```csharp
public override void OnInspectorGUI()
```
- **用途**: 渲染自定义 Inspector 面板
- **调用者**: Unity Editor 在 Inspector 刷新时自动调用（目标 GameObject 持有 GameContext 组件）
- **界面布局**:
  1. `DrawDefaultInspector()` — 绘制 GameContext 默认序列化字段
  2. 分隔线
  3. "Runtime Overview" — 只读显示 IsInitialized / Service Count / Snapshot Count
  4. "Service Registry (N)" — 可折叠列表，显示所有已注册 Service 类型
  5. "Snapshot Cache (N)" — 可折叠列表，显示所有 Snapshot 类型
- **备注**: 所有 Runtime 字段使用 `EditorGUI.DisabledScope(true)` 包裹，只读不可编辑

### DrawTypeList()
```csharp
private void DrawTypeList(ref bool foldout, string label, IEnumerable<Type> types)
```
- **用途**: 绘制可折叠的类型列表区域
- **参数**:
  - `foldout` — 折叠状态引用（按钮折叠时写回 false）
  - `label` — 折叠标题文本（如 `"Service Registry (3)"`）
  - `types` — 类型集合（可枚举）
- **调用者**: `OnInspectorGUI()` 内部，用于 Services 和 Snapshots 两个区域
- **逻辑**:
  - 如果 `types` 为空 → 显示 "None"
  - 否则 → 缩进后逐个显示 `type.FullName`
- **备注**: 使用 `EditorGUILayout.Foldout(foldout, label, true)` 的 toggle 样式

## 内部机制

### 条件编译
```csharp
#if UNITY_EDITOR
```
- 整个文件仅在 Editor 环境下编译

### 私有字段
```csharp
private bool showServices = true;    // Service 列表折叠状态
private bool showSnapshots = true;   // Snapshot 列表折叠状态
```

## 未来规划

| 规划 | 状态 | 来源 |
|------|------|------|
| 无具体规划。 | — | — |

## 依赖的 Unity Editor API

| API | 用途 |
|-----|------|
| `CustomEditor(typeof(GameContext))` | 绑定到 GameContext 类型 |
| `EditorGUILayout.Foldout` | 可折叠区域 |
| `EditorGUI.DisabledScope` | 只读字段 |
| `EditorGUILayout.LabelField` | 标签展示 |
| `EditorGUI.indentLevel` | 缩进层级控制 |
