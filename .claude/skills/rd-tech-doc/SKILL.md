---
name: rd-tech-doc
description: 编写/更新 tech 技术文档，遵循 L1→L5 架构层级。包含新鲜度检查和死类检测。
when_to_use: 新增模块后归档技术文档、更新已有模块文档、用户说"写文档""归档 tech"时、代码重构后需要更新技术文档时
---

## Purpose & Audience

Tech docs answer: **"How does this code work?"**

- **Primary audience**: Programmers — need to understand call chains, APIs, architecture to modify/extend/debug code
- **Secondary audience**: Technical design review — need to verify implementation matches architectural principles

Tech docs live in `.agent/tech/` and are organized by the L1→L5 architecture layer system.

---

## Pre-Write Checks (MANDATORY)

Before writing or updating ANY tech doc, run these checks. **Refuse to write unverified content.**

### Check 1: Freshness — Code File Existence

For every `.cs` file referenced in the doc (filename or call-chain diagrams), verify:
- The file exists in `Assets/Scripts/`
- The class name referenced matches the actual class name in the file

**How**: `Glob` for the `.cs` path, then `Grep` for `class ClassName`.

**If a file is MISSING**: Mark the doc stale at the top:
```markdown
> [!WARNING] FRESHNESS FAILED (YYYY-MM-DD)
> Files referenced below no longer exist:
> - `Assets/Scripts/.../DeletedClass.cs` — class deleted or renamed
> This doc requires update before it can be trusted.
```

Do NOT delete the doc — mark it so someone can update it later.

### Check 2: Deleted Class Detection

Scan the doc for PascalCase identifiers that look like class names. For each, search `Assets/Scripts/` for `class ClassName`. If NOT found, flag as deleted.

**Known deleted classes (as of 2026-06-20)**:

| Deleted Class | Replaced By |
|---------------|-------------|
| `AnimationAliasProfile` | `LocomotionAnimationSetSO` (direct clip references) |
| `LocomotionAnimationProfile` | `LocomotionAnimationConfigSO` (renamed) |
| `LocomotionProfile` | `GroundSystemConfigSO` (system params) + `CharacterPhysique` (character params) |
| `CharacterProfile` | `CharacterAnimationProfileSO` + specialized configs |
| `CharacterStats` | `PropertyAgent` / `FloatState` / `FloatModifier` |
| `CharacterPhysicsProfileSO` | `GroundSystemConfigSO` + Properties |
| `LocomotionProfileSO` | Properties `CharacterPhysique` |
| `KinematicProfileSO` | Properties `CharacterPhysique` |
| `BaseService` | `ModuleComponent` (direct inheritance) |
| `AbilityComponent` | `AbilityExecutor` + `AbilityReactor` |

### Check 3: Signature Verification

For every method signature quoted in the doc (lines matching `public|private|protected|internal.*\(`):
- Verify the signature in the actual `.cs` file matches
- If parameters, return type, or method name differ → flag as stale

### Check 4: Layer/Count Accuracy

For docs claiming a module has N layers/states/components:
- Count the actual number in code
- If mismatched → flag as stale

**Known stale examples**: `animation-brain.md` says 6 layers, code has 7 (Arm layer added). `base-layer.md` says 7 FSM states, code has 5.

### Check 5: Code-to-Doc Coverage

Before writing a NEW doc for a module:
- List all `.cs` files in the code directory
- Check which already have tech docs
- Report uncovered files
- Cover ALL public classes in the module

### Last Verified Stamp

Every tech doc MUST include this line immediately after the title:

```markdown
> **Last Verified**: YYYY-MM-DD | **Verification**: All referenced files exist, signatures match code
```

Or if stale:
```markdown
> **Last Verified**: YYYY-MM-DD | **Verification**: STALE — N referenced classes deleted, M signatures mismatched
```

When updating an existing doc, re-run all checks and update the stamp.

---

## Architecture Layers

Data flow direction: **L1 → L2 → L3 → L4 → L5. Layer-by-layer propagation and return. Cross-layer calls FORBIDDEN.**

```
L0  Unity Engine        Not documented

L1  GameManager         Root — owns all L2 Services

L2  Service             Service layer
    ├─ Base Service     Single file, no internal sub-modules
    ├─ Composite Service Contains L3 sub-modules
    └─ L2-modules/      Virtual L2 — independent modules (not owned by any single Service)

L3  Module              Module layer — two types:
    ├─ Affiliated Module   Belongs to an L2 Service
    └─ Independent Module  Shared by multiple Services

L4  Component           Component layer — responsibility components within L3

L5  Behavior            Behavior layer — VERY RARE, true behavioral decomposition
    └─ Only L5-states (FSM states) and L5-rules (strategy rules)

shared/                 Global Helpers — no layer restriction
```

## Important Distinction: Architecture Layer vs Code Organization

| Type | Prefix | Example | Meaning |
|------|--------|---------|---------|
| Architecture Layer | L-prefix | L4-animation, L5-states | Responsibility decomposition, independent architectural units |
| Code Organization | No L-prefix | config/, structs/, player/, button/ | Grouping similar files for discoverability |

**L5 is extremely rare**. Currently ONLY `L5-states` and `L5-rules` are true L5. Do NOT label code organization directories as L5.

## Directory Structure

```
tech/
├── README.md                    # Root index — complete layer map + file inventory
├── L1-core/                     # GameManager root
├── L2-services/                 # Service + Module layer
│   ├── (Base Service — single .md)
│   ├── L2-input/                # Composite Service
│   ├── L2-ui/                   # Composite Service
│   ├── L2-audio/                # Composite Service
│   └── L2-modules/              # Virtual L2 — independent module container
│       ├── L3-character/        # L3 independent
│       ├── L3-ability/          # L3 independent
│       ├── L3-properties/       # L3 independent
│       ├── L3-equipment/        # L3 independent
│       └── L3-pathfinding/      # L3 independent
├── editor/                      # Editor extensions (cross-layer)
├── conventions/                 # Naming & style conventions
└── shared/                      # Global Helpers — no L-prefix
    ├── logging/
    └── utility/
```

## Writing Process

```
1. Read Code          Understand the call chain and each method's responsibility
2. Verify Freshness   Run ALL 5 Pre-Write Checks (file existence, deleted classes, signatures, counts, coverage)
3. Determine Layer    Assign module to L1/L2/L3/L4/L5/shared
4. Write Sub-Module Docs  One .md per .cs file, depth to function level
5. Write Module Overview  Call chain + coupling + design decisions + future plans + sub-doc index
6. Update Root Index      Ensure file tree and annotations match code
7. Stamp Verification     Add/update Last Verified stamp
8. Format Check           Validate against format checklist
```

> The old `archive/tech-v1/` and `archive/tech-v2/` directories are archived. All new tech docs go to `tech/`.

## Root Index `tech/README.md`

Complete code tree organized by L1→L5 layers. One line per `.cs` file with a responsibility annotation.

**Update trigger**: Any add/delete/rename of `.cs` files.

## Module Overview Template

**Do NOT repeat the root index directory tree.** 6 required sections:

| Section | Required | Content |
|---------|----------|---------|
| Last Verified | ✅ | Verification stamp with date and result |
| Layer Position | ✅ | Which layer (L1-L5) and why |
| Call Chain | ✅ | ASCII diagram: internal call flow + external module interactions |
| Coupled Modules | ✅ | Table: module, dependency/consumer, relationship |
| Design Decisions | ✅ | Table: key decision, reason |
| Future Plans | ✅ | Table: plan, status, dependency, source |
| Sub-Document Index | ✅ | Table with links to each sub-module's detailed doc |

Optional: Layering (when module has internal lifecycle layers), Destroy Sequence.

## Sub-Module Document Template

Depth to function level. One `.md` per `.cs` file. File path as first line reference.

**8 required sections** (marked "Optional" as applicable):

| Section | Required | Content |
|---------|----------|---------|
| Last Verified | ✅ | Verification stamp |
| Call Chain | ✅ | Called by whom / Calls whom |
| Coupled Modules | ✅ | Table: direction, module, relationship |
| Public Properties | ✅ | Type + purpose for each property |
| Methods | ✅ | Signature + purpose + params + callers + notes |
| Internal Mechanics | Optional | Unity lifecycle (MonoBehaviour → required) |
| Usage Rules | Optional | Constraints and prohibitions (constraints exist → required) |
| Future Plans | ✅ | Extension directions + code TODOs |

**Method format**:
```markdown
### MethodName()
\`\`\`csharp
public ReturnType MethodName(ParamType param)
\`\`\`
- **Purpose**: One sentence
- **Params**: `param` — description
- **Returns**: Description (omit for void)
- **Callers**: Who calls it and when
- **Notes**: Cautions (optional)
```

## File Naming

- **Doc filenames**: kebab-case, meaningful. e.g., `character-actor.md` (NOT `characteractor.md`)
- **Directory names**: L-prefix + kebab-case for layers. e.g., `L4-actor/`, `L5-states/`
- **Code organization directories**: No L-prefix, short names. e.g., `config/`, `structs/`, `player/`

## Format Checklist

- [ ] Sub-module doc: Call Chain, Coupled Modules, Public Properties, Methods, Future Plans — all 5 present
- [ ] MonoBehaviour classes have Internal Mechanics section
- [ ] Classes with constraints have Usage Rules section
- [ ] Section names consistent: 内部机制 ≠ 内部方法, 使用规则 ≠ 通信规则
- [ ] Module overview does NOT repeat root index file tree or sub-module method details
- [ ] Future Plans entries each have "Status" and "Source"
- [ ] Filenames kebab-case, directory names L-prefixed
- [ ] Code organization and architecture layers NOT confused
- [ ] Root index file tree matches code
- [ ] Last Verified stamp present and dated today

## Staleness Self-Check (Run on EVERY write/update)

- [ ] All `.cs` files referenced in the doc exist on disk
- [ ] All class names referenced in the doc exist in the codebase
- [ ] Layer header names match actual code paths
- [ ] Method signatures in doc match actual method signatures in code
- [ ] Constructor parameter counts match actual constructors
- [ ] Count claims (N layers, M states) match actual code
- [ ] Last Verified stamp present and dated today
- [ ] If any check FAILED, doc is marked STALE with specific failures listed

## Principles

- **Split files**: One sub-module per file
- **Lazy loading**: Root Index → Module Overview → Sub-Module, navigate layer by layer
- **No duplication**: Sub-module content not in overview; overview content not in root index
- **Code is truth**: When doc and code disagree, code wins AND doc must be updated
- **L-prefix conservatively**: Only true architectural layers get L-prefix
- **Freshness mandatory**: No doc written without pre-write checks; no existing doc updated without re-verification
