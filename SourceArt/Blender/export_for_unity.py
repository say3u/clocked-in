"""Run with Blender: blender --background --python SourceArt/Blender/export_for_unity.py"""
import argparse
from pathlib import Path
import sys
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
