# Contract decision — no external API or message contract

**Decision (recorded):** UltraBlade 2 exposes **no external API and no external message contract**. The game is a
local, single-player Unity player build. It publishes no network endpoint, event stream, queue, webhook, plugin SDK,
CLI/IPC contract or save format read by another product. The contract-artefact requirement tracked as **UB2-D5** in
[DEFERRED.md](../DEFERRED.md) is closed as **not applicable**. No contract artefact or contract guide is generated.

## 1. What counts as external

An interface is external when something outside the player process consumes it or when it is published for another
team, product or service. Examples that would trigger this decision:

- an HTTP or WebSocket request/response API;
- a multiplayer or netcode protocol;
- cloud saves, accounts, authentication or entitlement APIs;
- telemetry ingestion, remote configuration or live-ops messages;
- a mod/plugin SDK or an embedded scripting API;
- a CLI, socket or IPC surface used by another process;
- a save or settings format published for another product to read;
- a web/admin surface.

These are **not** external contracts, even though the game owns them:

- internal C# types, private methods and assembly structure;
- Unity scenes, prefabs, materials, animation controllers and serialized assets;
- the `PlayerPrefs` settings keys and the league save documented in [ROLLBACK.md](./ROLLBACK.md);
- the build reports under `artifacts/`, the check scripts, and the Gypsy toolpack/slice manifests. They are
  repository tooling and evidence, not a game API.

Internal shapes may change as the game develops. The data layers follow the expand-only and rollback rules in
[ROLLBACK.md](./ROLLBACK.md); that is a data-safety policy, not a published API promise. UI behaviour is governed by
[UI-STANDARD.md](./UI-STANDARD.md).

## 2. Why the requirement is not applicable

- There is no consumer outside the game process, so there is nothing to version, test for compatibility or deprecate.
- A contract artefact would be empty, or would describe internal C# and Unity formats as if they were promises. An
  empty or invented artefact is exactly the kind of pretend feature the project rules forbid.
- The repository has no server, deployment, public endpoint or published schema. Nothing in the build can answer a
  contract request.
- The current player scripts contain no `UnityWebRequest`, `HttpClient`, socket, WebSocket or equivalent network
  call. The only Android 'API' references are Unity's minimum SDK and graphics API settings in
  [BuildUBlade2.cs](../unity/UBlade2/Assets/Editor/BuildUBlade2.cs); they are platform configuration, not a game API.

This is a decision about the present build, not a prediction that the game can never have an external interface.

## 3. What would reopen it

Reopen **UB2-D5** as soon as any of these is proposed or added:

- a network request, response, event or subscription that a client or service consumes;
- multiplayer, matchmaking, lobbies, relays or a netcode wire format;
- accounts, cloud saves, entitlements or authentication;
- telemetry, analytics ingestion, remote config or live-ops messaging;
- a mod/plugin SDK, embedded scripting API or documented extension point;
- a CLI, socket, named pipe or IPC surface used outside the player process;
- a save or settings format another product reads;
- a web or admin surface.

When reopened, the contract artefact must state: the owner, version, endpoints/messages, authentication and security
rules, compatibility and deprecation policy, error behaviour, and the tests or evidence that keep it true. It must
be generated from or checked against the real surface; a hand-written description that drifts is worse than none.
Until that exists, do not add a placeholder contract or a 'coming soon' API.

## 4. Review

Re-check this decision at the start of any production slice that adds networking, multiplayer, cloud data, plugins,
telemetry, an external tool boundary, or a save format for another product. If none is added, the decision stands
and **UB2-D5** remains closed as not applicable.

Recording a later reopen is a records edit; this document only records the decision for the current single-player
build.
