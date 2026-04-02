# CA2 Plan

## Feature Scope — Option C: Networked Score / Game State

### What Will Be Implemented

A networked round system consisting of:

- A countdown timer that starts automatically once two or more players have joined the room and counts down to zero
- A per-player score that increments when a player collects a score orb in the scene
- A game over state broadcast to all clients when the timer reaches zero, declaring a winner based on highest score

### Why This Scope

I decided to go with the countdown timer and score system because it directly demonstrates all required Fusion 2 authority concepts within a well-defined, testable scope:

- The timer is owned by a single `NetworkObject` (a `GameStateManager`) whose `StateAuthority` is the first client to join. This gives a clear, concrete example of `StateAuthority` controlling shared game state
- The per-player score is a `[Networked]` property on each player object, written only under `HasStateAuthority` and read by all clients for HUD display
- The game over broadcast uses an RPC to notify all peers simultaneously

### Scene Context

The feature will be implemented and tested in `Assets/Scenes/01_Sandbox/CA2_NetworkTest.unity`. I intend for this scene to serve as the CA2 demonstration environment, with integration into the CA3 vertical slice to follow after submission. The CA1 rendering scene remains separate and will be the visual foundation for the CA3 vertical slice.

## Synchronisation Approach

### [Networked] Properties

The [Networked] properties will be used for **persistent state**. These are values that must be correct for any client at any point during the session:

| Property | Location | Justification |
|---|---|---|
| `float TimeRemaining` | `GameStateManager` | All clients need the current timer value to display the HUD countdown accurately |
| `GamePhase Phase` | `GameStateManager` | Clients need to know whether the game is waiting, running, or over to show the correct UI |
| `int Score` | `Player` | Each client needs every player's score to determine the winner at game over |

### RPCs

These are used for short lived or once off events.

| RPC | Source | Target | Justification |
|---|---|---|---|
| `Rpc_GameOver(PlayerRef winner)` | `StateAuthority` | `All` | Game over is a single event. A late joining client does not need to receive it retroactively. The `Phase` networked property handles the persistent game-over state for late joiners |

### Authority Assignment

| Object | StateAuthority | InputAuthority |
|---|---|---|
| `GameStateManager` | First player to join (Shared mode — whoever spawns the object owns it) | N/A, no player input drives game state directly |
| `Player` | The client that owns the player object | The local client on each machine |

Timer countdown and score changes are written only inside `HasStateAuthority`. All clients read these values for display only. This prevents where multiple clients writing to the same networked property simultaneously.

## Connection to CA3

After CA2 submission the `ca2/networking-game-state` branch will be merged into `main`. The room browser, authentication flow, player spawning, and game state system will form the networking infrastructure layer for the CA3 vertical slice. The CA3 scene will be built on top of the CA1 rendering foundations with this networking layer integrated.
