# NPC Authority Strategy

## Overview

The NPC is implemented as a `NetworkObject` using Photon Fusion 2 in Shared Mode. Because Shared Mode has no dedicated server, a decision must be made about which peer holds `StateAuthority` over the NPC and how that authority is assigned consistently across all clients.

## Authority Assignment

The NPC is spawned exclusively by the **SharedModeMasterClient** which is the peer with the lowest `PlayerRef` index currently connected. This is checked in `NPCSpawner.Spawned()`:

```csharp
if (!Object.HasStateAuthority || !Runner.IsSharedModeMasterClient) return;
if (_npcSpawned) return;

_npcSpawned = true;
Runner.Spawn(_npcPrefab, position, rotation);
```

Because `Runner.Spawn()` is called by the master client, that peer automatically receives `StateAuthority` over the spawned NPC `NetworkObject` for the duration of the session.

## What StateAuthority Controls

Only the peer holding `StateAuthority` over the NPC executes:

- `OnEnterState()` — sets the NavMeshAgent destination on state entry
- `OnFixedUpdate()` — runs patrol waypoint logic and chase distance checks
- State machine transitions via `_machine.TryToggleState()`
- NavMeshAgent movement calls (`Agent.SetDestination`)

All other peers receive the NPC's replicated position and rotation via the `NetworkTransform` component and use `OnRender()` to drive the Animator — they never call `SetDestination` or modify state machine state.

## Why This Matters

Without the `HasStateAuthority` guard in `OnEnterState()`, the NavMeshAgent would attempt to run on every peer simultaneously. Each peer would compute slightly different paths depending on local physics state, causing the NPC position to desync across clients. The `NetworkTransform` component replicates the authoritative position to all peers, but only if a single peer is writing it.

## FSM Lifecycle — Authority vs All Peers

| Method            | Runs on             | Purpose                                   |
| ----------------- | ------------------- | ----------------------------------------- |
| `OnEnterState()`  | StateAuthority only | Set NavMesh destination, initialise state |
| `OnExitState()`   | StateAuthority only | Clean up state, stop agent                |
| `OnFixedUpdate()` | StateAuthority only | AI logic, transitions, movement           |
| `OnRender()`      | All peers           | Drive Animator, visual updates            |

This separation is enforced by the Fusion FSM addon — `OnFixedUpdate` is routed to authority only. `OnEnterState` requires an explicit `HasStateAuthority` guard because entry callbacks fire on all peers (documented as a common pitfall in the Week 10 lab slides).
