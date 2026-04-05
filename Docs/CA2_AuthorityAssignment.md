# CA2 Authority Assignment

## Overview

The CA2 networking implementation uses **Photon Fusion 2 in Shared Mode**, where there is no dedicated server. Authority over each `NetworkObject` is owned by the client that spawns it.

## StateAuthority Assignment

**StateAuthority is assigned to the client that calls `Runner.Spawn()`.**

- **Player objects**: Each client spawns its own player in `GameplayManager.Spawned()` via:
  ```csharp
  NetworkObject player = Runner.Spawn(PlayerPrefab, position, rotation, playerRef);
  ```
  Therefore, **Client A owns StateAuthority over its player; Client B owns StateAuthority over its player.**

- **GameStateManager**: Instantiated when the first client joins the session. **That client holds StateAuthority** over the timer (`TimeRemaining`), game phase (`Phase`), and winner state. Confirmed in `GameStateManager.FixedUpdateNetwork()` (line 67):
  ```csharp
  if (!HasStateAuthority) return;  // Only authority writes state
  ```

- **Score Orbs**: Owned by whoever spawns them. Only the authority client updates `IsCollected` and respawn timers.

## InputAuthority Assignment

**InputAuthority is set by passing `PlayerRef` to `Runner.Spawn()` — the fourth parameter:**

```csharp
Runner.Spawn(PlayerPrefab, position, rotation, playerRef);  // playerRef becomes InputAuthority
```

This means each player's object has `InputAuthority` matching the client that spawned it. Only that client detects input (e.g., keystroke, emote command) in `Player.FixedUpdateNetwork()` (line 79):

```csharp
if (HasInputAuthority)  // Only this client reads local input
{
    Rpc_PlayEmote(EmoteType.Wave);  // Starts emote RPC
}
```

## Authority Guard Pattern

All state writes are protected:

```csharp
// Writer (StateAuthority only)
if (!HasStateAuthority) return;
Score += amount;

// Reader (any client can read)
int playerScore = player.Score;  // All clients can read this
```

Remote clients receive replicated `[Networked]` properties but cannot write to them which helps to prevent desync.
