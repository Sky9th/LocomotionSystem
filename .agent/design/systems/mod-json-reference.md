# Mod JSON 格式手册

> 面向 Mod 作者。介绍如何用 JSON 定义新内容——物品、实体、技能、配方。
> 不需要 Unity，不需要编程。文本编辑器就够了。

---

## 一、快速入门（5 分钟）

### 1.1 一个最小的 Mod

```json
{
  "modId": "com.example.my-first-mod",
  "name": { "zh-CN": "我的第一个 Mod", "en": "My First Mod" },
  "version": "1.0.0",
  "author": "你的ID",
  "description": { "zh-CN": "加了一把新手剑", "en": "Adds a beginner sword." },
  "dependencies": [],
  "content": {
    "items": []
  }
}
```

这是一个空的 Mod——什么都不改，只是能加载。接下来往里填内容。

### 1.2 新增一把武器

在 `content.items` 里加一条：

```json
{
  "id": "novice_sword",
  "displayName": { "zh-CN": "新手剑", "en": "Novice Sword" },
  "category": "weapon.melee",
  "properties": {
    "Combat/BaseDamage": 12,
    "Combat/AttackSpeed": 1.0,
    "Common/DurabilityMax": 100,
    "Common/Weight": 2.5
  }
}
```

放进 `Mods/` 文件夹 → 启动游戏 → 背包里出现一把新手剑。**这就是你的第一个 Mod。**

### 1.3 覆盖一个官方物品

如果不想新增，只想改——改绷带的治疗量：

```json
{
  "modId": "com.example.better-bandage",
  "name": { "zh-CN": "更强绷带", "en": "Better Bandage" },
  "version": "1.0.0",
  "author": "你的ID",
  "description": { "zh-CN": "绷带治疗量翻倍", "en": "Doubles bandage heal." },
  "dependencies": [],
  "loadPriority": 0,
  "content": {
    "overrides": {
      "items": {
        "item.bandage": {
          "properties": {
            "Consumable/HealAmount": 40
          }
        }
      }
    }
  }
}
```

**注意**：覆盖是整对象替换——上面例子中，`item.bandage` 的**全部属性**被你的版本替换，不仅仅是 `HealAmount`。确保你把想保留的属性也写上。

---

## 二、Mod 清单（manifest）

每个 Mod 根目录下必须有一个 `manifest.json`：

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `modId` | string | ✅ | 全局唯一标识。推荐反向域名格式：`com.你的ID.mod名` |
| `name` | object | ✅ | 显示名称。至少包含 `zh-CN` 或 `en` |
| `version` | string | ✅ | 语义化版本 `主.次.修订`，如 `1.2.0` |
| `author` | string | ✅ | 作者 ID。决定自动命名空间前缀 |
| `description` | object | ❌ | 描述文本。语言映射同 name |
| `dependencies` | string[] | ❌ | 依赖的其他 Mod 的 `modId`。按依赖顺序加载 |
| `loadPriority` | int | ❌ | 加载优先级（默认 0）。数字大的后加载，后加载的覆盖先加载的 |
| `content` | object | ❌ | 内容定义——见下文各节 |
| `translations` | object | ❌ | 可选的本地化文件入口。见第八节 |

---

## 三、物品定义（content.items）

### 3.1 字段参考

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | string | ✅ | 物品唯一 ID。自动加命名空间前缀，你只需写 `sword` 不用写 `@author.sword` |
| `displayName` | object | ✅ | 显示名称。`{"zh-CN": "铁剑", "en": "Iron Sword"}` |
| `category` | string | ✅ | 分类路径。`weapon.melee` / `consumable.medical` / `armor.chest` / `tool` / `material` |
| `properties` | object | ✅ | 属性覆写。路径 → 值。见 3.2 |
| `icon` | string | ❌ | 图标路径。暂不支持自定义图标（AssetBundle 未来支持） |

### 3.2 常用属性路径

物品的属性走 PropertyTree 体系。以下是常用路径：

| 属性路径 | 类型 | 说明 | 示例值 |
|----------|------|------|--------|
| `Combat/BaseDamage` | float | 基础伤害 | `15.0` |
| `Combat/AttackSpeed` | float | 攻击速度倍率 | `1.2` |
| `Combat/CritChance` | float | 暴击率 (0~1) | `0.1` |
| `Combat/StaggerPower` | float | 失衡强度 | `5.0` |
| `Common/DurabilityMax` | float | 最大耐久 | `100` |
| `Common/Weight` | float | 重量 | `3.5` |
| `Common/StackSize` | int | 堆叠上限 | `64` |
| `Consumable/HealAmount` | float | 治疗量 | `25` |
| `Consumable/NutritionValue` | float | 食物饱腹值 | `50` |
| `Consumable/HydrationValue` | float | 饮水值 | `30` |

### 3.3 完整示例：新增一把武器

```json
{
  "id": "flame_blade",
  "displayName": { "zh-CN": "烈焰之刃", "en": "Flame Blade" },
  "category": "weapon.melee",
  "properties": {
    "Combat/BaseDamage": 28,
    "Combat/AttackSpeed": 0.9,
    "Combat/CritChance": 0.15,
    "Combat/StaggerPower": 8,
    "Common/DurabilityMax": 150,
    "Common/Weight": 4.0
  }
}
```

### 3.4 完整示例：新增消耗品

```json
{
  "id": "military_ration",
  "displayName": { "zh-CN": "军用口粮", "en": "Military Ration" },
  "category": "consumable.food",
  "properties": {
    "Consumable/NutritionValue": 80,
    "Consumable/HydrationValue": 10,
    "Consumable/HealAmount": 5,
    "Common/Weight": 0.5,
    "Common/StackSize": 20
  }
}
```

---

## 四、实体定义（content.entities）

### 4.1 字段参考

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | string | ✅ | 实体唯一 ID。如 `zombie_runner` |
| `displayName` | object | ✅ | 显示名称 |
| `category` | string | ✅ | `zombie` / `npc` / `animal` / `boss` |
| `properties` | object | ✅ | 属性覆写。见 4.2 |

### 4.2 常用属性路径

| 属性路径 | 类型 | 说明 | 示例值 |
|----------|------|------|--------|
| `Combat/BaseDamage` | float | 基础攻击伤害 | `20` |
| `Combat/AttackSpeed` | float | 攻击速度 | `1.0` |
| `Common/HealthMax` | float | 最大生命值 | `150` |
| `Common/MoveSpeed` | float | 移动速度 | `5.5` |
| `Common/Armor` | float | 护甲值 | `10` |
| `AI/DetectionRange` | float | 侦测范围 | `25` |
| `AI/AggressionLevel` | float | 攻击性 (0~1) | `0.8` |

### 4.3 完整示例：新增堕者变种

```json
{
  "id": "zombie_runner",
  "displayName": { "zh-CN": "狂奔堕者", "en": "Runner Zombie" },
  "category": "zombie",
  "properties": {
    "Common/HealthMax": 80,
    "Common/MoveSpeed": 9.0,
    "Combat/BaseDamage": 15,
    "Combat/AttackSpeed": 1.5,
    "AI/DetectionRange": 35,
    "AI/AggressionLevel": 1.0
  }
}
```

---

## 五、技能定义（content.abilities）

技能分两层：技能树（AbilityTree）和技能节点。一个 Mod 可以新增一棵技能树，也可以在官方树上嫁接新节点。

### 5.1 新增一棵技能树

```json
{
  "id": "flame_arts",
  "displayName": { "zh-CN": "烈焰武术", "en": "Flame Arts" },
  "description": { "zh-CN": "一套以火元素为核心的近战套路", "en": "A fire-element melee routine." },
  "category": "routine",
  "weaponTags": ["weapon.melee", "weapon.fire"],
  "gripTags": [],
  "exclusiveGroup": "",
  "nodes": []
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | string | ✅ | 树的唯一 ID |
| `displayName` | object | ✅ | 显示名称 |
| `description` | object | ❌ | 描述文本 |
| `category` | string | ✅ | 树的类别：`innate`（天生）/ `talent`（天赋）/ `routine`（套路） |
| `weaponTags` | string[] | ❌ | 兼容的武器标签。空 = 不限武器 |
| `gripTags` | string[] | ❌ | 兼容的握法标签。空 = 不限握法 |
| `exclusiveGroup` | string | ❌ | 互斥分组。同组只能选一个。空 = 不参与互斥 |
| `nodes` | object[] | ✅ | 树内的技能节点。见 5.2 |

### 5.2 技能节点

每个节点可以带一个主动技能、一个被动效果，或两者兼有。

```json
{
  "nodeId": "flame_slash",
  "prerequisites": [],
  "activeAbility": {},
  "passiveAbility": null
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `nodeId` | string | ✅ | 节点 ID。树内唯一 |
| `prerequisites` | string[] | ❌ | 前置节点的 nodeId。空数组 = 根节点（初始可解锁） |
| `activeAbility` | object | ❌ | 主动技能定义。null = 纯被动节点 |
| `passiveAbility` | object | ❌ | 被动效果定义。null = 纯主动节点 |

### 5.3 主动技能

```json
{
  "displayName": { "zh-CN": "烈焰斩", "en": "Flame Slash" },
  "activation": {
    "inputKey": "Q",
    "animationTrigger": "slash_fire",
    "cooldown": 3.0,
    "staminaCost": 25
  },
  "search": {
    "shape": "cone",
    "range": 3.0,
    "angle": 60,
    "filterTag": "enemy"
  },
  "effects": [
    { "type": "Damage", "multiplier": 1.5, "element": "Fire" },
    { "type": "ApplyBuff", "buffId": "burning", "duration": 5.0, "stacks": 1 }
  ],
  "noise": {
    "type": "combat",
    "radius": 20.0,
    "intensity": 0.5
  }
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `displayName` | object | ✅ | 技能显示名称 |
| `activation.inputKey` | string | ✅ | 激活按键。`Q` / `E` / `R` / `F` / `LeftMouse` / `RightMouse` |
| `activation.animationTrigger` | string | ✅ | 动画触发器名称。需匹配 Animator 中的 Trigger |
| `activation.cooldown` | float | ✅ | 冷却时间（秒） |
| `activation.staminaCost` | float | ❌ | 体力消耗 |
| `search.shape` | string | ❌ | 搜索形状。`cone` / `circle` / `ray` / `self` |
| `search.range` | float | ❌ | 搜索范围 |
| `search.angle` | float | ❌ | 锥形角度（仅在 shape=cone 时生效） |
| `search.filterTag` | string | ❌ | 目标筛选标签。`enemy` / `ally` / `self` |
| `effects` | object[] | ✅ | 效果数组。见 5.5 |
| `noise.type` | string | ❌ | 噪音类型。`stealth` / `combat` / `explosion` |

### 5.4 被动效果

```json
{
  "displayName": { "zh-CN": "火焰亲和", "en": "Fire Affinity" },
  "trigger": "OnEquip",
  "triggerValue": 0,
  "targetRequiredTag": null,
  "effects": [
    { "type": "ModifyStat", "stat": "Combat/FireResistance", "value": 25, "mode": "Add" }
  ]
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `displayName` | object | ✅ | 被动名称 |
| `trigger` | string | ✅ | 触发事件。`OnEquip` / `OnHit` / `OnKill` / `OnDamaged` / `OnLowHP` / `OnComboStage` |
| `triggerValue` | float | ❌ | 触发参数。OnLowHP 时 = HP 阈值 (0~1)，OnComboStage 时 = 连招段号 |
| `targetRequiredTag`| string | ❌ | 目标需持有此标签才触发。null = 无条件 |
| `effects` | object[] | ✅ | 效果数组 |

### 5.5 可用效果类型

| 效果 `type` | 说明 | 常用参数 |
|------------|------|---------|
| `Damage` | 造成伤害 | `multiplier`(伤害倍率), `element`(元素: Physical/Fire/Ice/Shock/Spore) |
| `Heal` | 恢复生命 | `amount`(恢复量) |
| `ApplyBuff` | 施加 Buff | `buffId`, `duration`(秒), `stacks`(层数) |
| `ModifyStat` | 修改属性 | `stat`(属性路径), `value`, `mode`(Add/Multiply/Override) |
| `SpawnProjectile` | 发射弹射物 | `projectileId`, `speed`, `count` |
| `Teleport` | 传送 | `target`(caster/target), `offset` |
| `AOE` | 范围效果 | `radius`, `innerEffects`(范围内目标施加的效果数组) |
| `Knockback` | 击退 | `distance`, `force` |
| `Cost` | 消耗资源 | `resource`(Stamina/Mana/Durability), `amount` |

### 5.6 完整示例：一棵带多个节点的技能树

```json
{
  "id": "flame_arts",
  "displayName": { "zh-CN": "烈焰武术", "en": "Flame Arts" },
  "description": { "zh-CN": "以火元素为核心的近战套路。", "en": "A fire-element melee routine." },
  "category": "routine",
  "weaponTags": ["weapon.melee"],
  "gripTags": [],
  "exclusiveGroup": "",
  "nodes": [
    {
      "nodeId": "flame_slash",
      "prerequisites": [],
      "activeAbility": {
        "displayName": { "zh-CN": "烈焰斩", "en": "Flame Slash" },
        "activation": { "inputKey": "Q", "animationTrigger": "slash_fire", "cooldown": 3.0, "staminaCost": 25 },
        "search": { "shape": "cone", "range": 3.0, "angle": 60, "filterTag": "enemy" },
        "effects": [
          { "type": "Damage", "multiplier": 1.5, "element": "Fire" },
          { "type": "ApplyBuff", "buffId": "burning", "duration": 5.0, "stacks": 1 }
        ]
      },
      "passiveAbility": null
    },
    {
      "nodeId": "flame_storm",
      "prerequisites": ["flame_slash"],
      "activeAbility": {
        "displayName": { "zh-CN": "烈焰风暴", "en": "Flame Storm" },
        "activation": { "inputKey": "E", "animationTrigger": "aoe_fire", "cooldown": 12.0, "staminaCost": 40 },
        "search": { "shape": "circle", "range": 5.0, "filterTag": "enemy" },
        "effects": [
          { "type": "AOE", "radius": 5.0, "innerEffects": [
            { "type": "Damage", "multiplier": 2.0, "element": "Fire" },
            { "type": "Knockback", "distance": 3.0, "force": 10.0 }
          ]}
        ]
      },
      "passiveAbility": null
    },
    {
      "nodeId": "fire_affinity",
      "prerequisites": ["flame_slash"],
      "activeAbility": null,
      "passiveAbility": {
        "displayName": { "zh-CN": "火焰亲和", "en": "Fire Affinity" },
        "trigger": "OnEquip",
        "effects": [
          { "type": "ModifyStat", "stat": "Combat/FireResistance", "value": 25, "mode": "Add" }
        ]
      }
    }
  ]
}
```

这棵树的结构：
```
flame_slash (Q, 根节点)
├── flame_storm (E, 需先解锁 flame_slash)
└── fire_affinity (被动, 需先解锁 flame_slash)
```

---

## 六、配方定义（content.recipes）

> ⚠️ 配方系统处于早期开发阶段。以下字段可能在正式版前变动。

```json
{
  "id": "craft_iron_sword",
  "displayName": { "zh-CN": "锻造铁剑", "en": "Forge Iron Sword" },
  "stationTag": "workbench.blacksmith",
  "inputs": [
    { "itemId": "resource.iron_ingot", "amount": 5 },
    { "itemId": "resource.leather_strip", "amount": 2 }
  ],
  "output": { "itemId": "weapon.iron_sword", "amount": 1 },
  "craftTime": 15.0,
  "requirements": {
    "techNode": "tech.smithing.basic",
    "skillLevel": { "skill": "crafting.blacksmith", "level": 2 }
  }
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | string | ✅ | 配方唯一 ID |
| `displayName` | object | ✅ | 配方显示名称 |
| `stationTag` | string | ✅ | 制作站标签。`workbench.basic` / `workbench.blacksmith` / `campfire` 等 |
| `inputs` | object[] | ✅ | 消耗材料。`itemId` + `amount` |
| `output` | object | ✅ | 产出物品。`itemId` + `amount` |
| `craftTime` | float | ✅ | 制作时间（秒） |
| `requirements.techNode` | string | ❌ | 需要解锁的科技节点 ID |
| `requirements.skillLevel` | object | ❌ | 需要的熟练度等级 |

---

## 七、覆盖官方数据（content.overrides）

覆盖不是合并——你提供的 JSON 会**整对象替换**官方定义。如果两个 Mod 覆盖了同一个 ID，加载优先级高的生效。

```json
{
  "content": {
    "overrides": {
      "items": {
        "item.bandage": {
          "properties": {
            "Consumable/HealAmount": 50,
            "Common/Weight": 0.1
          }
        }
      },
      "entities": {
        "entity.zombie.basic": {
          "properties": {
            "Common/HealthMax": 200,
            "Common/MoveSpeed": 4.0
          }
        }
      }
    }
  }
}
```

**覆盖的 ID 必须是官方 ID**（如 `item.bandage`），不能覆盖其他 Mod 的内容（Mod 间覆盖通过 `loadPriority` 排序）。

---

## 八、本地化（content.translations）

Mod 可以为游戏添加新语言的翻译文件：

```json
{
  "content": {
    "translations": {
      "locale": "ja",
      "entries": {
        "item.bandage": "包帯",
        "item.iron_sword": "鉄の剣",
        "ui.main_menu.play": "プレイ",
        "ui.inventory.weight": "重量"
      }
    }
  }
}
```

或者放在独立的翻译文件 `translations/ja.json` 中，manifest 中引用：

```json
{
  "translations": {
    "ja": "translations/ja.json",
    "ko": "translations/ko.json"
  }
}
```

更多细节见社区翻译文档（`localization.md`）。

---

## 九、C# 代码 Mod（进阶）

如果你会写 C#，可以通过 HybridCLR 写代码 Mod。代码 Mod 可以做 JSON 做不到的事——新效果类型、新 AI 行为、自定义 UI。

```csharp
// Mods/MyMod/mod.cs
using RedDust.Ability;

public class ExplosiveOnKillEffect : EffectSO
{
    public float radius = 5f;
    public float damage = 30f;

    public override void Execute(EffectContext ctx)
    {
        // 击杀时爆炸，对周围敌人造成伤害
        var targets = ctx.SearchArea(radius);
        foreach (var target in targets)
        {
            target.TakeDamage(damage, DamageType.Explosion);
        }
    }
}
```

**重要提醒**：
- **API 不稳定**——pre-1.0 阶段，游戏更新可能破坏你的 Mod。正式版后会逐步锁定 API
- **需要编程知识**——C# 基础 + 了解 RedDust 的 API（查看游戏目录下的 `Assembly-CSharp.dll` 或官方文档）
- **性能责任在你**——代码 Mod 的 bug 或性能问题由 Mod 作者负责

---

## 十、常见错误

| 错误 | 症状 | 解决 |
|------|------|------|
| JSON 逗号缺失 | Mod 加载失败，日志显示 JSON parse error | 检查最后一个字段后是否多了逗号，或两个字段间是否少了逗号 |
| 物品 ID 冲突 | 官方物品被你无意覆盖 | 检查 `id` 是否和官方 ID 重名。Mod 内容自动加命名空间前缀，一般不会冲突 |
| 属性路径不存在 | 物品出现但属性没变化 | 检查 property 路径拼写（大小写敏感）。参考本文档的属性路径列表 |
| 循环依赖 | Mod 加载失败，日志显示 cycle detected | 检查 `dependencies` 是否形成环（A 依赖 B，B 依赖 A） |
| 技能树节点 ID 重复 | 加载日志 warning | 同一棵树内 `nodeId` 必须唯一 |
| 前置节点不存在 | 加载日志 warning | `prerequisites` 引用的 `nodeId` 必须存在于同一棵树的节点列表中 |

---

## 关联文档

- [mod.md](mod.md) — Mod 系统总览（你能做什么、怎么发布）
- [mod-community-decision-record.md](../../plans/mod-community-decision-record.md) — 战略决策记录（了解背后的设计决策）
- [localization.md](../../../tech/shared/localization.md) — 社区翻译架构
