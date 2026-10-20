# Movement and fixed seafloor verification

The previous floor recycled nine 150 m tiles when the camera crossed a tile boundary. Its vertices were 3.75 m apart, but movement used the continuous terrain function instead of the rendered triangle heights. Each tile also recalculated its normals independently. Free movement attempted an unchecked teleport when stuck and automatically rotated the camera during collision avoidance.

The replacement generates fixed terrain once: 81 tiles cover 1,152 m square, with 2 m vertex spacing and continuous boundary normals. Four fixed horizon strips extend to 4,608 m from the map centre. The 800 m wide exploration area stays at least 176 m inside the detailed mesh; movement eases toward its outer 40 m while retreat remains available. Terrain collision and placement use barycentric interpolation of the same mesh triangles. No floor tile moves or regenerates while travelling.

Swimmers slide along solid obstacles and can back away without forced camera turns. Initial overlaps use a sphere penetration query. The probe must be an enabled trigger in this Unity version; a disabled probe failed the initial-overlap regression test. All movement queries ignore triggers. The previous unchecked escape teleport and the position smoothing/bobbing applied after collision resolution have been removed.

Interaction considers only the ray segment before solid scenery. Within that segment rescue targets take priority over animal discovery colliders, allowing the turtle knot to be selected. Starting a new mission releases the previously guided animal and clears movement/drag state.

## Executed checks

Unity 6000.3.26f1, editor batch mode with Xvfb and a graphics device, `MovementSeafloorValidation.Run`, process exit 0. Log: `Logs/movement-validation.log`.

- 10,449 terrain vertex/triangle/normal samples pass.
- All six movement directions pass in open water.
- A wall stops forward movement; backing away passes.
- An initially overlapping sphere exits the wall with 0.46 m surface clearance for a 0.4 m radius.
- A clear opening between arch posts and lintel remains traversable.
- Descending stops above the visible floor; movement cannot reach the terrain mesh edge.
- No terrain Update method remains to relocate tiles.
- The supplied Droid font plus Latin fallback yields valid glyphs for `W0123456789/、·○●海龟第盏距离米` in this editor run.

The synthetic arch fixture validates the collision algorithm, not every location in the imported reef model. Browser inspection of the actual arch and visual judgement of the new fixed horizon are still required. GPU frame rate and load duration were not measured by these numerical tests.
