# Minimal HUD and action-based guidance

Branch: `ui/progressive-preview-20261020`, based on `d535465` (the r5 repair).
Runtime commit: `22d82cb`.
Worktree: `/data00/home/luoqing.tsing/Projects/underwater-ui-preview-20261020`.

## Scope

The user's reference has one short objective, unobtrusive controls, and an
instruction that advances when the child performs the current action.

- One upper-left objective, with no task panel, title block or free-dive badge.
- Six existing 44–48 px translucent arrows remain. The bottom hint has no bar.
- Field guide and story subtitles open only on request. Narration is retained.
- Small target distance/direction text replaces the large target rectangle.
- Opening the menu preserves progress. An explicit exit button abandons the
  mission; choosing another mission uses the existing role-selection flow.
- After actual manual travel of 0.65 m, the movement hint changes to looking
  around. A total 36 px camera drag completes that introduction. Automatic
  touring, blocked travel and menu activity do not complete the movement step.
- Hovering an animal introduces discovering it; after discovery, the hint
  introduces selection. Selection and dragging each get their own instruction.
- Rescue first asks for the next orange knot. The ascent stage separately
  introduces selecting the turtle, then holding and dragging it upward.
- Completed basic hints disappear; the next relevant action supplies the hint.

## Isolation and rendering

The first working directory acquired concurrent, unrelated changes to terrain
streaming and exploration boundaries during this task. Those changes were
preserved, not reverted or committed as this UI work. Only the UI and input-
observation hunks were transferred to this clean worktree.

The static r5 terrain, collisions, models, textures, shaders, water, resolution
and post-processing remain unchanged here. No original-project files or the
existing r5 preview on port 8894 were overwritten. This experiment is not merged
or approved as a visual replacement.

## Verification

The clean-worktree `MissionGameplayValidation.RunAll` run passed and exited 0:

- Two missions × four roles, rescue replay, narration cleanup and all motion
  groups remaining active (14 animals).
- 10,449 terrain samples; wall/back-out, overlap, floor and arch tests;
  12/12 role/lantern routes in continuous 0.25 m collision steps.
- Short upper-left objectives; on-demand guide/subtitles; visible-only input
  regions; menu preserving the active mission.
- Manual movement followed by looking; menu/blocked-motion not advancing the
  movement lesson; discovery changing the animal action; completed basics hidden.
- Rescue selection and drag-state hints validated separately for each role.

Log: `Logs/minimal-ui-20261020/gameplay-progressive.log`.

The final font check passed (402 runtime characters; 28,601 original CJK glyph mappings,
outlines and advances unchanged). HTTP-server tests passed (4 tests).
The final WebGL build exited 0: 133,476,924 bytes. Build log:
`Logs/minimal-ui-20261020/webgl-final.log`.

Two final UI-only refinements after the action-state test run replaced a legacy
all-controls toast with a short status and measured the actual free-roam objective
for its label height. Both compiled in this final build; free-roam rendering was
then checked in the browser.

## Browser acceptance

The clean-worktree candidate was exercised through normal pointer input at
1440×900 and 856×408. The initial 1440 capture was still Unity's splash screen;
it is retained as a diagnostic, not a successful gameplay capture.

- Arrow-held travel changed the target distance from 30 m to 24 m and changed
  the bottom instruction from moving forward to looking around.
- A real camera drag completed that introduction and removed the basic hint.
- Opening the guide showed all four species on two complete rows at 856×408.
- Opening and resuming the menu retained the adventure goal and 24 m distance.
- Explicitly exiting removed the quest marker and changed the objective to
  finding an ocean friend, without repeating the completed basic lessons.
- The final r2 package was reloaded separately into free exploration and
  rendered the short upper-left objective and initial forward hint. Its capture
  recorded no JavaScript exceptions or failed network requests. Engine/cache
  compatibility warnings remain; this is not a claim of a warning-free console.

Evidence under `Logs/minimal-ui-20261020/browser-progressive/`:

- `progressive-before-move-1440.png`, `progressive-before-move-856.png`
- `progressive-after-move-856.png`, `progressive-after-look-856.png`
- `guide-complete-856.png`, `menu-preserve-exit-856.png`
- `menu-resume-keeps-progress-856.png`, `exit-to-exploration-856.png`
- `final-free-guidance-856.png` (final r2 package)
- `final-turtle-guidance-856.png` (final r2 turtle entry; no JS exceptions or
  failed requests)

Preview: [free exploration near the turtle](http://10.37.216.166:8896/?water=layered&start=turtle&v=progressive-20261020-r2).
The upper-right menu opens the two stories and their four role choices.

Animal assets, their pre-existing stylization and the legacy camera touring
behavior are not redesigned by this UI change. No new photorealism is claimed.

Before images from the unchanged r5 build are in
`Logs/minimal-ui-20261020/browser-before/`. Camera placement is controlled by the
normal adventure start; animal positions and lighting animations vary with time.

The preview is served independently on port 8896. The temporary, mixed-worktree
candidate on port 8895 was stopped. Original port 8878 and r5 port 8894 remain
untouched. The shared source worktree and its concurrent terrain edits were not
reverted, committed or merged into this experiment.

The first browser pass found a clipped second field-guide row in a narrow
window. The panel now sizes itself from measured header and body heights.
Batch Play Mode does not dispatch OnGUI, so the fixture constructs the game's
label skin with the production font/settings for its bounds check. Actual
appearance still requires browser screenshots.

Traex review with `openrouter-4o` was unavailable because model metadata could
not be resolved. This is not a passing external review. Local manual review
and real verification are used instead.

No iGPU frame-rate or loading-speed improvement is claimed by this UI change.
Full manual completion of both stories is outside this UI acceptance pass.
