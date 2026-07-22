<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Attribution And Safety

## Purpose

Use this file before copying, porting, rewriting, or adapting code, prototypes, maps, sprites, audio, UI, documentation, or any other external material into Space Dream.

This is a practical engineering checklist, not legal advice. When in doubt, read the actual local license files and the upstream source license before editing.

## Current SD Anchors

Use the current root files:

- `README.md`
- `LICENSE-AGPLv3.txt`
- `LICENSE-MIT.TXT`
- per-file `SPDX-*` headers where present
- neighboring `.license` files where present
- RSI `meta.json` files for sprite licensing and copyright
- other asset metadata files beside the asset being touched

Do not invent a license from memory. If the file has local metadata, that metadata wins for that file.

## Required Checks

Before porting anything, verify all of these:

1. Source is known.
   - Record the source repo.
   - Prefer an exact commit, PR, file path, or raw URL.
   - For TG, Goob, Shiptest, DeltaV, Corvax, White/BlueMoon, or any other fork, do not cite only “from upstream”; cite the exact place.

2. License is known.
   - Check the source repository license.
   - Check the source file header or asset metadata.
   - Check whether the source has extra per-file licensing.
   - For assets, check the sprite/audio/map metadata, not only the repository root license.

3. License is compatible with SD's current model.
   - SD README states code is AGPL-3.0, with MIT dual-license material present where headers or `LICENSE-MIT.TXT` apply.
   - Most media assets are CC-BY-SA 3.0 unless stated otherwise.
   - Some assets may be CC-BY-NC-SA 3.0 or another non-commercial license; flag these clearly because they are unsafe for commercial reuse and should not be silently mixed into general assets.

4. Attribution path is correct.
   - Code: preserve existing SPDX/copyright headers where required.
   - Assets: update `meta.json` or the neighboring license metadata.
   - Maps/prototypes: add source notes in comments only if the surrounding file style supports it; otherwise document the source in the PR body.
   - PR body: mention the source repo, commit/PR/path, and what was copied vs adapted.

5. No hidden carryover is introduced.
   - Do not paste AGPL/GPL/MPL/CC-BY-SA/CC-BY-NC-SA/other copyleft or non-commercial material while presenting it as original or MIT-only work.
   - Do not strip attribution from upstream assets.
   - Do not merge a third-party asset into an existing RSI without checking the whole RSI’s metadata.
   - Do not mix copied material with unrelated refactors; make review and attribution traceable.

## Contribution Rule

For agents: never tell the user that a PR is “legally clean” only because a root license file exists. Root licenses do not automatically make an external copied source compatible.

## Asset Rules

Sprites and RSIs are the most common place where attribution breaks.

When touching an RSI:

- open its `meta.json`;
- preserve existing `license` and `copyright`;
- add source commit/path for new states;
- do not add a new state from a different license unless the combined metadata still describes the full RSI correctly;
- if licenses differ per state and the project supports per-file `.license` metadata nearby, use it instead of pretending the whole RSI has one license;
- if the asset source is unknown, stop and ask for a source or replace it with original work.

When touching audio:

- prefer existing sound collection prototypes for gameplay use;
- still verify the actual audio file license;
- do not treat a prototype reference as attribution;
- record the original file source if adding a new `.ogg`/`.wav`.

When touching maps:

- map YAML can embed many prototype references, but external map layouts still need source tracking if copied;
- if importing from another fork, record source commit and map path;
- verify any bundled decals, tiles, custom entities, and map-specific assets separately.

## Code Rules

When copying or adapting code:

- keep SPDX headers where the local file style uses them;
- keep upstream copyright notices when required;
- do not change license headers casually;
- if adapting logic rather than copying text, still mention the source behavior in the PR description;
- if copying from TG/DM into C#, treat it as an adapted implementation and cite the exact DM path/commit in the PR;
- if copying from Goob/DeltaV/Corvax/other SS14 forks, check their root license and per-file headers before assuming compatibility.
