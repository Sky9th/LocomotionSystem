---
name: rd-session-doc
description: 归档会话工作日志——记录本次会话做了什么、为什么、决策与已知问题。每次会话结束时必须调用。
when_to_use: 会话结束时归档工作记录、用户说"写 session""归档会话""记录本次"、代码改动后需要记录开发上下文时
---

## Purpose & Audience

Session docs answer: **"What did we do in this session and why?"**

- **Primary audience**: Developers — need to understand what changed and why when resuming work days/weeks later
- **Secondary audience**: Design collaborator — need visibility into what was implemented and what trade-offs were made

Session docs are **working logs**, not polished documentation. They capture decisions at the moment they were made, before the reasoning fades.

## When to Create

Create a session doc at the **end of every development session** where:
- Code was written, refactored, or deleted
- A design decision was made (even if not yet implemented)
- A bug was investigated (even if not fixed)
- Plans were created or substantially revised
- Infrastructure/tooling was changed

Do NOT create a session doc for:
- Pure Q&A / exploration sessions that produced no artifacts
- Sessions under 5 minutes with no meaningful output

## Required Template — 5 Sections

Every session doc MUST have these exact five sections. No exceptions.

---

### ## Background

Why was this needed? What problem did it solve? What triggered this session?

**Do NOT** just repeat the title. Write at least 2 sentences explaining:
- What state was the project in before this session?
- What specific problem motivated the work?
- Is this part of a larger initiative? Link to the plan if so.

### ## Changes

What was done. Concrete and specific.

**Format**: Bulleted list grouped by subsystem. Each bullet states WHAT was changed, not HOW (leave HOW to tech docs).

```markdown
### Subsystem A
- Added X to handle Y
- Renamed Z to W because ...

### Subsystem B
- Fixed edge case where ...
```

**Do NOT** write vague entries like "improved things" or "cleaned up code".

### ## Decisions

Why we chose this approach over alternatives. Table preferred.

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Used pattern X | A: simpler but broke Y; B: too heavy for current scope | X balances correctness with delivery speed |

Each decision MUST include at least one rejected alternative and the reason for rejection.

When a decision touches game mechanics, note it here AND flag it for design doc creation (see Cross-References).

If truly no decisions were made (pure mechanical changes), write:
> _No design decisions this session — all changes were mechanical._

### ## Known Issues

What's still broken, incomplete, or untested after this session.

**Format**:
```
- [ ] Issue description — severity (P0/P1/P2) — plan for resolution
- [x] Resolved issue — keep briefly for context, mark with x
```

Be honest about:
- Incomplete edges (e.g., "only works for Player, NPC path untested")
- Known race conditions or timing issues
- Performance concerns not yet profiled
- Dependencies on future work (link to the plan)

If truly no known issues, write: `_None — all changes verified._`

### ## Cross-References

Links to related artifacts. This is MANDATORY — the audit found only 39% of sessions have cross-references.

```markdown
### Related Sessions
- [YYYY-MM-DD-topic.md](YYYY-MM-DD-topic.md) — one-line description of relationship

### Related Plans
- [../plans/short-term-plan.md](../plans/short-term-plan.md) — what part of the plan this advances

### Related Tech Docs
- [tech/L2-services/.../module.md](../tech/L2-services/.../module.md) — what was updated

### Related Design Docs
- [../design/system.md](../design/system.md) — what design decision was made or referenced

### Flag for Design Doc Creation
- [ ] NEW design doc needed for: subsystem/topic — because: reason
```

If NO related artifacts exist, write `_No related sessions/plans/docs._` — do NOT omit the section.

---

## Quality Gates

The following checks are **mechanical** — verify them before finalizing the file:

### Gate 1: Minimum Length
- **Absolute minimum**: 15 lines (under this, REFUSE to write — session too thin)
- **Target minimum**: 25 lines (warn if under)
- Count lines AFTER the title line. Count all section content.

### Gate 2: Required Sections
- [ ] `## Background` — present, at least 2 sentences
- [ ] `## Changes` — present, at least 1 grouped bullet
- [ ] `## Decisions` — present, at least 1 row OR explicit "no decisions" note
- [ ] `## Known Issues` — present, at least 1 entry OR explicit "none" note
- [ ] `## Cross-References` — present, with links OR explicit "no related artifacts" note

### Gate 3: Naming Rules
- **Filename format**: `YYYY-MM-DD-{topic}.md` — e.g., `2026-06-20-grip-switching.md`
- **Characters**: kebab-case ASCII ONLY — `[a-z0-9-]`. No uppercase, no Chinese, no spaces, no special chars except `-`
- **Date**: Must match the actual session date. Verify against `date` command output.
- **Year check**: If year in filename != current year, ERROR (examples found: `2025-04-29-*.md` when actual date was 2026)
- **Topic**: Specific, not generic. BAD: `refactor`. GOOD: `locomotion-ground-detection-refactor`

### Gate 4: Single-Topic Rule
- **Check**: Does this session mix unrelated subsystems?
- **Warn** if Changes section touches > 3 unrelated domains
- **Refuse** if the mixed topics would produce better documentation if split
- If mixing detected, suggest splitting into two session files

### Gate 5: Merge Suggestion
- **Check**: Does a session already exist for this same date AND same topic?
- **Behavior**: If yes, suggest merging into existing file instead of creating new
- **Pattern**: Scan `.agent/sessions/` for files starting with same `YYYY-MM-DD-`

---

## Anti-Patterns

### AP-1: Missing Background
```markdown
# 2026-06-20 — Grip Switching
## Changes              <-- WRONG: No Background section
- Added X
```
**Fix**: Always start with `## Background`.

### AP-2: Too Thin (< 15 lines)
A 9-line session is worse than no session — it creates the illusion of documentation. If the session was truly this thin, merge it into the next session or expand it.

### AP-3: Chinese or Special Chars in Filename
```
BAD: 2026-06-07-Ability标签重构.md       <-- Chinese characters
BAD: 2026-06-07-Editor菜单+Tag导入器.md  <-- Chinese + special char '+'
GOOD: 2026-06-07-ability-tag-refactor.md
GOOD: 2026-06-07-editor-menu-tag-importer.md
```

### AP-4: Mixed Unrelated Topics
```markdown
# 2026-06-19 — Plans + Cleanup
### Plans
- Rewrote short-term plan
### Animancer Cleanup
- Deleted 42 orphan assets      <-- Unrelated to plans
```
These should be separate sessions or clearly sub-headed under one coherent theme.

### AP-5: Wrong Year in Date
```
BAD: 2025-04-29-locomotion-snapshot.md    <-- content references 2026 work
```
Always verify the year against the current date.

### AP-6: Decisions Without Alternatives
Every decision must state at least one rejected alternative and why it was rejected.

### AP-7: Missing Cross-References
Session created a new tech doc or made a design decision but Cross-References section says "None." This is the most common quality failure.

### AP-8: Changes Without Subsystem Grouping
Flat bullet lists without subsystem headings make it hard to scan. Always group by subsystem.

---

## Example

```markdown
# 2026-06-20 — Grip Switching + Animation Asset Reorganization

## Background

The character animation system previously hard-coded a single grip stance. Adding
weapon-equipped locomotion required the animation pipeline to support grip-aware
layer switching. This is part of S4 Animation Pipeline in the short-term plan.

## Changes

### Grip Switching Runtime
- `LocomotionAnimationSetSO` — added `HasFullLocomotion` flag
- `CharacterActor.Update()` — resolve grip after director, pass animSet into Simulate
- `LocomotionDriver.Evaluate()` — detect grip change, swap BaseLayer or overlay Arm layer
- `AnimationBrain` — added Arm layer (index 2, armMask), exposed `ArmLayer` property
- `GroundLocomotion` / `ILocomotionSimulator` — signatures updated with animSet parameter
- Deleted deprecated `animancerTransitions` field from CharacterActor

### Animation Asset Reorganization
- Created 4 new locomotion sets: Sidearm Relax, Sidearm Combat, Blade Relax, Blade Combat
- Updated `Human.json` — 5 sets, gripTable uses Relax as default
- Updated `tags_all.json` — `Pistol` → `1H_Sidearm`, `Knife` → `1H_Blade`

### Fixes
- `TraversalAnimationSetSO` — StringAsset → ClipTransition
- `AnimationImportExport` — Traversal import/export adapted for ClipTransition

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Relax grip uses Base layer (no Walk/Run mixers) | A: Create 4-directional Relax sets → wastes ~40 clips. B: Don't animate weapon idle → looks dead. | Relax stances are stationary by design. Base handles idle/walk/run, Arm overlays weapon pose. Minimal cost. |
| Grip tag resolution on CharacterActor | A: Put in AnimationBrain → violates single-responsibility. | CharacterActor owns director pipeline; AnimationBrain stays pure animation. |
| Partial grip detected by `HasFullLocomotion` bool | A: Runtime null-check WalkMixer/RunMixer → fragile. B: Separate enum → duplicates info. | Bool computed once at asset validation time, checked trivially at runtime. |

## Known Issues

- [ ] Arm layer blending not smoothed — abrupt transition on grip switch (P1 — add 0.15s fade)
- [ ] Traversal animations untested with weapon equipped (P1 — needs dedicated test)
- [x] `animancerTransitions` field removal confirmed no external references remain

## Cross-References

### Related Sessions
- [2026-06-16-character-ctx-propagation.md](2026-06-16-character-ctx-propagation.md) — established ctx propagation pattern used for grip resolution

### Related Plans
- [../plans/short-term-plan.md](../plans/short-term-plan.md) — S4 Animation Pipeline, S4.1 grip-aware locomotion

### Related Tech Docs
- [tech/.../locomotion-driver.md](../tech/L2-services/L2-modules/L3-character/L4-animation/drivers/locomotion/locomotion-driver.md) — updated Evaluate() with grip detection
- [tech/.../animation-brain.md](../tech/L2-services/L2-modules/L3-character/L4-animation/animation-brain.md) — updated layer count 6→7, added Arm layer

### Related Design Docs
- None — grip switching is purely technical implementation.

### Flag for Design Doc Creation
- [ ] No design doc needed — this session was implementation of existing design.
```

---

## File Placement

All session docs go to `.agent/sessions/YYYY-MM-DD-{topic}.md`. Flat directory, no subdirectories.

## Integration

When the `rd-doc` command is invoked, it calls this skill for the session layer. This skill can also be invoked independently for session-only archiving.
