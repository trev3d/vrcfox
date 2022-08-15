import bpy

export_collection = bpy.data.collections['master']

for obj in export_collection.all_objects:
	bpy.context.selected_objects += obj