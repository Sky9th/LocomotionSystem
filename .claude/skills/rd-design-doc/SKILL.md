---
name: rd-design-doc
description: 编写/更新 design 设计文档——游戏机制、系统定位、玩家体验、数值设计。面向策划和设计讨论。
when_to_use: 新增游戏系统设计、讨论玩法机制、确定数值体系、定义玩家可见行为变化、用户说"写设计文档""归档 design"时
---

## Purpose & Audience

Design docs answer: **"What is this system, what should it do, and how should the player experience it?"**

- **Primary audience**: Designers/Planners — need to understand system intent, positioning, and behavior to make coherent design decisions
- **Secondary audience**: Programmers — need to understand WHAT to build before diving into HOW
- **Tertiary audience**: Future design iteration — need historical record of WHY a system was designed a certain way

Design docs live in `.agent/design/` and are organized by subsystem.

---

## What Counts as a "Design Decision"

Not every code change requires a design doc. Use this test:

### CREATE a design doc when:

1. **New mechanic** — introducing a game system that didn't exist before (e.g., "adding a morale system")
2. **System interaction change** — changing how two existing systems interact (e.g., "weapon skills now consume stamina")
3. **Player-visible behavior change** — the player will notice the difference (e.g., "attack speed now depends on weapon weight")
4. **Balance / numeric design** — defining formulas, stat curves, economy tuning parameters
5. **UX flow design** — defining screens, menus, control schemes the player interacts with
6. **Content structure** — defining how content is organized (e.g., "tech tree node structure", "loot table categories")

### Do NOT create a design doc when:

1. **Pure implementation** — the design is already documented, this is just coding it (→ session doc + reference existing design doc)
2. **Bug fix** — restoring intended behavior (→ session doc, mention which design doc defines "intended")
3. **Refactor** — same behavior, different code (→ session doc + tech doc update)
4. **Small tuning** — changing one number (→ session doc, note the change; update design doc if it contains stale numbers)
5. **Tool/infrastructure** — editor tools, build pipeline, logging (→ tech doc)

**When uncertain**, ask: "Would a designer need to know about this to make future decisions?" If yes → create a design doc.

---

## Required Template — 6 Sections

Every design doc MUST have these exact six sections.

---

### ## System Positioning

What is this system's role in the game? How does it relate to the player's core loop?

**Must answer**:
- **Placement**: Where does this system sit in the game's overall architecture? Reference `game-overview.md`.
- **Purpose**: What player need does it fulfill? What problem does it solve in the game experience?
- **Scope**: What is explicitly IN scope and OUT of scope for this system?
- **Inputs & Outputs**: Which systems feed into this one? Which systems consume its outputs?

**Example**:
```markdown
## System Positioning

### Placement
Health system is part of the Survival layer, alongside Hunger, Thirst, and Stamina.
It is the primary failure condition — when Health reaches 0, the player dies.

### Purpose
Health provides the core survival tension. All dangers (zombies, starvation, falling)
ultimately threaten Health. All preparations (armor, medicine, food, shelter)
ultimately protect Health.

### Scope
IN: Health values, damage types, death mechanics, injury effects, healing mechanics
OUT: NPC health (separate NPC system), zombie health (combat system), morale effects on health (A测 deferred)

### Inputs & Outputs
- INPUT: Damage events from Combat system, Hunger depletion from Survival system
- OUTPUT: Death event → GameState, Injury status → UI + Character movement debuffs
```

### ## Gameplay Mechanics

How does the system work, moment to moment? What are the rules?

**Must answer**:
- **Core loop**: What happens in the system on each tick / event?
- **Player interaction**: What can the player do? What is automatic?
- **Rules**: What are the explicit rules, formulas, and state transitions?
- **Progression**: Does this system have growth, levels, unlocks?

**Format**: ASCII diagrams for flows, tables for rules, formulas for numbers.

### ## Numeric Design

All numbers, curves, formulas, and balance parameters.

**Must answer**:
- **Base values**: What are the starting numbers?
- **Formulas**: How are values calculated? Show the actual math.
- **Curves**: How do values scale? Use tables or formulas.
- **Balance targets**: What's the intended TTK? Resource depletion rate? Success rate?
- **Tuning knobs**: Which numbers should be exposed for tuning vs. hard-coded?

**Important**: Numbers in design docs are DESIGN TARGETS, not source of truth for implementation (that's in code/ScriptableObjects). Mark them clearly as targets. If numbers are TBD, write `TBD — needs playtesting` with the design principle that will guide tuning.

**Anti-pattern**: "Health values will be balanced during playtesting." → Instead: "HP should be depleted in approximately 4-6 hits from a basic zombie. Base HP: TBD. Formula: `damage = max(baseDamage - armor, 1)`."

### ## Player Experience

What does the player see, feel, and do? How is the system communicated?

**Must answer**:
- **Onboarding**: How does the player discover this system? First encounter?
- **Feedback**: What visual/audio/haptic feedback does the player get? Be specific.
- **Clarity**: Is the system's state clear to the player? What information is shown vs. hidden?
- **Emotional arc**: What emotions should this system evoke? Tension? Satisfaction? Relief?
- **Failure states**: What happens when the system goes badly? Is it recoverable?

**Anti-pattern**: "The player will enjoy using this system." → Instead: "When HP drops below 25%, screen edges show a red vignette pulsing with heartbeat. Movement speed reduced 15%. Audio: heavy breathing + muffled heartbeat. At 10%, adds tunnel vision effect."

### ## Edge Cases

What unusual situations must the system handle gracefully?

**Must answer** (at least 5 entries):
- **Boundary values**: What happens at 0? At maximum? When negative?
- **Interaction conflicts**: What if two systems try to modify the same value simultaneously?
- **Missing dependencies**: What if a required resource/character/object doesn't exist?
- **Load/save**: What state persists across saves?
- **Multi-entity**: What if the player has 0 NPCs? 50 NPCs?

**Format**: Table — edge case → expected behavior.

**Anti-pattern**: "None — this is straightforward." Every system has edge cases. Think harder.

### ## A测 Scope

What is committed for the current pre-alpha phase?

**Must answer**:
- **A测 deliverable**: What exact features ship in A测?
- **Deferred to later**: What is explicitly out of A測 scope but planned?
- **Simplifications**: What corners are intentionally cut for A测?

**Rationale**: Design docs describe the full vision. This section prevents scope creep by explicitly bounding what ships now.

---

## Directory Structure & Naming

```
design/
├── game-overview.md              # Root — complete game concept, core systems
├── {subsystem}/                   # Subsystem directory
│   └── {topic}.md                # Specific design doc
└── ...
```

**Naming rules**:
- **Filenames**: kebab-case ASCII only, e.g., `grip-system.md`, `npc-morale.md`
- **Subsystem directories**: lowercase, kebab-case, e.g., `character/`, `combat/`, `tech-tree/`
- **No Chinese, no spaces, no special chars** in filenames or directories
- **No dates in filenames** — design docs evolve over time, unlike session docs

**CRITICAL**: `design/character/` directory exists but is EMPTY as of 2026-06-20. The audit found 6+ sessions where character design decisions should have been written there. When creating character-related design docs, populate this directory.

---

## Quality Gates

### Gate 1: All 6 Sections Present
- [ ] System Positioning — answers all 4 sub-questions
- [ ] Gameplay Mechanics — includes rules, not vague descriptions
- [ ] Numeric Design — has actual numbers or explicit `TBD` markers with principles
- [ ] Player Experience — covers at least feedback and failure states
- [ ] Edge Cases — at least 5 entries
- [ ] A測 Scope — bounded deliverable list

### Gate 2: Concrete, Not Vague
- BAD: "The combat system will be fun and responsive."
- GOOD: "Attack animation plays over 0.4s. Hit registers at frame 0.25 (the 'impact' frame). Player can queue next attack during last 0.1s of animation (input buffering window)."

### Gate 3: Design Decisions with Rationale
Every design decision must include WHY:
```markdown
| Decision | Alternatives | Rationale |
|----------|-------------|-----------|
| Health uses integer values | A: Float for precision; B: Percentage-based | Integers simplify UI, match survival aesthetic, avoid floating-point accumulation errors |
```

### Gate 4: Cross-References
- Link to `game-overview.md` — how this system fits the overall game
- Link to related design docs — systems this interacts with
- Link to related tech docs — where the implementation lives (if implemented)

### Gate 5: Minimum Length
- **Target**: ≥ 30 lines
- **Minimum**: 20 lines (under this, the design is too thin — expand Numeric Design or Edge Cases)

### Gate 6: Naming
- kebab-case ASCII only
- Subsystem directory must exist or be created
- No dates in filenames

---

## Anti-Patterns

### AP-1: Session-Quality Content in Design Doc
```markdown
# Combat System
## What we did
- Added damage formula
- Changed HP from float to int
```
This is a SESSION LOG. A design doc describes the system as designed, not as built.

### AP-2: No Numbers in Numeric Design
"Health values will be balanced during playtesting." → Write what you DO know: design principles, target ranges, variables that govern the balance.

### AP-3: Design Doc for Pure Implementation
Creating a design doc that just describes existing code with no design decisions. If the system's design was already documented elsewhere, update that doc instead.

### AP-4: Vague Player Experience
"The player will enjoy using this system." → Describe specific feedback mechanisms, not emotions.

### AP-5: Edge Cases Section Says "None"
Every system has edge cases. Think: zero values, max values, missing dependencies, concurrent events, save/load.

### AP-6: Design Doc as TODO List
"Next Steps: 1. Implement 2. Add UI 3. Test" → Remove. Implementation plans go in `plans/`. Design docs describe WHAT, not the plan to build it.

### AP-7: Empty Target Directory
`design/character/` exists but is empty. Don't create new empty directories without populating them. If work touches character systems, create at minimum an overview doc.

---

## Example (Abbreviated)

```markdown
# Morale System — 士气系统设计

> **Status**: Design Phase · Last Updated: 2026-06-20
> **Depends on**: [food-system.md](food-system.md) · [npc-system.md](npc-system.md)

## System Positioning

### Placement
Morale is a secondary stat for NPCs, sitting in the NPC Management layer.
NOT a primary survival stat — NPCs don't die from low morale.
IS the primary efficiency modifier — low morale NPCs work at reduced effectiveness.

### Purpose
Morale solves the "NPCs are robots" problem. Without morale, NPCs are interchangeable
labor units with no personality. Morale creates consequences for player decisions.

### Scope
IN (A测): NPC morale stat, food variety bonus, housing comfort bonus, death/survival events
OUT (A测): Player morale, NPC social relationships, luxury items, recreation activities

### Inputs & Outputs
- INPUT: Food quality (Cooking system), Housing level (Building system), NPC death events (Combat)
- OUTPUT: Work efficiency multiplier, skill growth rate multiplier

## Gameplay Mechanics

### Core Loop
Each in-game day at 06:00 (morning assessment):
  1. Calculate food morale delta: variety bonus, monotony penalty, recipe bonus
  2. Calculate housing morale delta: bed, shelter, heating
  3. Apply event modifiers: death shock (-20, decays +5/day), combat survival (+5)
  4. Clamp to [-50, +50]
  5. Convert to efficiency multiplier (5 tiers)

### Player Interaction
- Player CAN: Choose NPC food, build/assign housing, protect NPCs
- Player CANNOT: Directly set morale (no "motivate" button)
- Morale changes are AUTOMATIC based on resource management decisions

## Numeric Design

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Morale range | [-50, +50] | Symmetrical, divisible into 5 clear tiers |
| Food monotony threshold | 3 days | Players cook in batches; 3 days of same food is realistic |
| Death shock | -20 | Significant but recoverable in 4 days |
| Death shock decay | +5/day | Full recovery in 4 days with improving conditions |

### Efficiency Curve (TBD — needs playtesting)
Design principle: morale should MATTER (25% swing) but not dominate (base stats + tools are primary).

## Player Experience

### Onboarding
Player discovers morale when first NPC works noticeably slower.
Tooltip: "NPC morale is low. Improve food variety or sleeping conditions."

### Feedback
- NPC inspection panel shows morale value + tier label (Happy/Content/Neutral/Unhappy/Miserable)
- Work animation speed visibly scales with efficiency
- Very low morale NPCs play "sigh" or "slump" animations
- No morale UI on main HUD — per-NPC, not global

### Failure States
- Minimum morale (-50): 50% work speed, near-zero skill growth
- Recovery: morale always improvable within days if conditions improve
- No permanent morale damage in A测

## Edge Cases

| Edge Case | Expected Behavior |
|-----------|-------------------|
| NPC not eaten for 3+ days | Floor at -50; hunger stat handles its own effects |
| All NPCs die except one | Survivor gets cumulative death shock capped at -50 |
| NPC recruited after morning assessment | Neutral first-day assessment |
| Player destroys NPC's bed | Next morning: housing penalty applied |
| Save/load mid-day | Morale saved as-is; recalculated at next morning assessment |
| NPC has zero work tasks | Morale still calculated; no efficiency to modify |
| Multiple events same day | All modifiers sum, then clamp |

## A测 Scope

### A测 Deliverable
- NPC morale stat with [-50, +50] range
- 3 input sources: food quality, housing comfort, events
- 5-tier efficiency curve
- Per-NPC inspection UI

### Deferred
- Player morale
- NPC social relationships
- Luxury items / recreation
- Housing quality tiers (binary only in A测)
```

---

## File Placement

All design docs go to `.agent/design/{subsystem}/{topic}.md`.

Existing empty directory: `design/character/` — target for character system design docs.

## Integration

When the `rd-doc` command is invoked, it calls this skill for the design layer. This skill can also be invoked independently when a design discussion produces a decision worth documenting.
