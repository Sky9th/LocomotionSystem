# 2026-06-20 — Claude Doc Workflow Restructure

## Background

The `.claude` documentation tooling had grown organically: `rd-doc` was a monolithic command
that embedded orchestrator logic alongside three distinct doc templates (session, tech, design)
in a single file. As each template matured — session docs gained quality gates, tech docs added
staleness detection, design docs formalized a 6-section structure — the monolith became hard to
maintain. Adding a section to one template meant scrolling past unrelated templates.

This session extracts the three doc types into standalone skills (`rd-session-doc`, `rd-tech-doc`,
`rd-design-doc`) and rewrites `rd-doc` as a pure orchestrator: classify → dispatch → validate → report.

## Changes

### rd-doc — Orchestrator Rewrite
- Replaced monolithic template logic with Phase 1→4 dispatcher (classify, three-layer orchestrate,
  quality gates, report)
- Added calling-context detection: rd-commit trigger → full three-layer archive; user direct call →
  knowledge-classification dispatch
- Added design-impact decision matrix (when to create design doc vs. note in session)
- Added special scenarios: pure-doc changes, cross-module changes, hotfix, ambiguous user calls

### rd-session-doc — New Standalone Skill
- Extracted 5-section template: Background, Changes, Decisions, Known Issues, Cross-References
- Formalized quality gates: minimum length (15/25 lines), section presence, naming rules,
  single-topic rule, merge suggestion
- Documented 8 anti-patterns with examples (missing background, too thin, Chinese filenames, etc.)

### rd-design-doc — New Standalone Skill
- Extracted 6-section template: System Positioning, Gameplay Mechanics, Numeric Design,
  Player Experience, Edge Cases, A测 Scope
- Added creation trigger decision table (new mechanic, player-visible change, balance, etc.)
- Added `design/character/` special rule: mandatory first doc if directory is empty
- Integrated A测 scope tagging for scope control

### rd-tech-doc — Major Rewrite
- Restructured for standalone skill invocation (no longer embedded in rd-doc)
- Retained pre-write checks: file existence, dead class detection, signature verification
- Retained Last Verified stamp and staleness scanning

### rd-commit — Simplified
- Replaced inline doc-archiving steps with delegation to rd-doc orchestrator
- Removed duplicate versioning rules (now in rd-commit only)

## Decisions

| Decision | Alternatives Considered | Reason |
|----------|------------------------|--------|
| Extract skills rather than keep monolith | A: Keep monolithic rd-doc → harder to maintain as templates grow. B: Embed templates in each skill but keep rd-doc thin → same result, more hops. | Standalone skills can be invoked independently (e.g., user calls rd-session-doc directly) AND by the orchestrator. Single source of truth per doc type. |
| Orchestrator pattern (rd-doc dispatches to sub-skills) | A: Remove rd-doc entirely, have rd-commit call each sub-skill → 3 separate invocations, no quality-cross-check. B: Have sub-skills call each other → circular dependency risk. | Orchestrator owns the "what needs archiving" decision and cross-verification. Sub-skills own "how to write this doc type." Clean separation. |
| Session doc 5-section mandatory template | A: Free-form session notes → inconsistent quality, hard to scan. B: More sections (8+) → over-engineered for working logs. | 5 sections covers the essential questions every future reader asks: why, what, decisions, problems, links. |
| Design doc always opt-in (conditional on design impact) | A: Always create design doc for any code change → noise, many empty docs. B: Never create in code sessions → design decisions get lost. | Decision matrix in rd-doc §2.3 gates creation on actual player-facing or balance impact. |

## Known Issues

- [ ] New skill files (rd-session-doc, rd-design-doc) not yet battle-tested in a real multi-module commit (P2 — will observe in next session)
- [ ] rd-tech-doc staleness detection scoped to "same directory + parent" — may miss cross-directory stale references (P2 — deliberate trade-off, full scan too expensive per session)
- [ ] Design doc `character/` special rule untested — directory is not empty, so the trigger won't fire yet (P3 — verify when hitting a character design change)

## Cross-References

### Related Sessions
- [2026-06-20-grip-switching-animation-reorg.md](2026-06-20-grip-switching-animation-reorg.md) — earlier today, the session whose doc workflow pain motivated this restructure

### Related Plans
- None — this is tooling infrastructure, not on the game plan.

### Related Tech Docs
- None — no game code was changed.

### Related Design Docs
- None — no game design decisions were made.

### Flag for Design Doc Creation
- [x] No design doc needed — tooling infrastructure change, no player-facing impact.
