#!/usr/bin/env python3
"""Removes the results located in generated code from a SARIF file, in place.

Usage:
  filter_sarif.py <file.sarif>

CodeQL analyses everything the compiler builds, including the output of source generators (e.g. the
`[GeneratedRegex]` code under `obj/`), and its `paths-ignore` setting does not apply to C# built with
`build-mode: manual`. A result is removed when the file of its primary location has an `obj` folder in its path.
"""

import json
import sys


def is_generated(result):
    for location in result.get("locations", [])[:1]:
        uri = location.get("physicalLocation", {}).get("artifactLocation", {}).get("uri", "")
        if "obj" in uri.replace("\\", "/").split("/"):
            return True
    return False


def main(path):
    with open(path, encoding="utf-8") as file:
        sarif = json.load(file)

    removed = 0
    for run in sarif.get("runs", []):
        results = run.get("results", [])
        kept = [result for result in results if not is_generated(result)]
        removed += len(results) - len(kept)
        run["results"] = kept

    with open(path, "w", encoding="utf-8") as file:
        json.dump(sarif, file)

    print(f"Removed {removed} result(s) in generated code from {path}.")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    main(sys.argv[1])
