# 2026-06-15-character-config-migration

## 背景

Player.prefab 上分散在多个组件（AnimationBrain、LocomotionDriver、TraversalDriver、CharacterAudio）上的配置资产集中在 CharacterActor 统一管理。AnimationBrain 保留在 Model 子节点（因为 OnAnimatorMove 需求），但移除所有序列化字段，改为运行时从 CharacterActor 读取配置。

## 改动内容

### Character 模块配置集中

- AnimationBrain 11 个序列化字段移除，改为 `GetComponentInParent<CharacterActor>()` 读取
- LocomotionDriver 的 `aliasProfile`/`animationProfile`/`locomotionProfile` 移除
- TraversalDriver 的 `aliasProfile` 移除，`_aliasProfile` 在 OnEnable 缓存
- CharacterAudio 的 `config` 移除
- CharacterActor 新增：`modelPrefab`、`animationAliasProfile`、3 个 root motion bool、5 个 AvatarMask、`characterAudioConfig`
- CharacterActor.Awake() 新增模型运行时实例化逻辑

### Player.prefab 结构优化

最终结构：
```
Player (根)
├── 所有逻辑 MB（CharacterActor/AnimationBrain 除外）
├── LocomotionDriver / TraversalDriver
└── Model (运行时实例化)
    ├── Animator + NamedAnimancerComponent + SkinnedMeshRenderer
    └── AnimationBrain (纯运行时，零序列化字段)
```

### Bug 修复

- **EventHub 时序竞争**：`RegisterListener` 加 `isActiveAndEnabled` 检查，立即调 `BindEvents`
- **PlayerInput.BindEvents**：所有 `Get<>().Register()` 加 `?.` null-safe
- **BaseCharacterAnimationDriver**：`GetComponent` 改回 `GetComponentInChildren`（AnimationBrain 在子节点）
- **AbilityEditorWindow**：树选中加 `Repaint()`

### Ability Editor 改进

- 导入目录路由：从 abilityTag 层级推导（`Ability.Melee.Blade.LightCut` → `Abilities/Actives/Melee/Blade/`）
- 统一根目录 `Assets/Data/Ability/Abilities/`
- SubAssetPickerView Effect 树去掉 EditorCard 包裹

### 资产更新

- PolygonApocalypse 角色（Soldier_Male_01、Zombie_Male_01）和武器（Katana_01、Pistol_01）拷贝到 `Assets/Art/PolygonApocalypse/`
- 旧的 Search/Ability .asset 清理，JSON 补全

## 已知问题

- Character 模块缺乏显式生命周期管理——初始化顺序依赖 Unity 隐式调用（已标记 TODO）
- `modelPrefab` 需要干净的单个角色 Prefab（当前 Soldier_Male_01 是 30 角色的全家桶）
- PropertyAgent._def 未迁移——L2 基础服务保持独立是正确决策
