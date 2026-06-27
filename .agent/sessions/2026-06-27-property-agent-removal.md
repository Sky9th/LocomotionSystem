# 2026-06-27 — PropertyAgent 删除 + AbilityForest 搬家

## Background

v0.25.3 Code Review 暴露了 PropertyAgent 的冗余：它是一个 MonoBehaviour 壳，包裹着纯 C# 的 PropertyTable。壳的三个功能（Inspector 序列化、GetComponent 发现、Update Tick）都可以由 CharacterActor 直接承担。

更根本的是——ItemInstance 即将创建，它没有 GameObject，必须直接用 PropertyTable。如果 PropertyAgent 还存在，就会有两套属性访问路径（CharacterActor 用 PropertyAgent → PropertyTable，ItemInstance 用 PropertyTable）。统一路径成为前置条件。

同时发现 AbilityForest（纯 C# 技能运行时）放在 `L3_Character/Ability/` 是错误的——它没有任何 Character 依赖，namespace 却是 `RedDust.Character.Ability`。Vito 指出应搬到 `L3_Ability/`。

## Changes

### PropertyAgent 删除
- 删除 `PropertyAgent.cs`（101 行 MB 壳）
- 删除 `IPropertyReader.cs`（读-only 接口，PropertyTable 直接暴露后无必要）
- `CharacterActor` 新增 `[SerializeField] PropertyPresetSO propertyPreset`，`Properties` 公开为 `PropertyTable`
- `CharacterActor.Update` 末尾调用 `Properties.Tick(dt)`
- `[RequireComponent(typeof(PropertyAgent))]` 移除
- 写权限暴露标记为已知风险

### Consumer 全链更新
- `CharacterBuildContext`: `PropertyAgent` → `PropertyTable`
- `CharacterCombat`: `ctx.PropertyAgent` → `ctx.Properties`（6 处）
- `CharacterContainer`: `ctx.PropertyAgent` → `ctx.Properties`；`.name` → `ctx.Root.name`
- `CharacterPhysique`: `FromAgent(IPropertyReader)` → `From(PropertyTable)`
- `UIService/VitalsOverlay`: `IPropertyReader` → `PropertyTable`
- `AbilityExecutor`: `GetComponent<PropertyAgent>()` 注释 + TODO

### AbilityForest 搬家
- 文件: `L3_Character/Ability/AbilityForest.cs` → `L3_Ability/AbilityForest.cs`
- namespace: `RedDust.Character.Ability` → `RedDust.Ability`
- `CharacterActor`/`CharacterBuildContext`: 删除 `using RedDust.Character.Ability`

### 注释更新
- `Stance.cs`, `BuffEffectSO.cs`, `CharacterBuildContext.cs`, `CharacterPhysique.cs` — PropertyAgent 引用 → PropertyTable

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| 删除 IPropertyReader，不新建 IPropertyHost | A: 新建 IPropertyHost 接口替代 PropertyAgent 的 GetComponent 发现 | YAGNI——当前仅 CharacterActor 有属性。多实体时再抽 |
| Tick 放 Update 末尾 try-catch 外 | A: 放 try-catch 内 → Tick 异常会 disable 整个 Actor | Tick 错误不应影响 locomotion/animation 管线 |
| Awake 中 Properties==null 不 return | A: return 退出 → Start 中 buildCtx 为 null → NRE | 去掉 return，Start 加 null 守卫 |
| AbilityForest namespace 改 RedDust.Ability | A: 保留 RedDust.Character.Ability | 类本身无 Character 依赖，文档早已放在 L3-ability |

## Known Issues

- [ ] AbilityExecutor Buff FloatAdjunct 注入已注释（P2 — AbilityExecutor 重构时恢复）
- [ ] `.agent/` 内约 23 个文档仍引用 PropertyAgent（P3 — 后续批量更新）
- [ ] CharacterActor.propertyPreset 需在 Unity Editor 中手动赋值（已迁移 Player.prefab 和 NPC.prefab）

## Cross-References

### Related Sessions
- [2026-06-27-code-review-cleanup.md](2026-06-27-code-review-cleanup.md) — 同日前置 Code Review，暴露了 PropertyAgent 问题
- [2026-06-27-container-character-slots.md](2026-06-27-container-character-slots.md) — 同日 Container + 身体槽位落地

### Related Tech Docs
- [tech/.../L3-ability/ability-forest.md](../tech/L2-services/L2-modules/L3-ability/ability-forest.md) — 更新代码路径 + namespace

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactoring, no design-facing changes.
