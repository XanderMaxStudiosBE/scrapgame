"""Original compact-yard models, using the EXISTING 512px atlas without rewriting it.

blender -b --python-exit-code 1 --python ArtSource/build_compact_assets.py
Only the Compact* FBXs and their independent manifest are authored here.
"""
import bpy, json, math, sys, random
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import build_yard_assets as b
from build_yard_assets import cube,cyl,beam,ring,profile

def bolts(name,xs,y,z):
    for x in xs:cyl(name,(x,y,z),.025,.024,'metal','Z',6)

def generator():
    # A small workshop diesel set, not a generic cube: wheeled skid, engine,
    # slotted enclosure, fuel tank, radiator, exhaust and an accessible panel.
    for x in [-.65,.65]:cube('Generator skid',(x,.14,0),(.09,.16,1.12),'metal',.018)
    cube('Generator chassis',(0,.25,0),(1.47,.15,1.05),'metal',.035)
    for x in [-.69,.69]:
        for z in [-.36,.36]:
            cyl('Small transport wheel',(x,.20,z),.16,.13,'rubber','X',12)
            cyl('Wheel hub',(x*1.08,.20,z),.045,.018,'cream','X',8)
    cube('Worn yellow generator housing',(.15,.66,0),(1.1,.72,.91),'ochre',.055)
    cube('Exposed engine block',(-.48,.56,.03),(.40,.42,.60),'metal',.035)
    cyl('Alternator end',(-.49,.59,-.33),.17,.10,'blue','Z',12)
    for y in [.46,.53,.60,.67]:cube('Engine cooling fin',(-.49,y,.05),(.45,.018,.64),'cream')
    cube('Radiator dark opening',(.18,.61,-.469),(.78,.43,.027),'rubber',.018)
    for i in range(12):cube('Radiator grille slat',(-.17+i*.063,.61,-.49),(.018,.39,.023),'metal')
    cube('Generator service panel',(.27,.65,.469),(.64,.49,.025),'ochre',.018)
    for y in [.46,.85]:bolts('Panel fastener',[-.025,.56],y,.49)
    cube('Fuel tank',(.10,1.04,.10),(.79,.16,.72),'ochre',.035)
    cyl('Fuel cap',(.14,1.137,.10),.07,.027,'metal',verts=12)
    cube('Generator instrument panel',(.51,.82,-.492),(.26,.28,.03),'blue',.012)
    cyl('Analog power meter',(.51,.86,-.522),.063,.018,'ivory','Z',12)
    beam('Meter needle',(.51,.86,-.535),(.53,.897,-.535),.006,'metal')
    for x in [.45,.57]:cyl('Socket',(x,.75,-.529),.034,.02,'rubber','Z',8)
    for x in [-.60,.65]:
        beam('Carry handle leg',(x,.97,-.39),(x,1.22,-.39),.042,'metal')
        beam('Carry handle rail',(x,1.22,-.39),(x,1.22,.39),.042,'metal')
        beam('Carry handle leg',(x,1.22,.39),(x,.97,.39),.042,'metal')
    cyl('Short exhaust',(-.46,.97,.30),.035,.54,'metal')
    cyl('Exhaust muffler',(-.46,.93,.30),.072,.27,'rust')
    cube('Exhaust heat guard',(-.51,.94,.30),(.04,.32,.20),'metal')
    cube('Generator rating plate',(.03,.88,-.493),(.32,.07,.012),'cream')
    for x in [-.12,0,.12]:cube('Rating plate line',(x,.88,-.504),(.06,.007,.004),'metal')

def scrapper():
    # Manually fed cutter: the throat, side motor and front collection tray
    # communicate the operating direction even without a floating label.
    for x in [-.72,.72]:
        for z in [-.47,.47]:
            cube('Scrapper bolted foot',(x,.09,z),(.24,.15,.23),'metal',.015)
            cube('Scrapper steel leg',(x,.56,z),(.13,.95,.13),'metal',.016)
    cube('Scrapper lower brace',(0,.30,.08),(1.65,.08,1.0),'metal')
    cube('Scrapper processing housing',(0,1.02,.08),(1.58,.84,.91),'sage',.045)
    cube('Front inspection door',(0,1.02,-.386),(1.26,.56,.035),'sage',.012)
    for x in [-.52,.52]:
        cube('Door hinge',(x,1.08,-.415),(.055,.24,.045),'metal',.008)
        cyl('Case bolt',(x,.80,-.418),.025,.015,'metal','Z',6)
    cube('Service door latch',(.36,1.08,-.43),(.075,.16,.035),'cream',.012)
    # Tapered open hopper, built from sheet walls rather than a filled cube.
    profile('Hopper left sheet',.065,[(-.20,1.42),(.66,1.42),(.92,1.93),(-.47,1.93)],'ochre').location.x=-.71
    profile('Hopper right sheet',.065,[(-.20,1.42),(.66,1.42),(.92,1.93),(-.47,1.93)],'ochre').location.x=.71
    for z in [-.40,.82]:
        panel=cube('Flared hopper end',(0,1.68,z),(1.46,.59,.055),'ochre')
        panel.rotation_euler[0]=math.radians(25 if z<0 else -25)
    cube('Dark feed throat',(0,1.45,.20),(1.32,.06,.66),'rubber')
    for z in [.04,.34]:cyl('Feed cutter roller',(0,1.52,z),.095,1.18,'metal','X',12)
    for x in [-.57,.57]:cube('Hopper rim',(x,1.965,.22),(.075,.07,1.37),'metal')
    cyl('Side electric motor',(.88,1.0,.13),.21,.36,'blue','X',12)
    for x in [.75,.82,.89,.96,1.03]:cyl('Motor cooling rib',(x,1.0,.13),.225,.016,'metal','X',12)
    cube('Motor mounting flange',(.76,.77,.13),(.39,.08,.50),'metal')
    cube('Output tray bed',(0,.65,-.75),(1.35,.075,.61),'metal',.025)
    for x in [-.66,.66]:cube('Output tray side',(x,.75,-.75),(.045,.19,.65),'sage',.01)
    cube('Output safety lip',(0,.72,-1.045),(1.35,.14,.045),'sage')
    cube('Output opening',(0,.82,-.40),(.76,.22,.04),'rubber')
    cube('Control box',(-.90,1.03,-.10),(.21,.44,.31),'blue',.025)
    cyl('Emergency stop',(-.90,1.15,-.27),.052,.055,'rust','Z',12)
    cyl('Start button',(-.90,.98,-.27),.03,.025,'leaflight','Z',8)
    cube('Machine data plate',(.05,1.24,-.42),(.45,.08,.014),'cream')
    for x in [-.45,.45]:cube('Output hazard stripe',(x,.665,-.8),(.16,.012,.32),'ochre')

def motor():
    for x in [-.17,.17]:cube('Motor mounting foot',(x,.045,0),(.10,.09,.31),'metal',.008)
    cyl('Cast motor housing',(0,.21,0),.17,.41,'blue','Z',12)
    for z in [-.18,-.12,-.06,0,.06,.12,.18]:cyl('Cast cooling rib',(0,.21,z),.183,.017,'metal','Z',12)
    cyl('End winding aperture',(0,.21,-.215),.15,.021,'rubber','Z',12)
    ring('Exposed copper winding',(0,.21,-.236),.102,.028,'copper','Z')
    cyl('Motor shaft',(0,.21,-.29),.035,.15,'cream','Z',8)
    cube('Motor junction box',(.12,.37,.05),(.18,.09,.14),'blue',.018)
    cube('Motor label',(-.02,.385,.09),(.13,.012,.07),'cream')

def compressor():
    cube('Compressor base',(0,.04,0),(.44,.08,.35),'metal',.02)
    for x in [-.18,.18]:
        for z in [-.12,.12]:cyl('Rubber isolation foot',(x,.07,z),.035,.09,'rubber',verts=8)
    cyl('Sealed compressor shell',(0,.24,0),.18,.30,'metal',verts=16)
    cyl('Compressor seam',(0,.27,0),.189,.028,'rubber',verts=16)
    cyl('Domed cover',(0,.405,0),.145,.055,'metal',verts=16)
    cube('Relay terminal cover',(.16,.27,.09),(.11,.14,.14),'rubber',.025)
    beam('Copper pressure tube',(-.15,.34,-.02),(-.26,.34,-.02),.025,'copper')
    beam('Copper pressure elbow',(-.26,.34,-.02),(-.26,.43,-.02),.025,'copper')
    beam('Return copper tube',(.10,.38,-.14),(.22,.38,-.24),.021,'copper')
    cube('Compressor identification label',(0,.25,-.185),(.17,.095,.008),'cream')

def fridge():
    for x in [-.30,.30]:
        for z in [-.29,.29]:cyl('Adjustable fridge foot',(x,.065,z),.043,.13,'rubber',verts=8)
    cube('Fridge worn enamel case',(0,.96,0),(.78,1.72,.73),'cream',.034)
    cube('Lower door',(0,.79,-.387),(.745,1.22,.062),'ivory',.022)
    cube('Freezer door',(0,1.58,-.387),(.745,.34,.062),'ivory',.022)
    for y in [.98,1.57]:
        cube('Black handle mounting',(-.285,y,-.447),(.057,.25,.045),'metal',.008)
        cube('Fridge pull handle',(-.285,y,-.487),(.037,.21,.045),'cream',.012)
    cube('Door gasket seam',(0,1.414,-.416),(.74,.018,.014),'rubber')
    cube('Fridge top rim',(0,1.84,0),(.79,.045,.75),'cream',.012)
    cube('Brand badge',(.16,1.71,-.424),(.19,.047,.012),'metal',.005)
    cube('Lower service vent',(0,.19,-.416),(.57,.10,.015),'metal')
    for i in range(8):cube('Service vent slot',(-.245+i*.07,.19,-.427),(.014,.065,.009),'rubber')
    for x in [-.29,.29]:cube('Rear condenser vertical',(x,.96,.39),(.035,1.28,.035),'metal')
    for i in range(11):beam('Condenser coil',(-.29,.36+i*.115,.39),(.29,.36+i*.115,.39),.021,'metal')
    cube('Rust at lower corner',(.31,.29,-.421),(.09,.14,.008),'rust')
    cube('Rust at upper seam',(-.31,1.405,-.421),(.08,.022,.008),'rust')

def delivery_truck():
    # A small original cab-over salvage truck, parked inside the fixed receiving
    # bay. The open flatbed silhouette communicates delivery, not another car.
    cube('Truck ladder chassis',(0,.47,0),(1.65,.24,4.0),'metal',.035)
    cube('Weathered cream cab',(0,1.1,-1.32),(1.90,1.30,1.48),'cream',.06)
    cube('Cab roof',(0,1.81,-1.32),(1.98,.13,1.57),'cream',.04)
    cube('Truck windshield',(0,1.48,-2.08),(1.63,.55,.025),'glass',.015)
    cube('Split windshield pillar',(0,1.48,-2.105),(.04,.56,.032),'metal')
    for x in [-.97,.97]:
        cube('Side window',(x,1.47,-1.3),(.023,.49,.96),'glass',.015)
        cube('Door handle',(x,1.05,-1.15),(.04,.045,.16),'metal')
        beam('Wing mirror arm',(x,1.5,-1.89),(x*1.18,1.5,-1.91),.023,'metal')
        cube('Truck mirror',(x*1.18,1.56,-1.91),(.07,.22,.11),'metal',.01)
    for x in [-.85,.85]:
        for z in [-1.35,1.30]:
            cyl('Truck tyre',(x,.36,z),.35,.27,'rubber','X',16)
            cyl('Rusty steel truck wheel',(x*1.16,.36,z),.20,.025,'cream','X',12)
            cyl('Truck wheel hub',(x*1.18,.36,z),.078,.028,'metal','X',8)
    cube('Front bumper',(0,.50,-2.11),(2.05,.17,.14),'metal',.025)
    cube('Truck grille',(0,.85,-2.08),(.90,.20,.04),'metal')
    for x in [-.73,.73]:
        cyl('Round truck headlamp',(x,.82,-2.12),.12,.045,'ivory','Z',12)
        cube('Amber indicator',(x,.60,-2.12),(.15,.07,.02),'ochre')
    cube('Truck cargo bed',(0,.66,.77),(1.96,.14,2.75),'wood',.018)
    for x in [-.965,.965]:
        cube('Truck bed sideboard',(x,1.01,.77),(.065,.57,2.77),'wood')
        for z in [-.53,.10,.75,1.40,2.13]:cube('Truck bed metal stake',(x*1.02,1.02,z),(.06,.70,.07),'rust')
    cube('Truck tailgate',(0,1.01,2.145),(1.99,.57,.065),'wood')
    for x in [-.77,.77]:cube('Tail lamp',(x,.67,2.196),(.16,.075,.02),'rust')
    for i in range(4):
        panel=cube('Discarded panel on truck',((i%2-.5)*.7,.90+i*.10,.65+(i%2)*.55),(.76,.075,.82),'rust' if i%2 else 'metal',.02)
        panel.rotation_euler[2]=.12*(i-1)
    ring('Cable coil in truck',(.43,1.02,1.50),.24,.035,'rubber')

def plastic_fragments():
    # Thin cream moulded casing pieces, distinct from rusted/steel sheet stacks.
    for i in range(3):
        p=profile('Broken moulded casing',.38-i*.05,[(-.19,.022+i*.034),(.14,.022+i*.034),(.19,.058+i*.034),(.13,.08+i*.034),(-.15,.068+i*.034)],'cream')
        p.rotation_euler[2]=math.radians(i*13);p.location.x=(i-1)*.028
    for i in range(3):cube('Plastic reinforcing rib',(-.13+i*.11,.062,-.05),(.025,.025,.21),'ivory')

def insulation_coil():
    # Empty rubber jackets. No copper ends, so it reads differently from wire.
    for x,z,y in [(-.10,-.05,.025),(.09,.03,.044),(-.07,.07,.065),(.03,-.09,.086)]:
        ring('Recovered empty insulation',(x,y,z),.115,.017,'rubber')
    for x in [-.10,.07]:cyl('Short rubber offcut',(x,.025,-.08),.023,.24,'rubber','Z',8)

def boundary_tree():
    # Softer original foliage silhouette for the compact yard. Keep the existing
    # faceted legacy poplars untouched; smooth imported normals suit this new mesh.
    rng=random.Random(740)
    cyl('Boundary tree trunk',(0,2.25,0),.16,4.5,'darkwood',verts=9)
    for i in range(14):
        angle=i*2.4;height=3.2+i*.22;spread=.55+(i%3)*.18
        x=math.cos(angle)*spread;z=math.sin(angle)*spread
        beam('Small upward branch',(0,height-.8,0),(x,height,z),.055,'darkwood')
        bpy.ops.mesh.primitive_uv_sphere_add(segments=9,ring_count=5,radius=1,location=b.xyz((x,height+.4,z)))
        o=bpy.context.object;o.name='Soft irregular leaf cluster';o.scale=b.xyz((1.15+rng.random()*.28,1.3+rng.random()*.20,1.05+rng.random()*.25))
        for p in o.data.polygons:p.use_smooth=True
        b.add(o,'leaflight' if i%4==0 else 'leaf')
    for i in range(4):
        a=i*math.tau/4;beam('Exposed tree root',(0,.1,0),(.36*math.cos(a),.025,.36*math.sin(a)),.055,'darkwood')

BUILDERS={'CompactGenerator':generator,'CompactTier1Scrapper':scrapper,'CompactMotor':motor,'CompactCompressor':compressor,'CompactRefrigerator':fridge,'CompactDeliveryTruck':delivery_truck,'CompactPlasticFragments':plastic_fragments,'CompactInsulationCoil':insulation_coil,'CompactBoundaryTree':boundary_tree}

def main():
    b.prepare_materials(write_atlas=False)
    b.stats={}
    for name,builder in BUILDERS.items():b.export(name,builder)
    manifest={'source':'Original Blender-authored models; ArtSource/build_compact_assets.py','pack':'CompactStarter','units':'metres','atlas':'ScrapshiftPropAtlas.png','assets':b.stats}
    (b.OUT/'compact_asset_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print('SCRAPSHIFT_COMPACT_EXPORTED '+json.dumps(manifest))

if __name__=='__main__':main()
