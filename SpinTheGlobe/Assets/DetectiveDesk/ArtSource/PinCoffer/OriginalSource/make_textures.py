"""Generate lightweight, tileable-inspired PBR color textures from procedural noise."""
import os
import numpy as np
from PIL import Image
out=os.path.dirname(os.path.abspath(__file__))
rng=np.random.default_rng(41)
N=1024
x=np.arange(N,dtype=np.float32)
y=np.arange(N,dtype=np.float32)
# Long directional wood grain plus knots/variation; texture coordinates on cube faces
wave=(np.sin(x*.095+8*np.sin(y[:,None]*.011)+2*np.sin(x*.018)[None,:])+1)*.5
lines=np.sin(x*.51+3*np.sin(y[:,None]*.025)+2*np.sin(x*.004)[None,:])*.5+.5
broad=(np.sin(x*.017+1.4*np.sin(y[:,None]*.008))+.9)*.5
grain=np.clip(.13+.46*wave+.28*lines+.15*broad+rng.normal(0,.045,(N,N)),0,1)
for name,base in [('mahogany_color.png',(85,34,17)),('mahogany_dark_color.png',(52,22,13))]:
    color=np.stack([np.clip(b*(.53+.82*grain),0,255) for b in base],axis=-1)
    Image.fromarray(color.astype('uint8'),'RGB').save(os.path.join(out,name))
# Velvet: mottled plush surface
arr=rng.normal(0,1,(N,N))
for _ in range(6): arr=(arr+np.roll(arr,1,0)+np.roll(arr,1,1))/3
arr=(arr-arr.min())/max(1e-6,arr.max()-arr.min())
vel=np.stack([80+45*arr, 4+8*arr, 12+14*arr],axis=-1)
Image.fromarray(np.clip(vel,0,255).astype('uint8'),'RGB').save(os.path.join(out,'crimson_velvet_color.png'))
