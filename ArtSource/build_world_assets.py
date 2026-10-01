"""Original additive world pack. Export only new models; never regenerate legacy FBXs/atlas.
blender -b --python-exit-code 1 --python ArtSource/build_world_assets.py [-- --only Name,...]
"""
import bpy,sys,math,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import build_yard_assets as b
from build_yard_assets import cube,cyl,beam,ring,profile

def panel(name,points,mat):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata([b.xyz(p) for p in points],[],[tuple(range(len(points)))]);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);return b.add(o,mat)
def cable(name,points,width=.015,mat='rubber'):
    for a,z in zip(points,points[1:]):beam(name,a,z,width,mat)
def wrench(x,y,z,length=.4):
    beam('Spanner shaft',(x,y-length/2,z),(x,y+length/2,z),.027,'metal')
    for side in [-1,1]:
        cube('Open spanner jaw',(x+side*.04,y+length/2,z),(.03,.10,.025),'metal',.006)
    cyl('Spanner heel',(x,y-length/2,z),.045,.025,'metal','Z',8)
def pliers(x,y,z):
    for side in [-1,1]:
        beam('Pliers painted grip',(x+side*.045,y-.13,z),(x+side*.018,y,z),.034,'rust')
        beam('Pliers steel jaw',(x+side*.018,y,z),(x+side*.025,y+.09,z),.025,'metal')
    cyl('Pliers pivot',(x,y,z-.02),.021,.022,'metal','Z',8)
def workshop_details():
    for z in [-3.8,3.8]:
        cube('Eave flashing',(0,3.88,z),(19.15,.16,.08),'blue')
        # U-shaped gutter rather than an opaque pipe across the eave.
        cube('Gutter bed',(0,3.79,z),(19.15,.05,.19),'metal')
        for dz in [-.105,.105]:cube('Gutter rim',(0,3.83,z+dz),(19.15,.13,.035),'metal')
    for x in [-9,9]:
        for z in [-3,3]:
            cube('Upright boot',(x,.14,z),(.28,.28,.28),'metal',.01)
            for i in range(4):cube('Post warning stripe',(x,.48+i*.14,z-.095),(.18,.065,.014),'ochre' if i%2==0 else 'rubber')
            beam('Eave knee brace',(x,3.1,z),(x+(.7 if x<0 else -.7),3.84,z),.08,'metal')
        cable('Downpipe',[(x,3.8,3.9),(x,3.45,3.75),(x,.3,3.75),(x,.14,4)],.10,'metal')
    for x in [-6,6]:
        cube('Rear shelf beam',(x,2.73,3.22),(3.5,.12,.25),'wood')
        cube('Rear corrugated extension',(x,2.9,3.25),(3.2,.65,.055),'blue')
        for i in range(9):cube('Raised wall corrugation',(x-1.5+i*.375,2.9,3.20),(.035,.63,.035),'blue')
        cube('Cage lamp backplate',(x,2.38,3.12),(.18,.32,.07),'metal')
        cyl('Amber work lamp',(x,2.38,3.02),.09,.20,'warmglass',verts=8)
        for dx in [-.09,.09]:beam('Lamp cage',(x+dx,2.23,2.97),(x+dx,2.53,2.97),.014,'metal')
        ring('Lamp protective rim',(x,2.38,2.95),.115,.012,'metal','Z')
def toolwall():
    cube('Wood pegboard',(0,1.88,0),(4.2,1.5,.08),'wood',.025)
    for x in [-2.12,2.12]:cube('Steel pegboard edge',(x,1.88,-.02),(.06,1.55,.10),'metal')
    for i in range(17):
        for j in range(6):cube('Pegboard hole',(-1.95+i*.24,1.25+j*.25,-.047),(.016,.016,.005),'rubber')
    for i in range(4):wrench(-1.8+i*.23,1.95,-.085,.43-i*.04)
    pliers(-.70,1.9,-.1);pliers(-.45,1.9,-.1)
    beam('Hammer wood handle',(-.12,1.64,-.10),(-.12,2.12,-.10),.045,'wood')
    cube('Hammer head',(-.12,2.13,-.1),(.22,.075,.08),'metal',.014)
    for i in range(3):
        x=.25+i*.17;cube('Screwdriver handle',(x,1.81,-.09),(.045,.17,.05),'rust',.009)
        beam('Screwdriver shaft',(x,1.9,-.09),(x,2.14,-.09),.012,'metal')
    ring('Hanging tape roll',(.95,2.05,-.1),.12,.033,'ochre','Z')
    cube('Paper job card',(1.42,1.92,-.06),(.38,.45,.009),'ivory')
    for j in range(4):cube('Pencil marks',(1.42,2.05-j*.08,-.069),(.24,.008,.004),'metal')
    cube('Lower shelf',(0,1.1,-.19),(4.2,.12,.52),'wood',.018)
    # Drawer bank sits behind the bench; no added collider or inventory.
    cube('Parts cabinet',(-1.18,.49,0),(1.75,.94,.38),'blue',.025)
    for col in range(4):
        for row in range(3):
            x=-1.8+col*.41;y=.20+row*.26
            cube('Small parts drawer',(x,y,-.205),(.375,.225,.035),'cream',.008)
            cube('Drawer pull',(x,y-.05,-.237),(.13,.025,.025),'metal')
            cube('Drawer label',(x,y+.04,-.228),(.14,.045,.006),'ivory')
    for x in [.05,.50]:
        cube('Labelled parts tin',(x,1.24,-.05),(.38,.17,.26),'sage',.012)
        cube('Tin label',(x,1.25,-.19),(.17,.07,.008),'ivory')
    cube('Upper tool shelf',(0,2.73,.02),(4.35,.12,.58),'wood',.02)
    for i in range(5):
        x=-1.8+i*.75;cube('Old tool storage case',(x,2.99,.01),(.63,.42,.42),'blue' if i%2==0 else 'ochre',.035)
        cube('Case clasp',(x,2.92,-.21),(.075,.10,.03),'metal')
def repair_tools():
    cube('Multimeter rubber boot',(.20,.06,0),(.23,.11,.33),'ochre',.02)
    cube('Multimeter face',(.20,.12,0),(.19,.012,.28),'metal',.006)
    cube('LCD display',(.20,.131,.07),(.14,.007,.065),'leaflight')
    cyl('Selector dial',(.20,.141,-.035),.045,.018,'rubber',verts=12)
    cube('Dial index',(.20,.155,-.015),(.008,.006,.038),'ivory')
    cable('Red test lead',[(.25,.04,-.17),(.38,.015,-.27),(.42,.015,-.02),(.38,.015,.20),(.02,.015,.24),(-.12,.025,.07)],.012,'rust')
    cable('Black test lead',[(.15,.04,-.17),(.10,.015,-.25),(-.18,.015,-.25),(-.25,.025,-.10)],.012)
    for x,z,col in [(-.13,.03,'rust'),(-.26,-.04,'rubber')]:
        cube('Test probe grip',(x,.026,z),(.023,.025,.12),col,.005);beam('Probe tip',(x,.026,z-.06),(x,.026,z-.13),.006,'metal')
    cube('Screwdriver grip',(-.05,.027,-.11),(.035,.043,.13),'rust',.008)
    beam('Screwdriver shaft',(-.05,.027,-.05),(-.05,.027,.12),.010,'metal')
    cube('Parts tray',(-.23,.023,.17),(.15,.035,.12),'metal',.012)
    for i in range(3):cyl('Recovered hex nut',(-.27+i*.036,.049,.17),.016,.018,'ochre',verts=6)
def appliance_row():
    for x in [-1.7,0,1.7]:cube('Pallet bearer',(x,.10,0),(.16,.20,1.25),'darkwood')
    for i in range(10):cube('Pallet slat',(-1.95+i*.43,.23,0),(.35,.06,1.3),'wood')
    for x in [-1.3,-.10]:
        for dx in [-.42,.42]:
            for z in [-.36,.36]:cyl('Washer rubber foot',(x+dx,.30,z),.055,.07,'rubber',verts=8)
        cube('Salvaged washer casing',(x,.87,0),(1.02,1.08,.88),'cream',.025)
        cube('Washer top',(x,1.43,0),(1.08,.065,.94),'ivory',.018)
        cube('Control strip',(x,1.27,-.456),(.95,.20,.025),'cream')
        cube('Detergent drawer',(x-.27,1.28,-.482),(.34,.09,.023),'ivory',.008)
        cyl('Programme dial',(x+.24,1.28,-.48),.055,.04,'metal','Z',12)
        for i in range(3):cyl('Small control button',(x-.06+i*.07,1.29,-.48),.015,.025,'ochre','Z',8)
        cyl('Door recess',(x,.83,-.45),.325,.025,'rubber','Z',20)
        cyl('Smoked washer glass',(x,.83,-.49),.246,.018,'glass','Z',20)
        ring('Round cream door trim',(x,.83,-.48),.285,.043,'ivory','Z')
        cube('Door release',(x+.285,.83,-.515),(.07,.12,.04),'metal',.014)
        cube('Lower washer vent',(x,.46,-.47),(.61,.07,.012),'metal')
        cable('Hose behind appliance',[(x,.42,.46),(x+.44,.35,.48),(x+.47,.31,.27),(x+.48,.37,-.07)],.055,'rubber')
        cube('Paint chip',(x-.40,.52,-.464),(.09,.11,.01),'rust')
    cube('Old refrigerator',(1.2,1.20,.03),(1.06,1.9,.85),'cream',.03)
    for y,h in [(1.85,.53),(.94,1.18)]:
        cube('Refrigerator door',(1.2,y,-.41),(1.01,h,.045),'ivory',.018)
        cube('Fridge handle',(.81,y+.08,-.48),(.055,.26,.05),'metal',.012)
    cube('Microwave shell',(-1.3,1.69,.02),(.86,.46,.59),'cream',.02)
    cube('Microwave glass',(-1.40,1.69,-.29),(.57,.33,.015),'glass',.008)
    cube('Microwave door grip',(-1.08,1.69,-.31),(.035,.23,.04),'metal')
    for y in [1.61,1.76]:cyl('Microwave timer',(-.94,y,-.31),.042,.02,'metal','Z',8)
def shelter():
    for x in [-4.1,4.1]:
        for z in [-1.6,1.6]:
            cube('Salvage shelter post',(x,1.55,z),(.11,3.1,.11),'metal')
            beam('Shelf bay bracket',(x,2.4,z),(x+(.5 if x<0 else -.5),3.1,z),.06,'metal')
    roof=cube('Single pitch shelter roof',(0,3.23,0),(9,.10,4),'blue');roof.rotation_euler[0]=math.radians(-5)
    for i in range(17):beam('Shelter roof corrugation',(-4.2+i*.525,3.08,-2),(-4.2+i*.525,3.43,2),.028,'metal')
    cube('Shelter rear cladding',(0,1.5,1.66),(8.3,3,.06),'blue')
    for i in range(24):cube('Rear corrugation',(-4+i*.35,1.5,1.62),(.035,2.95,.035),'blue')
    for x in [-4.1,4.1]:beam('Wind brace',(x,.25,1.55),(x,2.7,-1.4),.045,'metal')
    cube('Shelter gutter',(0,3.04,-2.05),(9.1,.12,.12),'metal')
    cable('Shelter downpipe',[(4.15,3.04,-2.05),(4.15,.20,-1.9)],.085,'metal')
def office_details():
    # The existing office front is -Z, with a window at X +1.5 and a door at X -2.
    cube('Warm shallow office window',(1.5,1.75,-3.086),(1.90,1.06,.012),'warmglass')
    for x in [.5,1.5,2.5]:cube('Window mullion',(x,1.75,-3.13),(.045,1.18,.045),'cream')
    for y in [1.16,2.34]:cube('Window trim',(1.5,y,-3.13),(2.12,.07,.055),'ivory')
    # Silhouettes imply a modest lit interior without a promised enterable room.
    cube('Desk silhouette',(1.48,1.37,-3.104),(1.50,.035,.016),'darkwood')
    cube('Office paper stack',(1.1,1.43,-3.12),(.26,.10,.018),'ivory')
    cube('Desk lamp silhouette',(1.9,1.6,-3.12),(.025,.44,.018),'metal')
    cube('Desk lamp shade',(1.9,1.83,-3.123),(.24,.08,.025),'ochre')
    for i in range(4):cube('Half raised blinds',(1.5,2.15+i*.045,-3.15),(1.93,.018,.02),'cream')
    cube('Door inset window',(-2,1.72,-3.116),(.72,.64,.022),'warmglass')
    for dx in [-.4,.4]:cube('Door window trim',(-2+dx,1.72,-3.14),(.035,.72,.025),'cream')
    for y in [1.36,2.08]:cube('Door window trim',(-2,y,-3.14),(.83,.035,.025),'cream')
    cube('Porch edge',(-2,.06,-3.43),(2.7,.12,.6),'wood',.015)
    cube('Doormat',(-2,.128,-3.45),(.85,.02,.40),'rubber')
    cube('Electric meter',(-3.18,1.8,-3.13),(.28,.42,.08),'metal',.02)
    cyl('Meter dial',(-3.18,1.88,-3.18),.08,.018,'ivory','Z',12)
    cable('Meter conduit',[(-3.18,1.5,-3.14),(-3.18,.15,-3.14)],.025,'metal')
    for x in [-4.1,4.1]:cable('Office rainwater pipe',[(x,3.02,-3.2),(x,.20,-3.2),(x,.1,-3.48)],.075,'metal')
    cube('Office porch lamp',(-2,2.44,-3.18),(.29,.12,.09),'warmglass',.015)
    # A bench on the closed front wall, away from the entry and diary approach.
    for x in [2.55,3.55]:cube('Waiting bench leg',(x,.23,-3.48),(.09,.46,.52),'metal')
    cube('Worn waiting bench',(3.05,.48,-3.48),(1.45,.08,.56),'wood',.02)
    cube('Bench back',(3.05,.77,-3.15),(1.45,.35,.06),'wood',.018)
def works():
    # Two-metre brick panels keep texture scale believable on a large silhouette.
    for z in [-5,5]:
        for x in range(-12,13,2):
            for y in range(9):
                pts=[(x-1,y,z),(x+1,y,z),(x+1,y+1,z),(x-1,y+1,z)]
                panel('Brick factory facade',pts if z<0 else list(reversed(pts)),'soil')
    for x in [-13,13]:
        for z in range(-4,5,2):
            for y in range(9):
                pts=[(x,y,z-1),(x,y,z+1),(x,y+1,z+1),(x,y+1,z-1)]
                panel('Brick factory end',pts if x>0 else list(reversed(pts)),'soil')
    cube('Factory flat roof',(0,9.04,0),(26.5,.16,10.4),'metal')
    for z in [-5.07,5.07]:
        for x in [-10,-6,-2,2,6,10]:
            for y in [3,6]:
                cube('Factory window',(x,y,z),(1.5,1.7,.035),'glass')
                for dx in [-.78,0,.78]:cube('Factory steel mullion',(x+dx,y,z+(.03 if z>0 else -.03)),(.055,1.78,.03),'metal')
                for dy in [-.88,.88]:cube('Factory sill',(x,y+dy,z),(1.64,.07,.13),'cream')
    for x,h in [(-7,15),(7,17)]:
        cyl('Factory chimney',(x,h/2,3.0),.52,h,'soil',verts=12)
        cyl('Chimney cap',(x,h,3.0),.59,.16,'metal',verts=12)
    for x in [-8,8]:cube('Roof ventilation',(x,9.45,-1.6),(1.25,.8,1.4),'metal',.025)
def poplar():
    cyl('Poplar trunk',(0,2.7,0),.17,5.4,'darkwood',verts=8)
    for i in range(16):
        a=i*2.4;y=3.4+i*.34;r=1.20*(1-abs(y-6.2)/6)
        x=math.cos(a)*r;z=math.sin(a)*r
        beam('Poplar branch',(0,y-.5,0),(x,y,z),.055,'darkwood')
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=b.xyz((x,y,z)))
        o=bpy.context.object;o.name='Textured leaf cluster';o.scale=b.xyz((1.6,2,1.5));b.add(o,'leaf' if i%3 else 'leaflight')
    for i in range(5):
        a=i*math.tau/5;beam('Root',(0,.1,0),(.4*math.cos(a),.015,.4*math.sin(a)),.07,'darkwood')
def fixture():
    cube('Fluorescent steel housing',(0,.035,0),(.95,.075,.22),'metal',.015)
    cube('Reflector',(0,-.007,0),(.91,.01,.205),'ivory')
    for z in [-.055,.055]:cyl('Warm fluorescent tube',(0,-.035,z),.023,.82,'warmglass','X',8)
    for x in [-.44,.44]:cube('Tube end cap',(x,-.024,0),(.07,.04,.19),'cream',.005)
    for x in [-.34,.34]:beam('Suspension wire',(x,.07,0),(x,.6,0),.01,'metal')
def fencepost():
    cyl('Galvanized fence post',(0,1.14,0),.045,2.28,'metal',verts=8)
    cyl('Post cap',(0,2.29,0),.056,.05,'cream',verts=8)
    for y in [.18,1.1,2.1]:cube('Wire clamp',(0,y,0),(.12,.026,.06),'metal')
def weeds():
    for i in range(9):
        a=i*2.4;h=.18+(i%3)*.07;r=.12+(i%2)*.08;x=math.cos(a)*r;z=math.sin(a)*r
        # Tapered bent leaves with explicit back faces; no alpha or added material.
        pts=[(-.015,0,0),(.015,0,0),(x+.012,h*.65,z),(x,h,z)]
        panel('Grass blade',pts,'leaf' if i%2 else 'leaflight');panel('Grass blade back',list(reversed(pts)),'leaf')
    for i in range(3):
        a=i*2.1;beam('Dry weed stem',(.03*math.cos(a),0,.03*math.sin(a)),(.1*math.cos(a),.38,.1*math.sin(a)),.009,'ochre')
def teal_container():
    previous=b.MAT['sage'];b.MAT['sage']=b.MAT['blue'];b.container();b.MAT['sage']=previous

b.prepare_materials(False)
b.atlasmat.node_tree.nodes.get('Image Texture').image=bpy.data.images.load(str(b.ROOT/'Assets/Scrapshift/Resources/ScrapshiftWorld/WorldAtlas.png'))
manifest_path=b.OUT/'asset_manifest.json';manifest=json.loads(manifest_path.read_text());b.stats=manifest['assets']
builders=[('WorkshopDetails',workshop_details),('ToolWall',toolwall),('RepairTools',repair_tools),('ApplianceRow',appliance_row),('SalvageShelter',shelter),('OfficeDetails',office_details),('IndustrialWorks',works),('PoplarTree',poplar),('FluorescentFixture',fixture),('FencePost',fencepost),('TealContainer',teal_container),('WeedClump',weeds)]
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
selected=set(args[args.index('--only')+1].split(',')) if '--only' in args else {n for n,_ in builders}
assert selected.issubset({n for n,_ in builders}),'Unknown world model'
for name,builder in builders:
    if name in selected:
        b.export(name,builder);b.stats[name]['pack']='ScrapshiftWorld';assert b.stats[name]['triangles']<=5000,(name,b.stats[name])
manifest['assets']=b.stats;manifest['world_source']='Original additive Blender pack: ArtSource/build_world_assets.py; separate WorldAtlas material'
manifest_path.write_text(json.dumps(manifest,indent=2));print('WORLD_EXPORTED '+json.dumps({n:b.stats[n] for n in selected}))
