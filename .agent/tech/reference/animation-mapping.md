# 动画映射表 — Pistol / Knife

> 2026-06-19 · 源: PROTOFACTOR Ultimate Animation Collection
> 资产路径: `Assets/Art/Animations/PROTOFACTOR/`

## 架构

- **Base Layer**: 已有 Mocap Basic 动画包提供全身 locomotion (Walk/Run/Sprint/Crouch/Turn/Jump/Land)
- **UpperBody Layer**: 武器 Idle 覆盖上衣层——当武器包缺失某方向动画时，用 Mocap Base + 武器 Idle 叠加
- `← Base` = 武器包缺失，用 Mocap Base + 武器 Idle 作为 UpperBody 覆盖

| RedDust 槽位 | Pistol | Knife |
|---------|--------|-------|
| `idleL` | `IdleHold2HandedGun` | `IdleCombat1hMelee` |
| Walk Fwd | `WalkForward2HandedGun_RM` | `WalkForwardCombat1hMelee_RM` |
| Walk Back | ← Base | `WalkBackwardsCombat1hMelee_RM` |
| Walk Left | ← Base | `WalkLeftCombat1hMelee_RM` |
| Walk Right | ← Base | `WalkRightCombat1hMelee_RM` |
| Run Fwd | `RunForward2HandedGun_RM` | `RunForward1hMelee_RM` |
| Run Back | ← Base | `RunBackwards1hMelee_RM` |
| Run Left | ← Base | `RunLeft1hMelee_RM` |
| Run Right | ← Base | `RunRight1hMelee_RM` |
| `sprint` | `SprintForward2HandedGun_RM` | `SprintHold1hMelee_RM` |
| `turn90L` | ← Base | ← Base |
| `turn90R` | ← Base | ← Base |
| `airLight` | `JumpToApex2HandedGun` | `JumpToApex1hMelee` |
| `airHard` | `Falling2HandedGun` | `Falling1hMelee` |
| `landLight` | `LandingLight2HandedGun` | `LandingLight1hMelee` |
| `landHard` | `LandingHeavy2HandedGun` | `LandingHeavy1hMelee` |
| Crouch Fwd | `CrouchForward2HandedGun_RM` | `CrouchForward1hMelee_RM` |
| Crouch Back | `CrouchBackwards2HandedGun_RM` | `CrouchBackwards1hMelee_RM` |
| Crouch Left | ← Base | `CrouchLeft1hMelee_RM` |
| Crouch Right | ← Base | `CrouchRight1hMelee_RM` |
| Crouch Idle | `CrouchIdle2HandedGun` | `CrouchIdle1hMelee` |

## 资产清单

### Pistol (14)
`Assets/Art/Animations/PROTOFACTOR/Pistol/`
- `IdleHold2HandedGun`, `IdleLookAroundHold2HandedGun`
- `WalkForward2HandedGun_RM`
- `RunForward2HandedGun_RM`
- `SprintForward2HandedGun_RM`
- `CrouchIdle2HandedGun`, `CrouchIdleLookAround2HandedGun`
- `CrouchForward2HandedGun_RM`, `CrouchBackwards2HandedGun_RM`
- `JumpToApex2HandedGun`, `Falling2HandedGun`
- `LandingLight2HandedGun`, `LandingHeavy2HandedGun`, `LandingMedium2HandedGun`

### Knife (22)
`Assets/Art/Animations/PROTOFACTOR/Knife/`
- `IdleCombat1hMelee`, `Idle1hMelee`, `IdleLookAround1hMelee`
- `WalkForwardCombat1hMelee_RM`, `WalkBackwardsCombat1hMelee_RM`, `WalkLeftCombat1hMelee_RM`, `WalkRightCombat1hMelee_RM`
- `RunForward1hMelee_RM`, `RunBackwards1hMelee_RM`, `RunLeft1hMelee_RM`, `RunRight1hMelee_RM`
- `SprintHold1hMelee_RM`
- `CrouchIdle1hMelee`
- `CrouchForward1hMelee_RM`, `CrouchBackwards1hMelee_RM`, `CrouchLeft1hMelee_RM`, `CrouchRight1hMelee_RM`
- `JumpToApex1hMelee`, `Falling1hMelee`
- `LandingLight1hMelee`, `LandingHeavy1hMelee`, `LandingMedium1hMelee`
