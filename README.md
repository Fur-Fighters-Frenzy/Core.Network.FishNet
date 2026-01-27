# Core.Network.FishNet

FishNet adapter for **Core.Network**.

This package bridges FishNet networking to the `Core.Network` transport interfaces (`INetClient` / `INetServer`) and unified channels (`ChannelKind`), so higher-level code can remain transport-agnostic while using FishNet under the hood.

* [Core.Network (base)](https://github.com/Fur-Fighters-Frenzy/Core.Network)
* [FishNet](https://github.com/FirstGearGames/FishNet)

> **Status:** WIP

---

## What’s included

* `FishBridge` (partial): client/server bridge implemented on top of FishNet messaging/RPC
* `FishChannelMap`: maps FishNet reliability options to `ChannelKind`

Notes:

* `ChannelKind.Sequenced` is mapped to best-effort behavior on FishNet (transport-dependent).

---

## Requirements

* `Core.Network`
* FishNet

---

## Scripting define symbol

This adapter is compiled only when:

* `FISHNET_ENABLED`

---

## Usage (high-level)

```csharp
INetServer server = /* FishBridge (server side) */;
server.OnClientConnected += pid => { /* ... */ };
server.OnClientMessage += (pid, data, ch) => { /* decode envelope/DTO */ };
```

---

# Part of the Core Project

This package is part of the **Core** project, which consists of multiple Unity packages.
See the full project here: [Core](https://github.com/Fur-Fighters-Frenzy/Core)

---