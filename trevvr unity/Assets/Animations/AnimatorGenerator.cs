#if UNITY_EDITOR
using System;
using AnimatorAsCode.V0;
using AnimatorAsCodeFramework.Examples;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Animations
{
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
        public AnimatorController assetContainer;
        public string assetKey;

        public AvatarMask gestureMask;
        public AvatarMask lMask;
        public AvatarMask rMask;
        public Motion[] handMotions;

        public AvatarMask fxMask;

        public ExpressionPair[] expressionPairs;
    }

    [CustomEditor(typeof(AnimatorGenerator), true)]

    public class AnimatorGeneratorEditor : Editor
    {
        private const string Lgesture = "GestureLeft";
        private const string Rgesture = "GestureRight";
        private const string SystemName = "Trev Animations";
        private const float TransitionSpeed = 0.05f;


        public override void OnInspectorGUI()
        {
            AacExample.InspectorTemplate(this, serializedObject, "assetKey", Create, Remove);
        }

        private void Remove()
        {
            var my = (AnimatorGenerator)target;
            var aac = AacExample.AnimatorAsCode(SystemName, my.avatar, my.assetContainer, my.assetKey);

            aac.RemoveAllMainLayers();
            aac.RemoveAllSupportingLayers("left hand");
            aac.RemoveAllSupportingLayers("right hand");
            aac.RemoveAllSupportingLayers("brow");
            aac.RemoveAllSupportingLayers("mouth");
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

                AacFlNewTransitionContinuation bOr;
                AacFlNewTransitionContinuation mOr;
                
                foreach (int expressionIndex in exp.gestureTriggers)
                {
                    bLayer.EntryTransitionsTo(bState).When(bGesture.IsEqualTo(expressionIndex));
                    bExit.And(bGesture.IsNotEqualTo(expressionIndex));

                    mlayer.EntryTransitionsTo(mState).When(mGesture.IsEqualTo(expressionIndex));
                    mExit.And(mGesture.IsNotEqualTo(expressionIndex));
                }

                // use thumb-s up to grab objects without changing expression
                bExit.And(bGesture.IsNotEqualTo(7));
                mExit.And(bGesture.IsNotEqualTo(7));
            }
        }
    }
}
#endif