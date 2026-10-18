# Tycoon Metropolis VR — Multiplayer & Spatial Colocation Architecture Specification
**Document Version:** 1.0.0  
**Stack:** Unity 6000.6.5f1 | Netcode for GameObjects (NGO) 2.13.3 | Meta XR All SDK v207.0.1 | OpenXR 1.18.0  
**Status:** Approved Architectural Specification (Phase 7 Gate)

---

## 1. Executive Summary & Core Tenets

Tycoon Metropolis VR transitions from a single-headset prototype into a multi-headset mixed reality experience. To prevent desynchronization, cheating, and state corruption, the network architecture adheres to three non-negotiable principles:

1. **Server/Host Authority:** Clients never dictate game state. Clients send *intent* (commands); the Host validates all rules and economic constraints before committing state changes.
2. **Decoupled Spatial Alignment:** Physical table localization (Meta Spatial Anchors) and network state synchronization (NGO) are separate state machines. Network connectivity does not imply spatial alignment.
3. **Seat Identity vs Connection Index:** A player's table position (`SeatId` 0..3) is decoupled from their network transport connection ID (`OwnerClientId`) or join order.

---

## 2. Play Modes & Authority Matrix

| Play Mode | Transport / Stack | Turn Authority | Seat Allocation | Anchor Sharing | Production Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Solo vs AI** | Local Process | Local TurnMachine | Seat 0 = Human, Seats 1-3 = AI | Local Anchor only | **PRODUCTION READY** |
| **Pass-and-Play** | Local Process | Local TurnMachine (with Handover state) | Seats 0-3 = Configurable Humans | Local Anchor only | **PRODUCTION READY** |
| **Local Wi-Fi Colocation** | NGO (Unity Transport) + Meta Spatial Anchors | NGO Host (Seat 0 or Designated Host) | Negotiated SeatId (0..3) | Host exports anchor; Clients localize | **PHASE 7 ARCHITECTURE** |
| **Online Multiplayer** | NGO + Unity Relay / Lobby | Dedicated Server or NGO Host | Matchmaking Seat Reservation | Virtual Table Coordinate Space | **PHASE 7 SPECIFIED (LOBBY GATED)** |

---

## 3. Server-Authoritative Intent Protocol

### 3.1 Command Pipeline
All state-mutating actions follow the strict Command-Validation-Broadcast pipeline:

```
[Client Headset]                         [Authoritative Host]
       |                                          |
       |--- 1. SendIntentCommand(Args, TurnId) -->|
       |                                          |--- 2. Validate Preconditions
       |                                          |      (TurnId valid? Active player?
       |                                          |       Sufficient funds? Legal phase?)
       |                                          |
       |                                          |--- 3. Commit Domain Mutation
       |                                          |<-- 4. Reject / Exception if illegal
       |<-- 5. BroadcastStateUpdateRpc(Delta) ----|
       |                                          |
[Update Presentation / HUD]               [Update Presentation / HUD]
```

### 3.2 Defined RPC Intent Commands
Clients submit requests to the server via the following structured commands:

1. **`RequestRollDiceCommand(ulong clientId, int sessionGen, int turnPlayerIdx)`**
   - *Preconditions:* `turnPlayerIdx == currentTurnPlayer`, `CurrentPhase == TurnState.WaitingForRoll`.
   - *Host Logic:* Host generates cryptographically seeded dice values (1..6 each), calculates total, initiates physical or visual tumble, and advances state to `ResolvingMove`.
   - *Rejection:* Discarded if turn ID is stale or sender does not own the active seat.

2. **`RequestBuyPropertyCommand(ulong clientId, int sessionGen, int tileIdx)`**
   - *Preconditions:* `CurrentPhase == TurnState.BuyOrPassDecision`, `tileOwners[tileIdx] == -1`, `playerCash[seat] >= propertyCost`.
   - *Host Logic:* Deducts cost, assigns ownership, triggers deed stamp audio/haptics, advances turn to `TurnState.TurnCompleted`.

3. **`RequestPassPropertyCommand(ulong clientId, int sessionGen, int tileIdx)`**
   - *Preconditions:* `CurrentPhase == TurnState.BuyOrPassDecision`.
   - *Host Logic:* If house rule / setting dictates, enters `TurnState.AuctionInProgress`; otherwise advances to `TurnCompleted`.

4. **`RequestAuctionBidCommand(ulong clientId, int sessionGen, int bidAmount)`**
   - *Preconditions:* `CurrentPhase == TurnState.AuctionInProgress`, `bidAmount >= currentBid + minIncrement`, `playerCash[seat] >= bidAmount`.
   - *Host Logic:* Updates highest bidder and resets the auction countdown timer.

5. **`RequestUpgradePropertyCommand(ulong clientId, int sessionGen, int tileIdx)`**
   - *Preconditions:* `CanBuild(seat, tileIdx) == CanBuildResult.Success` (evaluated on Host using full domain rules: monopoly ownership, zero mortgages, even building, max level 5, liquid cash).
   - *Host Logic:* Deducts construction cost, increments `tileLevels[tileIdx]`, notifies clients.

---

## 4. Seat Identity and Session Lifecycle

### 4.1 Seat Allocation Model
- **Seats 0 to 3:**
  - Seat 0: South (Z: -0.59m, Yaw: 0°)
  - Seat 1: North (Z: +0.59m, Yaw: 180°)
  - Seat 2: West (X: -0.59m, Yaw: 90°)
  - Seat 3: East (X: +0.59m, Yaw: -90°)
- When a client connects via NGO, the Host inspects available seats and assigns a fixed `SeatId` via `ClientConnectedPayload`.
- Disconnecting players preserve their seat for a reconnection grace period (60 seconds). During grace, the seat is marked `Disconnected` and skips voluntary turns (or falls back to automated AI play).

### 4.2 Rejection of Client-Forged State
- Any packet attempting to directly specify `playerCash`, `tileLevels`, or `diceResult` is dropped immediately with an audit security warning log.
- All monetary operations route exclusively through `AuthoritativeEconomyEngine`.

---

## 5. Decoupled Spatial Anchor Colocation State Machine

Spatial colocation in Mixed Reality is distinct from network synchronization:

```
+-----------------------------------------------------------------+
| HOST WORKFLOW                                                   |
| 1. Create OVRSpatialAnchor at Table Center                      |
| 2. Save Anchor Locally & Retrieve UUID                          |
| 3. Share Anchor with Room / Client Headsets via Meta Cloud      |
| 4. Publish Anchor UUID via NGO Session Payload                  |
+-----------------------------------------------------------------+

+-----------------------------------------------------------------+
| CLIENT WORKFLOW                                                 |
| 1. Receive Anchor UUID via NGO Session Payload                  |
| 2. Query Meta Spatial Anchor API (OVRAnchor.FetchAnchorsAsync)  |
| 3. Localize Anchor in Physical Room                             |
|    - SUCCESS: Align virtual board origin with physical anchor   |
|    - FAILURE: Show visual tabletop manual calibration widget    |
| 4. Signal 'ColocationReady' to Host                             |
+-----------------------------------------------------------------+
```

### 5.1 Alignment Failure Fallback
If Meta Cloud Anchor localization fails or times out (e.g. poor room lighting, unsynced point clouds):
- The client receives a visual **Manual Alignment Widget**.
- The player pinches the board corners to align with their physical coffee table.
- Game state synchronization proceeds unaffected because table transform is purely local.

---

## 6. Deprecation of Legacy Raw UDP Broadcaster

- The prototype raw UDP broadcasting mechanism (`udpBroadcaster`, `udpListener` on port 47777) is permanently disabled and isolated by `enableLocalWiFiColocationSync = false` and `isNetworkStateSyncAuthorized = false`.
- All multi-headset synchronization routes through Netcode for GameObjects (NGO) RPCs and NetworkVariables.
- Unsolicited UDP packets on port 47777 are ignored and cannot mutate game state.
