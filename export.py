modelFilepath = "/Users/jt/source/trevir vrchat/Assets"
modelName = "trev avatar.fbx"

import bpy
from mathutils import Color
from random import *

gamma = 1 / 2.2

mesh = bpy.context.object.data
colors = mesh.vertex_colors[0].data

for vCol in colors:
	vCol.color[0] = pow(vCol.color[0], gamma)
	vCol.color[1] = pow(vCol.color[1], gamma)
	vCol.color[2] = pow(vCol.color[2], gamma)
	
