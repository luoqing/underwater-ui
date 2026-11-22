import * as T from "three";
import { floorHeight } from "./reef-life.js";
import { schoolBatch } from "./shoal.js";

import { mantaSwim } from "./manta-swim.js";
import { shoals, wildlifeSites } from "./wildlife-layout.js";
import { habitats } from "./bluewater-layout.js";
import { whaleRockClearance } from "./whale-clearance.js";
import { whaleMotion } from "./whale-swim.js";
const limits={left:-31,right:31.7,north:-60,south:19.75};
function height(x,z){
  const dx=Math.max(0,limits.left-x,x-limits.right),dz=Math.max(0,limits.north-z,z-limits.south);
  const margin=Math.hypot(dx,dz);
  const coast=5*Math.sin(x*.054+z*.039)+3.4*Math.sin(z*.097-x*.031);
  const distance=margin+coast*T.MathUtils.smoothstep(margin,4,38);
  const depth=34*T.MathUtils.smoothstep(distance,0,46)+84*T.MathUtils.smoothstep(distance,45,106);
  const relief=(4*Math.sin(x*.083)+3*Math.cos((x*.43+z)*.085))*T.MathUtils.smoothstep(distance,39,73);
  const natural=relief+floorHeight(x,z)-depth+T.MathUtils.smoothstep(distance,1,23)*(1.7*Math.sin(x*.13+z*.06)+1.2*Math.cos(z*.17));
  // A sediment bench supports the four feet, the docking lane, and the release shot.
  const apron=Math.hypot((x-1.3)/13,(z+76)/12);
  const shelf=-6.8+.08*Math.sin(.38*x+.29*z);
  const blend=(1-T.MathUtils.smoothstep(apron,.90,1.60))*T.MathUtils.smoothstep(-z,60,65);
  return T.MathUtils.lerp(natural,shelf,blend);
}
export function oceanFloor(material){
  const perimeter=[];
  for(let i=0;i<114;i++)perimeter.push([-31+i*.55,-60]);
  for(let j=0;j<145;j++)perimeter.push([31.7,-60+j*.55]);
  for(let i=0;i<114;i++)perimeter.push([31.7-i*.55,19.75]);
  for(let j=0;j<145;j++)perimeter.push([-31,19.75-j*.55]);
  const p=[],uv=[],color=[],index=[],n=perimeter.length;
  // Nested rings share the canyon's actual edge vertices; the outer ring lies beyond the navigable bounds.
  for(let row=0;row<=48;row++){
    const d=row*3;
    for(let i=0;i<n;i++){
      const [a,b]=perimeter[i],x=a+(a-.35)*d/31.35,z=b+(b+20.125)*d/39.875;
      const y=height(x,z);
      const slope=Math.hypot(height(x+1,z)-y,height(x,z+1)-y);
      const cliff=T.MathUtils.smoothstep(slope,.64,1.65);
      const v=(.86+.06*Math.sin(z*.8+x*.3))*(1-.56*cliff);
      p.push(x,y,z);uv.push(x/3.8,(z+y*.4)/3.8);color.push(v*(1-.19*cliff),v*.94,v*(.78+.12*cliff));
      if(row){const j=(i+1)%n,k=row*n;index.push(k-n+i,k+j,k+i,k-n+i,k-n+j,k+j)}
    }
  }
  const g=new T.BufferGeometry();
  g.setAttribute("position",new T.Float32BufferAttribute(p,3));g.setAttribute("uv",new T.Float32BufferAttribute(uv,2));g.setAttribute("color",new T.Float32BufferAttribute(color,3));g.setIndex(index);g.computeVertexNormals();
  const m=new T.Mesh(g,material);m.receiveShadow=true;return m;
}

export function offshoreAnimals(scene,sources,actor,reef,mobileMeshes,light,mobile) {
  const maxSchools=mobile?10:15;
  const batches=shoals.map(spec=>{
    const mesh=schoolBatch(sources[spec.key],maxSchools*spec.count,spec.length,false,light,mobileMeshes,true);
    mesh.instanceMatrix.setUsage(T.DynamicDrawUsage);mesh.count=0;scene.add(mesh);return mesh;
  });
  const spots=wildlifeSites(habitats,height).map((h,i)=>{
    const bottom=height(h.x,h.z);
    const y=h.level===undefined ? bottom+(h.clearance ?? (h.fish===2?6:2.4)) : T.MathUtils.lerp(bottom,6,h.level);
    return {...h,y,phase:i*2.19,fishData:null};
  });
  const forward=new T.Vector3(),ahead=new T.Vector3(),dummy=new T.Object3D(),previous=new T.Vector3(),delta=new T.Vector3();
  const roster=["turtle","ray","whale","jelly","turtle","ray","lion","turtle"];
  if(!mobile)roster.push("whale","jelly","lion");
  const slots=roster.map((type,i)=>({type,seed:i*1.73,site:null,node:null}));
  let next=-1,active=[],ready=[],moves=0;
  const stats={prepared:0,schools:0,fish:0,animals:"",species:"",bursting:0};
  function prepare(s){
    if(s.fishData)return;
    const spec=shoals[s.fish];
    const count=Math.max(1,Math.round(spec.count*(spec.count<4?1:.84+.16*Math.cos(s.phase))));
    s.fishData=Array.from({length:count},(_,i)=>{
      const a=i*2.39996+s.phase,r=Math.sqrt((i+.5)/count);
      return {across:spec.width*r*Math.cos(a),along:spec.train*r*Math.sin(a),
        up:spec.rise*Math.sin(a*1.51)*r,phase:a,scale:.83+(i*7%13)*.036,
        floorAt:-Infinity,bottom:0,lead:0,beat:0,effort:0,yaw:0,last:new T.Vector3(),placed:false};
    });
  }
  function load(slot){
    const size={turtle:1.48,ray:3.6,whale:8.7,lion:.48,jelly:.80}[slot.type]*(1+.12*Math.sin(slot.seed));
    if(slot.type==="jelly"){
      slot.members=Array.from({length:mobile?4:6},(_,i)=>{
        const node=sources.jelly.clone();
        scene.add(node);
        return {node,radius:size*(.48+.06*i),center:node.position,phase:slot.seed+i*2.4};
      });
      slot.node=slot.members[0].node;
      return;
    }
    const node=actor(sources[slot.type],size,true);node.visible=false;
    node.traverse(m=>{m.castShadow=false});
    slot.node=node;
    if(slot.type==="whale"){slot.motion=whaleMotion(node,2,light);slot.rocks=whaleRockClearance(node,size,reef)}
    if(slot.type==="ray")slot.poseRay=mantaSwim(node,slot.seed);
    slot.flippers=["FrontFlipper_L","FrontFlipper_R"].map(k=>node.getObjectByName(k));
    slot.fins=[];if(slot.type==="lion")node.traverse(m=>{if(m.morphTargetInfluences)slot.fins.push(m)});
    slot.hit={center:node.position,radius:size*.52};
  }
  // Lay out the entire circuit before the animal arrives. A ledge beyond the
  // next bend must influence the approach, not jerk the body up at its edge.
  function cruisingDepths(site,rx,rz,body,ceiling) {
    const n=96,depths=new Float64Array(n),step=2*Math.PI*Math.min(rx,rz)/n;
    for(let i=0;i<n;i++){
      const a=i/n*2*Math.PI,x=site.x+rx*Math.sin(a),z=site.z+rz*Math.cos(a);
      let bottom=reef.ground(x,z);
      for(const [dx,dz] of [[body*.7,0],[-body*.7,0],[0,body*.7],[0,-body*.7]])
        bottom=Math.max(bottom,reef.ground(x+dx,z+dz));
      depths[i]=Math.min(ceiling,Math.max(site.y+.35*Math.cos(a),bottom+body+.72));
    }
    for(let i=0;i<n*2;i++){
      const k=i%n,j=n-1-k;
      depths[k]=Math.max(depths[k],depths[(k+n-1)%n]-.26*step);
      depths[j]=Math.max(depths[j],depths[(j+1)%n]-.26*step);
    }
    return depths;
  }
  function show(slot,on){
    if(slot.members)for(const member of slot.members)member.node.visible=on;
    else if(slot.node)slot.node.visible=on;
  }
  function distance(s,p){return Math.hypot(s.x-p.x,s.y-p.y,s.z-p.z)}
  function plan(boat,t){
    boat.heading(forward);
    if(boat.throttle()<-.1)forward.negate();
    ahead.copy(boat.position).addScaledVector(forward,22+boat.velocity.length()*2);
    ready=spots.filter(s=>distance(s,ahead)<53 || distance(s,boat.position)<55);
    ready.sort((a,b)=>distance(a,boat.position)-distance(b,boat.position));
    for(const s of ready)prepare(s);
    for(const s of spots)if(s.fishData && !ready.includes(s) && distance(s,boat.position)>85)s.fishData=null;
    const aheadRank=s=>distance(s,boat.position)*.5+distance(s,ahead)*.5;
    const candidates=ready.filter(s=>distance(s,boat.position)<54 || distance(s,ahead)<38);
    // A nearby established school keeps its place. Give new species a turn
    // before a third cluster of the same fish occupies the view.
    const old=new Set(active),used=shoals.map(()=>0);
    active=[];
    while(active.length<maxSchools && candidates.length){
      let best=0,score=Infinity;
      for(let i=0;i<candidates.length;i++){
        const s=candidates[i],rank=aheadRank(s)+used[s.fish]*6-(old.has(s)?4:0);
        if(rank<score){score=rank;best=i}
      }
      const s=candidates.splice(best,1)[0];active.push(s);used[s.fish]++;
      if(!old.has(s))for(const f of s.fishData){f.placed=false;f.floorAt=-Infinity;f.lead=f.effort=0}
    }
    stats.species=shoals.filter((_,i)=>used[i]).map(s=>s.key).join(",");
    for(const slot of slots){
      if(slot.site){
        const d=distance(slot.site,boat.position);
        const behind=(slot.site.x-boat.position.x)*forward.x+(slot.site.z-boat.position.z)*forward.z;
        if(d>78 || (d>34 && behind<-26))slot.site=null;
      }
      if(!slot.site){
        const candidates=ready.filter(s=>s.animal===slot.type && !slots.some(v=>v.site===s)
          && (distance(s,boat.position)<34 || (s.x-boat.position.x)*forward.x+(s.z-boat.position.z)*forward.z>-26));
        const s=candidates.sort((a,b)=>aheadRank(a)-aheadRank(b))[0];
        if(s){slot.site=s;if(!slot.node)load(slot);slot.motion?.reset();slot.rocks?.reset();slot.placed=false;slot.extraTime=0;slot.alarm=0;
          slot.course=slot.type==="whale"?cruisingDepths(s,13,8,slot.hit.radius,5.8):
            slot.type==="ray"?cruisingDepths(s,6.4,4.5,slot.hit.radius,8):null;
          slot.lastCourse=new T.Vector3();}
      }
      show(slot,!!slot.site);
    }
    stats.prepared=ready.length;stats.schools=active.length;next=t+.22;
  }
  function update(t,dt,boat,on,wake){
    if(!on){for(const m of batches)m.visible=false;for(const a of slots){show(a,false);a.site=null}next=-1;return []}
    if(t>=next || t<next-1)plan(boat,t);
    const counts=shoals.map(()=>0);
    let bursting=0;
    for(const s of active){
      const spec=shoals[s.fish],mesh=batches[s.fish];
      const stroke=mesh.geometry.attributes.strokePhase;
      // Groupers hold station, then change position along the reef in short bouts.
      const cycle=t*.22+s.phase;
      const patrol=spec.key==="grouper"?(Math.floor(cycle)+T.MathUtils.smoothstep(cycle%1,.47,1)-s.phase)/.22:t;
      const angle=patrol*spec.speed/spec.rx+s.phase;
      for(const f of s.fishData){
        const alarm=f.placed?wake.threat(f.last,spec.length*.53,f.phase):0;
        f.effort=T.MathUtils.damp(f.effort,alarm,alarm>f.effort?8:1.7,dt);
        f.lead=T.MathUtils.damp(f.lead,spec.burst*f.effort,1.8,dt);
        f.beat+=dt*2*Math.PI*spec.beat*(1+spec.burst*f.effort);
        if(f.effort>.18)bursting++;
        const a=angle+f.lead/spec.rx+.06*Math.sin(t*.6+f.phase);
        const cs=Math.cos(a),sn=Math.sin(a);
        const dx=spec.rx*cs,dz=-spec.rz*sn,mag=Math.hypot(dx,dz);
        const x=s.x+spec.rx*sn+(dx*f.along-dz*f.across)/mag;
        const z=s.z+spec.rz*cs+(dz*f.along+dx*f.across)/mag;
        if(t>=f.floorAt || t<f.floorAt-1){
          f.bottom=reef.ground(x,z)+spec.length*.66;
          f.floorAt=t+.36+.12*Math.cos(f.phase);
        }
        const desiredY=Math.min(8.8,Math.max(s.y+f.up+.13*Math.sin(a*2+f.phase),f.bottom));
        f.height=f.placed?T.MathUtils.damp(f.height,desiredY,4,dt):desiredY;
        dummy.position.set(x,f.height,z);
        wake.avoid(f,dummy.position,.55*spec.length,dt,f.phase);
        if(dummy.position.distanceToSquared(boat.position)<49)reef.separate(dummy.position,spec.length*.39);
        delta.subVectors(dummy.position,f.last);
        if(!f.placed || delta.lengthSq()>1 || delta.lengthSq()<1e-10)delta.set(dx,0,dz);
        const yaw=Math.atan2(-delta.z,delta.x);
        f.yaw=f.placed?f.yaw+Math.atan2(Math.sin(yaw-f.yaw),Math.cos(yaw-f.yaw))*
          (1-Math.exp(-dt*(8+f.effort*15))):yaw;
        f.last.copy(dummy.position);f.placed=true;
        dummy.rotation.set(0,f.yaw,T.MathUtils.clamp(Math.atan2(delta.y,Math.hypot(delta.x,delta.z)),-.5,.5),"YXZ");
        dummy.scale.setScalar(f.scale);dummy.updateMatrix();
        const index=counts[s.fish]++;
        mesh.setMatrixAt(index,dummy.matrix);stroke.setX(index,f.beat+f.phase);
      }
    }
    for(const [i,b] of batches.entries()){b.count=counts[i];b.visible=!!b.count;b.instanceMatrix.needsUpdate=true;b.geometry.attributes.strokePhase.needsUpdate=true}
    stats.fish=counts.reduce((a,b)=>a+b,0);
    stats.bursting=bursting;
    const hits=[];
    for(const slot of slots){
      const {site:s,node,type}=slot;if(!s)continue;
      const threat=slot.placed?wake.threat(node.position,slot.hit?.radius ?? .5,slot.seed):0;
      slot.alarm=T.MathUtils.damp(slot.alarm,threat,threat>slot.alarm?4.6:1.3,dt);
      const whale=type==="whale",ray=type==="ray",lion=type==="lion";
      slot.extraTime+=dt*slot.alarm*(whale?.36:ray?.95:lion?1.7:1.15);
      const local=t+slot.extraTime;
      if(type==="jelly"){
        for(const [i,member] of slot.members.entries()){
          const drift=.19*local+member.phase,phase=local*(2.65+i*.09)+member.phase;
          const pulse=(1+Math.sin(phase))*.5;
          const x=s.x+Math.cos(member.phase)*2.1+.63*Math.sin(drift);
          const z=s.z+Math.sin(member.phase)*2.4+.72*Math.cos(drift);
          member.node.position.set(x,Math.min(8.8,Math.max(reef.ground(x,z)+member.radius,s.y+i*.54+.35*Math.sin(drift)+.075*pulse)),z);
          wake.avoid(member,member.center,member.radius,dt,member.phase);
          const size=member.radius*1.70;
          member.node.scale.set(size*(1-.10*pulse),size*(1+.15*pulse),size*(1-.10*pulse));
          member.node.rotation.set(.09*Math.cos(drift),drift,.14*Math.sin(drift));
          hits.push(member);
        }
        slot.placed=true;continue;
      }
      const rx=whale?13:ray?6.4:lion?1.25:3.5,rz=whale?8:ray?4.5:lion?.82:3.2;
      const a=local*(whale?.18:ray?.19:lion?.12:.12)+s.phase,stroke=local*1.8+s.phase;
      previous.copy(node.position);
      const x=s.x+rx*Math.sin(a),z=s.z+rz*Math.cos(a);
      let y;
      if(slot.course){
        const depths=slot.course,u=T.MathUtils.euclideanModulo(a,2*Math.PI)*depths.length/(2*Math.PI);
        const i=Math.floor(u)%depths.length;
        y=T.MathUtils.lerp(depths[i],depths[(i+1)%depths.length],u-Math.floor(u));
      }else y=Math.min(8,Math.max(reef.ground(x,z)+slot.hit.radius+.5,s.y+.75*Math.cos(a*.8)));
      node.position.set(x,y,z);
      wake.avoid(slot,node.position,slot.hit.radius,dt,s.phase);
      // Compare two route samples in the same coordinate frame. The final
      // rock correction from the last frame is not a turn or a dive command.
      delta.subVectors(node.position,slot.course?slot.lastCourse:previous);
      slot.lastCourse.copy(node.position);
      if(!slot.placed || delta.lengthSq()>4 || delta.lengthSq()<1e-9)delta.set(rx*Math.cos(a),0,-rz*Math.sin(a));
      const targetPitch=Math.atan2(delta.y,Math.hypot(delta.x,delta.z));
      const targetYaw=ray?Math.atan2(delta.z,-delta.x):Math.atan2(delta.x,delta.z);
      let pitch=targetPitch,yaw=targetYaw;
      if(slot.course){
        const turn=slot.placed?Math.atan2(Math.sin(targetYaw-slot.yaw),Math.cos(targetYaw-slot.yaw)):0;
        const turnRate=whale?.52:.88;
        slot.yaw=slot.placed?slot.yaw+T.MathUtils.clamp(turn,-turnRate*dt,turnRate*dt):targetYaw;
        slot.pitch=T.MathUtils.damp(slot.placed?slot.pitch:0,T.MathUtils.clamp(targetPitch,-.31,.31),2.2,dt);
        pitch=slot.pitch;yaw=slot.yaw;
      }
      if(ray){
        node.rotation.set(-.12,yaw,-pitch*.65,"YXZ");
        slot.poseRay(local);
      }else if(lion){
        node.rotation.set(0,Math.atan2(-delta.z,delta.x),pitch*.42,"YXZ");
        for(const m of slot.fins){
          m.morphTargetInfluences[0]=Math.max(0,Math.sin(stroke))*.58;
          m.morphTargetInfluences[1]=Math.max(0,-Math.sin(stroke))*.58;
        }
      }else{
        node.rotation.set(-pitch*.45,yaw,.045*Math.cos(a),"YXZ");
      }
      slot.placed=true;
      const effort=Math.min(1,.3+.7*(.5+.5*Math.cos(local*.72+s.phase))+.55*slot.alarm);
      slot.flippers.forEach((f,k)=>{if(f){f.rotation.y=(k?-1:1)*(.11+.46*effort*Math.sin(stroke));f.rotation.z=(k?-1:1)*.22*effort*Math.cos(stroke)}});
      if(slot.motion){slot.rocks.update(dt);slot.motion.advance(dt*(1+.36*slot.alarm));slot.motion.pose(dt);hits.push(...slot.rocks.probes)}
      else hits.push(slot.hit);
    }
    if(++moves%12===0)stats.animals=slots.filter(k=>k.site).flatMap(k=>k.members?k.members.map(()=>"jelly"):[k.type]).join(",");
    return hits;
  }
  update.stats=stats;
  return update;
}
