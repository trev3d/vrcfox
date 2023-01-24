relative_export_path = "//trevvr unity/Assets"
file_name = "trev avatar.gltf"
desired_model_name = "fox"
export_collection_name="master"
gamma = 2.23

import bpy
from mathutils import Color
from random import *

bpy.ops.object.select_all(action='DESELECT')

# combine all meshes into one
export_collection = bpy.data.collections['master']

for obj in export_collection.all_objects:
    obj.select_set(obj.type == "MESH")

bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]

bpy.ops.object.join()

# rename object
bpy.context.active_object.name = desired_model_name

# export
bpy.ops.export_scene.gltf(

    filepath=bpy.path.abspath(relative_export_path + "/" + file_name),
    check_existing=False,
    export_format='GLB',
    export_image_format='NONE',
    export_materials='NONE',
    export_colors=True,
    use_active_collection=True, 
    export_yup=True,
    export_apply=True,
    export_animations=True,
    export_morph=True,
    
    )
    
