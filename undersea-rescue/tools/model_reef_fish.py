"""Reconstruct each species as a closed, textured anatomical volume in metres."""
from pathlib import Path
import math
import sys
import numpy as np
from PIL import Image
from fish_glb import write_fish, silhouette

root=Path(__file__).resolve().parents[1]/'assets'
plate=Image.open(root/'reef-fish-plate.jpg').convert('RGB')
specs=[
 ('spotted-grouper','Epinephelus akaara',0,.335,.54,.095),
 ('moon-wrasse','Thalassoma lunare',.335,.594,.31,.06),
 ('olive-flounder','Paralichthys olivaceus',.594,1,.57,.04)]
specs.extend([('anthias','Pseudanthias squamipinnis',0,.40,.18,.072)])
for tag,name,top,bottom,length,girth in specs:
    if len(sys.argv)>1 and tag not in sys.argv[1:]: continue
    if tag == 'anthias': plate=Image.open(root/'reef-pair-skin.jpg').convert('RGB')
    img=plate.crop((0,round(top*plate.height),plate.width,round(bottom*plate.height)))
    rgb=np.asarray(img);alpha = silhouette(rgb)
    yy,xx=np.where(alpha>140);x0,x1=xx.min(),xx.max();center_y=(yy.min()+yy.max())*.5
    scale=length/(x1-x0);flat=tag=='olive-flounder'
    pos=[];uv=[];tris=[];rings=100;sides=40
    for j in range(rings):
        t=j/(rings-1);x=int(x0+(x1-x0)*t);col=np.where(alpha[:,x]>110)[0]
        upper,lower=(col.min(),col.max()) if len(col) else (center_y,center_y)
        center=(upper+lower)*.5;half=(lower-upper)*.5
        # The caudal fan and trailing fin margins stay thin.
        taper=min(1,max(0,(t-.12)/.27));depth=girth*length*(.035+.965*taper)*max(.01,math.sin(math.pi*t))**.48
        for i in range(sides):
            angle=2*math.pi*i/sides;c=math.cos(angle);height=-c*half
            thickness=math.sin(angle)*depth*max(.04,1-abs(c)**5)
            y=center_y-center+height
            px=(t-.5)*length
            if flat:
                pos.append((px,thickness if thickness>0 else thickness*.45,-y*scale))
            else:pos.append((px,y*scale,thickness))
            uv.append(((x+.5)/img.width,(center-height+.5)/img.height))
        if j:
            for i in range(sides):
                a=(j-1)*sides+i;b=(j-1)*sides+(i+1)%sides
                tris.extend([(a,b+sides,b),(a,a+sides,b+sides)])
    # Close both end rings rather than leaving holes at the snout and caudal tips.
    for k in range(1,sides-1):
        last=(rings-1)*sides
        tris.extend([(0,k+1,k),(last,last+k,last+k+1)])
    paddle = None
    if tag == 'moon-wrasse':
        # Texture landmarks on the 550px-wide reference plate. Chest is at +X.
        paddle = [0.] * len(pos)
        for side in (-1,1):
            base = len(pos)
            for ray in range(10):
                f = ray/9
                for k in range(7):
                    u=k/6
                    px=(390-3*f)*(1-u)+(349+10*math.cos(f*math.pi))*u
                    py=(253+14*f)*(1-u)+(262+15*f)*u
                    ax=px*plate.width/550; ay=py*plate.height/550-round(top*plate.height)
                    depth=.016+.009*u+.004*math.sin(math.pi*u)
                    pos.append(((ax-x0)*scale-length/2,(center_y-ay)*scale,side*depth))
                    uv.append((ax/img.width,ay/img.height))
                    paddle.append(side*u)
                    if ray and k:
                        a=base+ray*7+k
                        tris.extend([(a-8,a-1,a),(a-8,a,a-7)])
    if flat:
        # Eye sockets protrude from the eyed side. Match their lids and pupils
        # to the two distinct landmarks on the plate; keep the blind side plain.
        unit = img.width / 550
        for ex, ey, radius in [(477,86,5.4),(478,112,6.8)]:
            start=len(pos)
            for j in range(6):
                ring=j/5
                for i in range(24):
                    angle=i*math.tau/24
                    x=(ex+radius*ring*math.cos(angle))*unit
                    y=(ey+radius*.80*ring*math.sin(angle))*unit
                    col=np.where(alpha[:,int(x)]>110)[0]
                    v=np.clip((2*y-col.max()-col.min())/(col.max()-col.min()),-1,1)
                    t=(x-x0)/(x1-x0)
                    surface=girth*length*math.sin(math.pi*t)**.48*math.sqrt(1-v*v)*max(.04,1-abs(v)**5)
                    pos.append(((t-.5)*length,surface+.0038*(1-ring**2)+.0001,(y-center_y)*scale))
                    uv.append((x/img.width,y/img.height))
                    if j:
                        a=start+(j-1)*24+i;b=start+(j-1)*24+(i+1)%24
                        tris.extend([(a,b,b+24),(a,b+24,a+24)])
    pos=np.array(pos);tri=np.array(tris);norm=np.zeros_like(pos)
    for a,b,c in tri:
        n=np.cross(pos[b]-pos[a],pos[c]-pos[a]);norm[[a,b,c]]+=n
    norm/=np.maximum(1e-9,np.linalg.norm(norm,axis=1,keepdims=True))
    targets=[]
    for direction in [-1,1]:
        d=np.zeros_like(pos);t=np.clip(.5-pos[:,0]/length,0,1);d[:,2]=direction*length*.13*t**2.7
        if flat:d[:,1]=direction*.007*np.sin(pos[:,0]*28)*(np.abs(pos[:,2])/length)**.7
        targets.append(d)
    rgba=Image.fromarray(np.dstack((rgb,alpha)))
    if flat:
        # Separate pale blind-side UV island; eyes stay on the ocular side only.
        white=np.clip(np.mean(rgb,axis=2)*.22+152,0,255).astype('uint8')
        blind=Image.fromarray(np.dstack((white,white,np.uint8(white*.94),alpha)))
        atlas=Image.new('RGBA',(img.width,img.height*2));atlas.paste(rgba,(0,0));atlas.paste(blind,(0,img.height));rgba=atlas
        # Duplicate rim vertices so a triangle never interpolates between UV islands.
        underside=pos[tri,1].mean(axis=1)<0
        tri[underside]+=len(pos)
        uv=[(u,v*.5) for u,v in uv]+[(u,(v+1)*.5) for u,v in uv]
        pos=np.tile(pos,(2,1));norm=np.tile(norm,(2,1))
        targets=[np.tile(d,(2,1)) for d in targets]
    write_fish(root/(tag+'.glb'),name,pos,norm,uv,tri,targets,rgba,paddle)
