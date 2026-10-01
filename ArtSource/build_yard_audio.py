"""Original synthesized mono yard sounds; no sampled/third-party audio. Python standard library only."""
from pathlib import Path
import math, random, struct, wave, json
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Scrapshift/Resources/ScrapshiftAudio'
OUT.mkdir(parents=True,exist_ok=True)
RATE=22050
rng=random.Random(1937)
manifest={}
def export(name,seconds,make):
    samples=make(int(RATE*seconds))
    peak=max(abs(x) for x in samples) or 1
    gain=.7/max(1,peak)
    with wave.open(str(OUT/(name+'.wav')),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(RATE)
        f.writeframes(b''.join(struct.pack('<h',round(max(-1,min(1,x*gain))*32767)) for x in samples))
    manifest[name]={'seconds':seconds,'sample_rate':RATE,'channels':1,'source':'original deterministic synthesis'}
def step(n):
    data=[]; low=0
    for i in range(n):
        t=i/RATE;noise=rng.uniform(-1,1);low=.7*low+.3*noise
        env=math.exp(-t*24)*min(1,t*200)
        grit=(noise-low)*(.18+.12*math.sin(t*145))
        data.append(env*(.28*math.sin(2*math.pi*105*t)+grit))
    return data
def tool(n):
    data=[]
    for i in range(n):
        t=i/RATE
        hit=sum(math.sin(2*math.pi*f*t)*a for f,a in [(780,.18),(1275,.12),(2290,.07)])*math.exp(-35*t)
        scrape=rng.uniform(-.17,.17)*math.sin(math.pi*i/n)**2*math.exp(-8*t)
        data.append(hit+scrape)
    return data
def pickup(n):
    low=0;data=[]
    for i in range(n):
        low=.55*low+.45*rng.uniform(-1,1)
        data.append(.35*low*math.sin(math.pi*i/n)**2)
    return data
def sale(n):
    data=[]
    for i in range(n):
        t=i/RATE; value=0
        for start,f in [(0,880),(.13,1320)]:
            u=t-start
            if u>=0:value+=(math.sin(2*math.pi*f*u)*.14+math.sin(2*math.pi*f*2.6*u)*.025)*math.exp(-12*u)*min(1,u*500)
        data.append(value)
    return data
def motor(n,fan=False):
    # Whole-number cycles and crossfaded noise ensure no seam click.
    noises=[rng.uniform(-1,1) for _ in range(n)]
    fade=int(RATE*.08)
    for i in range(fade):
        blend=i/fade
        value=noises[i]*(1-blend)+noises[n-fade+i]*blend
        noises[i]=value;noises[n-fade+i]=value
    data=[]; low=0
    for i in range(n):
        t=i/RATE; low=.94*low+.06*noises[i]
        tone=(.025 if fan else .075)*math.sin(2*math.pi*60*t)+(.015 if fan else .04)*math.sin(2*math.pi*120*t)
        data.append(tone+low*(.1 if fan else .15))
    # Continuity at endpoints, with a quiet smooth seam.
    for i in range(fade):
        factor=.6+.4*math.sin(math.pi*.5*i/fade)
        data[i]*=factor;data[-1-i]*=factor
    data[-1]=data[0]
    return data
def ambience(n):
    data=[]; low=0
    for i in range(n):
        t=i/RATE;low=.99*low+.01*rng.uniform(-1,1)
        value=low*.7*(.7+.3*math.sin(2*math.pi*t/16))
        for start in [2.2,2.48,7.1,11.8,12.1]:
            u=t-start
            if 0<u<.16:
                value+=.018*math.sin(2*math.pi*(2400*u+1800*u*u))*math.sin(math.pi*u/.16)**2
        # Fade loop boundaries through silence, no discontinuity.
        value*=min(1,t/.3,(16-t)/.3)
        data.append(value)
    return data
export('GravelStep',.2,step);export('ToolStroke',.28,tool);export('Pickup',.18,pickup);export('Sale',.48,sale)
export('StripperLoop',2,lambda n:motor(n));export('FanLoop',2,lambda n:motor(n,True));export('YardAmbience',16,ambience)
(OUT/'audio_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print(json.dumps(manifest))
