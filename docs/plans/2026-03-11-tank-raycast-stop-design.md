# Tank Raycast Stop Design (2026-03-11)

## Goal
Stop the tank from moving when an obstacle is detected within 1.5 units in the movement direction (both forward and backward). Keep all physics-related logic in `FixedUpdate`.

## Constraints and Assumptions
- Tank movement uses `Rigidbody.MovePosition` and should stay in `FixedUpdate`.
- Input is read in `Update` and consumed in `FixedUpdate`.
- Raycast distance is fixed at `1.5f` for the stop condition.
- Detection should apply for both forward and backward movement.

## Approach (Chosen)
Use a single `Physics.Raycast` in `FixedUpdate` before moving:
- Compute movement direction from the current `Vertical` input (sign-based, forward or backward).
- Cast a ray from the tank position with a small upward offset to avoid ground hits.
- If hit distance is less than `1.5f`, skip `MovePosition` and keep the tank stationary.
- Otherwise, proceed with `MovePosition` using the current input value.

## Alternatives Considered
- BoxCast based on collider extents for more robust detection. Not chosen to keep changes minimal and aligned with the request.
- Rely solely on Rigidbody collision detection without raycast. Not aligned with the explicit stop-distance requirement.

## Data Flow
1. `Update` reads input and stores it (vertical axis).
2. `FixedUpdate` uses stored input to:
   - Determine direction (forward/backward).
   - Raycast in that direction for up to 1.5 units.
   - Block movement if hit distance < 1.5 units.
   - Move otherwise.

## Error Handling
- If input magnitude is near zero, skip raycast and movement.
- If no hit, allow movement.

## Testing
- Manual: move toward a wall forward and backward; verify stop within 1.5 units.
- Manual: move with no obstacles; verify normal movement.

