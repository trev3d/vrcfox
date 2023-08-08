# vrcfox
a tiny bare-bones VRChat avatar featuring color customization and face tracking

(todo, record facetracking video!)

### [Sketchfab preview](https://sketchfab.com/3d-models/vrcfox-9ed90de72e9c437b8820cbf0eeb32a50)

<img width="1440" alt="sc" src="https://github.com/cellomonster/vrcfox/assets/32079637/32ec5ee5-2350-4acf-8677-eb2edef943a3">


## Modifying

The full project includes both a Blend file and a Unity project.

The blend file requires Blender 3.6 or later

The easiest way to customize the model's colors is with vertex painting. This works well for solid colors but won't work for fancy patterns. Your avatar download size will be tiny without texture files. If you would rather use a texture, the model has a second set of UVs. You'll need to change each mesh's active UV layer to 'UVMap' and apply a textured material yourself. You'll also want to erase all of the vertex colors as these will still appear on the default material in the Unity project!

A script to easily export the model to Unity is included in the Blender file. It is visible at the bottom of the window when you open the project. Clicking the '▶' button will export the model to Unity.

![export](https://github.com/cellomonster/vrcfox/assets/32079637/be38158c-5d4d-4c26-9fee-7168ec719684)

The Unity project contains an 'avatar setup base' prefab and two scenes for Quest and PC. Changes made to the prefab will propogate to both the PC and Quest verions of the avatar, while changes made to the Quest/PC scenes are platform specific. 

The Unity project also includes a script to easily customize expression and facetracking blendshapes but it isn't yet user-friendly. If you make any manual changes to the animators you should **NEVER** press the 'Setup Animator!' button as this will **destroy your work!!!**

## TODO:
- [ ] User friendly animator wizard (WIP)
- [ ] Add body geometry. Right now the only thing under the coat is air!
- [X] Revise visemes and expression
- [X] Face tracking support
- [ ] Limited outfit customization (something better than that drab grey coat!)
- [X] UVs for textures
- [ ] VRM file?

## Attribution
- I'm using [hai-vr's av3-animator-as-code package](https://github.com/hai-vr/av3-animator-as-code) to set up animators
