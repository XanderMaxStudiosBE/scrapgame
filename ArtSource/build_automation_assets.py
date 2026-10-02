"""Original stage-C machinery. Exports only this pack; previous FBXs/PNGs stay unchanged.

blender -b --python-exit-code 1 --python ArtSource/build_automation_assets.py
All models use the existing 512px PropAtlas. Metres, shared single material.
"""
import bpy,json,sys,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import build_yard_assets as b
from build_yard_assets import cube,cyl,beam,ring

def port_frame(z,width=1.05):
    # Ground-level transport openings; deck top .7m matches the belt path.
    cube('Dark conveyor opening',(0,.81,z),(width,.46,.055),'rubber')
    for x in [-width/2,width/2]:cube('Port vertical guide',(x,.79,z-.01),(.07,.49,.12),'metal',.01)
    cube('Port top beam',(0,1.03,z),(width+.12,.06,.12),'metal')
    cube('Port roller tray',(0,.66,z),(width,.08,.26),'metal')
    for dz in [-.085,.085]:cyl('Port drive roller',(0,.685,z+dz),.034,width-.12,'cream','X',10)

def storage():
    for x in [-1.22,0,1.22]:cube('Storage pallet bearer',(x,.09,0),(.18,.18,2.35),'darkwood')
    for i in range(9):cube('Storage pallet slat',(-1.38+i*.345,.20,0),(.27,.07,2.35),'wood')
    for x in [-1.43,1.43]:
        for z in [-1.17,1.17]:
            cube('Storage heavy corner post',(x,1.18,z),(.10,2.05,.10),'blue',.016)
            cube('Bolted corner boot',(x,.23,z),(.18,.29,.18),'metal',.012)
    for x in [-1.45,1.45]:
        for i in range(5):cube('Aged side timber board',(x,.46+i*.35,0),(.065,.27,2.29),'wood')
        for z in [-.92,0,.92]:cube('Storage side steel strap',(x*1.015,1.12,z),(.035,1.96,.055),'metal')
    for z in [-1.18,1.18]:
        for y in [.40,1.17,1.66,2.05]:cube('Front and rear storage slat',(0,y,z),(2.82,.23,.065),'wood')
        for x in [-.97,.97]:cube('Port-side storage panel',(x,.81,z),(.84,.50,.065),'wood')
        port_frame(z,.97)
        cube('Storage transport identification',(0,1.42,z+(-.045 if z<0 else .045)),(.72,.16,.015),'cream')
    for z in [-1.19,1.19]:cube('Upper steel storage frame',(0,2.20,z),(2.95,.075,.095),'blue')
    for x in [-1.46,1.46]:cube('Upper side storage frame',(x,2.20,0),(.08,.075,2.4),'blue')
    for x in [-1.44,1.44]:beam('Rear storage diagonal brace',(x,.25,1.10),(-x,2.13,1.10),.03,'metal')
    cube('Latched access hatch',(.93,1.54,-1.22),(.68,.54,.042),'blue',.015)
    cube('Storage access handle',(.93,1.54,-1.256),(.17,.038,.045),'metal')

def tier_two():
    for x in [-1.17,1.17]:
        for z in [-.83,.83]:
            cube('Tier2 bolted skid foot',(x,.10,z),(.30,.20,.27),'metal',.02)
            cube('Tier2 load-bearing post',(x,.39,z),(.13,.65,.13),'metal',.012)
    cube('Tier2 heavy lower chassis',(0,.46,0),(2.83,.16,2.05),'metal',.03)
    cube('Tier2 cutting chamber',(0,1.13,0),(2.52,1.19,1.93),'sage',.045)
    for x in [-1.277,1.277]:
        cube('Tier2 removable service cover',(x,1.1,.20),(.036,.72,1.22),'blue',.018)
        for z in [-.24,.16,.56]:cube('Tier2 panel reinforcement',(x,1.1,z),(.07,.70,.06),'metal')
        for y in [.83,1.31]:
            for z in [-.26,.66]:cyl('Service panel bolt',(x*1.03,y,z),.024,.022,'cream','X',6)
    for z in [-1.15,1.15]:port_frame(z,1.20)
    for z in [-1.17,1.17]:
        for x in [-1.15,1.15]:cube('Thick conveyor-mouth guard',(x,.81,z),(.20,.43,.12),'sage',.018)
    cube('Raised gearbox housing',(0,1.90,.20),(1.86,.42,1.21),'ochre',.035)
    cube('Gearbox bolted cap',(0,2.13,.20),(1.99,.09,1.32),'metal',.02)
    for x in [-.74,.74]:
        for z in [-.28,.64]:cyl('Gearbox lid bolt',(x,2.186,z),.025,.024,'cream',verts=6)
    cyl('Tier2 external drive motor',(-1.36,1.16,.18),.25,.28,'blue','X',14)
    for x in [-1.26,-1.33,-1.40,-1.47]:cyl('Tier2 motor cooling ring',(x,1.16,.18),.269,.018,'metal','X',14)
    cube('Tier2 front operator panel',(.88,1.34,-.985),(.54,.54,.045),'blue',.016)
    cube('Analog throughput dial',(.88,1.45,-1.023),(.31,.16,.026),'ivory',.008)
    for i in range(6):cube('Dial graduation',(.75+i*.05,1.47,-1.044),(.007,.037,.005),'metal')
    cube('Dial needle',(.91,1.45,-1.048),(.011,.09,.005),'rust')
    for x,mat in [(.76,'rust'),(.99,'leaflight')]:cyl('Operator pushbutton',(x,1.23,-1.03),.038,.036,mat,'Z',10)
    cube('Tier2 recipe plate',(-.41,1.43,-.997),(.68,.19,.016),'cream')
    for i in range(5):cube('Dark ventilation louvre',(-.63+i*.17,1.20,-1.02),(.09,.21,.025),'metal')
    cyl('Tier2 power inlet rim',(-.90,.90,-1.018),.068,.035,'cream','Z',12)
    cyl('Tier2 power socket',(-.90,.90,-1.040),.046,.017,'rubber','Z',12)
    for x in [-.71,.71]:cube('Transport warning stripe',(x,.707,-1.15),(.12,.011,.15),'ochre')

def junction(merger=False):
    for x in [-.56,.56]:
        for z in [-.56,.56]:
            cube('Junction steel leg',(x,.31,z),(.075,.62,.075),'metal',.012)
            cube('Junction mounting foot',(x,.035,z),(.17,.07,.17),'metal',.008)
    cyl('Mechanical junction drum',(0,.40,0),.34,.36,'blue' if merger else 'sage',verts=16)
    cyl('Junction drive rim',(0,.55,0),.36,.065,'metal',verts=16)
    cube('Junction roller bed',(0,.635,0),(1.39,.08,1.39),'metal',.02)
    cube('Cross transport channel',(0,.681,0),(.56,.014,1.40),'rubber')
    cube('Cross transport channel',(0,.682,0),(1.40,.014,.56),'rubber')
    for x in [-.55,.55]:
        for z in [-.55,.55]:
            cube('Corner transport guard',(x,.755,z),(.36,.14,.055),'blue' if merger else 'sage',.014)
            cube('Corner transport guard',(x,.755,z),(.055,.14,.36),'blue' if merger else 'sage',.014)
    for z in [-.62,.62]:cyl('Z-port roller',(0,.702,z),.030,.51,'cream','X',10)
    for x in [-.62,.62]:cyl('X-port roller',(x,.702,0),.030,.51,'cream','Z',10)
    cube('Mechanical route selector',(0,.78,0),(.18,.11,.18),'metal',.012)
    cyl('Selector pivot',(0,.858,0),.042,.045,'ochre',verts=8)
    beam('Selector operating lever',(0,.87,0),(.08,.94,.04),.025,'metal')
    cube('Junction identification plate',(.43,.84,-.61),(.30,.13,.018),'cream')

BUILDERS={'CompactPortedStorage':storage,'CompactTier2Scrapper':tier_two,'CompactSplitter':junction,'CompactMerger':lambda:junction(True)}
def main():
    b.prepare_materials(write_atlas=False);b.stats={}
    for name,builder in BUILDERS.items():b.export(name,builder)
    manifest={'source':'Original Blender-authored models; ArtSource/build_automation_assets.py','pack':'CompactAutomation','units':'metres','atlas':'ScrapshiftPropAtlas.png','assets':b.stats}
    (b.OUT/'automation_asset_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print('SCRAPSHIFT_AUTOMATION_EXPORTED '+json.dumps(manifest))
if __name__=='__main__':main()
