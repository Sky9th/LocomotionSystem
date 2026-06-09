# GearInstance — 运行时装备个体

> `L3_Equipment/` · 技术文档 · 2026-06-08

## 定位

`GearDefSO` 是共享的**定义资产**——所有 Glock 17 枪身共享同一个 .asset 文件。`GearInstance` 是运行时的**装备个体**——地上的两把 Glock 17 有不同的耐久度、不同的词条、不同的已安装配件。

```
GearDefSO (共享)                        GearInstance (独立)
───────────────                        ─────────────────────
Glock17_Frame.asset                    地上的第一把 Glock 17:
  statsTree: Pistol                      def → Glock17_Frame.asset
  overrides: { Weight=0.25 }                durability: 85%
  ...                                    activeAffixes: [大口径弹膛]
                                         installedParts: { Barrel→..., Slide→... }

                                       地上的第二把 Glock 17:
                                         def → Glock17_Frame.asset  ← 同一个引用
                                         durability: 42%              ← 不同
                                         activeAffixes: []            ← 不同
                                         installedParts: { Barrel→... } ← 不同
```

---

## 字段定义

```csharp
public class GearInstance
{
    // ── 共享引用（不变）──
    public GearDefSO def;                            // ← 指向 .asset 文件。所有同型号装备共享

    // ── 独立状态（每个个体不同）──
    public float durability;                         // 当前耐久。0 = 损坏/不可用
    public List<AffixDefSO> activeAffixes;           // 当前生效的词条（掉落随机 / 锻造 / 命名装备固有）
    public Dictionary<string, GearInstance> installedParts; // 已安装的子零件。仅 def.slots 非空时有值
    public int currentAmmo;                          // 弹匣内剩余弹药数（仅 Magazine 零件）
}
```

| 字段 | 类型 | 来源 | 说明 |
|------|------|------|------|
| `def` | `GearDefSO` | 构造时传入 | 永不改变。决定这个装备"是什么" |
| `durability` | `float` | 构造时设定 / 使用中消耗 | `0..def.stats["MaxDurability"].Current`。0 = 不可用但可修理 |
| `activeAffixes` | `List<AffixDefSO>` | 掉落随机 / 锻造 / 命名装备固定词条 | 运行时挂载，可被替换（锻造台重铸词条） |
| `installedParts` | `Dictionary<string, GearInstance>` | 工作台组装 / 野外拆卸 | 键 = 槽位 slotId。仅 Receiver（`def.slots` 非空）有此字段 |
| `currentAmmo` | `int` | 装填操作 | `0..def.stats["MagSize"].Current`。0 = 需换弹 |

---

## 工厂流程

### 从 GearDefSO 创建

```
GearInstanceFactory.Create(def, initialDurability?, initialAffixes?):
  1. 创建 GearInstance
     instance.def = def

  2. 设置耐久
     instance.durability = initialDurability ?? def.stats["MaxDurability"].Current

  3. 设置词条
     instance.activeAffixes = initialAffixes ?? []

  4. 如果 def.slots 非空（这是 Receiver）：
     instance.installedParts = new Dictionary<string, GearInstance>()
     // 不自动填充 —— 必装零件需要玩家手动安装或由掉落系统配置

  5. 返回 instance
```

### 从掉落表创建

```
LootTableSO.Roll():
  gear = LootEntry.gear      // GearDefSO 引用
  durability = Random.Range(entry.minDurability, entry.maxDurability)
  affixes = entry.affixPool.RandomPick(1~3)

  instance = GearInstanceFactory.Create(gear, durability, affixes)

  // 如果是 Receiver，可能预装零件：
  if gear.slots 非空:
      foreach slot in gear.slots:
          if slot.required:
              part = PartPool.GetRandom(slot.acceptTag)  // 随机生成匹配零件
              instance.installedParts[slot.slotId] = GearInstanceFactory.Create(part, ...)
          else if Random.value < 0.3:                    // 30% 概率预装可选零件
              part = PartPool.GetRandom(slot.acceptTag)
              instance.installedParts[slot.slotId] = GearInstanceFactory.Create(part, ...)

  return instance
```

---

## Resolve() — 数值叠加流程

`GearInstance.Resolve()` 是装备系统的核心运算：将定义层 + 覆写层 + 词条层叠加为最终数值。

```
GearInstance.Resolve():
  ┌─────────────────────────────────────────────────────┐
  │  ① 类型基线                                          │
  │  def.statsTree.Resolve()                            │
  │  → StatInstance[]（ATK=15, Accuracy=70, ...）       │
  │                                                     │
  │  每个 stat 的值 = StatDefSO.Default                  │
  └────────────────────────┬────────────────────────────┘
                           │
                           ▼
  ┌─────────────────────────────────────────────────────┐
  │  ② 型号覆写                                          │
  │  foreach override in def.overrides:                  │
  │      stat = Find(override.stat)                      │
  │      stat.Override(override.value)                   │
  │  → StatInstance[]（Weight=0.25 (枪身自身); 战斗 stat 由零件在 ③a 贡献, ...）       │
  │                                                     │
  │  每个 stat 的值 = GearDefSO 型号值                    │
  └────────────────────────┬────────────────────────────┘
                           │
                           ▼
  ┌─────────────────────────────────────────────────────┐
  │  ③a 零件叠加（def.slots 非空时）                       │
  │  ├── 检查必装零件: 遍历 def.slots，required=true 但未安装 → 拒绝装备到身体槽位，Resolve() 仍可执行但返回不完整 stat
  │  foreach part in installedParts:                     │
  │      partStats = part.Resolve()  // 递归             │
  │      foreach stat in partStats:                      │
  │          self[stat.Def].Current += stat.Current      │
  │  → StatInstance[]（加上枪管/套筒/弹匣的贡献）        │
  └────────────────────────┬────────────────────────────┘
                           │
                           ▼
  ┌─────────────────────────────────────────────────────┐
  │  ③b 词条叠加                                         │
  │  foreach affix in activeAffixes:                     │
  │      foreach mod in affix.modifiers:                 │
  │          stat = Find(mod.stat)                       │
  │          stat.AddModifier(affix, mod)                │
  │  → StatInstance[]（ATK=35, Accuracy=65, ...）       │
  │                                                     │
  │  每个 stat 的值 = 装备最终输出                         │
  └─────────────────────────────────────────────────────┘

  ③a 只在 def.slots 非空时执行（枪支/复合弓等组装装备）。
  对于独立装备（近战武器、防具、工具），直接跳到 ③b。

### 词条作用范围

Resolve() 的步骤顺序隐含了词条的作用范围：

| 词条挂载位置 | 生效范围 | 原因 |
|-------------|---------|------|
| 零件 (Barrel 等) | **Self** — 仅零件自身 | 零件 Resolve() 独立递归，③b 在递归内执行 |
| Receiver | **Assembly** — 整个组装体 | Receiver 的 ③b 在 ③a（零件求和）之后执行 |

例：Receiver 上有一个 +10% ATK 词条 → 作用于 Receiver + Barrel + Slide + ... 的总 ATK。
Barrel 上有一个 +5 ATK 词条 → 仅作用于 Barrel 自身 ATK，然后被 ③a 累加到总值。

如需词条显式声明作用域，可在 AffixDefSO 上增加 EAffixScope { Self, Assembly } 枚举字段。
当前设计依赖挂载位置隐式决定。

```

### 叠加后消费

```
EquipmentComponent.OnEquip(instance):
  stats = instance.Resolve()       // GearInstance → StatInstance[]

  // 桥接到角色 Stats
  foreach stat in stats:
      actorStats.Get(stat.Def).AddModifier(instance, stat.Current)

// ─── Ability Pipeline ⑤ Effects ───
AbilityExecutor.TryActivate():
  weapon = equipment.GetWeapon()          // GearInstance（可能是组装枪或单件武器）
  stats = weapon.Resolve()                // 递归求和所有零件
  hit.IncomingDamage = stats["ATK"].Current
  hit.StaggerValue = stats["Stagger"].Current
```

---


---

## 耐久与损坏

### 耐久消耗

受击（防具）/ 使用（武器/工具）时由外部系统调用。

### 低耐久惩罚

| 耐久区间 | 效果 |
|---------|------|
| 100% ~ 50% | 正常运作 |
| 50% ~ 20% | 所有 stat 值 x0.8（磨损） |
| 20% ~ 0% | 所有 stat 值 x0.5（濒毁） |
| 0 | 不可用。Resolve() 返回的 StatInstance 全部归零。不可装备到身体槽位。可修理 |

### 损坏行为

- durability=0 的装备不能被装备到身体槽位
- 已装备的装备在战斗中耐久归零 → 自动卸下到背包
- Resolve() 仍可调用（用于 UI 显示原始属性），但返回的 stat 值全部为零
- 修理完成后 durability > 0，恢复正常 Resolve

## 生命周期

```
┌──────────┐   掉落/制造/交易   ┌──────────────┐   玩家操作   ┌──────────┐
│ GearDefSO │ ───────────────→ │ GearInstance │ ──────────→ │  角色    │
│ (.asset)  │  Factory.Create  │ (运行时个体) │  Equip/Apply│  身上    │
└──────────┘                   └──────────────┘             └──────────┘
                                      │
                                      │  词条重铸 / 零件换装 / 耐久修理
                                      ▼
                                字段被修改（durability, activeAffixes, installedParts）
                                      │
                                      ▼
                                卸下 → 回背包（保留所有运行时状态）
                                      │
                                      ▼
                                丢弃 / 拆解 / 交易 / 死亡掉落
```

**关键行为**：

| 事件 | 变化 |
|------|------|
| 掉落生成 | `Factory.Create(def, durability, affixes)` — 创建新个体 |
| 装备上 | `Resolve()` → `AddModifier` 到角色 Stats |
| 卸下 | `RemoveByOwner(instance)` 从角色 Stats 回收所有 Modifier |
| 零件换装 | `installedParts["Barrel"]` 替换 → `Resolve()` 重新计算 |
| 词条重铸 | `activeAffixes` 修改 → `Resolve()` 重新计算 |
| 受击/使用 | `durability -= delta`。归零 → 自动卸下 / 无法使用 |
| 修理 | `durability += delta`（受 MaxDurability 上限和修理技能限制） |
| 拆解 | 读 `def.salvageOutputs`，品质系数 = `durability / maxDurability` |

---

## 与 GearDefSO 的对比

| | GearDefSO | GearInstance |
|---|---|---|
| 类型 | `ScriptableObject` | `class` |
| 数据持久层 | `.asset` 文件（硬盘） | 存档（运行时序列化） |
| 同型号共享？ | 是。所有 Glock 17 共享一个 .asset | 否。每个实体独立 |
| 可变？ | 设计期可变，运行时不改 | 运行时可改（耐久、词条、零件） |
| 引用方式 | 文件 GUID（Unity 序列化引用） | 存档内的 instance ID |
| 主要字段 | statsTree, overrides, slots, icon, prefab... | def, durability, activeAffixes, installedParts |
| Resolve() | 基线 → 覆写（两层） | 基线 → 覆写 → 零件 → 词条（四层） |
