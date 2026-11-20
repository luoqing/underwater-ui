import * as T from "three";

// Preserve the imported color and normal maps. Restore warm tissue reflectance over
// the upper living branches; limestone, cavities and the lower foot keep their albedo.
export function legacyPigment(mat, box, tint, variation, light) {
  light(mat);
  const prior = mat.onBeforeCompile,
    shade = new T.Color(tint),
    height = box.max.y - box.min.y;
  mat.onBeforeCompile = (shader) => {
    prior(shader);
    Object.assign(shader.uniforms, {
      colonyInk: { value: shade },
      reefMin: { value: box.min.y },
      reefSize: { value: height },
      reefVariant: { value: variation },
    });
    shader.vertexShader =
      "varying vec3 specimenP;\n" +
      shader.vertexShader.replace(
        "#include <begin_vertex>",
        "#include <begin_vertex>\nspecimenP=position;",
      );
    shader.fragmentShader =
      "varying vec3 specimenP;uniform vec3 colonyInk;uniform float reefMin,reefSize,reefVariant;\n" +
      shader.fragmentShader;
    shader.fragmentShader = shader.fragmentShader.replace(
      "#include <color_fragment>",
      `#include <color_fragment>
   float localH=(specimenP.y-reefMin)/reefSize;
   vec3 q=specimenP/max(reefSize,.001);
   float bloom=.48+.24*sin(q.x*11.+q.z*5.+reefVariant)+.22*cos(q.z*12.-q.y*5.);
   float living=smoothstep(.13,.55,localH+.13*bloom);
   float lumin=dot(diffuseColor.rgb,vec3(.26,.59,.15));
   vec3 amber=vec3(1.,.35,.075),lilac=vec3(.57,.20,1.);
   vec3 pigment=mix(colonyInk,mix(amber,lilac,smoothstep(.42,.73,bloom)),.44*smoothstep(.32,.84,bloom));
   float highlight=smoothstep(.64,1.0,localH);
   pigment=mix(pigment,vec3(1.,.78,.62),.25*highlight);
   vec3 detail=pigment*(.13+1.17*pow(max(0.,lumin),.69));
   diffuseColor.rgb=mix(diffuseColor.rgb,detail,.79*living);
  `,
    );
    shader.fragmentShader = shader.fragmentShader.replace(
      "#include <opaque_fragment>",
      "outgoingLight+=diffuseColor.rgb*.16;\n#include <opaque_fragment>",
    );
  };
  mat.customProgramCacheKey = () => "living-reef-pigment";
}
