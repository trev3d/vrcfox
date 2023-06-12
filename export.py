relative_export_path = "//vrcfox unity project/Assets"
file_name = "vrcfox model.fbx"
desired_model_name = "Body"
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

# set master collection to active collection
export_layer_collection = bpy.context.view_layer.layer_collection.children[export_collection_name]
bpy.context.view_layer.active_layer_collection = export_layer_collection

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
colors_type="LINEAR",
use_armature_deform_only=True,
use_triangles=False,

)

bpy.ops.ed.undo_push()	
bpy.ops.ed.undo()