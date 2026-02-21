# Project 3 — Backlog Task List

This document lists all planned tasks for Project 3.  
Use the table below for a quick overview, or copy the individual issue bodies further down to create GitHub Issues manually (or run `scripts/create_issues.sh`).

---

## Task Overview

| # | Title | Priority | Labels | Estimate (h) | Description |
|---|-------|----------|--------|-------------|-------------|
| 1 | 资源系统框架 | P0 | 基础 | 20 | 物品数据结构 仓库 背包 鼠标拾取 |
| 2 | 生存指标 | P0 | 基础 | 44 | 饥饿/口渴/体力/生命 基础消耗恢复 |
| 3 | 建造系统原型 | P0 | 基础 | 48 | 网格化建造 放置/拆除 消耗材料 返还50% |
| 4 | 丧尸基础AI | P0 | 基础 | 48 | NavMesh 视觉感知 追逐 攻击 |
| 5 | 玩家基础控制 | P0 | 基础 | 48 | 鼠标移动 交互 UI框架 格子背包 |
| 6 | 地图原型 | P0 | 基础 | 28 | 用Synty资产搭建测试场景 |
| 7 | NPC基础AI | P1 | NPC | 32 | 玩家指令模式 框选 移动/攻击/工作 |
| 8 | NPC任务分配 | P1 | NPC | 48 | UI指派固定工作 自动产出 |
| 9 | NPC招募 | P1 | NPC | 36 | 解救幸存者 对话加入 |
| 10 | 战斗系统基础 | P1 | 战斗 | 36 | 近战武器 普通攻击 伤害计算 |
| 11 | 武器切换与技能栏 | P1 | 战斗 | 44 | 背包切换武器 技能栏更新 |
| 12 | 熟练度系统基础 | P1 | 战斗 | 36 | 武器类型熟练度 显示等级 |
| 13 | 农业系统 | P1 | 农业 | 56 | 农田建造 播种 生长 收获 留种 |
| 14 | 工具系统 | P1 | 工具 | 56 | 斧/镐/锤/锄/锯/厨具 耐久 维修 |
| 15 | 热武器基础 | P1 | 战斗 | 24 | 手枪 消耗弹药 噪音吸引 |
| 16 | 烹饪系统 | P2 | 烹饪 | 32 | 篝火 2种基础食谱 |
| 17 | 科技树界面 | P2 | 科技 | 32 | 列表式UI 显示节点 详情 |
| 18 | 图纸系统 | P2 | 科技 | 64 | 图纸物品 学习消耗 解锁效果 |
| 19 | 尸潮触发机制 | P2 | 尸潮 | 48 | 每日伪随机 概率动态调整 |
| 20 | 尸潮规模与构成 | P2 | 尸潮 | 24 | 简单公式 天数×系数 |
| 21 | 尸潮行为 | P2 | 尸潮 | 40 | 向据点移动 攻击建筑 掉落 |

---

## Copy-Pastable Issue Bodies

Each section below can be copied directly into a GitHub Issue body.  
Set the issue **Title** and **Labels** as noted in the header of each section.

---

### Task 1 — 资源系统框架

> **Title:** `[Task] 资源系统框架`  
> **Labels:** `P0`, `基础`

```
**Priority:** P0
**Estimate:** 20h
**Labels:** 基础

## Description
物品数据结构 仓库 背包 鼠标拾取

## Acceptance Criteria
- [ ] 物品数据结构设计完成（ScriptableObject 或等效方案）
- [ ] 仓库系统可存取物品
- [ ] 背包系统支持格子布局
- [ ] 鼠标拾取物品可正常触发
```

---

### Task 2 — 生存指标

> **Title:** `[Task] 生存指标`  
> **Labels:** `P0`, `基础`

```
**Priority:** P0
**Estimate:** 44h
**Labels:** 基础

## Description
饥饿/口渴/体力/生命 基础消耗恢复

## Acceptance Criteria
- [ ] 饥饿值随时间降低，归零时扣血
- [ ] 口渴值随时间降低，归零时扣血
- [ ] 体力消耗与恢复逻辑正确
- [ ] 生命值 UI 实时更新
```

---

### Task 3 — 建造系统原型

> **Title:** `[Task] 建造系统原型`  
> **Labels:** `P0`, `基础`

```
**Priority:** P0
**Estimate:** 48h
**Labels:** 基础

## Description
网格化建造 放置/拆除 消耗材料 返还50%

## Acceptance Criteria
- [ ] 网格预览显示正确
- [ ] 放置时消耗对应材料
- [ ] 拆除时返还50%材料
- [ ] 建筑物碰撞体正确生成
```

---

### Task 4 — 丧尸基础AI

> **Title:** `[Task] 丧尸基础AI`  
> **Labels:** `P0`, `基础`

```
**Priority:** P0
**Estimate:** 48h
**Labels:** 基础

## Description
NavMesh 视觉感知 追逐 攻击

## Acceptance Criteria
- [ ] 丧尸可在 NavMesh 上正常导航
- [ ] 视觉感知范围与角度参数可配置
- [ ] 发现玩家后进入追逐状态
- [ ] 到达攻击距离后触发攻击动画与伤害
```

---

### Task 5 — 玩家基础控制

> **Title:** `[Task] 玩家基础控制`  
> **Labels:** `P0`, `基础`

```
**Priority:** P0
**Estimate:** 48h
**Labels:** 基础

## Description
鼠标移动 交互 UI框架 格子背包

## Acceptance Criteria
- [ ] 鼠标点击移动玩家至目标位置
- [ ] 可与场景物件进行交互（拾取/使用）
- [ ] UI框架骨架搭建完成（HUD、菜单入口）
- [ ] 格子背包可打开、关闭、显示物品
```

---

### Task 6 — 地图原型

> **Title:** `[Task] 地图原型`  
> **Labels:** `P0`, `基础`

```
**Priority:** P0
**Estimate:** 28h
**Labels:** 基础

## Description
用Synty资产搭建测试场景

## Acceptance Criteria
- [ ] 测试场景包含地形、建筑、植被等基础元素
- [ ] NavMesh Bake 完成，AI可正常寻路
- [ ] 场景可在编辑器和运行时正常加载
```

---

### Task 7 — NPC基础AI

> **Title:** `[Task] NPC基础AI`  
> **Labels:** `P1`, `NPC`

```
**Priority:** P1
**Estimate:** 32h
**Labels:** NPC

## Description
玩家指令模式 框选 移动/攻击/工作

## Acceptance Criteria
- [ ] 支持框选多个NPC
- [ ] 右键指定移动目标
- [ ] 右键敌人时NPC进入攻击状态
- [ ] 右键工作点时NPC进入工作状态
```

---

### Task 8 — NPC任务分配

> **Title:** `[Task] NPC任务分配`  
> **Labels:** `P1`, `NPC`

```
**Priority:** P1
**Estimate:** 48h
**Labels:** NPC

## Description
UI指派固定工作 自动产出

## Acceptance Criteria
- [ ] UI面板可将NPC指派至指定工作岗位
- [ ] 岗位工作循环正确执行
- [ ] 自动产出物品进入仓库
```

---

### Task 9 — NPC招募

> **Title:** `[Task] NPC招募`  
> **Labels:** `P1`, `NPC`

```
**Priority:** P1
**Estimate:** 36h
**Labels:** NPC

## Description
解救幸存者 对话加入

## Acceptance Criteria
- [ ] 场景中幸存者NPC可被发现
- [ ] 对话系统触发招募流程
- [ ] 招募后NPC加入队伍并可被指挥
```

---

### Task 10 — 战斗系统基础

> **Title:** `[Task] 战斗系统基础`  
> **Labels:** `P1`, `战斗`

```
**Priority:** P1
**Estimate:** 36h
**Labels:** 战斗

## Description
近战武器 普通攻击 伤害计算

## Acceptance Criteria
- [ ] 近战武器可装备
- [ ] 普通攻击动画与命中检测正确
- [ ] 伤害公式可配置并正确计算
```

---

### Task 11 — 武器切换与技能栏

> **Title:** `[Task] 武器切换与技能栏`  
> **Labels:** `P1`, `战斗`

```
**Priority:** P1
**Estimate:** 44h
**Labels:** 战斗

## Description
背包切换武器 技能栏更新

## Acceptance Criteria
- [ ] 从背包拖拽武器至技能栏
- [ ] 技能栏快捷键切换武器
- [ ] 武器切换时角色动画与状态正确更新
```

---

### Task 12 — 熟练度系统基础

> **Title:** `[Task] 熟练度系统基础`  
> **Labels:** `P1`, `战斗`

```
**Priority:** P1
**Estimate:** 36h
**Labels:** 战斗

## Description
武器类型熟练度 显示等级

## Acceptance Criteria
- [ ] 使用武器后对应类型熟练度经验增加
- [ ] 达到阈值后等级提升
- [ ] UI正确显示当前熟练度等级与进度
```

---

### Task 13 — 农业系统

> **Title:** `[Task] 农业系统`  
> **Labels:** `P1`, `农业`

```
**Priority:** P1
**Estimate:** 56h
**Labels:** 农业

## Description
农田建造 播种 生长 收获 留种

## Acceptance Criteria
- [ ] 可建造农田格
- [ ] 选择种子播种
- [ ] 植物按时间阶段生长（可配置周期）
- [ ] 达到成熟阶段后可收获
- [ ] 收获时概率保留种子
```

---

### Task 14 — 工具系统

> **Title:** `[Task] 工具系统`  
> **Labels:** `P1`, `工具`

```
**Priority:** P1
**Estimate:** 56h
**Labels:** 工具

## Description
斧/镐/锤/锄/锯/厨具 耐久 维修

## Acceptance Criteria
- [ ] 各类工具可装备并触发对应交互
- [ ] 使用时耐久降低
- [ ] 耐久归零时工具损坏/效率降低
- [ ] 可使用材料维修工具
```

---

### Task 15 — 热武器基础

> **Title:** `[Task] 热武器基础`  
> **Labels:** `P1`, `战斗`

```
**Priority:** P1
**Estimate:** 24h
**Labels:** 战斗

## Description
手枪 消耗弹药 噪音吸引

## Acceptance Criteria
- [ ] 手枪可装备并射击
- [ ] 射击消耗对应弹药
- [ ] 射击产生噪音范围，吸引周围丧尸
```

---

### Task 16 — 烹饪系统

> **Title:** `[Task] 烹饪系统`  
> **Labels:** `P2`, `烹饪`

```
**Priority:** P2
**Estimate:** 32h
**Labels:** 烹饪

## Description
篝火 2种基础食谱

## Acceptance Criteria
- [ ] 可建造篝火
- [ ] 篝火UI显示可用食谱
- [ ] 至少2种食谱可正确制作
- [ ] 制作后获得食物物品
```

---

### Task 17 — 科技树界面

> **Title:** `[Task] 科技树界面`  
> **Labels:** `P2`, `科技`

```
**Priority:** P2
**Estimate:** 32h
**Labels:** 科技

## Description
列表式UI 显示节点 详情

## Acceptance Criteria
- [ ] 科技树UI以列表形式展示所有节点
- [ ] 点击节点显示详情（需求、效果）
- [ ] 已解锁节点有明显标识
```

---

### Task 18 — 图纸系统

> **Title:** `[Task] 图纸系统`  
> **Labels:** `P2`, `科技`

```
**Priority:** P2
**Estimate:** 64h
**Labels:** 科技

## Description
图纸物品 学习消耗 解锁效果

## Acceptance Criteria
- [ ] 图纸作为物品可拾取
- [ ] 使用图纸消耗对应资源并学习
- [ ] 学习后解锁对应配方/建筑/能力
```

---

### Task 19 — 尸潮触发机制

> **Title:** `[Task] 尸潮触发机制`  
> **Labels:** `P2`, `尸潮`

```
**Priority:** P2
**Estimate:** 48h
**Labels:** 尸潮

## Description
每日伪随机 概率动态调整

## Acceptance Criteria
- [ ] 每日游戏内时间结束时进行尸潮概率检定
- [ ] 初始概率与动态系数可配置
- [ ] 触发后进入尸潮状态并通知玩家
```

---

### Task 20 — 尸潮规模与构成

> **Title:** `[Task] 尸潮规模与构成`  
> **Labels:** `P2`, `尸潮`

```
**Priority:** P2
**Estimate:** 24h
**Labels:** 尸潮

## Description
简单公式 天数×系数

## Acceptance Criteria
- [ ] 尸潮规模根据天数×系数公式计算
- [ ] 系数可配置
- [ ] 丧尸类型构成随规模动态调整
```

---

### Task 21 — 尸潮行为

> **Title:** `[Task] 尸潮行为`  
> **Labels:** `P2`, `尸潮`

```
**Priority:** P2
**Estimate:** 40h
**Labels:** 尸潮

## Description
向据点移动 攻击建筑 掉落

## Acceptance Criteria
- [ ] 尸潮期间丧尸向玩家据点方向移动
- [ ] 丧尸可攻击并破坏建筑
- [ ] 击杀丧尸有概率掉落物品
```
