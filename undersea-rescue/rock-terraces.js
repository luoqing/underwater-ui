import * as T from "three";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";
import { habitats } from "./bluewater-layout.js";

// Stone strata on the flanks frame negative space along the sand channel.
export function rockTerraces(scene,ground,material){
  const result=[];
  for(const [i,h] of habitats.entries()){
    if(h.bed!=="wall" && h.bed!=="fan")continue;
    const pieces=[],side=i%2?1:-1;
    for(let j=0;j<4;j++){
      const scale=1+.24*Math.sin(i*2.8+j),x=h.x+side*(6+j*1.8),z=h.z+j*1.9;
      const g=new T.IcosahedronGeometry(1,2),a=g.attributes.position,colors=[];
      const base=ground(x,z);
      for(let k=0;k<a.count;k++){
        const px=a.getX(k),py=a.getY(k),pz=a.getZ(k);
        const ridge=1+.095*Math.sin(py*19+Math.sin(pz*2)+i);
        const wy=py*(j===1?4.2:2.6)*scale;
        a.setXYZ(k,x+px*3.6*ridge*scale,base+wy+.45*scale,z+pz*2.9*ridge*scale);
        const shade=.51+.084*Math.sin(wy*4+Math.sin(px*2)+i)+.065*py;
        colors.push(shade*.86,shade,shade*.93);
      }
      g.setAttribute("color",new T.Float32BufferAttribute(colors,3));g.computeVertexNormals();pieces.push(g);
    }
    const mesh=new T.Mesh(mergeGeometries(pieces),material);mesh.receiveShadow=true;
    mesh.geometry.computeBoundingSphere();scene.add(mesh);result.push(mesh);
  }
  // Eroded buttresses on alternating shoulders of the descent, with an
  // unbroken passage between them. Each formation is also a collision mesh.
  const spurs=[
    [-8.5,34,10,4.4],[12,45,19,5.5],[-10.5,58,26,5.1],
    [13.5,72,30,6.2],[-12,88,31,7.8],[15,106,36,7],[-11,129,39,8],
    [23,115,51,11],[-14,147,52,12],[17,157,48,15],[2,170,28,17],
    [52,-13,18,7],[-61,10,22,8],[36,-110,30,8],[-25,-129,38,10]
  ];
  for(const [i,[x,z,rise,width]] of spurs.entries()){
    const geometries=[];
    for(let j=0;j<3;j++){
      const cx=x+Math.sign(x)*j*width*.71,cz=z+(j-1)*width*.67;
      const base=ground(cx,cz);
      let foot=base-width*.62;
      for(let k=0;k<8;k++){
        const a=k*Math.PI/4;
        foot=Math.min(foot,ground(cx+width*Math.cos(a),cz+width*1.3*Math.sin(a))-1);
      }
      const top=Math.min(7.2-j*.8,base+rise*(1-.19*j));
      const p=[],uv=[],index=[],colour=[];
      const rows=17, sides=20;
      for(let k=0;k<=rows;k++)for(let n=0;n<=sides;n++){
        const u=k/rows, a=2*Math.PI*(n%sides)/sides;
        const ledge=.09*Math.sin(6*Math.PI*u+.65*Math.cos(a)+i*.7)
          +.04*Math.sin(15*Math.PI*u+.9*Math.sin(a*2));
        const cap=Math.sqrt(Math.max(0,1-Math.pow(u,6)));
        const r=width*(1-.14*j)*(1-(.22+.13*Math.sin(i))*u+ledge)
          *(1+.13*Math.cos(3*a+i)+.07*Math.sin(5*a-2*u+j))
          *cap;
        const yy=foot+(top-foot)*u+r*(.22*Math.cos(a+i)+.12*Math.sin(2*a-j))*u;
        const px=cx+r*Math.cos(a)+.20*width*Math.sin(3*u+i);
        const pz=cz+r*(1.42+.38*Math.sin(i))*Math.sin(a)+.44*width*u;
        p.push(px,yy,pz);uv.push(n/sides*2.5,yy*.21);
        const v=.54+.08*Math.sin(yy*1.9+.55*Math.sin(a))+.13*u;
        colour.push(v*.80,v,v*.94);
        if(k && n){const b=k*(sides+1)+n;index.push(b-sides-2,b-1,b,b-sides-2,b,b-sides-1)}
      }
      const g=new T.BufferGeometry();g.setAttribute("position",new T.Float32BufferAttribute(p,3));
      g.setAttribute("color",new T.Float32BufferAttribute(colour,3));g.setAttribute("uv",new T.Float32BufferAttribute(uv,2));
      g.setIndex(index);g.computeVertexNormals();
      // Project the actual sandstone texture in world metres. Cylindrical
      // UVs stretched the same few texels around the largest cliffs.
      const cliff=g.toNonIndexed(),pos=cliff.attributes.position,uvs=cliff.attributes.uv;
      const a=new T.Vector3(),b=new T.Vector3(),c=new T.Vector3(),n=new T.Vector3();
      for(let k=0;k<pos.count;k+=3){
        a.fromBufferAttribute(pos,k);b.fromBufferAttribute(pos,k+1);c.fromBufferAttribute(pos,k+2);
        T.Triangle.getNormal(a,b,c,n);
        const flat=Math.abs(n.y)>.65,side=Math.abs(n.x)>Math.abs(n.z);
        for(let j=k;j<k+3;j++){
          const u=flat || !side ? pos.getX(j) : pos.getZ(j);
          const v=flat ? pos.getZ(j) : pos.getY(j);
          uvs.setXY(j,u*.29,v*.29);
        }
      }
      geometries.push(cliff);
    }
    const mesh=new T.Mesh(mergeGeometries(geometries),material);
    mesh.geometry.computeBoundingSphere();mesh.receiveShadow=true;scene.add(mesh);result.push(mesh);
  }
  return result;
}
