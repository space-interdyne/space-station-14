# <!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

#!/usr/bin/env python3

import json
import sys


def main() -> int:
    try:
        json.load(sys.stdin)
    except Exception:
        pass

    result = {
        "hookSpecificOutput": {
            "hookEventName": "SessionStart",
            "additionalContext": (
                "Space Dream (SD) SS14 repo context: keep components data-only, "
                "place predicted player-visible logic in Content.Shared when "
                "appropriate, prefer _SD for fork-only content, localize all "
                "player-facing strings, and run the smallest relevant validation."
            ),
        }
    }
    json.dump(result, sys.stdout)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
