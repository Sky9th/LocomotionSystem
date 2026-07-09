# Mod 架构框架

> **定位**：贯穿所有开发阶段的架构约束，和 L1→L5 层级规则、数据流原则同级。写代码时对照检查。
>
> 关联：[mod-community-decision-record.md](../plans/mod-community-decision-record.md) — 10 项战略决策（WHY）
> 关联：[mod.md](../design/systems/mod.md) — 创作者/玩家视角（WHAT）
> 关联：[mod-json-reference.md](../design/systems/mod-json-reference.md) — Mod JSON 格式手册（HOW）

---

## 第一部分：框架标准

### 1. 程序集边界

Mod DLL 是外部 C# 程序集，通过 HybridCLR 解释器加载。它能调用什么、不能调用什么，由程序集拆分决定。

#### 1.1 三层程序集模型

```
RedDust.Core.dll         ← 引擎层：渲染、物理、网络、Addressables 管线
RedDust.Gameplay.dll     ← 游戏层：公开 API，Mod 编译时引用
RedDust.Modding.dll      ← SDK 层：Mod 入口接口、属性标记、基类

Mod 编译时：只引用 RedDust.Gameplay.dll + RedDust.Modding.dll
Mod 运行时：HybridCLR 加载，无法访问 Core 的 internal 类型
```

| 程序集 | 包含 | Mod 可见 | 稳定性承诺 |
|--------|------|---------|-----------|
| **Core** | EntityService 内部、PropertyTable 实现、AssetCatalog、Addressables 管线、渲染、物理 | ❌ `internal` | 无 |
| **Gameplay** | `IEntityReadOnly`、`IPropertyReadOnly`、`IDamageSource`、EffectSO、AbilityTreeSO、各种 DefSO | ✅ `public` | pre-1.0 unstable → 1.0 锁定 |
| **Modding** | `IModEntry`、`ModManifest`、`[ModEntry]` attribute、`ModContext` | ✅ `public` | 尽早锁定 |

#### 1.2 程序集引用规则

```
Core ← Gameplay ← Modding
  ↑        ↑         ↑
  └────────┴─────────┘ Mod DLL 只依赖 Gameplay + Modding
```

- Core **不引用** Gameplay 或 Modding
- Gameplay 引用 Core（调用引擎服务），但只暴露接口、不暴露实现
- Modding 引用 Gameplay（SDK 可以依赖公开 API）
- Mod DLL **永远不能**直接引用 Core

#### 1.3 当前状态与迁移路径

> ⚠️ 当前项目使用默认 `Assembly-CSharp`，未拆分程序集。以下为迁移路径，不阻塞当前开发。

| 阶段 | 动作 | 何时做 |
|------|------|--------|
| S0 | 程序集拆分方案定稿（本文档） | 现在 |
| S1 | 创建 `RedDust.Gameplay.asmdef` + `RedDust.Modding.asmdef`，移动类型 | HybridCLR 接入时 |
| S1 | 现有 `Assembly-CSharp` 重命名为 `RedDust.Core.asmdef` | 同上 |

**S0 期间遵守的临时规则**（程序集未拆分前）：
- `public` = 未来属于 Gameplay 或 Modding，Mod 可能依赖
- `internal` = 未来属于 Core，Mod 不可见
- 写新类时先问："这个类 Mod 需要看到吗？"——决定放 `public` 还是 `internal`

#### 1.4 不 sealed 原则

管道核心类不加 `sealed`，保留继承扩展可能。这些类 Mod 作者大概率想继承：

| 类 | 位置 | 说明 |
|----|------|------|
| `EffectSO` | L3_Ability | 效果基类。Mod 新增效果类型的第一入口 |
| `AbilityExecutor` | （待建） | 技能执行管道 |
| `BaseLocomotion` | （待建） | 移动仿真 |
| `PropertyPresetSO` | L3_Properties | 所有实体定义的基类 |

> `sealed` 只在性能热点（每帧遍历的类型）和安全性关键路径上使用。

---

### 2. API 设计约束

#### 2.1 Public = Contract

```
public → Mod 作者可以调 → 这就是你的 API 承诺
internal → Mod 作者看不到 → 可以随便改
```

**规则**：
- 加 `public` 前停顿 3 秒——这个签名我能承诺到 1.0 不改吗？
- 不确定 → 先用 `internal`，等需求明确再升级
- `internal` 升 `public` 没有破坏性，反过来有

#### 2.2 热路径不暴露虚方法

| 调用频率 | 扩展方式 |
|---------|---------|
| 每帧（Update/Tick） | delegate 回调 或 event |
| 每动作（技能触发） | virtual 方法 ✅ |
| 偶尔（初始化/配置） | interface 实现 ✅ |

**为什么**：虚方法在 HybridCLR 解释器中调用有跨语言开销。虽然不大，但每帧累积。delegate 可以在 AOT 侧被订阅、在 Mod 侧被调用，只需要一次跨语言跳转。

#### 2.3 数据定义 vs 数据使用

| 层 | 规则 | 示例 |
|----|------|------|
| 数据定义 | 用 `contentId`（string），不依赖 Unity asset name | `item.weapon.melee.iron_sword` |
| 数据使用 | 通过 `contentId` 查找，缓存引用 | `_cachedPreset = Registry.Find(contentId)` |
| 序列化 | 存 `contentId`，不存 asset name 或 GUID | 存档写 `contentId`，读档时解析 |

#### 2.4 虚方法 vs Delegate vs Event 的选择

| 场景 | 用 | 原因 |
|------|-----|------|
| Mod 想换掉一段逻辑 | `virtual` | 子类 override |
| Mod 想在特定时机被通知 | `event` | 订阅/退订，不破坏原有逻辑 |
| Mod 想注入计算公式 | `delegate` / `Func<>` | 公式是可替换的纯函数 |
| Mod 想提供全新实现 | `interface` | 注册新实现到字典/列表 |

---

### 3. 内容 ID 稳定性

#### 3.1 ID 格式

```
官方:  {category}.{subcategory}.{name}
Mod:   {category}.{subcategory}.@{author}.{name}
```

| Category | 覆盖内容 | 示例 |
|----------|---------|------|
| `item` | 武器、防具、弹药、消耗品、工具、容器 | `item.weapon.melee.iron_sword` |
| `entity` | 角色、丧尸、NPC、动物 | `entity.zombie.runner` |
| `ability` | 技能树（天赋/套路/天生） | `ability.routine.baji_quan` |
| `building` | 建筑 | `building.defense.watchtower` |
| `sceneitem` | 场景物品 | `sceneitem.furniture.wooden_chair` |
| `recipe` | 制作配方（未来） | `recipe.craft.iron_sword` |
| `tech` | 科技节点（未来） | `tech.survival.basic_farming` |

#### 3.2 命名规则

- 全小写，下划线分隔：`iron_sword` 不是 `IronSword`
- 从通用到具体：`item.weapon.ranged.pistol.glock_17`
- subcategory 最多 3 层
- 数字用下划线：`nine_mm` 不是 `9mm`

#### 3.3 废弃别名

```
v1.0:  contentId = "item.consumable.medical.bandage"
v1.1:  contentId = "item.consumable.medical.sterile_bandage"
       deprecatedIds = ["item.consumable.medical.bandage"]  ← 保留旧 ID
v2.0:  major version → 清理所有 deprecated alias
```

- 不删除 ID——只新增 + 废弃旧
- Mod 引用 deprecated ID → 加载成功 + LogWarning
- Major version 清理——给 Mod 作者整一个 minor 周期迁移

#### 3.4 contentId 字段落地方案

> ⚠️ 当前 `PropertyPresetSO` 没有 `contentId` 字段，所有标识走 Unity asset name。

| 步骤 | 操作 | 影响 |
|------|------|------|
| 1 | `PropertyPresetSO` 新增 `public string contentId;` | 所有子类继承 |
| 2 | Editor 中 `contentId` 默认 = asset name（迁移兼容） | 零破坏 |
| 3 | 新创建的 SO：手动填写规范的 `contentId` | 从 Phase 5 物品开始 |
| 4 | S1：AssetCatalog 查找改用 `contentId` 优先，asset name fallback | 逐类型切换 |
| 5 | S2：移除 asset name fallback | contentId 为唯一标识 |

---

### 4. 覆写语义

#### 4.1 核心规则

| 规则 | 说明 |
|------|------|
| **全对象替换** | Mod 覆盖官方 ID → 整个数据对象被 Mod 版本替换，不是字段级合并 |
| **Last Write Wins** | 两个 Mod 覆盖同一个 ID → `loadPriority` 高的生效 |
| **冲突日志** | 被覆盖的 Mod 记入日志（`.mod-conflicts.log`），Mod 管理面板可查看 |
| **依赖拓扑排序** | Mod 按 `dependencies` 拓扑排序加载。环 → 报错，所有相关 Mod 拒绝加载 |

#### 4.2 为什么不字段级合并

字段级合并的复杂度：需要定义"哪些字段可独立覆盖"的 Schema → 官方改字段结构后语义错乱。全对象替换——简单，语义清晰，不用维护字段级 Schema。

#### 4.3 加载优先级

```
loadPriority: 数字大的后加载
  0 = 默认
  1-9 = 可手动调整

官方内容: loadPriority = -1（最先加载，最低优先级）
```

- 相同 loadPriority：按文件名排序（确定性）
- 依赖声明强制排序：被依赖的 Mod 先加载（无视 loadPriority）

---

### 5. 扩展点模式

Mod 不只是一堆 JSON 数据——Level 2（效果组合）和 Level 4（C# 脚本）都需要代码级扩展点。

#### 5.1 两类扩展点

| 类型 | 用途 | 谁扩展 | 方式 |
|------|------|--------|------|
| **数据扩展** | 新物品、新实体、新技能 | JSON Mod 作者 | `contentId` + PropertyTree |
| **行为扩展** | 新效果、新 AI、新 UI | C# Mod 作者 | virtual / interface / delegate / event |

#### 5.2 效果组合系统（Level 2 核心扩展点）

这是 JSON Mod 作者能用到的最强工具——不需要写代码，通过组合已有积木创造新行为：

```
效果 = 触发条件 + 目标选择 + 效果列表

触发条件: OnEquip / OnHit / OnKill / OnDamaged / OnLowHP / OnComboStage
目标选择: Self / Target / Area(cone/circle/ray)
效果列表: Damage / Heal / ApplyBuff / ModifyStat / SpawnProjectile / AOE / Knockback
```

**约束**：
- 新效果类型通过 `EffectSO` 子类添加（C# Mod 可扩展）
- 效果组合的配置格式面向 JSON（非代码）
- 变量槽（`{attacker.atk}`, `{target.def}`）可引用运行时属性

#### 5.3 扩展点注册模式

```
有多个实现 → 用 interface + 注册表
  例: IModEntry → ModService.RegisterEntry()

有默认实现 + 可选替换 → 用 virtual
  例: EffectSO.Execute() → virtual

有默认行为 + 可选通知 → 用 event
  例: ModService.OnModLoaded += ...

有默认公式 + 可选替换 → 用 Func<> delegate
  例: DamageCalculator = (base, def) => base * (1 - def/100)
```

---

## 第二部分：模块落地指南

### 检查清单 A：新建 ScriptableObject

写新 SO 时逐项对照：

- [ ] 有 `contentId` 字段吗？（继承 `PropertyPresetSO` 自动有，其他 SO 需自己加）
- [ ] `contentId` 遵守 `category.subcategory.name` 格式吗？
- [ ] 有 `deprecatedIds` 预留吗？（`public string[] deprecatedIds;`）
- [ ] SO 内部引用的其他 SO 用的是 `contentId` 而非 asset name 吗？
- [ ] 编辑器窗口显示了 `contentId` 字段吗？

### 检查清单 B：新建 class / struct

- [ ] 这个类型 Mod 需要看到吗？
  - 需要 → `public`，放 Gameplay 程序集（或标 `// TODO: move to Gameplay`）
  - 不需要 → `internal`
- [ ] 如果是 `public`——这个签名我能承诺到 1.0 吗？
  - 不确定 → `internal`，等需求明确再升级
- [ ] 热路径（每帧调用）暴露了虚方法吗？
  - 是 → 改用 delegate / event
- [ ] 类需要被 Mod 继承吗？
  - 需要 → 不加 `sealed`

### 检查清单 C：新建 L2 Service / L3 模块

- [ ] 哪些配置 Mod 可覆写？
  - 全部数据定义类 SO → 可覆写
  - 引擎配置类 SO（渲染/物理）→ 不可覆写
- [ ] 哪些行为 Mod 可扩展？
  - EffectSO、Ability 管道 → 可扩展
  - EntityService 内部 → 不可扩展
- [ ] 模块的数据依赖用 `contentId` 了吗？
- [ ] 模块产出了可以被 Mod 引用的公共类型吗？
  - 产出了 → 在模块文档里标注 Mod 可见性

### 检查清单 D：修改已有代码

- [ ] 改了 `public` 方法签名吗？
  - 改了 → 这是 breaking change。记入 CHANGELOG 的 "Mod API Changes" 段
- [ ] 加了新 `public` 类型/方法吗？
  - 加了 → 这是新 API。标注稳定性级别（`stable` / `unstable` / `experimental`）
- [ ] 给一个类加了 `sealed` 吗？
  - 加了 → 检查这个类是否在"不 sealed 列表"中

---

## 第三部分：分阶段路线图

每个 Phase 在原有开发计划之上，新增"Mod 框架视角的义务"。

### S0：地基（当前阶段）

| 产出 | 类型 |
|------|------|
| 本文档 — Mod 架构框架 | 架构约束 |
| 决策记录 — 10 项战略决策（已完成） | 决策 |
| 策划文档 — mod.md + mod-json-reference.md（已完成） | 设计 |
| HybridCLR 技术分析（已完成） | 技术 |

**日常义务**：
- 新建 SO 遵循 contentId 规范（即使 contentId 字段还没加）
- 新 `public` 类/方法思考"Mod 需要看到它吗"

### S1：核心交付

| 原有计划 | Mod 框架视角的新增约束 |
|---------|---------------------|
| ModService 加载器 | 遵守依赖拓扑排序 + 冲突日志 |
| Steam Workshop 集成 | Mod 上传时校验 contentId 格式 |
| Mod 管理 UI | 显示 contentId、冲突状态、deprecated 警告 |
| 全对象 Override | 遵守覆写语义规则 |
| 存档 Mod 变更检测 | 遵守 contentId 稳定性——存档用 contentId，不用 asset name |
| 效果组合系统 | 遵守扩展点模式——新效果类型走 EffectSO virtual |

**文档义务**：
- 模块落地后更新对应 L2/L3 文档，标注 Mod 可见性
- 产出一份"Mod API Surface"清单（哪些 public 类型 Mod 可调）

### S2：深度

| 原有计划 | Mod 框架视角的新增约束 |
|---------|---------------------|
| 编辑器导出按钮 | 导出的 JSON 遵守 contentId 格式 |
| 正式 Mod API 文档 | 从"Mod API Surface"清单派生正式文档 |
| API 稳定性承诺 | 锁定 Gameplay 程序集中标 `stable` 的 API |

### S3：表现层

| 原有计划 | Mod 框架视角的新增约束 |
|---------|---------------------|
| AssetBundle Mod | 资产引用走 contentId，不用直接 Unity 引用 |
| Total Conversion | 所有系统全面走扩展点——Total Conversion = 换掉所有数据 + 大部分行为 |

---

## 附录：关键决策速查

| # | 决策 | 一句话 |
|---|------|--------|
| 0 | 脚本运行时 | HybridCLR 社区版。内部热重载用，Mod 门开着 |
| 1 | Mod 深度 | 目标 Level 2（效果组合），Level 4 C# 不堵门 |
| 2 | 数据主权 | 全对象 Override，Last Write Wins |
| 3 | 内容 ID | 纯字符串 + `@author.` 命名空间 + 废弃别名 |
| 4 | 互操作 | 允许硬依赖，拓扑排序，环检测拒绝 |
| 5 | 加载时机 | S1 启动时静态加载，不改代码热重载 |
| 6 | 存档 | 警告 + 占位物品，不禁止加载 |
| 7 | 平台 | Steam Workshop 首发 |
| 8 | 工具链 | S1 文档 + 示例，编辑器导出 S2 |
| 9 | 治理 | DMCA 流程，不主动审核 |
| 10 | 分阶段 | 激进路线：S1 直接 Workshop 集成 |
