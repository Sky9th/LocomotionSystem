# 短期开发计划 — 围绕 Character

> 日期: 2026-05-09
> 范围: P0 阶段，以 Character 为中心展开
> 原则: 每步有可玩增量，不预建空架子

## 路线总览

```
Phase 1 ──→ Phase 1.5 ──→ Phase 2 ──→ Phase 2.5 ──→ Phase 3 ──→ Phase 4 ──→ Phase 5
Loco完结   音效骨架     数值系统     Stats管理    HUD UI     战斗基础    动画增强
(已完成)    (已完成)     (已完成)    (已完成)     (已完成)    (后续)      (后续)
```

---

## Phase 1: LocomotionSystem 完结 ✅

**目标**: 运动系统达到可封装里程碑。

| 任务 | 状态 |
|------|------|
| HeadLook (归一化/平滑/冻结) | ✅ |
| Footstep (Animancer事件注入) | ✅ |

---

## Phase 1.5: 音效系统骨架 ✅

**目标**: 搭建音效系统最小骨架。

| 子项 | 状态 |
|------|------|
| AudioSetSO / AudioRequest / AudioResponse | ✅ |
| AudioChannel (static) | ✅ |
| CharacterAudio + FootstepSetSO | ✅ |
| 脚步回路接通 | ✅ |

---

## Phase 2: 通用数值系统 ✅

**目标**: 项目级 Stats 基础设施，角色作为首批消费者。

### 已完成

| 功能 | 状态 |
|------|------|
| StatsTreeSO + StatsNodeSO (树形SO) | ✅ |
| InheritsFrom 继承 + Resolve() | ✅ |
| 树路径做 Key（"Vitals/HP"） | ✅ |
| 能力接口（IStatConsumable/IStatRestorable/IStatCumulative）| ✅ |
| 修改器系统（StatModifier + ModifierContext 并行槽位）| ✅ |
| StatInstance.Tick 接口分派 + 修改器管道 | ✅ |
| StatDefSO + StatFactory | ✅ |
| CharacterStats 容器（路径 key）+ Actor 接入 | ✅ |
| CharacterStats.All 对外只读字典 | ✅ |
| StatsTreeWindow EditorWindow | ✅ |
| 基本 StatDef (HP/Hunger/Thirst/Stamina + 6 Attributes) | ✅ |
| Debug 打印 | ✅ |
| 去掉 StatType enum + Behaviors/ 目录 | ✅ |

### 划掉的探索

- ~~BindAll 概念~~ — 实为构造末尾一次字符串→引用解析，已合并进 CharacterStats
- ~~ConditionId/Condition 条件表~~ — 业务逻辑收敛于 Character，外部干预走修改器
- ~~SCharacterStatsSnapshot 独立 struct~~ — 直接暴露 `IReadOnlyDictionary<string, StatInstance>`

---

## Phase 2.5: Character Stats 管理 ✅

**目标**: Character 作为决策者，消费数值系统。Stat 自行 Tick，Character 管修改器、归零链、伤害入口。

| 子项 | 状态 |
|------|------|
| Rule 分层架构（5 种行为模式基类）| ✅ |
| SprintStaminaRule — 冲刺体力 ×3 | ✅ |
| HungerDepleteRule — 归零扣 HP | ✅ |
| DamageRule — 外伤攒批（待接入事件）| ✅ |
| Stats 推 SCharacterSnapshot | ✅ |
| Debug UI 显示实时数值 | ✅ |

**架构**: Rules 归 CharacterStats 管理，CharacterActor 只调 `stats.Update(ctx, dt)`

**依赖**: Phase 2 完整
**可玩增量**: 跑起来体力降，饿了扣血

---

## Phase 3: 基本 HUD UI ✅

> 更新: 2026-05-17
> 状态: 完成

**目标**: MainMenu → 进入游戏 → VitalsOverlay 显示。

### 已完成

- UIManager + UIScreen/UIOverlay 架构
- MainMenu（PZ 风格，新游戏/退出）
- VitalsOverlay（HP/Hunger/Thirst/Stamina 实时显示）
- 颜色风格系统（UIColorSet × 5，Button/Panel 色彩角色映射）
- [ExecuteAlways] WYSIWYG
- Prefab 库：Button.prefab, Label.prefab, Panel.prefab, StatBar.prefab
- UIPanelId 枚举替换魔术字符串
- UIButton.SetText/SetInteractable, UILabel.SetText
- StatsTree 编辑器合并显示 + 继承 Bug 修复
- NewGame 场景（开发用，直进 Playing）
- ESC 切换 Playing ↔ Paused

### 待办

- PauseMenu
- StatusOverlay
- ClockOverlay
- MainMenu 加载存档/设置子面板

### Prefab 资产

| Prefab | 路径 |
|--------|------|
| Button.prefab | Assets/Prefabs/UI/Button.prefab |
| Label.prefab | Assets/Prefabs/UI/Label.prefab |
| Panel.prefab | Assets/Prefabs/UI/Panel.prefab |
| StatBar.prefab | Assets/Prefabs/UI/StatBar.prefab |
| MainMenuScreen.prefab | Assets/Prefabs/UI/MainMenuScreen.prefab |
| VitalsOverlay.prefab | Assets/Prefabs/UI/VitalsOverlay.prefab |

UIThemeSO: Assets/Data/UI/UITheme.asset
UIPanelConfigSO: Assets/Data/UI/PanelConfig.asset

### 代码文件（15 个）

```
Assets/Scripts/UI/
├── UIManager.cs
├── Core/
│   ├── EUIPanelType.cs       EUIPanelId.cs       UIColorStyle.cs
│   ├── UIScreen.cs           UIOverlay.cs
├── Config/
│   ├── UIThemeSO.cs          UIPanelConfigSO.cs
├── Components/
│   ├── UIButton.cs           UILabel.cs
│   ├── UIStatBar.cs          UIPanel.cs
├── MainMenu/
│   └── MainMenuScreen.cs
└── HUD/
    ├── VitalsOverlay.cs      StatusOverlay.cs
```

**依赖**: Phase 2.5 Stats 管理
**可玩增量**: 主菜单 → 进入游戏 → 数值条实时变化

---

## Phase 4: 战斗基础

**目标**: 能做最简单的近战攻击。

| 子项 | 说明 |
|------|------|
| 近战攻击 | 鼠标左键触发，射线/碰撞检测 |
| 伤害判定 | 武器伤害 → 目标 HP 扣除 |
| 武器数据 | ScriptableObject，伤害值/攻速 |
| 基础反馈 | 命中音效/特效 |

**依赖**: Phase 2.5 的 HP 系统 + 伤害入口
**可玩增量**: 能砍丧尸，战斗循环建立

---

## Phase 5: 角色动画增强

**目标**: 受伤有视觉反馈，上半身动画独立。

| 子项 | 说明 |
|------|------|
| Hit React | 受击时播放受击动画 |
| UpperBody 覆盖 | 利用 Layer 1 + mask，上半身独立于下肢播放 |
| 多层仲裁雏形 | 为 UpperBody/Additive/Facial 铺路 |

**依赖**: Phase 4 + 多层仲裁器（同步实现）
**可玩增量**: 受击有反应，动画更自然

---

## 延后

- Vault / StepOver 障碍物穿越
- 姿势物理联动（crouch/prone）
- Crawl 爬行
