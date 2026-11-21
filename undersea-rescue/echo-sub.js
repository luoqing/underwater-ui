import * as T from "three";
import { fitSub } from "./sub-fleet.js";

export const boatLegs = new Set(["hydro", "fork", "shoalPass", "reefPass", "armDock"]);

export function rescueSub(model, scene) {
  const hull = new T.Group();
  const equip=fitSub(model);
  equip("echo");
  hull.add(model);
  scene.add(hull);
  const contact = new T.Vector3(), base = new T.Vector3(), elbow = new T.Vector3();
  const eye = new T.Vector3(), aim = new T.Vector3(), forward = new T.Vector3();
  // Replace the fixed concept arm with jointed links that can reach the crossbar.
  model.traverse(node => {
    if (/^(Manipulator|Soft_gripper|Soft gripper)/.test(node.name)) node.visible = false;
  });
  const enamel = new T.MeshStandardMaterial({color:0xffb246,metalness:.45,roughness:.36});
  const metal = new T.MeshStandardMaterial({color:0x8babb1,metalness:.58,roughness:.36});
  function link(material,radius) {
    const node = new T.Mesh(new T.CylinderGeometry(radius,radius,1,12), material);
    hull.add(node);
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
    lamp.position.set(-.72,.68,side*.20);
    hull.add(lamp);
    return lamp;
  });
  hull.visible=false;
  return {
    select(id){
      equip(id);
      lamps.forEach((lamp,i)=>{
        const side=i?1:-1;
        if(id==="swift")lamp.position.set(-.34,.255,side*.19);
        else if(id==="stone")lamp.position.set(-.32,.41,side*.19);
        else lamp.position.set(-.72,.68,side*.20);
      });
    },
    hide() { hull.visible=false; },
    drive(camera, diver, s, cage, reef, info) {
      hull.visible=true;
      const docked = s.route && s.foundCage && !s.leg;
      if (docked) {
        hull.position.set(cage.x, cage.y + .08, cage.z + 3.48);
        hull.rotation.set(0,Math.PI/2,0);
      } else {
        camera.getWorldDirection(forward);
        hull.position.copy(diver.position);
        hull.rotation.set(0,Math.atan2(-forward.z,forward.x),
          Math.asin(T.MathUtils.clamp(forward.y,-.7,.7)),"YXZ");
      }
      hull.updateMatrixWorld();
      base.set(.31,-.28,.15);
      contact.set(.99,-.38,.15);
      if (docked) {
        tip.set(cage.x,cage.y+.22+s.lift*3.3,cage.z+2.05);
        hull.worldToLocal(tip);
        contact.lerp(tip,T.MathUtils.smoothstep(s.armReach ?? 0,0,1));
      }
      elbow.copy(base).lerp(contact,.51);
      elbow.y-=.24;
      span(shoulder,base,elbow);span(forearm,elbow,contact);
      jaws.forEach((jaw,i)=>{tip.copy(contact);tip.x+=.075;tip.z+=(i?1:-1)*.09;span(jaw,contact,tip);});
      const age=s.time-(s.pingTime ?? -10);
      const flash=[0,.28,.67].some(at=>age>=at && age-at<.16);
      lamps.forEach((lamp,i)=>lamp.material.color.setHex(flash && (i ? info.signalPan>=-.2 : info.signalPan<=.2) ? 0xa0fff0:0x305151));
      if (docked) {
        aim.copy(cage).addScaledVector(axis, .45+s.lift*.7);
        eye.set(cage.x+4.2,cage.y+2.65,cage.z+6.7);
      } else {
        camera.getWorldDirection(forward);
        aim.copy(hull.position).addScaledVector(forward,3.4);
        aim.y+=.4;
        eye.copy(hull.position).addScaledVector(forward,-4.15);
        eye.y+=1.48;
      }
      eye.y=Math.max(eye.y,reef.ground(eye.x,eye.z)+.4);
      reef.cameraArm(hull.position,eye,.23);
      camera.position.copy(eye);camera.lookAt(aim);
    }
  };
}
