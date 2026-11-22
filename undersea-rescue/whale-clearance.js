import { Quaternion, Vector3 } from "three";

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
    separateNow(){if(calf.visible && mother.visible){resolve();last.copy(calf.position)}},
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

// World-space spheres along the spine, outstretched pectorals and flukes.
// The end spheres include the excursion of the animated tail.
export function whaleRockClearance(node, length, reef) {
  const stations=[
    [0,.025,.41,.13],[0,.04,.25,.18],[0,.02,.05,.175],
    [0,.015,-.14,.145],[0,.02,-.30,.115],[0,.03,-.44,.105],[0,.13,-.39,.10],
  ];
  for(const side of [-1,1])stations.push(
    [side*.16,-.06,.09,.11],[side*.26,-.13,.045,.10],[side*.32,-.18,.015,.075],
    [side*.12,.035,-.46,.095],[side*.20,.035,-.48,.075],
    [side*.12,.14,-.45,.112],[side*.19,.17,-.475,.125]);
  const probes=stations.map(([x,y,z,r])=>({local:new Vector3(x,y,z).multiplyScalar(length),
    center:new Vector3(),radius:r*length+.09}));
  const offset=new Vector3(), desired=new Vector3(), fwd=new Vector3(), side=new Vector3();
  const point=new Vector3(), moved=new Vector3(), last=new Vector3();
  const safe=new Vector3(),step=new Vector3(),safeRotation=new Quaternion();
  let hasSafePose=false;
  let countdown=0, tracked=false, preference=1;
  const body=()=>{for(const p of probes)p.center.copy(p.local).applyQuaternion(node.quaternion).add(node.position)};
  function clearance(lane) {
    let lift=0;
    for(const lead of [0,.40,.85]){
      for(const across of [-.21,0,.21]){
        point.copy(node.position).addScaledVector(fwd,length*lead).addScaledVector(side,lane+length*across);
        lift=Math.max(lift,reef.ground(point.x,point.z)+length*.21+.32-point.y);
      }
    }
    return lift;
  }
  function keepClear() {
    // Probe the actual reef BVHs, including vertical faces and overhangs.
    let resolved=false;
    for(let pass=0;pass<10;pass++){
      let touched=false,top=-Infinity;
      for(const p of probes){
        point.copy(p.local).applyQuaternion(node.quaternion).add(node.position);
        top=Math.max(top,point.y+p.radius);
        moved.copy(point);reef.separate(moved,p.radius);moved.sub(point);
        if(moved.lengthSq()<1e-9)continue;
        node.position.add(moved);touched=true;
      }
      if(top>9.84){node.position.y-=top-9.82;touched=true}
      if(!touched){resolved=true;break}
    }
    if(resolved){safe.copy(node.position);safeRotation.copy(node.quaternion);hasSafePose=true}
    else if(hasSafePose){node.position.copy(safe);node.quaternion.copy(safeRotation)}
    else safe.copy(node.position);
    body();
  }
  return {
    probes, keepClear,
    reset(){tracked=false;hasSafePose=false;countdown=0;offset.set(0,0,0);desired.set(0,0,0)},
    update(dt) {
      if(!node.visible){this.reset();return}
      if(tracked && last.distanceToSquared(node.position)>(length*2.5)**2)this.reset();
      last.copy(node.position);countdown-=dt;
      if(countdown<=0){
        countdown=.24;
        fwd.set(0,0,1).applyQuaternion(node.quaternion);fwd.y=0;fwd.normalize();
        side.set(fwd.z,0,-fwd.x);
        const straight=clearance(0);
        let rise=straight, lane=0;
        if(straight>.45){
          for(const sign of [preference,-preference]){
            const width=length*.45*sign, lift=clearance(width);
            if(lift+.24*Math.abs(width)<rise+.24*Math.abs(lane)-.2){rise=lift;lane=width}
          }
          if(lane)preference=Math.sign(lane);
        }
        desired.copy(side).multiplyScalar(lane);
        desired.y=Math.min(rise,Math.max(0,9.35-length*.20-node.position.y));
      }
      offset.lerp(desired,1-Math.exp(-dt*1.8));node.position.add(offset);
      // A path animation proposes an absolute point every frame. Walk from
      // the last resolved pose so the proposal cannot jump through a boulder.
      if(!tracked){
        // A restored checkpoint can start next to, or inside, a cliff.
        // Find open water above the bed before recording the first safe pose.
        const origin=node.position.clone();
        let found=false;
        for(const radius of [0,.32,.65,1.1,1.8,2.6]){
          for(let i=0;i<(radius?8:1);i++){
            const a=i*Math.PI/4;
            node.position.set(origin.x+length*radius*Math.cos(a),origin.y,origin.z+length*radius*Math.sin(a));
            let lift=0,ceiling=Infinity;
            for(const p of probes){
              point.copy(p.local).applyQuaternion(node.quaternion).add(node.position);
              lift=Math.max(lift,reef.ground(point.x,point.z)+p.radius+.06-point.y);
              ceiling=Math.min(ceiling,9.78-point.y-p.radius);
            }
            if(lift>ceiling)continue;
            node.position.y+=lift;
            keepClear();
            if(hasSafePose){found=true;break}
          }
          if(found)break;
        }
        if(!found)node.position.copy(origin);
      }
      if(tracked){
        step.subVectors(node.position,safe).clampLength(0,7*dt);
        const count=Math.max(1,Math.ceil(step.length()/.20));step.divideScalar(count);
        node.position.copy(safe);
        for(let i=0;i<count;i++){node.position.add(step);keepClear()}
      }else{keepClear();tracked=true}
    }
  };
}
