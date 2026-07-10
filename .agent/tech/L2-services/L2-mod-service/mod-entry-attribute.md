# ModEntryAttribute

> `Assets/Scripts/Services/L2_ModService/ModEntryAttribute.cs`
>
> **Last Verified**: 2026-07-10 | **Verification**: File exists, signature matches code

## Call Chain

- **Called by**: Mod authors (compile-time), `ModService.DiscoverAndInvokeEntries()` (runtime via `GetCustomAttribute<ModEntryAttribute>()`)
- **No calls** — pure marker attribute, no behavior

## Coupled Modules

| Direction | Module | Relationship |
|-----------|--------|-------------|
| Consumer | `ModService` | `Type.GetCustomAttribute<ModEntryAttribute>()` 检测 |
| Consumer | Mod author code | 贴在入口类上 |

## Public Properties

None — pure `System.Attribute` with no custom properties.

## Methods

None — no custom methods. Uses inherited `System.Attribute` members.

## Usage Rules

- **Only on classes**: `[AttributeUsage(AttributeTargets.Class)]` — compiler error if placed on method/interface/struct
- **Non-inherited**: `Inherited = false` — subclass of a `[ModEntry]` class is NOT automatically an entry point
- **Must combine with `IModEntry`**: `ModService` skips classes that have `[ModEntry]` but don't implement `IModEntry`

## Future Plans

| Plan | Status | Source |
|------|--------|--------|
| Move to `RedDust.Modding.dll` | S1 | mod-architecture-framework §1.1 |
