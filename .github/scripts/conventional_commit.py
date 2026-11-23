#!/usr/bin/env python3
"""Checks that commit subjects or pull request titles follow Conventional Commits.

Usage:
  conventional_commit.py <title>   Checks one pull request title.
  conventional_commit.py -         Checks every line of standard input (commit subjects); merge commits
                                   ("Merge …") are skipped.

The types are the ones `cliff.toml` and `docs/conventions.md` use. git-cliff silently drops a commit whose
message does not match, so a malformed title means a missing release note and a wrong version bump.
Past tense cannot be checked.
"""

import re
import sys

PATTERN = re.compile(r"^(feat|fix|perf|docs|chore|test|ci|build|style|refactor|revert)(\([^)]+\))?!?: \S.*$")
HINT = "Expected '<type>[(scope)][!]: <description>', e.g. 'fix: Rejected blank titles (#123)'."


def check(subjects):
    failures = [subject for subject in subjects if not PATTERN.match(subject)]
    for subject in failures:
        print(f"::error::Not a conventional commit: {subject!r}. {HINT}")
    return not failures


def main(args):
    if len(args) != 1:
        sys.exit(__doc__)

    if args[0] == "-":
        subjects = [line.rstrip("\n") for line in sys.stdin if line.strip() and not line.startswith("Merge ")]
    else:
        subjects = [args[0]]

    if not check(subjects):
        sys.exit(1)
    print(f"{len(subjects)} subject(s) OK.")


if __name__ == "__main__":
    main(sys.argv[1:])
