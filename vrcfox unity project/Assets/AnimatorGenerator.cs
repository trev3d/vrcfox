#if UNITY_EDITOR
using AnimatorAsCode.V0;
using AnimatorAsCodeFramework.Examples;
using Boo.Lang;
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VrcParameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter;


[Serializable]
public class ExpressionPair
{
	public string name;
	public Motion brow;
	public Motion mouth;
	public int[] gestureTriggers;
}

public class AnimatorGenerator : MonoBehaviour
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

	public ExpressionPair[] expressionPairs;

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

[CustomEditor(typeof(AnimatorGenerator), true)]

public class AnimatorGeneratorEditor : Editor
{
	private string[] LeftRight = { "Left", "Right" };
	private const string v2 = "v2/";
	private const string SystemName = "vrcfox";
	private const float TransitionSpeed = 0.05f;


	public override void OnInspectorGUI()
	{
		AacExample.InspectorTemplate(this, serializedObject, "assetKey", Create);
	}

	private void Create()
	{

		var my = (AnimatorGenerator)target;

		var aac = AacExample.AnimatorAsCode(SystemName, my.avatar, my.assetContainer, my.assetKey,
			AacExample.Options().WriteDefaultsOff());

		aac.ClearPreviousAssets();

		// hand gestures
		aac.CreateMainGestureLayer().WithAvatarMask(my.gestureMask);
		foreach (string side in LeftRight)
		{
			var layer = aac.CreateSupportingGestureLayer(side + " hand").WithAvatarMask(side == "Left" ? my.lMask : my.rMask);
			var gesture = layer.IntParameter("Gesture" + side);

			for (int i = 0; i < my.handMotions.Length; i++)
			{
				Motion motion = my.handMotions[i];

				var state = layer.NewState(side + " hand " + i, 1, i).WithAnimation(motion);

				layer.EntryTransitionsTo(state).When(gesture.IsEqualTo(i));
				state.Exits().WithTransitionDurationSeconds(TransitionSpeed).When(gesture.IsNotEqualTo(i));
			}
		}

		var fxLayer = aac.CreateMainFxLayer().WithAvatarMask(my.fxMask);

		List<VrcParameter> avatarParams = new List<VrcParameter>();

		// face tracking eye params (these animations are handled in the additive controller)
		foreach (string side in LeftRight)
		{
			avatarParams.Add(new VrcParameter()
			{
				name = v2 + "Eye" + side + "X",
				valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float,
				saved = false,
				networkSynced = true,
			});
		}

		avatarParams.Add(new VrcParameter()
		{
			name = v2 + "EyeY",
			valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float,
			saved = false,
			networkSynced = true,
		});

		// create fx tree
		var fxTreeLayer = aac.CreateSupportingFxLayer("body").WithAvatarMask(my.fxMask);

		fxTreeLayer.OverrideValue(fxTreeLayer.FloatParameter("Blend"), 1);

		var fxTree = aac.NewBlendTreeAsRaw();
		fxTree.name = "blendshapes and toggles";
		fxTree.blendType = BlendTreeType.Direct;

		fxTreeLayer.NewState(fxTree.name).WithAnimation(fxTree).WithWriteDefaultsSetTo(true);

		// expressions
		//var bLayer = aac.CreateSupportingFxLayer("brow").WithAvatarMask(my.fxMask);
		//var bGesture = bLayer.IntParameter("LeftGesture");

		//var mlayer = aac.CreateSupportingFxLayer("mouth").WithAvatarMask(my.fxMask);
		//var mGesture = mlayer.IntParameter("RightGesture");


		//for (var i = 0; i < my.expressionPairs.Length; i++)
		//{
		//	var exp = my.expressionPairs[i];

		//	var bState = bLayer.NewState(exp.name + " brow " + i, 1, i).WithAnimation(exp.brow);
		//	var mState = mlayer.NewState(exp.name + " mouth " + i, 1, i).WithAnimation(exp.mouth);

		//	var bExit = bState.Exits().WithTransitionDurationSeconds(TransitionSpeed).WhenConditions();
		//	var mExit = mState.Exits().WithTransitionDurationSeconds(TransitionSpeed).WhenConditions();

		//	foreach (int expressionIndex in exp.gestureTriggers)
		//	{
		//		bLayer.EntryTransitionsTo(bState).When(bGesture.IsEqualTo(expressionIndex));
		//		bExit.And(bGesture.IsNotEqualTo(expressionIndex));

		//		mlayer.EntryTransitionsTo(mState).When(mGesture.IsEqualTo(expressionIndex));
		//		mExit.And(mGesture.IsNotEqualTo(expressionIndex));
		//	}
		//}

		// face tracking
		var faceTrackingTree = aac.NewBlendTreeAsRaw();
		faceTrackingTree.name = "face tracking tree";
		faceTrackingTree.blendType = BlendTreeType.Direct;

		// face tracking eyelids
		foreach (string side in LeftRight)
		{
			faceTrackingTree.AddChild(DualBlendShapeSlider(aac, fxTreeLayer, avatarParams, my.skin, "EyeClosed" + side, "EyeWide" + side, 0, 0.8f, 1, "v2/EyeLid" + side));
		}

		// face tracking straight-forward blendshapes
		for (var i = 0; i < my.faceTrackingFloatShapeNames.Length; i++)
		{
			string shapeName = my.faceTrackingFloatShapeNames[i];

			faceTrackingTree.AddChild(BlendShapeSlider(aac, fxTreeLayer, avatarParams, my.skin, shapeName, v2 + shapeName, false));

			if (shapeName.EndsWith("Left"))
			{
				shapeName = shapeName.Replace("Left", "Right");

				faceTrackingTree.AddChild(BlendShapeSlider(aac, fxTreeLayer, avatarParams, my.skin, shapeName, v2 + shapeName, false));
			}
		}

		// face tracking mouth smile & frown
		faceTrackingTree.AddChild(DualBlendShapeSlider(aac, fxTreeLayer, avatarParams, my.skin, "MouthSad", "MouthSmile", -1, 0, 1, "v2/SmileSad"));

		// add face tree to fx tree
		fxTree.AddChild(faceTrackingTree);

		// body settings
		{
			string prefix = "body ";

			// for each blend shape with the 'body ' prefix, create a new blend shape control subtree
			for (var i = 0; i < my.skin.sharedMesh.blendShapeCount; i++)
			{
				string blendShapeName = my.skin.sharedMesh.GetBlendShapeName(i);

				if (blendShapeName.Substring(0, 5) != prefix)
				{
					continue;
				}

				fxTree.AddChild(BlendShapeSlider(aac, fxTreeLayer, avatarParams, my.skin, blendShapeName, blendShapeName.Replace(prefix, ""), true));
			}
		}

		// face tracking vs default animation control 
		{
			var layer = aac.CreateSupportingFxLayer("face tracking control").WithAvatarMask(my.fxMask);

			var param = CreateBoolParam(layer, avatarParams, "FaceTrackingActive", true, false);

			var offState = layer.NewState("face tracking off");
			var offControl = offState.State.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
			offControl.trackingEyes = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;
			offControl.trackingMouth = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Tracking;

			var onState = layer.NewState("face tracking on");
			var onControl = onState.State.AddStateMachineBehaviour<VRCAnimatorTrackingControl>();
			onControl.trackingEyes = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;
			onControl.trackingMouth = VRC.SDKBase.VRC_AnimatorTrackingControl.TrackingType.Animation;

			layer.AnyTransitionsTo(onState).WithTransitionToSelf().When(param.IsTrue());
			layer.AnyTransitionsTo(offState).When(param.IsFalse());
		}

		// add all the new avatar params to the avatar descriptor
		my.avatar.expressionParameters.parameters = avatarParams.ToArray();
	}

	private BlendTree BlendShapeSlider(AacFlBase aac, AacFlLayer layer, List<VrcParameter> vrcParams, SkinnedMeshRenderer skin, string blendShapeName, string paramName, bool save)
	{
		CreateFloatParam(layer, vrcParams, paramName, save, 0);

		var tree = Create1DTree(aac, paramName, 0, 1);

		var state000 = aac.NewClip().BlendShape(skin, blendShapeName, 0);
		state000.Clip.name = blendShapeName + " weight:0";

		var state100 = aac.NewClip().BlendShape(skin, blendShapeName, 100);
		state100.Clip.name = blendShapeName + " weight:100";

		tree.children = new[]
		{
			new ChildMotion {motion = state000.Clip, threshold = 0, timeScale = 1},
			new ChildMotion {motion = state100.Clip, threshold = 1, timeScale = 1}
		};

		return tree;
	}

	private BlendTree DualBlendShapeSlider(AacFlBase aac, AacFlLayer layer, List<VrcParameter> vrcParams, SkinnedMeshRenderer skin, string negName, string posName, float min, float mid, float max, string paramName)
	{
		CreateFloatParam(layer, vrcParams, paramName, false, mid);

		var tree = Create1DTree(aac, paramName, min, max);

		var pos0 = aac.NewClip().BlendShape(skin, posName, 0);
		pos0.Clip.name = posName + " weight:0";

		var pos100 = aac.NewClip().BlendShape(skin, posName, 100);
		pos100.Clip.name = posName + " weight:100";

		var neg0 = aac.NewClip().BlendShape(skin, negName, 0);
		neg0.Clip.name = negName + " weight:0";

		var neg100 = aac.NewClip().BlendShape(skin, negName, 100);
		neg100.Clip.name = negName + " weight:100";

		tree.children = new[]
		{
				new ChildMotion {motion = neg100.Clip, threshold = min, timeScale = 1},
				new ChildMotion {motion = neg0.Clip, threshold = mid, timeScale = 1},
				new ChildMotion {motion = pos0.Clip, threshold = mid, timeScale = 1},
				new ChildMotion {motion = pos100.Clip, threshold = max, timeScale = 1},
			};

		return tree;
	}

	private AacFlFloatParameter CreateFloatParam(AacFlLayer layer, List<VrcParameter> vrcParams, string paramName, bool save, float val)
	{
		vrcParams.Add(new VrcParameter()
		{
			name = paramName,
			valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float,
			saved = save,
			networkSynced = true,
			defaultValue = val,
		});

		return layer.FloatParameter(paramName);
	}

	private AacFlBoolParameter CreateBoolParam(AacFlLayer layer, List<VrcParameter> vrcParams, string paramName, bool save, bool val)
	{
		vrcParams.Add(new VrcParameter()
		{
			name = paramName,
			valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool,
			saved = save,
			networkSynced = true,
			defaultValue = val? 1 : 0,
		});

		return layer.BoolParameter(paramName);
	}

	private BlendTree Create1DTree(AacFlBase aac, string paramName, float min, float max)
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