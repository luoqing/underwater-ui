# Isolated underwater gameplay and visual bug fixes

This is a separate local Git repository, copied from the current developer project.
Do not edit, switch, overwrite or deploy the original project at
`/data00/home/luoqing.tsing/Projects/underwater-journey`.

- Current source baseline: `baseline/layered-water-20261020`, commit `26ee34c`.
- Work branch: `fix/ui-missions-20261020`.
- The user explicitly authorized gameplay, UI/font, mission, animal motion and
  damaged-model fixes on 2026-10-20. This supersedes the earlier loading-only
  scope. Preserve the accepted layered-water colors, existing texture bytes,
  post-processing and resolution while fixing demonstrated defects.
- BlueTang/ReefFishDrift/FishSwimmer are owned by the original developer bot;
  read its logs for context but do not overwrite its project or send messages.
- The previous texture-compression candidate at
  `/data00/home/luoqing.tsing/Projects/underwater-perf-A128g8` is a separate,
  visually unapproved experiment. Do not import it wholesale or treat its
  158 MB result as a result for this branch.
- Changes with possible visual impact require a separate experimental branch,
  controlled visual comparisons, and explicit user acceptance before integration.
- The user requested a lighter-water/coral/light reference preview on 2026-10-20.
  Branch `visual/clear-water-preview-20261020` may change those runtime grading
  parameters and depth of field only in its separate `Build/WebGLClearWater`
  output. The previous 8896 build must remain intact. `look=original&water=layered`
  provides a same-build comparison; new screenshots/recordings are not approval.
- The user requested a more transparent mission panel on 2026-10-20.
  Branch `visual/translucent-menu-20261020` uses `Build/WebGLGlassMenu`;
  preserve both existing 8896 and 8897 outputs and previews.
- The user requested a reference-first translucent fishing-net rescue on
  2026-10-20, followed by real 3D net geometry and a cutter stroke interaction.
  Branch `feat/net-cut-rescue-20261020` uses `Build/WebGLNetRescue`; preserve
  the earlier preview directories and ports 8896, 8897 and 8898.
- A real-input rescue regression at close range was found in that preview.
  Its corrected candidate builds to `Build/WebGLNetRescueR2`, preserving the
  first candidate while the close-drag and cut-marker corrections are tested.
- The user also requested natural multicolour corals, spaced reef patches and
  additional distributed fish schools in this same isolated preview.
- On 2026-10-21 the user requested an image-first simple emoji-like knife,
  replacing the guarded cutter. Branch `visual/simple-knife-20261021` builds to
  `Build/WebGLSimpleKnife`; preserve 8899/8900 and all previous build outputs.
  Keep existing net-cut gestures, reach limits and mission logic unchanged.
- Keep changes reviewable and reversible. Do not merge, push or publish without
  explicit user direction. Do not initialize Git in the original live project.
- The original project is still evolving. This is a verified source snapshot,
  not a claim that future developer changes are synchronized automatically.

Before committing, compare with the current source baseline. Record real Unity
and browser validation separately from numerical geometry checks. Do not call a
visual or playable requirement complete solely because a build or HTTP check passed.
