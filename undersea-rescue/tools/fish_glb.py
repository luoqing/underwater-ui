import io, json, struct
import numpy as np

def silhouette(rgb):
    """Remove exterior backdrop while preserving enclosed pupils and dark markings."""
    from scipy.ndimage import binary_closing, binary_fill_holes
    value = rgb.max(axis=2).astype(float)
    interior = binary_fill_holes(binary_closing(value > 25, iterations=2))
    return np.where(interior, 255, np.clip((value - 16) * 12.75, 0, 255)).astype(np.uint8)


def write_fish(path,name,geom,norm,uv,tri,targets,image,pectoral=None):
    picture=io.BytesIO();image.save(picture,format='PNG',optimize=True)
    doc={'asset':{'version':'2.0'},'scene':0,'scenes':[{'nodes':[0]}],'nodes':[{'name':name,'mesh':0}],'bufferViews':[],'accessors':[]}
    blob=bytearray()
    def view(data):
        blob.extend(b'\0'*(-len(blob)%4));idx=len(doc['bufferViews']);doc['bufferViews'].append({'buffer':0,'byteOffset':len(blob),'byteLength':len(data)});blob.extend(data);return idx
    def acc(a,typ,t=5126):
        a=np.asarray(a,dtype='<f4' if t==5126 else '<u2')
        dim={'SCALAR':1,'VEC2':2,'VEC3':3}[typ];a=a.reshape(-1,dim)
        i=len(doc['accessors']);doc['accessors'].append({'bufferView':view(a.tobytes()),'componentType':t,'count':len(a),'type':typ,'min':a.min(axis=0).tolist(),'max':a.max(axis=0).tolist()});return i
    doc['materials']=[{'name':'Skin and fins','doubleSided':True,'alphaMode':'MASK','alphaCutoff':.35,'pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicFactor':.15,'roughnessFactor':.42}}]
    doc['meshes']=[{'name':name,'weights':[0,0],'extras':{'targetNames':['Tail port','Tail starboard']},'primitives':[{'attributes':{'POSITION':acc(geom,'VEC3'),'NORMAL':acc(norm,'VEC3'),'TEXCOORD_0':acc(uv,'VEC2')},'indices':acc(tri,'SCALAR',5123),'material':0,'targets':[{'POSITION':acc(d,'VEC3')} for d in targets]}]}]
    if pectoral is not None:
        doc['meshes'][0]['primitives'][0]['attributes']['_PECTORAL']=acc(pectoral,'SCALAR')
    doc['animations']=[{'name':'Swim','samplers':[{'input':acc([0,.18,.36,.54,.72],'SCALAR'),'output':acc([0,0,1,0,0,0,0,1,0,0],'SCALAR')}],'channels':[{'sampler':0,'target':{'node':0,'path':'weights'}}]}]
    doc['images']=[{'mimeType':'image/png','bufferView':view(picture.getvalue())}];doc['textures']=[{'source':0}]
    doc['buffers']=[{'byteLength':len(blob)}];meta=json.dumps(doc,separators=(',',':')).encode();meta+=b' '*(-len(meta)%4);blob+=b'\0'*(-len(blob)%4)
    path.write_bytes(struct.pack('<4sII',b'glTF',2,28+len(meta)+len(blob))+struct.pack('<I4s',len(meta),b'JSON')+meta+struct.pack('<I4s',len(blob),b'BIN\0')+blob)
