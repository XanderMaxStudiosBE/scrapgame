"""Render actual runtime static meshes plus tracked FBXs in Blender, never Unity evidence.

Reproduce (requires Mono, Blender 4.x and Pillow in system python3):
  SCRAPSHIFT_MONO_ROOT=/path/to/mono Tests/run-machinery-presentation.sh --export /tmp/scrapshift-machinery.json
  blender -b --python-exit-code 1 --python ArtSource/preview_reference_machinery.py -- --source-json /tmp/scrapshift-machinery.json

The adapter's authored envelope fixtures are excluded from the JSON mesh export.
Actual tracked FBXs are imported here, at the exported model hierarchy transforms.
Materials/lighting/fonts/ground/camera are approximate Blender presentation only.
"""
import argparse
import json
import math
from pathlib import Path
import subprocess
import sys
import tempfile

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
PROPS = ROOT / "Assets/Scrapshift/Resources/ScrapshiftProps"
parser = argparse.ArgumentParser()
parser.add_argument("--source-json", type=Path, required=True)
parser.add_argument("--output", type=Path, default=ROOT / "Design/Previews/ReferenceMachineryAssemblies.png")
args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
data = json.loads(args.source_json.read_text())
if "fixture meshes excluded" not in data["evidence"]:
    raise ValueError("Expected the labelled actual runtime source export, with authored fixture meshes excluded")

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
materials = {}


def texture_material(resource):
    if resource in materials:
        return materials[resource]
    name = resource.rsplit("/", 1)[-1]
    path = PROPS / "ScrapshiftPropAtlas.png" if name == "PropAtlas" else ROOT / "Assets/Scrapshift/Art/Textures" / (name + ".png")
    material = bpy.data.materials.new(resource + " / Blender approximation")
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Roughness"].default_value = .86
    texture = material.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = bpy.data.images.load(str(path), check_existing=True)
    texture.interpolation = "Linear"
    material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    materials[resource] = material
    return material


def convert(point):
    # Existing source art uses Blender (Unity X, Unity Z, Unity Y).
    return point[0], point[2], point[1]


def world_transform(marker):
    p = marker["position"]
    x, y, z = marker["basisX"], marker["basisY"], marker["basisZ"]
    # Conjugate the Unity hierarchy matrix with the same Y/Z conversion.
    return Matrix(((x[0], z[0], y[0], p[0]),
                   (x[2], z[2], y[2], p[2]),
                   (x[1], z[1], y[1], p[1]),
                   (0, 0, 0, 1)))


source_objects = []
for entry in data["meshes"]:
    mesh = bpy.data.meshes.new(entry["name"] + " / actual C# vertices")
    indices = entry["triangles"]
    # The Y/Z conversion reflects coordinates; reverse winding to retain the
    # actual runtime triangle-facing direction in Blender's coordinate system.
    faces = [(indices[i], indices[i + 2], indices[i + 1]) for i in range(0, len(indices), 3)]
    mesh.from_pydata([convert(v) for v in entry["vertices"]], [], faces)
    mesh.materials.append(texture_material(entry["material"]))
    uv = mesh.uv_layers.new(name="Actual runtime UV")
    for polygon in mesh.polygons:
        for loop_index in polygon.loop_indices:
            uv.data[loop_index].uv = entry["uv"][mesh.loops[loop_index].vertex_index]
    obj = bpy.data.objects.new(entry["name"], mesh)
    bpy.context.collection.objects.link(obj)
    obj["source_hierarchy"] = entry["name"]
    source_objects.append(obj)

authored_cache = {}
for marker in data["authoredMarkers"]:
    name = marker["name"]
    if name not in authored_cache:
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(PROPS / (name + ".fbx")))
        imported = set(bpy.data.objects) - before
        authored_cache[name] = []
        for obj in imported:
            if obj.type == "MESH":
                mesh = obj.data.copy()
                mesh.transform(obj.matrix_world)
                mesh.materials.clear()
                mesh.materials.append(texture_material("ScrapshiftMaterials/PropAtlas"))
                for polygon in mesh.polygons:
                    polygon.material_index = 0
                authored_cache[name].append(mesh)
        if not authored_cache[name]:
            raise ValueError("Tracked FBX supplied no mesh: " + name)
        for obj in imported:
            bpy.data.objects.remove(obj, do_unlink=True)
    for mesh in authored_cache[name]:
        obj = bpy.data.objects.new(marker["hierarchy"] + " / actual FBX", mesh)
        bpy.context.collection.objects.link(obj)
        obj["source_hierarchy"] = marker["hierarchy"]
        obj.matrix_world = world_transform(marker)
        source_objects.append(obj)

# The actual rasterized Unity font/sign plates are not exported by the adapter.
# This deliberately omits those font fixtures; the runtime mesh IN/OUT stencil,
# arrows and original FBX markings above are the actual source geometry.
bpy.ops.mesh.primitive_plane_add(size=140, location=(12, 0, -.025))
ground = bpy.context.object
ground.name = "Neutral Blender preview floor / not compact yard geometry"
ground.data.materials.append(texture_material("ScrapshiftMaterials/Gravel"))
for datum in ground.data.uv_layers.active.data:
    datum.uv *= 32

world = bpy.context.scene.world
world.use_nodes = True
world.node_tree.nodes.get("Background").inputs["Color"].default_value = (.35, .39, .43, 1)
world.node_tree.nodes.get("Background").inputs["Strength"].default_value = .65
bpy.ops.object.light_add(type="SUN", location=(0, -15, 20))
sun = bpy.context.object
sun.rotation_euler = (math.radians(23), math.radians(-18), math.radians(-26))
sun.data.energy = 2.1
sun.data.angle = math.radians(7)
sun.data.color = (1, .84, .66)
bpy.ops.object.light_add(type="AREA", location=(13, -15, 17))
fill = bpy.context.object
fill.data.energy = 1800
fill.data.size = 28
fill.data.color = (.74, .83, 1)

bpy.ops.object.camera_add()
camera = bpy.context.object
camera.data.type = "ORTHO"
scene = bpy.context.scene
scene.camera = camera
scene.render.engine = "CYCLES"
scene.cycles.samples = 20
scene.cycles.use_denoising = False
scene.render.resolution_x = 1040
scene.render.resolution_y = 660
scene.render.resolution_percentage = 100
scene.render.threads_mode = "FIXED"
scene.render.threads = 8
scene.view_settings.view_transform = "AgX"
scene.view_settings.look = "AgX - Medium High Contrast"

label_material = bpy.data.materials.new("Visible provenance / Blender annotation")
label_material.use_nodes = True
label_shader = label_material.node_tree.nodes.get("Principled BSDF")
label_shader.inputs["Base Color"].default_value = (.94, .89, .77, 1)
label_shader.inputs["Emission Color"].default_value = (.94, .89, .77, 1)
label_shader.inputs["Emission Strength"].default_value = 1
bpy.ops.object.text_add()
label = bpy.context.object
label.parent = camera
label.data.materials.append(label_material)
label.data.align_y = "TOP"
label.data.space_line = 1.1

panels = [
    ("MANUAL WORKBENCH / PLAYER HEIGHT", (-6.1, -14.7, 1.7), (-9, -11, 1.12), 32, "PERSP"),
    ("PURCHASED TIER 1 IN THROAT / PLAYER HEIGHT", (12.4, -6.9, 1.7), (9, -11, 1.10), 30, "PERSP"),
    ("PRIMARY WHOLE-SCRAP / EXPORT", (16, -3, 10), (4.5, 11, 1.7), 19, "ORTHO"),
    ("PLAYER ROUTE / PHYSICAL IN-OUT", (40, -4, 1.7), (28, -4, .85), 24, "PERSP"),
]
args.output.parent.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(prefix="scrapshift-source-panels-") as directory:
    images = []
    for index, (title, position, target, scale, projection) in enumerate(panels):
        camera.location = position
        camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()
        camera.data.type = projection
        if projection == "ORTHO":
            camera.data.ortho_scale = scale
            label_width = scale
        else:
            camera.data.lens = scale
            label_width = camera.data.sensor_width / scale
        label.location = (-label_width * .48, label_width * .306, -1)
        label.data.size = label_width * .0105
        label.data.body = title + "\nACTUAL RUNTIME C# MESHES + TRACKED FBX / BLENDER PREVIEW\nNOT UNITY GAMEPLAY / APPROXIMATE MATERIALS, LIGHTING & GROUND"
        # Keep unrelated showcase rows out of each camera's foreground.
        prefix = "Actual runtime source mesh preview/Actual connected source line/"
        for obj in source_objects:
            hierarchy = obj["source_hierarchy"]
            connected = hierarchy.startswith(prefix)
            obj.hide_render = (index == 3) != connected
            if index != 3:
                # Meshes are already exported in world space; per-root names are
                # a stable way to select the actual source showcase, without moving it.
                allowed = ("Workbench",) if index == 0 else ("Tier1Scrapper",) if index == 1 else ("PrimaryScrapper", "ExportStation")
                root_name = hierarchy.split("/")[1] if "/" in hierarchy else ""
                obj.hide_render = obj.hide_render or root_name not in allowed
        image_path = Path(directory) / (str(index) + ".png")
        scene.render.filepath = str(image_path)
        bpy.ops.render.render(write_still=True)
        images.append(str(image_path))
    # Pillow is isolated to montage assembly; it does not alter source geometry.
    montage_code = "from PIL import Image; import sys; frames=[Image.open(p).convert('RGB') for p in sys.argv[2:]]; w,h=frames[0].size; out=Image.new('RGB',(w*2,h*2)); [out.paste(im,((i%2)*w,(i//2)*h)) for i,im in enumerate(frames)]; out.save(sys.argv[1])"
    subprocess.run(["python3", "-c", montage_code, str(args.output), *images], check=True)
print("SCRAPSHIFT_REFERENCE_MACHINERY_PREVIEW " + str(args.output) + " — actual source geometry; not Unity gameplay")
