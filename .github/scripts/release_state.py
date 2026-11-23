#!/usr/bin/env python3
"""Decides what the `publish` job of *Publish* still has to do, so that re-running it is idempotent.

Usage:
  release_state.py <tag> <version> <packages-dir> <build-attempt> <run-attempt>

Reads the versions on nuget.org and the assets of the GitHub release (`gh api`; needs GH_TOKEN and GH_REPO), compares
them with the packages in <packages-dir>, and prints step outputs for "$GITHUB_OUTPUT":
  action=noop|publish   noop: the version is on NuGet and every package is attached to the release.
  push=true|false       push to NuGet (the version is not there yet).
  upload=<names>        the packages missing from the release, space-separated.
Exits with an error instead of attaching a package that differs from what is already published:
- an asset on the release has a different SHA-256 than the package built here;
- the version is already on NuGet, but the package was built in this run attempt (attempt 1 of a version published
  elsewhere, or *Re-run all jobs*), so it cannot be the package that was pushed. *Re-run failed jobs* reuses the
  first build and passes.
"""

import hashlib
import json
import os
import subprocess
import sys
import urllib.error
import urllib.request

NUGET_INDEX = "https://api.nuget.org/v3-flatcontainer/{id}/index.json"
PACKAGE_ID = "maple.result.extensions.httpclient"


def decide(on_nuget, local, assets, rebuilt):
    """Returns (action, push, upload) or raises ValueError.

    local:  {file name: sha256} of the packages built for this release.
    assets: {file name: sha256 or None} already attached to the release (None: digest unknown).
    """
    missing = sorted(name for name in local if name not in assets)
    if on_nuget and not missing:
        return "noop", False, []

    mismatched = sorted(
        name for name in local if name in assets and assets[name] is not None and assets[name] != local[name])
    if mismatched:
        raise ValueError(
            f"The release already has {', '.join(mismatched)} with a different SHA-256 than the package built in "
            "this run. Use 'Re-run failed jobs' (it reuses the first build), or delete those assets from the "
            "release if they are wrong.")

    if on_nuget and rebuilt:
        raise ValueError(
            "The version is already on NuGet, but the package was built in this run attempt, so it is not the "
            "package that was pushed. Use 'Re-run failed jobs' (it reuses the first build), or attach the original "
            "package to the release by hand.")

    return "publish", not on_nuget, missing


def sha256(path):
    digest = hashlib.sha256()
    with open(path, "rb") as file:
        for chunk in iter(lambda: file.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def nuget_versions():
    try:
        with urllib.request.urlopen(NUGET_INDEX.format(id=PACKAGE_ID), timeout=30) as response:
            return json.load(response)["versions"]
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return []
        raise


def release_assets(tag):
    output = subprocess.run(
        ["gh", "api", f"repos/{{owner}}/{{repo}}/releases/tags/{tag}", "--jq", ".assets"],
        check=True, capture_output=True, text=True).stdout
    assets = {}
    for asset in json.loads(output or "[]"):
        digest = asset.get("digest") or ""
        assets[asset["name"]] = digest.removeprefix("sha256:") if digest.startswith("sha256:") else None
    return assets


def main(args):
    if len(args) != 5:
        sys.exit(__doc__)
    tag, version, packages_dir, build_attempt, run_attempt = args

    local = {name: sha256(os.path.join(packages_dir, name))
             for name in sorted(os.listdir(packages_dir)) if name.endswith((".nupkg", ".snupkg"))}
    if not any(name.endswith(".nupkg") for name in local):
        sys.exit(f"::error::No .nupkg in {packages_dir}.")

    on_nuget = version.lower() in nuget_versions()
    assets = release_assets(tag)
    rebuilt = build_attempt == run_attempt
    print(f"NuGet has {version}: {on_nuget}; release assets: {sorted(assets) or 'none'}; "
          f"package built in this attempt: {rebuilt} (build {build_attempt}, run {run_attempt})", file=sys.stderr)

    try:
        action, push, upload = decide(on_nuget, local, assets, rebuilt)
    except ValueError as error:
        sys.exit(f"::error::{error}")

    if action == "noop":
        print(f"::notice::{tag} is already published (NuGet and the release assets); nothing to do.", file=sys.stderr)
    print(f"action={action}")
    print(f"push={str(push).lower()}")
    print(f"upload={' '.join(upload)}")


if __name__ == "__main__":
    main(sys.argv[1:])
