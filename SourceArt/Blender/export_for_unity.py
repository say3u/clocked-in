"""Run with Blender: blender --background --python SourceArt/Blender/export_for_unity.py"""
import argparse
from pathlib import Path
import sys
import json
import re
import bpy

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--source', type=Path, default=root / 'SourceArt/Blender/HorrorGame_Office.blend')
parser.add_argument('--output', type=Path, default=root / 'Assets/HorrorGame_Office.fbx')
options = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
source = options.source.resolve()
output = options.output.resolve()
output.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
for image in bpy.data.images:
    texture = root / 'Assets/OfficeAssetPack/Textures' / Path(image.filepath.replace('\\', '/')).name
    if texture.is_file():
        image.filepath = str(texture)
    elif image.packed_file and image.source == 'FILE':
        # glTF imports (such as the elevator) may only have embedded images.
        # Extract those into Assets so the FBX also works on teammates' machines.
        packed = bytes(image.packed_file.data)
        extension = '.png' if packed.startswith(b'\x89PNG') else '.jpg' if packed.startswith(b'\xff\xd8') else Path(image.name).suffix or '.png'
        filename = re.sub(r'[^A-Za-z0-9_.-]+', '_', image.name)
        if not filename.lower().endswith(extension):
            filename += extension
        texture = root / 'Assets/OfficeAssetPack/Textures/ImportedMap' / filename
        texture.parent.mkdir(parents=True, exist_ok=True)
        texture.write_bytes(packed)
        image.filepath = str(texture)

# Keep a material description for mapping newly added Blender materials to URP.
# Existing material assets and their user edits are retained by the Unity importer.
materials = []
for material in bpy.data.materials:
    shader = material.node_tree.nodes.get('Principled BSDF') if material.use_nodes else None
    if shader is None:
        continue
    record = dict(name=material.name, color=list(shader.inputs['Base Color'].default_value),
                  metallic=shader.inputs['Metallic'].default_value,
                  smoothness=1-shader.inputs['Roughness'].default_value, baseTexture='')
    socket = shader.inputs['Base Color']
    if socket.is_linked:
        node = socket.links[0].from_node
        if node.type == 'TEX_IMAGE' and node.image:
            image_path = Path(bpy.path.abspath(node.image.filepath)).resolve()
            try:
                record['baseTexture'] = image_path.relative_to(root).as_posix()
            except ValueError:
                pass
    materials.append(record)
manifest = root / 'Tools/MapExport/materials.json'
manifest.parent.mkdir(parents=True, exist_ok=True)
manifest.write_text(json.dumps(dict(materials=materials), indent=2), encoding='utf-8')

# Match Unity 6000.6's own Unity-BlenderToFBX.py conversion settings. Keeping
# node names, modifiers and custom properties preserves existing scene references.
bpy.ops.export_scene.fbx(
    filepath=str(output),
    check_existing=False,
    use_selection=False,
    use_active_collection=False,
    object_types={'ARMATURE', 'CAMERA', 'LIGHT', 'MESH', 'OTHER', 'EMPTY'},
    use_mesh_modifiers=True,
    mesh_smooth_type='OFF',
    use_custom_props=True,
    bake_anim_use_nla_strips=False,
    bake_anim_use_all_actions=False,
    apply_scale_options='FBX_SCALE_ALL',
    path_mode='RELATIVE',
)
print('UNITY_FBX_EXPORTED', output, output.stat().st_size, flush=True)
