# 策划文档

> 面向策划和设计讨论。定义游戏机制、系统定位、玩家体验（WHY），不涉及技术实现（HOW）。

## 目录结构

```
design/
├── README.md                    ← 本文件
├── game-overview.md             ← GDD 总览：项目定位、世界观摘要、系统全景、核心循环
│
├── systems/                     ← 游戏机制详细设计
│   ├── combat/                  ← 战斗系统
│   │   ├── README.md            ← 战斗总览：操作方式 + 6 武器类型 + 系统接口
│   │   ├── skills.md            ← 技能系统：Ability 动作模式、武器-技能绑定
│   │   ├── proficiency.md       ← 熟练度：成长曲线、命中/暴击影响、近战vs枪械路线
│   │   └── damage-model.md      ← 伤害模型：装备地基 + 管道修正公式
│   │
│   ├── base/                    ← 据点建设
│   │   ├── README.md            ← 据点总览：4 个子系统如何协作
│   │   ├── building.md          ← 建造系统：网格化/分类/耐久/拆除/修复
│   │   ├── tech-tree.md         ← 科技树：图纸消耗/前置条件/解锁效果
│   │   ├── resources-tools.md   ← 资源与工具：六大类/工具耐久维修/存储/循环
│   │   └── farming-cooking.md   ← 农业与烹饪：开垦→播种→收获→食谱→士气
│   │
│   ├── npc.md                   ← NPC系统：指挥/工作/成长/招募 + 士气
│   ├── horde.md                 ← 尸潮系统：触发/规模/构成/行为/后效
│   ├── injury.md                ← 伤病系统：部位/5伤害类型/治疗/丧尸化
│   ├── death-save.md            ← 死亡与存档
│   ├── noise.md                 ← 噪音系统：6等级/传播/丧尸反应/连锁
│   ├── inventory-weight.md      ← 负重与背包
│   └── mod.md                   ← Mod 系统：社区创作支撑/零代码门槛/Steam Workshop
│
├── world/                       ← 世界观设定
│   ├── setting.md               ← 八重岛/母体雾区/封锁/幸存者/结局
│   └── spore-erosion-rules.md   ← 孢子侵蚀抗性梯度
│
└── data/                        ← 数值与配表设计
    └── stats-inventory.md       ← 全量 Stats 属性树 (~180 props)
```

## 文档约定

- **面向策划**：不包含代码、不涉及技术实现细节
- **每个文档 ~150-250 行**：AI 能完整理解，策划能一次读完
- **系统文档结构**：定位 → 设计原则 → 系统接口 → 详细机制 → 关联文档
- **不包含规划内容**：A测范围、暂缓列表、里程碑等已移至 `.agent/plans/`

## 快速导航

| 想了解… | 看这个 |
|---------|--------|
| 这是什么游戏？ | [game-overview.md](game-overview.md) |
| 战斗怎么打？ | [systems/combat/](systems/combat/) |
| 据点怎么建？ | [systems/base/](systems/base/) |
| NPC怎么管？ | [systems/npc.md](systems/npc.md) |
| 尸潮怎么来？ | [systems/horde.md](systems/horde.md) |
| 受伤怎么办？ | [systems/injury.md](systems/injury.md) |
| Mod 怎么做？ | [systems/mod.md](systems/mod.md) |
| 世界什么样？ | [world/setting.md](world/setting.md) |
| 有哪些属性？ | [data/stats-inventory.md](data/stats-inventory.md) |
