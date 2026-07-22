<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# SD Fork Gameplay Map

## Purpose

Use this file to route a Space Dream gameplay task to the correct code assembly, fork namespace, resource folder, localization file, and validation step before editing.

This map is for the current SD repository shape. SD is not a plain upstream SS14 tree: it contains upstream-style `Content.*` assemblies, fork-specific `_SD` overlays, and inherited prefixed content such as `_DV`, `Corvax`, and `Nyanotrasen`.

## Fast Triage

1. Name the player-facing mechanic, admin behavior, UI, entity family, or resource family.
2. Search for the mechanic in this order:
   - SD-local code and data: `Content.Shared/_SD/`, `Content.Server/_SD/`, `Content.Client/_SD/`, `Resources/Prototypes/_SD/`, `Resources/Locale/*/_SD/`, `Resources/Maps/_SD/`, `Resources/Textures/_SD/`, `Resources/Audio/_SD/`.
   - Other inherited fork overlays when the feature clearly belongs there: `_DV`, `Corvax`, `Nyanotrasen`, or similar prefixed folders.
   - Unprefixed upstream-style domains under `Content.Shared/`, `Content.Server/`, `Content.Client/`, and `Resources/`.
3. Start from `Content.Shared/<Domain>/` or `Content.Shared/_SD/<Domain>/` when the feature crosses networking, prediction, BUI state/messages, actions, appearance, or client/server event contracts.
4. Start from `Content.Server/<Domain>/` or `Content.Server/_SD/<Domain>/` when the feature is authority-only: spawning, objectives, round rules, admin commands, persistence, or server-side damage/effects.
5. Start from `Content.Client/<Domain>/` or `Content.Client/_SD/<Domain>/` when the task is visual-only: XAML, BUI windows, overlays, sprites, local animation, input presentation, or UI polish.
6. Immediately pair code with data: check `Resources/Prototypes/`, `Resources/Locale/en-US/`, `Resources/Locale/ru-RU/`, `Resources/Textures/`, `Resources/Audio/`, `Resources/Maps/`, and `Resources/ServerInfo/` for the same domain or prefix.
7. If prototypes, maps, or serialized component fields change, search for every prototype/component usage before renaming fields or IDs.
8. Check `Content.Tests/`, `Content.IntegrationTests/`, and any domain-specific test helpers when behavior changes are risky.

## Current Repository Shape

Core gameplay assemblies:

- `Content.Shared/`: upstream-style shared components, events, BUI contracts, predicted logic, shared helper types, and shared gameplay primitives.
- `Content.Server/`: authoritative simulation, spawning, round rules, admin commands, persistence hooks, server-only effects, and game-state mutation.
- `Content.Client/`: visuals, XAML, BUIs, overlays, local presentation, input affordances, and client-only effects.
- `Content.Tests/` and `Content.IntegrationTests/`: content and integration validation.

Database and tooling assemblies:

- `Content.Server.Database/` and `Content.Shared.Database/`: database models, persistence-facing types, and shared DB contracts. Do not hide normal gameplay logic here unless it really belongs to persistence.
- `Content.Tools/`, `Content.YAMLLinter/`, `Content.MapRenderer/`, `Content.Packaging/`, `Content.Replay/`, and similar top-level projects are tooling/runtime support, not normal gameplay feature homes.

Resource roots:

- `Resources/Prototypes/`: entities, components, actions, roles, rules, datasets, sound collections, guidebook prototypes, and other YAML content.
- `Resources/Locale/en-US/` and `Resources/Locale/ru-RU/`: player-facing and admin-facing FTL text. SD often needs both English and Russian strings.
- `Resources/Locale/en-US/ss14-ru/`: inherited/compatibility localization area; inspect it before creating duplicate strings for older translated prototype paths.
- `Resources/Textures/`: RSI sprites and texture assets.
- `Resources/Audio/`: sound assets and audio content.
- `Resources/Maps/`: station maps, shuttles, generated maps, and SD-specific domain maps.
- `Resources/ServerInfo/`: guidebook/server-info XML and content documentation.

## Assembly Ownership Rules

Use shared when the client and server both need the type or event:

- Components serialized in prototypes and read on both sides.
- BUI keys, BUI state, BUI messages, action events, network events, appearance data, predicted events, and shared enum/state definitions.
- Cross-assembly helpers that must stay deterministic or be used by both client and server.

Use server when the server owns the truth:

- Spawning, deletion, entity transformation, objective completion, round-end checks, admin commands, persistence, antagonists, station events, damage application, and access/security mutation.
- Server-only popups/audio triggers may still need shared events or shared component data when client presentation depends on them.

Use client when it only changes presentation:

- XAML, BUI windows, overlays, sprite visualizers, local animation, tooltips, local sounds, local input affordances, UI formatting, and purely visual effects.

## Namespace and Prefix Priority

Prefer the smallest correct layer:

1. `_SD` for SD-only gameplay, local balance, custom UI, local admin commands, and fork-local adult/content extensions.
2. Inherited prefixed areas such as `_DV`, `Corvax`, or `Nyanotrasen` when the existing feature is already there.
3. Unprefixed `Content.*` and `Resources/*` for upstream-style systems that are not fork-local.

Do not move an existing feature into `_SD` just because a task is requested for SD. Follow the existing file family unless the task is explicitly a fork-local extension.

## Common Cross-Assembly Shapes

- Shared component plus server system plus client visualizer.
- Shared action event plus server validation plus client popup/audio/visual feedback.
- Shared BUI key/state/messages plus server BUI handler plus client BUI/window XAML.
- Server-only rule/objective system backed by prototypes and localized text.
- Shared serialized component plus prototype data plus client sprite/appearance handling.

## Common Domain Clusters

### Character, body, damage, and status

Use these for body parts, species, mobs, health, equipment, restraints, movement modifiers, status effects, metabolism, surgery-like interactions, and medical state:

- `Body`, `Mobs`, `Humanoid`, `Hands`, `Inventory`, `Clothing`, `Damage`, `Medical`, `Species`, `Metabolism`, `Cuffs`, `Stunnable`, `Movement`, `StatusEffect`, `Drowsiness`, `Drunk`, `Drugs`, `Standing`.

### Item and interaction flow

Use these for verbs, held items, interaction gating, tool use, throwing, storage, equipment actions, construction interactions, and do-afters:

- `Actions`, `Interaction`, `Item`, `Storage`, `Tools`, `Throwing`, `Prying`, `Resist`, `Wieldable`, `Placeable`, `DoAfter`, `DragDrop`, `Construction`, `Hands`, `Inventory`, `Charges`, `Containers`.

### Station infrastructure

Use these for machines, wiring, atmospherics, power distribution, construction graphs, telecom/device links, shuttles, and station-level services:

- `Atmos`, `Power`, `SMES`, `APC`, `Machines`, `DeviceNetwork`, `DeviceLinking`, `NodeContainer`, `Construction`, `Wires`, `Shuttles`, `Station`, `Gravity`, `Holopad`, `Communications`, `Disposal`, `Doors`, `Cargo`.

### Roundflow, roles, objectives, and administration

Use these for role assignment, antagonist logic, objectives, round lifecycle, player sessions, ghost roles, admin-facing behavior, and station events:

- `Objectives`, `Roles`, `Antag`, `GameTicking`, `NukeOps`, `Revolutionary`, `Thief`, `Traitor`, `Administration`, `Preferences`, `Players`, `Respawn`, `Ghost`, `StationEvents`, `Mind`, `Jobs`, `LateJoin`.

### Presentation and feedback

Use these for visible feedback, overlays, alert state, guidebook content, UI state, sprite appearance, audio, and local-only polish:

- `Audio`, `Effects`, `Popups`, `Sprite`, `Alert`, `Alerts`, `StatusEffect`, `Guidebook`, `Overlays`, `UserInterface`, `Appearance`, `Animations`, `ContextMenu`, `Cooldown`, `Fullscreen`, `Outline`, `Viewport`, `HealthAnalyzer`.

### SD-local systems

Inspect `_SD` first for these systems. Current SD-local anchors include:

- `Content.Shared/_SD/Antag/`, `Content.Server/_SD/Antag/`: soft-command and related antag job helpers.
- `Content.Shared/_SD/Blocking/`, `Content.Shared/_SD/Movement/`: movement and blocking extensions such as `SharedMoverController.SD.cs`.
- `Content.Shared/_SD/CCVar/`, `Content.Shared/_SD/Input/`, `Content.Client/_SD/Input/`, `Content.Client/_SD/Options/`: SD cvars, input, and options UI.
- `Content.Shared/_SD/Vibrator/`, `Content.Server/_SD/Vibrator/`, `Content.Server/_SD/Arousal/`: adult-content systems and related server authority.
- `Content.Server/_SD/Power/`: SD power-side extensions when present.
- `Resources/Prototypes/_SD/`: alerts, catalog, device linking, entities (`Clothing`, `Objects`, `Structures`), loadouts, sound collections, status icons, voice.
- `Resources/Locale/en-US/_SD/` and `Resources/Locale/ru-RU/_SD/`: matching player-facing and UI strings.
- `Resources/Textures/_SD/`, `Resources/Audio/_SD/`: SD-only sprites and audio when present.

## Server-Heavy Hotspots

These areas are usually server-authoritative. Verify peers before assuming they are server-only:

- `Acz`, `Afk`, `Announcements`, `Chunking`, `Codewords`, `Connection`, `CPUJob`, `Database`, `Discord`.
- `GameTicking`, `StationEvents`, `Objectives`, `Roles`, `Antag`, `Jobs`, `Ghost`, `Respawn`, `Station`, `Shuttles`.
- `PowerSink`, `RandomAppearance`, `RandomMetadata`, `RequiresGrid`, `Screens`, `ServerInfo`, `ServerUpdates`.
- `Spawners`, `Tesla`, `VentHorde`, `Vocalization`, `VoiceTrigger`, `KillTracking`, `GuideGenerator`.
- SD-local server systems under `_SD/` such as `Antag`, `Arousal`, `Power`, and `Vibrator`.

If a task lands here, expect server validation and state authority. Add shared/client code only for contracts or presentation.

## Client-Heavy Hotspots

These areas are usually client presentation. Verify peers before assuming they are client-only:

- `Alerts`, `Animations`, `Changelog`, `Clickable`, `CloningConsole`, `ContextMenu`, `Cooldown`, `Credits`.
- `DamageState`, `DebugMon`, `FeedbackPopup`, `FlavorText`, `Fullscreen`, `Gameplay`, `Graphics`, `HealthAnalyzer`.
- `Interactable`, `Items`, `Kudzu`, `LateJoin`, `Launcher`, `Lobby`, `MainMenu`, `Markers`, `Message`.
- `NetworkConfigurator`, `Options`, `Orbit`, `Outline`, `Playtime`, `Replay`, `Resources`, `RichText`, `Screenshot`, `Stylesheets`, `Viewport`.
- SD-local UI folders such as `_SD/Input` and `_SD/Options`, plus any XAML-backed console/PDA UI.

If a task only touches these areas, avoid introducing new server/shared dependencies unless a real state contract is missing.

## Shared Utility Buckets

Some buckets are shared-first even when they do not map cleanly to a server/client folder pair:

- `ActionBlocker`, `APC`, `Blocking`, `Climbing`, `ComponentTable`, `DetailExaminable`, `Execution`, `Friction`.
- `Glue`, `HealthExaminable`, `Internals`, `Metabolism`, `Prototypes`, `Repairable`, `Rotatable`, `Spawning`.
- `StatusEffect`, `Timing`, `Warps`, `Whistle`, `Appearance`, `DoAfter`, `EntityTable`, `EntityList`, `BUI` state/message definitions.

Treat these as shared primitives that other domains compose. Keep them generic and avoid hiding feature-specific logic inside them.

## Code-To-Data Pairing Rules

- If you add or rename a serialized component field, search all YAML prototypes and maps using that component.
- If you add a new popup, action, UI label, admin command string, examine text, objective text, or machine message, add FTL for both `en-US` and `ru-RU` when the feature is SD-facing.
- If you touch inherited localization under `Resources/Locale/en-US/ss14-ru/`, verify whether the matching `_SD` or upstream locale already exists before duplicating keys.
- If you add reusable audio, prefer a sound collection prototype or existing resource pattern instead of hardcoding file paths.
- If you add a reusable visual state, decide whether it belongs in a shared appearance component, a client visualizer, a sprite RSI, or all three.
- If you add a new prototype family, keep parents/base prototypes in `base.yml` when that pattern already exists and put variants in neighboring files.
- If the mechanic has maps, check `Resources/Maps/` as well as entity prototypes. Shuttles, station events, and ruins often depend on map files.
- If the mechanic has guidebook/player documentation, check `Resources/ServerInfo/` and guidebook prototypes.

## Validation Checklist

Before handing off a patch, choose the relevant subset:

- Build: `dotnet build --configuration DebugOpt --no-restore /m` after `dotnet restore`, or a narrower project build.
- Code tests: `dotnet test --configuration DebugOpt Content.Tests/Content.Tests.csproj -- NUnit.ConsoleOut=0` and/or `dotnet test --configuration DebugOpt Content.IntegrationTests/Content.IntegrationTests.csproj -- NUnit.ConsoleOut=0 NUnit.MapWarningTo=Failed` when behavior changed.
- Prototype/YAML changes: run the repo's YAML/content validation path if available, and at minimum check IDs, parents, components, enum values, resource paths, and localization keys.
- UI changes: open the BUI/window in-game or through the nearest existing debug/admin path; check scaling, missing loc strings, disabled buttons, and stale state updates.
- Resource changes: verify RSI state names, sprite paths, audio paths, licenses/meta files, and casing.
- Networking/prediction changes: verify shared events/components are in shared code, server authority remains server-side, and client code does not mutate authoritative state.

## Useful Next References

- `../feature-checklist.md`
- `../../ss14-client-server-shared/references/client-server-primer.md`
- `../../ss14-client-server-shared/references/shared-and-prediction.md`
- `../../ss14-prototype-basics/references/first-prototype-workflow.md`
