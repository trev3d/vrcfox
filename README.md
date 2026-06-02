# vrcfox
a minimalistic furry avatar for vrchat

![avatar screenshot](screenshot.png)

## Get ⬇️

### [Use public test avatar >>>](https://vrchat.com/home/avatar/avtr_935bce4d-b613-4658-8f0c-057c62b4eb41)

### [Sketchfab preview >>>](https://sketchfab.com/3d-models/low-poly-fox-f46335a0a8064fb484712d2d228f3b14)

### [Download files >>>](https://github.com/cellomonster/vrcfox/releases/latest) 
(choose version and download 'source code' zip!)

## Strakacher's fork ⭐
A fork that comes with a body mesh, extra clothes meshes, and many other additional customization options! 
### [Fork repo >>>](https://github.com/strakacher21/vrcfox-2.3_body_and_cloth_edition)

## A request ⚠️
Given the license I can't legally stop you, but I'll still ask politely - please do not use this model for pornographic or suggestive content.

## Customization
The full project includes both a Blend 5.0 file and a Unity 2022.3.22f1 project.

The easiest way to customize the colors is with vertex painting rather than using a texture. This works well for solid colors but won't work for fancy patterns. Your avatar file size will stay tiny and quick to download without texture files. If you would rather use a texture, the model has a second set of UVs. You'll need to change each mesh's active UV layer to 'UVMap' and apply a texture yourself. You'll also want to erase all of the vertex colors as these will still appear on the default material in the Unity project!

To properly export a model from Blender to Unity, use the built-in Blender auto fbx export custom tool in Blender!
Click FBX Export in Blender’s 3D Viewport header (Workspace: Layout) to open the export popover.
Press Export FBX! to export to Unity in one click.

The Unity project contains an 'avatar setup base' prefab and two scenes for Quest and PC. Changes made to the prefab will propogate to both the PC and Quest verions of the avatar, while changes made to the Quest/PC scenes are platform specific. 

The Unity project also includes a script (attached to the avatar prefab) to easily customize facial expressions, player preferences, and facetracking features. You can disable some features to save on VRChat parameter budget or add your own blendshapes for expressions, body customization, clothing toggles, facetracking, etc.

## Attribution
AnimatorWizard script uses the [v3-animator-as-code](https://github.com/hai-vr/av3-animator-as-code) [(hai-vr)](https://github.com/hai-vr) package to set up animators.

Parameter Compressor is based on the ideas behind [LauraParamCompressor](https://github.com/LauraRozier/LauraParamCompressor) [(LauraRozier)](https://github.com/LauraRozier).

Also uses parts of [VRLabs Avatars 3.0 Manager](https://github.com/VRLabs/Avatars-3.0-Manager) [(AnimatorCloner)](https://github.com/VRLabs/Avatars-3.0-Manager/blob/main/Editor/AnimatorCloner.cs) to “reset” AnimatorWizard-generated FX/Gesture/Additive controllers and remove hidden garbage that accumulates in animator assets over time.

The automatic export script from Blender to Unity was taken from the [Blender-auto-fbx-export](https://github.com/strakacher21/Blender-auto-fbx-export) [(strakacher21)](https://github.com/strakacher21) repository.