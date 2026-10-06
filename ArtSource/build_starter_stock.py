"""Original stocked starter-yard fixtures; existing assets/atlas are never rewritten.

blender -b --python-exit-code 1 --python ArtSource/build_starter_stock.py
Designer coordinates use x/right, y/up, z/depth. Open fronts face negative z.
"""
import bpy, json, math, sys, uuid
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_yard_assets as b
# The older world authoring script executes exports on import. Keep these two
# tiny standalone tool primitives here so building this pack cannot touch it.
def wrench(x, y, z, length):
    b.beam('Spanner shaft', (x, y-length/2, z), (x, y+length/2, z), .027, 'metal')
    for side in [-1, 1]:
        b.cube('Open spanner jaw', (x+side*.04, y+length/2, z), (.03, .10, .025), 'metal', .006)
    b.cyl('Spanner heel', (x, y-length/2, z), .045, .025, 'metal', 'Z', 8)


def pliers(x, y, z):
    for side in [-1, 1]:
        b.beam('Pliers grip', (x+side*.045, y-.13, z), (x+side*.018, y, z), .034, 'rust')
        b.beam('Pliers jaw', (x+side*.018, y, z), (x+side*.025, y+.09, z), .025, 'metal')
    b.cyl('Pliers pivot', (x, y, z-.02), .021, .022, 'metal', 'Z', 8)

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Scrapshift/Resources/ScrapshiftProps'


def parts_shelf():
    for x in [-1.06, 1.06]:
        for z in [-.39, .39]:
            b.cube('Rusty angle upright', (x, .99, z), (.06, 1.98, .06), 'blue')
            b.cube('Rack foot plate', (x, .022, z), (.13, .044, .13), 'metal')
    for y in [.16, .88, 1.63]:
        b.cube('Worn timber shelf', (0, y, 0), (2.18, .065, .85), 'wood')
        for z in [-.405, .405]:
            b.cube('Shelf angle beam', (0, y-.052, z), (2.18, .07, .05), 'rust')
    b.beam('Rear cross brace', (-1.03, .22, .405), (1.03, 1.87, .405), .034)
    b.beam('Rear cross brace', (1.03, .22, .405), (-1.03, 1.87, .405), .034)
    for x in [-.73, -.14, .48]:
        b.cube('Recovered battery body', (x, .37, .02), (.43, .36, .39), 'rubber', .01)
        b.cube('Battery faded label', (x, .38, -.183), (.29, .14, .012), 'cream')
        for terminal in [-.12, .12]:
            b.cyl('Battery terminal', (x+terminal, .573, .06), .027, .046, 'metal', verts=8)
    for index, x in enumerate([-.70, -.17]):
        b.cube('Open parts tray bottom', (x, .944, 0), (.44, .055, .52), 'sage')
        for sx in [-.21, .21]:
            b.cube('Tray raised side', (x+sx, 1.01, 0), (.022, .17, .52), 'sage')
        b.cube('Tray back', (x, 1.01, .25), (.44, .17, .022), 'sage')
        for j in range(3):
            b.cyl('Recovered threaded part', (x-.12+j*.11, .996, -.06), .035, .07,
                  'metal' if index else 'rust', verts=6)
    b.cyl('Recovered motor housing', (.54, 1.105, .01), .15, .35, 'metal', 'X', 12)
    for x in [.42, .50, .58, .66]:
        b.cyl('Motor cooling ridge', (x, 1.105, .01), .165, .024, 'sage', 'X', 12)
    b.cyl('Motor spindle', (.795, 1.105, .01), .035, .13, 'metal', 'X', 8)
    for index, x in enumerate([-.75, -.25, .25]):
        b.cube('Old tool case', (x, 1.80, .07), (.43, .26, .43), 'ochre' if index == 1 else 'blue', .016)
        b.cube('Case clasp', (x, 1.77, -.151), (.06, .085, .026), 'metal')
        b.cube('Case label', (x, 1.85, -.151), (.16, .05, .01), 'cream')
    b.cyl('Old grease can', (.76, 1.805, .04), .117, .285, 'cream', verts=12)
    b.cyl('Grease can lid', (.76, 1.963, .04), .126, .022, 'metal', verts=12)
    b.cube('Grease tin corrosion', (.76, 1.79, -.08), (.12, .12, .014), 'rust')
    # A coiled hose hangs from the outside, rather than filling every shelf with boxes.
    b.ring('Hanging rubber hose', (1.15, 1.33, 0), .245, .025, 'rubber', 'X')
    b.ring('Hanging rubber hose', (1.19, 1.33, 0), .231, .023, 'rubber', 'X')
    b.beam('Loose hose end', (1.17, 1.1, -.05), (1.16, .77, -.12), .034, 'rubber')


def mixed_skip():
    b.cube('Skip lower base', (0, .12, 0), (2.45, .20, 1.40), 'metal')
    for x in [-1.24, 1.24]:
        side = b.cube('Tapered rusted skip side', (x, .60, 0), (.075, .98, 1.58), 'rust')
        side.rotation_euler[1] = math.radians(4 if x < 0 else -4)
        for z in [-.60, .18, .60]:
            b.cube('Skip welded side rib', (x, .60, z), (.12, .98, .055), 'metal')
        b.cube('Rolled skip top rim', (x, 1.12, 0), (.11, .065, 1.64), 'blue')
        b.cube('Lifting eye', (x*1.06, .83, 0), (.06, .21, .16), 'metal')
    b.cube('Skip rear wall', (0, .60, .77), (2.49, 1.02, .07), 'rust')
    b.cube('Lower loading side', (0, .40, -.76), (2.49, .64, .07), 'blue')
    b.cube('Low loading lip', (0, .76, -.77), (2.55, .065, .10), 'metal')
    for x in [-.82, .82]:
        b.cube('Low skip runner', (x, .043, 0), (.16, .086, 1.45), 'metal')
    for i in range(6):
        panel = b.cube('Unsorted bent sheet', (-.85+(i%3)*.75, .48+(i//3)*.22, -.12+(i%2)*.43),
                       (.60, .045, 1.0), ['cream', 'rust', 'metal'][i%3])
        panel.rotation_euler = (math.radians(12+i*7), math.radians(-8+i*5), math.radians(i*13))
    b.cyl('Discarded wheel hub', (-.68, .86, .14), .235, .14, 'metal', 'Z', 12)
    b.ring('Bent wheel hub rim', (-.68, .86, .05), .214, .031, 'rust', 'Z')
    b.cube('Appliance control panel', (.61, 1.02, .27), (.45, .37, .055), 'cream')
    for x in [.47, .72]:
        b.cyl('Detached rotary dial', (x, 1.05, .23), .053, .028, 'rubber', 'Z', 12)
    b.beam('Bent steel rod', (.05, .57, -.48), (.46, 1.24, .49), .040, 'metal')
    b.beam('Bent steel rod return', (.46, 1.24, .49), (.67, 1.16, .50), .040, 'metal')
    b.cube('Worn skip number plate', (-.73, .52, -.806), (.39, .15, .012), 'cream')
    for x in [.37, .65, .93]:
        b.cube('Painted loading caution block', (x, .52, -.81), (.13, .18, .014), 'ochre')


def workshop_rack():
    # A compact freestanding tool wall and drawer cabinet; decorative stock, never free equipment.
    for x in [-1.00, 1.00]:
        b.cube('Tool rack foot', (x, .036, 0), (.18, .072, .67), 'metal')
        b.cube('Tool rack upright', (x, 1.075, .19), (.065, 2.08, .065), 'rust')
    b.cube('Weathered tool backing', (0, 1.49, .19), (2.02, 1.07, .056), 'wood')
    for x in [-.94, -.69, -.44]:
        wrench(x, 1.53, .14, .40+(x+.94)*.15)
    pliers(-.13, 1.52, .12)
    b.beam('Hammer timber handle', (.17, 1.29, .13), (.17, 1.77, .13), .04, 'wood')
    b.cube('Hammer metal head', (.17, 1.77, .13), (.22, .070, .085), 'metal', .009)
    for x in [.44, .60, .76]:
        b.cube('Screwdriver handle', (x, 1.37, .12), (.04, .15, .045), 'ochre')
        b.beam('Screwdriver shaft', (x, 1.445, .12), (x, 1.76, .12), .01)
    b.cube('Bench-height tool shelf', (0, .94, 0), (2.08, .08, .62), 'wood')
    b.cube('Upper shelf', (0, 2.04, .14), (2.16, .075, .42), 'wood')
    for x in [-.66, -.19, .33]:
        b.cube('Vintage parts tin', (x, 2.17, .14), (.35, .19, .31), 'sage', .009)
        b.cube('Parts tin label', (x, 2.17, -.02), (.15, .065, .009), 'ivory')
    b.cube('Parts drawer cabinet', (-.57, .475, .07), (.89, .82, .46), 'blue', .015)
    for column in range(2):
        for row in range(3):
            x=-.785+column*.435; y=.21+row*.24
            b.cube('Recovered parts drawer', (x, y, -.169), (.39, .21, .025), 'cream')
            b.cube('Drawer handle', (x, y-.037, -.195), (.115, .026, .024), 'metal')
            b.cube('Drawer written label', (x, y+.041, -.186), (.14, .035, .01), 'ivory')
    b.ring('Hanging copper lead', (.60, .62, .01), .23, .029, 'copper', 'Z')
    b.ring('Hanging copper lead', (.60, .62, -.03), .209, .025, 'copper', 'Z')
    b.cube('Oil drip tray', (.61, .05, 0), (.49, .052, .46), 'metal')
    b.cube('Loose maintenance rag', (.28, 1.002, -.06), (.28, .041, .22), 'cream')


BUILDERS = {'CompactPartsShelf': parts_shelf, 'CompactMixedSkip': mixed_skip,
            'CompactWorkshopRack': workshop_rack}


def ensure_meta(path):
    meta = Path(str(path)+'.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')


def main():
    b.prepare_materials(write_atlas=False)
    b.stats = {}
    for name, builder in BUILDERS.items():
        obj = b.export(name, builder)
        vertices = [obj.matrix_world @ v.co for v in obj.data.vertices]
        b.stats[name]['bounds_unity'] = {
            'min': [min(v[a] for v in vertices) for a in [0, 2, 1]],
            'max': [max(v[a] for v in vertices) for a in [0, 2, 1]]}
        ensure_meta(OUT/(name+'.fbx'))
        assert b.stats[name]['triangles'] <= 2400, (name, 'source budget')
    path = OUT/'starter_stock_manifest.json'
    path.write_text(json.dumps({'source':'Original Blender-authored fixtures; ArtSource/build_starter_stock.py',
                               'pack':'CompactStarterStock', 'units':'metres', 'atlas':'ScrapshiftPropAtlas.png',
                               'runtimeMaterial':'ScrapshiftMaterials/PropAtlas', 'assets':b.stats}, indent=2)+'\n')
    ensure_meta(path)
    print('SCRAPSHIFT_STARTER_STOCK_EXPORTED '+json.dumps(b.stats))


if __name__ == '__main__':
    main()
