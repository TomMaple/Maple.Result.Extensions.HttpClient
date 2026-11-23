#!/usr/bin/env python3
"""Tells which kinds of files a pull request changes, as GitHub Actions step outputs.

Usage:
  changes.py           Reads the changed paths (one per line) from standard input.
  changes.py --all     Reports every kind as changed (pushes to main, schedules, manual runs).

Prints `code=true|false` and `workflows=true|false`; append the output to "$GITHUB_OUTPUT".
- code: anything that can change the build, the tests or the package (README.md, LICENSE and the icon are
  packed), including the workflows and their scripts. Documentation under docs/ and other Markdown files are not.
- workflows: the GitHub Actions workflows (what the CodeQL `actions` analysis scans).

Jobs gated on these outputs must use `if: ${{ !cancelled() && needs.changes.outputs.<kind> != 'false' }}`, so they
still run when this check fails (fail open). A skipped job counts as passed for a required status check.
"""

import fnmatch
import sys

# fnmatch's "*" also matches "/", so "src/*" covers the whole folder.
CODE = [
    "src/*",
    "tests/*",
    ".github/workflows/*",
    ".github/scripts/*",
    "*.props",
    "*.targets",
    "*.slnx",
    "global.json",
    "nuget.config",
    ".editorconfig",
    ".gitattributes",
    "cliff.toml",
    "README.md",
    "LICENSE",
    ".resources/*",
]
WORKFLOWS = [
    ".github/workflows/*",
    ".github/actions/*",
]


def matches(paths, patterns):
    return any(fnmatch.fnmatchcase(path, pattern) for path in paths for pattern in patterns)


def main(args):
    if args == ["--all"]:
        code = workflows = True
    elif not args:
        paths = [line.strip() for line in sys.stdin if line.strip()]
        code = matches(paths, CODE)
        workflows = matches(paths, WORKFLOWS)
        print(f"{len(paths)} changed path(s).", file=sys.stderr)
    else:
        sys.exit(__doc__)

    print(f"code={str(code).lower()}")
    print(f"workflows={str(workflows).lower()}")


if __name__ == "__main__":
    main(sys.argv[1:])
