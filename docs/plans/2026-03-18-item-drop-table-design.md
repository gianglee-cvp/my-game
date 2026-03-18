# Item Drop Table For Shooter Enemies Design

## Summary
Add a weighted drop table to `ItemSpawnManager` and spawn a random item at a shooter enemy's death position. Bomber enemies do not drop items.

## Goals
- Provide a weighted drop table with an explicit no-drop weight.
- Spawn items at shooter death position only.
- Keep existing startup spawn behavior unchanged.

## Non-Goals
- No change to bomber or boss drops.
- No new VFX/audio.
- No balancing changes beyond weights.

## Approach Options
1. Drop directly in `EnemyAI` on death (requires death hook).
2. Use `HP` death event and let `EnemyAI` subscribe (recommended).
3. Centralize in `EnemyPool.Despawn` (too broad).

Chosen: Option 2.

## Design
### Components
- `ItemSpawnManager`
  - Add `DropItem` class and `dropTable` + `noDropWeight` fields.
  - Add `GetRandomItemPrefab()` with weighted selection including no-drop.
  - Add `SpawnItemAtTransform(Transform target)`.
- `HP`
  - Add `OnDied` event and invoke in `Die()`.
- `EnemyAI`
  - Add `itemSpawner` reference.
  - Subscribe to `HP.OnDied` only for `EnemyType.Shooter`.
  - On death, call `itemSpawner.SpawnItemAtTransform(transform)`.

### Data Flow
1. Shooter enemy dies in `HP.Die()`.
2. `HP.OnDied` fires.
3. `EnemyAI` receives event and calls `ItemSpawnManager.SpawnItemAtTransform`.
4. `ItemSpawnManager` selects a prefab from drop table and spawns it at the enemy position.

### Error Handling
- If total weight is 0, return null (no spawn).
- Ignore null prefabs in the drop table.

### Inspector Setup Example
- No Drop Weight: 30
- Drop Table:
  - Coin: 40
  - Cross: 20
  - Shield: 10

Total = 100, so the chances are 30% no drop, 40% coin, 20% cross, 10% shield.

### Testing
- Kill shooter: item drops with expected ratios.
- Kill bomber: no drop.
- Set `noDropWeight` high: most kills produce no drop.
