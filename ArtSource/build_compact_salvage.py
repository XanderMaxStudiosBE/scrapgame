"""Original, recognizable salvage stock for the compact SCRAPSHIFT yard.

blender -b --python-exit-code 1 --python ArtSource/build_compact_salvage.py
Only four NEW FBXs and their independent manifest are exported. The existing
working atlas, meshes, material assets, layouts and metadata are never rewritten.
"""
import bpy, json, math, sys, uuid
from pathlib import Path
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_yard_assets as b

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Scrapshift/Resources/ScrapshiftProps'


def pipe(name, points, width, material):
    for start, finish in zip(points, points[1:]):
        b.beam(name, start, finish, width, material)


def shell():
    # An already-stripped small hatchback. Empty engine bay, missing doors and
    # glass, exposed seats/rails and bare front hubs distinguish it from a job.
    for x in [-.57, .57]:
        b.cube('Visible chassis sill', (x, .27, 0), (.12, .17, 3.27), 'rust', .014)
        for z in [-1.02, 1.01]:
            b.beam('Suspension stub', (0, .27, z), (x * 1.24, .27, z), .075, 'metal')
    b.cube('Stripped passenger floor', (0, .35, .26), (1.22, .065, 1.81), 'darkwood')
    for z in [-.51, .20, .84]:
        b.cube('Pressed floor stiffener', (0, .395, z), (1.10, .025, .035), 'metal')
    for x in [-.33, .33]:
        b.cube('Seat cushion', (x, .47, .02), (.43, .13, .42), 'rubber', .025)
        seat = b.cube('Worn exposed seat back', (x, .69, .20), (.43, .43, .11), 'rubber', .022)
        seat.rotation_euler.x = math.radians(-9)
        b.cube('Torn seat fabric', (x + .07, .72, .13), (.09, .19, .014), 'cream')
        for z in [-.10, .16]:
            b.cube('Visible seat rail', (x, .415, z), (.38, .045, .035), 'metal')
    b.cube('Rear wheel housing', (0, .54, 1.12), (1.42, .30, .82), 'blue', .032)
    b.cube('Empty rear hatch shelf', (0, .74, 1.10), (1.24, .04, .70), 'darkwood')
    for x in [-.72, .72]:
        fender = b.profile('Front wing with missing lamp', .085,
                           [(-1.68, .30), (-1.68, .59), (-1.33, .68), (-.83, .68),
                            (-.60, .57), (-.60, .30), (-.80, .30), (-.82, .51),
                            (-1.30, .51), (-1.37, .30)], 'blue')
        fender.location.x = x
        b.cube('Empty headlight recess', (x * .79, .48, -1.70), (.25, .16, .035), 'rubber')
        b.cube('Rusty hatch lamp surround', (x * .84, .64, 1.57), (.21, .15, .035), 'rust')
    b.cube('Open engine bay crossmember', (0, .36, -1.57), (1.46, .09, .085), 'metal')
    b.cube('Engine bay bulkhead', (0, .63, -.65), (1.35, .32, .08), 'blue')
    for x in [-.45, .45]:
        b.cube('Empty engine mount', (x, .42, -1.0), (.15, .10, .22), 'metal')
    pipe('Discarded wire loom', [(-.55, .72, -.70), (-.35, .65, -.80),
                               (-.33, .48, -.98), (-.17, .39, -1.18)], .018, 'copper')
    for side in [-1, 1]:
        x = side * .64
        b.beam('Bare windscreen pillar', (x, .74, -.66), (x, 1.24, -.35), .045, 'blue')
        b.beam('Missing door rear pillar', (x, .69, .77), (x, 1.24, .52), .053, 'rust')
        b.beam('Empty side window roof rail', (x, 1.24, -.35), (x, 1.24, .52), .05, 'blue')
        b.beam('Rear hatch frame', (x, 1.23, .52), (x, .77, 1.41), .05, 'blue')
    b.cube('Dented roof skin', (0, 1.26, .10), (1.35, .045, .96), 'blue', .012)
    b.cube('Roof corrosion patch', (.31, 1.286, .24), (.36, .006, .34), 'rust')
    b.beam('Windscreen top rail', (-.64, 1.24, -.35), (.64, 1.24, -.35), .046, 'metal')
    b.cube('Empty dashboard', (0, .80, -.51), (1.13, .13, .25), 'rubber', .01)
    b.ring('Bare steering wheel', (-.34, .85, -.29), .13, .018, 'metal', 'Z')
    for x in [-.77, .77]:
        b.cyl('Bare front brake drum', (x, .28, -1.06), .145, .105, 'rust', 'X', 12)
        b.cyl('Front hub cap', (x * 1.07, .28, -1.06), .048, .035, 'metal', 'X', 8)
        b.cyl('Rear steel wheel', (x, .29, 1.03), .255, .10, 'metal', 'X', 12)
        b.ring('Rear wheel rolled rim', (x * 1.07, .29, 1.03), .198, .025, 'rust', 'X')
        b.cyl('Rear hub', (x * 1.08, .29, 1.03), .067, .03, 'cream', 'X', 8)
    for x in [-.53, .53]:
        b.cube('Supporting scrap sleeper', (x, .05, -.15), (.16, .10, 1.0), 'darkwood')


def washer(x, z, missing_door=False):
    b.cube('Old washing machine case', (x, .49, z), (.69, .88, .67), 'cream', .021)
    b.cube('Washer plinth', (x, .075, z), (.68, .11, .69), 'metal', .01)
    b.cube('Worn enamel top', (x, .955, z), (.72, .065, .70), 'ivory', .01)
    front = z - .349
    b.cyl('Dark open drum', (x, .47, front), .236, .027, 'rubber', 'Z', 16)
    b.ring('Pressed metal drum rim', (x, .47, front - .018), .211, .025, 'metal', 'Z')
    if not missing_door:
        b.ring('Washing machine ivory door ring', (x, .47, front - .051), .226, .031, 'ivory', 'Z')
        b.cyl('Scratched dark door glass', (x, .47, front - .045), .195, .035, 'glass', 'Z', 16)
        b.cube('Washer door pull', (x + .232, .47, front - .08), (.057, .18, .055), 'cream', .012)
    else:
        for row in range(3):
            for column in range(5):
                b.cube('Visible drum perforation', (x - .10 + column * .05, .40 + row * .055, front - .017),
                       (.012, .012, .012), 'cream')
    b.cube('Washer controls strip', (x, .836, front - .012), (.65, .10, .025), 'ivory')
    b.cube('Soap drawer', (x - .17, .838, front - .032), (.20, .06, .018), 'cream')
    b.cyl('Program dial', (x + .19, .835, front - .052), .036, .03, 'metal', 'Z', 12)
    b.cube('Old rating badge', (x + .05, .837, front - .032), (.09, .026, .012), 'blue')
    b.cube('Lower enamel corrosion', (x - .23, .19, front - .012), (.12, .17, .012), 'rust')
    b.cube('Service-panel seam', (x, .17, front - .017), (.44, .013, .009), 'metal')


def washer_lot():
    # Two recognizable washers with an unplugged microwave atop the taller
    # stock. One missing door shows the dark drum rather than another flat box.
    washer(-.40, .12, True)
    washer(.40, .30)
    b.cube('Rusty sorting rack foot', (0, .035, .20), (1.62, .07, .91), 'darkwood')
    for x in [-.66, -.22, .22, .66]:
        b.cube('Sorting rack plank', (x, .089, .20), (.29, .03, .87), 'wood')
    b.cube('Discarded microwave case', (-.40, 1.205, .15), (.66, .38, .52), 'blue', .022)
    b.cube('Microwave dark window', (-.49, 1.22, -.127), (.41, .24, .023), 'rubber', .006)
    for i in range(5):
        b.cube('Microwave safety grille', (-.65 + i * .08, 1.22, -.143), (.01, .20, .012), 'metal')
    b.cube('Microwave ivory handle', (-.233, 1.22, -.15), (.025, .20, .026), 'ivory')
    for y in [1.13, 1.27]:
        b.cyl('Microwave rotary control', (-.143, y, -.14), .03, .025, 'cream', 'Z', 10)
    pipe('Unplugged appliance lead', [(.67, .49, .63), (.73, .20, .68), (.53, .08, .71),
                                    (.07, .082, .75), (-.12, .083, .59)], .014, 'rubber')
    b.cube('Appliance plug', (-.12, .083, .59), (.07, .035, .05), 'metal')
    for y in [.35, .57, .79]:
        b.cube('Washer side stamped rib', (.756, y, .27), (.01, .025, .39), 'cream')


def reel():
    # A wooden cable drum with separate broad cheeks, visible staves, copper
    # cable and axle opening. It is horizontal on low chocks, not a donut pile.
    for x in [-.25, .25]:
        b.cyl('Wood cable drum cheek', (x, .49, 0), .47, .075, 'wood', 'X', 16)
        b.ring('Cable drum metal edge band', (x * 1.15, .49, 0), .454, .014, 'metal', 'X')
        b.cyl('Axle socket', (x * 1.18, .49, 0), .073, .018, 'rubber', 'X', 12)
        for i in range(6):
            angle = math.tau * i / 6
            b.cyl('Reel cheek through bolt', (x * 1.20, .49 + .31 * math.cos(angle), .31 * math.sin(angle)),
                  .017, .018, 'metal', 'X', 6)
        for y in [.22, .37, .61, .76]:
            half = math.sqrt(max(0, .41 ** 2 - (y - .49) ** 2))
            b.cube('Reel timber board seam', (x * 1.17, y, 0), (.011, .012, half * 2), 'darkwood')
    b.cyl('Cable drum inner barrel', (0, .49, 0), .23, .46, 'darkwood', 'X', 12)
    for x in [-.17, -.085, 0, .085, .17]:
        b.ring('Wound recovered copper cable', (x, .49, 0), .323, .031, 'copper', 'X')
    pipe('Unwound cable end', [(.05, .20, -.29), (.08, .08, -.42), (.27, .035, -.52),
                             (.40, .035, -.46)], .024, 'copper')
    for z in [-.34, .34]:
        b.cube('Drum rolling chock', (0, .048, z), (.66, .096, .13), 'darkwood', .006)
    b.cube('Reel faded batch plate', (-.308, .62, -.13), (.012, .11, .19), 'cream')


def radiator_rack():
    # Salvaged car radiators stand in an open cradle. Closely spaced ribs read as
    # cooling fins at eye height, with visible rubber hose necks/metal end tanks.
    for x in [-.49, .49]:
        b.cube('Radiator cradle foot', (x, .043, 0), (.16, .085, .58), 'rust')
        b.beam('Open cooling-stock upright', (x, .07, .21), (x, .91, .21), .045, 'metal')
    for y in [.20, .78]:
        b.beam('Radiator rack horizontal', (-.49, y, .21), (.49, y, .21), .045, 'metal')
    for index, z in enumerate([-.20, .04]):
        y = .47 + index * .12
        b.cube('Radiator recessed core', (0, y, z), (.78, .52, .045), 'rubber')
        for x in [-.41, .41]:
            b.cube('Radiator side tank', (x, y, z), (.07, .62, .085), 'metal', .011)
        for i in range(14):
            b.cube('Radiator cooling fin', (-.36 + i * .055, y, z - .033), (.018, .51, .026),
                   'cream' if i % 5 == 0 else 'metal')
        for edge in [-.28, .28]:
            b.cube('Radiator end cross rail', (0, y + edge, z), (.86, .044, .085), 'rust')
        for x, side in [(-.37, 1), (.36, -1)]:
            b.cyl('Cut radiator hose stub', (x, y + side * .31, z), .034, .13, 'rubber', 'Y', 8)
    b.cube('Radiator dismantling tag', (.45, .70, -.28), (.13, .09, .012), 'ochre')


BUILDERS = {'CompactSalvageShell': shell, 'CompactWasherLot': washer_lot,
            'CompactCableReel': reel, 'CompactRadiatorRack': radiator_rack}


def ensure_meta(path):
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n')


def main():
    b.prepare_materials(write_atlas=False)
    b.stats = {}
    for name, builder in BUILDERS.items():
        obj = b.export(name, builder)
        points = [obj.matrix_world @ v.co for v in obj.data.vertices]
        b.stats[name]['bounds_unity'] = {
            'min': [min(v[a] for v in points) for a in [0, 2, 1]],
            'max': [max(v[a] for v in points) for a in [0, 2, 1]]}
        ensure_meta(OUT / (name + '.fbx'))
        assert b.stats[name]['triangles'] <= 3500, (name, 'source triangle budget')
    path = OUT / 'salvage_asset_manifest.json'
    path.write_text(json.dumps({'source': 'Original Blender-authored models; ArtSource/build_compact_salvage.py',
                                'pack': 'CompactRecognizableSalvage', 'units': 'metres',
                                'atlas': 'ScrapshiftPropAtlas.png',
                                'runtimeMaterial': 'ScrapshiftMaterials/PropAtlas',
                                'assets': b.stats}, indent=2) + '\n')
    ensure_meta(path)
    print('SCRAPSHIFT_SALVAGE_EXPORTED ' + json.dumps(b.stats))


if __name__ == '__main__':
    main()
