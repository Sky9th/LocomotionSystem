# ModManifest

> `Assets/Scripts/Services/L2_ModService/ModManifest.cs`
>
> **Last Verified**: 2026-07-10 | **Verification**: File exists, signatures match code

## Call Chain

- **Called by**: `ModService.LoadSingleMod()` — `JsonUtility.FromJson<ModManifest>(json)`
- **No calls** — pure data class

## Coupled Modules

| Direction | Module | Relationship |
|-----------|--------|-------------|
| Consumer | `ModService` | `LoadSingleMod()` 反序列化后读取字段 |
| Consumer | Mod author | 编写 `manifest.json` 对应此结构 |

## Public Properties

| Field | Type | Description |
|-------|------|-------------|
| `modId` | `string` | 全局唯一标识，推荐反向域名格式 |
| `name` | `string` | 显示名称 |
| `version` | `string` | 语义化版本 `主.次.修订` |
| `author` | `string` | 作者 ID |
| `description` | `string` | 描述文本 |

## Methods

None — pure data class. Uses `[Serializable]` and public fields for `JsonUtility` compatibility.

## Usage Rules

- **Field names must match JSON keys exactly** (case-sensitive) — `modId` not `ModId`
- **All fields optional except `modId`**: `ModService` skips the mod if `modId` is null/empty
- **`[Serializable]` required**: or `JsonUtility.FromJson` returns null

## Known Limitations

- `JsonUtility` does NOT support top-level arrays (`string[] dependencies`). Adding `dependencies` field requires switching to `Newtonsoft.Json`.

## Future Plans

| Plan | Status | Source |
|------|--------|--------|
| Add `dependencies[]` field | S1 — requires Newtonsoft.Json | mod-architecture-framework |
| Add `loadPriority` field | S1 | mod-architecture-framework §4.3 |
| Add `content` field | S1 | mod-json-reference.md |
| Move to `RedDust.Modding.dll` | S1 | mod-architecture-framework §1.1 |
