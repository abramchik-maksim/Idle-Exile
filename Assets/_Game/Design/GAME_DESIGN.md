# Idle Exile - Game Design Index

This file is the entry point for game design documentation.
Detailed design has been split into focused documents to keep context clear and reduce noise during implementation.

## Read Order

1. `Assets/_Game/Design/Fundamentals.md`
2. `Assets/_Game/Design/Combat.md`
3. `Assets/_Game/Design/Statuses_And_Ailments.md`
4. `Assets/_Game/Design/Progression_TreeTalents.md`
5. `Assets/_Game/Design/Items_And_Skills.md`
6. `Assets/_Game/Design/Loot_And_Economy.md`
7. `Assets/_Game/Design/Changelog.md`

## Scope Rules

- `Current implementation` sections describe behavior already present in code.
- `Target design` sections describe intended future behavior not fully implemented yet.
- Formulas are authoritative only when they are listed under `Current implementation`.

## Migration Notes

- The previous monolithic `GAME_DESIGN.md` was split into domain-specific documents.
- Legacy sections were either moved, rewritten with precise code-backed mechanics, or marked as target-only.
