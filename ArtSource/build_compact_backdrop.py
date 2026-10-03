"""Original static industrial neighbours for SCRAPSHIFT's compact yard.

blender -b --python-exit-code 1 --python ArtSource/build_compact_backdrop.py
New independent files only. Existing atlas/model/material bytes are not regenerated.
Coordinates are metres, Unity X/right Y/up Z/north; source Blender uses Z/up.
"""
import bpy, json, math, random, sys, uuid
from pathlib import Path
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_yard_assets as b

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Scrapshift/Resources/ScrapshiftProps'


def panel(name, points, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([b.xyz(p) for p in points], [], [tuple(range(len(points)))])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return b.add(obj, material)


def wall_tiles(width, depth, height):
    # Separate face islands prevent bricks stretching across whole facades.
    # The recovered original WorldAtlas soil island is a brick/mortar texture.
    nx, nz, ny = math.ceil(width/2), math.ceil(depth/2), math.ceil(height)
    for z in [-depth/2, depth/2]:
        for i in range(nx):
            left, right = -width/2 + i*width/nx, -width/2 + (i+1)*width/nx
            for j in range(ny):
                low, high = j*height/ny, (j+1)*height/ny
                points = [(left,low,z),(right,low,z),(right,high,z),(left,high,z)]
                panel('Brick courses', points if z<0 else points[::-1], 'soil')
    for x in [-width/2, width/2]:
        for i in range(nz):
            near, far = -depth/2 + i*depth/nz, -depth/2 + (i+1)*depth/nz
            for j in range(ny):
                low, high = j*height/ny, (j+1)*height/ny
                points = [(x,low,near),(x,low,far),(x,high,far),(x,high,near)]
                panel('Brick end courses', points if x>0 else points[::-1], 'soil')
    b.cube('Deep brick plinth', (0,-.11,0), (width+.12,.48,depth+.12), 'metal')
    for z in [-depth/2-.04,depth/2+.04]:
        b.cube('Weathered wall coping', (0,height-.07,z), (width+.14,.18,.12), 'cream')


def industrial_window(x, y, z, width=1.6, height=1.45):
    b.cube('Dusty multi-pane glazing', (x,y,z), (width,height,.055), 'glass')
    for dx in [-width/2,0,width/2]:
        b.cube('Old steel window bars', (x+dx,y,z-.045), (.055,height+.12,.06), 'cream')
    for dy in [-height/2,0,height/2]:
        b.cube('Old window transom', (x,y+dy,z-.045), (width+.12,.055,.065), 'cream')
    b.cube('Projecting stone sill', (x,y-height/2-.07,z-.045), (width+.22,.12,.27), 'metal')


def shutter(x, z, width=3, height=3.3, material='blue'):
    b.cube('Recessed loading bay', (x,height/2,z), (width+.24,height+.12,.10), 'rubber')
    b.cube('Roller shutter', (x,height/2,z-.06), (width,height,.065), material)
    for i in range(11):
        b.cube('Shutter horizontal ribs', (x,.13+i*(height-.26)/10,z-.105), (width-.08,.035,.04), 'metal')
    for dx in [-width/2-.1,width/2+.1]:
        b.cube('Door brick surround', (x+dx,height/2,z-.12), (.16,height+.2,.14), 'cream')
    b.cube('Shutter lintel', (x,height+.05,z-.13), (width+.42,.17,.25), 'metal')
    b.cube('Loading threshold', (x,.065,z-.35), (width+.5,.13,.8), 'metal')


def painted_lettering(text, x, y, z, cell=.06):
    # Original small stencilled glyphs, baked into the shared mesh. No font asset,
    # new material, additional renderer or runtime text callback is required.
    glyphs={'M':['101','111','111','101','101'],'E':['111','100','110','100','111'],
            'T':['111','010','010','010','010'],'A':['010','101','111','101','101'],
            'L':['100','100','100','100','111'],'W':['101','101','111','111','101'],
            'R':['110','101','110','101','101'],'K':['101','101','110','101','101']}
    left=x-(len(text)*4-1)*cell/2
    for column,letter in enumerate(text):
        for row,bits in enumerate(glyphs[letter]):
            for i,bit in enumerate(bits):
                if bit=='0':continue
                px=left+(column*4+i)*cell;py=y+(2-row)*cell
                w=cell*.84
                panel('Worn painted workshop lettering',[(px,py-w/2,z),(px+w,py-w/2,z),(px+w,py+w/2,z),(px,py+w/2,z)],'darkwood')


def brick_chimney():
    # Metre-high face islands retain brick scale up a tall chimney as well.
    vertices=[]
    for j in range(12):
        y=-.35+j*10.7/11
        for i in range(12):
            angle=i*math.tau/12
            vertices.append(b.xyz((-5.4+math.cos(angle)*.40,y,2.3+math.sin(angle)*.40)))
    faces=[tuple(reversed(range(12)))]
    for j in range(11):
        for i in range(12):faces.append((j*12+i,j*12+(i+1)%12,(j+1)*12+(i+1)%12,(j+1)*12+i))
    mesh=bpy.data.meshes.new('Course-mapped brick chimney');mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new('Workshop furnace chimney',mesh);bpy.context.collection.objects.link(obj);b.add(obj,'soil')


def brick_workshop():
    width, depth, eaves, ridge = 18, 8, 4.8, 6.6
    wall_tiles(width,depth,eaves)
    for x in [-width/2,width/2]:
        points=[(x,eaves,-depth/2),(x,eaves,depth/2),(x,ridge,0)]
        panel('Workshop brick gable', points if x>0 else points[::-1], 'soil')
        b.cube('Gable roof vent', (x+( .025 if x>0 else -.025),5.4,0), (.06,.64,1.25), 'metal')
    for side in [-1,1]:
        # Large sheets carry a visible silhouette; seams add restrained roof rhythm.
        points=[(-9.35,4.78,side*4.45),(9.35,4.78,side*4.45),(9.35,6.65,0),(-9.35,6.65,0)]
        panel('Pitched galvanised roof', points if side<0 else points[::-1], 'metal')
        for i in range(13):
            x=-9.3+i*1.55
            b.beam('Roof sheet folded seam',(x,4.805,side*4.45),(x,6.67,0),.045,'blue')
        b.cube('Eaves gutter',(0,4.74,side*4.45),(18.8,.13,.13),'rust')
    b.cube('Ridge flashing',(0,6.66,0),(18.8,.12,.22),'metal')
    for x in [-7,-4.4,4.5,7.1]:
        industrial_window(x,2.45,-4.055,1.7,1.65)
    shutter(0,-4.08,3.8,3.4,'sage')
    b.cube('Weathered industrial sign frame',(0,4.15,-4.13),(4.65,.59,.095),'metal')
    b.cube('Faded painted sign plate',(0,4.15,-4.19),(4.45,.43,.035),'cream')
    painted_lettering('METAALWERK',0,4.15,-4.21)
    for x in [-8.45,8.45]:
        b.cyl('Cast-iron downpipe',(x,2.25,-4.22),.066,4.5,'rust',verts=8)
        b.beam('Bent drain elbow',(x,.13,-4.22),(x,.13,-4.64),.12,'rust')
    b.cube('Lean-to roof',(6.2,2.3,-4.8),(3.3,.13,1.4),'blue')
    for x in [4.72,7.68]:
        b.beam('Canopy bracket',(x,2.24,-5.45),(x,1.64,-4.10),.075,'metal')
    # Squat furnace chimney gives an industrial landmark without giant smokestacks.
    brick_chimney()
    for y in [6.9,8.7,10.35]:b.cyl('Chimney iron band',(-5.4,y,2.3),.43,.09,'metal',verts=12)
    b.cyl('Chimney coping',(-5.4,10.39,2.3),.48,.20,'cream',verts=12)
    b.cube('Roof ventilator',(4.6,5.95,1.5),(1.6,.48,1.1),'metal')
    for i in range(5):b.cube('Ventilator slats',(4.6,5.80+i*.08, .93),(1.45,.025,.04),'rubber')


def sawtooth_works():
    width, depth, eaves = 14, 10, 5.2
    wall_tiles(width,depth,eaves)
    # Three real sawtooth skylight bays, not a flat generic roof block.
    for bay in range(3):
        near=-5.2+bay*3.48;far=near+3.48
        points=[(-7.25,5.23,near),(7.25,5.23,near),(7.25,7.17,far),(-7.25,7.17,far)]
        panel('Factory pitched roof sheet',points,'blue')
        points=[(-7.25,5.23,far),(7.25,5.23,far),(7.25,7.17,far),(-7.25,7.17,far)]
        panel('Sawtooth northern glazing',points[::-1],'glass')
        for x in [-7.25,7.25]:
            points=[(x,5.2,near),(x,5.2,far),(x,7.17,far)]
            panel('Sawtooth roof end',points if x>0 else points[::-1],'metal')
        for i in range(10):
            x=-7.15+i*1.59
            b.beam('Raised standing roof seam',(x,5.27,near),(x,7.21,far),.04,'metal')
            b.cube('Skylight mullion',(x,6.2,far-.035),(.065,1.96,.09),'cream')
        b.cube('Sawtooth ridge cap',(0,7.20,far),(14.65,.12,.2),'metal')
    for x in [-5,-2.3,4.7]:industrial_window(x,3.25,-5.055,1.7,1.6)
    shutter(1.25,-5.08,2.8,3.45,'blue')
    b.cube('Service door',(-5,.97,-5.10),(1.08,1.94,.08),'sage')
    b.cube('Service door handle',(-4.60,.98,-5.17),(.055,.22,.08),'metal')
    for x in [-7.12,7.12]:
        b.cyl('Factory downpipe',(x,2.50,-5.20),.07,5,'metal',verts=8)
    b.cube('Old roof duct',(5.4,7.5,3.6),(.86,.75,.86),'rust')
    b.cube('Vent hood',(5.4,7.91,3.6),(1.13,.12,1.13),'metal')
    b.cube('Loading platform',(1.25,.15,-5.6),(4,.30,1.3),'metal')


def leaf_spray(rng, origin, size, tone):
    # Small irregular leafy volumes remain visible from eye height. Four pointed
    # sprays break the edges, while gaps between branch tips show branches/sky.
    # No transparent shader, icosphere or giant single canopy primitive.
    ox,oy,oz=origin
    vertices=[]
    for y,radius in [(-.56,.58),(.28,.86)]:
        for i in range(8):
            angle=math.tau*i/8
            radius_i=radius*rng.uniform(.78,1.12)*size
            vertices.append(b.xyz((ox+math.cos(angle)*radius_i,oy+(y+rng.uniform(-.07,.07))*size,oz+math.sin(angle)*radius_i*.84)))
    vertices.append(b.xyz((ox+.16*size,oy+.80*size,oz-.10*size)))
    faces=[tuple(reversed(range(8)))]
    faces += [(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
    faces += [(8+i,8+(i+1)%8,16) for i in range(8)]
    mesh=bpy.data.meshes.new('Irregular leafy branch volume');mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new('Compact angular leaf mass',mesh);bpy.context.collection.objects.link(obj);b.add(obj,tone)
    for i in range(4):
        angle=rng.random()*math.tau
        x=ox+math.cos(angle)*size*.64;z=oz+math.sin(angle)*size*.64
        y=oy+rng.uniform(-.38,.38)*size
        length=size*rng.uniform(.38,.63);breadth=length*.46
        along=Vector((math.cos(angle),0,math.sin(angle)))
        cross=Vector((-math.sin(angle),0,math.cos(angle)))
        center=Vector((x,y,z))
        points=[tuple(center-along*length),tuple(center-cross*breadth+Vector((0,.08*size,0))),tuple(center+along*length),tuple(center+cross*breadth+Vector((0,.08*size,0)))]
        panel('Angular textured leaf fan',points,tone)
        panel('Leaf fan back',points[::-1],tone)


def tree_group():
    rng=random.Random(87531)
    # Irregular connected grove: five forked trees plus low brambles, with three
    # crown tones and no single isolated sphere silhouette.
    for tree,(x,z,height) in enumerate([(-4.5,.25,8.8),(-2.25,-.40,7.4),(0,.25,9.6),(2.8,-.55,8.2),(4.8,.55,7.5)]):
        b.cyl('Grove trunk',(x,height*.27,z),.14+tree%2*.035,height*.54+.22,'darkwood',verts=7)
        b.beam('Exposed root',(x,.10,z),(x+.65,-.14,z+.35),.10,'darkwood')
        for branch in range(5):
            angle=branch*2.35+tree*.65
            spread=1.0+branch%2*.38
            end=(x+math.cos(angle)*spread,height*(.58+branch*.068),z+math.sin(angle)*spread*.75)
            b.beam('Visible forked branch',(x,height*.38,z),end,.095 if branch<3 else .065,'darkwood')
            leaf_spray(rng,end,1.25 if branch<4 else .95,'leaflight' if (branch+tree)%4==0 else 'leaf')
        leaf_spray(rng,(x,height-.65,z),1.25,'leaf')
    for i in range(13):
        x=-5.7+i*.95;z=-.40+math.sin(i*2.1)*.65
        b.beam('Bramble woody stalk',(x,-.12,z),(x+.25,.92,z),.055,'darkwood')
        leaf_spray(rng,(x,.74,z),.67,'leaflight' if i%3==0 else 'leaf')


def utility_pole():
    b.cyl('Weathered telegraph pole',(0,3.66,0),.14,7.62,'darkwood',verts=10)
    b.cube('Pole steel base shoe',(0,.27,0),(.34,.70,.34),'metal')
    b.cube('Cross arm',(0,7.32,0),(.15,.16,2.15),'wood')
    for z in [-.65,0,.65]:
        b.cyl('Porcelain wire insulator',(0,7.55,z),.09,.23,'cream',verts=8)
        b.cyl('Insulator lip',(0,7.57,z),.12,.07,'ivory',verts=8)
    for z in [-.85,.85]:b.beam('Crossarm brace',(0,6.7,0),(0,7.3,z),.06,'metal')
    b.cube('Pole junction box',(.21,2.15,0),(.30,.45,.32),'sage')
    b.cyl('Junction cable',(.24,1.13,0),.023,1.9,'rubber',verts=6)
    for y in [1.65,2.8,4.1,5.4]:b.cube('Pole clamp',(0,y,0),(.29,.04,.29),'metal')


def overhead_span():
    # 23m span at authored pole height; three simple catenary-like arcs.
    for z in [-.65,0,.65]:
        points=[]
        for i in range(13):
            t=i/12
            points.append((23*t,7.65-.9*4*t*(1-t),z))
        for a,c in zip(points,points[1:]):b.beam('Sagging overhead utility cable',a,c,.028,'rubber')


def layout():
    placements=[]
    def add(name,model,sector,x,z,yaw=0,y=0,scale=1):
        placements.append(dict(name=name,model=model,sector=sector,x=x,y=y,z=z,yaw=yaw,scale=scale))
    add('North brick repair works','BackdropBrickWorkshop',0,-9,31)
    add('North sawtooth workshops','BackdropSawtoothWorks',0,14,33)
    add('West brick workshop','BackdropBrickWorkshop',1,-35,6,-90,scale=.95)
    add('East engineering works','BackdropSawtoothWorks',2,35.5,5,90,scale=.95)
    for name,x,z,sector in [('West yard containers',-34,-10,1),('East yard containers',35.5,-10,2),('North container stock',-24,31,0)]:
        add(name,'TealContainer',sector,x,z,90)
    add('East stacked container','TealContainer',2,35.5,-10,90,y=2.95)
    for i,x in enumerate([-23,0,23]):add('Northern utility pole '+str(i),'BackdropUtilityPole',0,x,25)
    for i,x in enumerate([-23,0]):add('Northern overhead cables '+str(i),'BackdropOverheadSpan',0,x,25)
    for i,(x,z,yaw,scale) in enumerate([(-29,39,12,1),(-10,40,0,.96),(9,41,7,1.04),(29,40,-9,.95)]):
        add('North connected woodline '+str(i),'BackdropTreeGroup',3,x,z,yaw,scale=scale)
    for i,(x,z,yaw,scale) in enumerate([(-42,0,90,.90),(-42,15,90,1),(-30,19,0,.78)]):
        add('West layered grove '+str(i),'BackdropTreeGroup',4,x,z,yaw,scale=scale)
    for i,(x,z,yaw,scale) in enumerate([(44,-3,90,.88),(43,14,90,.95),(31,20,0,.78)]):
        add('East layered grove '+str(i),'BackdropTreeGroup',5,x,z,yaw,scale=scale)
    return {'version':1,'provenance':'Original neighbouring scenery authored for this project; placement excludes the playable 48x36m yard and south road. Buildings are decorative and stock is not recoverable inventory.','placements':placements}


def metadata(path):
    target=Path(str(path)+'.meta')
    if not target.exists():target.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')


def main():
    b.prepare_materials(write_atlas=False)
    b.atlasmat.node_tree.nodes.get('Image Texture').image=bpy.data.images.load(str(ROOT/'Assets/Scrapshift/Resources/ScrapshiftWorld/WorldAtlas.png'))
    b.stats={}
    for name,builder in [('BackdropBrickWorkshop',brick_workshop),('BackdropSawtoothWorks',sawtooth_works),('BackdropTreeGroup',tree_group),('BackdropUtilityPole',utility_pole),('BackdropOverheadSpan',overhead_span)]:
        obj=b.export(name,builder)
        corners=[obj.matrix_world@v.co for v in obj.data.vertices]
        # Bounds in Unity axes for layout audits, including non-centred cable pivots.
        b.stats[name]['bounds_unity']={
            'min':[min(v[a] for v in corners) for a in [0,2,1]],
            'max':[max(v[a] for v in corners) for a in [0,2,1]]}
        metadata(OUT/(name+'.fbx'))
    manifest={'source':'Original Blender-authored geometry; ArtSource/build_compact_backdrop.py','pack':'CompactIndustrialBackdrop','units':'metres','atlas':'ScrapshiftWorld/WorldAtlas.png','runtimeMaterial':'ScrapshiftWorld/WorldProps','assets':b.stats}
    for filename,content in [('backdrop_asset_manifest.json',manifest),('compact_backdrop_layout.json',layout())]:
        path=OUT/filename;path.write_text(json.dumps(content,indent=2)+'\n');metadata(path)
    print('SCRAPSHIFT_BACKDROP_EXPORTED '+json.dumps(b.stats))


if __name__=='__main__':main()
