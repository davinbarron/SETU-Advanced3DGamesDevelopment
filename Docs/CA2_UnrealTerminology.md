# A3: Unreal Engine 5 Replication Terminology — Comparative Note

## 1. Authority

In Fusion’s Shared Mode, authority is distributed per object. A client holds StateAuthority to write networked state and InputAuthority to provide the local input driving that object.

Unreal Engine 5 uses a centralised server-authoritative model:

- The Server: Holds authority over all actors. Only the server can write to replicated state.

- Owning Client: The client that owns a specific actor. This is the Unreal equivalent of Fusion’s InputAuthority.

- Simulated Proxy: Other clients receiving state updates passively. These function like Fusion’s proxies, which read but cannot write networked data.

Fusion allows any peer to hold authority over an object, Unreal mandates that the server remains the ultimate source of truth for all replicated actors.

---

## 2. Persistence - Syncing State

Fusion’s [Networked] attribute marks properties for synchronisation, often using a ChangeDetector to handle logic when values update.

In Unreal, the equivalent is a Replicated UPROPERTY. To mirror Fusion’s detection pattern, Unreal uses the ReplicatedUsing specifier to trigger an OnRep callback function. Both engines share the same fundamental principle where a persistent state that all clients need access to must be stored in a replicated property rather than an RPC.

---

## 3. RPC - Short-Lived Events

RPCs in both engines handle short-lived events that do not require persistent state.

- InputAuthority to All (Multicast): In Fusion, this sends an event from the owner to everyone. In Unreal, this is a NetMulticast, which is called by the server to broadcast to all clients.

- All to StateAuthority (Server RPC): Fusion uses this to send data from a peer to the object owner. Unreal uses a Server RPC (UFUNCTION(Server)), where a client requests the server to perform an authoritative action.

- StateAuthority to Owning Client: This maps to Unreal’s Client RPC, specifically targeting the individual client that owns the actor.

---

## 4. Key Difference — Authority Model

The most meaningful difference is that Fusion’s Shared Mode distributes authority across peers, whereas Unreal’s standard model centralizes it on the server. In Unreal, the architecture is structurally trustless. A client cannot write to a replicated variable directly; they must send a Server RPC requesting a change, and only the server performs the write.

In Fusion Shared Mode, authority is a permission granted to a peer. The engine relies on the developer to enforce HasStateAuthority guards manually. If a guard is omitted, a client might attempt to send an RPC or modify state they do not own, leading to local simulation errors. Unreal’s model enforces strict client-server separation by design, while Fusion Shared Mode requires more deliberate authority discipline to ensure peers do not conflict.
