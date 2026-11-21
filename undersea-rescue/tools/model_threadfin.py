"""Build the threadfin's body, fins and filament against its original lateral atlas."""
from pathlib import Path
import math
import numpy as np
from PIL import Image
from fish_glb import write_fish, silhouette

assets = Path(__file__).resolve().parents[1] / "assets"
image = Image.open(assets / "reef-pair-skin.jpg").convert("RGB")
# Landmarks use the full square plate at 550 px. Reuse the original cropped atlas.
crop_top = round(.40 * image.height)
crop = image.crop((0, crop_top, image.width, image.height))
rgb = np.asarray(crop)
alpha = silhouette(rgb)
atlas = Image.fromarray(np.dstack((rgb, alpha)))
metres = .24 / (507-82)  # standard silhouette from caudal edge to snout
positions, uvs, triangles, paddle = [], [], [], []
# x, dorsal body margin, ventral margin and half-width (fins excluded).
stations = np.array([
    (144,375,405,3),(163,370,413,8),(190,352,428,14),(215,312,458,22),
    (245,276,477,32),(283,259,484,38),(322,261,480,42),(365,272,470,41),
    (394,289,455,36),(421,317,449,28),(439,344,440,22),(454,374,435,16),
    (470,393,429,10),(491,401,419,6),(506,407,414,2),(507,410,412,.6)])
def vertex(x,y,z,fin=0):
    positions.append(((x-295)*metres,(386-y)*metres,z*metres))
    uvs.append((x/550,(y*image.height/550-crop_top)/crop.height))
    paddle.append(fin)
    return len(positions)-1

def section(x):
    return [np.interp(x,stations[:,0],stations[:,i]) for i in (1,2,3)]

def flank(x,y):
    top,bottom,r=section(x)
    v=(2*y-top-bottom)/max(1,bottom-top)
    return r*math.sqrt(max(0,1-v*v))*(1+.11*v)

sides=48
for row,x in enumerate(np.linspace(144,507,100)):
    top,bottom,r=section(x)
    for i in range(sides):
        angle=i*math.tau/sides
        y=(top+bottom+(bottom-top)*math.cos(angle))/2
        z=math.sin(angle)*r*(1+.11*math.cos(angle))
        # An eye socket and corneal dome give the head a readable three-quarter view.
        z+=math.copysign(3.3*math.exp(-((x-451)/8)**2-((y-392)/7)**2),z)
        vertex(x,y,z)
    if row:
        for i in range(sides):
            a=(row-1)*sides+i; b=(row-1)*sides+(i+1)%sides
            triangles.extend([(a,b+sides,b),(a,a+sides,b+sides)])
for start in (0,99*sides):
    for i in range(1,sides-1):
        triangles.append((start,start+i,start+i+1) if start else (0,i+1,i))

def membrane(rays,side=0):
    """Span between measured base-tip pairs, with curved, articulated pectorals."""
    start=len(positions)
    for (bx,by),(ex,ey) in rays:
        for k in range(7):
            u=k/6
            x,y=bx+(ex-bx)*u,by+(ey-by)*u
            z=(flank(bx,by)+.8+16*u+5*math.sin(u*math.pi)) if side else 1.7*math.sin(math.pi*u)
            vertex(x,y,z*(side or 1),side*u)
    for i in range(len(rays)-1):
        for k in range(6):
            a=start+i*7+k
            triangles.extend([(a,a+7,a+1),(a+1,a+7,a+8)])

membrane([((151,378),(94,327)),((148,384),(85,347)),((145,391),(82,387)),
          ((148,398),(85,431)),((153,404),(95,456))])
membrane([((154,376),(141,347)),((175,367),(137,318)),((194,346),(150,284)),
          ((215,316),(170,261)),((238,288),(205,249)),((275,269),(255,248)),
          ((309,268),(304,250)),((351,276),(350,259)),((391,297),(388,284)),((419,321),(412,312))])
for x in range(223,384,17):
    y=np.interp(x,[223,280,350,384],[250,247,260,279])
    membrane([((x,y+6),(x-8,y-9)),((x+8,y+9),(x+5,y+2))])
membrane([((150,404),(138,428)),((170,412),(133,462)),((190,426),(150,488)),
          ((218,457),(183,506)),((243,472),(217,511)),((270,480),(260,503)),((300,480),(298,485))])
membrane([((285,481),(294,535)),((313,476),(311,528)),((340,469),(364,475))])
for side in (-1,1):
    membrane([((389,415),(347,395)),((393,418),(331,402)),((395,421),(318,418)),
              ((393,425),(311,448)),((388,429),(328,455)),((384,432),(347,447))],side)
# The long dorsal filament has a round cross-section and a continuous root.
points=[(251,251,3.4),(222,247,3.6),(188,247,3),(153,249,2.2),(110,252,1.6),(73,257,1),(36,267,.6),(15,278,.18)]
start=len(positions)
for i,(x,y,r) in enumerate(points):
    for k in range(8):
        a=k*math.tau/8
        vertex(x,y+r*math.cos(a),r*math.sin(a))
    if i:
        for k in range(8):
            a=start+(i-1)*8+k; b=start+(i-1)*8+(k+1)%8
            triangles.extend([(a,b,b+8),(a,b+8,a+8)])
pos=np.asarray(positions); tri=np.asarray(triangles)
normal=np.zeros_like(pos)
face=np.cross(pos[tri[:,1]]-pos[tri[:,0]],pos[tri[:,2]]-pos[tri[:,0]])
for i in range(3): np.add.at(normal,tri[:,i],face)
normal/=np.maximum(1e-10,np.linalg.norm(normal,axis=1,keepdims=True))
tail=np.clip(.47-pos[:,0]/.24,0,1)**2
delta=np.zeros_like(pos);delta[:,2]=.018*tail
write_fish(assets/'threadfin.glb','Chaetodon auriga',pos,normal,uvs,tri,[-delta,delta],atlas,paddle)
