# vrcfox
a tiny bare-bones VRChat avatar featuring color customization and face tracking

## [Download >>>](https://github.com/cellomonster/vrcfox/releases/latest) (choose version and download 'source code' zip!)

## [Sketchfab preview](https://sketchfab.com/3d-models/vrcfox-9ed90de72e9c437b8820cbf0eeb32a50)

![Screenshot_3](https://github.com/cellomonster/vrcfox/assets/32079637/9d5b7d82-6fe0-44bb-9798-a9bc0a6ca2ae)


## Modifying

The full project includes both a Blend file and a Unity project.

The blend file requires Blender 3.6 or later

The easiest way to customize the colors is with vertex painting rather than using a texture. This works well for solid colors but won't work for fancy patterns. Your avatar file size will stay tiny and quick to download without texture files. If you would rather use a texture, the model has a second set of UVs. You'll need to change each mesh's active UV layer to 'UVMap' and apply a texture yourself. You'll also want to erase all of the vertex colors as these will still appear on the default material in the Unity project!

A script to easily export the model to Unity is included in the Blender file. It is visible at the bottom of the window when you open the project. Clicking the '▶' button will export the model to Unity.

The Unity project contains an 'avatar setup base' prefab and two scenes for Quest and PC. Changes made to the prefab will propogate to both the PC and Quest verions of the avatar, while changes made to the Quest/PC scenes are platform specific. 

The Unity project also includes a script (attached to the avatar prefab) to easily customize facial expressions, player preferences, and facetracking features. You can disable some features to save on VRChat parameter budget or add your own blendshapes for expressions, body customization, clothing toggles, facetracking, etc.

## TODO:
- [X] User friendly animator wizard
- [ ] Add body geometry. Right now the only thing under the coat is air!
- [X] Revise visemes and expression
- [X] Face tracking support
- [ ] Limited outfit customization (something better than that drab grey coat!)
- [X] UVs for textures
- [ ] VRM file?

## Attribution
- I'm using [hai-vr's av3-animator-as-code package](https://github.com/hai-vr/av3-animator-as-code) to set up animators
