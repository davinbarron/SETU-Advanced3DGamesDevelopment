# Week 8-A: Lobby System and Room Browser

## Goal

Create a lobby system so players can create and join named rooms before entering the game, along with a networked player name tag that displays each player's name above their character.

## What Was Completed

- Modified `NetworkManager` to connect to a Fusion lobby via `JoinSessionLobby` after authentication instead of joining a fixed room directly
- Once the lobby connection is made the runner is handed off to `LobbyManager` which takes over room creation and joining
- Players can set a custom display name in the lobby UI — defaults to the Unity Player Account display name fetched via `GetPlayerNameAsync()`
- The chosen name is stored as a static property on `UnityServiceManager` so it is accessible at spawn time

### Scripts Implemented

`UnityServiceManager.cs` — updated from W7-B

- Added `GetPlayerNameAsync()` call after sign-in to fetch the Unity Player Account display name
- Stored as static `PlayerName` property alongside the existing `PlayerId` and `AccessToken`
- `PlayerName` has a public setter so `RoomBrowserUI` can update it if the player types a custom name

`NetworkManager.cs` — updated from W7-B

- `OnAuthComplete` now calls `JoinSessionLobby` instead of `StartGame`
- Passes the runner to `LobbyManager.Initialise()` once the lobby connection succeeds
- Exposes `StartSession(string roomName)` which `LobbyManager` calls when the player creates or joins a room

`LobbyManager.cs` — new

- Singleton, receives the runner from `NetworkManager` via `Initialise()`
- Adds itself as an `INetworkRunnerCallbacks` listener to receive `OnSessionListUpdated`
- Fires `OnRoomsUpdated` event with the latest session list for `RoomBrowserUI`
- `CreateRoom(name)` and `JoinRoom(name)` both delegate to `NetworkManager.StartSession()`

`RoomBrowserUI.cs` — new

- Built entirely in code
- Subscribes to `LobbyManager.OnLobbyReady` and `LobbyManager.OnRoomsUpdated`
- Shows a player name field pre-filled with `UnityServiceManager.PlayerName`, editable before joining
- Room list rebuilt on each session list update using `RoomListItem.Create()`

`RoomListItem.cs` — new

- Static factory pattern where each room entry is built entirely in code
- Displays room name, player count, and a Join button
- Join button disabled when the session is full or closed

`PlayerNameTag.cs` — new

- `[Networked] NetworkString<_32> NickName` propagated to all peers automatically
- On `Spawned` with `HasStateAuthority`, sets `NickName` from `UnityServiceManager.PlayerName`
- `ChangeDetector` in `Render()` detects name changes and updates the world-space label
- World-space canvas built in code — floats above the player and faces the camera each frame

## Evidence

- Lobby UI before authentication: `Docs/Screenshots/W8-A/Room Browsing.png`
- Room created and visible in list for second client: `Docs/Screenshots/W8-A/Room browser after login.png`
- Two players in the same room with name tags: `Docs/Screenshots/W8-A/Players in same room.png`
- Console showing lobby connect and room join: `Docs/Screenshots/W8-A/Console Log.png`
