#if UNITY_EDITOR
using AnimatorAsCode.V0;
using AnimatorAsCodeFramework.Examples;
using Cysharp.Threading.Tasks.Triggers;
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;


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

	public string[] faceTrackingShapes;
}

[CustomEditor(typeof(AnimatorGenerator), true)]

public class AnimatorGeneratorEditor : Editor
{
	private const string Lgesture = "GestureLeft";
	private const string Rgesture = "GestureRight";
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

		var lLayer = aac.CreateSupportingGestureLayer("left hand").WithAvatarMask(my.lMask);
		var lGesture = lLayer.IntParameter(Lgesture);

		var rLayer = aac.CreateSupportingGestureLayer("right hand").WithAvatarMask(my.rMask);
		var rGesture = rLayer.IntParameter(Rgesture);

		for (int i = 0; i < my.handMotions.Length; i++)
		{
			Motion motion = my.handMotions[i];

			var lState = lLayer.NewState("left hand " + i, 1, i).WithAnimation(motion);
			var rState = rLayer.NewState("right hand " + i, 1, i).WithAnimation(motion);

			lLayer.EntryTransitionsTo(lState).When(lGesture.IsEqualTo(i));
			lState.Exits().WithTransitionDurationSeconds(TransitionSpeed).When(lGesture.IsNotEqualTo(i));

			rLayer.EntryTransitionsTo(rState).When(rGesture.IsEqualTo(i));
			rState.Exits().WithTransitionDurationSeconds(TransitionSpeed).When(rGesture.IsNotEqualTo(i));
		}

		// expressions
		aac.CreateMainFxLayer().WithAvatarMask(my.fxMask);

		var bLayer = aac.CreateSupportingFxLayer("brow").WithAvatarMask(my.fxMask);
		var bGesture = bLayer.IntParameter(Lgesture);

		var mlayer = aac.CreateSupportingFxLayer("mouth").WithAvatarMask(my.fxMask);
		var mGesture = mlayer.IntParameter(Rgesture);


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

		// create layer
		var bodyShapeLayer = aac.CreateSupportingFxLayer("body").WithAvatarMask(my.fxMask);

		// create blend param to force direct blend tree on
		bodyShapeLayer.OverrideValue(bodyShapeLayer.FloatParameter("Blend"), 1);

		// create direct tree
		var bodyShapeTree = aac.NewBlendTreeAsRaw();
		bodyShapeTree.name = "body shape tree";
		bodyShapeTree.blendType = BlendTreeType.Direct;
		bodyShapeTree.blendParameter = "Blend";

		// for each blend shape with the 'body ' prefix, create a new blend shape control subtree
		for (var i = 0; i < my.skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
		{
			string blendShapeName = my.skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);

			if (blendShapeName.Substring(0, 5) != "body ")
			{
				continue;
			}

			bodyShapeTree.AddChild(BlendShapeControl(aac, bodyShapeLayer, my.skinnedMeshRenderer, blendShapeName));
		}

		for(var i = 0; i < my.faceTrackingShapes.Length; i++)
		{
			string shapeName = my.faceTrackingShapes[i];

			bodyShapeTree.AddChild(BlendShapeControl(aac, bodyShapeLayer, my.skinnedMeshRenderer, shapeName));

			if(shapeName.EndsWith("Left"))
			{
				bodyShapeTree.AddChild(BlendShapeControl(aac, bodyShapeLayer, my.skinnedMeshRenderer, shapeName.Replace("Left", "Right")));
			}
		}

		bodyShapeLayer.NewState("tree").WithAnimation(bodyShapeTree).WithWriteDefaultsSetTo(true);
	}

	private BlendTree BlendShapeControl(AacFlBase aac, AacFlLayer layer, SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName)
	{
		layer.FloatParameter(blendShapeName);

		var sliderTree = aac.NewBlendTreeAsRaw();
		sliderTree.name = blendShapeName;
		sliderTree.blendParameter = blendShapeName;
		sliderTree.blendType = BlendTreeType.Simple1D;
		sliderTree.minThreshold = 0;
		sliderTree.maxThreshold = 1;
		sliderTree.useAutomaticThresholds = true;

		var zero = aac.NewClip().BlendShape(skinnedMeshRenderer, blendShapeName, 0);
		zero.Clip.name = blendShapeName + " weight:0";
		
		var one = aac.NewClip().BlendShape(skinnedMeshRenderer, blendShapeName, 1);
		one.Clip.name = blendShapeName + " weight:1";

		sliderTree.children = new[]
		{
			new ChildMotion {motion = zero.Clip, timeScale = 1, threshold = 0},
			new ChildMotion {motion = one.Clip, timeScale = 1, threshold = 1}
		};

		return sliderTree;
	}
}
#endif