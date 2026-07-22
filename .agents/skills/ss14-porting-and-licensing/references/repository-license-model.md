<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Repository License Model

This file summarizes the current Space Dream repository license model for agent routing and review. It is not a legal opinion.

## Current Local Anchors

Use these root files and local metadata when checking licensing:

- `README.md`
- `LICENSE-AGPLv3.txt`
- `LICENSE-MIT.TXT`
- per-file `SPDX-*` headers
- neighboring `.license` files where present
- RSI `meta.json` files for sprites
- asset-specific metadata beside audio, textures, maps, or other resources

## Practical Summary

The README currently describes Space Dream (SD) as an SS14 fork for the Space Dream project. Upstream for game content is WizDen; material may also originate from Corvax and other forks before reaching this tree.

The README’s license section states:

- code in the codebase is AGPL-3.0 (see `LICENSE-AGPLv3.txt`);
- most media assets are CC-BY-SA 3.0 unless stated otherwise in RSI/`meta.json` or neighboring metadata;
- some assets may be CC-BY-NC-SA 3.0 or similar non-commercial licenses and must be flagged or replaced before commercial reuse.

`LICENSE-MIT.TXT` exists and contains MIT text with Space Wizards Federation copyright notices. Do not assume a file is MIT-only merely because this file exists. Check the specific file’s headers or neighboring license metadata.

## Rule

If a port touches external code or assets, read the local license files and the source license before proceeding.
