# editor · 编辑器工具

> Unity Editor 扩展工具集 — Core 场景自动加载、GameContext 运行时调试面板、Synty Prototype 资源浏览器。

**源文件目录**: `Assets/Scripts/Editor/`

## 层级定位

全局 Helper — 不限层级。所有代码仅在 `UNITY_EDITOR` 条件下编译，不参与 Runtime 构建。

- **仅 Editor 环境**: 使用 `[InitializeOnLoad]` / `[CustomEditor]` / `EditorWindow` 等 Editor API。
- **辅助开发流程**: 不提供运行时功能，仅提升开发调试效率。
- **独立于业务模块**: 对 L1-L5 模块只读访问，不修改数据。

## 调用链

```
Unity 编辑器启动
  │
  ├── [InitializeOnLoad] EditorCoreLoader 静态构造
  │     └── EditorApplication.playModeStateChanged += OnPlayModeChanged
  │           └── ExitingEditMode → 检查 activeScene
  │                 ├── 是 Core → 跳过
  │                 └── 非 Core → EditorSceneManager.OpenScene(Core.unity, Additive)
  │
  ├── GameContextEditor (Inspector)
  │     └── OnInspectorGUI → GameContext 运行时属性展示
  │           ├── Services: IsInitialized / RegisteredServiceCount / RegisteredServiceTypes
  │           └── Snapshots: SnapshotCount / SnapshotStructTypes
  │
  └── Synty Prototype Browser (Window)
        ├── [MenuItem] SyntyPrototypeMenu.Browse()
        │     └── GetCategories() → ScanAllFolders()
        │           └── CreateEntry() → DetermineCategory() + FormatDisplayName()
        │
        └── SyntyPrototypeBrowser.Open(categories)
              ├── OnGUI: 搜索栏 → 材质栏 → 分类侧栏 + 缩略图网格
              ├── ScanMaterials() → 扫描 PolygonPrototype/Materials
              ├── PlaceWithMaterial(path) → SyntyPrototypeMenu.InstantiateByPath()
              └── DrawFilteredResults() → 跨分类搜索
```

## 耦合模块

| 本模块 | 依赖/消费方 | 关系 |
|--------|-----------|------|
| EditorCoreLoader | Core 场景 (GameService) | 确保 Core.unity 在 Play Mode 时已加载 |
| GameContextEditor | GameContext (01-core) | 读取 GameContext 运行时属性用于展示 |
| SyntyPrototypeBrowser | SyntyPrototypeMenu | 接收 CategoryData 列表、调用 InstantiateByPath |
| SyntyPrototypeMenu | — | 菜单入口 + 数据扫描，独立于其他模块 |
| SyntyPrototypeMenu/Browser | Art Assets (PolygonPrototype) | 直接扫描资产目录的 Prefab 和 Material |

## 设计决策

| 决策 | 原因 |
|------|------|
| EditorCoreLoader 使用 [InitializeOnLoad] | 无需手动挂载 GameObject，编辑器启动即自动注册事件 |
| 只对 ExitingEditMode 响应 | 只在进入 Play Mode 前加载，避免场景冲突 |
| GameContextEditor 只读展示 | 不改动运行时数据，纯调试用途 |
| SyntyPrototypeBrowser 懒加载 Prefab/Material 引用 | 避免编辑器启动时扫描全部资源导致卡顿 |
| Material bar 在 Browser 中独立扫描 | 材质数量固定 (20 个)，按编号规则命名，直接硬编码路径扫描 |

## 未来规划

| 规划 | 状态 | 依赖 | 来源 |
|------|------|------|------|
| Core 加载增加 UI 反馈 | 待做 | — | 代码分析 |
| GameContextEditor 支持 Snapshot 值预览 | 待做 | — | 代码分析 |
| Prototype Browser 增加收藏/最近使用 | 待做 | — | 代码分析 |
| Prototype Browser 增加多选批量放置 | 待做 | — | 代码分析 |
| EditorCoreLoader 场景路径可配置 | 待做 | GameProfile | 代码分析 |

## 子文档索引

| 文件 | 内容 |
|------|------|
| [editor-core-loader.md](editor-core-loader.md) | Play Mode 自动加载 Core 场景 |
| [game-context-editor.md](game-context-editor.md) | GameContext Inspector 调试面板 |
| [L4-prototype/synty-prototype-browser.md](L4-prototype/synty-prototype-browser.md) | Prototype 浏览器 EditorWindow |
| [L4-prototype/synty-prototype-menu.md](L4-prototype/synty-prototype-menu.md) | 菜单入口 + 分类扫描 + 实例化 |
