# SO 资产与菜单整理

> 独立任务 · 可随时执行 · 涉及 12 个 .cs 修改 + ~10 个 .asset 移动

## 背景

项目开发初期未规划 SO 资产目录，导致两个问题：

1. **CreateAssetMenu 菜单不统一** — 输入事件在 `Events/` 根下，其余在 `RedDust/`，子系统深度不一致
2. **资产目录结构扁平** — `Assets/Data/Character/` 下 6 个 SO 全摊在一起，与菜单层级不匹配

## 目标一：统一 CreateAssetMenu 菜单层级

全部归入 `RedDust/` 根，按子系统层级组织。

| 类名 | 当前 menuName | 目标 menuName | 文件路径 |
|------|--------------|--------------|----------|
| `GameProfile` | `RedDust/Core/Game Profile` | `RedDust/Core/Game Profile` | `L1_Core/GameProfile.cs` |
| `GameplayTagDefinitionSO` | `RedDust/GameplayTag` | `RedDust/GameplayTag` | `L1_Core/GameplayTag/GameplayTagDefinitionSO.cs` |
| `CharacterProfile` | `RedDust/Character/Character Profile` | `RedDust/Character/Character Profile` | `L3_Character/Config/CharacterProfile.cs` |
| `LocomotionProfile` | `RedDust/Character/Locomotion Profile` | `RedDust/Character/Locomotion Profile` | `L3_Character/L4_Locomotion/Config/LocomotionProfile.cs` |
| `AnimationAliasProfile` | `RedDust/Character/Animation Alias Profile` | `RedDust/Character/Animation/Animation Alias Profile` | `L3_Character/L4_Animation/Config/AnimationAliasProfile.cs` |
| `LocomotionAnimationProfile` | `RedDust/Character/Animation/Locomotion Animation Profile` | `RedDust/Character/Animation/Locomotion Animation Profile` | `L3_Character/L4_Animation/Config/LocomotionAnimationProfile.cs` |
| `LocomotionModeProfile` | `RedDust/Character/Animation/Locomotion Mode Profile` | `RedDust/Character/Animation/Locomotion Mode Profile` | `L3_Character/L4_Animation/Config/LocomotionModeProfile.cs` |
| `CharacterAudioConfigSO` | `RedDust/Character/Audio Config` | `RedDust/Character/Audio/Audio Config` | `L3_Character/L4_Audio/Config/CharacterAudioConfigSO.cs` |
| `FootstepSetSO` | `RedDust/Character/Audio/Footstep Set` | `RedDust/Character/Audio/Footstep Set` | `L3_Character/L4_Audio/Config/FootstepSetSO.cs` |
| `StatDefSO` | `RedDust/Stats/Stat Definition` | `RedDust/Stats/Stat Definition` | `L3_Stats/Definition/StatDefSO.cs` |
| `StatsTreeSO` | `RedDust/Stats/Stats Tree` | `RedDust/Stats/Stats Tree` | `L3_Stats/Tree/StatsTreeSO.cs` |
| `StatsNodeSO` | `RedDust/Stats/Stats Node` | `RedDust/Stats/Stats Node` | `L3_Stats/Tree/StatsNodeSO.cs` |
| `UIThemeSO` | `RedDust/UI/Theme` | `RedDust/UI/Theme` | `L2_UI/Config/UIThemeSO.cs` |
| `UIPanelConfigSO` | `RedDust/UI/Panel Config` | `RedDust/UI/Panel Config` | `L2_UI/Config/UIPanelConfigSO.cs` |
| `CrouchInputEvent` | `Events/Input/Crouch Event` | `RedDust/Input/Crouch Event` | `L2_Input/Events/CrouchInputEvent.cs` |
| `ProneInputEvent` | `Events/Input/Prone Event` | `RedDust/Input/Prone Event` | `L2_Input/Events/ProneInputEvent.cs` |
| `StandInputEvent` | `Events/Input/Stand Event` | `RedDust/Input/Stand Event` | `L2_Input/Events/StandInputEvent.cs` |
| `SprintInputEvent` | `Events/Input/Sprint Event` | `RedDust/Input/Sprint Event` | `L2_Input/Events/SprintInputEvent.cs` |
| `PrimaryInteractEvent` | `Events/Input/Primary Interact Event` | `RedDust/Input/Primary Interact Event` | `L2_Input/Events/PrimaryInteractEvent.cs` |
| `SecondaryInteractEvent` | `Events/Input/Secondary Interact Event` | `RedDust/Input/Secondary Interact Event` | `L2_Input/Events/SecondaryInteractEvent.cs` |

### 额外修复

- `AnimationAliasProfile` 缺少 `fileName` 参数 → 加上 `fileName = "AnimationAliasProfile"`

## 目标二：资产目录重组

移动现有 `.asset` 文件，使目录结构匹配菜单层级。以下列出所有需要移动的项目自定义 SO 资产。

### 当前 → 目标映射

| 当前路径 | 目标路径 |
|---------|---------|
| `Assets/Data/Character/AnimationAliasProfile.asset` | `Assets/Data/Character/Animation/AnimationAliasProfile.asset` |
| `Assets/Data/Character/LocomotionAnimationProfile.asset` | `Assets/Data/Character/Animation/LocomotionAnimationProfile.asset` |
| `Assets/Data/Character/LocomotionProfile.asset` | `Assets/Data/Character/LocomotionProfile.asset` |
| `Assets/Data/Character/CharacterProfile.asset` | `Assets/Data/Character/CharacterProfile.asset` |
| `Assets/Data/Character/CharacterAudioConfig.asset` | `Assets/Data/Character/Audio/CharacterAudioConfig.asset` |
| `Assets/Data/Character/FootstepSet.asset` | `Assets/Data/Character/Audio/FootstepSet.asset` |
| `Assets/Data/Character/ModeProfiles/` (3 files) | `Assets/Data/Character/Animation/ModeProfiles/` |
| `Assets/Data/Events/InputRelay.asset` | `Assets/Data/Input/InputRelay.asset` |
| `Assets/Data/Events/Input/PrimaryInteractEvent.asset` | `Assets/Data/Input/Events/PrimaryInteractEvent.asset` |
| `Assets/Data/Events/Input/SecondaryInteractEvent.asset` | `Assets/Data/Input/Events/SecondaryInteractEvent.asset` |
| `Assets/Data/Events/Input/SprintEvent.asset` | `Assets/Data/Input/Events/SprintEvent.asset` |

### 创建缺失资产

以下输入事件类型已定义但 `.asset` 不存在，需在 `Assets/Data/Input/Events/` 下创建：

| 类名 | 文件名 |
|------|--------|
| `CrouchInputEvent` | `CrouchEvent.asset` |
| `ProneInputEvent` | `ProneEvent.asset` |
| `StandInputEvent` | `StandEvent.asset` |

### 不动的目录

| 目录 | 原因 |
|------|------|
| `Assets/Data/Animancer/` | 第三方资产（Animancer StringAsset），非项目 SO |
| `Assets/Data/Stats/` | 已组织良好（Defs/Nodes/Trees 三目录） |
| `Assets/Data/UI/` | 仅 2 个文件，无需子目录 |
| `Assets/Data/InputAction/` | Unity InputActionAsset，非项目 SO |
| `Assets/Settings/` | 引擎/插件设置，不归项目管理 |
| `Assets/Resources/` | DOTween + Astar 设置 |

### 目标完整目录树

```
Assets/Data/
├── Core/
│   └── GameProfile.asset
├── GameplayTags/                          ← 预留，Phase 5 创建 Tag SO
├── Character/
│   ├── CharacterProfile.asset
│   ├── LocomotionProfile.asset
│   ├── Animation/
│   │   ├── AnimationAliasProfile.asset
│   │   ├── LocomotionAnimationProfile.asset
│   │   └── ModeProfiles/
│   │       ├── Stand_Run.asset
│   │       ├── Stand_Sprint.asset
│   │       └── Stand_Walk.asset
│   └── Audio/
│       ├── CharacterAudioConfig.asset
│       └── FootstepSet.asset
├── Input/
│   ├── InputRelay.asset
│   └── Events/
│       ├── PrimaryInteractEvent.asset
│       ├── SecondaryInteractEvent.asset
│       ├── SprintEvent.asset
│       ├── CrouchEvent.asset              ← 新建
│       ├── ProneEvent.asset               ← 新建
│       └── StandEvent.asset               ← 新建
├── InputAction/
│   ├── System Time Resume.asset
│   ├── System Time Slow.asset
│   ├── UI Escape Action.asset
│   └── Control/ (10 个)
├── Stats/
│   ├── Defs/ (10 个 StatDef_*.asset)
│   ├── Nodes/ (7 个)
│   └── Trees/ (2 个)
├── UI/
│   ├── PanelConfig.asset
│   └── UITheme.asset
└── Animancer/
    ├── HumanTransitions.asset
    ├── HumanWalkSet.asset
    ├── Clips/ (38 个)
    └── Parameters/ (4 个)
```

## 执行步骤

### Step 1: 修改 CreateAssetMenu（12 个 .cs 文件）

按上表修改 `[CreateAssetMenu]` 的 `menuName`，`AnimationAliasProfile` 加上 `fileName`。全部用 **Edit** 逐文件精确替换。

### Step 2: 移动资产文件（Bash mv）

按映射表逐批移动。Unity 通过 GUID 追踪引用，移动 `.asset` + `.meta` 不会断引用。每批移动后在 Unity 中 Refresh 验证无 missing reference。

### Step 3: 创建缺失的输入事件资产

在 Unity 编辑器中右键 `Assets/Data/Input/Events/` → Create → `RedDust/Input/Crouch Event`，生成 `CrouchEvent.asset`。同样创建 Prone 和 Stand。

### Step 4: 清理空目录

移动完成后删除空目录：
- `Assets/Data/Character/ModeProfiles/` → 已移走
- `Assets/Data/Events/` → 已清空

### Step 5: 验证

- `Player.prefab` 无 missing reference
- Unity Console 无 GUID 相关 Warning
- Create Asset 右键菜单展示统一 `RedDust/` 层级
- 新建 SO 生成在对应子目录

## 风险

- **零风险**：Unity 资产引用基于 GUID（`.meta` 文件），移动同时移动 `.meta`，引用不丢
- 唯一注意：移动时务必同时移动 `.asset` 和 `.asset.meta`，不要只移一个
