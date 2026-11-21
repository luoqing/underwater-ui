import * as T from "three";
import { ConvexGeometry } from "three/addons/geometries/ConvexGeometry.js";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";

export const fleet = {
  echo:  { name:"听潮号", feature:"听声定位", turn:1.50, thrust:2.65, lift:.20, bearing:.46 },
  swift: { name:"虎鲨艇", feature:"快速转向", turn:2.0, thrust:3.25, lift:.18, bearing:.35 },
  stone: { name:"蓝鲸艇", feature:"稳稳托举", turn:1.25, thrust:2.55, lift:.29, bearing:.35 },
};

// Each station defines the axial position, half-width, half-height, and rise of the keel.
function buildVessel(shark) {
  const group=new T.Group();
  const enamel=color=>new T.MeshStandardMaterial({color,metalness:.18,roughness:.36});
  const paint=enamel(shark?0xed672e:0x305c89), cream=enamel(0xe5dbbc), trim=enamel(0x283c43);
  const glass=new T.MeshStandardMaterial({color:shark?0x263f48:0x178c9b,metalness:.55,roughness:.19});
  const lamps=new T.MeshStandardMaterial({color:0xfff1bf,emissive:0xffcc88,emissiveIntensity:.45});
  const stations=shark?
    [[-1.68,.07,.08,.08],[-1.39,.21,.22,.07],[-.86,.38,.36,.05],[-.22,.49,.43,.04],[.45,.47,.38,.01],[1.03,.36,.26,-.05],[1.48,.19,.12,-.1],[1.73,.015,.014,-.13]]:
    [[-1.63,.045,.045,.35],[-1.38,.19,.20,.25],[-1.04,.36,.37,.12],[-.66,.58,.53,.06],[-.15,.69,.62,.04],[.55,.73,.65,.04],[.98,.70,.61,.07],[1.27,.55,.49,.08],[1.44,.34,.32,.08],[1.52,.01,.01,.07]];
  function skin(x,a,clearance=0) {
    let i=1;
    while(i<stations.length-1 && x>stations[i][0]) i++;
    const left=stations[i-1],right=stations[i],u=T.MathUtils.clamp((x-left[0])/(right[0]-left[0]),0,1);
    const at=k=>T.MathUtils.lerp(left[k],right[k],u);
    return [x,at(3)+(at(2)+clearance)*Math.sin(a),(at(1)+clearance)*Math.cos(a)];
  }
  // Sample patches on the same hull surface so window seals and markings follow its curvature.
  function panel(material,point,rows=30,cols=48) {
    const p=[],indices=[];
    for(let i=0;i<=rows;i++)for(let j=0;j<=cols;j++){
      p.push(...point(i/rows,j/cols));
      const k=i*(cols+1)+j;
      if(i && j)indices.push(k-cols-2,k-1,k,k-cols-2,k,k-cols-1);
    }
    const g=new T.BufferGeometry();g.setAttribute("position",new T.Float32BufferAttribute(p,3));g.setIndex(indices);g.computeVertexNormals();group.add(new T.Mesh(g,material));
  }
  const start=stations[0][0],end=stations.at(-1)[0],full=Math.PI*2;
  const tau=(u,v)=>skin(T.MathUtils.lerp(start,end,u),-.5*Math.PI+v*full);
  panel(paint,tau);
  panel(cream,(u,v)=>skin(T.MathUtils.lerp(start,end,u),Math.PI*1.17+v*Math.PI*.66,.006),30,20);
  function oval(mat,x,y,z,sx,sy,sz) {
    const o=new T.Mesh(new T.SphereGeometry(1,24,14),mat);
    o.position.set(x,y,z);o.scale.set(sx,sy,sz);group.add(o);return o;
  }
  function foil(points,thickness,vertical=false){
    const ring=points.map(p=>new T.Vector3(...p)), center=new T.Vector3();
    for(const v of ring)center.add(v);
    center.divideScalar(ring.length);
    const axis=vertical?new T.Vector3(0,0,1):new T.Vector3(0,1,0);
    const vertices=ring.flatMap(v=>[-1,1].map(s=>v.clone().addScaledVector(axis,s*.008)));
    for(const v of ring)for(const s of [-1,1])vertices.push(v.clone().lerp(center,.40).addScaledVector(axis,s*thickness));
    group.add(new T.Mesh(new ConvexGeometry(vertices),paint));
  }
  if(shark){
    oval(trim,.44,.28,0,.69,.25,.36);oval(glass,.46,.305,0,.62,.245,.33);
    for(let i=0;i<3;i++){
      panel(trim,(u,v)=>{const a=-.18+v*(Math.PI+.36);
        return skin(-.96+i*.38+.20*Math.sin(a)+u*(.05+.12*Math.sin(a)),a,.011)},3,32);
    }
    foil([[-.73,.29,0],[.13,.37,0],[-.72,1.00,0],[-.79,.78,0]],.05,true);
    foil([[-1.31,.06,0],[-1.80,.83,0],[-1.73,.28,0],[-1.61,.03,0],[-1.76,-.49,0]],.044,true);
  }else{
    // Wrap the visor around the rounded bow; use the angular coordinate around X on each side.
    for(const side of [0,1]){
      const visor=(edge,u,v)=>{
        const a=T.MathUtils.lerp(-.18-edge,.45+edge,v);
        const aft=.59+.14*Math.pow(2*v-1,2)-edge;
        return skin(T.MathUtils.lerp(aft,end-.002,u),side?Math.PI-a:a,.014+(.03-edge)*.15);
      };
      for(const [mat,edge] of [[trim,.035],[glass,0]]) panel(mat,(u,v)=>visor(edge,side?1-u:u,v),18,12);
    }
    oval(paint,-.22,.65,0,.24,.11,.18);
  }
  for(const s of [-1,1]){
    if(shark){
      foil([[.39,-.07,s*.33],[-.27,-.13,s*.44],[-.84,-.29,s*1.22],[-.63,-.23,s*1.28]],.039);
      oval(lamps,1.15,-.038,s*.306,.16,.029,.035);
    }else{
      foil([[.12,-.09,s*.56],[-.54,-.13,s*.49],[-.84,-.28,s*1.06],[-.53,-.30,s*1.17]],.072);
      foil([[-1.36,.28,s*.02],[-1.76,.38,s*.01],[-1.98,.42,s*.65],[-1.77,.45,s*1.00],[-1.50,.43,s*.65]],.058);
      for(const x of [-.56,-.16,.24]){
        const [px,py,pz]=skin(x,.19);
        oval(trim,px,py,s*pz,.129,.129,.038);
        oval(glass,px,py,s*(pz+.022),.098,.098,.03);
      }
      oval(lamps,.93,-.22,s*.59,.13,.035,.038);
    }
  }
  const rotors=[];
  function duct(x,y,z,radius){
    const ring=new T.Mesh(new T.TorusGeometry(radius,.055,10,32),paint);
    ring.position.set(x,y,z);ring.rotation.y=Math.PI/2;group.add(ring);
    oval(trim,x+.017,y,z,.066,radius,radius);
    const blades=[];
    for(let i=0;i<4;i++){
      const a=i*Math.PI/2;
      const blade=oval(cream,x-.056,y+radius*.48*Math.cos(a),z+radius*.48*Math.sin(a),.017,radius*.45,.026);
      blade.position.set(0,radius*.48*Math.cos(a),radius*.48*Math.sin(a));
      blade.rotation.x=a+.23;blade.updateMatrix();
      blades.push(blade.geometry.applyMatrix4(blade.matrix));
      group.remove(blade);

    }
    const rotor=new T.Mesh(mergeGeometries(blades),cream);
    rotor.name="Propeller";rotor.position.set(x-.071,y,z);rotors.push(rotor);
  }
  if(shark)duct(-1.59,.07,0,.19);
  else for(const s of [-1,1]){oval(paint,-.40,-.34,s*.56,.44,.19,.20);duct(-.75,-.34,s*.56,.157)}
  // Static components use one draw call per finish on the selected boat.
  const parts=new Map();
  for(const node of group.children){
    node.updateMatrix();
    const g=node.geometry.index?node.geometry.toNonIndexed():node.geometry;
    g.deleteAttribute("uv");g.applyMatrix4(node.matrix);
    if(!parts.has(node.material))parts.set(node.material,[]);
    parts.get(node.material).push(g);
  }
  group.clear();
  for(const [mat,partsOfFinish] of parts)group.add(new T.Mesh(mergeGeometries(partsOfFinish),mat));
  group.add(...rotors);
  return group;
}

export function fitSub(model) {
  const echo=new T.Group();
  echo.add(...model.children.slice());
  const originalBlades=[];
  echo.traverse(n=>{if(n.isMesh && /^Impeller[ _]vane/.test(n.name))originalBlades.push(n)});
  for(const side of [-1,1]){
    const blades=originalBlades.filter(b=>Math.sign(b.position.z)===side);
    if(!blades.length)continue;
    const rotor=new T.Group();
    rotor.name="Propeller";
    for(const b of blades)rotor.position.add(b.position);
    rotor.position.divideScalar(blades.length);echo.add(rotor);
    for(const b of blades)rotor.attach(b);
  }
  const variants={echo,swift:buildVessel(true),stone:buildVessel(false)};
  model.add(...Object.values(variants));
  model.scale.setScalar(.69);
  const props={};
  for(const [key,node] of Object.entries(variants)){
    props[key]=[];
    node.traverse(part=>{if(part.name==="Propeller")props[key].push(part)});
  }
  let selected="echo",rate=0;
  return {
    select(id){
      selected=Object.hasOwn(variants,id)?id:"echo";rate=0;
      for(const [key,node] of Object.entries(variants))node.visible=key===selected;
    },
    spin(dt,throttle){
      rate=T.MathUtils.damp(rate,throttle*17,throttle===0?3.4:4.8,dt);
      if(Math.abs(rate)<.01)rate=0;
      props[selected].forEach((rotor,i)=>{rotor.rotation.x=(rotor.rotation.x+rate*dt*(i?-1:1))%(2*Math.PI)});
    }
  };
}
