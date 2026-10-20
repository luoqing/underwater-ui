# Whale rendering and story guidance verification

Checked on 2026-10-20. Runtime: `22d82cb`, isolated preview on port 8896.
The user excluded the incorrect port 8878 page from acceptance during this run.
The earlier read-only comparison is retained only as historical evidence below;
all current acceptance conclusions concern port 8896. No runtime changes were made in
this verification. The provisional all-left control changes were withdrawn:
movement stays at bottom left, ascent/descent at bottom right.

## Whale

- The original 8878 WebGL screenshot still shows a strongly folded/corrugated
  body. Its source `WhaleRigged.glb` is different from the isolated repair.
- The isolated Editor Play Mode captures show a continuous body/tail and
  changing tail posture, with production materials, lighting and post effects.
  Between time 3.3532 and 3.6965 seconds, the whale advances approximately
  0.994 m and its tail-tip rotation changes. The visual capture command exits 0.
- This is a stable-state, fixed whale-relative inspection at 640x480, without
  IMGUI. It is not a manual WebGL close-up or a 1080p/iGPU performance test.
  The Editor URL is empty, so it is not a pixel-matched layered-water comparison.
- The WebGL `start=whale` camera only rotates toward the animal. It does not
  follow its position; the moving animal becomes small. This entry is not a
  reliable sustained close-up inspection (UnderwaterWorld.cs:2312).
- The present render still looks soft/stylized. Photorealistic skin, anatomy
  and swimming are not accepted merely because the folded mesh is repaired.
  Gaussian depth of field begins at 11 m and ends at 42 m
  (UnderwaterWorld.cs:380); the fixed inspection camera is about 15 m away.
  This is one plausible contributor to softness, not proof that it is the
  only cause. A matched-distance, higher-resolution inspection is still needed.

Read-only geometry audit rerun on both current source GLBs:

| Source | Triangles | Welded boundary edges | Longest boundary edge |
| --- | ---: | ---: | ---: |
| Original developer project | 80,000 | 19,792 | 0.02896156 |
| Isolated repair | 119,996 | 46 | 0.00116451 |

Lengths are in normalized asset coordinates. Neither this audit nor the two
frames establish that the mesh is perfectly watertight at all poses. Geometry
and normals are finite. All three embedded image hashes and material JSON are
identical between these two assets: no lower-resolution or recompressed
texture was substituted by this repair.

## Actual implemented steps

Adventure: select a role; approach the coral-garden lantern; approach the
manta-channel lantern; approach the rock-arch lantern; completion message.
Approaching within 4.2 m advances a lantern; clicking it is not required.

Rescue: select a role; click the three orange knots in order; double-click
DouDou to select; hold and drag upward; release to continue along the chosen
heading; turtle reaching y=47.7 (actual surface y=48) completes rescue.
The right-hand ascent arrow moves the player/camera, not the selected turtle.

The bottom introduction changes after 0.65 m of real manual movement, then
after 36 px of camera dragging. Completed basic instructions disappear.
Rescue selection/drag hints take priority over the general introduction.

## Guidance findings

- **P1: A lit lantern disappears.** ReachAdventureBeacon disables the reached
  object (OceanMissions.cs:388), despite dialogue describing it as lit and the
  final story describing an illuminated route. Keep an illuminated completed
  lantern visible while moving the target marker to the next one.
- **P1: Narrated guides do not actually guide the quest.** Fish schools orbit
  fixed centers; manta movement does not advance with the mission. Whale calls
  originate from the roaming whale at random intervals, not the next lantern
  (UnderwaterWorld.cs:658, 3572; OceanMissions.cs:383, 398). Children following
  those directions can leave the intended route. Use the real lantern marker
  in the instruction, or implement a quest-linked animal/audio guide.
- **P2: Completion rewards are text only.** CompleteMission calls ShowStatus,
  with no discovery/badge update (OceanMissions.cs:441). Adventure promises a
  jellyfish entry, but the field guide only has four main species. Do not claim
  an unlock until the corresponding state and visible entry exist.
- **P2: Role selection is first-person configuration.** The four choices alter
  collision size, speed and turn response, not a visible player animal. The
  current wording can suggest an avatar that children will not see.

## Automated checks versus browser coverage

`MissionGameplayValidation.RunAll` rerun exits 0, including 2 missions x 4
roles, rescue replay, 14 enabled animals, contextual guidance, 10,449 terrain
samples, continuous collision-based 12/12 role/lantern routes, and surface
ascent checks. Mission-stage assertions deliberately place the camera/turtle
to exercise transitions; the separate route probes use 0.25 m collision steps.
Neither is represented as an end-to-end manual playthrough.

Normal browser input at 856x408 verified role selection, the initial adventure
objective, forward-button travel (30 m to 15 m), and the movement-to-looking
hint. The first lantern then advanced the objective to 2/3, with a 47 m marker
for the next lantern. A real camera drag completed the looking lesson and
removed its footer. Further travel ended 26 m from the second target with a
turn-right hint; the second and third lanterns were not manually completed.
Rescue was entered using the Whale role. All three orange knots were clicked
through normal pointer input, advancing 1/3 to 2/3 to 3/3 and then to the
surface stage. The footer changed to selecting DouDou, the upper-left goal
changed to bringing it to the surface, and the target reported 6 m remaining.
Selection, upward dragging and final breathing were not manually completed;
the automated checks cover those transitions separately. Browser captures
recorded no JavaScript exceptions or failed network requests. The isolated
test browser was closed at the end of the run.

Evidence:

- `Logs/whale-story-validation-20261020/gameplay.log`
- `Logs/whale-story-validation-20261020/whale-visual.log`
- `Logs/bugfix-20261020/whale-visual/recheck/whale-side-01.png` and `02.png`,
  with camera/time/pose JSON beside each image
- `Logs/whale-story-validation-20261020/browser/original-8878-whale-entry.png`
- `Logs/whale-story-validation-20261020/browser/whale-runtime-1280.png`
- `Logs/whale-story-validation-20261020/browser/adventure-first-lantern-result.png`
- `Logs/whale-story-validation-20261020/browser/rescue-after-first-knot.png`
- `Logs/whale-story-validation-20261020/browser/rescue-after-second-knot.png`
- `Logs/whale-story-validation-20261020/browser/rescue-after-third-knot.png`

These original/repair captures have different cameras, times and resolution;
they are defect evidence, not a controlled pixel-by-pixel quality comparison.
The runs retain engine/cache compatibility warnings and an Editor-only
UnityEditor.Search indexing exception. No warning-free-console or GPU/FPS
acceptance is claimed. Runtime source, models, textures and rendering settings
were not modified by this verification.

## New water/coral reference direction (proposal only)

The user supplied a four-panel reef reference and requested a lighter water
gradient. Treat this as the new direction for a separate visual comparison,
not a reason to overwrite the accepted preview without showing the result.

- A pale cyan surface/transmitted-light zone, muted turquoise midwater and
  desaturated blue distance, with a gradual transition and retained deep shade.
- Warm off-white sand and mostly sand-brown, cream and gray-green reef colors;
  restrained orange/yellow/pale-purple accents instead of saturated highlights
  on every coral. Preserve texture and rough surface response.
- One coherent oblique light direction, sparse soft-edged shafts and subtle
  moving caustics; bright exposed sand with shaded reef cavities. Do not lift
  every shadow or replace natural light with bright geometric columns.
- Preserve near detail and use distance-dependent color/contrast attenuation;
  compare the existing 11-42 m Gaussian blur separately before changing it.
- The reference also has foreground/midground/background reef occlusion. The
  preview's large exposed sand areas and broad uniform blue background cannot
  be made equally convincing through color correction alone.

No water, coral material, shader, exposure or post-processing parameter was
changed during this review. No appearance mockup is being represented as a
running Unity result.
