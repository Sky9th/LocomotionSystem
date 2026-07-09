> ⚠️ **已废弃** — 被 [mod-community-decision-record.md](mod-community-decision-record.md) 取代。
> 原方案在 10 项战略决策缺失下直接跳到了代码结构（ModService.cs、GameRegistry 方法签名），
> 未回答 Mod 深度边界、ID 体系、数据主权、存档策略、平台策略等前置问题。
> 新文档包含完整的四轮辩论论证链 + 6 角度评议团审查结论。

# Mod 社区支撑 — 架构方案（已废弃）

---

## 一、背景

### 当前架构优势（天然支持 Mod）

| 系统 | 已有能力 | Mod 价值 |
|------|---------|----------|
| **PropertyTree** | Schema/Data 分离，属性通过 JSON 覆写 | Mod 可新增物品/武器/实体而不改代码 |
| **Entity 系统** | Entity = Id + Preset + Properties，纯数据 | Mod 可定义全新实体类型 |
| **Ability 系统** | AbilityDefSO + AbilityTreeSO 数据驱动 | Mod 可新增技能/套路 |
| **本地化** | JSON 外部化，community/ 目录 | 已有社区翻译模式可直接复制 |
| **Editor 导入导出** | JSON ↔ SO 双向转换，5 阶段验证 | Mod 作者可用同样管线制作内容 |

### 当前缺口

| 缺口 | 影响 |
|------|------|
| **无运行时 Mod 加载** | 所有数据走 Addressables boot 标签，启动时固化 |
| **GameRegistry 只读** | 无动态 Add/Remove API，Mod 数据无法注入 |
| **无 Mod 清单格式** | Mod 作者没有标准化的元数据描述方式 |
| **无加载顺序/冲突解决** | 多个 Mod 同时存在时行为不确定 |
| **无 Steam Workshop 集成** | 分发依赖手动操作 |

---

## 二、设计决策

### 2.1 Mod 范围：纯数据 Mod

**Mod 只能修改数据，不能执行代码。** 这是安全和稳定性的底线。

| 可 Mod | 不可 Mod |
|--------|---------|
| 物品/武器/装备/消耗品数值 | 游戏逻辑/C# 代码 |
| 实体/NPC/敌人定义 | Unity Prefab（需 Unity 编辑器） |
| 技能/Ability/效果 | 核心系统行为 |
| 科技树节点/配方 | 渲染管线/Shader |
| 建筑属性/分类 | 引擎设置 |
| 属性/Skill 树 | NavMesh/寻路 |
| 本地化文本 | — |
| UI 布局/主题 | — |
| 音效引用（替换，不新增） | 音效文件（需 Unity 打包） |
| 对话文本/事件脚本 | — |

> Prefab/模型/音效等 Unity 资源依赖 AssetBundle，初期不做。先聚焦纯数据 Mod。

### 2.2 Mod 文件格式

基于已有 JSON 基础设施，采用与 Editor 导入导出**同一 Schema**：

```json
{
  "modId": "com.example.weapon-pack",
  "name": { "zh-CN": "武器扩展包", "en": "Weapon Pack" },
  "version": "1.0.0",
  "author": "ModAuthor",
  "description": { "zh-CN": "...", "en": "..." },
  "dependencies": [],
  "loadPriority": 0,
  "content": {
    "items": [...],
    "entities": [...],
    "abilities": [...],
    "recipes": [...],
    "techNodes": [...],
    "buildings": [...],
    "translations": { "locale": "ja", "entries": {...} }
  }
}
```

- **与 Editor JSON 同构**：Mod 作者可在编辑器中制作内容 → 导出 JSON → 发布为 Mod
- **或纯手写 JSON**：社区作者用文本编辑器即可创建简单物品 Mod

### 2.3 加载顺序与冲突解决

```
加载优先级: 官方数据 < Mod A < Mod B（按 loadPriority 升序）
冲突策略: 后加载覆盖前加载（Last Write Wins）
```

- 同一 Key（如 `item.bandage.name`）被多个 Mod 修改 → 高优先级生效
- Mod 的 `dependencies` 字段确保依赖的 Mod 先加载
- 启动时输出 Mod 加载日志，标记冲突项

### 2.4 分发渠道

| 渠道 | 阶段 |
|------|------|
| **Steam Workshop** | A测后（Steamworks.NET 集成） |
| **手动安装** | A测即可（放入 Mods/ 文件夹） |
| **GitHub** | 社区自发 |

---

## 三、技术架构

### 3.1 新增组件

```
L2_ModService/                    ← 新增 L2 Service
├── ModService.cs                 ← Mod 加载/卸载/生命周期
├── ModManifest.cs                ← Mod 清单数据结构
├── ModRegistry.cs                ← 已加载 Mod 索引
└── ModConflictResolver.cs        ← 冲突检测与报告
```

### 3.2 Mod 加载管线

```
游戏启动
  → GameService.Bootstrap()
    → GameRegistry.LoadBootAssets()     ← 加载官方数据（现有）
    → ModService.ScanModFolder()         ← 扫描 persistentDataPath/Mods/
    → ModService.ValidateMods()          ← 校验依赖、版本、格式
    → ModService.LoadMods()              ← 按优先级加载 + 冲突解决
    → GameRegistry.AppendModData()       ← 动态注入 Mod 数据到 Registry
    → EventHub.Publish(SModsLoaded)      ← 通知其他系统
  → 继续正常启动
```

### 3.3 GameRegistry 改造

现有 GameRegistry 是只读的——需要新增动态注入接口：

```csharp
// 当前: 只有 Boot 时填充
// 新增:
public void RegisterItem(PropertyPresetSO item)    // 动态添加物品
public void RegisterEntity(EntityDefSO entity)      // 动态添加实体
public void RegisterAbility(AbilityTreeSO tree)     // 动态添加技能树
public void RemoveModEntries(string modId)           // Mod 卸载时清理
```

### 3.4 目录结构

```
{persistentDataPath}/
├── Mods/                          ← Mod 扫描根目录
│   └── {modId}/
│       ├── manifest.json          ← Mod 清单
│       ├── data/                  ← 数据定义
│       │   ├── items.json
│       │   ├── entities.json
│       │   ├── abilities.json
│       │   ├── recipes.json
│       │   ├── tech-tree.json
│       │   └── buildings.json
│       └── translations/          ← 可选翻译
│           └── {locale}.json
│
├── Locales/
│   └── community/                 ← 已有，社区翻译
│       └── {locale}/
│           └── *.json
│
└── ModCache/                      ← 运行时缓存（自动生成）
    └── {modId}_compiled.json
```

---

## 四、策划文档更新

### 4.1 新增设计文档

`design/systems/mod.md` — Mod 系统设计（面向策划+Mod 作者）

内容：
- Mod 能改什么（能力边界表）
- 文件格式与示例
- 制作 Mod 的工作流（编辑器制作 vs 手写 JSON）
- 发布与分发流程
- 游戏内 Mod 管理界面设计

### 4.2 更新 game-overview.md

在核心关键词和系统全景中增加 Mod 条目：
- 关键词：`Mod 支持（纯数据驱动，JSON 格式，Steam Workshop）`
- 系统全景：新增 Mod 系统行

---

## 五、分阶段实施

| 阶段 | 内容 | 优先级 |
|------|------|--------|
| **Phase 1：基础框架** | Mod 清单格式 + ModService 扫描/加载 + GameRegistry 动态注入 + 手动安装 | P0 |
| **Phase 2：内容创作** | Editor Mod 导出工具 + 示例 Mod + Mod 作者文档 | P1 |
| **Phase 3：管理界面** | 游戏内 Mod 管理 UI（启用/禁用/排序）+ 冲突可视化 | P1 |
| **Phase 4：分发** | Steam Workshop 上传/订阅 + Mod 依赖自动下载 | P2 |

---

## 六、本次交付

1. **策划文档** — 新建 `design/systems/mod.md`
2. **技术文档** — 新建 `tech/L2-services/L2-mod-service/` 目录 + README
3. **更新** — `game-overview.md`、`design/README.md`、`tech/README.md`、`.agent/README.md`

---

## 七、验证

- Mod 加载管线在启动日志中可见
- 放置一个测试 Mod（新增一个物品）到 `Mods/` → 游戏内物品栏出现该物品
- 两个 Mod 修改同一 Key → 高优先级生效，冲突记入日志
