# IModEntry

> `Assets/Scripts/Services/L2_ModService/IModEntry.cs`
>
> **Last Verified**: 2026-07-10 | **Verification**: File exists, signature matches code

## Call Chain

- **Called by**: `ModService.DiscoverAndInvokeEntries()` — `(IModEntry)instance.Initialize()`
- **Implemented by**: Mod author's entry class — `[ModEntry] public class X : IModEntry { ... }`

## Coupled Modules

| Direction | Module | Relationship |
|-----------|--------|-------------|
| Consumer | `ModService` | Cast from `Activator.CreateInstance` result, call `Initialize()` |
| Consumer | Mod author code | Implement on entry class |

## Public Properties

None — interface defines no properties.

## Methods

### Initialize()
```csharp
void Initialize();
```
- **Purpose**: Mod entry point. Called once after mod assembly is loaded.
- **Params**: None
- **Callers**: `ModService.DiscoverAndInvokeEntries()` after `Activator.CreateInstance` succeeds
- **Notes**: Mod authors put initialization logic here (register items, subscribe events, etc.). Exceptions are caught by ModService and logged; do NOT crash the game.

## Future Plans

| Plan | Status | Source |
|------|--------|--------|
| Move to `RedDust.Modding.dll` | S1 | mod-architecture-framework §1.1 |
| Add `ModContext` parameter | Discussion | mod-architecture-framework §1.1 |
