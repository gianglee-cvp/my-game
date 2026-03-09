# Item NavMesh Spawn Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Spawn 36 pickups at game start on random NavMesh-valid points while avoiding monsters and solid objects.

**Architecture:** Add a dedicated `ItemSpawnManager` MonoBehaviour responsible for one-time startup spawning. It uses random points in a configured area, snaps to NavMesh via `NavMesh.SamplePosition`, then validates physical clearance with `Physics.CheckSphere` and spacing against already spawned items.

**Tech Stack:** Unity C#, NavMesh (`UnityEngine.AI`), 3D Physics.

---

### Task 1: Add startup item spawner component

**Files:**
- Create: `Assets/myscript/ItemSpawnManager.cs`

**Step 1: Create manager skeleton**
- Add prefab refs for coin/cross/shield.
- Add count refs with defaults 20/6/10.
- Add spawn area + validation parameters.

**Step 2: Implement spawn workflow in Start()**
- Build list of prefabs by count.
- Shuffle list for mixed distribution.
- Try to find valid position and instantiate each item.

**Step 3: Implement candidate validation**
- Generate random point in bounds.
- Sample NavMesh point.
- Reject blocked points with `Physics.CheckSphere`.
- Reject near-duplicate points using `minItemSpacing`.

**Step 4: Add debug gizmos and warnings**
- Draw spawn bounds in Scene view.
- Log warnings for missing prefab or exhausted attempts.

### Task 2: Verify compile and behavior

**Files:**
- Verify: `Assets/myscript/ItemSpawnManager.cs`

**Step 1: Run lightweight compile verification**
- Use local build/compile command available in environment.

**Step 2: Manual scene setup checklist**
- Attach `ItemSpawnManager` in `Battle_Arena_2`.
- Assign 3 prefabs and blocked layers.
- Tune `spawnAreaSize`, `clearRadius`, `minItemSpacing`.

**Step 3: Manual runtime validation**
- Start scene, confirm item counts and non-overlap behavior.
- Check warnings for failed spawn attempts.
