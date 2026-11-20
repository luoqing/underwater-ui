"""Build the editable canyon set in metres, Y up. Python standard library only."""
import array
import json
import math
import random
import struct
from collections import defaultdict
from pathlib import Path

random.seed(41)
sin, cos, tau = math.sin, math.cos, math.tau
parts = defaultdict(lambda: ([], [], [], []))


def vertex(part, xyz, rgb, uv=(0, 0)):
    positions, colors, uvs, _ = parts[part]
    positions.append(xyz)
    colors.append(tuple(max(0.001, min(1, c)) for c in rgb))
    uvs.append(uv)
    return len(positions) - 1


def quad(part, a, b, c, d):
    parts[part][3].extend((a, b, d, b, c, d))


def floor(x, z):
    return .25*sin(x*.6+sin(z*.29))+.18*cos(z*.48)+.055*sin(z*4.0+x*1.15+1.5*sin(x*.43))


for j in range(146):
    z = -60+j*.55
    for i in range(115):
        x = -31+i*.55
        value = .86+.06*sin(z*.8+x*.3)
        a = vertex('Rippled sand', (x, floor(x, z), z), (value, value*.94, value*.78), (x/3.8, z/3.8))
        if i and j:
            quad('Rippled sand', a-116, a-1, a, a-115)


def rock(x, z, radius, height):
    part = 'Eroded sandstone'
    count = 32 if radius > 1.8 else 15
    phase = random.random()*tau
    base = floor(x,z)-.6
    rings = []
    layers = 18 if radius > 1.8 else 10
    for j in range(layers+1):
        t = j/layers
        ring = []
        scale = (1-.21*t)*(1-.93*max(0,(t-.69)/.31)**2)
        stratum = 1+.032*sin(j*3.6+phase)+.06*sin(j*.6+phase)
        for i in range(count+1):
            a = i*tau/count
            edge = 1+.16*sin(3*a+phase)+.10*cos(5*a+phase)+.045*sin(9*a+.32*j+phase)
            px = x+radius*scale*edge*stratum*cos(a)+t*.3
            pz = z+radius*.76*scale*edge*stratum*sin(a)
            py = base+height*(t+.058*sin(a*2+phase)*sin(t*math.pi))
            light = .72+.27*sin(j*3.2+phase)+random.uniform(-.09,.09)
            ring.append(vertex(part,(px,py,pz),(.88*light,.91*light,.84*light),(a*radius/3.3,-py/2.8)))
        if rings:
            for i in range(count):
                n=i+1
                quad(part,rings[-1][i],ring[i],ring[n],rings[-1][n])
        rings.append(ring)
    center=vertex(part,(x+.3,base+height*1.01,z),(.73,.72,.63))
    for i in range(count):
        parts[part][3].extend((ring[i],center,ring[i+1]))


for side in (-1,1):
    for j in range(29):
        z = 13-j*2.6
        x = side*(11.8+2.5*sin(z*.14)+random.uniform(-1,2))
        rock(x,z,random.uniform(3,6),random.uniform(3,9))
    for _ in range(90):
        z=random.uniform(-48,13)
        x=side*random.uniform(4.5,11.5)
        radius=random.uniform(.13,1.65)
        rock(x,z,radius,radius*random.uniform(.7,1.5))

# Thickets of long, twisting blades. Fixed roots and flexible tips share UV height.
for _ in range(170):
    side=random.choice((-1,1))
    x=side*random.uniform(5.3,10.7)
    z=random.uniform(-50,12)
    for __ in range(random.randint(2,5)):
        angle=random.random()*tau
        height=random.uniform(.65,3.1)
        reach=random.uniform(.35,1.05)
        tint=random.uniform(.62,1.25)
        for j in range(10):
            t=j/9
            midx=x+cos(angle)*reach*t+.15*sin(t*7+angle)*t
            midz=z+sin(angle)*reach*t
            width=(.025+.14*sin(math.pi*t)**.7)*(1+.2*sin(t*18))
            for side in (-1,1):
                a=vertex('Kelp ribbons',(midx+side*width*cos(angle+t*2),floor(x,z)+height*t,midz+side*width*sin(angle+t*2)),(.19*tint,.24*tint,.038*tint),((side+1)/2,t))
            if j: quad('Kelp ribbons',a-3,a-2,a,a-1)

doc={'asset':{'version':'2.0','generator':'Tidal Rescue / canyon modeller'},'scene':0,'scenes':[{'nodes':[]}],'nodes':[],'meshes':[],'materials':[],'buffers':[],'bufferViews':[],'accessors':[]}
blob=bytearray()


def accessor(values, fmt, kind, dimension):
    while len(blob)%4: blob.append(0)
    data=array.array(fmt,values).tobytes()
    view=len(doc['bufferViews'])
    doc['bufferViews'].append({'buffer':0,'byteOffset':len(blob),'byteLength':len(data)})
    blob.extend(data)
    desc={'bufferView':view,'componentType':5126 if fmt=='f' else 5125,'count':len(values)//dimension,'type':kind}
    if kind=='VEC3':
        desc.update(min=[min(values[i::3]) for i in range(3)],max=[max(values[i::3]) for i in range(3)])
    doc['accessors'].append(desc)
    return len(doc['accessors'])-1


for name,(pos,col,uv,ind) in parts.items():
    normal=[[0.,0.,0.] for _ in pos]
    for a,b,c in zip(ind[::3],ind[1::3],ind[2::3]):
        u=[pos[b][k]-pos[a][k] for k in range(3)]
        v=[pos[c][k]-pos[a][k] for k in range(3)]
        cross=[u[1]*v[2]-u[2]*v[1],u[2]*v[0]-u[0]*v[2],u[0]*v[1]-u[1]*v[0]]
        for i in (a,b,c):
            for k in range(3): normal[i][k]+=cross[k]
    for n in normal:
        length=math.sqrt(sum(v*v for v in n)) or 1
        for k in range(3): n[k]/=length
    attrs={}
    for key,data,dim in [('POSITION',pos,3),('NORMAL',normal,3),('COLOR_0',col,3),('TEXCOORD_0',uv,2)]:
        attrs[key]=accessor([v for row in data for v in row],'f','VEC'+str(dim),dim)
    idx=len(doc['nodes'])
    doc['materials'].append({'name':name,'doubleSided':name=='Kelp ribbons','pbrMetallicRoughness':{'metallicFactor':0,'roughnessFactor':.86}})
    doc['meshes'].append({'name':name,'primitives':[{'attributes':attrs,'indices':accessor(ind,'I','SCALAR',1),'material':idx}]})
    doc['nodes'].append({'name':name,'mesh':idx})
    doc['scenes'][0]['nodes'].append(idx)

# Embed the authored material maps so the standalone GLB keeps its finish.
asset_dir=Path(__file__).resolve().parents[1]/'assets'
doc['textures']=[]
doc['images']=[]
doc['samplers']=[{'wrapS':10497,'wrapT':10497,'minFilter':9987,'magFilter':9729}]
for mat,stem in zip(doc['materials'][:2],['ripple-sand','sandstone']):
    for suffix in ['', '-normal']:
        path=asset_dir/(stem+suffix+'.jpg')
        image=path.read_bytes()
        blob.extend(b'\0'*(-len(blob)%4))
        view=len(doc['bufferViews'])
        doc['bufferViews'].append({'buffer':0,'byteOffset':len(blob),'byteLength':len(image)})
        blob.extend(image)
        idx=len(doc['images'])
        doc['images'].append({'name':path.stem,'mimeType':'image/jpeg','bufferView':view})
        doc['textures'].append({'sampler':0,'source':idx})
        if suffix:
            mat['normalTexture']={'index':idx,'scale':.65}
        else:
            mat['pbrMetallicRoughness']['baseColorTexture']={'index':idx}

doc['buffers']=[{'byteLength':len(blob)}]
meta=json.dumps(doc,separators=(',',':')).encode()
meta+=b' '*(-len(meta)%4)
blob+=b'\0'*(-len(blob)%4)
out=Path(__file__).resolve().parents[1]/'assets/tidal-canyon.glb'
out.write_bytes(struct.pack('<4sII',b'glTF',2,28+len(meta)+len(blob))+struct.pack('<I4s',len(meta),b'JSON')+meta+struct.pack('<I4s',len(blob),b'BIN\0')+blob)
