<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Fork Only Content

## Use `_SD` When

- The behavior is genuinely SD-specific.
- You are adding new SD prototypes, locale, assets, or sidecar systems that should stay clearly separated from inherited content.
- A matching `_SD` feature folder already exists.

## Respect Existing Non-SD Fork Trees

This repository also contains inherited/vendor trees such as `_DV`, `Corvax`, `Nyanotrasen`, and other prefixed areas in code and resources. If a feature already lives in one of those trees, extend that owner instead of moving it to `_SD` without a task-specific reason.

## Current SD Anchors

- `Content.Shared/_SD/`: shared SD contracts and systems such as `Antag`, `Blocking`, `CCVar`, `Input`, `Movement`, and `Vibrator`.
- `Content.Server/_SD/`: server-authoritative SD systems such as `Antag`, `Arousal`, `Power`, and `Vibrator`.
- `Content.Client/_SD/`: SD presentation/UI such as `Input` and `Options`.
- `Resources/Prototypes/_SD/`: SD content data including `Alerts`, `Catalog`, `DeviceLinking`, `Entities`, `Loadouts`, `SoundCollections`, `StatusIcon`, and `Voice`.
- `Resources/Locale/en-US/_SD/` and `Resources/Locale/ru-RU/_SD/`: SD localization (for example `alerts`, `chat`, `components`, `device`, `escape-menu`, `preferences`).
- `Resources/Textures/_SD/`, `Resources/Audio/_SD/`, `Resources/Maps/_SD/`, and `Resources/ServerInfo/_SD/` when present: SD assets, maps, and server/guidebook info.
