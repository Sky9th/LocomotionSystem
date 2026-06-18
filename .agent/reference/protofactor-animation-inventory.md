# PROTOFACTOR Ultimate Animation Collection — 动画全量清单

> 来源: `I:\game\External\Assets\PROTOFACTOR\Ultimate Animation Collection\Animations`
> 整理日期: 2026-06-17
> 总计: 23 Animset, ~1200+ 动画

---

## 符号说明

| 标记 | 含义 |
|------|------|
| 🔵 | Unarmed — 无武器依赖，纯角色动画 |
| 🟢 | 武器包 — 持特定武器，Humanoid 可跨包复用 |
| ⭐ | HumanTransitions 直接可用 |
| △ | 近似替代 |
| ✗ | 缺失 |

---

## 1. Basic Locomotion Animset 🔵⭐

**路径**: `Animations/Basic Locomotion Animset/FBX Motions/`
**角色**: `Protof-Actor@BasicLocomotionAnimset.fbx`
**数量**: 87 动画 (不含 _RM)

### Walk (11)
- WalkForwardUnarmed2
- WalkForwardLeftUnarmed / WalkForwardRightUnarmed
- WalkBackwardsUnarmed / WalkBackwardsLeftUnarmed / WalkBackwardsRightUnarmed
- WalkLeftUnarmed / WalkRightUnarmed
- WalkUTurnLeftUnarmed / WalkUTurnRightUnarmed
- WalkForwardTurnLeftUnarmed / WalkForwardTurnRightUnarmed

### Run (11)
- RunForward2Unarmed
- RunForwardLeftUnarmed / RunForwardRightUnarmed
- RunBackwardsUnarmed / RunBackwardsLeftUnarmed / RunBackwardsRightUnarmed
- RunLeftUnarmed / RunRightUnarmed
- RunUTurnLeftUnarmed / RunUTurnRightUnarmed
- RunForwardTurnLeftUnarmed / RunForwardTurnRightUnarmed

### Sprint (3)
- RunFastForwardUnarmed
- RunFastTurnLeftUnarmed / RunFastTurnRightUnarmed
- SprintForwardLeftUnarmed / SprintForwardRightUnarmed

> ⚠️ **没有直线 SprintForward Unarmed**，仅有左右斜向 Sprint

### Turn (6)
- Turn90LeftUnarmed / Turn90RightUnarmed
- Turn180LeftUnarmed / Turn180RightUnarmed
- CrouchTurn90LeftUnarmed / CrouchTurn90RightUnarmed
- CrouchTurn180LeftUnarmed / CrouchTurn180RightUnarmed

### Crouch (16)
- CrouchForwardUnarmed / CrouchForwardLeftUnarmed / CrouchForwardRightUnarmed
- CrouchBackwardsUnarmed / CrouchBackwardsLeftUnarmed / CrouchBackwardsRightUnarmed
- CrouchLeftUnarmed / CrouchRightUnarmed

### Idle (14)
- IdleLookAroundScratchYawnUnarmed
- IdleAimGrenadeUnarmed
- CrouchIdleBreathe1Unarmed / CrouchIdleBreathe2Unarmed
- CrouchIdleLookAround1Unarmed / CrouchIdleLookAround2Unarmed
- IdleCrouchToTakeCoverCrouchingUnarmed
- IdleStandingToTakeCoverCrouchingUnarmed
- IdleTakeCoverCrouchingToIdleStandingUnarmed
- IdleTakeCoverStandingUnarmed / IdleUnderCoverCrouchingUnarmed
- IdlePeekLeftUnderCoverCrouchingUnarmed / IdlePeekLeftUnderCoverStandingUnarmed
- IdlePeekRightUnderCoverCrouchingUnarmed / IdlePeekRightUnderCoverStandingUnarmed
- IdlePeekLeftToIdleTakeCoverCrouchingUnarmed / IdlePeekLeftToIdleTakeCoverStandingUnarmed
- IdlePeekRightToIdleTakeCoverCrouchingUnarmed / IdlePeekRightToIdleTakeCoverStandingUnarmed

> ⚠️ **没有基础站立 Idle**，Idle 全是变体（蹲姿/掩体/手雷/张望）

### Jump / Air (3)
- WalkJumpToApexLeftFootUnarmed / WalkJumpToApexRightFootUnarmed
- RunJumpToApexLeftFootUnarmed / RunJumpToApexRightFootUnarmed
- LandingMediumUnarmed

### Throw (12)
- ThrowGrenadeUnarmed1 / ThrowGrenadeUnarmed2
- ThrowAimedGrenadeUnarmed
- CrouchThrowGrenadeUnarmed1 / CrouchThrowGrenadeUnarmed2
- GoToAimGrenadeUnarmed
- ThrowGrenadeLeftUnderCoverCrouching / ThrowGrenadeLeftUnderCoverStanding
- ThrowGrenadeRightUnderCoverCrouching / ThrowGrenadeRightUnderCoverStanding

### Cover (12)
- GoToTakeCoverStandingtUnarmed
- GoBackToCoverLeftCrouchingUnarmed / GoBackToCoverLeftStandingUnarmed
- GoBackToCoverRightCrouchingUnarmed / GoBackToCoverRightStandingUnarmed
- GoOutOfCoverLeftCrouchingUnarmed / GoOutOfCoverLeftStandingUnarmed
- GoOutOfCoverRightCrouchingUnarmed / GoOutOfCoverRightStandingUnarmed
- GoToPeekLeftUnderCoverCrouchingUnarmed / GoToPeekLeftUnderCoverStandingUnarmed
- GoToPeekRightUnderCoverCrouchingUnarmed / GoToPeekRightUnderCoverStandingUnarmed
- StrafeLeftTakeCoverCrouchingUnarmed / StrafeLeftTakeCoverStandingUnarmed
- StrafeRightTakeCoverCrouchingUnarmed / StrafeRightTakeCoverStandingUnarmed

---

## 2. Climbing Animset 🔵⭐

**路径**: `Animations/Climbing Animset/FBX Motions/`
**角色**: `Protof-Actor@ClimbingAnimset.fbx`
**数量**: 38 动画 (不含 _RM)

### Obstacle Climb (2)
- ClimbUpHalfMeterObstacleLeftUnarmed
- ClimbUpHalfMeterObstacleRightUnarmed

> ⚠️ **没有 1m / 2m 障碍攀爬 Unarmed**

### Wall Climb (14)
- EnterWallBottom / EnterWallTop
- ExitWallBottom / ExitWallTop
- ExitDropFromWall
- WallClimbUp / WallClimbUpLeft / WallClimbUpRight
- WallClimbDown / WallClimbDownLeft / WallClimbDownRight
- WallClimbLeft / WallClimbRight

### Wall Jump (5)
- WallJumpUp / WallJumpDown
- WallJumpLeft / WallJumpRight

### Jump / Fall (3)
- JumpToApex
- Falling / FallingToEnterWall

### Landing (3)
- LandingLight / LandingMedium / LandingHeavy

### Ladder (2)
- ClimbUpLadder / ClimbDownLadder

### Idle (6)
- IdleWallClimb
- IdleBreathe / IdleBreatheLadder2
- IdleLookAroundWallClimb
- IdlePrepareJumpOppositeWallLeft / IdlePrepareJumpOppositeWallRight
- IdlePrepareJumpOppositeWallLeftToIdleWallClimb / IdlePrepareJumpOppositeWallRightToIdleWallClimb

### Jump Opposite Wall (4)
- GoToPrepareJumpOppositeWallLeft / GoToPrepareJumpOppositeWallRight
- JumpOppositeWallLeft / JumpOppositeWallRight

---

## 3. 1Handed Melee Weapon Animset 🟢

**路径**: `Animations/1Handed Melee Weapon Animset/FBX Motions/`
**角色**: 单手近战武器
**数量**: 65 动画

### 高亮
- **IdleUnarmed** ← 整个包唯一的站立无武器 Idle
- Idle1hMelee / IdleCombat1hMelee / IdleLookAround1hMelee / IdleBlock1hMelee
- SprintHold1hMelee
- Falling1hMelee / JumpToApex1hMelee
- LandingLight1hMelee / LandingMedium1hMelee / LandingHeavy1hMelee
- 完整 Walk/Run/Crouch 移动
- 7 种 Attack 变体, 3 种 Combo, Draw/PutBack
- Death/Dead/GetHit/GetBackUp

---

## 4. 2Handed Gun Animset 🟢

**路径**: `Animations/2Handed Gun Animset/FBX Motions/`
**数量**: 97 动画

### 高亮
- **IdleUnarmed** ← 另一份无武器 Idle
- **SprintForward2HandedGun** ← 直线冲刺（持枪）
- **ClimbUp1MeterObstacle2HandedGun / ClimbUp2MetersObstacle2HandedGun**
- **Pass1MeterObstacleLeft/Right2HandedGun**
- ClimbUpHalfMeterObstacleLeft/Right2HandedGun
- 完整掩体系统 (TakeCover/Peek/GoTo/GoOut/Strafe)
- 手雷系统 (Aim/Throw)
- Dodge 四方向, MeleeAttack, Reload
- Crouch/Run/Walk 全部持枪姿态

---

## 5. 2Handed Melee Weapon Animset 🟢

**路径**: `Animations/2Handed Melee Weapon Animset/FBX Files/`
**数量**: 81 动画

### 高亮
- **IdleUnarmed** ← 第三份无武器 Idle
- IdleCombatA/B/C2HandMelee / IdleLookAround2HandMelee / IdleBlock2HandMelee
- IdleBreathe2HandMelee / IdleCrouchBreathe2HandMelee / IdleCrouchLookAround2HandMelee
- Sprint2HandMelee ← 直线冲刺（双手近战）
- 8 种 Attack 变体 (A-G), Combo
- 完整 Walk/Run/Crouch

---

## 6. Assault Rifle Animset 🟢

**路径**: `Animations/Assault Rifle Animset/FBX motions/`
**数量**: 87 动画

### 高亮
- **SprintForwardAssaultRifle**
- **ClimbUp1MeterObstacleAssaultRifle / ClimbUp2MetersObstacleAssaultRifle**
- **Pass1MeterObstacleLeft/RightAssaultRifle**
- ClimbUpHalfMeterObstacleLeft/RightAssaultRifle
- 完整掩体/手雷/Dodge/Melee 系统
- CrouchShootPrimary/Secondary

---

## 7. Combat Bare Fists Animset 🟢🔵

**路径**: `Animations/Combat Bare Fists Animset/FBX Motions/`
**数量**: 66 动画

### 高亮
- **IdleCombat** ← 格斗站姿
- IdleBlockHigh / IdleBlockMedium
- 丰富的拳击/踢击组合: Jab, Hook, Uppercut, FrontKick, SpinningKick, Knee
- 多种 Combo (2-hit ~ 5-hit)
- Dodge 四方向 (Combat)
- Taunt 1-5
- Stunned
- Walk/Run 全部 Combat 姿态

---

## 8. Crowd Animset 🔵

**路径**: `Animations/Crowd Animset/FBX Motions/`
**数量**: 144 动画

### 高亮
- **Idle1 ~ Idle7** — 多种站立 Idle 变体
- **Idle1LookLeft / Idle1LookRight** ← IdleL/R 候选
- Idle1Agree/Argue/Explain/Laugh/Surprised 等对话动画
- IdleSit1 ~ IdleSit14 — 丰富的坐姿 Idle
- IdleSitPhone 系列 — 打电话
- Cheer1 ~ Cheer15 — 欢呼
- Walk1 ~ Walk22 — 各种行走变体

---

## 9. Bazooka Animset 🟢

**路径**: `Animations/Bazooka Animset/FBX Motions/`
**数量**: 87 动画

### 高亮
- **SprintForwardBazooka**
- **ClimbUp1MeterObstacleBazooka / ClimbUp2MetersObstacleBazooka**
- **Pass1MeterObstacleLeft/RightBazooka**
- ClimbUpHalfMeterObstacleLeft/RightBazooka
- 完整掩体/手雷/Dodge/Melee 系统

---

## 10. Shotgun Animset 🟢

**路径**: `Animations/Shotgun Animset/FBX Motions/`
**数量**: 95 动画

### 高亮
- **SprintForwardShotgun**
- **ClimbUp1MeterObstacleShotgun / ClimbUp2MetersObstacleShotgun**
- **Pass1MeterObstacleLeft/RightShotgun**
- ClimbUpHalfMeterObstacleLeft/RightShotgun
- ReloadShotgun, ShootCrouchingShotgun

---

## 11. Minigun Animset 🟢

**路径**: `Animations/Minigun Animset/FBX Motions/`
**数量**: 97 动画

### 高亮
- **SprintForwardMinigun**
- **ClimbUp1MeterObstacleMinigun / ClimbUp2MetersObstacleMinigun**
- **Pass1MeterObstacleLeft/RightMinigun**
- OverheatingMinigun
- ReloadMinigun

---

## 12. Double Guns Animset 🟢

**路径**: `Animations/Double Guns Animset/FBX Motions/`
**数量**: 97 动画

### 高亮
- **SprintForward2Guns**
- **ClimbUp1MeterObstacle2Guns / ClimbUp2MetersObstacle2Guns**
- **Pass1MeterObstacleLeft/Right2Guns**
- ReloadFast2Guns / ReloadNormal2Guns
- ShootPrimary/Secondary2Guns

---

## 13. Dual Swords Animset 🟢

**路径**: `Animations/Dual Swords Animset/FBX Files/`
**数量**: 101 动画

### 高亮
- IdleCombatDualSwords / IdleLookAroundDualSwords / IdleBreatheDualSwords
- 18 Attack 变体, 多种 Combo (2~5 Hit)
- SpinAttack, Parry 四方向
- Draw/PutBack
- JogDualSwords / RunFastDualSwords
- Taunt 1-5, WarmUp 1-2

---

## 14. Fencing Animset 🟢

**路径**: `Animations/Fencing Animset/FBX Files/`
**数量**: 73 动画

### 高亮
- IdleCombatFencing / IdleLookAroundFencing / IdleCrouchFencing / IdleBreatheFencing
- 14 Attack 变体（含 Leap 突刺）, Combo
- Parry 四方向
- DrawSword/PutBackSword
- 完整 Walk/Run/Crouch

---

## 15. Sword & Shield Animset 🟢

**路径**: `Animations/Sword&Shield Animset/FBX Motions/`
**数量**: 80 动画

### 高亮
- IdleCombatS&S / IdleGetReadyS&S / IdleBreatheS&S / IdleLookAroundS&S
- SwordAttack 1-3, ShieldAttack 1-3
- 多种 Combo, Parry 四方向, Block 三级
- FrontKick 1-2
- RunFastNormalS&S / RunNormalS&S
- Taunt 1-3

---

## 16. Bow & Arrow Animset 🟢

**路径**: `Animations/Bow & Arrow Animset/FBX Motions/`
**数量**: 109 动画

### 特色
- 完整弓姿态系统: BowLoaded / BowAiming / NoBow 三种状态
- IdleBreathe/IdleLookAround × 三种状态
- SprintBow
- DrawArrow/DrawBow, ShootArrow 全流程
- CrouchShootArrowAllInOne
- Death/Dead/GetHit × 三种状态

---

## 17. Wizard Animset 🟢

**路径**: `Animations/Wizard Animset/FBX Files/`
**数量**: 66 动画

### 高亮
- IdleWizard / IdleLookAroundWizard / CrouchIdleWizard
- CastSpell 1-14
- SprintWizard / RunFastWizard
- DodgeLeft/Right

---

## 18. Creature Animset 🟢

**路径**: `Animations/Creature Animset/FBX Motions/`
**数量**: 37 动画

### 特色
- 怪物/野兽动画: Attack, Roar, Death, GetHit
- IdleBreatheCreature / IdleLookAroundCreature
- Walk/Run 全部 Creature 姿态
- Turn180/Turn90
- Jump 四方向

---

## 19. Zombie Animset 🟢

**路径**: `Animations/Zombie Animset/FBX Files/`
**数量**: 33 动画

### 特色
- 僵尸姿态: IdleZombieA-D, WalkZombieA-I
- Attack, BiteAttack, 3HitCombo
- Feast, Death
- CrawlZombie, StrafeLeft/RightZombie
- KneelZombieA/B, IdleKneelZombie

---

## 20. Hostage Animset 🟢

**路径**: `Animations/Hostage Animset/FBX motions/`
**数量**: 31 动画

### 特色
- 人质姿态: HandsOnHead / HandsUp / Scared
- IdleBreatheHandsOnHead / IdleBreatheHandsUp
- IdleKneelHandsOnHead / IdleKneelHandsUp
- IdleSitOntheGroundHostage
- KneelExecuted, Death × 两种姿态
- WalkForward/Backwards × 三种姿态

---

## 21. Injured Animset 🔵

**路径**: `Animations/Injured Animset/FBX Files/`
**数量**: 56 动画

### 特色
- 受伤姿态 A-G (7种风格)
- IdleInjured, IdleKneelInjured, IdleSitInjured
- GoToKneel, GoToSit, SitToIdle
- WalkInjured, RunInjured

---

## 22. Campfire Animset 🔵

**路径**: `Animations/Campfire Animset/FBX Files/`
**数量**: 26 动画

### 特色
- 营地互动: 跪/坐/躺/睡 多种 Idle
- 烧烤、投木柴、点火（打火机/火柴/打火石/木棍）
- Stand↔Kneel↔Sit↔Lay↔Sleep 过渡

---

## 23. Push & Pull Cube Animset 🔵

**路径**: `Animations/Push&Pull Cube Animset/FBX Motions/`
**数量**: 13 动画

### 特色
- PushCube 1-3, PullCube
- PushCubeIdle 1-3
- 推拉八个方向

---

## HumanTransitions 对照总结

| 槽位 | 匹配 | 来源 | 状态 |
|------|------|------|------|
| WalkMixer | WalkForwardUnarmed2 | Basic Locomotion | ✅ |
| RunMIxer | RunForward2Unarmed | Basic Locomotion | ✅ |
| Sprint | RunFastForwardUnarmed | Basic Locomotion | △ RunFast≠Sprint姿态 |
| TurnInWalk180L/R | WalkUTurnLeft/RightUnarmed | Basic Locomotion | ✅ |
| TurnInRun180L/R | RunUTurnLeft/RightUnarmed | Basic Locomotion | ✅ |
| TurnInSprint180L/R | RunFastTurnLeft/RightUnarmed | Basic Locomotion | △ |
| TurnInPlaceL90/R90 | Turn90Left/RightUnarmed | Basic Locomotion | ✅ |
| Idle | IdleUnarmed | 1Handed/2HandedGun/2HandedMelee | ✅ 武器包 |
| IdleL/R | Idle1LookLeft/Right | Crowd | △ 不同姿态 |
| IdleToRun180L/R | Turn180Left/RightUnarmed | Basic Locomotion | △ 非过渡动画 |
| LookMixer | IdleLookAroundScratchYawnUnarmed | Basic Locomotion | ✅ |
| AirLoop | Falling | Climbing | ✅ |
| LandLight | LandingLight | Climbing | ✅ |
| LandMedium | LandingMedium（Basic Loco / Climbing） | ✅ | |
| LandHard | LandingHeavy | Climbing | ✅ |
| LandFromWall | ExitDropFromWall | Climbing | ✅ |
| ClimbUpHalfMeter | ClimbUpHalfMeterObstacleLeftUnarmed | Climbing | ✅ |
| **ClimbUp1meter** | ClimbUp1MeterObstacle2HandedGun 等 | 武器包 | △ 非Unarmed |
| **ClimbUp2meter** | WallClimbUp | Climbing | △ 垂直墙≠障碍翻越 |
| **SprintForward** | SprintForwardAssaultRifle 等 | 武器包 | ✗ 无Unarmed版 |

### 真正缺失（无任何替代）

| 数量 | 描述 |
|------|------|
| 1 | **SprintForward Unarmed** — 所有武器包都有直线冲刺，Unarmed 没有 |
| 1 | **站立基础 Idle Unarmed** — 在武器包里，Basic Locomotion 不自带 |
| 2 | **1m/2m 障碍攀爬 Unarmed** — 武器包全部有，Climbing 只有 HalfMeter 和 Wall |
| 1 | **IdleToRun 过渡** — 整个包无此类型 |

### 武器包可填补但非 Unarmed 的动画

所有武器包的 Humanoid 动画与 Unarmed 共享骨骼，攀爬/冲刺时手臂持武器对躯干运动影响很小。可选用最轻量的武器包（如 AssaultRifle 或 2HandedGun）来填补 1m/2m 攀爬和直线冲刺。

---

## 推荐复制到项目的 Animset

1. **Basic Locomotion Animset** — 核心移动 (Walk/Run/Turn/Crouch)
2. **Climbing Animset** — 攀爬/下落/着陆
3. **Combat Bare Fists Animset** — 徒手战斗 ⭐
4. **1Handed Melee Weapon Animset** — 单手近战 + IdleUnarmed
5. **Zombie Animset** — 敌人动画
6. **Injured Animset** — 受伤状态
7. **Crowd Animset** — NPC 社交/Idle 变体
8. （可选）Assault Rifle / Shotgun — 枪械战斗
