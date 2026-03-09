# Item NavMesh Spawn Design

**Date:** 2026-03-10

## Goal
Spawn pickup items one time when game starts, with fixed counts Coin=20, Cross=6, Shield=10, at random valid map positions without overlapping enemies or world obstacles.

## Chosen Approach
Use NavMesh-based random placement with physics clearance checks.

- Generate random points inside configurable spawn bounds.
- Project candidate to nearest baked NavMesh point.
- Reject candidates blocked by configured collision layers.
- Reject candidates too close to already accepted item positions.
- Instantiate item prefabs for each required count.

## Why this approach
- Reuses existing baked NavMesh used by enemy movement.
- Avoids spawning in unreachable/off-map positions.
- Keeps behavior configurable per scene via inspector fields.
- Avoids hard dependency on scene-specific spawn points.

## Data and Config
- Prefabs: `coinPrefab`, `crossPrefab`, `shieldPrefab`
- Counts: `coinCount=20`, `crossCount=6`, `shieldCount=10`
- Spawn area: `spawnAreaCenter`, `spawnAreaSize`
- Validation: `blockedLayers`, `clearRadius`, `minItemSpacing`
- Search limits: `maxAttemptsPerItem`, `navMeshSampleDistance`

## Error Handling
- If prefab is missing, that item type is skipped with warning.
- If no valid position is found within attempts, log warning and continue.
- Never throw or stop the game loop due to spawn failure.

## Testing Strategy
- Manual verification in `Battle_Arena_2` scene:
  - Start game and confirm near 36 items appear.
  - Confirm no item intersects enemies/obstacles.
  - Confirm items are placed on walkable NavMesh.
  - Tune `blockedLayers`, `clearRadius`, `spawnAreaSize` if density is too high.
