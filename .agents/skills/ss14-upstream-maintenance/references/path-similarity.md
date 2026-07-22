<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Path Similarity

## Rule

When creating fork/module-specific files, mirror the existing feature path as closely as practical.

## Example

- upstream-like behavior under `Content.Shared/<Feature>/...`
- SD-side extension under `Content.Shared/_SD/<Feature>/...` when the SD split is warranted, or the matching inherited subtree when that feature already has one

## Why

- keeps drift discoverable
- makes upstream rebases easier
- helps humans and agents find the fork/module delta quickly
