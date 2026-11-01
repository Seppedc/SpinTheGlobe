from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

out=Path(__file__).resolve().parents[2]/'SpinTheGlobe/Assets/DetectiveDesk/Generated/MaterialTextures'
out.mkdir(parents=True,exist_ok=True)
n=512
rng=np.random.default_rng(73)
y,x=np.mgrid[0:n,0:n]/n
tau=2*np.pi
noise=rng.random((n,n))-.5
def smooth_noise(radius):
    a=Image.fromarray(np.uint8(rng.random((n,n))*255)).filter(ImageFilter.GaussianBlur(radius))
    a=np.asarray(a,dtype=float)/255
    return (a-a.mean())/(a.std()+1e-6)
cloud=smooth_noise(13)
fine=smooth_noise(1.2)
grain=np.sin(tau*(y*68+np.sin(tau*x)*.6))
scratch=Image.new('L',(n,n));d=ImageDraw.Draw(scratch)
for i in range(110):
    sx=int(rng.integers(0,n));sy=int(rng.integers(0,n));length=int(rng.integers(5,85))
    d.line((sx,sy,sx+length,sy+int(rng.integers(-2,3))),fill=int(rng.integers(55,180)),width=1)
sc=np.asarray(scratch,dtype=float)/255
for name in ['Mahogany','AgedBrass','Parchment','DarkStone','CrimsonVelvet','EmeraldGlass','SmokyGlass']:
    if name=='Mahogany':
        v=grain*.035+fine*.012-sc*.22
        color=np.array([.26,.105,.060])[None,None,:]*(1+v[:,:,None])
        color+=sc[:,:,None]*np.array([.055,.027,.014])
        height=grain*.14+fine*.025-sc*.8;metal=np.zeros((n,n));smooth=.8+sc*.12+noise*.03
    elif name=='AgedBrass':
        pat=np.clip(cloud*.055+.08,0,.24)
        color=np.array([.57,.37,.15])[None,None,:]*(1-pat[:,:,None])+np.array([.12,.21,.15])[None,None,:]*pat[:,:,None]
        color+=fine[:,:,None]*.003; height=cloud*.025+fine*.035;metal=.92-pat*.25;smooth=.87-pat*.3
    elif name=='Parchment':
        v=cloud*.012+fine*.008
        color=np.array([.80,.70,.52])[None,None,:]+v[:,:,None]
        height=fine*.14+noise*.08;metal=np.zeros((n,n));smooth=.85+noise*.08
    elif name=='DarkStone':
        v=cloud*.013+fine*.008
        color=np.array([.19,.20,.19])[None,None,:]+v[:,:,None]
        height=cloud*.2+fine*.17;metal=np.zeros((n,n));smooth=.72+np.clip(cloud,-2,2)*.045
    elif name=='CrimsonVelvet':
        weave=np.sin(tau*x*128)*np.sin(tau*y*128)
        color=np.array([.27,.018,.045])[None,None,:]*(1+(weave*.025+cloud*.02)[:,:,None])
        height=weave*.025+noise*.035;metal=np.zeros((n,n));smooth=.85+noise*.04
    else:
        tint=[.035,.27,.11] if name=='EmeraldGlass' else [.14,.19,.22]
        color=np.broadcast_to(np.array(tint),(n,n,3)).copy()
        height=fine*.007;metal=np.zeros((n,n));smooth=.96+noise*.025
    dx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*.65
    dy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*.65
    normal=np.stack([-dx,dy,np.ones_like(dx)],axis=-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
    packed=np.ones((n,n,4));packed[:,:,0]=metal;packed[:,:,1:3]=0;packed[:,:,3]=np.clip(smooth,0,1)
    Image.fromarray(np.uint8(np.clip(color,0,1)*255)).save(out/(name+'_BaseColor.png'))
    Image.fromarray(np.uint8((normal*.5+.5)*255)).save(out/(name+'_Normal.png'))
    Image.fromarray(np.uint8(np.clip(packed,0,1)*255)).save(out/(name+'_MetallicSmoothness.png'))
(out/'README.txt').write_text('Procedural PBR placeholders, generated for DetectiveDesk.\nBaseColor: sRGB. Normal: tangent space, import as Normal Map. MetallicSmoothness: linear RGB, metallic in R, smoothness mask in A.\nNo supplied PBR normal, metallic/roughness, AO, or height maps were present. Existing map/rain artwork is retained in materials.\nAO and displacement are intentionally omitted; no baked geometry-specific information is fabricated.\n')
print('Created 21 reusable texture maps in',out)
