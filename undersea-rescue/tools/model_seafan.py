"""Build a perforated, rounded 3D sea fan from its branching tissue silhouette."""
from pathlib import Path
import numpy as np
from PIL import Image
from scipy.ndimage import distance_transform_edt, gaussian_filter
from skimage.measure import marching_cubes
import trimesh

root=Path(__file__).resolve().parents[1]/'assets'
source=Image.open(root/'sea-fan-skin.jpg').convert('RGB')
# Geometry already contains the holes. Extend tissue pigment beneath the UV edges
# so texture mipmaps do not average the branches with a black studio background.
rgb=np.asarray(source);pigment=rgb.max(axis=2)>89
nearest=distance_transform_edt(~pigment,return_distances=False,return_indices=True)
skin=Image.fromarray(np.uint8(np.clip(rgb[tuple(nearest)].astype(float)*1.12+8,0,255)))
for name,width in [('sea-fan',375),('sea-fan-distant',155)]:
    im=source.resize((width,round(width*source.height/source.width)),Image.Resampling.BILINEAR)
    pixels=np.asarray(im)
    mask=pixels.max(axis=2)>46
    # Distance to each open mesh window determines the branch's cross-section.
    distance=distance_transform_edt(mask)-distance_transform_edt(~mask)
    z=np.arange(-10,11,dtype=np.float32)
    field=gaussian_filter(distance.astype('f'),.40)[:,:,None]-np.abs(z[None,None,:])*1.25
    vertices,faces,_,_=marching_cubes(field,0,allow_degenerate=False)
    iy,ix,iz=vertices.T;u=ix/(width-1);v=iy/(im.height-1)
    x=(u-.44)*1.83;y=(.979-v)*1.38
    depth=(iz-10)*1.83/width+(.09*np.sin(u*8+v*2)+.04*np.cos(v*8)+.16*(u-.5)**2)*(1.04-v)
    xyz=np.stack([x,y,depth],axis=1)
    uv=np.stack([u,1-v],axis=1)
    geom=trimesh.Trimesh(vertices=xyz,faces=faces,process=False)
    if geom.volume<0:geom.invert()
    geom.visual=trimesh.visual.TextureVisuals(uv=uv,material=trimesh.visual.material.PBRMaterial(baseColorTexture=skin,metallicFactor=0,roughnessFactor=.76,doubleSided=True))
    geom.export(root/(name+'.glb'));print(name,len(faces))
