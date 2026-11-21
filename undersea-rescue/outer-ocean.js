import * as T from "three";
import { floorHeight } from "./reef-life.js";
import { schoolBatch } from "./shoal.js";

export const habitats=[
  {x:1,z:23,fish:0},{x:8,z:37,fish:1},{x:13,z:54,fish:2},
  {x:-2,z:33,fish:2,mid:true},{x:.8,z:46,fish:0,mid:true},
  {x:2.8,z:59,fish:1,mid:true},{x:-2,z:65,fish:0},{x:6,z:72,fish:2},
  {x:-19,z:57,fish:1},{x:-45,z:18,fish:0},{x:-58,z:-27,fish:2},
  {x:50,z:-8,fish:1},{x:62,z:-44,fish:0},{x:5,z:-73,fish:2},
  {x:-17,z:-92,fish:1},{x:21,z:-106,fish:2},
];
const limits={left:-31,right:31.7,north:-60,south:19.75};
function height(x,z){
  const dx=Math.max(0,limits.left-x,x-limits.right),dz=Math.max(0,limits.north-z,z-limits.south);
  const distance=Math.max(dx,dz);
  const depth=34*T.MathUtils.smoothstep(distance,0,46);
  return floorHeight(x,z)-depth+T.MathUtils.smoothstep(distance,1,23)*(1.7*Math.sin(x*.13+z*.06)+1.2*Math.cos(z*.17));
}
export function oceanFloor(material){
  const perimeter=[];
  for(let i=0;i<114;i++)perimeter.push([-31+i*.55,-60]);
  for(let j=0;j<145;j++)perimeter.push([31.7,-60+j*.55]);
  for(let i=0;i<114;i++)perimeter.push([31.7-i*.55,19.75]);
  for(let j=0;j<145;j++)perimeter.push([-31,19.75-j*.55]);
  const p=[],uv=[],color=[],index=[],n=perimeter.length;
  // Nested rings share the canyon's actual edge vertices; the outer ring lies beyond the navigable bounds.
  for(let row=0;row<=27;row++){
    const d=row*3;
    for(let i=0;i<n;i++){
      const [a,b]=perimeter[i],x=a+(a-.35)*d/31.35,z=b+(b+20.125)*d/39.875;
      const v=.86+.06*Math.sin(z*.8+x*.3);
      p.push(x,height(x,z),z);uv.push(x/3.8,z/3.8);color.push(v,v*.94,v*.78);
      if(row){const j=(i+1)%n,k=row*n;index.push(k-n+i,k+j,k+i,k-n+i,k-n+j,k+j)}
    }
  }
  const g=new T.BufferGeometry();
  g.setAttribute("position",new T.Float32BufferAttribute(p,3));g.setAttribute("uv",new T.Float32BufferAttribute(uv,2));g.setAttribute("color",new T.Float32BufferAttribute(color,3));g.setIndex(index);g.computeVertexNormals();
  const m=new T.Mesh(g,material);m.receiveShadow=true;return m;
}

export function offshoreAnimals(scene,sources,actor,reef,mobileMeshes,light,mobile) {
  const count=mobile?18:28,range=mobile?32:44;
  const batches=[sources.anthias,sources.tang,sources.mackerel].map((s,i)=>{
    const mesh=schoolBatch(s,count*habitats.length,[.23,.31,.42][i],false,light,mobileMeshes);
    mesh.count=0;mesh.instanceMatrix.setUsage(T.DynamicDrawUsage);scene.add(mesh);return mesh;
  });
  const spots=habitats.map(({x,z,fish,mid},i)=>({x,z,fish,y:mid ? Math.min(6,reef.ground(x,z)*.77+3.8) : reef.ground(x,z)+(fish===2?6:3.3),phase:i*2.19}));
  const turtles=Array.from({length:mobile?2:3},(_,i)=>{
    const node=actor(sources.turtle,1.15+i*.24);
    node.visible=false;node.traverse(m=>{m.castShadow=false});
    return {node,flippers:["FrontFlipper_L","FrontFlipper_R"].map(k=>node.getObjectByName(k)),hit:{center:node.position,radius:.65}};
  });
  const dummy=new T.Object3D(), visible=[],counts=[0,0,0];
  let next=-1;
  return (t,eye,on)=>{
    if(!on){for(const m of batches)m.visible=false;for(const a of turtles)a.node.visible=false;next=-1;return []}
    if(t>=next || t<next-1){
      next=t+.09;visible.length=0;counts.fill(0);
      for(const s of spots){
        const d=(s.x-eye.x)**2+(s.z-eye.z)**2+(s.y-eye.y)**2;
        if(d>range*range)continue;
        visible.push({s,d});
        for(let i=0;i<count;i++){
          const q=i*2.39996,a=t*.18+s.phase+.125*Math.sin(q);
          const x=s.x+4.3*Math.sin(a)+2.1*Math.cos(q),z=s.z+2.5*Math.cos(a)+1.6*Math.sin(q);
          dummy.position.set(x,Math.max(s.y+.65*Math.sin(q*1.5)+.25*Math.cos(a*2),height(x,z)+1.1),z);
          dummy.rotation.y=Math.atan2(2.5*Math.sin(a),4.3*Math.cos(a));
          dummy.scale.setScalar(.77+(i*7%13)*.04);dummy.updateMatrix();
          batches[s.fish].setMatrixAt(counts[s.fish]++,dummy.matrix);
        }
      }
      for(const [i,b] of batches.entries()){b.count=counts[i];b.visible=!!b.count;b.instanceMatrix.needsUpdate=true}
      visible.sort((a,b)=>a.d-b.d);
    }
    const hits=[];
    for(let i=0;i<turtles.length;i++){
      const {node,flippers,hit}=turtles[i],s=visible[i]?.s;
      node.visible=!!s;if(!s)continue;
      const a=t*.12+s.phase,stroke=t*1.8+s.phase;
      const x=s.x+3.5*Math.sin(a),z=s.z+3.2*Math.cos(a);
      node.position.set(x,Math.max(height(x,z)+1.2,s.y+.75*Math.cos(a*.8)),z);
      node.rotation.set(.07*Math.sin(a),Math.atan2(3.5*Math.cos(a),-3.2*Math.sin(a)),.045*Math.cos(a));
      const effort=.3+.7*(.5+.5*Math.cos(t*.72+s.phase));
      flippers.forEach((f,k)=>{if(f){f.rotation.y=(k?-1:1)*(.11+.46*effort*Math.sin(stroke));f.rotation.z=(k?-1:1)*.22*effort*Math.cos(stroke)}});
      reef.separate(node.position,hit.radius);hits.push(hit);
    }
    return hits;
  };
}
