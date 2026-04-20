# PHASE 3.3 SPEC - Remaining Buildings and Systems

## Scope
Implement the missing Castle Age structures and their gameplay loop in RealmForge.

## Status
- Implemented in this iteration:
  - Building types added: University, Monastery, SiegeWorkshop, Castle.
  - Runtime spawning and custom mesh generation for each new building.
  - Age gating integrated via BuildingData.MinAge (Castle Age).
  - Imperial Age requirements now align with Castle Age infrastructure:
    - University + Siege Workshop + Monastery.
- Not yet implemented (next steps):
  - Monks and monk tech tree.
  - Siege units (Mangonel/Trebuchet) and deploy/undeploy behavior.
  - Castle unique units by race and castle-only upgrades.
  - University defensive technologies (Masonry, Ballistics, etc.).

## Design Goals
1. Keep progression readable: each Age unlocks meaningful new production options.
2. Preserve RTS pacing: no instant spikes, use queue times and resource pressure.
3. Avoid bypasses: gating must exist in UI and runtime logic.

## Building Definitions

### University
- Age: Castle (3)
- Cost: 200 Wood, 200 Stone
- Role: defensive and engineering research hub.
- First planned techs:
  - Masonry: building HP +10%
  - Fortified Wall: wall HP +25%
  - Ballistics: ranged projectile tracking improvement

### Monastery
- Age: Castle (3)
- Cost: 175 Wood
- Role: monk production + support tech.
- First planned techs:
  - Sanctity: monk HP bonus
  - Fervor: monk speed bonus
  - Redemption: convert siege/buildings (late unlock)

### Siege Workshop
- Age: Castle (3)
- Cost: 200 Wood
- Role: siege production.
- First planned units:
  - Mangonel (anti-mass / structure pressure)
  - Trebuchet (long range structure siege, must deploy)

### Castle
- Age: Castle (3)
- Cost: 650 Stone
- Role: fortress, unique unit production and civ upgrades.
- First planned upgrades:
  - Hoardings
  - Conscription

## Runtime Rules
- `BuildingData.MinAge` is the canonical unlock gate.
- Runtime gate must remain in `BuildingManager.StartPlacement` even if UI is bypassed.
- Research and training panels must hide/disable unavailable entries by age.

## Integration Plan (Next Iteration)
1. Add new `UnitType` entries for Monk, Mangonel, Trebuchet.
2. Extend `UnitData` factory and `SceneSetup.SetupUnitRegistry` mappings.
3. Expand `ResearchCatalog` with University/Monastery/Castle techs.
4. Add deploy/undeploy state machine for Trebuchet.
5. Add race-linked unique unit mapping for Castle.
6. Add telemetry counters for tech completion and age timings.

## Acceptance Criteria
- Player can place all Castle Age buildings after reaching Age 3.
- Imperial advance requires University + Siege Workshop + Monastery.
- New buildings render with distinct silhouettes and health/build lifecycle works.
- No compile errors in modified scripts.
