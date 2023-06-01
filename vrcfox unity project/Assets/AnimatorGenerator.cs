#if UNITY_EDITOR
using AnimatorAsCode.V0;
using AnimatorAsCodeFramework.Examples;
using Boo.Lang;
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using AvatarParameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter;


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
	public SkinnedMeshRenderer skinnedMeshRenderer;
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

		// hand gestures

		aac.CreateMainGestureLayer().WithAvatarMask(my.gestureMask);

		foreach (string side in LeftRight)
		{
			var layer = aac.CreateSupportingGestureLayer(side + " hand").WithAvatarMask(my.rMask);
			var gesture = layer.IntParameter("Gesture" + side);

			for (int i = 0; i < my.handMotions.Length; i++)
			{
				Motion motion = my.handMotions[i];

				var state = layer.NewState(side + " hand " + i, 1, i).WithAnimation(motion);

				layer.EntryTransitionsTo(state).When(gesture.IsEqualTo(i));
				state.Exits().WithTransitionDurationSeconds(TransitionSpeed).When(gesture.IsNotEqualTo(i));
			}
		}

		// expressions
		aac.CreateMainFxLayer().WithAvatarMask(my.fxMask);

		var bLayer = aac.CreateSupportingFxLayer("brow").WithAvatarMask(my.fxMask);
		var bGesture = bLayer.IntParameter("LeftGesture");

		var mlayer = aac.CreateSupportingFxLayer("mouth").WithAvatarMask(my.fxMask);
		var mGesture = mlayer.IntParameter("RightGesture");


		for (var i = 0; i < my.expressionPairs.Length; i++)
		{
			var exp = my.expressionPairs[i];

			var bState = bLayer.NewState(exp.name + " brow " + i, 1, i).WithAnimation(exp.brow);
			var mState = mlayer.NewState(exp.name + " mouth " + i, 1, i).WithAnimation(exp.mouth);

			var bExit = bState.Exits().WithTransitionDurationSeconds(TransitionSpeed).WhenConditions();
			var mExit = mState.Exits().WithTransitionDurationSeconds(TransitionSpeed).WhenConditions();

			foreach (int expressionIndex in exp.gestureTriggers)
			{
				bLayer.EntryTransitionsTo(bState).When(bGesture.IsEqualTo(expressionIndex));
				bExit.And(bGesture.IsNotEqualTo(expressionIndex));

				mlayer.EntryTransitionsTo(mState).When(mGesture.IsEqualTo(expressionIndex));
				mExit.And(mGesture.IsNotEqualTo(expressionIndex));
			}
		}

		// body morphs

		List<AvatarParameter> parameters = new List<AvatarParameter>();

		var bodyShapeLayer = aac.CreateSupportingFxLayer("body").WithAvatarMask(my.fxMask);

		bodyShapeLayer.OverrideValue(bodyShapeLayer.FloatParameter("Blend"), 1);

		var fxTree = aac.NewBlendTreeAsRaw();
		fxTree.name = "body shape tree";
		fxTree.blendType = BlendTreeType.Direct;
		fxTree.blendParameter = "Blend";

		// for each blend shape with the 'body ' prefix, create a new blend shape control subtree
		for (var i = 0; i < my.skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
		{
			string blendShapeName = my.skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);

			if (blendShapeName.Substring(0, 5) != "body ")
			{
				continue;
			}

			fxTree.AddChild(BlendShapeControl(aac, bodyShapeLayer, my.skinnedMeshRenderer, blendShapeName));

			parameters.Add(new AvatarParameter()
			{
				name = blendShapeName,
				valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float,
				saved = true,
				networkSynced = true
			});
		}

		// face tracking

		for (var i = 0; i < my.faceTrackingFloatShapeNames.Length; i++)
		{
			string shapeName = my.faceTrackingFloatShapeNames[i];

			fxTree.AddChild(BlendShapeControl(aac, bodyShapeLayer, my.skinnedMeshRenderer, shapeName, v2 + shapeName));

			parameters.Add(new AvatarParameter()
			{
				name = v2 + shapeName,
				valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float,
				saved = false,
				networkSynced = true,
			});

			if (shapeName.EndsWith("Left"))
			{
				shapeName = shapeName.Replace("Left", "Right");

				fxTree.AddChild(BlendShapeControl(aac, bodyShapeLayer, my.skinnedMeshRenderer, shapeName, v2 + shapeName));

				parameters.Add(new AvatarParameter()
				{
					name = v2 + shapeName,
					valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float,
					saved = false,
					networkSynced = true,
				});
			}
		}

		// eyelids
		foreach (string side in LeftRight)
		{
			string eyelidParamName = v2 + "EyeLid" + side;
			string eyeClosedSide = "EyeClosed" + side;
			string eyeWidenSideName = "EyeWide" + side;

			bodyShapeLayer.FloatParameter(eyelidParamName);

			parameters.Add(new AvatarParameter()
			{
				name = eyelidParamName,
				valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float,
				saved = false,
				networkSynced = true,
			});

			var eyelidTree = aac.NewBlendTreeAsRaw();
			eyelidTree.name = eyeClosedSide;
			eyelidTree.blendParameter = eyelidParamName;
			eyelidTree.blendType = BlendTreeType.Simple1D;
			eyelidTree.minThreshold = 0;
			eyelidTree.maxThreshold = 1;
			eyelidTree.useAutomaticThresholds = false;

			var closed0 = aac.NewClip().BlendShape(my.skinnedMeshRenderer, eyeClosedSide, 0);
			closed0.Clip.name = eyeClosedSide + " weight:0";

			var closed100 = aac.NewClip().BlendShape(my.skinnedMeshRenderer, eyeClosedSide, 100);
			closed100.Clip.name = eyeClosedSide + " weight:100";

			var wide0 = aac.NewClip().BlendShape(my.skinnedMeshRenderer, eyeWidenSideName, 0);
			wide0.Clip.name = eyeWidenSideName + " weight:0";

			var wide100 = aac.NewClip().BlendShape(my.skinnedMeshRenderer, eyeWidenSideName, 100);
			wide100.Clip.name = eyeWidenSideName + " weight:100";

			eyelidTree.children = new[]
			{
				new ChildMotion {motion = closed100.Clip, threshold = 0f, timeScale = 1},
				new ChildMotion {motion = closed0.Clip, threshold = 0.8f, timeScale = 1},
				new ChildMotion {motion = wide0.Clip, threshold = 0.8f, timeScale = 1},
				new ChildMotion {motion = wide100.Clip, threshold = 1, timeScale = 1}
			};

			fxTree.AddChild(eyelidTree);
		}

		string smileFrownParamName = "v2/SmileSad";
		string mouthSmile = "MouthSmile";
		string mouthSad = "MouthSad";

		bodyShapeLayer.FloatParameter(smileFrownParamName);

		parameters.Add(new AvatarParameter()
		{
			name = smileFrownParamName,
			valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float,
			saved = false,
			networkSynced = true,
		});

		var smileFrownTree = aac.NewBlendTreeAsRaw();
		smileFrownTree.name = "smile frown";
		smileFrownTree.blendParameter = smileFrownParamName;
		smileFrownTree.blendType = BlendTreeType.Simple1D;
		smileFrownTree.minThreshold = -1;
		smileFrownTree.maxThreshold = 1;
		smileFrownTree.useAutomaticThresholds = false;

		var smile0 = aac.NewClip().BlendShape(my.skinnedMeshRenderer, mouthSmile, 0);
		smile0.Clip.name = mouthSmile + " weight:0";

		var smile100 = aac.NewClip().BlendShape(my.skinnedMeshRenderer, mouthSmile, 100);
		smile100.Clip.name = mouthSmile + " weight:100";

		var sad0 = aac.NewClip().BlendShape(my.skinnedMeshRenderer, mouthSad, 0);
		sad0.Clip.name = mouthSad + " weight:0";

		var sad100 = aac.NewClip().BlendShape(my.skinnedMeshRenderer, mouthSad, 100);
		sad100.Clip.name = mouthSad + " weight:100";

		smileFrownTree.children = new[]
		{
			new ChildMotion {motion = smile0.Clip, threshold = 0f, timeScale = 1},
			new ChildMotion {motion = smile100.Clip, threshold = 1, timeScale = 1},
			new ChildMotion {motion = sad0.Clip, threshold = 0, timeScale = 1},
			new ChildMotion {motion = sad100.Clip, threshold = -1, timeScale = 1}
		};

		fxTree.AddChild(smileFrownTree);

		// eye params
		foreach (string side in LeftRight)
		{
			parameters.Add(new AvatarParameter()
			{
				name = v2 + "Eye" + side + "X",
				valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float,
				saved = false,
				networkSynced = true,
			});
		}

		parameters.Add(new AvatarParameter()
		{
			name = v2 + "EyeY",
			valueType = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float,
			saved = false,
			networkSynced = true,
		});


		my.avatar.expressionParameters.parameters = parameters.ToArray();

		bodyShapeLayer.NewState("tree").WithAnimation(fxTree).WithWriteDefaultsSetTo(true);
	}




	private BlendTree BlendShapeControl(AacFlBase aac, AacFlLayer layer, SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName)
	{
		return BlendShapeControl(aac, layer, skinnedMeshRenderer, blendShapeName, blendShapeName);
	}

	private BlendTree BlendShapeControl(AacFlBase aac, AacFlLayer layer, SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName, string parameterName)
	{
		layer.FloatParameter(parameterName);

		var sliderTree = aac.NewBlendTreeAsRaw();
		sliderTree.name = blendShapeName;
		sliderTree.blendParameter = parameterName;
		sliderTree.blendType = BlendTreeType.Simple1D;
		sliderTree.minThreshold = 0;
		sliderTree.maxThreshold = 1;
		sliderTree.useAutomaticThresholds = false;

		var state000 = aac.NewClip().BlendShape(skinnedMeshRenderer, blendShapeName, 0);
		state000.Clip.name = blendShapeName + " weight:0";

		var state100 = aac.NewClip().BlendShape(skinnedMeshRenderer, blendShapeName, 100);
		state100.Clip.name = blendShapeName + " weight:100";

		sliderTree.children = new[]
		{
			new ChildMotion {motion = state000.Clip, threshold = 0, timeScale = 1},
			new ChildMotion {motion = state100.Clip, threshold = 1, timeScale = 1}
		};

		return sliderTree;
	}
}
#endif