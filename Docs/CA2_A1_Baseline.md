# A1: CA2 Setup and Spawn Baseline

## Overview

Before implementing the CA2 features, I have a working two-client Fusion 2 baseline in `Assets/Scenes/01_Sandbox/CA2_NetworkTest.unity`. Two clients can connect to the same named room using the lobby browser, authenticate through Unity Player Accounts and Photon Custom Auth, and spawn as independent KCC player characters with correct input and state authority assignment.

## NetworkRunner Configuration

The `NetworkRunner` is instantiated at runtime from a prefab by `NetworkManager` after authentication completes. It is configured programmatically with `GameMode.Shared`, a player-chosen `SessionName`, and `NetworkSceneManagerDefault`.

See: `Docs/Screenshots/CA2/NetworkRunner.png`

## NetworkObject Prefab

The player prefab has a `NetworkObject` component at its root. Each client spawns its own player object through `PlayerSpawner` when `player == Runner.LocalPlayer` triggers in `IPlayerJoined`, passing itself as the `inputAuthority` argument to `Runner.Spawn()`. This means the spawning client holds both `StateAuthority` and `InputAuthority` over its own player object.

See: `Docs/Screenshots/CA2/NetworkObjectPlayer.png`

## NetworkProjectConfig — Registered Prefabs

All networked prefabs are registered in `Fusion → Network Project Config` so Fusion can resolve them by GUID across all connected clients. Prefabs not registered here cannot be spawned in the network.

See: `Docs/Screenshots/CA2/NetworkProjectConfig.png`

## Two-Client Baseline Confirmation

Both clients connect to the same room, authenticate successfully, and spawn as separate player characters visible to each other.

See: `Docs/Screenshots/CA2/Players in the same room.png`

## Authority Summary

- `StateAuthority` over each player object is held by the client that spawned it
- `InputAuthority` is assigned to the same client using the `PlayerRef` argument in `Runner.Spawn()`
- Remote clients receive replicated state and render it but cannot write to networked properties on objects they do not own
