#if UNITY_EDITOR
using AnimatorAsCode.V0;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Collections.Generic;

public class VrcFoxAnimatorWizard : MonoBehaviour
{
	public VRCAvatarDescriptor avatar;
	public SkinnedMeshRenderer skin;
	public AnimatorController assetContainer;
	public string assetKey;

	public AvatarMask gestureMask;
	public AvatarMask lMask;
	public AvatarMask rMask;
	public Motion[] handMotions;

	public AvatarMask fxMask;

	public Motion primaryColor0;
	public Motion primaryColor1;

	public Motion secondColor0;
	public Motion secondColor1;

	public string[] mouthGestureExpressions;
	public string[] browGestureExpressions;

	public string[] faceTrackingFloatShapeNames =
	{
		"JawOpen",
		"MouthClosed",
		"MouthSadLeft",
		"MouthUpperUpLeft",
		"MouthLowerDownLeft",
		"BrowLowererLeft",
		"BrowPinchLeft",
		"BrowInnerUpLeft",
		"BrowOuterUpLeft",
		"MouthCornerPullLeft",
		"MouthStretchLeft",
		"MouthTightener",
	};
}

[CustomEditor(typeof(VrcFoxAnimatorWizard), true)]

public class AnimatorGeneratorEditor : Editor
{
	private readonly string[] _leftRight = { "Left", "Right" };
	private const string SystemName = "vrcfox";
	private const float TransitionSpeed = 0.05f;

	public override void OnInspectorGUI()
	{
		if (GUILayout.Button("Setup animator!"))
			Create();

		DrawDefaultInspector();
	}

	private void Create()
	{
		var my = (VrcFoxAnimatorWizard)target;

		var aac = AacV0.Create(new AacConfiguration
		{
			SystemName = SystemName,
			AvatarDescriptor = my.avatar,
			AnimatorRoot = my.avatar.transform,
			DefaultValueRoot = my.avatar.transform,
			AssetContainer = my.assetContainer,
			AssetKey = my.assetKey,
			DefaultsProvider = new AacDefaultsProvider(false)
		});

		aac.ClearPreviousAssets();

		// hand gestures
		{
			aac.CreateMainGestureLayer().WithAvatarMask(my.gestureMask);
			foreach (string side in _leftRight)
			{
				var layer = aac.CreateSupportingGestureLayer(side + " hand")
					.WithAvatarMask(side == "Left" ? my.lMask : my.rMask);

				var gesture = layer.IntParameter("Gesture" + side);

				for (int i = 0; i < my.handMotions.Length; i++)
				{
					Motion motion = my.handMotions[i];

					var state = layer.NewState(motion.name, 1, i)
						.WithAnimation(motion);

					layer.EntryTransitionsTo(state)
						.When(gesture.IsEqualTo(i));
					state.Exits()
						.WithTransitionDurationSeconds(TransitionSpeed)
						.When(gesture.IsNotEqualTo(i));
				}
			}
		}

		aac.CreateMainFxLayer().WithAvatarMask(my.fxMask);

		var vrcParams = new List<VRCExpressionParameters.Parameter>();

		AacFlBoolParameter faceTrackingActiveParam;
		AacFlFloatParameter blendFaceTrackingParam;

		// face tracking vs default animation control 
		{
			string prefix = "prefs/";
			
			var layer = aac.CreateSupportingFxLayer("face animations toggle").WithAvatarMask(my.fxMask);
			
			faceTrackingActiveParam
				= CreateBoolParam(layer, vrcParams, prefix + "FaceTrackingActive", true, false);

			blendFaceTrackingParam = layer.FloatParameter("BlendFaceTracking");

			var offState = layer.NewState("face tracking off")
				.Drives(blendFaceTrackingParam, 0);
				// .WithAnimation(my.enableVrcNativeEyelids);
			var offControl = offState.State.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
			offControl.trackingEyes = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;
			offControl.trackingMouth = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;

			var onState = layer.NewState("face tracking on")
				.Drives(blendFaceTrackingParam, 1);
				//.WithAnimation(my.disableVrcNativeEyelids);
			var onControl = onState.State.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
			onControl.trackingEyes = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
			onControl.trackingMouth = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;

			layer.AnyTransitionsTo(onState).WithTransitionToSelf().When(faceTrackingActiveParam.IsTrue());
			layer.AnyTransitionsTo(offState).When(faceTrackingActiveParam.IsFalse());
		}

		// create fx tree
		var fxTreeLayer = aac.CreateSupportingFxLayer("tree").WithAvatarMask(my.fxMask);

		fxTreeLayer.OverrideValue(fxTreeLayer.FloatParameter("Blend"), 1);

		var masterTree = aac.NewBlendTreeAsRaw();
		masterTree.name = "master tree";
		masterTree.blendType = BlendTreeType.Direct;

		fxTreeLayer.NewState(masterTree.name).WithAnimation(masterTree).WithWriteDefaultsSetTo(true);

		// brow gesture expressions
		{
			var prefix = "exp/brows/";
			var expressions = my.browGestureExpressions;
			var layer = aac.CreateSupportingFxLayer("brow gestures").WithAvatarMask(my.fxMask);
			var gesture = layer.IntParameter("GestureLeft");

			List<string> allPossibleExpressions = new List<string>();

			foreach ( var shapeName in expressions)
			{
				if(!allPossibleExpressions.Contains(shapeName))
					allPossibleExpressions.Add(shapeName);
			}

			for (int i = 0; i < expressions.Length; i++)
			{
				var clip = aac.NewClip();

				foreach(var shapeName in  expressions)
				{
					clip.BlendShape(my.skin, prefix + shapeName, shapeName == expressions[i] ? 100 : 0);
				}

				var state = layer.NewState(expressions[i], 1, i)
					.WithAnimation(clip);

				var enter = layer.EntryTransitionsTo(state)
					.When(gesture.IsEqualTo(i));
				var exit = state.Exits()
					.WithTransitionDurationSeconds(TransitionSpeed)
					.When(gesture.IsNotEqualTo(i));

				if (i == 0)
				{
					enter.Or().When(faceTrackingActiveParam.IsTrue());
					exit.And(faceTrackingActiveParam.IsFalse());
				} else
				{
					enter.And(faceTrackingActiveParam.IsFalse());
					exit.Or().When(faceTrackingActiveParam.IsTrue());
				}
			}
		}

		// mouth gesture expressions
		{
			var prefix = "exp/mouth/";
			var expressions = my.mouthGestureExpressions;
			var layer = aac.CreateSupportingFxLayer("mouth gestures").WithAvatarMask(my.fxMask);
			var gesture = layer.IntParameter("GestureRight");

			List<string> allPossibleExpressions = new List<string>();

			foreach (var shapeName in expressions)
			{
				if (!allPossibleExpressions.Contains(shapeName))
					allPossibleExpressions.Add(shapeName);
			}

			for (int i = 0; i < expressions.Length; i++)
			{
				var clip = aac.NewClip();

				foreach (var shapeName in expressions)
				{
					clip.BlendShape(my.skin, prefix + shapeName, shapeName == expressions[i] ? 100 : 0);
				}

				var state = layer.NewState(expressions[i], 1, i)
					.WithAnimation(clip);

				var enter = layer.EntryTransitionsTo(state)
					.When(gesture.IsEqualTo(i));
				var exit = state.Exits()
					.WithTransitionDurationSeconds(TransitionSpeed)
					.When(gesture.IsNotEqualTo(i));

				if (i == 0)
				{
					enter.Or().When(faceTrackingActiveParam.IsTrue());
					exit.And(faceTrackingActiveParam.IsFalse());
				}
				else
				{
					enter.And(faceTrackingActiveParam.IsFalse());
					exit.Or().When(faceTrackingActiveParam.IsTrue());
				}
			}
		}

		// body preferences
		{
			string prefix = "prefs/";

			var tree = aac.NewBlendTreeAsRaw();
			tree.name = "body preferences";
			tree.blendType = BlendTreeType.Direct;

			// for each blend shape with the 'prefs/' prefix,
			// create a new blend shape control subtree
			for (var i = 0; i < my.skin.sharedMesh.blendShapeCount; i++)
			{
				string blendShapeName = my.skin.sharedMesh.GetBlendShapeName(i);

				if (blendShapeName.Substring(0, prefix.Length) != prefix)
				{
					continue;
				}

				tree.AddChild(BlendShapeSlider(aac, fxTreeLayer, vrcParams,
					my.skin, blendShapeName, true));
			}

			// color changing
			tree.AddChild(SliderSubtree(aac, fxTreeLayer, vrcParams,
				my.primaryColor0, my.primaryColor1, prefix + "pcol", true));

			tree.AddChild(SliderSubtree(aac, fxTreeLayer, vrcParams,
				my.secondColor0, my.secondColor1, prefix + "scol", true));

			masterTree.AddChild(tree);
		}

		// face tracking
		{
			string prefix = "v2/";

			var tree = aac.NewBlendTreeAsRaw();
			tree.name = "face tracking";
			tree.blendType = BlendTreeType.Direct;

			tree.blendParameter = faceTrackingActiveParam.Name;
			tree.blendParameterY = faceTrackingActiveParam.Name;

			// straight-forward blendshapes
			for (var i = 0; i < my.faceTrackingFloatShapeNames.Length; i++)
			{
				string shapeName = my.faceTrackingFloatShapeNames[i];

				tree.AddChild(
					BlendShapeSlider(aac, fxTreeLayer, vrcParams, my.skin,
					prefix + shapeName, false));

				if (shapeName.EndsWith("Left"))
				{
					shapeName = shapeName.Replace("Left", "Right");

					tree.AddChild(
						BlendShapeSlider(aac, fxTreeLayer, vrcParams, my.skin,
						prefix + shapeName, false));
				}
			}

			// smile sad
			{
				var param = CreateFloatParam(fxTreeLayer, vrcParams, prefix + "SmileSad", false, 0);

				var smile = prefix + "MouthSmile";
				var sad = prefix + "MouthSad";

				var smileClip = aac.NewClip()
					.BlendShape(my.skin, smile, 100)
					.BlendShape(my.skin, sad  , 0);
				smileClip.Clip.name = "1 smile";

				var neutralClip = aac.NewClip()
					.BlendShape(my.skin, smile, 0)
					.BlendShape(my.skin, sad  , 0);
				neutralClip.Clip.name = "0 default";

				var sadClip = aac.NewClip()
					.BlendShape(my.skin, smile, 0)
					.BlendShape(my.skin, sad  , 100);
				sadClip.Clip.name = "-1 sad";

				var subtree = Create1DTree(aac, param.Name, -1f, 1);
				subtree.children = new[]
				{
					new ChildMotion { threshold = 1,     timeScale = 1, motion = smileClip.Clip },
					new ChildMotion { threshold = 0,     timeScale = 1, motion = neutralClip.Clip },
					new ChildMotion { threshold = -1f, timeScale = 1, motion = sadClip.Clip },
				};
				
				tree.AddChild(subtree);
			}
			
			//mouth X
			
			// {
			// 	var param = CreateFloatParam(fxTreeLayer, vrcParams, prefix + "MouthX", false, 0);
			//
			// 	var right = prefix + "MouthRight";
			// 	var left = prefix + "MouthLeft";
			//
			// 	var smileClip = aac.NewClip()
			// 		.BlendShape(my.skin, right, 100)
			// 		.BlendShape(my.skin, left  , 0);
			// 	smileClip.Clip.name = "1 right";
			//
			// 	var neutralClip = aac.NewClip()
			// 		.BlendShape(my.skin, right, 0)
			// 		.BlendShape(my.skin, left  , 0);
			// 	neutralClip.Clip.name = "0 default";
			//
			// 	var sadClip = aac.NewClip()
			// 		.BlendShape(my.skin, right, 0)
			// 		.BlendShape(my.skin, left  , 100);
			// 	sadClip.Clip.name = "-1 left";
			//
			// 	var subtree = Create1DTree(aac, param.Name, -1f, 1);
			// 	subtree.children = new[]
			// 	{
			// 		new ChildMotion { threshold = 1,     timeScale = 1, motion = smileClip.Clip },
			// 		new ChildMotion { threshold = 0,     timeScale = 1, motion = neutralClip.Clip },
			// 		new ChildMotion { threshold = -1f, timeScale = 1, motion = sadClip.Clip },
			// 	};
			// 	
			// 	tree.AddChild(subtree);
			// }
			
			// jaw X
			{
				var param = CreateFloatParam(fxTreeLayer, vrcParams, prefix + "JawX", false, 0);

				var right = prefix + "JawRight";
				var left = prefix + "JawLeft";

				var smileClip = aac.NewClip()
					.BlendShape(my.skin, right, 100)
					.BlendShape(my.skin, left  , 0);
				smileClip.Clip.name = "1 right";

				var neutralClip = aac.NewClip()
					.BlendShape(my.skin, right, 0)
					.BlendShape(my.skin, left  , 0);
				neutralClip.Clip.name = "0 default";

				var sadClip = aac.NewClip()
					.BlendShape(my.skin, right, 0)
					.BlendShape(my.skin, left  , 100);
				sadClip.Clip.name = "-1 left";

				var subtree = Create1DTree(aac, param.Name, -1f, 1);
				subtree.children = new[]
				{
					new ChildMotion { threshold = 1,     timeScale = 1, motion = smileClip.Clip },
					new ChildMotion { threshold = 0,     timeScale = 1, motion = neutralClip.Clip },
					new ChildMotion { threshold = -1f, timeScale = 1, motion = sadClip.Clip },
				};
				
				tree.AddChild(subtree);
			}
			
			// eyelids
			
			{
				foreach (string side in _leftRight)
				{
					var param = CreateFloatParam(fxTreeLayer, vrcParams, prefix + "EyeLid" + side, false, 0);
			
					var wide = prefix + "EyeWide" + side;
					var closed = prefix + "EyeClosed" + side;
			
					var smileClip = aac.NewClip()
						.BlendShape(my.skin, wide, 100)
						.BlendShape(my.skin, closed  , 0);
					smileClip.Clip.name = "1 wide";
			
					var neutralClip = aac.NewClip()
						.BlendShape(my.skin, wide, 0)
						.BlendShape(my.skin, closed  , 0);
					neutralClip.Clip.name = "neutral";
			
					var closedClip = aac.NewClip()
						.BlendShape(my.skin, wide, 0)
						.BlendShape(my.skin, closed  , 100);
					closedClip.Clip.name = "0 closed";
			
					var subtree = Create1DTree(aac, param.Name, 0, 1);
					subtree.children = new[]
					{
						new ChildMotion { threshold = 1,    timeScale = 1, motion = smileClip.Clip },
						new ChildMotion { threshold = 0.8f, timeScale = 1, motion = neutralClip.Clip },
						new ChildMotion { threshold = 0,    timeScale = 1, motion = closedClip.Clip },
					};
					
					tree.AddChild(subtree);
				}
			}
			
			// eyebrows
			
			{
				foreach (string side in _leftRight)
				{
					var param = CreateFloatParam(fxTreeLayer, vrcParams, prefix + "BrowExpression" + side, false, 0);
			
					var up = prefix + "BrowUp" + side;
					var down = prefix + "BrowDown" + side;
			
					var upClip = aac.NewClip()
						.BlendShape(my.skin, up, 100)
						.BlendShape(my.skin, down  , 0);
					upClip.Clip.name = "1 wide";
			
					var neutralClip = aac.NewClip()
						.BlendShape(my.skin, up, 0)
						.BlendShape(my.skin, down  , 0);
					neutralClip.Clip.name = "neutral";
			
					var downClip = aac.NewClip()
						.BlendShape(my.skin, up, 0)
						.BlendShape(my.skin, down  , 100);
					downClip.Clip.name = "0 closed";
			
					var subtree = Create1DTree(aac, param.Name, -1, 1);
					subtree.children = new[]
					{
						new ChildMotion { threshold = 1,    timeScale = 1, motion = upClip.Clip },
						new ChildMotion { threshold = 0, timeScale = 1, motion = neutralClip.Clip },
						new ChildMotion { threshold = -1,    timeScale = 1, motion = downClip.Clip },
					};
					
					tree.AddChild(subtree);
				}
			}
			
			masterTree.AddChild(tree);

			var children = masterTree.children;
			children[children.Length - 1].directBlendParameter = blendFaceTrackingParam.Name;
			masterTree.children = children;
			
			// eyes
			{
				CreateFloatParamVrcOnly(vrcParams, "v2/EyeLeftX", false, 0);
				CreateFloatParamVrcOnly(vrcParams, "v2/EyeRightX", false, 0);
				CreateFloatParamVrcOnly(vrcParams, "v2/EyeY", false, 0);
			}
		}
		

		// add all the new avatar params to the avatar descriptor
		my.avatar.expressionParameters.parameters = vrcParams.ToArray();
	}

	private BlendTree BlendShapeSlider(AacFlBase aac, AacFlLayer layer,
		List<VRCExpressionParameters.Parameter> vrcParams, 
		SkinnedMeshRenderer skin, string paramAndShapeName, bool save)
	{
		return BlendShapeSlider(aac, layer, vrcParams, skin,
			paramAndShapeName, paramAndShapeName, save);
	}

	private BlendTree BlendShapeSlider(AacFlBase aac, AacFlLayer layer,
		List<VRCExpressionParameters.Parameter> vrcParams, 
		SkinnedMeshRenderer skin, string shapeName, string paramName, bool save)
	{
		var state000 = aac.NewClip().BlendShape(skin, shapeName, 0);
		state000.Clip.name = shapeName + " weight:0";

		var state100 = aac.NewClip().BlendShape(skin, shapeName, 100);
		state100.Clip.name = shapeName + " weight:100";

		return SliderSubtree(aac, layer, vrcParams, state000.Clip, state100.Clip, paramName, save);
	}

	private BlendTree SliderSubtree(AacFlBase aac, AacFlLayer layer,
		List<VRCExpressionParameters.Parameter> vrcParams, 
		Motion clip0, Motion clip1, string paramName, bool save)
	{

		CreateFloatParam(layer, vrcParams, paramName, save, 0);

		var tree = Create1DTree(aac, paramName, 0, 1);

		tree.children = new[]
		{
			new ChildMotion {motion = clip0, threshold = 0, timeScale = 1},
			new ChildMotion {motion = clip1, threshold = 1, timeScale = 1}
		};

		return tree;
	}

	private AacFlFloatParameter CreateFloatParam(AacFlLayer layer,
		List<VRCExpressionParameters.Parameter> vrcParams, 
		string paramName, bool save, float val)
	{
		CreateFloatParamVrcOnly(vrcParams, paramName, save, val);

		return layer.FloatParameter(paramName);
	}

	private void CreateFloatParamVrcOnly(List<VRCExpressionParameters.Parameter> vrcParams,
		string paramName, bool save, float val)
	{
		vrcParams.Add(new VRCExpressionParameters.Parameter()
		{
			name = paramName,
			valueType = VRCExpressionParameters.ValueType.Float,
			saved = save,
			networkSynced = true,
			defaultValue = val,
		});
	}

	private AacFlBoolParameter CreateBoolParam(AacFlLayer layer,
		List<VRCExpressionParameters.Parameter> vrcParams, 
		string paramName, bool save, bool val)
	{
		vrcParams.Add(new VRCExpressionParameters.Parameter()
		{
			name = paramName,
			valueType = VRCExpressionParameters.ValueType.Bool,
			saved = save,
			networkSynced = true,
			defaultValue = val ? 1 : 0,
		});

		return layer.BoolParameter(paramName);
	}

	private BlendTree Create1DTree(AacFlBase aac, string paramName,
		float min, float max)
	{
		var tree = aac.NewBlendTreeAsRaw();
		tree.name = paramName;
		tree.blendParameter = paramName;
		tree.blendType = BlendTreeType.Simple1D;
		tree.minThreshold = min;
		tree.maxThreshold = max;
		tree.useAutomaticThresholds = false;

		return tree;
	}
}
#endif