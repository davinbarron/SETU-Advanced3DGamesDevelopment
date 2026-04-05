# CA2 Test Matrix

## Test Environment

- **Engine:** Unity 6 with Photon Fusion 2 (Shared Mode)
- **Clients:** One standalone build + Unity Editor, both on the same machine via Photon relay
- **Scene:** `Assets/Scenes/01_Sandbox/CA2_NetworkTest.unity`

---

## A1 — Baseline

| # | Scenario | Expected Result | Actual Result | Pass / Fail |
|---|---|---|---|---|
| 1 | Two clients authenticate via Unity Player Accounts and connect to the same named room | Both clients join the session, player objects spawn at spawn points, name tags visible above each character | Both clients connected and spawned correctly. Name tags displayed Unity account display names | Pass |
| 2 | Client joins a room that already has one player | Existing player visible to joining client, joining player visible to existing client | Both players visible to each other immediately on join | Pass |
| 3 | Player prefab not registered in NetworkProjectConfig | Runner.Spawn fails, object does not appear on remote client | Confirmed during early testing — fixed by registering prefab in NetworkProjectConfig | Pass (after fix) |

---

## Option C — Networked Game State (Timer and Phase)

| # | Scenario | Expected Result | Actual Result | Pass / Fail |
|---|---|---|---|---|
| 4 | One client in room — waiting state | HUD shows "Waiting for players..." on both clients, timer shows 0:00 | Single clients in a room shows waiting state | Pass |
| 5 | Second client joins — countdown starts | Both clients transition to "Get Ready!" phase simultaneously, 5-second countdown begins | Both HUDs updated to countdown phase at the same tick | Pass |
| 6 | Player leaves during countdown | Countdown aborts on both clients, both return to "Waiting for players..." | Phase reset to Waiting correctly on both clients | Pass |
| 7 | Countdown reaches zero | Both clients transition to "Round in progress", 60-second match timer begins | Phase transition and timer start confirmed on both clients simultaneously | Pass |
| 8 | Player leaves during playing phase | Match aborts on both clients, both return to Waiting state | Abort triggered correctly via `TryAbortRound` on StateAuthority | Pass |
| 9 | Match timer reaches zero | Both clients transition to GameOver simultaneously, final scoreboard appears | GameOver phase triggered on StateAuthority, `Rpc_GameOver` propagated to all clients | Pass |
| 10 | State changes written by non-authority client | No state change occurs — guard prevents write | Confirmed via `HasStateAuthority` guard in `FixedUpdateNetwork` | Pass |

---

## Option B — Networked Score Orbs (Pickup / Interaction)

| # | Scenario | Expected Result | Actual Result | Pass / Fail |
|---|---|---|---|---|
| 11 | Player walks into score orb | Orb disappears on both clients simultaneously, collecting player's score increments by 10 | Orb hidden via `ChangeDetector` on all clients, score updated correctly | Pass |
| 12 | Two players attempt to collect same orb simultaneously | Only one player receives the score, orb disappears once | `HasStateAuthority` guard on `FixedUpdateNetwork` prevents double collection | Pass |
| 13 | Orb respawns after 10 seconds | Orb reappears at same position on both clients after respawn timer expires | Respawn confirmed at correct interval on both clients | Pass |
| 14 | Score orb collected outside Playing phase | Orb cannot be collected during Waiting, Countdown or GameOver | Phase check in `FixedUpdateNetwork` prevents collection outside Playing | Pass |
| 15 | Score panel shows correct values for all players | Each player's name and score displayed correctly on left-side HUD during Playing phase | Scores updated in real time from replicated `[Networked] int Score` property | Pass |

---

## Win State and Rankings

| # | Scenario | Expected Result | Actual Result | Pass / Fail |
|---|---|---|---|---|
| 16 | Timer expires — winner determined by highest score | Player with highest score shown as 1st on both clients' scoreboards | Correct ranking displayed on both clients via `Rpc_GameOver` | Pass |
| 17 | Players have equal scores at game over | Both players shown in scoreboard — order determined by iteration, no crash | Equal score case handled without error | Pass |
| 18 | Scoreboard displays for 5 seconds then switches to vote panel | Vote panel appears on both clients after 5-second delay | Transition confirmed at correct timing on both clients | Pass |

---

## Rematch Vote System

| # | Scenario | Expected Result | Actual Result | Pass / Fail |
|---|---|---|---|---|
| 19 | One player votes rematch (2-player session, majority = 2) | Vote counter shows 1/2, rematch does not start yet | `RematchVotes` incremented on StateAuthority, displayed correctly on both clients | Pass |
| 20 | Both players vote rematch | Scores reset to zero, new countdown begins on both clients | `Rpc_StartRematch` starts then phase resets to Waiting, `TryStartRound` triggers new countdown | Pass |
| 21 | Player attempts to vote twice | Second vote ignored | `_voters` HashSet prevents duplicate votes on StateAuthority | Pass |
| 22 | Player clicks Leave Room | Local client disconnects and lobby browser reappears | Local `Runner.Shutdown()` called, other client remains in room | Pass |

---

## RPC Correctness

| # | Scenario | Expected Result | Actual Result | Pass / Fail |
|---|---|---|---|---|
| 23 | Player presses emote key (1/2/3) | Emote bubble appears above that player on both clients simultaneously | `Rpc_PlayEmote` from InputAuthority to All, bubble displayed on both | Pass |
| 24 | `Rpc_CastVote` called by non-StateAuthority client | Vote counted correctly on StateAuthority, RematchVotes incremented | RpcSources.All to RpcTargets.StateAuthority confirmed working | Pass |
| 25 | `Rpc_GrantScore` called when player object not found | No crash, score not awarded | Null check on `Runner.GetPlayerObject` handles missing object | Pass |

---

## Known Issues

| # | Issue | Root Cause | Planned Fix (CA3) |
|---|---|---|---|
| KI-1 | When the host client (StateAuthority for `GameStateManager`) leaves the room, the remaining client becomes stuck — phase does not reset and the session cannot be restarted without both clients leaving and rejoining | In Fusion Shared Mode, `StateAuthority` over scene objects is assigned to the first client to join. When that client disconnects, no other client automatically inherits authority over `GameStateManager`, so `TryAbortRound` never starts and the phase is permanently frozen | Implement `StateAuthority` migration on `GameStateManager`|
| KI-2 | If the original host rejoins after leaving, their new runner instance conflicts with the existing session state, causing both clients to become unresponsive | The rejoining client spawns a new `GameStateManager` instance or inherits a stale one with no valid authority chain | Resolved by KI-1 fix where proper authority migration prevents the stale state that causes the conflict on rejoin |