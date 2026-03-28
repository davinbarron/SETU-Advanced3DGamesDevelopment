# Week 7-B: Unity Authentication Integration

## Goal

Integrate Unity Authentication into the Fusion multiplayer session so that anonymous access is disabled and only authenticated players can connect to Photon rooms.

## What Was Completed

### Unity Services Setup
- Installed the Unity Authentication package via Package Manager → Services → Authentication
- Linked the Unity project to a Unity Services organisation and project via Project Settings → Services
- Added the Unity Player Account identity provider via the Unity Dashboard
- Selected PC as the target platform for the provider

### Photon Dashboard Configuration
- Disabled anonymous client access on the Photon dashboard for the app
- Configured Custom Authentication on the Photon dashboard pointing to the existing Azure Function from W6-B

### Scripts Implemented

`UnityServiceManager.cs` — Services initialisation and sign-in

- Initialises Unity Services via `UnityServices.InitializeAsync()`
- Subscribes to `PlayerAccountService.Instance.SignedIn` event
- Calls `PlayerAccountService.Instance.StartSignInAsync()` which opens the browser for Unity Player Account login
- On sign-in callback, exchanges the Player Account access token with `AuthenticationService.Instance.SignInWithUnityAsync()` to get a Fusion-compatible token
- Static `IsAuthenticated`, `PlayerId`, and `AccessToken` allows them to be used in other scripts
- `OnAuthenticated` event when the full auth completes

`NetworkManager.cs` — Starting the Fusion session after authentication

- Subscribes to `UnityServiceManager.OnAuthenticated`
- On auth complete, builds `AuthenticationValues` with `AuthType = Custom`, `UserId`, and auth parameters `id` and `token` matching what the Azure Function
- Calls `Runner.StartGame()` with `GameMode.Shared`, fixed `SessionName = "Room_01"`, and the populated `AuthValues`

`GameSpawner.cs` — `SimulationBehaviour` implementing `IPlayerJoined` and `IPlayerLeft`:

- Placed on the NetworkRunner prefab (required — scene GameObjects do not receive `IPlayerJoined` callbacks in Shared mode)
- On `PlayerJoined` spawns the local player prefab at a scene `SpawnPoint` selected by player index
- On `PlayerLeft` uses `PlayerManager<Player>` to locate and despawn the disconnected player's object

## Evidence

- Unity Dashboard provider setup: `Docs/Screenshots/W7-B/Unity Dashboard.png`
- Photon custom auth configured: `Docs/Screenshots/W7-B/Anonymous Disabled.png`
- Console showing successful auth and room join: `Docs/Screenshots/W7-B/Game Log.png`
- Two players spawned in same room: `Docs/Screenshots/W7-B/Players in Game.png`
