# 2026-06-27 — Code Review 整理：命名 / 访问修饰符 / 生命周期 / 错误处理

## Background

v0.25.2 落地 L3_Container + 角色身体槽位后，对 CharacterActor 周边代码做了一次逐行审查。Vito 逐行质疑了多个设计选择：访问修饰符虚高、命名模糊（`agent`）、`CharacterPhysique.FromAgent()` 的 Awake 竞态、`GetFloat` 静默返回 0f、`public IPropertyReader Properties` 被 L2 直读违反架构原则。本 session 逐一修正。

## Changes

### 命名消歧义
- CharacterActor: `private PropertyAgent agent` → `private PropertyAgent propertyAgent`
- CharacterBuildContext: `public PropertyAgent Agent` → `public PropertyAgent PropertyAgent`，构造函数参数同步改名
- CharacterContainer: `ctx.Agent` → `ctx.PropertyAgent`
- CharacterCombat: `ctx.Agent` → `ctx.PropertyAgent`

### 访问修饰符降级
- `internal CharacterPhysique Physique { get; private set; }` → `private CharacterPhysique physique`（全走 BuildContext，无外部消费者）
- `internal CharacterContainer Container { get; private set; }` → `private CharacterContainer container`（零消费者，下阶段接入时升级）

### 生命周期修正
- `CharacterPhysique.FromAgent()` 从 Awake 移至 Start：与 CharacterContainer 同一原则——所有 Awake 完成后才读跨组件数据
- BuildContext.Physique 改为 `{ get; internal set; }`，支撑 Start 阶段延迟赋值

### 错误处理强化
- PropertyTable `WarnPath` → `ErrorPath`：路径不存在由 `Debug.LogWarning` 改为 `Debug.LogError`
- `GetFloat/GetInt/GetBool/GetString/GetTagList/GetAsset/GetMin/GetMax` 全部路径缺失时抛错，替代静默返回默认值
- 修正：`ErrorPath` 前先查 `_structure.ContainsKey`——路径在结构中但值未写入（表构建期 DoWrite 取旧值）不报错

### 架构债标记（TODO）
- `public IPropertyReader Properties`：标记 L2→L3 反查，待属性事件/快照到位后切除
- `AddModifier("Vitals/Hunger")`：标记 Actor 内联业务逻辑，待 CharacterAttributes 子模块接管
- `CharacterPhysique`：标记为临时方案，应由 CharacterAttributes 子模块统一持有角色属性路径映射

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Physique 提取从 Awake 移到 Start | A: 留在 Awake + `[DefaultExecutionOrder]` 强行排序 → Vito：用外力破坏自有生命周期体系，自毁长城 | 遵循 ModuleChild 生命周期原则，跟 CharacterContainer 一致 |
| `GetFloat` 路径不存在直接 `Debug.LogError` | 初版实现未考虑表构建期 GetFloat 取旧值场景（值字典空但路径合法），导致启动报错。修正：先查 _structure，路径在结构中但值未写入 → 返回 default（非错误） | 真 typo 当场炸，未写入的不误报 |
| `CharacterPhysique` 价值重定位：类型边界而非性能快照 | A: 继续用"hot path 性能"辩护（字典查找 ~200ns/帧，不构成热路径） | 承认性能论不成立；价值在于一次字符串→强类型转换、编译期保证、不可变传递 |

## Known Issues

- [ ] `public IPropertyReader Properties` 仍被 UIService.TryGetPlayerProps() 读取（P2 架构债 — 待属性事件/快照体系）
- [ ] 饥饿消耗 `AddModifier` 仍硬编码在 CharacterActor.Start()（P2 架构债 — 待生理系统设计）
- [ ] `private CharacterContainer container` 未被读取，IDE 提示（预期内 — 下阶段接入）

## Cross-References

### Related Sessions
- [2026-06-27-container-character-slots.md](2026-06-27-container-character-slots.md) — 同日 L3_Container + 身体槽位落地，本次审查的上下文

### Related Tech Docs
- [tech/.../property-table.md](../tech/L2-services/L2-modules/L3-properties/property-table.md) — GetFloat 等读方法签名更新、ErrorPath 替换 WarnPath
- [tech/.../property-agent.md](../tech/L2-services/L2-modules/L3-properties/property-agent.md) — DefaultExecutionOrder 新增
- [tech/.../character-actor.md](../tech/L2-services/L2-modules/L3-character/L4-actor/character-actor.md) — 字段重命名、访问修饰符变更、TODO 标记

### Flag for Design Doc Creation
- [x] No design doc needed — internal refactoring, no design-facing changes.
