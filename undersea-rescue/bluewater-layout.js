// Metres, Y up. Reefs grow in patches around open passages between them.
export const habitats=[
  {x:1,z:23,fish:0,animal:"turtle",bed:"grass"},
  {x:8,z:37,fish:1,animal:"ray",bed:"wall"},
  {x:6,z:52,fish:2,animal:"whale",bed:"open",level:.69},
  {x:-2,z:33,fish:2,bed:"grass",level:.32},
  {x:.8,z:46,fish:3,bed:"wall"},{x:2.8,z:59,fish:1,bed:"fan"},
  {x:-2,z:69,fish:4,animal:"ray",bed:"fan",level:.48},{x:6,z:81,fish:2,animal:"whale",bed:"open",level:.36},
  {x:2,z:101,fish:2,animal:"ray",bed:"fan"},{x:9,z:127,fish:2,animal:"whale",bed:"open",level:.53},
  {x:-19,z:57,fish:1,bed:"fan"},{x:-45,z:18,fish:0,animal:"turtle",bed:"grass"},
  {x:-58,z:-27,fish:2,bed:"open"},{x:50,z:-8,fish:3,bed:"wall"},
  {x:62,z:-44,fish:0,animal:"ray",bed:"fan"},{x:14,z:-76,fish:2,bed:"wall"},
  {x:-17,z:-92,fish:1,animal:"ray",bed:"fan"},{x:21,z:-106,fish:2,animal:"whale",bed:"open"},
];
// Crossing schools occupy different layers above the deep floor.
habitats.push(
  {x:5,z:64,fish:2,bed:"open",level:.64},
  {x:10,z:91,fish:1,bed:"open",level:.47},
  {x:3,z:104,fish:2,bed:"open",level:.39,animal:"whale"},
  {x:13,z:115,fish:2,bed:"open",level:.34},
  {x:6,z:133,fish:2,bed:"open",level:.27,animal:"ray"},
  {x:16,z:145,fish:2,bed:"open",level:.20,animal:"whale"}
);
let seed=419;
const random=()=>((seed=(Math.imul(seed,1664525)+1013904223)>>>0)/4294967296);
for(let z=-170;z<=140;z+=29)for(let x=-125;x<=125;x+=31){
  const px=x+(random()-.5)*14,pz=z+(random()-.5)*13;
  if(Math.abs(px)<29 && pz>-71 && pz<23 || habitats.some(h=>Math.hypot(h.x-px,h.z-pz)<18))continue;
  const index=habitats.length,open=index%4===0,shallow=Math.max(Math.abs(px)-31,pz-20,-60-pz)<30;
  const bed=open?"open":shallow?"grass":index%3?"fan":"wall";
  habitats.push({x:px,z:pz,fish:open?2:index%5,level:open?.5+random()*.35:undefined,bed,
    animal:open?"whale":shallow?"turtle":index%3===0?"ray":null});
}

// Playable bounds in world metres, with north toward negative Z.
export const seaBounds={west:-143,east:143,north:-182,south:155,bottom:-141,surface:10};
