import { habitats } from "./bluewater-layout.js";
import * as T from "three";
export function plantKelp(scene, reef, source, light) {
  const model = source.scene.getObjectByProperty("isMesh", true);
  const geo = model.geometry.clone();
  geo.computeBoundingBox();
  const bounds = geo.boundingBox;
  const height = bounds.max.y - bounds.min.y;
  geo.translate(
    -(bounds.min.x + bounds.max.x) / 2,
    -bounds.min.y,
    -(bounds.min.z + bounds.max.z) / 2,
  );
  geo.scale(1 / height, 1 / height, 1 / height);
  const mat = model.material.clone();
  light(mat);
  const illuminated = mat.onBeforeCompile;
  mat.onBeforeCompile = (shader) => {
    illuminated(shader);
    shader.vertexShader = shader.vertexShader.replace(
      "#include <begin_vertex>",
      `#include <begin_vertex>
       float bend=pow(clamp(position.y,0.,1.),1.7);
       float surge=.11*sin(tideTime*1.05+instanceMatrix[3].z*.8-position.y*1.5)+.065*flow;
       transformed.x+=bend*surge; transformed.z+=bend*.041*sin(tideTime*.8+position.x*4.);`,
    );
  };
  mat.customProgramCacheKey = () => "legacy-kelp-current";
  const plants = new T.InstancedMesh(geo, mat, 22),
    tmp = new T.Object3D();
  for (let i = 0; i < plants.count; i++) {
    const side = i % 2 ? 1 : -1,
      j = Math.floor(i / 2);
    const x = side * (2.45 + 0.65 * Math.sin(j * 2.37 + side)),
      z = 4.5 - 4.6 * j + side * 0.7;
    tmp.position.set(x, reef.ground(x, z) - 0.08, z);
    tmp.rotation.y = i * 2.39;
    tmp.scale.setScalar(0.8 + 0.62 * (0.5 + 0.5 * Math.sin(i * 4.17)));
    tmp.updateMatrix();
    plants.setMatrixAt(i, tmp.matrix);
  }
  plants.receiveShadow = true;
  scene.add(plants);
  const meadows=[];
  for(const [index,h] of habitats.entries()){
    if(h.bed!=="grass")continue;
    const patch=new T.InstancedMesh(geo,mat,14);
    for(let i=0;i<patch.count;i++){
      const a=i*2.4+index,r=1+Math.sqrt(i)*.56;
      const x=h.x+3.1+Math.cos(a)*r,z=h.z+3.4+Math.sin(a)*r*1.65;
      tmp.position.set(x,reef.ground(x,z)-.11,z);
      tmp.rotation.y=a;tmp.scale.setScalar(.63+.95*(.5+.5*Math.sin(i*1.88+index)));
      tmp.updateMatrix();patch.setMatrixAt(i,tmp.matrix);
    }
    patch.computeBoundingSphere();scene.add(patch);meadows.push(patch);
  }
  return eye=>{for(const patch of meadows)patch.visible=patch.boundingSphere.center.distanceToSquared(eye)<48**2};
}
