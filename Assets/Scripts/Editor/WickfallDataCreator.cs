using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Wickfall.Core;
using Wickfall.Core.Events;
using Wickfall.Data;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Wickfall > Create Default Data": creates every config, runtime and event channel asset
    /// phase 1 needs. Assets that already exist are left untouched, so it is safe to run again.
    /// </summary>
    public static class WickfallDataCreator
    {
        private const string PlayerDir = "Assets/Data/Player";
        private const string EnemyDir = "Assets/Data/Enemies";
        private const string EventDir = "Assets/Data/EventChannels";
        private const string InputActionsPath = "Assets/Settings/Wickfall_Input.inputactions";

        [MenuItem("Wickfall/Create Default Data")]
        public static void CreateDefaultData()
        {
            int created = 0;

            Create<PlayerConfigSO>(PlayerDir, "PlayerConfig", ref created);
            Create<LightConfigSO>(PlayerDir, "LightConfig", ref created);
            Create<DimConfigSO>(PlayerDir, "DimConfig", ref created);
            Create<PlayerRuntimeSO>(PlayerDir, "PlayerRuntime", ref created);
            AssignInputActions(Create<InputReaderSO>(PlayerDir, "InputReader", ref created));

            Create<EnemyConfigSO>(EnemyDir, "DriplingConfig", ref created);

            CreateChannel<WaxEventChannelSO>("WaxChanged", ref created,
                "Raised by WaxSystem every time wax changes (current, max). HUD, LightController and PlayerController listen.");
            CreateChannel<FloatEventChannelSO>("WaxGainRequested", ref created,
                "Raise with an amount to give the player wax (wax pickups). WaxSystem listens.");
            CreateChannel<VoidEventChannelSO>("PlayerExtinguished", ref created,
                "Raised by WaxSystem when wax reaches 0. PlayerController switches to the extinguished state.");
            CreateChannel<VoidEventChannelSO>("PlayerRespawned", ref created,
                "Raised by PlayerController after moving the player to the respawn point. WaxSystem refills to max.");
            CreateChannel<Vector3EventChannelSO>("BrazierRested", ref created,
                "Raised by a Brazier with its respawn position. WaxSystem refills, PlayerController saves the respawn point.");

            AssetDatabase.SaveAssets();
            Debug.Log($"[Wickfall] Default data ready: {created} asset(s) created in Assets/Data.");
        }

        private static T Create<T>(string dir, string name, ref int created) where T : ScriptableObject
        {
            string path = $"{dir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            WickfallEditorUtil.EnsureFolder(dir);
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created++;
            return asset;
        }

        private static void CreateChannel<T>(string name, ref int created, string description) where T : ScriptableObject
        {
            var channel = Create<T>(EventDir, name, ref created);
            var so = new SerializedObject(channel);
            var prop = so.FindProperty("description");
            if (prop != null && string.IsNullOrEmpty(prop.stringValue))
            {
                prop.stringValue = description;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AssignInputActions(InputReaderSO reader)
        {
            var so = new SerializedObject(reader);
            var prop = so.FindProperty("inputActions");
            if (prop.objectReferenceValue != null) return;

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                Debug.LogWarning($"[Wickfall] {InputActionsPath} not found. Assign it on InputReader by hand.");
                return;
            }

            prop.objectReferenceValue = actions;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
