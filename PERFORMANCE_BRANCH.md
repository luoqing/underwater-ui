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
No remote is configured. Any performance preview is separate from port 8878.

## Initial branch changes

- Build to the separate `Build/WebGLPerformance` directory.
- Generate content-hashed filenames for safe immutable HTTP caching.
- Keep gzip, using native browser decompression with the supplied server headers.
- Serve gzip with correct MIME types; cache recognized hashed files immutably,
  while revalidating HTML and unhashed files. Do not gzip-label 404 responses.
- Preserve the baseline's development-build and exception settings for now,
  because the developer is actively debugging the new story implementation.
- Package the development build separately with `Tools/prepare_webgl_delivery.mjs`.
  Development output may remain uncompressed despite the Unity gzip setting.
  The packaging step gzips the data, WebAssembly and framework files, hashes
  their names, and checks every decoded byte against the input. Loader JavaScript
  remains plain. Only the four asset URLs change in the HTML; static assets and
  the original build remain intact. An existing output directory is refused.

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
node --test Tools/test_prepare_webgl_delivery.mjs
```

Review changed paths:

```sh
git diff --name-only baseline/quality-20261019 HEAD
```

Previous candidate measurements are not transferred to this branch. Serve the
new package with the updated server: native gzip requires Content-Encoding.
Keep the preview separate from the existing service on port 8878.

## Reproduce the isolated preview

Build with Unity 6000.3.26f1 and the `UnderwaterBuild.BuildWebGL` editor method,
then run these commands from this repository. Use a fresh output directory for
each new package; never overwrite a running preview.

```sh
node Tools/prepare_webgl_delivery.mjs Build/WebGLPerformance Build/WebGLPreview
python3 Tools/serve_webgl.py --directory Build --port 8893
```

`/WebGLPerformance/` is the unchanged build output control. `/WebGLPreview/` is
the lossless delivery variant of exactly that build. Its `delivery-manifest.json`
records decoded SHA-256 checksums and both byte sizes. This comparison does not
depend on the other developer's changing live build.

`Tools/measure_webgl.mjs` uses a temporary browser profile and software WebGL
for loading/console/network smoke checks. It does not measure laptop GPU FPS.

## Acceptance

The isolated preview has now been built and browser-checked. See
`Review/LOADING_PREVIEW.md` for measurements, integrity checks and the existing
mission-shader error reproduced in both versions. Core download bytes are down
38.70%; end-to-end laptop loading improvement remains unverified, and the local
fast-network smoke test did not become faster. This is not a production release.

Compare against the saved baseline on the same laptop and network. Measure the
0-to-100-percent loading wait, cold and warm visits separately. Do not claim
loader completion is the first interactive frame. Any future change touching
rendered appearance requires matching camera, lighting and animation conditions;
visible loss is a reason to reject it, not to lower the acceptance standard.
