"""Original Stage D machinery, using the existing worn 512px palette atlas.

blender -b --python-exit-code 1 --python ArtSource/build_industry_assets.py
Writes only CompactPrimaryScrapper/CompactExportStation and this pack's manifest.
Dimensions use metres and Unity-style X/right, Y/up, Z/depth.
"""
import bpy, json, math, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_yard_assets as b
from build_yard_assets import cube, cyl, beam


def bolted_plate(name, x, y, z, width, height, tone='ochre'):
    cube(name, (x,y,z), (width,height,.045), tone, .018)
    for dx in [-width*.40,width*.40]:
        for dy in [-height*.35,height*.35]:
            cyl('Hexagonal service-cover fixing',(x+dx,y+dy,z-.030),.028,.025,'metal','Z',6)


def hydraulic_hose(name, points):
    # Faceted rigid tubing communicates the hydraulic system without live curves.
    for a,c in zip(points,points[1:]):beam(name,a,c,.034,'rubber')


def primary_scrapper():
    # Open automobile cradle and two-column hydraulic gantry. The player can see
    # the loaded car/fridge; neither a closed filled box nor a vehicle crusher wall.
    for x in [-1.90,1.90]:
        cube('Primary long skid',(x,.13,0),(.28,.26,6.70),'metal',.018)
        for z in [-2.92,2.92]:
            cube('Anchored gantry foot',(x,.28,z),(.56,.30,.68),'rust',.02)
            for dz in [-.21,.21]:cyl('Concrete-anchor hex bolt',(x,.452,z+dz),.045,.035,'cream',verts=6)
    cube('Open scrap receiving bed',(0,.33,-.10),(3.62,.21,5.65),'metal',.035)
    for x in [-1.32,0,1.32]:
        cube('Receiving bed longitudinal rail',(x,.462,-.10),(.08,.052,5.42),'cream')
    for z in [-2.72,-1.82,-.92,-.02,.88,1.78,2.55]:
        cube('Cradle transverse strengthening bar',(0,.22,z),(3.55,.16,.14),'rust')
    for x in [-1.73,1.73]:
        for z in [-1.70,1.70]:
            cube('Hydraulic gantry upright',(x,2.06,z),(.30,3.70,.37),'ochre',.025)
            cube('Gantry upright dark inner slide',(x*.91,2.06,z),(.055,3.16,.30),'metal')
        for y in [.70,1.00]:cube('Low receiving-bed side guard',(x,y,-.10),(.085,.11,5.3),'ochre')
        cube('Upper gantry longitudinal member',(x,3.87,0),(.36,.29,4.02),'ochre',.018)
        for z in [-1.69,1.69]:
            beam('Gantry knee brace',(x,3.35,z),(x,3.84,z-math.copysign(.53,z)),.13,'metal')
    for z in [-1.70,1.70]:
        cube('Overhead hydraulic crossbeam',(0,4.055,z),(3.87,.32,.40),'ochre',.025)
        cube('Crossbeam faded face stripe',(0,4.03,z-math.copysign(.213,z)),(3.50,.062,.015),'cream')
    cube('Supported dismantling head',(0,2.94,0),(3.09,.25,2.70),'metal',.018)
    for z in [-.93,0,.93]:cube('Head underside reinforced edge',(0,2.79,z),(2.91,.06,.10),'rust')
    for x in [-1.05,1.05]:
        cyl('Large hydraulic press cylinder',(x,3.53,0),.19,.91,'blue',verts=12)
        cyl('Cylinder cap flange',(x,4.008,0),.25,.072,'metal',verts=12)
        cyl('Exposed hydraulic piston rod',(x,3.02,0),.094,.41,'cream',verts=10)
        cube('Press-cylinder anchor',(x,4.06,0),(.50,.16,.52),'metal',.012)
        hydraulic_hose('High pressure hydraulic line',[(x,3.86,.25),(x,4.25,.25),(2.05,4.25,.25),(2.05,.85,.25),(2.28,.85,.30)])
    # Side hydraulic power pack is readable from eye height: reservoir, external
    # finned motor, protective louvres, pressure gauge and an operator console.
    cube('Hydraulic reservoir',(2.37,.69,.47),(1.05,.87,1.67),'blue',.035)
    cube('Reservoir bolted top',(2.37,1.17,.47),(1.15,.11,1.76),'metal',.018)
    cyl('Hydraulic oil filler',(2.59,1.27,.88),.073,.10,'ochre',verts=10)
    cyl('Primary electric drive motor',(2.41,1.50,.65),.23,.95,'blue','Z',12)
    for z in [.27,.43,.59,.75,.91,1.07]:cyl('Motor cooling rib',(2.41,1.50,z),.247,.028,'metal','Z',12)
    bolted_plate('Reservoir service door',2.37,.70,-.391,.73,.55,'blue')
    for x in [2.12,2.25,2.38,2.51,2.64]:cube('Reservoir dark vent slot',(x,.70,-.423),(.066,.29,.016),'rubber')
    cube('Operator-console pedestal',(2.37,1.03,-1.13),(.17,1.56,.18),'metal')
    bolted_plate('Angled original operator-console face',2.37,1.75,-1.23,.67,.57,'ochre')
    cube('Analog job meter',(2.37,1.86,-1.268),(.43,.16,.025),'ivory',.005)
    for i in range(7):cube('Meter graduation',(2.19+i*.058,1.86,-1.286),(.008,.063 if i%2==0 else .038,.008),'metal')
    beam('Meter red needle',(2.35,1.80,-1.292),(2.43,1.89,-1.292),.011,'rust')
    for x,tone in [(2.22,'rust'),(2.48,'leaflight')]:cyl('Protected operator button',(x,1.60,-1.29),.045,.035,tone,'Z',10)
    cyl('Primary power socket rim',(2.56,.89,-.451),.090,.035,'cream','Z',12)
    cyl('Primary power socket face',(2.56,.89,-.478),.061,.020,'rubber','Z',10)
    # Exit roller throat and tray face north (+Z); .7m transport top is shared
    # with Stage C. Whole-car loading remains the open southern approach.
    cube('Component discharge hopper',(0,1.15,2.67),(1.63,.77,.66),'sage',.025)
    cube('Dark component outlet',(0,.81,3.015),(1.09,.28,.026),'rubber')
    cube('Discharge belt tray',(0,.635,3.19),(1.41,.11,.47),'metal',.015)
    for x in [-.67,.67]:cube('Discharge tray side guard',(x,.735,3.20),(.055,.17,.50),'sage')
    for z in [3.08,3.29]:cyl('Discharge tray roller',(0,.696,z),.040,1.21,'cream','X',10)
    for x in [-.48,.48]:cube('Output safety stripe',(x,.72,3.19),(.11,.012,.33),'ochre')
    cube('Primary identification plate',(0,3.77,-1.925),(1.95,.27,.038),'cream',.012)
    cube('Loading ramp',(0,.245,-3.17),(2.57,.18,.49),'metal',.012)
    for x in [-1.12,1.12]:cube('Receiving ramp edge stripe',(x,.344,-3.17),(.16,.010,.44),'ochre')
    for x,z in [(-1.69,-1.53),(1.70,.34),(-1.69,1.45)]:
        cube('Restrained gantry rust scar',(x,1.89,z),(.012,.25,.16),'rust')


def export_station():
    # Portable weigh-and-dispatch bay: a steel roller counter, pallet cage and
    # tall analogue scale head. The input mouth faces -Z and stays at .7m.
    for x in [-1.20,1.20]:cube('Export station skid',(x,.07,0),(.13,.14,2.36),'metal',.012)
    cube('Heavy export scale platform',(0,.31,.10),(2.73,.17,2.12),'metal',.02)
    for x in [-1.12,1.12]:
        for z in [-.84,.94]:
            cube('Scale platform support',(x,.18,z),(.14,.28,.14),'rust')
            cyl('Load-cell isolator',(x,.37,z),.105,.045,'rubber',verts=8)
    cube('Front conveyor receiving bed',(-.15,.644,-.66),(1.32,.105,1.01),'blue',.016)
    for z in [-1.08,-.91,-.74,-.57,-.40]:cyl('Receiving-counter roller',(-.15,.692,z),.036,1.16,'cream','X',10)
    for x in [-.78,.48]:cube('Receiving-counter edge guide',(x,.745,-.69),(.046,.14,1.05),'metal')
    cube('Transfer mouth black depth',(-.15,.69,-.08),(1.24,.09,.30),'rubber')
    # Worn stackable pallet and open cage prevent the dispatch silhouette from
    # reading as another generic processing machine or an extra office counter.
    for x in [-.91,0,.91]:cube('Dispatch pallet bearer',(x,.49,.50),(.16,.23,.95),'darkwood')
    for i in range(7):cube('Dispatch pallet slat',(-1.02+i*.34,.63,.50),(.27,.066,1.04),'wood')
    for x in [-1.15,1.15]:
        for z in [.04,1.05]:cube('Open dispatch-cage upright',(x,1.20,z),(.08,1.15,.08),'blue',.01)
        for y in [.82,1.26,1.69]:cube('Dispatch cage side rail',(x,y,.545),(.060,.09,1.09),'metal')
        for z in [.24,.56,.88]:cube('Dispatch cage vertical stave',(x,1.23,z),(.025,.93,.035),'cream')
    for y in [.84,1.24,1.68]:cube('Back dispatch-cage rail',(0,y,1.05),(2.35,.10,.060),'blue')
    for x in [-.89,-.45,0,.45,.89]:cube('Back cage slat',(x,1.26,1.05),(.036,.90,.036),'metal')
    cube('Dispatch lower protective lip',(0,.80,.06),(2.30,.27,.05),'wood')
    # Side instrument pedestal deliberately stays outside the belt mouth.
    cube('Weighing-head pedestal',(1.18,1.10,-.49),(.075,1.41,.09),'metal')
    cube('Original mechanical weigh head',(1.16,1.83,-.49),(.51,.44,.31),'sage',.035)
    cube('Cream weighing dial',(1.16,1.84,-.663),(.42,.27,.025),'ivory',.008)
    for i in range(8):cube('Scale dial graduation',(.99+i*.047,1.87,-.681),(.006,.076 if i%2==0 else .045,.008),'metal')
    beam('Scale needle',(1.18,1.73,-.686),(1.09,1.90,-.686),.010,'rust')
    cube('Export power-junction box',(1.18,.94,-.49),(.23,.31,.21),'blue',.01)
    cyl('Export power socket rim',(1.18,.94,-.619),.059,.027,'cream','Z',10)
    cyl('Export power socket face',(1.18,.94,-.643),.039,.020,'rubber','Z',8)
    cube('Dispatch docket clipboard',(.89,1.02,-.94),(.44,.49,.035),'darkwood',.007)
    cube('Cream weigh docket',(.89,1.03,-.962),(.36,.40,.008),'ivory')
    for i in range(5):cube('Typed weigh-docket line',(.87,1.15-i*.066,-.968),(.25 if i%2 else .29,.010,.004),'metal')
    cube('Clip retaining docket',(.89,1.28,-.969),(.15,.047,.011),'metal')
    cube('Export identification plate',(0,1.43,1.092),(1.46,.25,.028),'cream',.008)
    for x in [-.48,.18]:cube('Receiving-tray safety stripe',(x,.733,-.79),(.095,.010,.49),'ochre')


BUILDERS={'CompactPrimaryScrapper':primary_scrapper,'CompactExportStation':export_station}
FOOTPRINTS={'CompactPrimaryScrapper':[6,7,4.5],'CompactExportStation':[3,2.5,2.4]}
BUDGETS={'CompactPrimaryScrapper':4500,'CompactExportStation':2500}


def main():
    b.prepare_materials(write_atlas=False);b.stats={}
    for name,builder in BUILDERS.items():
        b.export(name,builder)
        assert b.stats[name]['triangles']<=BUDGETS[name],(name,b.stats[name])
    manifest={'source':'Original Blender-authored machinery; ArtSource/build_industry_assets.py',
              'pack':'CompactIndustry','units':'metres','atlas':'ScrapshiftPropAtlas.png',
              'footprints_unity':FOOTPRINTS,'triangle_budgets':BUDGETS,'assets':b.stats}
    (b.OUT/'industry_asset_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print('SCRAPSHIFT_INDUSTRY_EXPORTED '+json.dumps(manifest))


if __name__=='__main__':main()
