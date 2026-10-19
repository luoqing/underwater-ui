# Isolated loading-performance work

This is a separate local Git repository, copied from the current developer project.
Do not edit, switch, overwrite or deploy the original project at
`/data00/home/luoqing.tsing/Projects/underwater-journey`.

- Baseline: `baseline/quality-20261019`, commit `60c1811`.
- Work branch: `perf/loading-20261019`.
- Preserve models, texture bytes/import settings, animation, shaders, lighting,
  post-processing, resolution and story behavior when optimizing loading.
- The previous texture-compression candidate at
  `/data00/home/luoqing.tsing/Projects/underwater-perf-A128g8` is a separate,
  visually unapproved experiment. Do not import it wholesale or treat its
  158 MB result as a result for this branch.
- Changes with possible visual impact require a separate experimental branch,
  controlled visual comparisons, and explicit user acceptance before integration.
- Keep changes reviewable and reversible. Do not merge, push or publish without
  explicit user direction. Do not initialize Git in the original live project.
- The original project is still evolving. This is a verified source snapshot,
  not a claim that future developer changes are synchronized automatically.

Before committing, check that only intended loading/build/serving files differ
from the baseline. In particular, runtime Assets and ProjectSettings must remain
unchanged unless the user explicitly approves a broader scope.
