# vrcfox
a bare-bones furry avatar for VRChat featuring basic body & color customization and face tracking

(todo, record facetracking video!)

![Screenshot_4](https://github.com/cellomonster/vrcfox/assets/32079637/ac0921cf-05ab-407d-bc21-43188dd42ca3)

## Modifying

The easiest way to customize the model's colors is with vertex painting. This works well for solid colors but won't work for fancy patterns. However, your avatar will be tiny and load super fast since there is no large texture file. If you would rather use a texture, the model has a second set of UVs but you'll need to change each mesh's active UV layer to 'UVMap' and apply a textured material yourself. You'll also want to erase all of the vertex colors as these will still appear on the default material in the Unity project!

A script to quickly export the model to Unity with all the correct settings is included and is visible at the bottom of the window when you open the project. Clicking the '▶' button will export the model to Unity.

![export](https://github.com/cellomonster/vrcfox/assets/32079637/be38158c-5d4d-4c26-9fee-7168ec719684)

The Unity project contains an 'avatar setup base' prefab and two scenes for Quest and PC. Changes made to the prefab will propogate to both the PC and Quest verions of the avatar, while changes made to the Quest/PC scenes will be platform specific. 

The Unity project also includes a script to easily customize expressions but it contains a lot of hard-coded values and isn't meant to be user-friendly YET. If you make any manual changes to the animators you should **NEVER** press the 'Setup Animator!' button as this will **destroy your work!!!**

## TODO:
- [ ] Add body geometry. Right now the only thing under the coat is air!
- [X] Revise visemes and expression
- [X] Face tracking support
- [ ] Limited outfit customization (something better than that drab grey coat!)
- [X] UVs for textures
- [ ] VRM file

## Attribution
- I'm using [hai-vr's av3-animator-as-code package](https://github.com/hai-vr/av3-animator-as-code) to set up animators
