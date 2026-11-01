using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Tools > Wickfall > Art > Import Candleman Walk Down": sets the walk sheet's import settings,
    /// slices it into 4 frames of 26x56, builds the walk and idle clips and the Candleman animator.
    /// Safe to run again after replacing the PNG.
    /// </summary>
    public static class CandlemanSpriteImporter
    {
        private const string WalkDownPath = "Assets/Art/Sprites/Player/candleman_walk_down_small.png";
        private const string FramePrefix = "candleman_walk_down";
        private const int FrameWidth = 26;
        private const int FrameHeight = 56;
        private const int FrameCount = 4;
        private const float WalkFps = 7f;

        private const string AnimDir = "Assets/Animations/Player";
        private const string WalkClipPath = AnimDir + "/Candleman_WalkDown.anim";
        private const string IdleClipPath = AnimDir + "/Candleman_Idle.anim";
        private const string ControllerPath = AnimDir + "/Candleman.controller";

        // Animator parameter names, shared with PlayerController (step 5).
        public const string ParamIsMoving = "IsMoving";
        public const string ParamMoveX = "MoveX";
        public const string ParamMoveY = "MoveY";

        [MenuItem("Tools/Wickfall/Art/Setup All Player Art", priority = 0)]
        public static void SetupAll()
        {
            ImportWalkDown();
            FlamePlaceholderGenerator.CreatePlaceholderFlame();
        }

        [MenuItem("Tools/Wickfall/Art/Import Candleman Walk Down", priority = 20)]
        public static void ImportWalkDown()
        {
            var frames = WickfallEditorUtil.ImportAndSliceRow(WalkDownPath, FrameWidth, FrameHeight, FrameCount, FramePrefix);
            if (frames.Length != FrameCount)
            {
                Debug.LogError($"[Wickfall] Expected {FrameCount} frames in {WalkDownPath}, got {frames.Length}.");
                return;
            }

            var walk = WickfallEditorUtil.CreateSpriteClip(WalkClipPath, frames, WalkFps);
            // Placeholder idle: the first walk frame, until an idle sheet exists.
            var idle = WickfallEditorUtil.CreateSpriteClip(IdleClipPath, new[] { frames[0] }, WalkFps);

            var controller = WickfallEditorUtil.LoadOrCreateController(ControllerPath, out bool created);
            if (created) BuildController(controller, idle, walk);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Wickfall] Candleman walk-down imported: {FrameCount} frames, clip at {WalkFps} fps, animator at {ControllerPath}.");
        }

        private static void BuildController(AnimatorController controller, AnimationClip idle, AnimationClip walk)
        {
            controller.AddParameter(ParamIsMoving, AnimatorControllerParameterType.Bool);
            // Unused until the up and right sheets exist; PlayerController already sets them.
            controller.AddParameter(ParamMoveX, AnimatorControllerParameterType.Float);
            controller.AddParameter(ParamMoveY, AnimatorControllerParameterType.Float);

            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle");
            idleState.motion = idle;
            var walkState = machine.AddState("Walk");
            walkState.motion = walk;
            machine.defaultState = idleState;

            var toWalk = idleState.AddTransition(walkState);
            toWalk.hasExitTime = false;
            toWalk.duration = 0f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, ParamIsMoving);

            var toIdle = walkState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, ParamIsMoving);
        }
    }
}
