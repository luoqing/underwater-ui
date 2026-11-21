import { Vector3 } from "three";

// Half-width and vertical envelope measured along the rig, in body lengths.
// Leave room around the animated flukes, pectorals and the tail's downstroke.
const sections = [
  [-.53, .22, -.10, .35], [-.32, .18, -.10, .29],
  [-.13, .15, -.10, .19], [-.01, .36, -.26, .19],
  [.10, .34, -.25, .20], [.18, .24, -.13, .20],
  [.35, .12, -.06, .20], [.51, .07, -.04, .15],
];
const localHull = sections.flatMap(([z, width, low, high]) =>
  [-width, width].flatMap(x => [new Vector3(x, low, z), new Vector3(x, high, z)]));
const axes = [
  [1,0,0],[0,1,0],[0,0,1],[1,0,1],[1,0,-1],[.5,0,1],[-.5,0,1],
].map(v => new Vector3(...v).normalize());

export function whaleClearance(calf, mother) {
  const calfHull = localHull.map(() => new Vector3());
  const motherHull = localHull.map(() => new Vector3());
  const axis = new Vector3(), correction = new Vector3(), delta = new Vector3();
  const last = new Vector3();
  let tracking = false;
  function hull(out, node, length) {
    out.forEach((point,i) => point.copy(localHull[i]).multiplyScalar(length)
      .applyQuaternion(node.quaternion).add(node.position));
  }
  function resolve() {
    if (calf.position.distanceToSquared(mother.position) > 110) return;
    hull(calfHull, calf, 5.7);
    hull(motherHull, mother, 10);
    let shortest = Infinity;
    for (const node of [mother, calf]) {
      for (const local of axes) {
        axis.copy(local).applyQuaternion(node.quaternion);
        let aMin=Infinity, aMax=-Infinity, bMin=Infinity, bMax=-Infinity;
        for (let i=0; i<localHull.length; i++) {
          const a=calfHull[i].dot(axis), b=motherHull[i].dot(axis);
          aMin=Math.min(aMin,a); aMax=Math.max(aMax,a);
          bMin=Math.min(bMin,b); bMax=Math.max(bMax,b);
        }
        const forward=bMax-aMin+.16, reverse=aMax-bMin+.16;
        if (forward<=0 || reverse<=0) return;
        const push=forward<reverse ? forward : -reverse;
        if (Math.abs(push)<shortest) {
          shortest=Math.abs(push);
          correction.copy(axis).multiplyScalar(push);
        }
      }
    }
    calf.position.add(correction);
  }
  return {
    reset() { tracking=false; },
    update(dt, reef) {
      if (!calf.visible || !mother.visible) { tracking=false; return; }
      // Keep continuity when a patrol or the pointer asks for a point beyond the adult.
      if (tracking) {
        delta.subVectors(calf.position,last).clampLength(0,6*dt);
        calf.position.copy(last).add(delta);
      }
      resolve();
      reef.separate(calf.position,1.75);
      resolve();
      last.copy(calf.position);
      tracking=true;
    },
  };
}
