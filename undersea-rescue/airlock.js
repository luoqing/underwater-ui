import * as T from "three";
import {crewFigure} from "./rescue-crew.js";

const smooth=(v,a,b)=>T.MathUtils.smoothstep(v,a,b);
const vec=(x,y,z)=>new T.Vector3(x,y,z);
export function airlock(model){
  // Vertices are projected into the rigid hull frame once, so each aperture
  // stays registered while driving, independently of wheel and propeller animation.
  const ports={value:new T.Vector4()},cutEnabled={value:0};
  const portRadii={value:new T.Vector2()};
  model.updateWorldMatrix(true,true);
  const inv=model.matrixWorld.clone().invert(),pos=new T.Vector3();
  const patched=new Map();
  model.traverse(node=>{
    if(!node.isMesh || /Propeller|Impeller/i.test(node.name))return;
    const geom=node.geometry.clone(),points=geom.getAttribute("position");
    const coordinates=new Float32Array(points.count*3);
    const mat=new T.Matrix4().multiplyMatrices(inv,node.matrixWorld);
    for(let i=0;i<points.count;i++)pos.fromBufferAttribute(points,i).applyMatrix4(mat).toArray(coordinates,i*3);
    geom.setAttribute("hullCoord",new T.BufferAttribute(coordinates,3));node.geometry=geom;
    function patch(source){
      if(patched.has(source))return patched.get(source);
      const m=source.clone();
      m.onBeforeCompile=shader=>{
        shader.uniforms.portRadii=portRadii;shader.uniforms.rescuePorts=ports;shader.uniforms.activePorts=cutEnabled;
        shader.vertexShader="attribute vec3 hullCoord; varying vec3 cabinPoint;\n"+shader.vertexShader.replace("#include <begin_vertex>","#include <begin_vertex>\n cabinPoint=hullCoord;");
        shader.fragmentShader="uniform vec2 portRadii; uniform vec4 rescuePorts; uniform float activePorts; varying vec3 cabinPoint;\n"+shader.fragmentShader.replace("#include <clipping_planes_fragment>","#include <clipping_planes_fragment>\n if(activePorts>.5 && cabinPoint.z>.12 && (distance(cabinPoint.xy,rescuePorts.xy)<portRadii.x || distance(cabinPoint.xy,rescuePorts.zw)<portRadii.y))discard;");
      };patched.set(source,m);return m;
    }
    node.material=Array.isArray(node.material)?node.material.map(patch):patch(node.material);
  });
  const station=new T.Group();
  model.add(station);
  const enamel=new T.MeshStandardMaterial({color:0x176575,roughness:.41,metalness:.32});
  const rim=new T.MeshStandardMaterial({color:0xf2b34e,roughness:.46,metalness:.25});
  const dark=new T.MeshStandardMaterial({color:0x183942,roughness:.8,side:T.DoubleSide});
  const glass=new T.MeshPhysicalMaterial({color:0x9ae5e0,transparent:true,opacity:.14,roughness:.16,depthWrite:false});
  function ring(parent,x,y,z,r,t,mat){
    const m=new T.Mesh(new T.TorusGeometry(r,t,10,44),mat);m.position.set(x,y,z);parent.add(m);return m;
  }
  function barrel(parent,r,len,x,y,z,mat,open=false){
    const m=new T.Mesh(new T.CylinderGeometry(r,r,len,48,1,open),mat);
    m.rotation.x=Math.PI/2;m.position.set(x,y,z);parent.add(m);return m;
  }
  const chamber=new T.Group();station.add(chamber);
  const steel=new T.MeshStandardMaterial({color:0x80999b,metalness:.55,roughness:.36});
  const shell=new T.MeshStandardMaterial({color:0xf3b850,metalness:.22,roughness:.43});
  // A closed return around each opening overlaps the curved hull. From an
  // oblique view the transition reads as plating instead of the back of a tube.
  function insetSurround(parent,r,depth,material){
    const profile=[[r*.96,-depth],[r*1.24,-depth],[r*1.18,-depth*.42],[r*1.06,-.01],[r,.019],[r*.96,.01],[r*.96,-depth]];
    const surface=new T.LatheGeometry(profile.map(([radius,z])=>new T.Vector2(radius,z)),64);
    surface.rotateX(Math.PI/2);
    parent.add(new T.Mesh(surface,material));
  }
  insetSurround(chamber,.317,.34,shell);
  const windowGlass=new T.MeshPhysicalMaterial({color:0x70bcc7,transparent:true,opacity:.26,roughness:.19,
    metalness:.18,clearcoat:1,depthWrite:false});
  function glazing(parent,r,z){
    const lens=new T.Mesh(new T.SphereGeometry(1,48,24),windowGlass);
    lens.scale.set(r,r,.018);lens.position.z=z;parent.add(lens);
  }
  const amber=new T.MeshStandardMaterial({color:0xffe3a3,emissive:0xe3b357,emissiveIntensity:.65});
  // The coaming runs inside the cut hull; only the lip stands proud of the plating.
  barrel(chamber,.321,.55,0,0,-.255,dark,true);
  barrel(chamber,.32,.025,0,0,-.52,enamel);
  for(const z of [-.38,-.20,.012])ring(chamber,0,0,z,.312,.012,steel);
  for(const x of [-.226,.226])barrel(chamber,.022,.014,x,.17,-.28,amber);
  ring(chamber,0,0,.024,.327,.027,enamel);ring(chamber,0,0,.04,.31,.011,dark);
  const panel=(parent,w,h,d,x,y,z,material)=>{
    const m=new T.Mesh(new T.BoxGeometry(w,h,d),material);m.position.set(x,y,z);parent.add(m);return m;
  };
  const inner=panel(chamber,.57,.61,.029,0,0,-.475,steel);
  panel(inner,.033,.19,.017,.18,0,.022,dark);
  // A flooded transfer lock and a separate pocket along the outside of the dry hull.
  const door=new T.Group();chamber.add(door);
  const profile=[[.177,-.015],[.30,-.015],[.32,.002],[.316,.027],[.295,.043],[.212,.052],[.177,.052],[.177,-.015]];
  const leaf=new T.Mesh(new T.LatheGeometry(profile.map(([r,z])=>new T.Vector2(r,z)),56),shell);
  leaf.rotation.x=Math.PI/2;door.add(leaf);
  ring(door,0,0,.053,.182,.012,enamel);glazing(door,.18,.051);
  ring(door,0,0,-.013,.30,.012,dark);
  const lamp=amber.clone();
  barrel(chamber,.029,.016,.258,.239,.048,lamp);
  // The visible line is the water surface inside the circular transfer lock.
  const water=new T.Mesh(new T.PlaneGeometry(1,.47),new T.MeshStandardMaterial({
    color:0x94dfe1,emissive:0x276d78,transparent:true,opacity:.63,side:T.DoubleSide,depthWrite:false}));
  water.rotation.x=-Math.PI/2;water.position.z=-.234;chamber.add(water);
  const wetFace=new T.Mesh(new T.CircleGeometry(.305,48),new T.ShaderMaterial({
    transparent:true,depthWrite:false,uniforms:{height:{value:-.32},t:{value:0}},
    vertexShader:"varying vec2 at;void main(){at=position.xy;gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.);}",
    fragmentShader:"varying vec2 at;uniform float height;uniform float t;void main(){float d=height+.006*sin(28.*at.x+t*3.)-at.y;if(d<0.)discard;float edge=1.-smoothstep(.001,.012,d);gl_FragColor=vec4(mix(vec3(.10,.57,.66),vec3(.82,1.,.96),edge),.24+.54*edge);}"
  }));
  wetFace.position.z=-.023;chamber.add(wetFace);
  const captain=crewFigure("captain",{seated:true}),doc=crewFigure("medic");
  chamber.add(doc.root);doc.root.scale.setScalar(.285);
  const helm=glass.clone();helm.opacity=.13;
  const bubble=new T.Mesh(new T.SphereGeometry(.48,32,22),helm);
  bubble.scale.set(1,1.13,.93);bubble.position.set(0,1.17,.015);doc.root.add(bubble);
  ring(doc.root,0,.79,.02,.265,.026,enamel).rotation.x=Math.PI/2;
  for(const side of [-1,1]){
    const fin=new T.Mesh(new T.SphereGeometry(1,16,10),enamel);
    fin.scale.set(.13,.041,.21);fin.position.set(side*.15,.056,.151);doc.root.add(fin);
  }
  const bottle=new T.Mesh(new T.CapsuleGeometry(.096,.34,5,14),rim);
  bottle.position.set(-.07,.55,-.247);doc.root.add(bottle);
  const cockpit=new T.Group();station.add(cockpit);
  insetSurround(cockpit,.226,.32,enamel);
  barrel(cockpit,.227,.43,0,0,-.203,dark,true);barrel(cockpit,.225,.02,0,0,-.415,dark);
  ring(cockpit,0,0,.015,.227,.016,enamel);glazing(cockpit,.222,.023);
  // Every hull points along +X. The seat, controls and character share that heading.
  const helmSeat=new T.Group();
  helmSeat.rotation.y=Math.PI/2;
  helmSeat.position.set(-.035,-.245,-.115);
  cockpit.add(helmSeat);
  helmSeat.add(captain.root);
  captain.root.scale.setScalar(.245);
  panel(helmSeat,.175,.026,.15,0,.044,-.008,dark);
  panel(helmSeat,.17,.18,.025,0,.122,-.066,enamel).rotation.x=-.10;
  panel(helmSeat,.22,.033,.084,0,.14,.105,enamel);
  panel(helmSeat,.077,.005,.048,0,.16,.113,amber);
  const path=new T.CatmullRomCurve3([vec(0,-.22,-.275),vec(0,-.22,.26),vec(.10,-.16,.78),vec(.76,-.12,1.86),vec(2.05,-.29,3.6),vec(3.72,-.58,3.81),vec(5.02,-.62,3.60)]);
  const bubbles=new T.InstancedMesh(new T.SphereGeometry(1,10,8),new T.MeshStandardMaterial({color:0xc9fff5,transparent:true,opacity:.46,roughness:.19}),12);
  chamber.add(bubbles);
  const helper=new T.Object3D(),point=vec(0,0,0),eye=vec(0,0,0),aim=vec(0,0,0);
  function select(id){
    const shark=id==="swift";
    chamber.scale.setScalar(shark?.87:1);
    chamber.position.set(-.16,shark?.03:.13,shark?.50:id==="stone"?.71:.73);
    cockpit.scale.setScalar(shark?.87:1);
    cockpit.position.set(.63,shark?.125:.32,shark?.414:id==="stone"?.65:.64);
    portRadii.value.set(.314*chamber.scale.x,.222*cockpit.scale.x);
    ports.value.set(chamber.position.x,chamber.position.y,cockpit.position.x,cockpit.position.y);
    shell.color.setHex(id==="swift"?0xdf7440:id==="stone"?0x486e97:0xf3b850);
  }
  select("echo");
  return {select, update(s){
    station.visible=!!s.boat;cutEnabled.value=station.visible?1:0;
    if(!station.visible)return;
    const out=s.step==="crewOut",back=s.step==="crewBack",outside=["arm","escape","lower"].includes(s.step);
    const sec=s.crewTime||0;
    const u=out?smooth(sec,6.15,13.1):back?1-smooth(sec,.8,8.1):outside?1:0;
    const opened=out?smooth(sec,4.45,6.05)*(1-smooth(sec,9.8,11.4)):back?smooth(sec,2.0,3.65)*(1-smooth(sec,8.65,10.30)):0;
    const unseated=out?smooth(sec,4.02,4.4)*(1-smooth(sec,11.4,11.85)):back?smooth(sec,1.65,1.98)*(1-smooth(sec,10.3,10.8)):0;
    const level=out?smooth(sec,1.4,3.95):back?1-smooth(sec,10.95,13.55):outside?1:0;
    inner.position.x=out?-.49*(1-smooth(sec,.10,1.26)):back?-.49*smooth(sec,13.75,14.85):0;
    water.position.y=T.MathUtils.lerp(-.31,.31,level);
    water.scale.x=2*Math.sqrt(Math.max(0,.311**2-water.position.y**2));
    water.visible=level>.004 && level<.996;
    wetFace.visible=water.visible;
    wetFace.material.uniforms.height.value=water.position.y;
    wetFace.material.uniforms.t.value=s.time;
    door.position.set(-.70*opened,0,.056+.041*unseated+.041*opened);
    lamp.color.setHex(level>.99?0x7fdac3:0xedb057);lamp.emissive.copy(lamp.color);
    path.getPointAt(u,point);
    point.multiplyScalar(T.MathUtils.lerp(1,1/chamber.scale.x,smooth(u,.2,1)));
    doc.root.position.copy(point);
    const moving=(out||back)?smooth(u,0,.17)*(1-smooth(u,.86,1)):0;
    doc.root.position.y+=.086*level+.026*u*Math.sin(s.time*2);
    const heading=path.getTangentAt(u,eye);
    const turn=back?Math.PI*smooth(sec,.15,1.25)*(1-smooth(sec,8.15,9.05)):0;
    doc.root.rotation.order="YXZ";
    doc.root.rotation.y=.07+Math.atan2(heading.x,heading.z)*moving+turn-.62*smooth(u,.80,1);
    doc.root.rotation.x=.91*moving;
    doc.pose(s.time,moving*.66,(outside?.32:0)+(out?smooth(sec,12.65,13.4):back?1-smooth(sec,.1,1.35):0));
    doc.head.rotation.x=-.67*moving;
    if(back && sec>14.55){
      const enter=smooth(sec,14.55,16.2);
      doc.root.position.z-=.48*enter;
      doc.root.position.x-=.16*enter;
      doc.root.visible=enter<.98;
    }else doc.root.visible=out||back||outside||s.line==="cage";
    captain.pose(s.time);
    bubbles.visible=water.visible || (opened>.12 && opened<.96);
    if(bubbles.visible){
      for(let i=0;i<bubbles.count;i++){
        const a=i*2.399,v=(sec*.62+i*.216)%1;
        helper.position.set(.17*Math.cos(a)+.026*v,water.visible?-.29+v*(water.position.y+.29):-.1+v*.71,water.visible?-.22+.09*Math.sin(a):.08+.08*Math.sin(a));
        helper.scale.setScalar(.009+.018*(1-v));helper.updateMatrix();bubbles.setMatrixAt(i,helper.matrix);
      }bubbles.instanceMatrix.needsUpdate=true;
    }
  }, shot(s,camera,reef){
    if(s.step!=="crewOut" && s.step!=="crewBack")return;
    const sec=s.crewTime,back=s.step==="crewBack";
    const wide=back?1-smooth(sec,.7,5.8):smooth(sec,6.0,11.8);
    aim.copy(doc.root.position);aim.y+=.25;aim.x-=.105*(1-wide);
    eye.copy(aim);eye.x+=.43-wide*.90;eye.y+=.27+.33*wide;eye.z+=1.74+.96*wide;
    chamber.localToWorld(eye);chamber.localToWorld(aim);
    reef.cameraArm(aim,eye,.14);
    const blend=back?1:smooth(sec,0,1.1)*(1-smooth(sec,13.18,14.04));
    point.copy(camera.position);const from=camera.quaternion.clone();
    camera.position.copy(eye);camera.lookAt(aim);
    camera.position.lerp(point,1-blend);camera.quaternion.slerp(from,1-blend);
  }};
}
