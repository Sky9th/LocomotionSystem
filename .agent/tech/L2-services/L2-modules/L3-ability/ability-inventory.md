# Ability Inventory — 技能全量树

> `L3_Ability/` · 设计文档 · 2026-06-07
>
> 两个 Agent 交叉设计+验证后合成的最终技能库存。覆盖全部 13 个 `Skill.*` 标签分类，共约 154 技能。

---

## 目录

- [设计原则](#设计原则)
- [消耗与数值基准](#消耗与数值基准)
- [一、Skill.Combat.Melee — 近战](#一skillcombatmelee--近战)
- [二、Skill.Combat.Ranged — 远程](#二skillcombatranged--远程)
- [三、Skill.Combat.Throwable — 投掷物](#三skillcombatthrowable--投掷物)
- [四、Skill.Combat.Defensive — 防御](#四skillcombatdefensive--防御)
- [五、Skill.Combat.Stealth — 潜行](#五skillcombatstealth--潜行)
- [六、Skill.Utility.Medical — 医疗](#六skillutilitymedical--医疗)
- [七、Skill.Utility.Survival — 生存](#七skillutilitysurvival--生存)
- [八、Skill.Utility.Craft — 工艺](#八skillutilitycraft--工艺)
- [九、Skill.Utility.Trade — 交易](#九skillutilitytrade--交易)
- [十、Skill.Utility.Lockpicking — 撬锁](#十skillutilitylockpicking--撬锁)
- [十一、Skill.Trap — 陷阱](#十一skilltrap--陷阱)
- [十二、伤害/效果/形状/被动 全量覆盖矩阵](#十二伤害效果形状被动-全量覆盖矩阵)
- [十三、跨系统缺口与 Phase 2+ 预留](#十三跨系统缺口与-phase-2-预留)
- [十四、闭环测试最小技能集](#十四闭环测试最小技能集)

---

## 设计原则

| 原则 | 说明 |
|------|------|
| **技能绑定武器** | 捡到武器=自动学会其技能组（近战/远程），切换武器=技能栏切换 |
| **武功+热武器双线** | 近战消耗体力(可再生)，热武器消耗弹药(仅搜刮)+噪音(吸引丧尸) |
| **操作差异化** | 每种武器/玩法手感完全不同 |
| **实用格斗术命名** | 末世生存格斗，不搞奇幻武侠。命名规则：`[动作描述] - [武器分类]` |
| **技能≠物品** | Medical 是急救能力（需要物品催化剂），Craft 是制作能力，Trap 是部署能力 |
| **主动 Q/E/R/F + 被动** | 核心战斗武器 4 主动+1 被动；非战斗子系统按需分配键位 |
| **每技能映射 SO** | 主动→AbilityDefSO，被动→PassiveAbilitySO，消耗→CostEffectSO，标签→GameplayTag |

---

## 消耗与数值基准

### 伤害倍率

| 定位 | 倍率(vs 普攻) | 典型用途 |
|------|--------------|---------|
| 极低 | 0.4–0.6× | 附加效果为主要价值（掩护射击、压制） |
| 低 | 0.7–0.9× | 控制型技能 |
| 中 | 1.0–1.2× | 常规输出 |
| 高 | 1.5–2.0× | 核心输出技能 |
| 极高 | 2.5–4.0× | 终结技/大招/狙击 |

### 冷却分级

| 等级 | 时间 | 典型技能 |
|------|------|---------|
| 极短 CD | 0.5–2s | 普攻替代、翻滚、切换技 |
| 短 CD | 3–5s | 常规输出技能 |
| 中 CD | 6–10s | 控制/爆发技能 |
| 长 CD | 12–20s | 大招/清场/斩杀 |
| 超长 CD | 25–60s | 救场/终极技 |

### 噪音分级

| 等级 | 描述 | 丧尸反应半径 | 典型来源 |
|------|------|-------------|---------|
| 1 | 极低 | < 3m | 潜行、屏息 |
| 2 | 低 | ~5m | 手枪(消音)、潜行移动 |
| 3 | 中 | ~15m | 近战挥空、跑步 |
| 4 | 高 | ~30m | 近战命中、撞击、烟雾弹 |
| 5 | 极高 | ~60m | 手枪射击、重击、燃烧瓶 |
| 6 | 震耳 | ~120m | 步枪、霰弹枪、爆炸、警报 |

### 消耗类型

| 消耗类型 | 来源标签 | 可恢复性 | 适用范围 |
|----------|---------|---------|---------|
| 体力 Stamina | Stat.Vital.Stamina | 自动恢复 + 食物 | 近战/翻滚/防御 |
| 弹药 Ammo | Stat.Pool.Ammo | 仅搜刮/复装 | 热武器 |
| 投掷物物品 | 物品栏 | 仅搜刮/制作 | 投掷技能 |
| 医疗物资 | 物品栏 | 仅搜刮/制作 | 医疗技能催化剂 |
| 材料 Material | 物品栏 | 仅搜刮/采集 | 工艺/陷阱 |
| 燃料 Fuel | Stat.Pool.Fuel | 仅搜刮 | 热切割/火焰类工具 |

---

## 一、Skill.Combat.Melee — 近战

> **三层架构**：武器决定伤害形态 → 套路决定连击技法 → 基本功为通用生存动作。
>
> 核心设计原则：
> - **武器**：决定轻击/重击/格挡的搜索形状、伤害类型、基础倍率和距离。捡到即拥有。
> - **套路**：现实武学流派（拳击、八极拳、泰拳、剑道等）。可装备，决定连击链长度、加成曲线、基础技强化版、终结技。
> - **基本功**：所有角色天生拥有（翻滚闪避等）。

---

### 1.0 武器基础技（所有角色捡到武器即拥有）

> 这些是"不会武功的普通人"抡武器的水平。装备武学套路后会被套路的强化基础技覆盖。

#### 刀 (Blade) — Slash / Pierce

| 技能名 | 形状 | 伤害 | 效果 | 消耗 | 噪音 |
|--------|------|------|------|------|------|
| 轻击 Light Cut | Cone 60° 2m | 中 1.0× Slash | — | 体力8 | 3 |
| 重击 Heavy Chop | Ray 2.5m | 高 1.5× Slash | 前摇较长 | 体力18 | 4 |
| 格挡 Block | None(正面) | — | 正面格挡率+50%, 格挡成功消耗体力=受伤×0.4 | 体力(格挡时) | — |

#### 棍 (Staff) — Blunt / 控制

| 技能名 | 形状 | 伤害 | 效果 | 消耗 | 噪音 |
|--------|------|------|------|------|------|
| 轻击 Light Swing | Cone 90° 2.5m | 低 0.8× Blunt | — | 体力10 | 3 |
| 重击 Heavy Smash | Ray 3m | 中 1.3× Blunt | Impact(小硬直) | 体力20 | 4 |
| 格挡 Block | None(正面) | — | 正面格挡率+55%, 格挡成功消耗体力=受伤×0.35 | 体力(格挡时) | — |

#### 斧 (Axe) — Slash / 重击

| 技能名 | 形状 | 伤害 | 效果 | 消耗 | 噪音 |
|--------|------|------|------|------|------|
| 轻击 Light Hack | Cone 40° 2m | 高 1.2× Slash | 窄角高伤 | 体力12 | 4 |
| 重击 Heavy Cleave | Ray 3m | 极高 1.8× Slash | 长前摇(0.6s), 不可取消 | 体力25 | 5 |
| 格挡 Block | None(正面) | — | 正面格挡率+40%, 格挡成功消耗体力=受伤×0.5(斧重，格挡更费体力) | 体力(格挡时) | — |

> **A 测近战武器排序**：刀(快速/破防) < 棍(范围/控制) < 斧(慢速/重击)。未来可扩展：盾(Defensive)、匕首(Stealth)、长矛(Ray特长)等。

---

### 1.1 武学套路 — Martial Arts Routine

> 可装备的武学技能组。搜刮武术手册/秘籍获得，学习后永久掌握。在武器界面选择一个套路+一把武器 = 确定当前技能栏。
>
> 套路决定你的**连击链**。同一套"八极拳"，装在刀上和装在棍上：连击技相同，但伤害类型和距离由武器决定。

#### 稀有度体系

| 稀有度 | 连击上限 | 连击加成曲线 | 强化基础技 | 终结技数量 | 终结技效果 | 搜刮来源 |
|--------|---------|-------------|-----------|-----------|-----------|---------|
| 残卷 Fragment | 3连 | 每段+5% | 无(用武器基础技) | 0 | — | 民居书架、丧尸身上 |
| 完整 Complete | 5连 | 每段+8%, 第5段+20% | 轻击/重击升级 | 1 | 中伤害+控制 | 书店、道馆、军营 |
| 精妙 Refined | 7连 | 每段+10%, 第5段+20%, 第7段+35% | 轻击/重击/格挡升级 | 2 | 高伤害+控制+破防 | 特种部队基地、大师遗物 |
| 绝学 Masterwork | 9连 | 每段+12%, 第5段+25%, 第7段+40%, 第9段+60% | 轻击/重击/格挡大幅升级 | 3 | 极高伤害+控制+破防+斩杀 | 隐藏地点、极限难度尸潮奖励 |

#### 连击链规则

- 连击通过轻击(L)和重击(H)的组合触发。必须在**时间窗口**内连续命中（未命中或超时则链断）。
- 终结技仅在连击链最后一段可用，命中或窗口结束则链重置。
- 切换武器/被击倒/翻滚 → 链断。
- 连击加成对链中每一段生效，加成乘算最终伤害。

**连击链示例（完整·八极拳）**：
```
L → L → H → L → H(终结)
第1段 +8% → 第2段 +16% → 第3段 +24% → 第4段 +32% → 第5段(终结) +20%额外 = +52%
```

---

### 1.2 套路目录

#### 打击系 Striking — 进攻向，连击节奏快

**1. 拳击 Boxing** `西方/打击`
> 脚步灵活，组合拳密集。轻击速度极快，重击=上勾拳/摆拳。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | L-L-H | 无 | 轻击速度+10% |
| 完整 | L-L-H-L-H | **爆肝拳** (Ray, Blunt, 1.8×, Impact硬直) | 轻击速度+15%, 闪避后下一击+20% |
| 精妙 | L-L-H-L-L-H-H | **爆肝拳** + **连续刺拳** (Cone, 6连轻击, 每拳0.4×, 总伤2.4×) | 轻击可移动中释放, 连击窗口+0.3s |
| 绝学 | L-L-H-L-L-L-H-L-H | 精妙终结+ **KO右直拳** (Ray, 3.0×, Execute HP<20%, 必定暴击) | 全基础+连击段数越高破防率越高 |

**2. 泰拳 Muay Thai** `泰国/打击`
> 肘膝终结技，近身毁灭性。重击=鞭腿/膝撞，终结技=肘击。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | L-H-H | 无 | 重击伤害+10% |
| 完整 | H-L-H-L-H | **肘击** (Ray 1m, Slash 2.5×, Impact硬直1.5s) | 近身(2m内)伤害+15% |
| 精妙 | L-H-L-H-H-L-H | **肘击** + **飞膝** (Ray 3m, Blunt 2.8×, Knockback+追击位移) | 终结技命中回复体力10% |
| 绝学 | L-H-L-L-H-H-L-H-H | 精妙终结+ **泰式箍颈连膝** (Ray 1m, 3连膝撞, 每击1.2×+眩晕, 总3.6×) | 霸体状态下终结技不可被打断 |

**3. 八极拳 Baji Quan** `中国/打击`
> "文有太极安天下，武有八极定乾坤"。寸劲爆发，贴身短打，撞靠震开包围。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | H-H-L | 无 | 重击破防率+15% |
| 完整 | H-L-H-L-H | **贴山靠** (Ray 2m, Blunt 2.2×, Knockback 4m+路径丧尸受0.5×撞击) | 格挡后轻击伤害+25% |
| 精妙 | H-L-L-H-H-L-H | **贴山靠** + **崩拳** (Ray 1.5m, Blunt 2.5×, 必然破防+硬直2s) | 被包围时(3m内≥3敌)伤害+20% |
| 绝学 | H-L-H-L-L-H-H-L-H | 精妙终结+ **猛虎硬爬山** (Cone 120°, Slash 4.0×, 击杀重置连击) | 每击杀+5%伤害, 可堆叠, 持续至战斗结束 |

**4. 咏春拳 Wing Chun** `中国/打击`
> 近距离连环快打，中线理论。轻击=日字冲拳(Chain Punch)，极快但单发低伤。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | L-L-L | 无 | 轻击速度+15% |
| 完整 | L-L-L-L-H | **寸劲** (Ray 1m, Blunt 2.0×, 无视格挡) | 轻击可边打边移动, 第4段起轻击伤害+15% |
| 精妙 | L-L-L-H-L-L-H | **寸劲** + **连环冲拳** (Ray 1.5m, 5连轻击, 每拳0.35×, 总1.75×, 最后一拳必硬直) | 连击窗口+0.5s, 咏春独有:可在格挡姿态下释放轻击 |
| 绝学 | L-L-L-H-L-L-L-H-L | 精妙终结+ **标指** (Ray 1m, Pierce 3.5×, Execute HP<30%) | 攻击速度+25%, 体力消耗-20% |

#### 控制系 Control — 防守反击 / 以静制动

**5. 太极拳 Tai Chi** `中国/控制`
> 以柔克刚。格挡性能最优，连击加成侧重防守反击，终结技为借力打力的发劲。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | L-H-L | 无 | 格挡体力消耗-25% |
| 完整 | L-L-H-L-H | **揽雀尾·发劲** (Cone 120° 3m, Blunt 1.8×, 被格挡后反击=伤害×1.5, 3m击退) | 格挡成功后3s内连击伤害+30% |
| 精妙 | L-H-L-L-H-L-H | 完整终结+ **云手·化劲** (Circle 3m, 自身, Buff: 5s内所有正面攻击自动格挡, 被格挡伤害40%返还攻击者) | 体力恢复速度+20%, 静止站立1s后体力恢复翻倍 |
| 绝学 | L-L-H-L-H-L-L-H-L | 精妙终结+ **如封似闭** (Cone 180°, 4m Knockback, 伤害1.0×, 范围内丧尸全部倒地3s, 对Boss减速80%) | 体力低于30%时格挡消耗再-30%, 太极拳体力消耗整体-20% |

**6. 柔道 Judo** `日本/控制`
> 利用对手动量投掷。对敌单体最强控制，终结技=投技+地面压制。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | L-H-L | 无 | 被击退距离-30% |
| 完整 | L-L-H-L-H | **背负投** (Ray 1.5m, 单体, Blunt 2.0×, 目标被投掷至身后3m+倒地3s, 投掷路径撞飞其他丧尸0.8×) | 自身重心稳定(免疫击退效果)+20% |
| 精妙 | L-H-L-L-H-L-H | **背负投** + **袈裟固** (Ray 1m, 单体, 地面压制3s, 期间目标无法行动, 每秒0.5×伤害, 其他敌人可攻击你) | 投掷距离+50%, 投掷路径伤害+30% |
| 绝学 | — | 精妙终结+ 全投技可对Boss和精英使用(基础版对Boss无效) | 每投掷击杀一个敌人，CD重置(每10s最多1次) |

**7. 菲律宾魔杖 Eskrima / Kali** `菲律宾/器械`
> 棍/短刀格斗术，攻防一体。连续轻击=棍花(X-pattern)，重击=劈击，终结技=缴械/要害刺击。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | L-L-H | 无 | 棍轻击范围+10% |
| 完整 | L-H-L-H-L | **太阳穴打击** (Ray 2m, Blunt 2.3×, Impact眩晕3s, 仅对单体) | 连击窗口+0.2s, 易打出5连 |
| 精妙 | L-L-H-L-H-L-H | 完整终结+ **缴械斩** (Ray 1.5m, Slash 1.5×, Debuff: 人类敌人缴械3s, 丧尸攻击力-30% 8s) | 轻击速度+20%, 终结技后轻击免费(不耗体力)1次 |
| 绝学 | L-L-H-L-L-H-L-H-L | 精妙终结+ **要害连环** (Ray 2m, Pierce, 5连刺 每击0.8×, 总4.0×, 最后一击=Execute HP<25%) | 持棍时获得"自动格挡反击": 格挡成功自动轻击回击(0.5×) |

#### 器械系 Weapon Arts — 武器专精

**8. 剑道 Kendo** `日本/器械`
> 精准劈斩，一击必杀的追求。仅刀可用。重击=唐竹割(竖劈), 终结技=突刺/小手切。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | H-L-H | 无 | 重击暴击+15% |
| 完整 | H-L-H-L-H | **突刺 Tsuki** (Ray 3m, Pierce 2.8×, 必定暴击) | 重击有明显前摇但伤害+20% |
| 精妙 | L-H-H-L-H-L-H | **突刺** + **小手切** (Ray 1.5m, Slash 2.0×, 命中后目标攻击力-30% 8s) | 对单体目标每段额外+5%加成 |
| 绝学 | H-L-H-L-H-H-L-H-L | 精妙终结+ **一之太刀** (Ray 4m, Slash 5.0×, 单发, 3s蓄力, 不可取消, Execute HP<35%) | 刀系全部伤害+15%, 斩杀后5s内移速翻倍 |

**9. 苗刀术 Miao Dao** `中国/器械`
> 长刀/巨剑技法，大开大合。轻击=横扫，重击=下劈。仅刀可用。终结技=回身斩/力劈华山。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | H-L-H | 无 | 大开大合: 攻击范围+15% |
| 完整 | H-L-H-L-H | **回身斩** (Circle 3m, Slash 2.5×, 360°转身横扫, 对所有周围敌人) | 重击对建筑+30%伤害 |
| 精妙 | H-L-L-H-L-H-H | **回身斩** + **力劈华山** (Ray 3m, Slash 3.5×, 单体, 对满血目标伤害×1.5) | 击杀后攻击力+15%, 持续6s |
| 绝学 | — | 精妙终结+ **破阵斩** (Cone 150° 3m, Slash 4.5×, 前方超广角, 范围内敌人全部击退+硬直) | 每击杀重置连击, 连杀3人后下一击免体力 |

#### 综合系 Mixed / 军用

**10. 以色列格斗术 Krav Maga** `以色列/军用`
> 实战生存格斗。不讲美观，只讲效率。轻击=快手打击(喉/眼/裆), 重击=膝肘组合。唯一能同时使用近战+手枪的套路。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | H-L-H | 无 | 被击倒后起身速度+50% |
| 完整 | H-L-H-L-H | **缴械反击** (Ray 1.5m, 人类敌人缴械+击退, 丧尸=Blunt 2.0×+眩晕2s) | 被多个敌人围攻时伤害+12% |
| 精妙 | L-H-H-L-H-L-H | 完整终结+ **要害打击** (Ray 1m, Pierce, 对丧尸=脖颈重击 3.0× 必暴, 对人类=裆部打击 2.0×+眩晕5s) | 近战+手枪可混用(切换无惩罚), 近战击杀后手枪下一击+25% |
| 绝学 | H-L-H-L-H-H-L-H-L | 精妙终结+ **绝境反杀** (Cone 60° 2m, Blunt 3.5×, 自身HP越低伤害越高: HP<50%=+25%, <25%=+60%) | 濒死(HP<20%)时伤害+30%, 体力消耗-30%, 不可被处决 |

**11. 散打 Sanda / Sanshou** `中国/军用`
> 中国军警格斗术。踢打摔拿结合。终结技=鞭腿/接腿摔。

| 稀有度 | 连击链 | 终结技 | 被动 |
|--------|--------|--------|------|
| 残卷 | L-H-L | 无 | 移动中伤害不减 |
| 完整 | L-H-L-H-L | **鞭腿** (Cone 90° 3m, Blunt 2.3×, Impact击退+硬直1s) | 轻击+重击混合时每段额外+3%加成 |
| 精妙 | L-L-H-H-L-H-L | **鞭腿** + **接腿摔** (Ray 2m, 单体, Blunt 2.0×, 将目标摔到地面=倒3s+周围1.5m内敌人硬直0.5s) | 闪避后可立即释放重击(无前摇) |
| 绝学 | — | 精妙终结+ **侧踹** (Ray 3m, Blunt 4.0×, 单体, 极长前摇但不可格挡, 命中后目标飞撞墙=二次碰撞伤害2.0×) | 攻击每命中3次获得1层"散打节奏"(+8%攻速, 叠3层, 持续至战斗结束) |

---

### 1.3 套路与武器对照

> 拳法套路可以空手使用(伤害低但不依赖武器)。器械套路需要对应武器类型。

| 套路 | 类型 | 适合武器 | 空手可用 | 风格关键词 |
|------|------|---------|---------|-----------|
| 拳击 Boxing | 打击 | 任意(拳法为主) | ✅ | 快节奏组合拳 |
| 泰拳 Muay Thai | 打击 | 任意(拳法为主) | ✅ | 肘膝终结，近身毁灭 |
| 八极拳 Baji Quan | 打击 | 任意 | ✅ | 寸劲爆发，撞靠突围 |
| 咏春拳 Wing Chun | 打击 | 任意 | ✅ | 连环快打，格挡中出拳 |
| 太极拳 Tai Chi | 控制 | 任意 | ✅ | 以柔克刚，体力管理 |
| 柔道 Judo | 控制 | 任意 | ✅ | 投技控制，单体最强 |
| 菲律宾魔杖 Eskrima | 器械 | 棍/短刀 | ❌ | 攻防一体棍法 |
| 剑道 Kendo | 器械 | 刀 | ❌ | 精准劈斩，一击必杀 |
| 苗刀术 Miao Dao | 器械 | 刀(大型) | ❌ | 大开大合，战场清扫 |
| 以色列格斗术 Krav Maga | 综合 | 任意+手枪 | ✅ | 实战生存，不讲美观 |
| 散打 Sanda | 综合 | 任意 | ✅ | 踢打摔拿，军警格斗 |

> **A 测优先实现**：残卷/完整稀有度 × 3个流派（拳击、八极拳、菲律宾魔杖）= 覆盖打击/爆发/器械三类，验证连击链管道。

---

### 1.4 基本功（所有角色天生拥有，不占套路槽）

| 技能名 | 形状 | 效果 | 消耗 | CD | 噪音 |
|--------|------|------|------|----|------|
| 翻滚闪避 Combat Roll | None(自身) | 0.3s无敌帧, 位移3m, 可取消攻击后摇, 但断连击链 | 体力20 | 2s | 3 |
| 脚踢 Push Kick | Ray 1.5m | Blunt 0.3×, Knockback(中幅, 2m), 不参与连击链, 不消耗武器耐久 | 体力10 | 3s | 2 |

---

## 二、Skill.Combat.Ranged — 远程

> **核心理念**：弹药决定伤害类型和基础伤害值，射击技能=技术/姿态。装了什么子弹就打什么子弹——装鹿弹就是散射，装独头弹就是单发高伤，装燃烧弹才是火焰。
>
> A 测简化：每种武器默认配发标准弹药（手枪=9mm FMJ, 霰弹=12号鹿弹, 步枪=5.56mm FMJ），特殊弹药(独头弹/穿甲弹/燃烧弹)通过搜刮获得并手动装填。
>
> 弹药稀有+噪音吸引丧尸是热武器的核心代价。**所有攻击行为都是技能，走完整 Ability Pipeline。**

### 2.1 手枪 (Pistol) — 灵活 / 快速反应 / 弹药相对充裕

> 废土最可靠的副武器。操作灵活，移动中散布惩罚小，弹药相对好找。

| 技能名 | 形状 | 伤害 | 效果 | 消耗 | CD | 噪音 | 说明 |
|--------|------|------|------|------|----|------|------|
| 普通射击 Normal Fire | Ray 12m | 跟随弹药 | 基础射击。站定散布小，移动散布×1.3 | 弹药1 | — | 5 | 手枪基础攻击，可移动中释放 |
| 快速拔枪 Quick Draw | Ray 10m | 跟随弹药 | 切换至手枪后首发射击：出枪速度+60%, 散布-30% | 弹药1 | 3s | 5 | 武器切换后的奖励技，鼓励手枪作为应急副武器 |
| 双持 Akimbo | Ray 10m | 跟随弹药 | Toggle：双枪姿态。射速×2, 弹药消耗×2, 散布+35%, 换弹时间翻倍 | 无(切换) | 1s | 5 | 短时间内倾泻火力的拼命姿态 |
| 快速换弹 Speed Reload | None(自身) | — | 本次换弹时间-50%，丢弃当前弹匣(残弹损失) | 无 | 8s | — | 紧急状态下牺牲残弹换取速度 |
| 移动射击 Moving Fire | None(自身) | — | Buff：6s内移动中射击散布惩罚减半(维持腰射精度) | 体力10 | 12s | 5 | 手枪核心机动技，风筝丧尸的基础 |
| 手枪熟手 Pistol Adept | 被动 | — | 换弹速度+25%, 携弹上限+20%, 切换至手枪速度+30% | — | — | — | 手枪操作综合提升 |

### 2.2 霰弹枪 (Shotgun) — 近距冲击 / 扇形毁伤 / 极高噪音

> 近距王者。装弹类型决定散布和伤害：鹿弹=扇形散射，独头弹=单发重击。噪音等级6，每开一枪都是全图警报。

| 技能名 | 形状 | 伤害 | 效果 | 消耗 | CD | 噪音 | 说明 |
|--------|------|------|------|------|----|------|------|
| 普通射击 Normal Fire | Cone 50° 5m | 跟随弹药 | 基础射击。3m内伤害最大(1.5×)，5m衰减至1.0× | 弹药1 | — | 6 | 逐发装填，弹仓2发。装独头弹则变为Ray精准射击 |
| 抵近射击 Close Quarters | Cone 40° 4m | 跟随弹药 | 主动收紧射击姿态：散布角-40%, 有效集中射程+2m, 自身移速-30%。再按取消 | 弹药1 | 1.5s | 6 | 牺牲移动换取命中集中度——"等他走近一点" |
| 枪托挥击 Stock Strike | Ray 1.5m | Blunt 中 1.0× | Knockback(中幅, 2m), 打断丧尸攻击前摇 | 体力15 | 3s | 3 | 不耗弹药。弹仓打空时的保命技 |
| 快速装填 Combat Load | None(自身) | — | 一次填入2发弹药(正常装填为逐发填入) | 弹药2 | 5s | — | 减少装填窗口暴露时间 |
| 火力压制 Suppressive Blast | Cone 70° 6m | 跟随弹药×0.6 | 引导3s持续射击：伤害降低40%，范围内丧尸移速-50%+无法冲刺(持续6s) | 弹药3 | 14s | 6 | 不是杀丧尸——是让它们走不动，给队友/NPC争取时间 |
| 近距本能 Close Range Instinct | 被动 | — | 3m内伤害+25%, 1.5m内必定硬直 | — | — | — | 奖励贴脸的极端玩法 |

### 2.3 步枪 (Rifle) — 远距精准 / 弹药最稀有

> 全游戏射程最远、精度最高、弹药最稀有。核心玩法是"选位→架枪→精密射击"。每一发子弹都珍贵。

| 技能名 | 形状 | 伤害 | 效果 | 消耗 | CD | 噪音 | 说明 |
|--------|------|------|------|------|----|------|------|
| 普通射击 Normal Fire | Ray 25m | 跟随弹药 | 基础射击。站定散布极小，移动散布×2.0(不适合移动射击) | 弹药1 | — | 6 | 弹匣5发。移动中几乎无法有效命中 |
| 屏息精瞄 Breath Control | Ray 30m | 跟随弹药 | 屏息3s：散布-60%, 瞄准晃动归零, 暴击+15%。体力持续消耗(8/s)，体力耗尽或移动提前结束。结束后2s内体力恢复-50% | 弹药1+体力8/s | 4s | 6 | 步枪核心精度循环——屏息→射击→恢复→再屏息 |
| 架枪 Braced Fire | Ray 30m | 跟随弹药 | Toggle：需靠近掩体/窗台/地面。后坐力-50%, 射速+25%, 散布-30%, 无法移动。移动即取消 | 弹药1 | 2s(切换) | 6 | 将掩体转化为战斗力倍增器——步枪手的核心站位技 |
| 快速补射 Follow-up Shot | Ray 25m | 跟随弹药 | Buff 4s：每发子弹后后坐力恢复时间-70%, 射速有效提升 | 体力10+弹药1 | 10s | 6 | 对单目标快速补枪——Boss战输出窗口 |
| 远程狙击 Long Range Sniping | Ray 50m | 跟随弹药×1.8 | 进入狙击姿态：视野拉近2×, 射程翻倍, 命中+25%, 暴击伤害+50%, 无法移动+起身需1s。首发命中后自动解除 | 弹药1+体力20 | 15s | 6* | 远距终结技——选好位置、算好距离、一发入魂 |
| 步枪专家 Rifle Expert | 被动 | — | 暴击伤害+40%, 站立不动2s后散布-20%(移动后重置), 架枪姿态下额外+10%暴击 | — | — | — | 鼓励站桩架点的被动设计 |

> *狙击姿态噪音传播半径+50%，有效可达180m。

### 2.4 通用技能（不绑定武器）

| 技能名 | 形状 | 伤害 | 效果 | 消耗 | CD | 噪音 |
|--------|------|------|------|------|----|------|
| 翻滚闪避 Combat Roll | None(自身) | — | 0.3s无敌帧, 位移3m, 可取消近战后摇 | 体力20 | 2s | 3 |
| 紧急换弹 Emergency Reload | None(自身) | — | 立即装填50%弹夹, 之后15s换弹速度-40% | 无 | 25s | — |

---

## 三、Skill.Combat.Throwable — 投掷物

> 独立子系统。投掷物从物品栏消耗。抛物线弹道，部分有引信延迟。覆盖伤害型和非伤害型。

### 3.1 主动技能

| 技能名 | 形状 | 伤害类型 | 伤害 | 效果 | 消耗 | CD | 噪音 |
|--------|------|---------|------|------|------|----|------|
| 燃烧瓶 Molotov Cocktail | Circle 4m | Fire | 中 1.2×+DoT | DoT(0.4×/tick, 6s)+地面燃烧区8s, 丧尸绕行 | 燃烧瓶×1 | 8s | 5 |
| 破片手雷 Frag Grenade | Circle 5m | Pierce(破片) | 高 2.5× | Knockback(大幅, 4m), 2s引信 | 手雷×1 | 15s | 6 |
| 烟雾弹 Smoke Grenade | Circle 5m | — | — | Debuff(丧尸视野归零/移速-50%, 8s), 玩家可穿越 | 烟雾弹×1 | 10s | 4 |
| 震撼弹 Flashbang | Circle 4m | — | — | Debuff(致盲4s+减速30% 6s) | 震撼弹×1 | 12s | 5 |
| 酸液瓶 Acid Vial | Circle 3m | Acid | 中 1.0×+DoT | DoT(腐蚀, 0.3×/tick, 5s)+Debuff(-25%护甲, 10s) | 酸液瓶×1 | 8s | 4 |
| 毒气瓶 Poison Gas | Circle 5m | Poison | 低 0.5×+DoT | DoT(0.2×/tick, 12s), 云团持续12s扩散至6m | 毒气瓶×1 | 14s | 4 |
| 飞刀 Throwing Knife | Ray 15m | Pierce | 中 1.2× | 瞬发无引信, 可回收 | 飞刀×1 | 1s | 2 |
| 诱饵瓶 Bait Bottle | Circle 8m(噪音区) | — | — | 着陆点制造噪音5级+气味标记(丧尸优先前往, 8s) | 诱饵瓶×1 | 6s | 5(着陆) |
| 投掷瞄准 Throw Aim | None(自身) | — | — | 显示抛物线预览+落点范围圈 | 无 | — | — |

### 3.2 被动技能

| 技能名 | 触发 | 效果 |
|--------|------|------|
| 投掷精通 Throw Mastery | OnEquip | 投掷距离+20%, 抛物线预览时间+1s |
| 爆炸专家 Explosives Expert | OnEquip | 爆炸类范围+15%, 对建筑伤害+30% |
| 轻装简行 Light Loadout | OnEquip | 投掷物携带重量-30%, 投掷后摇-20% |
| 引信控制 Fuse Control | OnEquip | 可长按延迟投掷("cook"手雷), 最大3s |
| 回收利用 Salvage Throws | OnKill | 击杀丧尸10%概率回收一枚已消耗投掷物 |

---

## 四、Skill.Combat.Defensive — 防御

> 完整防御子系统。核心资源为体力，部分需要盾牌装备。定位是"承受伤害+保护NPC+制造反击窗口"。

### 4.1 主动技能

| 技能名 | 形状 | 伤害类型 | 伤害 | 效果 | 消耗 | CD | 噪音 |
|--------|------|---------|------|------|------|----|------|
| 格挡姿态 Guard Stance | None(自身) | — | — | Toggle：正面格挡率+60%, 移速-30%, 格挡成功消耗体力=受伤×0.3 | 无 | 0.5s | 1 |
| 盾牌猛击 Shield Bash | Ray 1.5m | Blunt | 低 0.6× | Impact(击退2m+硬直0.8s), 打断丧尸攻击前摇 | 体力15 | 4s | 3 |
| 铁壁 Hunker Down | None(自身) | — | — | Buff(伤害减免+40%, 无法移动, 霸体, 5s), 可提前取消 | 体力25 | 14s | 2 |
| 掩护射击 Covering Fire | Ray 15m | Pierce | 极低 0.4× | Debuff(命中丧尸移速-30%, 2s), 3s连发压制 | 弹药5 | 10s | 5 |
| 守护光环 Guardian Aura | Circle 6m | — | — | Buff(范围内盟友伤害减免+20%, 自身仇恨+100%, 8s) | 体力30 | 20s | 3 |
| 盾墙 Shield Wall(蓄力) | Cone 120° 2m | — | — | 前方阻挡区, 丧尸碰触即止步, 持续4s, 蓄力越久越宽 | 体力35 | 18s | 4 |
| 招架 Parry | Ray 2m | 跟随武器 | 中 1.3× | 受击前0.3s触发→免疫该次伤害+自动反击, 失败则受全伤+硬直 | 体力10 | 2s | 3 |

### 4.2 被动技能

| 技能名 | 触发 | 效果 |
|--------|------|------|
| 盾牌专精 Shield Specialist | OnEquip(持盾) | 持盾时格挡率+25%, 格挡体力消耗-20% |
| 坚韧不屈 Unyielding | OnLowHP(<30%) | 伤害减免+20% |
| 守护者 Guardian | OnEquip | 5m内盟友伤害减免+15% |
| 钢筋铁骨 Iron Body | OnEquip | 硬直阈值+40%, 被击退距离-30% |
| 反弹护甲 Reactive Armor | OnDamaged(近战) | 对攻击者返还伤害×15% |
| 以守为攻 Defensive Rhythm | OnHit(格挡成功后) | 格挡成功后3s内下一次攻击伤害+20% |

---

## 五、Skill.Combat.Stealth — 潜行

> 完整潜行子系统。核心是**噪音管理和可见度管理**——移动技术、注意力操控、环境利用。潜行状态降低丧尸探测半径约60%。
>
> 消音/瞄准镜等 = 装备属性，不是技能。技能 = 你**做的动作**。

### 5.1 主动技能

| 技能名 | 形状 | 伤害 | 效果 | 消耗 | CD | 噪音 |
|--------|------|------|------|------|----|------|
| 潜行模式 Stealth Mode | None(自身) | — | Toggle：脚步噪音-80%, 移速-25%, 可见度-40% | 无 | 0.5s(切换) | 1 |
| 暗杀 Assassination | Ray 1.5m | Pierce 极高 3.5× | 未被发现时HP<50%即死, 被发现后仅1.5× | 体力25 | 6s | 1 |
| 闷棍 Blackjack | Ray 1.5m | Blunt 中 1.0× | Impact(眩晕4s+丧尸解除警戒), 需未被发现 | 体力18 | 5s | 1 |
| 原地伪装 Improvised Camouflage | None(自身) | — | 静止3s后可见度-80%, 持续至移动, 最大30s。利用周围环境就地伪装 | 体力10 | 15s | 1 |
| 引诱哨 Distraction Whistle | Ray 25m | — | 瞄准点制造噪音4级+气味标记(6s), 引导丧尸前往 | 体力5 | 8s | 4(哨声) |
| 尸体搬运 Drag Body | None(尸体) | — | 拖拽尸体到玩家位置, 移速-40%, 可随时松手 | 无 | — | 2 |
| 快速脱离 Quick Escape | None(自身) | — | 脱离战斗+重置丧尸搜索状态(非Boss), 冲刺5m | 体力30 | 20s | 2 |

### 5.2 被动技能

| 技能名 | 触发 | 效果 |
|--------|------|------|
| 无声杀戮 Silent Killer | OnEquip | 暗杀/闷棍噪音-2级, 尸体不发出倒地声响 |
| 第六感 Sixth Sense | OnEquip | 被丧尸察觉前0.5s屏幕边缘红光方向提示 |
| 背刺专家 Backstab Expert | OnEquip | 背后120°攻击伤害+35% |
| 暗影行者 Shadow Walker | OnEquip | 夜间/室内可见度额外-30%, 潜行移速惩罚减半 |
| 窃听 Ear to the Ground | OnEquip | 可听到墙后丧尸呼吸声(10m), 小地图方向指示 |

---

## 六、Skill.Utility.Medical — 医疗

> **技能≠物品。** 医疗技能代表急救能力，施放时消耗医疗物品作为催化剂。没有技能的角色即使有绷带也只能缓慢自愈。摆脱"红药水"思维——分阶段伤口处理：止血→消毒→缝合→手术。

### 6.1 主动技能

| 技能名 | 目标 | 效果 | 消耗 | 引导 | CD |
|--------|------|------|------|------|----|
| 快速包扎 Quick Bandage | 自身/友方 | 恢复HP 15%+移除Bleeding | 绷带×1 | 1.5s | 4s |
| 止血带 Tourniquet | 自身/友方 | 移除Bleed/Bleeding DoT+5s内免疫Bleed | 止血带×1 | 2s | 8s |
| 骨折固定 Splint | 自身/友方 | 移除Cripple状态+移速恢复 | 夹板×1+布料×1 | 3s | 12s |
| 心肺复苏 CPR | 友方(倒地) | 复活Downed队友(HP 20%), 需倒地30s内 | 无 | 8s(可中断) | 60s |
| 解毒 Detoxification | 自身/友方 | 移除Poison/Poisoned DoT+10s抗毒Buff | 活性炭×1 | 2s | 6s |
| 消毒清创 Disinfect Wound | 自身/友方 | 移除Infected+Disease风险, 防止丧尸化 | 消毒剂×1 | 2.5s | 8s |
| 战地急救 Combat Medic | 友方 | 恢复HP 30%, 可在战斗中引导(移速-50%) | 急救包×1 | 4s | 15s |
| 诊断 Assessment | 自身/友方 | 显示所有负面状态+剩余时间+HP精确值 | 无 | 0.5s | 2s |

### 6.2 被动技能

| 技能名 | 触发 | 效果 |
|--------|------|------|
| 医学知识 Medical Knowledge | OnEquip | 所有医疗技能效果+20% |
| 快速施救 Quick Hands | OnEquip | 医疗技能引导时间-25% |
| 战地医师 Field Surgeon | OnEquip | 医疗技能取消战斗中引导移速惩罚 |
| 药剂增效 Pharmacology | OnEquip | 药品类物品效果+30%, 副作用-50% |
| 自体愈合 Natural Recovery | OnEquip | HP自然恢复+50%, 非战斗每30s移除一种轻微负面状态 |

---

## 七、Skill.Utility.Survival — 生存

> 荒野求生的核心技能。影响资源循环、环境适应、长期生存能力。核心定位："没有对应技能就无法执行对应动作"——能力门设计。

### 7.1 主动技能

| 技能名 | 效果 | 消耗 | 引导 | CD |
|--------|------|------|------|----|
| 生火 Make Fire | 制造营火:光源+取暖+可烹饪+驱赶小动物 | 木材×2+打火机耐久 | 3s | — |
| 净水 Purify Water | 脏水→净水(需靠近火源或用净水片) | 脏水×1+(净水片或营火) | 5s/1s | — |
| 搭建临时庇护所 Shelter | 建造临时庇护点:加速回血+保存游戏+防风雨 | 木材×5+布×3+绳索×2 | 15s | 60s |
| 追踪猎物 Track Prey | Buff(动物足迹高亮+狩猎产量+30%, 30s) | 体力15 | — | 20s |
| 屠宰 Field Dress | 从尸体获取额外资源(肉/皮/骨/脂肪)+30% | 体力20 | 5s/具 | — |
| 采集增效 Foraging Focus | Buff(采集产量+50%+稀有草药发现率翻倍, 30s) | 体力10 | — | 60s |
| 天气预判 Weather Sense | 显示未来6h天气趋势, 可提前2h预警尸潮 | 无 | 1s | 60s |
| 紧急避难 Emergency Shelter | 原地躲藏:可见度-90%, 丧尸不主动攻击, 10s | 无 | — | 40s |

### 7.2 被动技能

| 技能名 | 触发 | 效果 |
|--------|------|------|
| 野外知识 Wilderness Knowledge | OnEquip | 可采摘植物显示名称+毒性/可食用标识+稀有度 |
| 觅食大师 Master Forager | OnEquip | 采集产量+20%, 稀有发现率+10% |
| 方向感 Sense of Direction | OnEquip | 地图常驻指北针, 迷雾-20% |
| 耐寒耐热 Extreme Conditioning | OnEquip | 温度舒适区±15°C, 极端温度伤害-40% |
| 资源眼 Resource Eye | OnEquip | 15m内可采集资源小地图图标 |

---

## 八、Skill.Utility.Craft — 工艺

> 维修、改装、弹药复装、简易制作。核心定位："维持装备运转，降低对外部物资依赖"。与Trap树联动（制作陷阱消耗品）。

### 8.1 主动技能

| 技能名 | 效果 | 消耗 | 引导 | CD |
|--------|------|------|------|----|
| 临时修理 Field Repair | 恢复目标装备耐久30% | 零件×3 | 3s | — |
| 武器打磨 Weapon Sharpening | Buff(武器伤害+15%, 持续至耐久损耗15%) | 磨刀石耐久 | 2s | 30s |
| 弹药复装 Ammo Reloading | 弹壳+火药+弹头→成品弹药 | 弹壳+火药+弹头 | 5s/发 | — |
| 简易炸药 Improvised Explosive | 制作简易爆炸物(1.8×, 可投掷) | 火药×3+容器×1+引信×1 | 8s | 30s |
| 武器改装 Weapon Modding | 加装配件(消音器/瞄准镜/弹匣, 持续至卸下) | 零件×5+对应材料 | 5s | — |
| 零部件拆解 Part Salvage | 拆解废旧物品获取零件(+50%回收) | 体力10 | 2s/件 | — |
| 陷阱制作 Craft Trap | 制作简易陷阱(捕兽夹/警报器/绊索) | 视陷阱类型 | 6s | 10s |
| 化学提炼 Chemical Extraction | 从废料提炼燃料/酸液/毒剂(需靠近水源) | 废料×2+水×1 | 8s | 20s |

### 8.2 被动技能

| 技能名 | 触发 | 效果 |
|--------|------|------|
| 能工巧匠 Handyman | OnEquip | 所有制作/修理速度+25% |
| 废品利用 Scavenger's Eye | OnEquip | 拆解回收+30%, 5%概率获得稀有零件 |
| 精密工艺 Precision Crafting | OnEquip | 制作装备/弹药品质提升一档(普通→精良→优秀) |
| 材料节省 Material Efficiency | OnEquip | 制作/修理15%概率不消耗材料 |
| 即兴创造 Improvised Genius | OnEquip | 可用替代材料完成制作(品质-1档) |

---

## 九、Skill.Utility.Trade — 交易

> 多为被动技能。末日经济学核心——不是"卖得更贵"，而是"建立信任、扩展网络、理解价值"。

### 9.1 主动技能

| 技能名 | 效果 | CD |
|--------|------|----|
| 议价 Haggle | 本次交易价格-20%(魅力检定, 成功率50%+魅力修正), 失败则不能再议价 | 每交易限1次 |
| 估价 Appraise | 显示物品在全部已知定居点的当前卖价+买价+稀缺度 | 2s |
| 唬骗 Fast Talk | 本次价格-35%, 但失败则+20%且信誉-1(难度+30%) | 每交易限1次 |
| 投资 Investment | 投入200物资, 此后该商人商品种类+30%+价格-10%(永久), 需声望≥友好 | 每定居点限1次 |

### 9.2 被动技能

| 技能名 | 触发 | 效果 |
|--------|------|------|
| 商人之眼 Merchant's Eye | OnEquip | 所有物品显示基础估价+稀有度颜色 |
| 人情债 Favors Owed | OnEquip | 每个定居点首次交易价格-15% |
| 以物易物 Barter Economy | OnEquip | 以物代币价值折损-20% |
| 人脉网络 Network | OnEquip | 解锁隐藏商人+特殊商品, 每3定居点+1隐藏商品 |
| 精明买家 Shrewd Buyer | OnEquip | 所有交易买价-8%+卖价+8% |
| 旅途商人 Caravan Trader | OnEquip | 可向已发现商人远程下订单(1-3天送达) |
| 信誉积累 Reputation | OnEquip | 每次完成商人委托, 该定居点永久价格-3%(最多-15%) |

---

## 十、Skill.Utility.Lockpicking — 撬锁

> 废墟探索核心技能。每个上锁的箱子、车门、保险柜都是机会。

### 10.1 主动技能

| 技能名 | 目标 | 效果 | 消耗 | 引导 | CD | 噪音 |
|--------|------|------|------|------|----|------|
| 撬锁 Lockpicking | 机械锁 | 成功概率基于技能等级+工具品质 | 发卡(可能断裂) | 3-8s | — | 1 |
| 暴力破锁 Force Entry | 锁/门 | 高成功率但有噪音+10%概率损坏内容物 | 体力25 | 2s | — | 5 |
| 电子锁破解 Hack Terminal | 电子锁 | 迷你游戏匹配代码序列 | 电子工具×1 | 5-12s | 8s | 1 |
| 热切割 Thermal Cut | 金属门/保险柜 | 100%成功但极慢+极高噪音+消耗燃料 | 燃料20 | 8s | — | 5 |
| 钥匙模具 Key Impression | 锁 | 制作临时钥匙(需先观察到钥匙), 成功率70% | 模具材料×1 | 6s | — | 1 |
| 安全检查 Security Check | 锁/门/容器 | 检查是否被陷阱保护, 显示陷阱类型+难度 | 无 | 1s | 2s | — |

### 10.2 被动技能

| 技能名 | 触发 | 效果 |
|--------|------|------|
| 灵巧手指 Nimble Fingers | OnEquip | 撬锁速度+30%, 发卡断裂概率-50% |
| 安全专家 Security Expert | OnEquip | 显示锁难度等级+是否被陷阱保护 |
| 电子学基础 Basic Electronics | OnEquip | 电子锁破解时间-25%, 容错+1 |
| 静默破门 Silent Entry | OnEquip | 所有撬锁/破解技能噪音-80% |
| 寻宝直觉 Loot Sense | OnEquip | 进入建筑5s内高亮上锁容器(5m内) |

---

## 十一、Skill.Trap — 陷阱

> 完整陷阱子系统。陷阱为可放置物理物件，触发后产生一次性效果。与Craft树联动（制作陷阱需要Craft技能）。所有陷阱复用已有管道：`OnEnterArea → NotifyEvent → PassiveAbilitySO匹配 → Effects → HitReactionComponent`。

### 11.1 主动技能

| 技能名 | 触发形状 | 伤害类型 | 伤害 | 效果 | 消耗 | 放置 | CD | 噪音 |
|--------|---------|---------|------|------|------|------|----|------|
| 捕兽夹 Bear Trap | Circle 1m | Pierce | 中 1.5× | Bleed DoT(0.3×, 6s)+定身4s | 捕兽夹×1 | 1.5s | 5s | 1(放)/4(触发) |
| 绊雷 Trip Mine | Cone 60° 3m | Pierce(破片) | 高 2.0× | Knockback(中幅, 3m), 0.5s引爆 | 绊雷×1 | 2.5s | 12s | 1(放)/6(引爆) |
| 警报器 Alarm Trap | Circle 12m(噪音) | — | — | 噪音6级+信号弹升空, 8s | 警报器×1 | 1s | 8s | 6(触发) |
| 毒气陷阱 Gas Trap | Circle 4m→6m | Poison | 低 0.5×+DoT | DoT(0.25×, 10s), 云团扩散 | 毒气罐×1 | 2s | 14s | 1(放)/4(触) |
| 电击陷阱 Shock Trap | Circle 2m | Shock | 中 1.0× | 眩晕2s+连锁电击(2m内2目标, -50%/跳) | 电池+电容 | 2s | 10s | 1(放)/4(触) |
| 铁丝网 Razor Wire | Line 3m(持续) | Slash | 低 0.4×/接触 | Bleed DoT(0.2×)+减速60%, 有HP可被摧毁 | 铁丝网×1 | 3s | 15s | 3(装)/4(碰) |
| 地刺 Pitfall Spike | Circle 1m | Pierce | 极高 3.0× | 需先挖坑(8s)+放尖刺(3s), 非Boss即死 | 木材×3+体力30 | 11s | 25s | 2(装) |
| 油滑陷阱 Oil Slick | Circle 3m | — | — | Debuff(滑倒2s, 然后减速60% 8s) | 油×1 | 1.5s | 8s | 1 |
| 绊索 Tripwire | Line 2m | — | — | Impact(绊倒+硬直1.5s), 不造成伤害 | 绳索×2 | 1.5s | 5s | 1 |
| 噪音诱饵 Noise Decoy | Circle 10m(噪音) | — | — | 5s倒计时后噪音6级+灯光8s, 吸引丧尸 | 电子零件+电池 | 1s | 12s | 6(触发) |

### 11.2 被动技能

| 技能名 | 触发 | 效果 |
|--------|------|------|
| 陷阱大师 Trap Master | OnEquip | 陷阱伤害+25%, 触发半径+15% |
| 快速部署 Rapid Deployment | OnEquip | 陷阱放置时间-40% |
| 伪装陷阱 Camouflaged Trap | OnEquip | 陷阱可见度-70%, 丧尸探测陷阱范围-50% |
| 拆卸专家 Trap Disarm | OnEquip | 可发现并拆除敌对陷阱, 回收材料70% |
| 连环陷阱 Chain Trap | OnEquip | 一个触发时5m内其他陷阱1s内可触发 |
| 陷阱回收 Trap Recovery | OnEquip | 未触发陷阱可完整回收, 触发后回收+30% |

---

## 十二、伤害/效果/形状/被动 全量覆盖矩阵

### 12.1 逐分类技能数量

| 标签分类 | 主动 | 被动 | 合计 | Phase |
|---------|------|------|------|-------|
| Skill.Combat.Melee | 16 | 3 | **19** | A测 |
| Skill.Combat.Ranged | 15 | 3 | **18** | A测 |
| Skill.Combat.Throwable | 9 | 5 | **14** | Phase 2+ |
| Skill.Combat.Defensive | 7 | 6 | **13** | Phase 2+ |
| Skill.Combat.Stealth | 8 | 5 | **13** | Phase 2+ |
| Skill.Utility.Medical | 8 | 5 | **13** | Phase 2+ |
| Skill.Utility.Survival | 8 | 5 | **13** | Phase 2+ |
| Skill.Utility.Craft | 8 | 5 | **13** | Phase 2+ |
| Skill.Utility.Trade | 4 | 7 | **11** | Phase 2+ |
| Skill.Utility.Lockpicking | 6 | 5 | **11** | Phase 2+ |
| Skill.Trap | 10 | 6 | **16** | Phase 2+ |
| **合计** | **99** | **55** | **154** | |

### 12.2 伤害类型覆盖

| 伤害类型 | 技能数 | 代表性技能 |
|---------|--------|-----------|
| Slash | ~20 | 刀/斧基础技、剑道终结技(突刺/一之太刀)、苗刀术终结技(回身斩/力劈华山)、铁丝网(陷阱) |
| Blunt | ~15 | 棍基础技、拳击/泰拳/八极拳/咏春/太极/柔道/散打各流派终结技、枪托猛击(霰弹)、盾牌猛击(防御) |
| Pierce | ~16 | 刀基础重击、手枪全系、步枪全系、飞刀(投掷)、捕兽夹(陷阱)、咏春标指终结技 |
| Fire | 3 | 龙息弹(霰弹E)、燃烧瓶(投掷Q)、油滑+燃烧连锁 |
| Shock | 1 | 电击陷阱(陷阱G) |
| Acid | 2 | 酸液瓶(投掷G)、化学废料环境 |
| Poison | 3 | 毒气瓶(投掷T)、毒气陷阱(陷阱F) |
| True(火焰) | 1 | 龙息弹(霰弹E) |
| Bite | — | 丧尸专用，玩家不造成 |
| Cold | Phase 2+ | 液氮陷阱预留 |
| Radiation | Phase 2+ | 辐射区+辐射手雷预留 |
| Fall | — | 环境伤害（击退坠楼/踩坑） |

### 12.3 搜索形状覆盖

| 形状 | 技能数 | 分布 |
|------|--------|------|
| **Cone** | ~15 | 近战横扫/劈砍、霰弹散射、盾墙、绊雷 |
| **Ray** | ~25 | 近战刺击/重击、手枪全系、步枪全系、飞刀、暗杀、盾击、招架 |
| **Circle** | ~20 | 棍AoE、斧旋转、投掷物全部、陷阱触发区、守护光环 |
| **None** | ~45 | 自身Buff、医疗/生存/工艺/交易/撬锁大量技能 |

### 12.4 被动触发覆盖

| ETriggerEvent | 已使用技能数 | 状态 |
|---------------|------------|------|
| OnEquip | 大量 | ✅ 所有武器专精+非战斗被动 |
| OnKill | 5+ | ✅ 八极拳绝学(击杀伤害叠层)、苗刀术精妙(击杀回攻)、苗刀术绝学(杀3人免体力)、柔道绝学(投技击杀CD重置)、回收利用(投掷) |
| OnHit | 3+ | ✅ 咏春连击加成、散打节奏叠层、以守为攻(防御) |
| OnDamaged | 3 | ✅ 太极拳格挡反击、反弹护甲(防御)、近距杀手(霰弹) |
| OnLowHP | 4+ | ✅ Krav Maga绝学(濒死增伤)、太极拳绝学(低体力格挡减免)、坚韧不屈(防御)、Krav Maga被动(濒死不可被处决) |
| OnEnterArea | 全部陷阱 | ✅ 已落地 |
| OnExitArea | 1 | ✅ 陷阱解除 |
| OnDodge | Phase 2+ | 闪避反击预留 |
| OnComboStage | Phase 2+ | 连击节奏预留 |
| **OnBlock** | **新增需求** | Agent E 审查建议新增 |
| **OnHeal** | **新增需求** | 医疗触发额外效果 |
| **OnCraft** | **新增需求** | 制作完成触发奖励 |
| **OnLockPick** | **新增需求** | 开锁触发效果 |
| **OnStealthBreak** | **新增需求** | 潜行被发现触发应急 |

---

## 十三、跨系统缺口与 Phase 2+ 预留

### 13.1 Agent E 审查标记的关键缺失

| 缺失维度 | 严重度 | 建议 | Phase |
|----------|--------|------|-------|
| **照明系统** | 高 | 手电筒(Toggle)、火把(Craft)、夜视适应(Survival被动)、信号弹(Throwable) | Phase 2+ |
| **NPC指挥技能** | 高 | 指挥:掩护开火、指挥:撤离、指挥:使用医疗包、指挥:部署陷阱 | Phase 2+ |
| **即兴武器** | 高 | 水管/扳手(Blunt低)、棒球棍(Blunt中)、碎玻璃(Pierce低) | Phase 2+ |
| **环境互动** | 高 | 推撞墙(Impact撞墙→额外伤害)、射击油桶(Circle爆炸)、翻越障碍(Vault) | Phase 2+ |
| **噪音管理—消减** | 中 | 消音器通用(任何热武器可加装)、无声奔跑(Stealth被动) | Phase 2+ |
| **尸潮专属** | 中 | 集结号令(Circle Buff)、紧急路障(Instant)、尸潮感知(Survival被动预警) | Phase 2+ |
| **濒死被动** | 中 | Last Stand (OnLowHP: ATK+30%, DEF-20%) | Phase 2+ |

### 13.2 需要新增的 EffectSO 子类

| 新增类型 | 用途 | 关键字段 |
|---------|------|---------|
| **HealEffectSO** | 医疗技能恢复HP | `healAmount`, `canOverheal(bool)` |
| **CleanseEffectSO** | 移除Debuff/DoT标签 | `removedTags[]` (GameplayTag[]) |
| **SpawnObjectEffectSO** | 放置篝火/陷阱物体 | `prefabRef`, `duration` |
| **ItemConsumeEffectSO** | 消耗物品（绷带/子弹/材料） | `itemTag` (Equip.Type.*), `amount` |
| **NoisePlaceEffectSO** | 在特定位置产生噪音 | `noiseType`, `level`, `position` |

### 13.3 需要新增的 ETriggerEvent

| 新触发 | 使用场景 |
|--------|---------|
| `OnBlock` | 格挡成功时触发反击/反击奖励 |
| `OnHeal` | 医疗行为触发额外恢复 |
| `OnCraft` | 制作完成触发批量奖励 |
| `OnLockPick` | 开锁成功/失败触发效果 |
| `OnStealthBreak` | 潜行被发现触发应急反应 |

### 13.4 激活方式扩展

| 方式 | 现有 | Phase 2+ |
|------|------|----------|
| Instant(瞬发) | ✅ 全部 | — |
| Charged(蓄力) | 0 | 弓弩、蓄力重击、盾墙蓄力 |
| Channel(持续) | 0 | 火焰喷射器、电锯、医疗引导 |
| Toggle(开关) | 0(用OnEquip模拟) | 手电筒、潜行模式、格挡姿态 |

---

## 十四、闭环测试最小技能集

> 当前 Ability Pipeline（②→③→④→⑤→⑥→⑧）已实现。连击链延后。
> 5 个技能覆盖全部搜索形状、Physical伤害、效果类型、消耗类型、4档噪音。

### 14.1 测试技能 (5 个)

| # | 技能 | 形状 | 伤害 | 效果 | 消耗 | 噪音 | 验证点 |
|---|------|------|------|------|------|------|--------|
| 1 | 刀·轻击 | Cone 60° 2m | Slash 1.0× | Damage | 体力8 | 3 | ②门控→③扣费→④Cone搜索→⑤SResolvedHit→⑥结算→⑧广播 |
| 2 | 棍·重击 | Ray 3m | Blunt 1.3× | Impact(小硬直) | 体力20 | 4 | Ray搜索+Impact效果 |
| 3 | 手枪·普通射击 | Ray 12m | Pierce(弹药决定) | Damage | 弹药1 | 5 | 弹药Cost+Noise 5广播 |
| 4 | 霰弹·普通射击 | Cone 50° 5m | Pierce(弹药决定) | Damage | 弹药1 | 6 | 弹药Cost+Noise 6广播 |
| 5 | 捕兽夹(被动) | Circle 1m | Pierce 1.5× | Bleed DoT+定身 | 捕兽夹×1 | 4(触发) | OnEnterArea→NotifyEvent→管道全程 |

### 14.2 覆盖矩阵

| 维度 | 覆盖 | 由谁验证 |
|------|------|---------|
| 搜索形状 | Cone ✅ Ray ✅ Circle ✅ | 刀+霰弹(Cone) / 棍+手枪(Ray) / 捕兽夹(Circle) |
| Physical伤害 | Slash ✅ Blunt ✅ Pierce ✅ | 刀(Slash) / 棍(Blunt) / 手枪+霰弹+捕兽夹(Pierce) |
| 效果类型 | Damage ✅ Impact ✅ DoT ✅ | 刀+手枪+霰弹(Damage) / 棍(Impact) / 捕兽夹(DoT) |
| 消耗类型 | 体力 ✅ 弹药 ✅ 物品 ✅ | 刀+棍(体力) / 手枪+霰弹(弹药) / 捕兽夹(物品) |
| 噪音广播 | Lv3 ✅ Lv4 ✅ Lv5 ✅ Lv6 ✅ | 刀(3) / 棍+捕兽夹(4) / 手枪(5) / 霰弹(6) |
| 触发类型 | 主动 TryActivate ✅ 被动 NotifyEvent ✅ | 前4个/捕兽夹 |
| 门控 | 冷却+互斥+条件 ✅ | 捕兽夹走完整被动门控 |

### 14.3 需要创建的 SO 资产

| # | 类型 | 资产名 | 说明 |
|---|------|--------|------|
| 1 | `AbilityDefSO` | `Melee_Blade_Light` | 刀轻击：Cone, Slash, 体力8 |
| 2 | `AbilityDefSO` | `Melee_Staff_Heavy` | 棍重击：Ray, Blunt+Impact, 体力20 |
| 3 | `AbilityDefSO` | `Ranged_Pistol_Normal` | 手枪射击：Ray, 弹药1, 噪音5 |
| 4 | `AbilityDefSO` | `Ranged_Shotgun_Normal` | 霰弹射击：Cone, 弹药1, 噪音6 |
| 5 | `PassiveAbilitySO` | `Trap_BearTrap` | 捕兽夹：OnEnterArea, Circle, Pierce+Bleed |
| 6 | `DamageEffectSO` | `Dmg_Slash_Base` | Slash 基础伤害 |
| 7 | `DamageEffectSO` | `Dmg_Blunt_Base` | Blunt 基础伤害 |
| 8 | `DamageEffectSO` | `Dmg_Pierce_Base` | Pierce 基础伤害 |
| 9 | `DamageEffectSO` | `Dmg_Pierce_Bleed` | Pierce+Bleed DoT |
| 10 | `ImpactEffectSO` | `Impact_LightStun` | 小硬直 |
| 11 | `CostEffectSO` | `Cost_Stamina_8` | 体力消耗 8 |
| 12 | `CostEffectSO` | `Cost_Stamina_20` | 体力消耗 20 |
| 13 | `CostEffectSO` | `Cost_Ammo_1` | 弹药消耗 1 |
| 14 | `ConeSearchSO` | `Search_Cone_2m` | Cone 60° range 2m |
| 15 | `ConeSearchSO` | `Search_Cone_5m` | Cone 50° range 5m |
| 16 | `RaySearchSO` | `Search_Ray_3m` | Ray range 3m |
| 17 | `RaySearchSO` | `Search_Ray_12m` | Ray range 12m |
| 18 | `CircleSearchSO` | `Search_Circle_1m` | Circle radius 1m |
| 19 | `NoiseEventSO` | `Noise_Lv3_MeleeLight` | 噪音 3 |
| 20 | `NoiseEventSO` | `Noise_Lv4_MeleeHeavy` | 噪音 4 |
| 21 | `NoiseEventSO` | `Noise_Lv5_Pistol` | 噪音 5 |
| 22 | `NoiseEventSO` | `Noise_Lv6_Shotgun` | 噪音 6 |
| 23 | `AbilityActivationSO` | `Activation_Instant` | 瞬发激活，共用 |
| 24 | `StatDefinitionSO` | `Stat_Stamina` | 体力属性 |
| 25 | `StatDefinitionSO` | `Stat_Ammo` | 弹药属性 |

### 14.4 需补建的 Tag 资产

> 当前已有 32 个 Actor + Damage 标签。闭环测试需补建 4 棵子树（Noise/Stat/Damage/Ability）。
>
> `Skill.*` 树不再用于运行时 Tag——仅编辑时引用。互斥由 `abilityTag` 层级实现。

**Noise 树** (全新, 5 个)
```
Noise/Tag_Noise                 ← 根
Noise/Combat/Tag_Combat          ← parent=Noise
Noise/Combat/Tag_MeleeSwing      ← parent=Combat
Noise/Combat/Tag_WeaponFire      ← parent=Combat
Noise/Combat/Tag_Impact          ← parent=Combat
```

**Stat 树** (全新, 5 个, A测够用)
```
Stat/Tag_Stat                   ← 根
Stat/Vital/Tag_Vital             ← parent=Stat
Stat/Vital/Tag_Stamina           ← parent=Vital
Stat/Pool/Tag_Pool               ← parent=Stat
Stat/Pool/Tag_Ammo               ← parent=Pool
```

**Damage 补充** (1 个)
```
Damage/Physical/Tag_Blunt        ← parent=Physical (已存在)
```

**Ability 树** (全新, 12 个，闭环测试用)
```
Ability/Tag_Ability                     ← 根
Ability/Melee/Tag_Melee                  ← parent=Ability
Ability/Melee/Blade/Tag_Blade            ← parent=Melee
Ability/Melee/Blade/Tag_LightCut         ← parent=Blade   (刀轻击)
Ability/Melee/Staff/Tag_Staff            ← parent=Melee
Ability/Melee/Staff/Tag_HeavySmash       ← parent=Staff   (棍重击)
Ability/Ranged/Tag_Ranged               ← parent=Ability
Ability/Ranged/Pistol/Tag_Pistol         ← parent=Ranged
Ability/Ranged/Pistol/Tag_NormalFire     ← parent=Pistol  (手枪射击)
Ability/Ranged/Shotgun/Tag_Shotgun       ← parent=Ranged
Ability/Ranged/Shotgun/Tag_NormalFire    ← parent=Shotgun (霰弹射击)
Ability/Trap/Tag_Trap                   ← parent=Ability
Ability/Trap/Tag_BearTrap                ← parent=Trap    (捕兽夹)
```

| 技能 | abilityTag | 互斥层级(Parent) |
|------|-----------|-----------------|
| 刀·轻击 | `Ability.Melee.Blade.LightCut` | `Ability.Melee.Blade` |
| 棍·重击 | `Ability.Melee.Staff.HeavySmash` | `Ability.Melee.Staff` |
| 手枪·普通射击 | `Ability.Ranged.Pistol.NormalFire` | `Ability.Ranged.Pistol` |
| 霰弹·普通射击 | `Ability.Ranged.Shotgun.NormalFire` | `Ability.Ranged.Shotgun` |
| 捕兽夹 | `Ability.Trap.BearTrap` | `Ability.Trap` |

**共计 23 个新 Tag 资产**（Noise 5 + Stat 5 + Damage 1 + Ability 12）。

---

## 十五、完整 abilityTag 树

> 每个技能一个叶标签。`Ability.*` 为新增第 10 根标签树。
>
> 激活时施加(冷却>0)，冷却结束移除。层级决定互斥粒度：父标签做前缀匹配。必须是**叶标签**（无子节点）。
>
> `Skill.*` 树不再作为运行时 Tag——仅用于编辑时引用分类。

### 15.1 abilityTag 树 — 新增 `Ability` 根

> 每个主动/被动技能一个叶标签。`Ability.*` 为新增第 10 根标签树。

```
Ability/
├── Tag_Ability                              ← 根, parent=null
│
├── Melee/
│   ├── Tag_Melee                            ← parent=Ability
│   │
│   ├── Blade/                               ← 刀武器基础技
│   │   ├── Tag_Blade
│   │   ├── Tag_LightCut                      ← 轻击
│   │   ├── Tag_HeavyChop                     ← 重击
│   │   └── Tag_Block                         ← 格挡
│   │
│   ├── Staff/                               ← 棍武器基础技
│   │   ├── Tag_Staff
│   │   ├── Tag_LightSwing                    ← 轻击
│   │   ├── Tag_HeavySmash                    ← 重击
│   │   └── Tag_Block                         ← 格挡
│   │
│   ├── Axe/                                 ← 斧武器基础技
│   │   ├── Tag_Axe
│   │   ├── Tag_LightHack                     ← 轻击
│   │   ├── Tag_HeavyCleave                   ← 重击
│   │   └── Tag_Block                         ← 格挡
│   │
│   └── Routines/                            ← 武学套路 (Phase 2+)
│       ├── Tag_Routines
│       ├── Boxing/
│       │   ├── Tag_Boxing
│       │   ├── Tag_Fragment                  ← 残卷
│       │   ├── Tag_Complete                  ← 完整
│       │   ├── Tag_Refined                   ← 精妙
│       │   └── Tag_Masterwork                ← 绝学
│       ├── MuayThai/
│       ├── BajiQuan/
│       ├── WingChun/
│       ├── TaiChi/
│       ├── Judo/
│       ├── Eskrima/
│       ├── Kendo/
│       ├── MiaoDao/
│       ├── KravMaga/
│       └── Sanda/
│
├── Ranged/
│   ├── Tag_Ranged                            ← parent=Ability
│   │
│   ├── Pistol/
│   │   ├── Tag_Pistol
│   │   ├── Tag_NormalFire                    ← 普通射击
│   │   ├── Tag_QuickDraw                     ← 快速拔枪
│   │   ├── Tag_Akimbo                        ← 双持
│   │   ├── Tag_SpeedReload                   ← 快速换弹
│   │   └── Tag_MovingFire                    ← 移动射击
│   │
│   ├── Shotgun/
│   │   ├── Tag_Shotgun
│   │   ├── Tag_NormalFire                    ← 普通射击
│   │   ├── Tag_CloseQuarters                 ← 抵近射击
│   │   ├── Tag_StockStrike                   ← 枪托挥击
│   │   ├── Tag_CombatLoad                    ← 快速装填
│   │   └── Tag_SuppressiveBlast              ← 火力压制
│   │
│   └── Rifle/
│       ├── Tag_Rifle
│       ├── Tag_NormalFire                    ← 普通射击
│       ├── Tag_BreathControl                 ← 屏息精瞄
│       ├── Tag_BracedFire                    ← 架枪
│       ├── Tag_FollowUpShot                  ← 快速补射
│       └── Tag_LongRangeSniping              ← 远程狙击
│
├── Throwable/
│   ├── Tag_Throwable
│   ├── Tag_Molotov                           ← 燃烧瓶
│   ├── Tag_FragGrenade                       ← 破片手雷
│   ├── Tag_SmokeGrenade                      ← 烟雾弹
│   ├── Tag_Flashbang                         ← 震撼弹
│   ├── Tag_AcidVial                          ← 酸液瓶
│   ├── Tag_PoisonGas                         ← 毒气瓶
│   ├── Tag_ThrowingKnife                     ← 飞刀
│   └── Tag_BaitBottle                        ← 诱饵瓶
│
├── Defensive/
│   ├── Tag_Defensive
│   ├── Tag_GuardStance                       ← 格挡姿态
│   ├── Tag_ShieldBash                        ← 盾牌猛击
│   ├── Tag_HunkerDown                        ← 铁壁
│   ├── Tag_CoveringFire                      ← 掩护射击
│   ├── Tag_GuardianAura                      ← 守护光环
│   ├── Tag_ShieldWall                        ← 盾墙
│   └── Tag_Parry                             ← 招架
│
├── Stealth/
│   ├── Tag_Stealth
│   ├── Tag_StealthMode                       ← 潜行模式
│   ├── Tag_Assassination                     ← 暗杀
│   ├── Tag_Blackjack                         ← 闷棍
│   ├── Tag_ImprovisedCamouflage              ← 原地伪装
│   ├── Tag_DistractionWhistle                ← 引诱哨
│   ├── Tag_DragBody                          ← 尸体搬运
│   └── Tag_QuickEscape                       ← 快速脱离
│
├── Medical/
│   ├── Tag_Medical
│   ├── Tag_QuickBandage                      ← 快速包扎
│   ├── Tag_Tourniquet                        ← 止血带
│   ├── Tag_Splint                            ← 骨折固定
│   ├── Tag_CPR                               ← 心肺复苏
│   ├── Tag_Detoxification                    ← 解毒
│   ├── Tag_DisinfectWound                    ← 消毒清创
│   ├── Tag_CombatMedic                       ← 战地急救
│   └── Tag_Assessment                        ← 诊断
│
├── Survival/
│   ├── Tag_Survival
│   ├── Tag_MakeFire                          ← 生火
│   ├── Tag_PurifyWater                       ← 净水
│   ├── Tag_Shelter                           ← 搭建庇护所
│   ├── Tag_TrackPrey                         ← 追踪猎物
│   ├── Tag_FieldDress                        ← 屠宰
│   ├── Tag_ForagingFocus                     ← 采集增效
│   ├── Tag_WeatherSense                      ← 天气预判
│   └── Tag_EmergencyShelter                  ← 紧急避难
│
├── Craft/
│   ├── Tag_Craft
│   ├── Tag_FieldRepair                       ← 临时修理
│   ├── Tag_WeaponSharpening                  ← 武器打磨
│   ├── Tag_AmmoReloading                     ← 弹药复装
│   ├── Tag_ImprovisedExplosive               ← 简易炸药
│   ├── Tag_WeaponModding                     ← 武器改装
│   ├── Tag_PartSalvage                       ← 零部件拆解
│   ├── Tag_CraftTrap                         ← 陷阱制作
│   └── Tag_ChemicalExtraction                ← 化学提炼
│
├── Trade/
│   ├── Tag_Trade
│   ├── Tag_Haggle                            ← 议价
│   ├── Tag_Appraise                          ← 估价
│   ├── Tag_FastTalk                          ← 唬骗
│   └── Tag_Investment                        ← 投资
│
├── Lockpicking/
│   ├── Tag_Lockpicking
│   ├── Tag_Lockpick                          ← 撬锁
│   ├── Tag_ForceEntry                        ← 暴力破锁
│   ├── Tag_HackTerminal                      ← 电子锁破解
│   ├── Tag_ThermalCut                        ← 热切割
│   ├── Tag_KeyImpression                     ← 钥匙模具
│   └── Tag_SecurityCheck                     ← 安全检查
│
├── Trap/
│   ├── Tag_Trap
│   ├── Tag_BearTrap                          ← 捕兽夹
│   ├── Tag_TripMine                          ← 绊雷
│   ├── Tag_AlarmTrap                         ← 警报器
│   ├── Tag_GasTrap                           ← 毒气陷阱
│   ├── Tag_ShockTrap                         ← 电击陷阱
│   ├── Tag_RazorWire                         ← 铁丝网
│   ├── Tag_PitfallSpike                      ← 地刺
│   ├── Tag_OilSlick                          ← 油滑陷阱
│   ├── Tag_Tripwire                          ← 绊索
│   └── Tag_NoiseDecoy                        ← 噪音诱饵
│
└── Universal/
    ├── Tag_Universal
    ├── Tag_CombatRoll                         ← 翻滚闪避
    └── Tag_EmergencyReload                    ← 紧急换弹
```

### 15.2 新增 Tag 统计

| 根 | 新建资产数 | 说明 |
|----|-----------|------|
| `Noise.*` | **5** | 根+Combat+MeleeSwing/WeaponFire/Impact |
| `Stat.*` | **5** | 根+Vital+Stamina+Pool+Ammo |
| `Damage.Physical.Blunt` | **1** | 补充 |
| `Ability.*` | **~110** | 全量(含 Phase 2+ 预留), A测闭环需 12(含父节点) |
| **合计** | **~121** | |

> `Skill.*` 树不再作为运行时 Tag。`State.*` 树不需要。互斥由 `abilityTag.Parent` + `extraExclusionTags` 实现。

### 15.3 互斥层级示例

| 技能 | abilityTag | 默认互斥 Parent | 效果 |
|------|-----------|----------------|------|
| 刀·轻击 | `Ability.Melee.Blade.LightCut` | `Ability.Melee.Blade` | 阻止其他刀系技能 |
| 棍·重击 | `Ability.Melee.Staff.HeavySmash` | `Ability.Melee.Staff` | 阻止其他棍系技能 |
| 手枪·普通射击 | `Ability.Ranged.Pistol.NormalFire` | `Ability.Ranged.Pistol` | 阻止其他手枪技能 |
| 霰弹·普通射击 | `Ability.Ranged.Shotgun.NormalFire` | `Ability.Ranged.Shotgun` | 阻止其他霰弹技能 |
| 捕兽夹 | `Ability.Trap.BearTrap` | `Ability.Trap` | 阻止其他陷阱技能 |

> 可通过 `extraExclusionTags` 添加跨分类互斥。如霰弹枪破门技额外填 `[Ability.Melee]`，则近战动作期间也无法使用。

---

## 设计决策记录

| 决策 | 原因 |
|------|------|
| 医疗技能消耗物品催化剂 | 技能是能力，物品是物资——分开但联动 |
| 投掷物走物品栏消耗 | 投掷物是物理物品，不是"体力/弹药"能模拟的 |
| 防御盾牌技标注"需装备盾牌" | 盾牌是装备类型，与技能树交叉但不耦合 |
| 陷阱映射到AbilityDefSO走完整管道 | 陷阱=OnEnterArea被动→⑤→⑥→⑦→⑧，完全复用管道 |
| Trade以被动为主 | 交易核心是知识和关系网，不需要按键获得折扣 |
| 潜行不是"隐身" | 降低探测半径和噪音等级，不是魔法消失 |
| Q键=子系统基础技能 | 操作一致：切到哪个子系统Q都是最常用的基础技 |
| 154技能覆盖13标签 | 两个Agent独立设计后交叉验证，覆盖率充分 |
