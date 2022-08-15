relative_export_path = "//project/Assets"
file_name = "trev avatar.fbx"
desired_model_name = "fox"
export_collection_name="master"
gamma = 2.2

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

# gamma correct vertex colors
mesh = bpy.context.object.data
colors = mesh.vertex_colors[0].data

for v_col in colors:
	v_col.color[0] = pow(v_col.color[0], gamma)
	v_col.color[1] = pow(v_col.color[1], gamma)
	v_col.color[2] = pow(v_col.color[2], gamma)


# export
bpy.ops.export_scene.fbx(

	filepath=bpy.path.abspath(relative_export_path + "/" + file_name),
	check_existing=False,
	use_active_collection=True,
	bake_space_transform=True, 
	object_types={'ARMATURE', 'MESH'}, 
	use_mesh_modifiers=False, 
	use_mesh_modifiers_render=False, 
	bake_anim_use_all_bones=False,
	bake_anim_force_startend_keying=False,
	bake_anim_simplify_factor=0.0
	
	)