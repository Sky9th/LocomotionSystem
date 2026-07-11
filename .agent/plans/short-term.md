# 短期开发计划

> 更新: 2026-07-11 | 分支: `feature/phase5-item-economy`
> 聚焦: Phase 5 物品经济（P5.1 → P5.5）
> 长期路线: 见 `.agent/plans/long-term.md`
> Mod 框架: 见 `.agent/tech/mod-architecture-framework.md`

---

## ✅ Mod 补课 — HybridCLR 接入 + Mod 闭环（已完成）

> 2026-07-10 · 2 sessions · `feature/hybridclr-mod-verification`

| 里程碑 | 产出 | Commit |
|--------|------|--------|
| HybridCLR 社区版接入 | IL2CPP 获得 `Assembly.Load(byte[])` 能力，66 AOT 元数据通过 Addressables 加载 | `ee054aad` |
| L2 ModService 基础设施 | `[ModEntry]` + `IModEntry` 接口，`ModManifest` JSON 解析，扫描 `Mods/` → 反射加载 → 调用 `Initialize()` | `fa945104` |

**已验证**: Editor 下端到端闭环——外部 C# DLL 编译 → 放入 Mods/ → 游戏加载执行 → `Debug.Log` 输出。

详见: [hybridclr-integration](../.agent/sessions/2026-07-10-hybridclr-integration.md) · [mod-service](../.agent/sessions/2026-07-10-mod-service-infrastructure.md)

### 已知缺口（记录，不阻塞 Phase 5）

| 缺口 | 优先级 | 说明 |
|------|--------|------|
| `link.xml` 为空 | P1 | IL2CPP stripping 可能裁掉 Mod 引用的 public 类型 |
| 依赖拓扑排序 | P1 | Mod 加载顺序未按 `dependencies[]` 排序 |
| ID 冲突检测 + `loadPriority` | P1 | 同名 Mod 无冲突检测 |
| IL2CPP Build 验证 | P1 | 仅在 Editor 验证，未做独立 Build 测试 |
| Mod 管理 UI | P2 | 无 Mod 列表/启用禁用 UI |
| Workshop 集成 | P3 | Steam Workshop 上传下载 |

---

## S0 基础设施（与 Phase 5 并行推进）

> contentId 是物品经济的硬依赖——Phase 5 推进过程中逐步完成。

### 🔴 P0：PropertyPresetSO 加 contentId 字段 ✅

> 实际实现：不走 C# 字段，走 PropertyTree 节点 + ContentIdUtility。数据在 PropertyTable 中，Mod 可寻址。

| 步骤 | 操作 | 状态 |
|------|------|------|
| P0.1 | `PropertyTree` 新增 `Common/Category` (RdTag) + `Common/Id` (string) 节点 | ✅ |
| P0.2 | `ContentIdUtility`：从 PropertyTable 拼 contentId = `{category}.{id}` | ✅ |
| P0.3 | `EntityEditorWindow` 首次保存时自动从 asset name 推导 `Common/Id`（snake_case） | ✅ |
| P0.4 | 49 件道具 contentId 回填（11 Consumable + 26 Equipment + 12 Ammo） | ✅ |

**优于原方案**：contentId 在数据层而非 Unity 对象层——Mod 覆写 PropertyTree 值即可改变 contentId，无需继承 SO 类。

### 🟡 P1：AssetCatalog 改为 contentId 查找

> Mod 闭环已验证，可以启动。与 Phase 5 开发协调，不阻塞 P5.1。

| 步骤 | 操作 | 文件 |
|------|------|------|
| P1.1 | Items + Characters 注册表 contentId 优先，asset name fallback | `AssetCatalog.cs` |
| P1.2 | 其余注册表 contentId 优先 | `AssetCatalog.cs` |
| P1.3 | `PlayerService` 硬编码引用改为 contentId | `PlayerService.cs`（~5 处） |

### 🟢 P2：Phase 1-4 关键类 sealed 审查（不改代码，只标注）

> 不改代码。仅输出"需要改的类清单"，P3 以后修复。

| 审查项 | 状态 |
|--------|------|
| **AbilityReactor** | ⚠️ 已确认 `sealed`——需改 |
| **AbilityExecutor** | ⚠️ 已确认 `sealed`——需改 |
| EffectSO 层级 | 待审查——base 是否有 sealed？叶子类是否有 sealed？ |
| HitReaction DriverArbiter | ⚠️ 抢占规则已确认 hardcode——需提取接口 |

---

## 当前进度

```
Phase 4 ✅ ──── 战斗/动画/属性/技能管线全封闭
    │
Phase 5 🔄 ──── 物品经济
    ├── P5.0 基础设施 + 道具数据 ✅ (v0.42.0)
    ├── P5.1 物品能存在于世界上 ✅
    ├── P5.2 玩家能拾取物品 ← 当前
    ├── P5.3 玩家能看到背包
    ├── P5.4 玩家能装备/卸下物品
    └── P5.5 玩家能使用消耗品
```

---

## Phase 5 — 物品经济

> 从玩家视角：探索世界→发现物品→捡起来→装备/使用→管理负重。

### 玩家故事

| # | 玩家能做什么 | 为什么重要 |
|---|------------|----------|
| P5.1 | 在地上看到物品 | 世界有东西可以交互——物品经济的第一步 |
| P5.2 | 点击物品捡起来 | 从"看到"到"拥有"——最基本的采集循环 |
| P5.3 | 打开背包看到所有物品 | 拥有感——"这是我的东西" |
| P5.4 | 把武器装备到手上 / 卸下来 | 武器显示在角色手上——视觉反馈 |
| P5.5 | 使用消耗品（食物/绷带） | 物品产生效果——闭环完成 |

---

### P5.0 — 基础设施 + 道具数据 ✅

> v0.42.0。Equipment/Ammo/Consumable 三个 Editor + ImportExport 已就位，49 件道具数据全量落地。

---

### P5.1 — 物品能存在于世界上 ✅

> 2026-07-11 · 1 session · commit `75df7526`

玩家走进房间，地上有物品——坐标可见。

**实际产出**（与原计划有偏差）：

| 产出 | 说明 |
|------|------|
| **AssetRef 运行时修复** | `AssetRefPropertyDefSO.Load()` → `Addressables.LoadAssetAsync(guid).WaitForCompletion()` + static cache，移除 `#if UNITY_EDITOR` 分裂。Editor + Build + Mod catalog 统一路径 |
| **VisualPrefab 迁移** | `properties_all.json` — `VisualPrefab` 从 `Equipment/Presentation` 迁至 `Entity/Common`，全实体类型继承，Mod 可覆写 |
| **EntityService.CreateGameObject()** | 统一入口：角色 → `Preset.Prefab`，物品 → `Common/VisualPrefab` → CoinBag → Cube 三级 fallback |
| **GroundItem.cs 删除** | 物品生成无需独立 Prefab 或组件，`AddComponent<Identity>()` 直接绑数据 |

**架构决策**：
- VisualPrefab 放 `Common/` 而非 `Presentation/`——ConsumableBase/AmmoBase 不继承 Equipment，需通过 Entity 根树才可见
- AssetRef 值统一为 GUID——Mod SDK 发布 `.bundle + catalog_*.json`，`LoadContentCatalogAsync` 后 GUID 天然可解析
- 测试代码硬编码 asset name——等 P1 AssetCatalog 支持 contentId 查找后再改

**已知缺口**：
- [ ] AssetCatalog 仍用 SO `.name` 查找（非 contentId）—— P1 待办
- [ ] Build 未验证——仅在 Editor Play Mode 测试
- [ ] Mod catalog 加载未实现——未来 Mod SDK 部分
- [ ] 角色仍走 `Preset.Prefab` 路径——标注 TODO 后续迁移

**Session**: [2026-07-11-p5.1-item-spawn.md](../.agent/sessions/2026-07-11-p5.1-item-spawn.md)

---

### P5.2 — 玩家能拾取物品

> 玩家点击地上的物品→物品从地面消失→出现在背包里。

| # | 任务 | 说明 |
|---|------|------|
| P5.2a | **拾取交互** | 鼠标射线检测物品 GameObject（Identity 组件）→ 触发拾取 |
| P5.2b | **跨容器转移** | 物品从地面 → 角色背包容器（原子操作，失败回滚） |
| P5.2c | **ItemService 创建** | L2 服务：EntityId→世界坐标索引 + SpawnWorldItem + GetNearbyItems（P5.1 未做，P5.2 补齐） |

**已有基础**: RdContainer.Place/Remove/CanAccept、CharacterBuildContext.Container、InventoryQuery、EntityService.CreateGameObject()（物品生成统一入口）

**Mod 义务**：跨容器转移是引擎操作——不暴露给 Mod，但转移结果通过 event 通知（Mod 可订阅）。

---

### P5.3 — 玩家能看到背包

> 按 Tab 打开背包面板——物品图标、名称、数量、负重。

| # | 任务 | 说明 |
|---|------|------|
| P5.3a | **背包面板 UI** | UIScreen 网格显示背包物品 |
| P5.3b | **物品图标** | ItemDefSO 图标——走 PropertyTree AssetRef 节点（非 C# 字段，Mod 可覆写） |
| P5.3c | **负重显示** | 当前重量/负重上限 + 轻/中/重/超载 |

**已有基础**: WeaponBarOverlay、UIIconSlot、RdContainer.CurrentWeight

**Mod 义务**：背包 UI 数据接口不暴露 PropertyTable 内部——通过只读接口获取显示用属性。

---

### P5.4 — 玩家能装备 / 卸下物品

> 背包拖武器到右手槽→武器出现在角色手上。拖回→武器消失。

| # | 任务 | 说明 |
|---|------|------|
| P5.4a | **装备槽 UI** | 背包面板旁装备槽（右手/左手/头/胸/腿）——5 槽，匹配 PropertyTree 4 护甲类 + 2 武器槽 |
| P5.4b | **装备操作** | 背包↔装备槽 物品转移 |

**已有基础**: CharacterEquipment.SyncEquipment()、SlotBoneMapper、WeaponAttachPoint

**Mod 义务**：装备操作的 EffectSO 调用链预留 virtual。SyncEquipment 槽位来自 SlotDef[]（✅ 数据驱动），但 CharacterBuildContext 中槽位定义需确认非 hardcode。

---

### P5.5 — 玩家能使用消耗品

> 右键绷带→消耗→HP 恢复。右键食物→消耗→饥饿恢复。

| # | 任务 | 说明 |
|---|------|------|
| P5.5a | **消耗品使用** | 右键触发 Use → 读 EffectSO[] → AbilityReactor 执行 |
| P5.5b | **数量变化** | StackCount--，归零时物品销毁 |

**已有基础**: PropertyPresetSO.GetDamageEffects()、AbilityReactor.Resolve()、Entity.StackCount

**Mod 义务**：EffectSO 基类已 virtual ✅，但注意叶子类（BuffEffectSO/DamageEffectSO 等）是 sealed——Mod 只能新建 EffectSO 子类，不能继承已有效果类型。

---

## 后续预览

Phase 5 完成后进入 **Phase 6 — 生存威胁**（见 `long-term.md`）：
- 时间 + 昼夜
- 生存指标（饥饿/口渴/体力/HP）
- 僵尸 AI（视线检测 + 追击 + 攻击）

---

## 不纳入短期

- 地图 / LootSystem / 建造 → Phase 7
- NPC / 农业 / 烹饪 → Phase 8
- 科技树 / 尸潮 / 工具 → Phase 9
- 存档 / 收尾 → Phase 10
- 角色创建 UI / 技能槽溢出 / PropertyType.Struct
