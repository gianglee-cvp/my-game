# Bomb Straight-Shot Drop Design

## Summary
Enable bomber bombs to fire in a straight line toward the player's position at drop time so the player can dodge. Keep existing homing behavior available for other uses.

## Goals
- Bomb dropped by bomber travels straight toward the player's position at drop time.
- Preserve current homing behavior for other callers.
- Minimal behavior change outside bomber drop flow.

## Non-Goals
- No new visuals or FX changes.
- No randomness or spread added in this change.

## Approach Options
1. Straight-shot only (replace homing): simplest, but removes homing globally.
2. Add mode (recommended): keep homing and add straight-shot for bomber.
3. Straight-shot + random spread: adds unpredictability, but needs tuning.

Chosen: Option 2.

## Design
### Components
- `Bomb` gains a straight-shot launch path that sets velocity once and disables homing updates.
- `EnemyAI.DropBomb()` uses the straight-shot path with the player's position at drop time.

### Data Flow
- Bomber calls `Bomb.LaunchStraightAtPosition(player.position)`.
- `Bomb` initializes rigidbody as before, calculates direction to target position, sets `rb.linearVelocity` once, and sets `isHoming = false` so `FixedUpdate()` does not retarget.

### Error Handling
- If target direction is too small, skip velocity set.
- Ensure rigidbody exists (existing logic).

### Testing
- Player stands still: bomb flies straight and hits.
- Player moves after drop: bomb flies to the old position and can be dodged.
- Other homing uses remain unchanged.
