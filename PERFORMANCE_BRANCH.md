# Performance branch with a preserved quality baseline

## Isolation

This local repository contains 147 copied source/configuration files from the
developer project. SHA-256 comparisons confirmed every copied file matched its
source before the baseline commit. Library, builds, temporary files, user
settings, generation credentials and reference-art folders were not copied.
The original project did not have a Git repository; it was left untouched.

| Branch | Purpose |
| --- | --- |
| `baseline/quality-20261019` | Unmodified project snapshot, commit `60c1811` |
| `perf/loading-20261019` | Loading-only work, initially limited to build output and HTTP caching |

The snapshot retains the developer's current mission/story code. Nothing was
copied from the older candidate's runtime scripts, shaders or converted models.
No remote is configured and no deployment has been performed.

## Initial branch changes

- Build to the separate `Build/WebGLPerformance` directory.
- Generate content-hashed filenames for safe immutable HTTP caching.
- Keep gzip, using native browser decompression with the supplied server headers.
- Serve gzip with correct MIME types; cache recognized hashed files immutably,
  while revalidating HTML and unhashed files. Do not gzip-label 404 responses.
- Preserve the baseline's development-build and exception settings for now,
  because the developer is actively debugging the new story implementation.

Gzip and HTTP caching do not change decoded image pixels. This branch adds no
lossy texture compression, mesh simplification or quality-setting changes.
It does not yet reduce the initial model data payload to 158 MB. That number
belongs to the separate, unapproved texture experiment.

## Validation

HTTP tests:

All four server tests passed when establishing this branch. Runtime assets,
Packages and ProjectSettings were checked against the baseline and remain unchanged.

```sh
python3 -m unittest discover -s Tools -p test_serve_webgl.py -v
```

Review changed paths:

```sh
git diff --name-only baseline/quality-20261019 HEAD
```

The new branch has not been rebuilt in Unity or browser-benchmarked. Previous
candidate measurements are not transferred to this branch. A future build must
be served with the updated server, because native gzip requires Content-Encoding.
Keep any preview separate from the existing service on port 8878.

## Acceptance

Compare against the saved baseline on the same laptop and network. Measure the
0-to-100-percent loading wait, cold and warm visits separately. Do not claim
loader completion is the first interactive frame. Any future change touching
rendered appearance requires matching camera, lighting and animation conditions;
visible loss is a reason to reject it, not to lower the acceptance standard.
