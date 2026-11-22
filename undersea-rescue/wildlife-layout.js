// Distances in metres. Each shoal has its own home, even when its meshes
// are reused elsewhere. The patrol radii and formation widths vary by species.
export const shoals = [
  {key:"anthias",length:.23,count:27,speed:.40,beat:2.7,rx:1.9,rz:1.3,width:1.05,train:1.4,rise:.70,burst:2.4},
  {key:"tang",length:.33,count:21,speed:.52,beat:2.1,rx:3.2,rz:2.2,width:1.15,train:2.0,rise:.55,burst:2.1},
  {key:"mackerel",length:.47,count:34,speed:1.18,beat:2.8,rx:7.5,rz:4.7,width:1.35,train:3.7,rise:.65,burst:2.0},
  {key:"butterfly",length:.28,count:2,speed:.23,beat:1.8,rx:1.5,rz:1.0,width:.24,train:.33,rise:.12,burst:2.5},
  {key:"wrasse",length:.35,count:8,speed:.62,beat:2.35,rx:3.0,rz:1.6,width:.70,train:1.5,rise:.35,burst:2.3},
  {key:"clown",length:.25,count:3,speed:.18,beat:2.55,rx:.68,rz:.51,width:.32,train:.45,rise:.18,burst:2.2},
  {key:"grouper",length:.62,count:1,speed:.14,beat:.95,rx:1.15,rz:.8,width:0,train:0,rise:0,burst:4.0},
];

// Near-field encounters alternate on the two sides of the swimming lane.
// Midwater fish use a fraction of the water column, reef residents a bottom clearance.
export function wildlifeSites(habitats, bottomAt) {
  const sites=habitats.map(h=>({...h}));
  function add(x,z,fish,level,animal,clearance) {
    sites.push({x,z,fish,level,animal,clearance});
  }
  add(-1.8,18,0,.56);add(2.8,22,3,.52);add(-3.6,25,5,undefined,undefined,1.05);
  add(2.3,30,1,.74);add(-2.8,36,4,.58,"turtle");
  add(3.8,42,0,.56);add(-4.2,48,2,.59,"jelly");add(4.3,55,1,.60,"ray");
  add(-2.1,64,4,.48);add(4.6,73,2,.48);add(-4.7,80,0,.26);
  add(8.2,91,2,.67,"jelly");add(1.8,99,4,.30);add(7.2,112,2,.37,"ray");
  add(2.5,123,2,.24,"jelly");add(13,141,2,.22);
  add(-1.5,4,0,.48);add(2,-3,1,.54);add(-2,-20,4,.52);add(2.3,-33,2,.62);
  add(-4.5,-47,0,.45);add(6,-59,4,.45,"turtle");
  habitats.forEach((h,i)=>{
    const a=i*2.39996,x=h.x+Math.cos(a)*5.4,z=h.z+Math.sin(a)*6.8;
    const bottom=bottomAt(x,z);
    if(bottom> -27 && h.bed!=="open"){
      const fish=[3,0,5,4,6,1][i%6];
      add(x,z,fish,undefined,i%8===0?"lion":undefined,fish===5?.95:fish===6?.65:2);
    }else{
      // Alternate bottom hunters and passing schools so a horizontal grid
      // does not leave the middle of the deeper water column empty.
      if(i%3===0)add(x,z,6,undefined,undefined,.8);
      add(x+3,z-4,2,.26+.51*((i*.618)%1),i%5===0?"jelly":undefined);
    }
  });
  add(-5.8,25,6,undefined,"lion",.8);
  add(5.4,37,5,undefined,undefined,1);
  return sites;
}
