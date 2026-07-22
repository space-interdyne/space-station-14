<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Localized Prototype Examples

## Clothing / Loadout

- `Resources/Prototypes/_SD/Entities/Clothing/Underwear/undershirt.yml`
- `Resources/Prototypes/_SD/Loadouts/Miscellaneous/undershirt.yml`
- `Resources/Locale/en-US/_SD/preferences/loadout-groups.ftl`
- `Resources/Locale/ru-RU/_SD/preferences/loadout-groups.ftl`

## Entity Prototypes

- `Resources/Prototypes/_SD/Entities/Objects/Devices/vibrator.yml`
- `Resources/Locale/ru-RU/_SD/entities/objects/devices/vibrator.ftl`

## Why These Are Useful

- the prototype IDs map directly to localized display keys
- the content is clearly fork-scoped under `_SD`
- the pair demonstrates prototype-plus-locale edits in the same feature subtree

## Reminder

When a prototype is visible to players, update the matching locale in the same pass instead of leaving a later TODO.
