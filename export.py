relative_export_path = "//trevvr unity/Assets"
file_name = "trev avatar.fbx"
desired_model_name = "fox"
export_collection_name="master"

import bpy
from mathutils import Color
from random import *

bpy.ops.object.select_all(action='DESELECT')

export_collection = bpy.data.collections['master']

for obj in export_collection.all_objects:
	if obj.type == "MESH":
		obj.select_set(True)
		bpy.context.view_layer.objects.active = obj
		
		for _, m in enumerate(obj.modifiers):
			if m.type != "ARMATURE":
				bpy.ops.object.modifier_apply(modifier=m.name)

bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]

# combine all meshes into one
bpy.ops.object.join()

# rename object
bpy.context.active_object.name = desired_model_name

# gamma correct vertex colors
# fbx exporter actually takes care of this oops.

#mesh = bpy.context.object.data
#colors = mesh.vertex_colors[0].data

#for v_col in colors:
#	
#	for i, item in enumerate(v_col.color):

#		v_col.color[i] = pow((v_col.color[i] + 0.055) / 1.055, 2.4)


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
	bake_anim_simplify_factor=0.0,
	colors_type="LINEAR"
	
	)
	
