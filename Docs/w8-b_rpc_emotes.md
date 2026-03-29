# Week 8-B: RPC Emote System

## Goal

Create a Remote Procedure Call (RPC) so players can trigger an emote visible to all connected clients.

## What Was Completed

### RPC vs Networked Variables

The key distinction demonstrated in this lab:
- `[Networked]` properties are for persistent state. These are values that need to exist and be correct for any client that joins at any point (e.g. the player's name from the last lab)
- RPCs are for short-lived temporary events. These are things that happen once and don't need to be replayed for late-joining clients (e.g. an emote bubble that appears briefly and disappears or an explosion)

The emote system deliberately uses an RPC rather than a networked variable because there is no state to preserve. A client joining mid-emote does not need to see it.

### Scripts Modified

`GameplayInput.cs` — added three emote button constants alongside the existing `JUMP_BUTTON`:

- `EMOTE_WAVE` mapped to key 1
- `EMOTE_CHEER` mapped to key 2
- `EMOTE_TAUNT` mapped to key 3

`PlayerInput.cs` — added emote key polling in `BeforeUpdate()` using `keyboard.digit1Key`, `digit2Key`, `digit3Key` to match the existing new Input System pattern already used for movement and jump

`Player.cs` — added emote detection in `FixedUpdateNetwork` and the RPC method:

- `HasInputAuthority` guard ensures only the owning player detects the key press and fires the RPC
- `WasPressed` compares current vs previous input to catch a single press rather than a held key
- `Rpc_PlayEmote` is tagged `[Rpc(RpcSources.InputAuthority, RpcTargets.All)]` fires from the owning player and executes on every peer's copy of that object
- On execution calls `NameTag.ShowEmote(emote)` which handles the visual

`PlayerNameTag.cs` — added emote bubble alongside the existing name tag:

- `BuildEmoteBubble()` constructs a second world-space canvas above the name tag, built entirely in code
- `ShowEmote(EmoteType emote)` sets the bubble text and records a hide time
- `Render()` checks the hide timer each frame and deactivates the bubble when it expires — no coroutine needed

## Evidence

- Emote bubble visible above player: `Docs/Screenshots/W8-B/Emote.png`
- Emote bubble visible on player from second client: `Docs/Screenshots/W8-B/Friend Emote.png`
- Console showing no errors during emote RPC: `Docs/Screenshots/W8-B/Console Log.png`
