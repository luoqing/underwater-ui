import * as T from "three";
import { RoundedBoxGeometry } from "three/addons/geometries/RoundedBoxGeometry.js";

const sphere=new T.SphereGeometry(1,32,22);
const colors={ snow:0xfff7e7, face:0xffefd9, blush:0xeab3a6, ink:0x182735, navy:0x1f5275, blue:0x54b2ce, ice:0xbcebe5, orange:0xf4af43, belt:0x244455, mint:0x3eafa9 };
const materials=Object.fromEntries(Object.entries(colors).map(([k,color])=>[k,new T.MeshStandardMaterial({color,roughness:.52})]));
function mesh(parent,g,m,x,y,z){
  const part=new T.Mesh(g,materials[m]||m); part.position.set(x,y,z);parent.add(part);return part;
}
function oval(p,m,x,y,z,a,b,c){
  const o=mesh(p,sphere,m,x,y,z);o.scale.set(a,b,c);return o;
}
function box(p,m,x,y,z,a,b,c,r){
  return mesh(p,new RoundedBoxGeometry(a,b,c,3,r),m,x,y,z);
}
function group(p,x,y,z){const g=new T.Group();g.position.set(x,y,z);p.add(g);return g;}
function stroke(p,m,coords,r){
  const points=coords.map(v=>new T.Vector3(...v));
  return mesh(p,new T.TubeGeometry(new T.CatmullRomCurve3(points),12,r,7,false),m,0,0,0);
}
function cap(head,bear){
  const hat=group(head,0,bear?.45:.41,.005);
  hat.rotation.z=bear?-.045:.10;
  oval(hat,"navy",0,0,.006,.33,.078,.26);
  oval(hat,"blue",0,.072,-.01,.354,.115,.263);
  if(bear)oval(hat,"navy",0,-.011,.22,.29,.035,.15);
  const badge=group(hat,0,.069,.249);
  oval(badge,"snow",0,.014,0,.043,.036,.012);
  for(const x of [-.027,0,.027])oval(badge,"snow",x,-.016,0,.009,.018,.009);
}
export function crewFigure(kind,{seated=false}={}){
  const bear=kind==="captain",root=new T.Group(),body=group(root,0,0,0);
  root.name=bear?"巴克队长":"皮医生";
  oval(body,bear?"navy":"ink",0,.54,0,.315,.39,.25);
  if(!bear)oval(body,"snow",0,.48,.16,.247,.30,.11);
  for(const side of [-1,1]){
    if(seated){
      oval(body,"navy",side*.15,.32,.10,.114,.103,.20);
      oval(body,"navy",side*.15,.21,.225,.11,.135,.105);
    }else oval(body,"navy",side*.15,.17,0,.114,.155,.123);
    const footZ=seated?.225:0;
    oval(body,"blue",side*.15,.091,footZ+.032,.135,.098,.162);
    oval(body,"ice",side*.15,.034,footZ+.038,.13,.032,.155);
  }
  const neck=group(body,0,.79,.01);
  for(const side of [-1,1]){
    const collar=oval(neck,"blue",side*.104,-.007,.17,.14,.055,.075);
    collar.rotation.z=side*.52;
  }
  if(bear){
    oval(body,"belt",0,.38,0,.319,.055,.244);
    box(body,"blue",0,.38,.246,.13,.075,.025,.013);
  }else{
    stroke(body,"mint",[[-.2,.78,.14],[-.08,.65,.24],[.06,.52,.273],[.23,.38,.19]],.025);
    stroke(body,"mint",[[-.2,.78,.14],[-.22,.79,0],[-.20,.72,-.16],[-.08,.62,-.235],[.12,.49,-.218],[.24,.38,-.10],[.26,.36,.09]],.024);
    const bag=group(body,.267,.37,.09);bag.rotation.z=-.11;
    box(bag,"ice",0,0,0,.24,.215,.135,.036);
    box(bag,"snow",0,.048,.061,.245,.10,.044,.02);
    box(bag,"mint",0,0,.087,.09,.027,.01,.003);
    box(bag,"mint",0,0,.088,.027,.09,.01,.003);
  }
  const arms=[-1,1].map(s=>{
    const joint=group(body,s*.273,.727,.004);
    const arm=oval(joint,bear?"navy":"ink",s*.041,-.139,.013,bear?.113:.095,.202,bear?.115:.078);
    arm.rotation.z=s*.20;
    if(bear)oval(joint,"snow",s*.077,-.282,.021,.088,.101,.085);
    return joint;
  });
  const head=group(body,0,1.13,.005);
  oval(head,bear?"snow":"ink",0,0,0,bear?.456:.423,bear?.413:.40,bear?.367:.333);
  if(bear){
    for(const s of [-1,1]){
      oval(head,"snow",s*.346,.275,-.002,.131,.138,.10);
      oval(head,"face",s*.355,.282,.071,.072,.08,.046);
    }
    oval(head,"face",0,-.108,.289,.255,.155,.123);
    oval(head,"ink",0,-.048,.398,.095,.057,.035);
    stroke(head,"belt",[[0,-.097,.404],[0,-.128,.402],[-.046,-.148,.399],[-.082,-.14,.391]],.009);
    stroke(head,"belt",[[0,-.128,.402],[.046,-.147,.399],[.081,-.137,.391]],.009);
  }else{
    for(const s of [-1,1]){
      const cheek=oval(head,"snow",s*.144,-.054,.226,.218,.259,.118);cheek.rotation.z=-s*.18;
    }
    oval(head,"orange",0,-.096,.34,.105,.054,.101);
    oval(head,"orange",0,-.131,.337,.084,.037,.079);
  }
  for(const s of [-1,1]){
    const x=s*(bear?.179:.146);
    oval(head,"ink",x,.047,bear?.328:.331,.038,.048,.023);
    oval(head,"snow",x-.01,.061,bear?.348:.351,.010,.012,.006);
    if(bear)stroke(head,"belt",[[x-.052,.139,.305],[x,.155,.315],[x+.045,.139,.306]],.013);
  }
  cap(head,bear);
  return {root,head,pose(t,swim=0,wave=0){
    head.rotation.y=(seated?-.34:0)+.085*Math.sin(t*1.4);head.rotation.z=.028*Math.cos(t*1.1);
    for(const [i,joint] of arms.entries()){
      const side=i?1:-1;
      joint.rotation.z=side*(.16+swim*(.6+.5*Math.sin(t*5))+(i?wave*(1.9+.23*Math.sin(t*7)):0));
      joint.rotation.x=seated?-1.12+.025*Math.sin(t*1.8+i):-.09-.09*Math.sin(t*1.8+i)-swim*.32;
    }
    body.rotation.x=.15*swim;body.rotation.z=.025*Math.sin(t*2);
  }};
}
