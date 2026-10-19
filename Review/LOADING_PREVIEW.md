# Lossless loading preview — 2026-10-19

## Outcome and limits

Branch: `perf/loading-20261019`; immutable source baseline: `60c1811`.
The original developer project and port 8878 were not modified or replaced.
This preview uses the saved source snapshot, not subsequent developer changes.

- Preview: http://10.37.216.166:8893/WebGLPreview/
- Same-build control: http://10.37.216.166:8893/WebGLPerformance/
- Persistent preview session: `underwater-perf-preview-8893`.

Core download bytes fell by **38.70%**, but a faster total load on the user's
laptop is **not yet demonstrated**. On this host's fast local network, the
compressed cold run took longer to finish the loader than the uncompressed run.
Do not merge or replace the live build on the basis of download size alone.

## Quality boundary and integrity

Runtime Assets, Packages and ProjectSettings match the saved baseline exactly.
The only changed existing Unity source is the editor build helper (output path,
hashed names and native decompression settings). Unity-generated scene IDs,
temporary performance-test metadata changes and serialized build flags were
restored after the build. No lossy texture compression, model simplification,
shader edits, resolution reduction, lighting or shadow changes were introduced.

The control and preview are the **same compiled build**. Each compressed file
was decoded and compared byte-for-byte with the control before publishing the
isolated preview. A second check streamed each file through the actual HTTP
server, decoded it and verified its SHA-256 and decoded byte count. All four
checks passed. `delivery-manifest.json` records the original hashes and sizes.
Static template and StreamingAssets files were copied unchanged; only the four
asset URL references change in the HTML.

## Download measurements

Decimal MB; four core files only, excluding the small template/static files.

| File | Uncompressed bytes | Delivered bytes |
| --- | ---: | ---: |
| Loader JavaScript | 58,344 | 58,344 |
| Scene data | 364,307,579 | 319,763,276 |
| Framework JavaScript | 937,674 | 182,777 |
| WebAssembly | 213,106,915 | 34,552,376 |
| Total | **578,410,512** | **354,556,773** |

Saved: 223,853,739 bytes. Browser resource timing independently reported these
encoded and decoded sizes. Native gzip and correct JavaScript/WebAssembly MIME
headers were verified. The candidate's warm visit reported zero transfer bytes
for all four core files, demonstrating cache reuse in this browser.

## Browser checks and timings

Isolated Chromium 1248, software WebGL/SwiftShader, 960x540 emulated viewport
(Unity canvas remained 960x600), no network throttle, same machine as the HTTP
server. The shared host had other active rendering/build tasks. One run per
case: these are smoke checks, not a statistically controlled laptop benchmark.

| Case | Loader complete, navigation-relative |
| --- | ---: |
| Uncompressed control, cold | 11.588 s |
| Compressed candidate, cold | 14.077 s |
| Compressed candidate, warm | 21.382 s |

Readiness was polled approximately once per second; the table gives first
observed readiness, not the exact loader-promise resolution instant. Loader
completion means the loading bar was hidden, **not first interactive frame**.
Software rendering, cache reads, decompression and engine startup still
cost time. Warm caching did not translate into a lower total wait in this run.
These results do not establish 1080p/60 FPS or user-device loading performance.

Both cold runs displayed the underwater scene in captured screenshots. All
recorded requests succeeded and no browser JavaScript exception was reported.
This does **not** mean the Unity console was clean: the baseline error below
was reproduced in both versions. Full mission acceptance is therefore pending.

Local raw evidence (ignored by Git): `Logs/preview-candidate.json`,
`Logs/preview-candidate-warm.json`, `Logs/preview-control.json` and the two cold
run PNGs. Build evidence: `Logs/preview-build.log` reports Success and exits 0.

## Existing baseline issue, not changed in this performance branch

**[P1] Missing mission shader aborts startup.** Both builds throw the identical
`ArgumentNullException: shader` in `OceanMissionDirector.CreateAdventureBeacons`.
`Assets/Scripts/OceanMissions.cs:308` constructs a Material from
`Shader.Find("Universal Render Pipeline/Unlit")` without a retained material
reference or null guard. In this build the lookup is null. The exception exits
`Start()` before rescue-target setup and initial mission-menu selection. The
same lookup pattern exists at line 460 for the rescue line material.

This needs a shader-retention/material-reference fix and mission-path testing
by the developer working on story content. It was not hidden or patched here.
There are also three internal unsupported-shader log messages in both versions.

## Validation

- Unity 6000.3.26f1 build succeeded.
- Three lossless-packaging tests passed (raw/gzip inputs, HTML/static fidelity,
  no overwrite, nested output rejection, unsafe/missing asset references).
- Four real-HTTP serving tests passed (gzip/MIME/cache/HEAD/304/404 behavior).
- Independent read-only review found no actionable packaging/server defect.
- No model, texture, runtime script or rendering setting differs from baseline.

Next acceptance: compare both links on the user's laptop and network, first
visit and refresh separately. Keep quality unchanged. If startup rather than
download dominates there, profile that phase before choosing another change.
