import { seaBounds as sea } from "./bluewater-layout.js";

const width=sea.east-sea.west,height=sea.south-sea.north;
const inks=[[0,137,185,169],[15,76,156,157],[35,42,125,141],[65,25,91,118],[95,20,69,97],[150,13,42,65]];

function survey(ground, done) {
  const base=document.createElement("canvas");
  base.width=190;base.height=Math.round(190*height/width);
  const w=base.width,h=base.height,depth=new Float32Array(w*h);
  const ctx=base.getContext("2d"),pixels=ctx.createImageData(w,h);
  let row=0;
  function scan(){
    const until=performance.now()+3;
    do {
      for(let x=0;x<w;x++)
        depth[row*w+x]=sea.surface-ground(sea.west+(x+.5)*width/w,sea.north+(row+.5)*height/h);
      row++;
    }while(row<h && performance.now()<until);
    if(row<h){requestAnimationFrame(scan);return}
    paint();
  }
  function paint(){
    for(let y=0;y<h;y++)for(let x=0;x<w;x++){
      const i=y*w+x,d=depth[i],west=x?depth[i-1]:d,north=y?depth[i-w]:d;
      let j=1;while(j<inks.length-1 && d>inks[j][0])j++;
      const low=inks[j-1],high=inks[j],mix=Math.max(0,Math.min(1,(d-low[0])/(high[0]-low[0])));
      const slope=Math.max(-.21,Math.min(.20,(west+north-2*d)*.042));
      const contour=Math.floor(d/10)!==Math.floor(west/10) || Math.floor(d/10)!==Math.floor(north/10);
      for(let c=1;c<=3;c++)pixels.data[i*4+c-1]=(low[c]+(high[c]-low[c])*mix)*(1+slope)+(contour?13:0);
      pixels.data[i*4+3]=255;
    }
    ctx.putImageData(pixels,0,0);done(base);
  }
  requestAnimationFrame(scan);
}

export function seaChart(canvas) {
  const w=420,h=Math.round(w*height/width);
  canvas.width=w;canvas.height=h;
  const ctx=canvas.getContext("2d");
  const px=x=>(x-sea.west)*w/width,py=z=>(z-sea.north)*h/height;
  let surveying=false,base=null;
  function prepare(ground){
    if(surveying)return;
    surveying=true;
    survey(ground,relief=>{
      base=document.createElement("canvas");base.width=w;base.height=h;
      const ctx=base.getContext("2d");ctx.drawImage(relief,0,0,w,h);
      ctx.strokeStyle="#c3e8dd28";ctx.lineWidth=.8;
      for(let x=-100;x<=100;x+=50){ctx.beginPath();ctx.moveTo(px(x),0);ctx.lineTo(px(x),h);ctx.stroke()}
      for(let z=-150;z<=150;z+=50){ctx.beginPath();ctx.moveTo(0,py(z));ctx.lineTo(w,py(z));ctx.stroke()}
      ctx.textAlign="center";ctx.font="600 24px system-ui";ctx.fillStyle="#eaf4e7";
      ctx.shadowColor="#062d3c";ctx.shadowBlur=5;
      for(const [name,x,z] of [["峡口",0,-43],["浅礁",-32,5],["外礁",52,47],["海沟",-42,114]])
        ctx.fillText(name,px(x),py(z));
      ctx.shadowBlur=0;
      ctx.textAlign="left";ctx.font="bold 20px system-ui";ctx.fillStyle="#eff9e9";ctx.fillText("↑ N",13,29);
      ctx.strokeStyle="#ecf3e4";ctx.lineWidth=2;ctx.beginPath();ctx.moveTo(16,h-21);ctx.lineTo(16+50*w/width,h-21);ctx.stroke();
      ctx.font="17px system-ui";ctx.fillText("50 m",16,h-29);
    });
  }
  function draw({position,forward,zone,target}){
    if(base)ctx.drawImage(base,0,0);else{ctx.fillStyle="#195367";ctx.fillRect(0,0,w,h)}
    if(target){ctx.beginPath();ctx.arc(px(target.x),py(target.z),7,0,2*Math.PI);ctx.strokeStyle="#ffc85a";ctx.lineWidth=3;ctx.stroke()}
    ctx.save();ctx.translate(px(position.x),py(position.z));ctx.scale(1.25,1.25);ctx.rotate(Math.atan2(forward.x,-forward.z));
    ctx.beginPath();ctx.moveTo(0,-14);ctx.lineTo(9,10);ctx.lineTo(0,6);ctx.lineTo(-9,10);ctx.closePath();
    ctx.fillStyle="#fff7be";ctx.fill();ctx.strokeStyle="#15383d";ctx.lineWidth=2.7;ctx.stroke();ctx.restore();
    const direction=["北","东北","东","东南","南","西南","西","西北"][(Math.round(Math.atan2(forward.x,-forward.z)/(Math.PI/4))+8)%8];
    canvas.setAttribute("aria-label",zone+"，朝"+direction+"，水深"+(sea.surface-position.y).toFixed(1)+"米");
  }
  return {prepare,draw};
}
