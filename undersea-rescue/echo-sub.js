import * as T from "three";
import { seaBounds } from "./bluewater-layout.js";
import { airlock } from "./airlock.js";
import { fitSub } from "./sub-fleet.js";

export const boatLegs = new Set(["hydro", "fork", "shoalPass", "reefPass", "armDock"]);

export function rescueSub(model, scene) {
  const hull = new T.Group();
  const equip=fitSub(model);
  equip.select("echo");
  hull.add(model);
  scene.add(hull);
  const crew=airlock(model);
  const contact = new T.Vector3(), base = new T.Vector3(), elbow = new T.Vector3();
  const eye = new T.Vector3(), aim = new T.Vector3(), forward = new T.Vector3();
  // Replace the fixed concept arm with jointed links that can reach the crossbar.
  model.traverse(node => {
    if (/^(Manipulator|Soft_gripper|Soft gripper)/.test(node.name)) node.visible = false;
  });
  const enamel = new T.MeshStandardMaterial({color:0xffb246,metalness:.45,roughness:.36});
  const metal = new T.MeshStandardMaterial({color:0x8babb1,metalness:.58,roughness:.36});
  const arm=new T.Group();
  arm.name="RescueArm";arm.visible=false;hull.add(arm);
  function link(material,radius) {
    const node = new T.Mesh(new T.CylinderGeometry(radius,radius,1,12), material);
    arm.add(node);
    return node;
  }
  const shoulder = link(enamel,.07), forearm = link(metal,.045);
  const jaws = [link(enamel,.038),link(enamel,.038)];
  const axis = new T.Vector3(0,1,0), along = new T.Vector3(), tip = new T.Vector3();
  function span(mesh, a, b) {
    along.subVectors(b,a);
    mesh.scale.y=along.length();
    mesh.quaternion.setFromUnitVectors(axis, along.normalize());
    mesh.position.copy(a).lerp(b,.5);
  }
  const lamps = [-1,1].map(side=>{
    const lamp=new T.Mesh(new T.SphereGeometry(.051,10,8), new T.MeshBasicMaterial({color:0x385b60}));
    lamp.position.set(.966,-.117,side*.229);
    lamp.scale.x=.27;
    hull.add(lamp);
    return lamp;
  });
  function poseArm(reach){
    elbow.copy(base).lerp(contact,.51);elbow.y-=.24*reach;
    span(shoulder,base,elbow);span(forearm,elbow,contact);
    jaws.forEach((jaw,i)=>{tip.copy(contact);tip.x+=.075*reach;tip.z+=(i?1:-1)*.09*reach;span(jaw,contact,tip)});
  }
  const course=new T.Vector3(), starboard=new T.Vector3(), candidate=new T.Vector3();
  let followYaw=null;
  const aftQuarter = Math.atan2(.76, -.55);
  const defaultElevation = .385;
  let orbitAngle = aftQuarter, elevation = defaultElevation;
  const alternativeAngles = [[0,0],[.44,.15],[-.44,.15],[.85,.30],[-.85,.30],[Math.PI,.18]];
  function followCamera(camera,reef,dt){
    const yaw=hull.rotation.y;
    if(followYaw===null)followYaw=yaw;
    const delta=Math.atan2(Math.sin(yaw-followYaw),Math.cos(yaw-followYaw));
    followYaw+=delta*(1-Math.exp(-dt*4.5));
    course.set(Math.cos(followYaw),0,-Math.sin(followYaw));
    starboard.set(-course.z,0,course.x);
    // Frame the full 2.8 m envelope with margin even on a narrow viewport.
    const halfFov=T.MathUtils.degToRad(camera.fov*.5);
    const fit=1.54/Math.sin(Math.min(halfFov,Math.atan(Math.tan(halfFov)*camera.aspect)));
    const range=Math.max(4.15,fit+.40);
    const origin=hull.position;
    let best=-1;
    for(const [offset,tilt] of alternativeAngles){
      const azimuth = orbitAngle + offset;
      const polar = elevation + tilt;
      const aft = Math.cos(azimuth) * Math.cos(polar);
      const side = Math.sin(azimuth) * Math.cos(polar);
      const rise = Math.sin(polar);
      candidate.copy(origin).addScaledVector(course,aft*range).addScaledVector(starboard,side*range);
      candidate.y+=rise*range;
      candidate.y=T.MathUtils.clamp(candidate.y,reef.ground(candidate.x,candidate.z)+.36,seaBounds.surface-.24);
      reef.cameraArm(origin,candidate,.24);
      const clearance=candidate.distanceTo(origin);
      if(clearance>best){best=clearance;eye.copy(candidate)}
      if(clearance>=fit+.32)break;
    }
    aim.copy(origin).addScaledVector(course,Math.min(.70,Math.max(0,best-fit)));
    aim.y+=.15;
  }
  hull.visible=false;
  return {
    orbit(dx,dy){
      orbitAngle -= dx;
      elevation = T.MathUtils.clamp(elevation + dy, -.42, 1.12);
    },
    faceAhead(){orbitAngle = Math.PI; elevation = defaultElevation;},
    select(id){
      equip.select(id);crew.select(id);followYaw=null;orbitAngle=aftQuarter;elevation=defaultElevation;
      lamps.forEach((lamp,i)=>{
        const side=i?1:-1;
        if(id==="swift")lamp.position.set(.635,-.019,side*.263);
        else if(id==="stone")lamp.position.set(.635,-.069,side*.471);
        else lamp.position.set(.966,-.117,side*.229);
        lamp.scale.set(id==="echo"?.27:.82,.82,id==="echo"?1:.27);
      });
    },
    exhibit(display){
      crew.update({boat:true,time:0});
      display.add(hull);hull.visible=true;
      hull.position.set(0,0,0);hull.rotation.set(0,0,0);
      arm.visible=false;
    },
    leaveExhibit(){scene.add(hull);hull.visible=false},
    leaveDock(diver) {
      forward.set(1,0,0).applyQuaternion(hull.quaternion);
      diver.reset(hull.position,forward);
    },
    spin:equip.spin,
    hide() { hull.visible=false; },
    drive(camera, diver, s, cage, reef, info, dt) {
      hull.visible=true;
      const docked = s.route && s.foundCage && !s.leg;
      if (docked) {
        hull.position.set(cage.x, Math.max(cage.y+.08,reef.ground(cage.x,cage.z+3.48)+.92), cage.z + 3.48);
        hull.rotation.set(0,Math.PI/2,0);
      } else {
        diver.heading(forward);
        hull.position.copy(diver.position);
        hull.rotation.set(0,Math.atan2(-forward.z,forward.x),
          Math.asin(T.MathUtils.clamp(forward.y,-.7,.7)),"YXZ");
      }
      crew.update(s);
      hull.updateMatrixWorld();
      const reach=docked ? T.MathUtils.smoothstep(s.armReach ?? 0,0,1) : 0;
      arm.visible=reach>.001;
      if (arm.visible) {
        base.set(.31,-.22,.15);
        tip.set(cage.x,cage.y+.22+s.lift*3.3,cage.z+2.05);
        hull.worldToLocal(tip);
        contact.copy(base).lerp(tip,reach);
        poseArm(reach);
      }
      const age=s.time-(s.pingTime ?? -10);
      const flash=[0,.28,.67].some(at=>age>=at && age-at<.16);
      lamps.forEach((lamp,i)=>lamp.material.color.setHex(flash && (i ? info.signalPan>=-.2 : info.signalPan<=.2) ? 0xa0fff0:0x305151));
      if (docked) {
        aim.copy(cage).addScaledVector(axis, .45+s.lift*.7);
        eye.set(cage.x+4.2,cage.y+2.65,cage.z+6.7);
        followYaw=null;
        eye.y=Math.max(eye.y,reef.ground(eye.x,eye.z)+.4);
        reef.cameraArm(hull.position,eye,.23);
      }else followCamera(camera,reef,dt);
      camera.position.copy(eye);camera.lookAt(aim);
      crew.shot(s,camera,reef);
    }
  };
}
