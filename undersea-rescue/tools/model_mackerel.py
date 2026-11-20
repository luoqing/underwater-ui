"""Loft a fusiform fish from anatomical sections and attach separate ray fins."""
from pathlib import Path
import io, json, math, struct
import numpy as np
from PIL import Image

root=Path(__file__).resolve().parents[1]/'assets'
im=Image.open(root/'mackerel-skin.jpg').convert('RGB')
W,H=im.size
# Coordinates refer to the 1774 x 887 lateral plate. Width controls body girth.
stations=np.array([(132,421,450,2),(204,401,453,6),(320,398,450,13),(410,381,463,18),(540,347,503,39),(710,313,553,66),(900,276,598,89),(1090,257,621,108),(1250,261,622,111),(1410,286,600,97),(1520,314,572,81),(1610,346,541,59),(1690,383,508,36),(1750,417,480,15),(1770,449,468,2)])
pos=[];uv=[];index=[]
def add(x,y,z):
    i=len(pos);pos.append(((x-910)/4000,(449-y)/4000,z/4000));uv.append((x/1774,y/887));return i
def face(a,b,c,d):index.extend([a,b,c,a,c,d])
for j,x in enumerate(np.linspace(132,1770,64)):
    top,bot,thick=[np.interp(x,stations[:,0],stations[:,k]) for k in (1,2,3)]
    for i in range(32):
        a=i*math.tau/32
        add(x,(top+bot)/2+(bot-top)/2*math.cos(a),thick*math.sin(a))
    if j:
        for i in range(32):
            a=(j-1)*32+i;b=(j-1)*32+(i+1)%32
            face(a,b,b+32,a+32)
# Thin curved surfaces with distinct tips, unlike a flat side-profile card.
def fan(origin,outline,depth,side=1):
    center=add(*origin);prev=None
    for x,y in outline:
        cur=add(x,y,side*depth(x,y))
        if prev is not None:index.extend((center,prev,cur))
        prev=cur
fan((316,426,0),[(325,399),(235,354),(113,288),(5,258),(48,313),(136,395),(165,433),(112,496),(5,632),(38,626),(170,544),(268,477),(325,450)],lambda x,y:math.sin((x+y)/85)*3)
fan((720,320,0),[(371,386),(450,361),(573,320),(719,270),(825,224),(895,183),(858,159),(890,166),(975,211),(1020,253),(821,287),(583,345)],lambda x,y:3)
fan((1180,268,0),[(1057,255),(1093,223),(1106,151),(1135,162),(1229,218),(1281,260),(1177,270)],lambda x,y:2)
fan((752,563,0),[(379,459),(548,512),(775,600),(918,617),(850,665),(756,697),(756,673),(655,622),(516,550),(395,490)],lambda x,y:3)
fan((1255,622,0),[(1190,621),(1169,739),(1202,721),(1287,652),(1325,610)],lambda x,y:7)
for side in [-1,1]:
    fan((1369,456,side*96),[(1340,444),(1254,423),(1092,411),(936,424),(1018,453),(1119,482),(1228,486),(1338,469),(1340,444)],lambda x,y:98+(1360-x)*.09,side)
geom=np.array(pos)
tri=np.array(index).reshape((-1,3))
norm=np.zeros_like(geom)
# Loft has clockwise winding viewed from the outside.
tri[:(63*32*2)]=tri[:(63*32*2)][:,::-1]
for a,b,c in tri:
    n=np.cross(geom[b]-geom[a],geom[c]-geom[a]);norm[[a,b,c]]+=n
norm/=np.maximum(np.linalg.norm(norm,axis=1,keepdims=True),1e-10)
# Two tail deformation targets and an editable looping glTF animation.
tail=np.clip((.035-geom[:,0])/.25,0,1)**1.9
targets=[]
for sign in [-1,1]:
    d=np.zeros_like(geom);d[:,2]=sign*tail*.055;d[:,0]=tail*.008;targets.append(d)
# Black background around the fin outline becomes transparent.
rgb=np.asarray(im);alpha=np.uint8(np.clip((np.max(rgb,axis=2).astype(float)-14)/18,0,1)*255)
image=Image.fromarray(np.dstack((rgb,alpha)));image.thumbnail((1200,600))
from fish_glb import write_fish
write_fish(root/'horse-mackerel.glb','Japanese horse mackerel',geom,norm,uv,tri,targets,image)
