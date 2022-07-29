relativePath = "//project/Assets"
modelName = "trev avatar.fbx"

import bpy
from mathutils import Color
from random import *

gamma = 2.2

mesh = bpy.context.object.data
colors = mesh.vertex_colors[0].data

for vCol in colors:
	vCol.color[0] = pow(vCol.color[0], gamma)
	vCol.color[1] = pow(vCol.color[1], gamma)
	vCol.color[2] = pow(vCol.color[2], gamma)
	
bpy.ops.export_scene.fbx(

	filepath=bpy.path.abspath(relativePath + "/" + modelName),
	check_existing=False,
	use_active_collection=True,
	bake_space_transform=True, 
	object_types={'ARMATURE', 'MESH'}, 
	use_mesh_modifiers=False, 
	use_mesh_modifiers_render=False, 
	
	)