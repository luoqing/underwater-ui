"""Loft an opaque trunk and fit separate fin membranes to the lateral reference."""
from pathlib import Path
import math
import numpy as np
from PIL import Image, ImageDraw
from scipy.ndimage import binary_fill_holes
from fish_glb import silhouette, write_fish

ASSETS = Path(__file__).resolve().parents[1] / "assets"


def model(tag, latin, plate, length, extent, stations, eye, pectoral, spread):
    image = Image.open(ASSETS / plate).convert('RGB')
    scale = image.width / 550
    rgb = np.array(image)
    if tag == 'blue-tang':
        # The legacy turnaround has a neutral grey studio background.
        distance = np.max(np.abs(rgb.astype(float) - rgb[0, 0]), axis=2)
        alpha = np.uint8(binary_fill_holes(distance > 28) * 255)
    else:
        alpha = silhouette(rgb)
    sections = np.array(stations)
    hull = Image.new('L', image.size)
    shape = list(sections[:, [0, 1]]) + list(sections[::-1, [0, 2]])
    ImageDraw.Draw(hull).polygon([tuple(p * scale) for p in shape], fill=255)
    alpha = np.maximum(alpha, np.asarray(hull))
    atlas = Image.fromarray(np.dstack((rgb, alpha)))
    unit = length / (extent[1] - extent[0])
    positions, uvs, faces = [], [], []
    center = (extent[1] + extent[0]) / 2
    mid_y = (sections[:, 1].min() + sections[:, 2].max()) / 2

    def vertex(x, y, z):
        positions.append(((x-center)*unit, (mid_y-y)*unit, z*unit))
        uvs.append((x/550, y*scale/image.height))
        return len(positions)-1

    def section(x):
        return tuple(np.interp(x, sections[:, 0], sections[:, i]) for i in (1, 2, 3))

    sides = 28
    columns = np.unique(np.r_[np.linspace(sections[0,0], sections[-1,0], 39), sections[:,0], eye[0]])
    for j, x in enumerate(columns):
        upper, lower, radius = section(x)
        for i in range(sides):
            a = math.tau*i/sides
            y = (upper+lower+(lower-upper)*math.cos(a))/2
            bulge = eye[2]*.45*math.exp(-((x-eye[0])**2+(y-eye[1])**2)/eye[2]**2)
            vertex(x, y, (radius+bulge)*math.sin(a))
        if j:
            for i in range(sides):
                a = (j-1)*sides+i
                b = (j-1)*sides+(i+1)%sides
                faces.extend([(a,b+sides,b),(a,a+sides,b+sides)])
    for k in range(1,sides-1):
        end = (len(columns)-1)*sides
        faces.extend([(0,k,k+1),(end,end+k+1,end+k)])

    # Only fin and ray pixels reach the transparent membrane. Paired pectorals
    # fan outward from the gill; the caudal and median fins stay on the midline.
    fin = Image.new('L', image.size)
    ImageDraw.Draw(fin).polygon([(x*scale,y*scale) for x,y in pectoral],fill=255)
    wing = np.asarray(fin)
    outside = np.asarray(hull)==0
    for side in (0,-1,1):
        region = (outside & (wing==0)) if side==0 else (wing!=0)
        visible = region & (alpha>96)
        if tag=='blue-tang': visible[:,:int(extent[0]*scale)-2]=False
        ys, xs = np.where(visible)
        step = 5 if tag=='lionfish' else 3
        vertices = {}
        def point(x,y):
            if (x,y) not in vertices:
                t = min(1,math.hypot(x-pectoral[0][0],y-pectoral[0][1])/spread)
                z = 0 if side==0 else side*(section(pectoral[0][0])[2]*.88+spread*.54*t)
                vertices[x,y] = vertex(x,y,z)
            return vertices[x,y]
        for y in range(int(ys.min()/scale), math.ceil(ys.max()/scale)+1, step):
            for x in range(int(xs.min()/scale), math.ceil(xs.max()/scale)+1, step):
                pixels=visible[int(y*scale):int((y+step)*scale)+1,int(x*scale):int((x+step)*scale)+1]
                if not pixels.any(): continue
                a,b,c,d = point(x,y),point(x+step,y),point(x+step,y+step),point(x,y+step)
                faces.extend([(a,c,b),(a,d,c)] if side>=0 else [(a,b,c),(a,c,d)])
    pos, tri = np.array(positions), np.array(faces)
    normals = np.zeros_like(pos)
    faces_normal = np.cross(pos[tri[:,1]]-pos[tri[:,0]],pos[tri[:,2]]-pos[tri[:,0]])
    for i in range(3): np.add.at(normals,tri[:,i],faces_normal)
    normals /= np.maximum(1e-10,np.linalg.norm(normals,axis=1,keepdims=True))
    delta = np.zeros_like(pos)
    delta[:,2] = length*.11*np.clip(.5-pos[:,0]/length,0,1)**2.4
    write_fish(ASSETS/(tag+'.glb'),latin,pos,normals,uvs,tri,[-delta,delta],atlas)


model('lionfish','Pterois volitans','lionfish-skin.jpg',.39,(5,521),
      [(99,174,220,3),(116,173,218,6),(146,167,218,9),(180,156,226,18),
       (224,139,239,31),(275,131,249,43),(326,125,253,51),(370,128,259,50),
       (409,141,259,45),(447,161,253,34),(469,179,245,25),(495,193,237,15),
       (514,211,227,5),(519,218,222,1)],(460,180,8),
      [(378,208),(365,239),(404,306),(398,350),(235,360),(184,329),(214,279),
       (114,304),(99,255),(135,232),(213,209),(148,183),(182,149),(320,190)],175)
model('blue-tang','Paracanthurus hepatus','blue-tang-reference.jpg',.245,(302,532),
      [(345,102,114,2),(357,95,121,5),(376,83,138,9),(398,74,151,13),
       (423,69,163,18),(449,70,169,20),(473,75,171,18),(490,84,168,16),
       (505,96,162,12),(517,111,157,9),(525,132,150,5),(531,144,148,1)],
      (505,115,8),[(470,104),(472,121),(464,150),(453,148),(449,111),(452,98)],46)
