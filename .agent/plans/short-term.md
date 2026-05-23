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

> 更新: 2026-05-22
> 状态: 完成（架构重构为 Service + Core Scene 直显主菜单）

**目标**: MainMenu → 进入游戏 → VitalsOverlay 显示。

### 已完成

- UIManager + UIScreen/UIOverlay 架构
- MainMenu（PZ 风格，新游戏/退出）
- VitalsOverlay（HP/Hunger/Thirst/Stamina 实时显示）
- 颜色风格系统（UIColorSet × 5，Button/Panel 色彩角色映射）
- [ExecuteAlways] WYSIWYG
- Prefab 库：Button.prefab, Label.prefab, Panel.prefab, StatBar.prefab
- UIScreenId/UIOverlayId/UIModalId 枚举替换魔术字符串
- UIButton.SetText/SetInteractable, UILabel.SetText
- StatsTree 编辑器合并显示 + 继承 Bug 修复
- NewGame 场景（开发用，直进 Playing）
- ESC 切换 Playing ↔ Paused

### Phase 3.5: PauseMenu + Loading ✅

> 完成。UIPanelId 拆分为 UIScreenId/UIOverlayId/UIModalId 三枚举，TransitionWithLoading 替代 TransitionToGameplay。

- PauseMenuScreen：半透明遮罩 + 居中面板 + 继续/设置/保存/主菜单 四按钮
- LoadingOverlay：场景切换过渡，SetPhase/SetProgress API 预留
- TransitionWithLoading：通用异步加载协程，替换 TransitionToGameplay
- 完整闭环：MainMenu → Loading → Playing ↔ PauseMenu → MainMenu

### Phase 3.6: Service 架构加固 ✅

> 2026-05-23 完成。会话层显式化、TimeService 职责重整、Editor 直开 Core、IGameplaySessionHandler 接口。
> 详见 tech/modules/service-architecture.md

### 待办

- ~~命名统一: 全部 `S{What}IAction` → `SIAction{What}` (11 structs)~~ ✅ 2026-05-23
- ~~UI/Gameplay 时间线分离~~ ✅ 2026-05-23 — DOTween 全局 `defaultTimeScaleIndependent=true` + 基类 `DeltaTime` 属性
- StatusOverlay — 延后，暂不拓展 condition/buff 代码，仅通过 modifier 速率调整
- ClockOverlay — 延后
- MainMenu 加载存档/设置子面板 — 延后

> **整体策略**: 先完成玩法闭环（消耗 / 装备 / 拾取 / 战斗 / 僵尸 AI），再调整数值速率。

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
│   ├── UIScreenId.cs           UIOverlayId.cs        UIModalId.cs
│   ├── UIColorStyle.cs         UIScreen.cs           UIOverlay.cs
├── Config/
│   ├── UIThemeSO.cs            UIPanelConfigSO.cs
├── Components/
│   ├── UIButton.cs             UILabel.cs
│   ├── UIStatBar.cs            UIPanel.cs
├── MainMenu/
│   ├── MainMenuScreen.cs       PauseMenuScreen.cs
└── HUD/
    ├── VitalsOverlay.cs        StatusOverlay.cs      LoadingOverlay.cs
```

**依赖**: Phase 2.5 Stats 管理
**可玩增量**: 主菜单 → 进入游戏 → 数值条实时变化

---

## Phase 4: 战斗基础 + 敌人 AI 基础 + 噪音骨架

> 设计: `injury-system.md` `noise-system.md`

**目标**: 能砍丧尸，丧尸有听觉感知，攻击施加伤病。

### 战斗 + 伤病

| 子项 | 说明 |
|------|------|
| 近战攻击 | 鼠标左键触发，射线/碰撞检测 |
| 伤害判定 | 武器伤害 → DamageRule → 目标 HP 扣除 |
| 施加伤害 | 根据武器类型施加割伤/钝器伤/咬伤（丧尸）→ 触发流血/疼痛/感染 |
| 武器数据 | ItemDefSO 变体，伤害值/攻速/范围/噪音等级 |
| 命中反馈 | 音效 + 特效（已有 AudioChannel）+ 受击动画（简单） |

### 敌人 AI + 噪音

| 子项 | 说明 |
|------|------|
| 丧尸生成 | NavMesh 随机位置，基础属性（HP/伤害/移速） |
| 听觉感知 | 订阅 SNoiseEvent → 在半径内 → Alerted → 向声源移动 |
| 视觉感知 | 视线锥形范围 → 看到玩家 → 直接 Chase |
| 行为 FSM | Idle → Alerted → Investigating → Chase → Attack |
| SNoiseEvent | 走路/跑步/近战/开门 等行为发布噪音事件（等级 + 位置 + 类型） |
| 死亡 | HP 归零 → 播放死亡动画 → 移除 + 基础掉落（尸体上保留物品） |

**依赖**: Phase 2.5 DamageRule + Phase 1.5 音效 + EventDispatcher
**可玩增量**: 跑路发出噪音→丧尸听到→追你→你砍它→它流血→死亡→掉东西

**不做的**: 噪音连锁反应、障碍物衰减、潜行降噪、丧尸化过程（仅做感染值累积）

---

## Phase 5: 资源系统 + 负重 + 存档

> 设计: `inventory-weight.md` `death-mechanics.md`

**目标**: 物品拾取、背包管理、负重取舍、手动存档。

### 物品 + 背包

| 子项 | 说明 |
|------|------|
| ItemDefSO | 物品数据（ID/名称/图标/类型/重量/堆叠上限） |
| 地面物品 | 世界空间可拾取物，点击/靠近拾取 |
| 背包 UI | 物品列表 + 负重条（当前/上限 + 等级标签） |
| 消耗品 | 食物回饥饿、水回口渴、绷带止血、止痛药降疼痛 |

### 负重

| 子项 | 说明 |
|------|------|
| 重量字段 | 每物品配置重量值 |
| 四级负重 | 轻载/中载/重载/超载 → 移速/冲刺/体力惩罚 |
| 软上限 | 超载可慢走但体力持续消耗 → 力竭停止 |
| 背包装备 | 基础容量（无背包）+ 小背包/中型背包加成 |

### 存档

| 子项 | 说明 |
|------|------|
| 手动存档 | 暂停菜单 → "保存游戏" |
| 自动存档 | 返回据点时自动存一次 |
| 存档槽 | 1 自动槽 + 3 手动槽 |
| 死亡读档 | 死亡画面 → 读取最近存档 |

**依赖**: Phase 4 物品掉落 + Phase 3 UI + Phase 2 Stats
**可玩增量**: 捡东西→负重大了走不动→取舍→吃喝治疗→回家存档

---

## Phase 6: 建造基础

**目标**: 采集材料，搭建基础防御。

| 子项 | 说明 |
|------|------|
| 建造模式 | 按 B 进入，选建筑，网格预览，点击确认 |
| 基础建筑 | 木墙/木门/木地板，消耗木材 |
| 建筑耐久 | 墙体有 HP，可被丧尸攻击破坏 |
| 材料采集 | 树木→木材（临时交互） |

**可玩增量**: 砍树→建墙→围安全区→丧尸被挡在外面

---

## Phase 7: 时间与日夜

**目标**: 游戏内时钟、日夜光照切换。

| 子项 | 说明 |
|------|------|
| 时间系统 | 游戏内时钟，可配置时间流速 |
| 日夜光照 | Directional Light 旋转 + 强度/色温变化 |
| 视野限制 | 夜晚玩家视野缩小 |
| ClockOverlay | HUD 显示时间 + 天数 |

**可玩增量**: 天黑→视野变差→丧尸更危险→天亮恢复→昼夜节奏

---

## Phase 8-11 及扩展

详见 [long-term.md](long-term.md)：
- Phase 8: 农业 + 烹饪
- Phase 9: NPC 基础
- Phase 10: 尸潮基础
- Phase 11: 科技树基础
- Phase 12+: 全生态联通 + 扩展打磨

---

## 延后

- Vault / StepOver 障碍物穿越
- 姿势物理联动（crouch/prone）
- Crawl 爬行
- 角色动画增强（Hit React multi-layer / UpperBody 覆盖）
