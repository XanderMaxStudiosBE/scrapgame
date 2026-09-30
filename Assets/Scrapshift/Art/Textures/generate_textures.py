# Original deterministic surface art; requires Python 3 and Pillow.
# This only regenerates PNG pixels. It never rewrites .meta files or materials.
from pathlib import Path
import random, math
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
N=64
SURFACES=['RustPaint','DarkMetal','CorrugatedMetal','WeatheredWood','Gravel','Copper','WireInsulation']
def noise(seed,cells=8):
 r=random.Random(seed); grid=[[r.random() for _ in range(cells)] for _ in range(cells)]
 def sample(x,y):
  u=x/(N-1)*cells;v=y/(N-1)*cells;a=int(u);b=int(v);fx=u-a;fy=v-b
  fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy)
  return ((1-fx)*grid[b%cells][a%cells]+fx*grid[b%cells][(a+1)%cells])*(1-fy)+((1-fx)*grid[(b+1)%cells][a%cells]+fx*grid[(b+1)%cells][(a+1)%cells])*fy
 return sample
bases={'RustPaint':(143,91,59),'DarkMetal':(42,50,47),'CorrugatedMetal':(95,105,90),'WeatheredWood':(108,93,67),'Gravel':(93,87,68),'Copper':(182,109,61),'WireInsulation':(35,43,39)}
for idx,name in enumerate(SURFACES):
 coarse=noise(idx+40,4);fine=noise(idx+90,24);detail=noise(idx+110,8)
 image=Image.new('RGB',(N,N)); pixels=image.load()
 for y in range(N):
  for x in range(N):
   u=x/(N-1);v=y/(N-1); n=coarse(x,y);f=fine(x,y);d=detail(x,y);base=bases[name];shade=(f-.5)*12+(d-.5)*10
   if name=='RustPaint':
    rust=max(0,min(1,(n-.40)*4)); base=tuple(a*(1-rust)+b*rust for a,b in zip((109,117,93),(183,89,53))); shade+=(f-.5)*12
   elif name=='CorrugatedMetal': shade+=math.cos(u*math.tau*8)*13+(n-.5)*7
   elif name=='WeatheredWood':
    shade+=math.sin(u*math.tau*12+(n-.5)*1.3)*9+(n-.5)*20
    if abs(math.sin(u*math.tau*4))<.08: shade-=15
   elif name=='Gravel':
    # Coarse pebbles merge into packed earth; periodic noise has no borders.
    shade+=(n-.5)*13+(f-.5)*29
   elif name=='Copper': shade+=math.cos(u*math.tau*7)*5+(n-.5)*12
   elif name=='WireInsulation': shade+=math.cos(u*math.tau*6)*3
   pixels[x,y]=tuple(max(0,min(255,round(c+shade))) for c in base)
 path=ROOT/'Art/Textures'/f'{name}.png';image.save(path)
