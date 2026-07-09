# 长期开发计划 — A测路线图

> 更新: 2026-07-09 | 剩余时间: ~5 个月
> A测目标: 系统完整、内容半满、可玩闭环 — 玩家能在 30-60 分钟内体验完整生存循环

---

## 方法论

### 依据：7 款成熟游戏 + 3 套生产方法论

**参考项目**：Project Zomboid / RimWorld / 7 Days to Die / Don't Starve / Subnautica / Valheim / Factorio

**共同规律**：

| 原则 | 证据 |
|------|------|
| 核心循环先于一切 | 所有 7 款游戏最早的可玩版本都只包含：角色在地图上→收集资源→转化优势→一个威胁。没有 NPC、没有科技树、没有完整 UI |
| 系统先于内容 | RimWorld Alpha 1 只有 10% 内容量但 90% 系统完整度。系统产生涌现内容——一个小时的代码胜过一百个小时的手摆内容 |
| 每层一个可玩闭环 | Factorio 从"一个传送带自动化链"开始。PZ 从"一栋房子的生存体验"开始。每一层做到可玩再加下一层 |
| Alpha = 系统完整，不是内容完整 | Valheim EA 首发 9 个生物群系只有 6 个可用。Subnautica 首发没有基地建造。目标是"玩家从开局到理解游戏本质"的完整 30-60 分钟体验 |
| 威胁在核心循环之后 | RimWorld 的袭击者在 Alpha 1 之后。Subnautica 的危险生物在基地建造之后。玩家需要先理解"要保护什么"再被要求保护 |

**生产方法论**：Cerny Method（原型→垂直切片→生产）设计用于向发行商推销，不适用于 solo 自筹资金。**Sylvester 的 Connected Systems Playable**（所有核心系统以粗略形式共存→验证涌现交互是否有趣）是系统驱动游戏的正确模式。

**适用 RedDust 的混合方法**：预生产探索 + 每 Phase 一个垂直切片 + 持续可玩的横向构建。不做 GDD 驱动——做依赖图驱动。

### Mod 架构约束（跨所有 Phase）

> 详见 `.agent/tech/mod-architecture-framework.md`。Mod 不是"一个功能"——是贯穿所有开发阶段的架构约束。

| 约束 | 含义 | 违反的代价 |
|------|------|-----------|
| **contentId 先于资产** | 每个内容 SO 有 `category.subcategory.name` 格式的显式 ID | 以后重命名 = 批量改资产 + 断引用 |
| **public = Mod contract** | 每个 `public` 方法 Mod 作者可以调，签名的改动是 breaking change | 改了 = Mod 断裂 |
| **不 sealed 管道核心类** | EffectSO、PropertyPresetSO 等不加 `sealed` | Mod 无法继承扩展 | ⚠️ AbilityReactor/AbilityExecutor 已确认 sealed——P3 修复 |
| **全对象替换** | Mod 覆盖官方内容时整对象替换，非字段合并 | 复杂度在引擎侧而非 Mod 作者侧 |

**前置准备**（现在补）：`PropertyPresetSO` 加 `contentId` 字段，AssetCatalog 改为 contentId 查找，现有 SO 分配规范 ID。

**后续**：Phase 5 开始每个模块对照 `mod-architecture-framework.md` 的检查清单。

---

## 全局依赖图

```
                        ┌─────────────────┐
                        │  Tech Tree (10)  │ ← 需要建造+NPC+农业产出的解锁目标
                        │  Horde Events(10)│ ← 需要僵尸AI+建造+NPC
                        │  Tool System(10) │ ← 最浅系统，数据驱动
                        └──────┬──────────┘
                               │
            ┌──────────────────┼──────────────────┐
            │                  │                  │
    ┌───────▼──────┐  ┌───────▼──────┐  ┌───────▼──────┐
    │ Farming(9)   │  │ Cooking(9)   │  │ Morale(9)    │
    │ 种植+收获    │  │ 食谱+产出    │  │ NPC效率系数  │
    └───────┬──────┘  └───────┬──────┘  └───────┬──────┘
            │                  │                  │
            └──────────────────┼──────────────────┘
                               │
                        ┌───────▼──────┐
                        │  NPC (8)     │ ← 需要角色管线+建造(床位)+生存指标(需求)
                        │  指令+自主AI │
                        └───────┬──────┘
                               │
            ┌──────────────────┼──────────────────┐
            │                  │                  │
    ┌───────▼──────┐  ┌───────▼──────┐  ┌───────▼──────┐
    │ Building(7)  │  │ Map(6)       │  │ LootSystem(6)│
    │ 3种基础建筑  │  │ Synty场景    │  │ 战利品表     │
    └───────┬──────┘  └───────┬──────┘  └───────┬──────┘
            │                  │                  │
            └──────────────────┼──────────────────┘
                               │
                        ┌───────▼──────┐
                        │ Zombie AI(5) │ ← 需要Pathfinding+NavMesh+Combat管线
                        │ 视线+追击    │
                        └───────┬──────┘
                               │
            ┌──────────────────┼──────────────────┐
            │                  │                  │
    ┌───────▼──────┐  ┌───────▼──────┐  ┌───────▼──────┐
    │ Survival(4)  │  │ Time(4)      │  │ Item Econ(1-3)│
    │ 饥饿/口渴/HP │  │ 昼夜循环     │  │ 物品管线     │
    └───────┬──────┘  └───────┬──────┘  └───────┬──────┘
            │                  │                  │
            └──────────────────┼──────────────────┘
                               │
                    ┌───────────▼───────────┐
                    │ EntityService/Container│ ← Phase 4 已完成
                    │ PropertyTable/Identity │
                    └───────────────────────┘
```

**数字 = Phase 编号。** 同一 Phase 内的系统可以并行开发。

**关键路径**（最长依赖链）：物品经济 → 时间+生存 → 僵尸 AI → 地图+LootSystem → 建造 → NPC → 农业+烹饪 → 科技树。约 18-20 周顺序执行。通过并行化可以压缩到 ~16 周。

---

## A 测简化策略

每个系统标注 A 测范围和被砍内容。原则：**能跑通 ≠ 做完整。**

| 系统 | A 测范围 | 砍掉 |
|------|---------|------|
| **生存指标** | 饥饿、口渴、体力、HP。随时间递减。消耗品恢复 | 血液/感染/体温/意识/疼痛/完整伤病系统/六维属性 |
| **昼夜** | 24 分钟=1 天。灯光旋转。简单 ClockOverlay | 季节/天气/夜间机制变化 |
| **僵尸 AI** | 视线检测。追击。近身攻击。 | 噪音连锁/尸群协同/搜索行为/僵尸变种 |
| **地图** | 小镇中心+农田+森林。Synty 预组合建筑 | 多区域/地下/载具道路 |
| **LootSystem** | 5 张战利品表。加权独立投骰。`LootContainer` 组件 | 区域驱动/容器重生/品质系统 |
| **建造** | 木墙/木地板/木门。自由摆放+碰撞检测 | 旋转/网格/多种材料/工作站/农田 |
| **NPC** | 2 个 NPC。跟随/待命指令。饿了吃、困了睡 | 框选/工作分配/技能成长/招募对话/工具绑定 |
| **农业** | 1 种作物(土豆)。种植→等待天数→收获 | 多种作物/留种/土壤质量/NPC 种植 |
| **烹饪** | 1 个设施(篝火)。1 个食谱(烤土豆) | 灶台/多种食谱/NPC 烹饪/食谱发现 |
| **科技树** | 6 节点(每线1个)。蓝图物品→消耗→标记解锁 | 树形可视化/AND 前置/多种解锁效果 |
| **工具** | 斧/锄/锤。PropertyTable 耐久度。修理=消耗材料 | 全工具类型/效率公式/NPC 绑定 |
| **士气** | 单 float。热餐+1，无屋顶-1。乘上工作效率 | 食物多样性/舒适度/社交/无聊 |
| **尸潮** | 每 7 天夜晚触发。8-12 僵尸从地图边缘刷出 | 伪随机触发/评分系统/强度缩放/多路进攻 |
| **存档** | EntityService.All→JSON。单存档槽。手动保存 | 多存档/自动保存/版本迁移/增量快照 |

### 永久延后（A 测不做）

| 系统 | 理由 |
|------|------|
| 完整伤病系统 | HP 受伤足够提供张力 |
| 噪音系统（运行时） | NoiseEventSO 资产已有(~44)，但接入僵尸 AI 是复杂度倍乘器 |
| 武器熟练度成长 | 战斗已闭环，熟练度是增量加法 |
| NPC 招募/对话 | 硬编码 2 个 NPC 直接刷在基地旁 |
| 载具系统 | GDD 已将载具列入暂缓内容 |

---

## Phase 路线

### 相对工作量标记

**S** = Small (2-4天) · **M** = Medium (1-2周) · **L** = Large (2-4周)

时间线约 20 周。代码预算 ~12-14 周 + 内容/打磨 ~6-8 周。

---

---

### Phase 1-4 — 基础设施 ✅ 已完成

> 2025-04 ~ 2026-07。从 locomotion 到 combat pipeline 全链。

| Phase | 内容 | 关键产出 |
|-------|------|---------|
| 1 | Locomotion 完结 | HeadLook + Footstep |
| 1.5 | 音效骨架 | AudioSetSO / AudioChannel / CharacterAudio |
| 2 | 通用数值系统 | StatsTreeSO → 后来重构为 L3_Properties |
| 2.5 | Stats 管理 | Rule 分层架构（5 行为模式） |
| 3 | HUD UI | uGUI 重建：UIScreen/Overlay/Panel + MainMenu + VitalsOverlay |
| 4 | 战斗 + 技能管线 | 俯视角切换 + A* 寻路 + S1-S5（Properties 集成 → 装备技能闭环 → 8 状态管道 → 战斗管道收尾 → 动画增强） |

> ⚠️ **Mod 补课待办**：Phase 1-4 建造时没有 Mod 约束。需要补 contentId 字段、AssetCatalog 标准化、关键类可扩展性确认。**详见 `short-term.md` 前置准备。**

---

### Phase 5 — 物品经济 🔄 施工中

> 切片 A: "物品存在于世界" — 地上看到物品→捡起→背包看到→装备→使用消耗品。

| # | 任务 | 工作量 | 复用 |
|---|------|--------|------|
| P5.1 | GroundItem + ItemService + EntityService 改 | **S** | EntityService 事件管线、Identity 组件 |
| P5.2 | 拾取交互（跨容器转移） | **S** | RdContainer.Place/Remove、鼠标射线检测 |
| P5.3 | 背包面板 UI | **M** | UIScreen/UIPanel/UIIconSlot、WeaponBarOverlay 参考 |
| P5.4 | 装备/卸下 | **S** | CharacterEquipment.SyncEquipment()、SlotBoneMapper |
| P5.5 | 消耗品使用 | **S** | GetDamageEffects()、AbilityReactor、StackCount |

**详细计划**：见 `.agent/plans/p5.1-world-item-spawn.md`。

**Mod 义务**：新物品 SO 按 `category.subcategory.name` 命名 contentId。ItemService 公开 API 用 IItemReadOnly 不暴露 PropertyTable 内部。装备操作的 EffectSO 调用链预留 virtual。

---

### Phase 6 — 生存威胁

> 切片 B+C: 时间流逝→饥饿→吃喝→昼夜→僵尸→战斗。此时有一个完整的"末日生存"游戏。

| # | 任务 | 工作量 | 复用 |
|---|------|--------|------|
| **Time** | 游戏内时钟（24 分钟=1 天）+ 方向光旋转 + ClockOverlay | **S** | TimeService（timeScale 已有）、UI overlay 模式 |
| **Stats** | 饥饿/口渴/体力每秒递减，HP 为零=死亡 | **S** | VitalsQuery（路径已有）、PropertyTable、CharacterActor.Update 挂载点 |
| **Stats HUD** | 生命值/体力/饥饿/口渴 UI 条接入真实数据 | **S** | VitalsOverlay（已有预制体）、UIStatBar |
| **Death** | HP=0→禁用输入→死亡画面→重新加载 | **S** | CharacterActor + Input 禁用模式 |
| **Z1** | ZombieDefSO（PropertyPresetSO 子类）+ Prefab + Editor | **S** | PropertyPresetSO 模式、EntityEditorWindow 模板 |
| **Z2** | ZombieActor 组件（Identity + PropertyTable + 简单 FSM） | **M** | CharacterActor 参考、Animation FSM 模式 |
| **Z3** | 视线检测（锥形角度+距离+射线遮蔽） | **S** | Unity Physics.Raycast、昼夜视野系数 |
| **Z4** | 追击+攻击（A* 寻路→近身→SDamageInfo→Combat 管线） | **M** | A* Pathfinding、Combat 管线全链、AbilityReactor |
| **Z5** | 地图生成（Day 0 散布 N 只，缓慢重生） | **S** | EntityService.SpawnRequest |

**为什么僵尸放在生存指标之后**：生存指标给"为什么搜刮"提供意义——食物恢复饥饿。僵尸给"为什么紧张"提供压力——被追击需要消耗体力。两者一前一后激活了物品经济的完整闭环。

**为什么不做噪音**：噪音是放大器。纯视线僵尸已经产生紧张感。噪音是"每件事都有后果"——等核心 AI 循环稳固后再接线。

**Mod 义务**：生存指标递减公式用 delegate（Mod 可换公式）。僵尸 AI 状态机走 interface 注册（Mod 可加新状态）。ZombieDefSO 创建时走 contentId。

---

### Phase 7 — 世界落地

> 切片 D: 地图+LootSystem+建造。从"测试空地"升级为"有探索价值的真实世界"。

| # | 任务 | 工作量 | 复用 |
|---|------|--------|------|
| **M1** | Unity 场景：小镇中心+农田+森林。Synty 预组合建筑 | **L**（内容为主） | Polygon Apocalypse/Town/Farm 套件（已有 19 个 Prefab 映射） |
| **M2** | NavMesh 烘焙 | **S** | A* Pathfinding Project（已有 GraphCache） |
| **L1** | LootContainer 组件（MonoBehaviour: lootTableId + isLooted） | **S** | Identity 模式、SceneItem Prefab |
| **L2** | LootSystem 核心：JSON 加载→加权投骰→调 ItemService.SpawnWorldItem | **M** | ItemService（P5.1 已建）、EntityImporter JSON 模式 |
| **B1** | BuildMode 输入（B 键切换、鼠标→世界网格、绿/红预览） | **M** | InputService 模式、鼠标到地面投影 |
| **B2** | 建筑放置：验证（无重叠、有材料）→Instantiate Prefab→容器扣除材料 | **M** | EntityService.SpawnRequest、Container.Remove |
| **B3** | 3 个建筑 Prefab：木墙/木地板/木门（Synty 资产+碰撞体） | **S** | Polygon Apocalypse 建造套件 |
| **B4** | 建筑耐久度：墙体 HP→僵尸攻击→墙体破坏 | **S** | SDamageInfo→Combat 管线、PropertyTable(Durability) |

**为什么地图代码量小但工作量大**：地图是手摆 Synty 预制体的内容工作。可以和 Phase 6 代码并行（独力开发者交替"写代码日"和"摆场景日"）。LootSystem 代码是简单加权随机——真正的工作是设计战利品表 JSON 条目。

**为什么建造只做 3 种 Prefab**：目标是回答"玩家能创造防御僵尸的边界吗？"——木墙/木地板/木门足以回答。旋转、网格、多种材料是 Beta 迭代。

**Mod 义务**：战利品表 JSON 格式对 Mod 可见（Mod 可新增/覆写）。建筑 Prefab 引用走 contentId 而非直接 Unity 引用。BuildMode 验证逻辑数据驱动（Mod 可加新建筑类型）。

---

### Phase 8 — NPC + 据点

> 切片 E+F: NPC 加入→分配工作→种田→做饭→士气影响效率。激活"种田流"核心差异化。

| # | 任务 | 工作量 | 复用 |
|---|------|--------|------|
| **N1** | NPCDefSO + 2 个 NPC 在场景中生成 | **S** | Human PropertyTree（已有 60+ 属性）、CharacterActor（NPC 复用角色系统） |
| **N2** | 指令系统（点击 NPC→右键下达移动/待命） | **M** | InputService（secondary interact）、A* Pathfinding |
| **N3** | 自主 AI（无指令时：饿了吃、困了睡、空闲） | **M** | AIService（L2 已注册，实现 OnTick）、VitalsQuery 需求属性 |
| **N4** | NPC HUD：选择→显示状态→分配跟随/待命 | **S** | UIScreen 模式、Stats 显示模式 |
| **F1** | 农田：指定地块→耕地 Prefab 生成→种种子 | **S** | BuildMode 放置逻辑、物品消耗管线 |
| **F2** | 生长计时：种植后 N 个游戏天数→成熟→可收获 | **S** | TimeService.WorldTime、Coroutine/deltaTime |
| **F3** | 收获：交互→获得作物物品→地块回到耕地状态 | **S** | ItemService.SpawnWorldItem（物品进背包） |
| **C1** | 篝火 Prefab + 交互打开烹饪 UI | **S** | 建筑放置模式 |
| **C2** | 食谱：消耗生土豆→产出烤土豆（容器内物品交换） | **S** | Container.Place/Remove |
| **M1** | 士气计算：每日检查→热餐+1、无屋顶-1→乘上工作速度 | **S** | PropertyTable(Morale 属性)、NPC Update tick |

**为什么 NPC 需要 Phase 7 完成**：NPC 需要床（建造）、食物（烹饪）。代码可以提前写但测试需要这些系统。

**Mod 义务**：NPC 行为树走 interface 注册（Mod 可加新行为）。食谱 JSON 对 Mod 可见。士气公式用 delegate。

---

### Phase 9 — 长期驱动

> 切片 G: 科技树+尸潮+工具。激活"成长→验证→更强"的正向循环。

| # | 任务 | 工作量 | 复用 |
|---|------|--------|------|
| **T1** | 蓝图物品类型（ConsumableSO + TechNode 引用） | **S** | ConsumableSO 已有 |
| **T2** | TechTreeSO 数据（6 节点，ID+名称+前置+解锁效果）+ JSON 导入 | **S** | PropertyTreeSO 模式、JSON 导入工具 |
| **T3** | 科技面板 UI（显示节点→锁定/可用/已解锁→点击学习） | **M** | UIScreen 模式、EditorTreeView 参考 |
| **T4** | 解锁效果：发事件→建造/制作系统开启新配方 | **S** | EventHub（每科技节点一个事件） |
| **H1** | HordeService：追踪天数，每 7 晚从地图边缘生成 N 只僵尸 | **S** | TimeService（WorldDay 事件）、EntityService.SpawnRequest |
| **H2** | 尸潮寻路：生成的僵尸路径走向玩家位置 | **S** | A*、已有僵尸追击行为 |
| **Tool** | 工具耐久度：EquipmentDefSO+Durability→使用递减→修复 | **S** | PropertyTable(Durability 在 ToolBase 树中)、EquipmentModule |

**为什么到这一步加速**：所有基础系统存在。科技节点只是标记翻转。尸潮是"生成僵尸+路径走向玩家"——僵尸 AI 已经承担了所有繁重工作。工具是给已有装备添加耐久度字段。

**Mod 义务**：科技树节点 Mod 可新增（数据驱动）。尸潮规则 JSON 可配置。工具类型走 GameplayTag（Mod 注册新 tag 即可加新工具）。

---

### Phase 10 — 收尾

> 切片 H: 存档+内容填充+数值平衡+修 Bug。

| # | 任务 | 工作量 | 复用 |
|---|------|--------|------|
| **SV1** | EntitySerializer：遍历 EntityService.All→每个 Entity 写 PropertyTable JSON | **S** | PropertyTable 内置序列化（PropertyImportExport 已有 ToJson/FromJson） |
| **SV2** | Container 序列化：每个容器写 ContainerSlotRef 列表 | **S** | ContainerSlotRef（已 Serializable struct） |
| **SV3** | 世界状态：建筑位置、农田地块、僵尸位置→JSON | **M** | EntitySerializer 模式扩展到世界对象 |
| **SV4** | 加载管线：清空世界→反序列化 Entity→恢复 Container→放置建筑→生成作物 | **M** | EntityService.Register + SpawnRequest |
| **内容** | 更多战利品表（厨房/药房/五金店/警察局） | **M** | 纯内容工作 |
| **内容** | 2-3 种额外作物 + 2 个额外食谱 | **S** | 纯数据添加 |
| **数值** | 调饥饿/口渴消耗速率、僵尸属性、建筑 HP、作物生长期 | **M** | PropertyTable 数值、无代码 |
| **Bug** | 稳定性通过、A 测构建 | **M** | 缓冲时间 |

**Mod 义务**：存档 header 预留 `activeMods: [{modId, version}]`。存档读写用 contentId（不存 asset name 或 GUID）。

---

## 时间线（估算）

```
周1 周2 周3 周4 周5 周6 周7 周8 周9 周10 周11 周12 周13 周14 周15 周16 周17 周18 周19 周20
├── 物品经济(5) ──┤
              ├── 生存+时间(6a) ──┤
                        ├── 僵尸AI(6b) ──────┤
                                    ├── 地图+LootSystem(7a) ────┤  (地图可与僵尸并行，交替
                                    │                             代码日/内容日)
                                                ├── 建造(7b) ────┤
                                                        ├── NPC(8a) ────┤
                                                                ├── 农业烹饪士气(8b) ──┤
                                                                            ├── 科技尸潮工具(9) ──┤
                                                                                       ├── 收尾(10) ──┤
```

**关键路径**: 物品经济→建造→NPC→农业→科技树 ≈ 16 周。两个可并行点：
1. 地图场景可以在 Phase 5 完成后立即开始（零代码依赖）
2. 时间/昼夜独立于一切——可以和物品经济并行

---

## 风险与底线

| 最大风险 | 影响 | 缓解 |
|---------|------|------|
| 僵尸 AI 超预期耗时 | 阻塞地图/尸潮 | 激进砍范围：仅 Idle/Chase/Attack 三个状态。每秒寻路一次。单一僵尸类型 |
| 地图内容超时 | 阻塞 LootSystem/建造/所有空间系统 | 用 Synty 预组合建筑（整套房子当一个 Prefab）。接受"平面地图"——仅地面层 |
| NPC 行为出乎意料地复杂 | 阻塞农业/士气/自动化循环 | 降级为"NPC 是属性修正器"——分配 NPC 到农田 = 作物生长 2x 速度。无可见 NPC 移动/指令 |
| 建筑放置算法（碰撞/网格）膨胀 | 拖垮整个据点开发 | 切换到 Unity Tilemap 网格吸附。单层 2D 网格，无旋转，建筑是 tile |
| 整体耗时超出 5 个月 | A 测交付不完整 | **最小可行 A 测**: 物品经济 + 时间 + 生存指标 + 僵尸 AI + 地图 + LootSystem + 存档 |

---

## 交付标准：A 测 = 什么

| 不是 | 是 |
|------|-----|
| 所有内容就位 | 所有系统跑通 |
| 没有崩溃 | 可接受崩溃 |
| 画面精美 | 占位资源可以 |
| 数值平衡 | 数值可调 |
| 50 小时深度 | **一次 30-60 分钟的完整循环体验** |

**A 测的通过条件**：一个外部玩家从进入游戏开始，能在 30-60 分钟内理解"这是什么游戏"并想继续玩。所有核心系统（生成→拾取→装备→消耗→建造→战斗→生存）都在运转，即使每个系统只有最小内容量。

---

### 参考来源

- **RimWorld 构建序列**: Tynan Sylvester "The Simulation Dream" (2013) + Kickstarter 更新 + Alpha 1-3 changelogs
- **Project Zomboid**: 2011 技术演示 + Rezzed "How (Not) To Make a Game" + 14 年构建历史
- **7 Days to Die**: Alpha 1-21 全量 changelog + Kickstarter 策略
- **Don't Starve**: ECS/Prefab 架构 + `SetPristine()` 客户端/服务器分离
- **Subnautica**: GDC 2019 事后分析 + "Earliest Access" 策略
- **Valheim**: Good Enough 原则 + 3 biome 故意空置 + 单人程序员前 5 个月
- **Factorio**: 传送带优化 (FFF #176) + 实体原型继承链
- **Cerny Method**: D.I.C.E. Summit 2002 + GameRes 中文翻译
- **Solo Dev 生产**: Wayline 生存游戏 solo 开发指南 + Toño Game Consultants 范围管理
