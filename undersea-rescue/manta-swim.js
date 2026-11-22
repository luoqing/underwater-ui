import * as T from "three";

// manta.glb is pitched in its bind pose. These unit vectors follow its
// head-to-tail midline and the dark dorsal surface; wings extend along Z.
const wingShader = `
uniform vec3 mantaStroke;
const vec3 dorsal = vec3(-.8944272,-.4472136,0.);
const vec3 forwardRay = vec3(-.4472136,.8944272,0.);
vec3 bendManta(vec3 p) {
  float span = abs(p.z);
  float reach = max(0.,span-.075);
  float u = clamp(reach/.425,0.,1.);
  float chord = dot(p,forwardRay);
  // Roots lead the tips; the trailing edge takes the stroke a little later.
  float phase = mantaStroke.x - 1.02*u + 2.7*(chord-.16);
  float angle = mantaStroke.y * smoothstep(0.,.80,u) * sin(phase);
  float height = dot(p,dorsal);
  p += dorsal * (reach*sin(angle) + height*(cos(angle)-1.));
  p.z += sign(p.z) * (reach*(cos(angle)-1.) - height*sin(angle));
  float tail = smoothstep(.12,.43,-chord);
  p.z += tail*.014*sin(.53*mantaStroke.x + 4.*chord + mantaStroke.z);
  return p;
}
`;

export function mantaSwim(ray, offset = 0) {
  ray.children[0].rotation.set(Math.PI,0,Math.atan2(2,1));
  const stroke = {value:new T.Vector3(offset,.48,offset)};
  function deform(material, normals) {
    const water = material.onBeforeCompile.bind(material);
    const baseKey = material.customProgramCacheKey();
    material.onBeforeCompile = shader => {
      water(shader);
      shader.uniforms.mantaStroke = stroke;
      shader.vertexShader = wingShader + shader.vertexShader.replace(
        "#include <begin_vertex>", "vec3 transformed = bendManta(position);");
      if (normals) shader.vertexShader = shader.vertexShader.replace("#include <beginnormal_vertex>", `
        #include <beginnormal_vertex>
        vec3 n = normalize(objectNormal);
        vec3 tangentWing = normalize(cross(abs(n.z)<.9 ? vec3(0.,0.,1.) : vec3(0.,1.,0.),n));
        vec3 bitangentWing = cross(n,tangentWing);
        vec3 centerWing = bendManta(position);
        objectNormal = normalize(cross(bendManta(position+.001*tangentWing)-centerWing,
                                       bendManta(position+.001*bitangentWing)-centerWing));`);
    };
    material.customProgramCacheKey = () => baseKey+"-manta-swim";
  }
  ray.traverse(mesh => {
    if (!mesh.isMesh) return;
    deform(mesh.material,true);
    if(mesh.castShadow) {
      mesh.customDepthMaterial = new T.MeshDepthMaterial({depthPacking:T.RGBADepthPacking});
      deform(mesh.customDepthMaterial,false);
    }
  });
  return (time, freedom=1) => {
    const t=time+offset;
    const glide=T.MathUtils.smoothstep(Math.cos(t*.47),.40,.93);
    stroke.value.x=t*2.55+.21*Math.sin(t*.9);
    stroke.value.y=T.MathUtils.lerp(.10,.57-.43*glide,freedom);
  };
}
