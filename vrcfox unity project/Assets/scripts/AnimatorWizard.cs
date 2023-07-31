#if UNITY_EDITOR
using System;
using AnimatorAsCode.V0;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Collections.Generic;

[Serializable]
public struct DualShape
{
	public string paramName, minShapeName, maxShapeName;
	public float minValue, neutralValue, maxValue;

	public DualShape(string paramName, string minShapeName, string maxShapeName, float minValue,
		float neutralValue, float maxValue)
	{
		this.paramName = paramName;
		this.minShapeName = minShapeName;
		this.maxShapeName = maxShapeName;

		this.minValue = minValue;
		this.neutralValue = neutralValue;
		this.maxValue = maxValue;
	}

	public DualShape(string paramName, string minShapeName, string maxShapeName)
	{
		this.paramName = paramName;
		this.minShapeName = minShapeName;
		this.maxShapeName = maxShapeName;

		this.minValue = -1;
		this.neutralValue = 0;
		this.maxValue = 1;
	}
}

public class AnimatorWizard : MonoBehaviour
{
	private const int SpaceSize = 30;

	public VRCAvatarDescriptor avatar;
	public SkinnedMeshRenderer skin;
	public AnimatorController assetContainer;

	[Header("Avatar masks")] [Space(SpaceSize)]
	public AvatarMask fxMask;

	public AvatarMask gestureMask;
	public AvatarMask lMask;
	public AvatarMask rMask;

	[Header("Hand gesture pose motions. Index corresponds to gesture parameter value!")] [Space(SpaceSize)]
	public Motion[] handPoses;

	[Header("Player preference blendshape prefix.")]
	[Tooltip(
		"Any blendshapes on 'skin' with this prefix will create matching vrc parameters that are saved and network synced")]
	[Space(SpaceSize)]
	public string prefsPrefix = "prefs/";

	public bool colorCustomization = true;

	public Motion primaryColor0;
	public Motion primaryColor1;

	public Motion secondColor0;
	public Motion secondColor1;

	[Header("Brow & mouth expressions controlled by hand gestures. Index corresponds to gesture parameter value!")]
	[Space(SpaceSize)]
	public string mouthPrefix = "exp/mouth/";

	public string[] mouthShapeNames;

	public string browsPrefix = "exp/brows/";
	public string[] browShapeNames;

	[Header("Face tracking settings")] [Space(SpaceSize)]
	public bool ftSupport = true;

	public string ftPrefix = "v2/";

	public string[] ftSingleShapes =
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

	public DualShape[] ftDualShapes =
	{
		new DualShape("SmileSad", "MouthSad", "MouthSmile"),
		new DualShape("JawX", "JawLeft", "JawRight"),
		new DualShape("EyeLidLeft", "EyeClosed", "EyeWide", 0, 0.8f, 1),
		new DualShape("BrowExpressionLeft", "BrowDown", "BrowUp"),
	};
}

[CustomEditor(typeof(AnimatorWizard), true)]
public class AnimatorGeneratorEditor : Editor
{
	private const string Left = "Left";
	private const string Right = "Right";

	private const string SystemName = "vrcfox";
	private const float TransitionSpeed = 0.05f;

	public override void OnInspectorGUI()
	{
		if (GUILayout.Button("Setup animator! (DESTRUCTIVE!!!)"))
			Create();

		DrawDefaultInspector();
	}

	private void Create()
	{
		var my = (AnimatorWizard)target;
		var vrcParams = new List<VRCExpressionParameters.Parameter>();
		var aac = AacV0.Create(new AacConfiguration
		{
			SystemName = SystemName,
			AvatarDescriptor = my.avatar,
			AnimatorRoot = my.avatar.transform,
			DefaultValueRoot = my.avatar.transform,
			AssetContainer = my.assetContainer,
			AssetKey = SystemName,
			DefaultsProvider = new AacDefaultsProvider(false)
		});

		aac.ClearPreviousAssets();
		aac.RemoveAllSupportingLayers("");

		// Gesture layer
		aac.CreateMainGestureLayer().WithAvatarMask(my.gestureMask);

		{
			// hand gestures
			foreach (string side in new[] { Left, Right })
			{
				var layer = aac.CreateSupportingGestureLayer(side + " hand")
					.WithAvatarMask(side == Left ? my.lMask : my.rMask);

				var gesture = layer.IntParameter("Gesture" + side);

				if (my.handPoses.Length != 8)
					throw new Exception("Number of hand poses must equal number of hand gestures (8)!");

				for (int i = 0; i < my.handPoses.Length; i++)
				{
					Motion motion = my.handPoses[i];

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

		// FX layer
		var fxLayer = aac.CreateMainFxLayer().WithAvatarMask(my.fxMask);
		fxLayer.OverrideValue(fxLayer.FloatParameter("Blend"), 1);

		{
			AacFlBoolParameter ftActiveParam = CreateBoolParam(fxLayer,
				vrcParams, my.prefsPrefix + "FaceTrackingActive", true, false);
			AacFlFloatParameter ftBlendParam = fxLayer.FloatParameter("BlendFaceTracking");

			// master fx tree
			var fxTreeLayer = aac.CreateSupportingFxLayer("tree").WithAvatarMask(my.fxMask);

			var masterTree = aac.NewBlendTreeAsRaw();
			masterTree.name = "master tree";
			masterTree.blendType = BlendTreeType.Direct;
			fxTreeLayer.NewState(masterTree.name).WithAnimation(masterTree).WithWriteDefaultsSetTo(true);

			// brow gesture expressions
			{
				var expressions = my.browShapeNames;
				var layer = aac.CreateSupportingFxLayer("brow poses").WithAvatarMask(my.fxMask);
				var gesture = layer.IntParameter("GestureLeft");

				List<string> allPossibleExpressions = new List<string>();

				foreach (var shapeName in expressions)
				{
					if (!allPossibleExpressions.Contains(shapeName))
						allPossibleExpressions.Add(shapeName);
				}

				if (expressions.Length != 8)
					throw new Exception("Number of face poses must equal number of hand gestures (8)!");

				for (int i = 0; i < expressions.Length; i++)
				{
					var clip = aac.NewClip();

					foreach (var shapeName in expressions)
					{
						clip.BlendShape(my.skin, my.browsPrefix + shapeName, shapeName == expressions[i] ? 100 : 0);
					}

					var state = layer.NewState(expressions[i], 1, i)
						.WithAnimation(clip);

					var enter = layer.EntryTransitionsTo(state)
						.When(gesture.IsEqualTo(i));
					var exit = state.Exits()
						.WithTransitionDurationSeconds(TransitionSpeed)
						.When(gesture.IsNotEqualTo(i));

					if (my.ftSupport)
					{
						if (i == 0)
						{
							enter.Or().When(ftActiveParam.IsTrue());
							exit.And(ftActiveParam.IsFalse());
						}
						else
						{
							enter.And(ftActiveParam.IsFalse());
							exit.Or().When(ftActiveParam.IsTrue());
						}
					}
				}
			}

			// mouth gesture expressions
			{
				var expressions = my.mouthShapeNames;
				var layer = aac.CreateSupportingFxLayer("mouth poses").WithAvatarMask(my.fxMask);
				var gesture = layer.IntParameter("GestureRight");

				List<string> allPossibleExpressions = new List<string>();

				foreach (var shapeName in expressions)
				{
					if (!allPossibleExpressions.Contains(shapeName))
						allPossibleExpressions.Add(shapeName);
				}

				if (expressions.Length != 8)
					throw new Exception("Number of face poses must equal number of hand gestures (8)!");

				for (int i = 0; i < expressions.Length; i++)
				{
					var clip = aac.NewClip();

					foreach (var shapeName in expressions)
					{
						clip.BlendShape(my.skin, my.mouthPrefix + shapeName, shapeName == expressions[i] ? 100 : 0);
					}

					var state = layer.NewState(expressions[i], 1, i)
						.WithAnimation(clip);

					var enter = layer.EntryTransitionsTo(state)
						.When(gesture.IsEqualTo(i));
					var exit = state.Exits()
						.WithTransitionDurationSeconds(TransitionSpeed)
						.When(gesture.IsNotEqualTo(i));

					if (my.ftSupport)
					{
						if (i == 0)
						{
							enter.Or().When(ftActiveParam.IsTrue());
							exit.And(ftActiveParam.IsFalse());
						}
						else
						{
							enter.And(ftActiveParam.IsFalse());
							exit.Or().When(ftActiveParam.IsTrue());
						}
					}
				}
			}

			// body preferences
			{
				var tree = masterTree.CreateBlendTreeChild(0);
				tree.name = "body preferences";
				tree.blendType = BlendTreeType.Direct;

				// for each blend shape with the 'prefs/' prefix,
				// create a new blend shape control subtree
				for (var i = 0; i < my.skin.sharedMesh.blendShapeCount; i++)
				{
					string blendShapeName = my.skin.sharedMesh.GetBlendShapeName(i);

					if (blendShapeName.Substring(0, my.prefsPrefix.Length) == my.prefsPrefix)
					{
						tree.AddChild(BlendshapeTree(aac, fxTreeLayer, vrcParams,
							my.skin, blendShapeName, true));
					}
				}

				if (my.colorCustomization)
				{
					// color changing
					tree.AddChild(Subtree(aac, fxTreeLayer, vrcParams,
						new[] { my.primaryColor0, my.primaryColor1 }, new[] { 0f, 1f }, my.prefsPrefix + "pcol", true));

					tree.AddChild(Subtree(aac, fxTreeLayer, vrcParams,
						new[] { my.secondColor0, my.secondColor1 }, new[] { 0f, 1f }, my.prefsPrefix + "scol", true));
				}
			}

			// face tracking
			if (my.ftSupport)
			{
				var layer = aac.CreateSupportingFxLayer("face animations toggle").WithAvatarMask(my.fxMask);

				var offState = layer.NewState("face tracking off")
					.Drives(ftBlendParam, 0);
				var offControl = offState.State.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
				offControl.trackingEyes = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;
				offControl.trackingMouth = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;

				var onState = layer.NewState("face tracking on")
					.Drives(ftBlendParam, 1);
				var onControl = onState.State.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
				onControl.trackingEyes = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
				onControl.trackingMouth = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;

				layer.AnyTransitionsTo(onState).WithTransitionToSelf().When(ftActiveParam.IsTrue());
				layer.AnyTransitionsTo(offState).When(ftActiveParam.IsFalse());


				var tree = masterTree.CreateBlendTreeChild(0);
				tree.name = "face tracking";
				tree.blendType = BlendTreeType.Direct;

				tree.blendParameter = ftActiveParam.Name;
				tree.blendParameterY = ftActiveParam.Name;

				// straight-forward blendshapes
				for (var i = 0; i < my.ftSingleShapes.Length; i++)
				{
					string shapeName = my.ftSingleShapes[i];

					for (int flip = 0; flip < Flip(ref shapeName); flip++)
					{
						tree.AddChild(BlendshapeTree(aac, fxTreeLayer, vrcParams, my.skin,
							my.ftPrefix + shapeName, false));
					}
				}

				// dual blendshapes
				for (var i = 0; i < my.ftDualShapes.Length; i++)
				{
					DualShape shape = my.ftDualShapes[i];
					string paramName = shape.paramName;

					for (int flip = 0; flip < Flip(ref paramName); flip++)
					{
						tree.AddChild(DualBlendshapeTree(aac, fxTreeLayer, vrcParams, my.skin,
							my.ftPrefix + paramName,
							my.ftPrefix + shape.minShapeName + GetSide(paramName),
							my.ftPrefix + shape.maxShapeName + GetSide(paramName),
							shape.minValue, shape.neutralValue, shape.maxValue));
					}
				}

				var children = masterTree.children;
				children[children.Length - 1].directBlendParameter = ftBlendParam.Name;
				masterTree.children = children;

				// eyes
				{
					CreateFloatParamVrcOnly(vrcParams, my.ftPrefix + "EyeLeftX", false, 0);
					CreateFloatParamVrcOnly(vrcParams, my.ftPrefix + "EyeRightX", false, 0);
					CreateFloatParamVrcOnly(vrcParams, my.ftPrefix + "EyeY", false, 0);
				}
			}
		}


		// add all the new avatar params to the avatar descriptor
		my.avatar.expressionParameters.parameters = vrcParams.ToArray();
	}

	private BlendTree BlendshapeTree(AacFlBase aac, AacFlLayer layer,
		List<VRCExpressionParameters.Parameter> vrcParams,
		SkinnedMeshRenderer skin, string paramAndShapeName, bool save)
	{
		return BlendshapeTree(aac, layer, vrcParams, skin,
			paramAndShapeName, paramAndShapeName, save);
	}

	private BlendTree BlendshapeTree(AacFlBase aac, AacFlLayer layer,
		List<VRCExpressionParameters.Parameter> vrcParams,
		SkinnedMeshRenderer skin, string shapeName, string paramName, bool save)
	{
		var state000 = aac.NewClip().BlendShape(skin, shapeName, 0);
		state000.Clip.name = paramName + " weight:0";

		var state100 = aac.NewClip().BlendShape(skin, shapeName, 100);
		state100.Clip.name = paramName + " weight:100";

		return Subtree(aac, layer, vrcParams, new Motion[] { state000.Clip, state100.Clip }, new[] { 0f, 1f },
			paramName, save);
	}

	private BlendTree DualBlendshapeTree(AacFlBase aac, AacFlLayer layer,
		List<VRCExpressionParameters.Parameter> vrcParams, SkinnedMeshRenderer skin, string paramName,
		string minShapeName, string maxShapeName, float minValue, float neutralValue, float maxValue)
	{
		var param = CreateFloatParam(layer, vrcParams, paramName, false, 0);

		var minClip = aac.NewClip()
			.BlendShape(skin, minShapeName, 100)
			.BlendShape(skin, maxShapeName, 0);
		minClip.Clip.name = param.Name + " min";

		var neutralClip = aac.NewClip()
			.BlendShape(skin, minShapeName, 0)
			.BlendShape(skin, maxShapeName, 0);
		neutralClip.Clip.name = param.Name + " neutral";

		var maxClip = aac.NewClip()
			.BlendShape(skin, minShapeName, 0)
			.BlendShape(skin, maxShapeName, 100);
		maxClip.Clip.name = param.Name + " max";

		return Subtree(aac, layer, vrcParams,
			new[] { minClip.Clip, neutralClip.Clip, maxClip.Clip },
			new[] { minValue, neutralValue, maxValue }, param.Name, false);
	}


	private BlendTree Subtree(AacFlBase aac, AacFlLayer layer,
		List<VRCExpressionParameters.Parameter> vrcParams,
		Motion[] motions, float[] thresholds, string paramName, bool save)
	{
		CreateFloatParam(layer, vrcParams, paramName, save, 0);

		var tree = Create1DTree(aac, paramName, 0, 1);

		ChildMotion[] children = new ChildMotion[motions.Length];

		for (int i = 0; i < motions.Length; i++)
		{
			children[i] = new ChildMotion { motion = motions[i], threshold = thresholds[i], timeScale = 1 };
		}

		tree.children = children;

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
		tree.useAutomaticThresholds = false;
		tree.name = paramName;
		tree.blendParameter = paramName;
		tree.minThreshold = min;
		tree.maxThreshold = max;
		tree.blendType = BlendTreeType.Simple1D;

		return tree;
	}

	private static bool IsSide(string str)
	{
		return str.Contains(Right) || str.Contains(Left);
	}

	private static int Flip(ref string str)
	{
		if (str.EndsWith(Right))
		{
			str = str.Replace(Right, Left);
		}
		else if (str.EndsWith(Left))
		{
			str = str.Replace(Left, Right);
		}
		else
		{
			return 1;
		}

		return 2;
	}

	private static string GetSide(string str)
	{
		if (str.EndsWith(Right))
			return Right;
		if (str.EndsWith(Left))
			return Left;
		return "";
	}
}
#endif