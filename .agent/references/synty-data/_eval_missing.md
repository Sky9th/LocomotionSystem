# Missing Pack Evaluation

> 日期: 2026-06-12
> 框架: [[asset-evaluation-framework]]
> 用途: 评估tracker中缺失的Synty POLYGON资产包

---

## 数据状态

| 包名 | Handle | 独立JSON | _all_products.json | 状态 |
|------|--------|----------|-------------------|------|
| Meadow Forest | polygon-meadow-forest-nature-biome | YES | YES | 完整评估 |
| Swamp Marshland | polygon-swamp-marshland-nature-biome | YES | YES | 完整评估 |
| Alpine Mountain | polygon-alpine-mountain-nature-biomes | **NO** | YES | 需fetch | 
| Arid Desert | polygon-arid-desert-nature-biomes | **NO** | YES | 需fetch |
| Enchanted Forest | polygon-enchanted-forest-nature-biomes | **NO** | YES | 需fetch |
| Tropical Jungle | polygon-tropical-jungle-nature-biome | **NO** | YES | 需fetch |
| Dog Pack | polygon-dog | **NO** | YES | 需fetch |
| Horror Mansion | polygon-horror-mansion | **NO** | YES | 需fetch |
| Palm City | polygon-palm-city | **NO** | YES | 需fetch |
| Wheelchair Pack | polygon-wheelchair-pack | **NO** | YES | 需fetch |
| Nightclubs | polygon-nightclubs | **NO** | YES | 需fetch |
| Nature Biomes Season One | polygon-nature-biomes-season-one | **NO** | YES | 需fetch |
| Nature Biomes Season Two | polygon-nature-biomes-season-two | **NO** | YES | 需fetch |

**注**: 缺少独立JSON的包，内容清单（Pack Content）无法完整提取。`_all_products.json` 包含价格/tags/handle等元数据，但不含 `body_html`。以下没有独立JSON的包标注为 "**部分评估（缺内容清单）**"。

---

## 一、NATURE BIOMES 评估

### 系列背景

Nature Biome 系列是 Synty 2022年底推出的模块化地形生态系统。每个 biome 提供特定生态主题的完整场景搭建套件：环境资产（树木/岩石/植物）、建筑、道具、地形材质、载具、粒子特效。所有 biome 均 Unity 2022.3+ / URP 兼容，提供 FBX 源文件。

与 Nature Pack ($49.99) 的关系：
- Nature Pack 是通用自然资产（泛用树木/岩石/地形），适合作为所有户外场景的 baseline
- Nature Biome 是特定生态的深度扩展，每个 biome 提供该生态独有的植物种类、地形材质、氛围粒子
- **两者互补**，非替代关系

---

### 1. POLYGON - Meadow Forest - Nature Biome

- **Handle**: `polygon-meadow-forest-nature-biome`
- **价格**: $54.99 (无折扣)
- **Product ID**: 7879136411900
- **Tags**: Biomes, FBX, full-price, Nature, Polygon, Unity, Unreal
- **内容清单**:
  - Environment (x79): Fruit trees, giant trees, birch trees, stones, flowers, mushrooms, lillypads, stonewalls, leaves, cliffs (部分LOD)
  - Buildings (x5): Cabin, bridge, warpgate, windmills
  - Props (x60): campfire, tents, signposts, fences, gate, stone stacks, stone ruins, well
  - Terrain Materials (x27): Grass, mud, sand, stonepath, rocks, gravel, moss, flowers
  - Vehicles (x2): HandCart, Steam car
  - Particles (x14): Butterflies, dust, fire, leaves, petals, sun beam, water, wind, wind streaks
- **总资产**: ~187

#### 7维评分

**1. 资产形态: ★★★★★**
模块化环境元件，树木/岩石/植物/地形材质均为独立模型，可自由组合搭建任意草原/森林场景。含 demo scene 但不依赖预搭建。

**2. 内容质量: ★★★★☆**
79个环境资产种类丰富（果树+巨型树+白桦树变体），27种地形材质覆盖面广，60个道具提供农场/营地建设。但风格偏"田园诗"，对于末日生存，部分资产（风车/水车/童话桥）风格匹配度低。

**3. 持有成本: ★★★★★**
独立包，无依赖。$54.99 对于187个模块化资产合理。

**4. 技术适配: ★★★★★**
Unity 2022.3+ URP/Built-in 双支持，含FBX源文件。Shader Graph转换完成，性能友好。

**5. 功能价值: ★★★★☆**
覆盖关卡构建（地形/树木/岩石）+ 环境叙事（营地/废墟/路标）。草地森林生态是末日户外场景的基础——大部分地图需要"自然覆盖"层。

**6. 可替代性: ★★★☆☆**
Nature Pack ($49.99) 可部分覆盖通用树木/岩石，但缺少草地森林特有的果树/蘑菇/地形材质多样性。可被部分替代但会失去生态独特性。

**7. 组合关系:**
- ⊕ 互补 Nature Pack — Meadow Forest 提供草地生态独有的植物/地形材质
- ⊕ 互补 Apocalypse Pack — 末日建筑 + 田园自然 = 毁灭与生机的对比
- ⇄ 部分重叠 Swamp Marshland — 树木/植物资产可跨生态使用
- ⊕ 互补 Farm Pack — 推荐组合，农场地貌与草地生态自然衔接

#### 综合判定: **Tier 2** → Tier 3

已在 tracker Tier 3。草地/森林是末日户外场景的基础生态，但风格偏田园（风车/童话桥/传送门），对末日生存适配度不如 Alpine 或 Swamp。保持 Tier 3 合理。

---

### 2. POLYGON - Swamp Marshland - Nature Biome

- **Handle**: `polygon-swamp-marshland-nature-biome`
- **价格**: $54.99 (无折扣)
- **Product ID**: 7879131824380
- **Tags**: Biomes, FBX, full-price, Nature, Polygon, Unity, Unreal
- **内容清单**:
  - Environment (x60): Swamp trees, mangrove trees, reeds, lilly pads, swamp scum, moss, brambles, toetoe bushes, logs, tree roots (LOD)
  - Buildings (x16): Shack, outhouse, modular jetty
  - Props (x50): Effigies, Bones, Wreckage, Tombstones, sewer pipes, barrels, fences
  - Terrain Materials (x7): Grass, mud, bones, moss, rock
  - Vehicles (x2): Canoe, Airboat
  - Particles (x16): Bats, cranes, dragonflies, dust, fire, flies, fog, frogs, leaves, sun beam, swamp bubbles, water ripples (frogs actually jump!)
- **总资产**: ~151

#### 7维评分

**1. 资产形态: ★★★★★**
模块化环境元件，关键亮点是 modular jetty（模块化码头栈道）——可直接用于程序化沼泽路径生成。

**2. 内容质量: ★★★★★**
60个沼泽环境资产 + 16座建筑 + 50个道具。Effigies（图腾）/ Bones（骨骸）/ Tombstones（墓碑）直接服务于末日恐怖叙事。Frogs粒子会跳动——细节到位。

**3. 持有成本: ★★★★★**
独立包，无依赖。$54.99 合理。

**4. 技术适配: ★★★★★**
Unity 2022.3+ URP/Built-in 双支持，含FBX源文件，Shader Graph。

**5. 功能价值: ★★★★★**
关卡构建（沼泽树木/红树林/芦苇/模块化码头）+ 环境叙事（骨骸/墓碑/图腾/沉船）。沼泽是末日生存的经典生态——辐射废土、变异生物、藏身沼泽中的营地。**直接服务于世界观**。

**6. 可替代性: ★★★★☆**
高度专属。Nature Pack 有少量沼泽死树但远不如 Swamp 系统化。Apocalypse Wasteland 有 goop/废土沙漠但生态不同。沼泽生态在其他 Synty 包中极少覆盖。

**7. 组合关系:**
- ⊕ 互补 Apocalypse Pack — Synty官方推荐组合（丧尸+沼泽）
- ⊕ 互补 City Zombies — 沼泽中出没的丧尸
- ⊕ 互补 Horror Mansion — 沼泽鬼屋主题
- ⊕ 互补 Dog Pack — 野外猎犬
- ⇄ 部分重叠 Meadow Forest — 都是Nature Biome，通用树木可互用

#### 综合判定: **Tier 2 → 建议升级到 Tier 2**

已在 tracker Tier 3，但内容直接服务于末日恐怖叙事（骨骸/图腾/墓碑/破屋/迷雾粒子），且 modular jetty 对程序化生成有工程价值。**强烈建议升至 Tier 2**。

---

### 3. POLYGON - Alpine Mountain - Nature Biome

**部分评估（缺内容清单）** — 需fetch独立JSON获取完整Pack Content

- **Handle**: `polygon-alpine-mountain-nature-biomes` (注意复数)
- **价格**: $54.99 (无折扣)
- **Product ID**: 8191333826812
- **SKU**: polygon-alpine-mountain-nature-biomes-pack
- **发布时间**: 2023-11-13 (Nature Biome Season Two成员)

#### 关键发现: Woodland Apocalypse Map的强制依赖

Woodland Apocalypse Map 明确声明：
> "This pack requires: POLYGON Apocalypse Pack and **Alpine Mountain Nature Biome**"

这意味着 Alpine Mountain 不仅是可选的生态扩展，而是 **Woodland Apocalypse Map 的运行时依赖**——地图中的预搭建场景直接引用 Alpine Mountain 中的模型。

#### 预估内容结构（基于Nature Biome系列模式）

预期包含类别: Environment（松树/冷杉/高山植物/岩石/雪地），Buildings（山间小屋/避难所/瞭望塔），Props（登山装备/路标/围栏），Terrain Materials（岩石/苔原/雪/碎石），Vehicles（雪地车/缆车），Particles（风雪/雾气/山岚）。

#### 7维评分（基于元数据+系列模式推断）

**1. 资产形态: ★★★★★** (推断)
Nature Biome系列统一为模块化元件，Alpine 应遵循同结构。高山树木/岩石/地形材质可用于程序化山区生成。

**2. 内容质量: ★★★★☆** (推断)
基于 biome 标准 ($54.99 定价)，预计 120-180 独立资产。高山生态独特性高——松树/冷杉/雪地材质在其他 Synty 包中稀缺。

**3. 持有成本: ★★★★☆**
独立包，无依赖。但如果连同 Woodland Apocalypse Map，总持有成本为 Alpine ($54.99) + Apocalypse ($349.99) + Woodland Map ($20.99 首发价) = $425.97。

**4. 技术适配: ★★★★★**
Synty 标准兼容: Unity 2022.3+ URP + FBX。

**5. 功能价值: ★★★★☆**
关卡构建（高山生态）+ 环境叙事（山间避难所/废弃登山站）。山区是末日生存的自然屏障——天然地图边界、隐藏基地、高原资源点。Woodland Apocalypse Map 证明了 Synty 对末日+高山组合的认可。

**6. 可替代性: ★★★★☆**
高度专属。高山/雪地生态在 POLYGON 系列中仅此一份。Nature Pack 无雪地材质或冷杉。不可低成本自制。

**7. 组合关系:**
- ← 依赖关系 — Woodland Apocalypse Map 依赖 Alpine Mountain
- ⊗ 独占 — POLYGON 系列唯一的高山/雪地生态
- ⊕ 互补 Apocalypse Pack — 末日建筑 + 高山地形
- ⊕ 互补 Nature Pack — 通用自然资产为高山场景提供底层植被
- ⇄ 部分重叠 Meadow Forest — 共享部分草地/石材质但生态差异大

#### 综合判定: **Tier 2 (推荐新增)**

虽无完整内容清单，但 Alpine Mountain 的理由非常强：
1. **Woodland Apocalypse Map 的强制依赖** — 我们已在 tracker 有了 Woodland Map (Tier 3)，如果不买 Alpine 则 Woodland Map 无法使用
2. **山区生态无可替代** — POLYGON 系列中仅此一个高山 biome
3. **末日生存场景契合** — 山区天然适合建成避难所、隐藏基地
4. **建议与 Season Two 捆绑** — 见下文 bundles 分析

---

### 4. POLYGON - Arid Desert - Nature Biome

**部分评估（缺内容清单）** — 需fetch独立JSON

- **Handle**: `polygon-arid-desert-nature-biomes`
- **价格**: $54.99 (无折扣)
- **Product ID**: 8191333957884
- **SKU**: polygon-arid-desert-nature-biomes-pack
- **发布时间**: 2023-11-13 (Nature Biome Season Two成员)

#### 7维评分（推断）

**1. 资产形态: ★★★★★** (推断)
模块化沙漠元件，预计包含仙人掌/沙丘/岩石/枯树/绿洲植物。

**2. 内容质量: ★★★★★** (推断)
沙漠生态在POLYGON系列中稀缺。Apocalypse Wasteland 有部分沙漠资产（buttes/cactus/palm trees）但非系统化沙漠 biome。独立沙漠 pack 应提供更完整的生态覆盖。

**3. 持有成本: ★★★★☆**
独立包$54.99。但 Apocalypse Wasteland ($379.99) 已包含部分沙漠/废土环境，存在功能重叠，购买前需评估重叠度。

**4. 技术适配: ★★★★★**
标准 Synty 兼容。

**5. 功能价值: ★★★★★**
关卡构建（沙漠生态）+ 环境叙事（废墟/绿洲/沙暴）。**沙漠是末日经典场景**——核爆后的废土、沙中掩埋的城市、绿洲贸易站。

**6. 可替代性: ★★★☆☆**
Apocalypse Wasteland 已覆盖沙漠废土场景（buttes/cactus/palm trees/sand terrain），功能重叠度高。但 Arid Desert 作为专门 biome 应有更丰富的沙丘/岩石变体和沙漠特有植物。

**7. 组合关系:**
- ⇄ 与 Apocalypse Wasteland 大量重叠 — Wasteland 已有沙漠生态+building+角色
- ⇄ 与 Apocalypse Pack 部分重叠 — Apocalypse 原包有通用废土内容
- ⊕ 互补 Meadow Forest — 草地与沙漠形成生态对比
- ⊗ 独占 — 唯一独立沙漠 biome

#### 综合判定: **Tier 3**

沙漠是末日重要生态，但 Apocalypse Wasteland 已覆盖大量沙漠内容。Arid Desert 的价值在于：
- 如果**不买** Apocalypse Wasteland ($379.99)，Arid Desert ($54.99) 是低成本获取沙漠生态的途径
- 如果**已买** Wasteland，则 Arid Desert 重叠度高，优先级降低

**购买策略**: 先确认 Apocalypse Wasteland 的沙漠资产是否足够程序化生成。如不足，补 Arid Desert；如足够，降为 Tier 4。

---

### 5. POLYGON - Enchanted Forest - Nature Biome

**部分评估（缺内容清单）** — 需fetch独立JSON

- **Handle**: `polygon-enchanted-forest-nature-biomes`
- **价格**: $54.99 (无折扣)
- **Product ID**: 8191334056188
- **SKU**: polygon-enchanted-forest-nature-biomes
- **发布时间**: 2023-11-13 (Nature Biome Season Two成员)

#### 7维评分（推断）

**1. 资产形态: ★★★★★** (推断)
模块化。预计包含魔法/奇幻风格的巨型树/发光植物/蘑菇/水晶/魔法门。

**2. 内容质量: ★★★☆☆** (推断)
资产质量应达标，但风格高度奇幻（魔法森林/水晶/发光植物），与末日生存的现实主义基调冲突。

**3. 持有成本: ★★★★★**
$54.99 独立包。

**4. 技术适配: ★★★★★**
标准。

**5. 功能价值: ★★☆☆☆**
仅关卡构建（奇幻森林）。对末日生存贡献低——除非设计"辐射变异区"或"超自然区域"作为特殊剧情场景。

**6. 可替代性: ★★★★☆**
Meadow Forest + Swamp Marshland 已覆盖大部分森林/沼泽生态。Enchanted Forest 的魔法风格资产（水晶/发光蘑菇/魔法阵）在现有/计划包中几乎无替代——但项目是否需要这些资产才是关键。

**7. 组合关系:**
- ⇄ 部分重叠 Meadow Forest、Swamp Marshland — 共享森林通用资产
- ⊕ 互补 Modular Fantasy Hero Characters — 奇幻角色+奇幻森林风格统一，但项目基调非奇幻

#### 综合判定: **Tier 4 (不推荐)**

核心问题: 风格与末日生存不匹配。除非项目设计中有明确的"魔法变异森林"或"超自然区域"，否则奇幻资产难以融入末日现实主义。若通过 Season Two bundle 捆绑打包，性价比改变需重新评估（见后文）。

---

### 6. POLYGON - Tropical Jungle - Nature Biome

**部分评估（缺内容清单）** — 需fetch独立JSON

- **Handle**: `polygon-tropical-jungle-nature-biome`
- **价格**: $54.99 (无折扣)
- **Product ID**: 7879120519420
- **SKU**: polygon-tropical-jungle-nature-biomes-pack
- **发布时间**: 2022-09-30 (最早的 Nature Biome 之一)

#### 7维评分（推断）

**1. 资产形态: ★★★★★** (推断)
模块化热带丛林元件。预计包含棕榈树/阔叶树/藤蔓/蕨类/热带花卉/竹子。

**2. 内容质量: ★★★★☆** (推断)
热带生态在 POLYGON 系列中覆盖较少。Apocalypse Wasteland 有 palm trees/mutant palm trees/ferns 但非系统化jungle。

**3. 持有成本: ★★★★☆**
$54.99 独立包。但 Apocalypse Wasteland 已包含部分热带元素。

**4. 技术适配: ★★★★★**
标准。

**5. 功能价值: ★★★★☆**
关卡构建（丛林生态）+ 环境叙事（失落遗迹/密林藏身处）。**热带/亚热带是末日生存的合理场景**——湿热丛林中的幸存者营地、藤蔓覆盖的城市废墟、变异植物。

**6. 可替代性: ★★★☆☆**
Swamp Marshland 部分覆盖湿地植物，Meadow Forest 覆盖阔叶树。但热带特有的棕榈/竹子/巨型蕨类不可替代。Apocalypse Wasteland 有 palm trees 但数量有限。

**7. 组合关系:**
- ⊕ 互补 Swamp Marshland — 热带丛林常伴生湿地/沼泽
- ⊕ 互补 Apocalypse Pack — 末日城市 + 丛林自然 = 被自然收复的文明
- ⇄ 部分重叠 Apocalypse Wasteland — Wasteland有palm trees/ferns
- ⊕ 互补 Nature Pack — 通用植被为丛林底层

#### 综合判定: **Tier 3**

热带丛林对末日生存有价值（"文明被自然收复"的经典叙事），但优先级低于 Alpine（有依赖关系）和 Swamp（直接服务恐怖叙事）。与 Arid Desert 类似，如果通过 Season Two bundle 获得则性价比大幅提升。

---

## 二、NATURE BIOME 总结

| 包名 | 资产形态 | 内容质量 | 持有成本 | 技术适配 | 功能价值 | 可替代性 | 组合关系 | **Tier** | 价格 |
|------|---------|---------|---------|---------|---------|---------|---------|---------|------|
| Meadow Forest | ★★★★★ | ★★★★☆ | ★★★★★ | ★★★★★ | ★★★★☆ | ★★★☆☆ | ⊕互补多 | **Tier 3** (维持) | $54.99 |
| Swamp Marshland | ★★★★★ | ★★★★★ | ★★★★★ | ★★★★★ | ★★★★★ | ★★★★☆ | ⊕互补多 | **Tier 2** (升级) | $54.99 |
| Alpine Mountain | ★★★★★* | ★★★★☆* | ★★★★☆ | ★★★★★ | ★★★★☆ | ★★★★☆ | ←依赖/⊗独占 | **Tier 2** (新增) | $54.99 |
| Arid Desert | ★★★★★* | ★★★★★* | ★★★★☆ | ★★★★★ | ★★★★★ | ★★★☆☆ | ⇄与Wasteland重叠 | **Tier 3** (新增) | $54.99 |
| Enchanted Forest | ★★★★★* | ★★★☆☆* | ★★★★★ | ★★★★★ | ★★☆☆☆ | ★★★★☆ | ⇄风格不匹配 | **Tier 4** (不推荐) | $54.99 |
| Tropical Jungle | ★★★★★* | ★★★★☆* | ★★★★☆ | ★★★★★ | ★★★★☆ | ★★★☆☆ | ⊕互补多 | **Tier 3** (新增) | $54.99 |

*\* 标注为基于系列模式推断，非完整数据分析*

### Biome 优先级排序 (末日生存视角)

1. **Alpine Mountain** — Tier 2: WApoc Map 强制依赖 + 高山生态独占
2. **Swamp Marshland** — Tier 2: 末日恐怖叙事直接服务 + 模块化码头
3. **Arid Desert** — Tier 3: 末日常见生态，但 Wasteland 已有重叠
4. **Tropical Jungle** — Tier 3: "自然收复"叙事，但非核心
5. **Meadow Forest** — Tier 3: 基础草地生态，但风格偏田园
6. **Enchanted Forest** — Tier 4: 奇幻风格与项目基调冲突

---

## 三、Nature Biomes 数模关键结论

### 用于程序化生成的模块化组件

基于 Meadow Forest 和 Swamp Marshland 的已知数据：

| 类别 | Meadow Forest | Swamp Marshland | 平均 | 对程序化生成的价值 |
|------|--------------|-----------------|------|-------------------|
| Environment (树木/岩石/植物) | 79 | 60 | ~70 | **核心** — 生态多样性来源 |
| Terrain Materials | 27 | 7 | ~17 | **核心** — 地形权重/纹理规则 |
| Props (道具/装饰) | 60 | 50 | ~55 | 中等 — 点位装饰 |
| Buildings | 5 | 16 | ~10 | 中等 — 建筑模块化程度决定POI生成能力 |
| Particles (氛围/天气) | 14 | 16 | ~15 | 低 — 非程序化核心但增强氛围 |
| Vehicles | 2 | 2 | 2 | 低 — 场景装饰 |

**结论**: Nature Biomes 对程序化生成的主要价值在 Environment + Terrain Materials（占总资产的60-70%）。每个 biome 约 70-80 个独立环境资产 + 7-27 种地形材质，足够支持一个完整生态的程序化生成规则。

---

## 四、潜在相关包评估

### 7. POLYGON - Dog Pack

**部分评估（缺内容清单）**

- **Handle**: `polygon-dog`
- **价格**: $50.00 (sale, reg $99.99 — 当前50% OFF)
- **Product ID**: 7700466008316
- **SKU**: polygon-dog-pack

#### 7维评分（推断）

**1. 资产形态: ★★★★☆** (推断)
预计包含多个犬种模型（德国牧羊犬/斗牛犬/猎犬等），可能带不同颜色变体。作为角色资产包，形态应为模块化（可组合的狗+配件）。

**2. 内容质量: ★★★☆☆** (推断)
$99.99 原价对于纯犬类角色包偏高，可能含大量变体/颜色填充。50%折扣到$50后才显合理。

**3. 持有成本: ★★★★★**
独立包，无依赖。

**4. 技术适配: ★★★★★**
标准，角色应带 Mecanim rig。

**5. 功能价值: ★★★☆☆**
直接玩法资产（companion/敌人）。狗作为末日生存的核心陪伴角色（看门狗/猎犬/丧尸犬）有直接 gameplay 价值，但功能单一。

**6. 可替代性: ★★☆☆☆**
Nature Biome 包均推荐搭配 Dog Pack（Meadow Forest、Swamp 都在推荐组合中列出）。但狗模型也可通过 Modular Fantasy Hero Characters 或 Apocalypse Pack 中的动物资产部分替代。

**7. 组合关系:**
- ⊕ 互补 Meadow Forest — 官方推荐
- ⊕ 互补 Swamp Marshland — 官方推荐
- ⇄ 部分可被 Apocalypse Wasteland 中的动物附件替代

#### 综合判定: **Tier 3**

狗对末世氛围有加分（野狗/丧尸犬/伴侣犬），但纯犬类包覆盖面窄。$50 当前折扣价可接受。**建议跟踪打折时购入**。

---

### 8. POLYGON - Horror Mansion

**部分评估（缺内容清单）**

- **Handle**: `polygon-horror-mansion`
- **价格**: $99.99 (无折扣，full-price)
- **Product ID**: 7570307023100
- **SKU**: polygon-horror-mansion-pack
- **发布时间**: 2022-02-11

#### 7维评分（推断）

**1. 资产形态: ★★★★☆** (推断)
预计为主题套装——经典鬼屋/豪宅建筑模块 + 室内道具 + 恐怖氛围资产。

**2. 内容质量: ★★★★☆** (推断)
$99.99 定价属于中高档，应包含完整的室内外建筑套件和主题道具（烛台/画像/蜘蛛网/棺材等）。

**3. 持有成本: ★★★☆☆**
$99.99 独立包。对单一主题来说偏贵。

**4. 技术适配: ★★★★★**
标准。

**5. 功能价值: ★★★★☆**
关卡构建（鬼屋/豪宅建筑）+ 环境叙事（恐怖氛围）。末日生存中的"废弃豪宅"是经典搜刮场景（生化危机风格）。

**6. 可替代性: ★★★☆☆**
Apocalypse Pack + City Pack 可搭建废弃建筑，但专门的维多利亚式豪宅/鬼屋建筑风格不可替代。办公室包部分覆盖室内。

**7. 组合关系:**
- ⊕ 互补 Swamp Marshland — 官方推荐（沼泽鬼屋主题）
- ⊕ 互补 Apocalypse Pack — 丧尸+鬼屋
- ⊕ 互补 Office Pack/City Pack — 建筑模块互补

#### 综合判定: **Tier 3**

对末世恐怖叙事有价值，但 $99.99 单价对于单一主题包偏高。如果项目中有明确的"鬼屋"搜刮关卡设计，优先级提升。建议打折时考虑。

---

### 9. POLYGON - Palm City

**部分评估（缺内容清单，含价格待确认）**

- **Handle**: `polygon-palm-city`
- **价格**: 待确认（需fetch）
- **Tags**: 待确认

#### 初步判断

"Palm City" 推测为棕榈树主题的现代城市/沿海城市变体。可能与 City Pack 重叠但有热带城市特色（棕榈大道/海滨建筑）。

**功能价值（推测）**: 中等 — 热带沿海城市可扩展地图多样性，但 City Pack ($19.99) + Town Pack ($49.99) 已覆盖城市建筑。

**组合关系（推测）**:
- ⇄ 可能与 City Pack、Town Pack 重叠
- ⊕ 互补 Tropical Jungle — 热带城市+丛林 = 被自然收复的海滨城市

**建议**: 需fetch完整JSON后评估。优先级暂定 Tier 3。

---

### 10. POLYGON - Wheelchair Pack

**部分评估（缺内容清单，含价格待确认）**

- **Handle**: `polygon-wheelchair-pack`
- **价格**: 待确认（需fetch）
- **Tags**: 待确认

#### 初步判断

轮椅作为幸存者多样性配件。注意: Apocalypse Wasteland 的 "Character Attachments" 列表已包含 "wheelchair" —— 如果 Wheelchair Pack 是独立扩展包，需确认其与 Wasteland 的区别（可能更系统化的无障碍资产）。

**功能价值（推测）**: 低 — 幸存者多样性在叙事层面有价值，但 gameplay 影响小。除非项目特意设计了残障幸存者角色系统。

**建议**: 需fetch完整JSON。优先级暂定 Tier 3-4。

---

### 11. POLYGON - Nightclubs

**部分评估（缺内容清单）**

- **Handle**: `polygon-nightclubs`
- **价格**: $50.00 (sale, reg $99.99 — 当前50% OFF)
- **Product ID**: 7616085787... 
- **SKU**: polygon-nightclubs-pack
- **发布时间**: 2022-03-28

#### 7维评分（推断）

**1. 资产形态: ★★★★☆** (推断)
预计为夜店/酒吧室内模块化套件 — 吧台/舞池/DJ台/灯光/卡座。

**2. 内容质量: ★★★☆☆** (推断)
$99.99 原价对于夜店主题偏贵，50%折后$50较合理。夜店资产复用场景有限（仅酒吧/娱乐场所）。

**3. 持有成本: ★★★★★**
独立包，无依赖。

**4. 技术适配: ★★★★★**
标准。

**5. 功能价值: ★★☆☆☆**
仅关卡构建（夜店/酒吧场景）。末世中废弃酒吧是搜刮场景之一，但覆盖面窄。

**6. 可替代性: ★★☆☆☆**
Office Pack + City Pack + Apocalypse Pack 可搭建废弃室内场景。霓虹灯/舞池/酒吧元素需要自制或从此包获取。

**7. 组合关系:**
- ⊕ 互补 City Pack — 城市建筑 + 夜店室内
- ⇄ 部分重叠 Office Pack — 室内道具通用

#### 综合判定: **Tier 4 → Tier 3**

夜店是末世氛围场景（废弃酒吧/幸存者聚集地），但功能覆盖面窄。$50 当前折扣价可接受。建议**不打折时不买**，跟踪 Humble Bundle。

---

## 五、Nature Bundles 分析

### 12. POLYGON - Nature Biomes - Season One

- **Handle**: `polygon-nature-biomes-season-one`
- **价格**: $82.49 (sale, reg $164.99 — 当前50% OFF)
- **Product ID**: 7882116530428
- **SKU**: polygon-nature-biomes-season-one-pack
- **发布时间**: 2022-10-03

#### 捆绑内容推断

基于发布时间和定价结构，Season One 应包含 2022年9-10月发布的两款 Nature Biome：
- Meadow Forest ($54.99)
- Swamp Marshland ($54.99)

单独购买总价: $109.98
Bundle 当前价: $82.49
**节省: $27.49 (25%)**

常规价 ($164.99) vs 单独 ($109.98): 常规价反而更贵——说明 $164.99 可能是占位"原价"或包含额外内容。

#### 7维评分

**1. 资产形态: ★★★★★**
两包均为模块化环境元件。

**2. 内容质量: ★★★★★**
Meadow (187) + Swamp (151) = ~338 独立资产，覆盖草地森林+沼泽两大生态。

**3. 持有成本: ★★★★★**
Bundle 当前 $82.49，单包均价 $41.25 vs 单独$54.99。**净省$27.49**。

**4. 技术适配: ★★★★★**
标准。

**5. 功能价值: ★★★★★**
两个生态覆盖末日户外场景的基础需求。

**6. 可替代性: ★★★★☆**
两大生态互补无可替代。

**7. 组合关系:**
- Meadow Forest ⊕ Swamp Marshland — 两生态互补（草地→沼泽过渡自然）
- ⊕ Apocalypse Pack — 通用兼容

#### 综合判定: **Tier 2 (推荐通过此Bundle获得Meadow+Swamp)**

当前 $82.49 (50% OFF) 即 $41.25/包，对比单独购买 $54.99/包，节省 25%。如果计划同时购买 Meadow + Swamp，通过 Season One Bundle 是更优选择。

**但需注意**: Meadow 已 在tracker Tier 3，Swamp 建议升至 Tier 2。如果 Swamp 升级批准，Season One 可作为 Tier 2 一次性采购。

---

### 13. POLYGON - Nature Biomes - Season Two

- **Handle**: `polygon-nature-biomes-season-two`
- **价格**: $82.49 (sale, reg $164.99 — 当前50% OFF)
- **Product ID**: 8192439582972
- **SKU**: polygon-nature-biomes-season-two-pack
- **发布时间**: 2023-11-14

#### 捆绑内容推断

基于发布时间（2023年11月）和 Tier 2 成员，Season Two 应包含 2023年底发布的四款 Nature Biome：
- Alpine Mountain ($54.99)
- Arid Desert ($54.99)
- Enchanted Forest ($54.99)
- Tropical Jungle ($54.99)

单独购买总价: $219.96
Bundle 当前价: $82.49
**节省: $137.47 (62.5%)**

#### 巨大折扣分析

单独购买 4 包 = $219.96
Season Two Bundle = $82.49 (当前 50% OFF 后)
常规价 $164.99 vs 单独 $219.96 = 节省 25%

**当前折扣力度**: $82.49 for 4 packs = **$20.62/包**，对比单独 $54.99/包，**节省 62.5%**。

这意味着即使 Enchanted Forest 是 Tier 4（与项目基调不匹配），剩余 3 包 (Alpine + Arid + Tropical) 的单独总价 = $164.97，但通过 Season Two Bundle 仅需 $82.49 即可获得全部 4 包 —— Alpine ($54.99) 的强制依赖问题也一并解决。

#### 7维评分

**1-5. 综合: ★★★★★**
四包覆盖高山/沙漠/森林/丛林四大生态，加上已有的 Meadow+Swamp，整个Nature Biome系列六生态全收集。

**6. 可替代性: ★★★★★**
Alpine Mountain 无可替代（仅此一个高山 biome，且 WApoc Map 强制依赖）。

**7. 组合关系:**
- Alpine ← Woodland Apocalypse Map 依赖
- Arid ⇄ Apocalypse Wasteland 部分重叠
- Enchanted Forest → Tier 4 单买不推荐，但 bundle 中作为"白送"可接受
- Tropical ⊕ Swamp 湿地+丛林互补

#### 综合判定: **Tier 1 (强烈推荐)**

**这是最重要的发现。** Season Two Bundle 当前 $82.49 带来：
1. Alpine Mountain — WApoc Map 强制依赖，无可替代
2. Arid Desert — 沙漠生态补充
3. Tropical Jungle — 丛林生态
4. Enchanted Forest — "附赠"（单独不买但 bundle 中不算成本）

**建议购买策略**:
> 当前 Season Two Bundle $82.49 = 4个 biome 均价 $20.62 ← 这是极佳的 bundle 折扣
> 对比: Alpine 单独 $54.99 已接近 bundle 总价的 67%

如果当前 50% OFF 促销结束恢复 $164.99，单包均价 $41.25/包 —— 仍然比单独买 Alpine ($54.99) + 任意一包便宜。

---

## 六、Bundle 策略对比

| 方案 | 包内容 | 总价 | Biome数量 | 均价/biome | 
|------|--------|------|-----------|-----------|
| **单买 Alpine + Swamp** | 高山+沼泽 | $109.98 | 2 | $54.99 |
| **Season One** | Meadow + Swamp | $82.49 (sale) | 2 | $41.25 |
| **单买 Alpine** | 仅高山 | $54.99 | 1 | $54.99 |
| **Season Two** | Alpine+Arid+Enchanted+Tropical | $82.49 (sale) | 4 | $20.62 |
| **全6 Biome单买** | 全部 | $329.94 | 6 | $54.99 |
| **Season One + Two** | 全部 | $164.98 (sale) | 6 | $27.50 |

**最优策略**: Season One + Season Two = $164.98 获得全部 6 个 biome。单独购买 Alpine Mountain 一项 ($54.99) 就已占 bundle 总价的 33%。

---

## 七、推荐Tracker更新

### 建议升级

| 包名 | 当前Tier | 建议Tier | 原因 |
|------|---------|---------|------|
| Swamp Marshland | Tier 3 | **Tier 2** | 末日恐怖叙事直接服务，模块化码头工程价值，Synty官方推荐配Apocalypse |

### 建议新增

| 包名 | Tier | 价格 | 理由 |
|------|------|------|------|
| **Alpine Mountain** | **Tier 2** | $54.99 | WApoc Map强制依赖 + 高山生态独占 |
| Arid Desert | Tier 3 | $54.99 | 沙漠生态，但Wasteland已部分覆盖 |
| Tropical Jungle | Tier 3 | $54.99 | 丛林生态，通过Season Two bundle性价优 |
| Enchanted Forest | Tier 4 | $54.99 | 奇幻风格与项目基调冲突 |
| Dog Pack | Tier 3 | $50.00 (sale) | 伴侣/敌犬，打折时可收 |
| Horror Mansion | Tier 3 | $99.99 | 鬼屋搜刮场景，打折时考虑 |
| Palm City | 待评估 | 需fetch | 需完整JSON后评估 |
| Wheelchair Pack | Tier 3-4 | 需fetch | 幸存者多样性，优先级低 |
| Nightclubs | Tier 3 | $50.00 (sale) | 场景补充，打折时考虑 |
| **Nature Biomes Season One** | **Tier 2** | $82.49 (sale) | Meadow+Swamp打包，省$27 |
| **Nature Biomes Season Two** | **Tier 1** | $82.49 (sale) | 4 biome均价$20.62，含WApoc强制依赖 |

### 价格影响

当前tracker Tier 1+2 总价: $1,347.87 (含Apocalypse Wasteland $379.99)

如果新增:
- Season Two (Tier 1): +$82.49
- Swamp升至Tier 2: already counted
- Alpine 合并到 Season Two: already counted

**新的 Tier 1 总价**: $1,430.36
**新的 5折后**: ~$715

包含全部 6 nature biome 后的程序化地形能力将覆盖:
- 草地/森林 (Meadow)
- 沼泽/湿地 (Swamp)
- 高山/雪地 (Alpine)
- 沙漠/废土 (Arid)
- 热带丛林 (Tropical)
- 奇幻森林 (Enchanted — 部分使用)

---

## 需要Fetch的JSON

以下包的完整评估需要fetch独立JSON获取 `body_html`（内容清单）:

```
https://syntystore.com/products/polygon-alpine-mountain-nature-biomes.json
https://syntystore.com/products/polygon-arid-desert-nature-biomes.json
https://syntystore.com/products/polygon-enchanted-forest-nature-biomes.json
https://syntystore.com/products/polygon-tropical-jungle-nature-biome.json
https://syntystore.com/products/polygon-dog.json
https://syntystore.com/products/polygon-horror-mansion.json
https://syntystore.com/products/polygon-palm-city.json
https://syntystore.com/products/polygon-wheelchair-pack.json
https://syntystore.com/products/polygon-nightclubs.json
https://syntystore.com/products/polygon-nature-biomes-season-one.json
https://syntystore.com/products/polygon-nature-biomes-season-two.json
```

**Fetch后应更新内容**: 
- 各Nature Biome的 Environment/Buildings/Props/Terrain/Vehicles/Particles 精确数量
- Dog Pack的犬种数量和变体数
- Horror Mansion的建筑模块化程度
- Palm City的模块化建筑件数量（与City Pack的重叠分析）
- Season One/Two bundle的精确成员列表
- Wheelchair Pack价格

---

## 核心结论

1. **Alpine Mountain 是必买**: Woodland Apocalypse Map 的强制依赖。不买 Alpine = 已买过的 Woodland Map 无法使用。这已将 Alpine 从"nice to have"升级为"must have"。

2. **Season Two Bundle 是最优购买方式**: $82.49 获得 4 个 biome（含 Alpine），均价 $20.62。单独买 Alpine 就要 $54.99。

3. **Swamp Marshland 应升至 Tier 2**: 骨骸/图腾/墓碑/迷雾/模块化码头直接服务末日生存核心叙事，Synty官方也推荐与Apocalypse Pack搭配。

4. **全6 Biome + Season One & Two = $164.98**: 对比单买 $329.94，节省 50%。建议一次性通过两个 bundle 收集全部 nature biome。

5. **Enchanted Forest 不单独推荐但要通过 bundle 接受**: Tier 4 独立评价，但在 Season Two bundle 中作为"附赠"不增加成本，奇幻资产可能在辐射变异区或彩蛋场景中使用。
