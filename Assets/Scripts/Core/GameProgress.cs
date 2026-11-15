using UnityEngine;

namespace Wickfall.Core
{
    /// <summary>Small persistent save for Continue: current scene and its start/checkpoint position.</summary>
    public static class GameProgress
    {
        private const string HasProgressKey = "wickfall.progress.has";
        private const string SceneKey = "wickfall.progress.scene";
        private const string HasSpawnKey = "wickfall.progress.spawn.has";
        private const string SpawnXKey = "wickfall.progress.spawn.x";
        private const string SpawnYKey = "wickfall.progress.spawn.y";
        private const string WonKey = "wickfall.progress.won";

        public static bool HasWon => PlayerPrefs.GetInt(WonKey, 0) == 1;
        public static string ContinueScene => HasProgress
            ? PlayerPrefs.GetString(SceneKey, "Level_01")
            : "Level_01";

        private static bool HasProgress => PlayerPrefs.GetInt(HasProgressKey, 0) == 1;

        /// <summary>Clears the old run before New Game loads its first level.</summary>
        public static void BeginNewGame()
        {
            PlayerPrefs.DeleteKey(HasProgressKey);
            PlayerPrefs.DeleteKey(SceneKey);
            PlayerPrefs.DeleteKey(HasSpawnKey);
            PlayerPrefs.DeleteKey(SpawnXKey);
            PlayerPrefs.DeleteKey(SpawnYKey);
            PlayerPrefs.DeleteKey(WonKey);
            PlayerPrefs.Save();
        }

        /// <summary>Uses a saved checkpoint in this scene, or records the scene's authored player start.</summary>
        public static Vector2 ResolveSpawn(string sceneName, Vector2 sceneStart)
        {
            if (HasProgress && PlayerPrefs.GetString(SceneKey, "") == sceneName
                && PlayerPrefs.GetInt(HasSpawnKey, 0) == 1)
            {
                return new Vector2(PlayerPrefs.GetFloat(SpawnXKey), PlayerPrefs.GetFloat(SpawnYKey));
            }

            SaveCheckpoint(sceneName, sceneStart);
            return sceneStart;
        }

        /// <summary>Records a new level as current; its P start is saved when the player spawns there.</summary>
        public static void PrepareForLevel(string sceneName)
        {
            PlayerPrefs.SetInt(HasProgressKey, 1);
            PlayerPrefs.SetString(SceneKey, sceneName);
            PlayerPrefs.SetInt(HasSpawnKey, 0);
            PlayerPrefs.Save();
        }

        /// <summary>Saves a brazier's player respawn position for both death and Continue.</summary>
        public static void SaveCheckpoint(string sceneName, Vector2 position)
        {
            PlayerPrefs.SetInt(HasProgressKey, 1);
            PlayerPrefs.SetString(SceneKey, sceneName);
            PlayerPrefs.SetInt(HasSpawnKey, 1);
            PlayerPrefs.SetFloat(SpawnXKey, position.x);
            PlayerPrefs.SetFloat(SpawnYKey, position.y);
            PlayerPrefs.Save();
        }

        public static void MarkWon()
        {
            PlayerPrefs.SetInt(WonKey, 1);
            PlayerPrefs.SetInt(HasProgressKey, 1);
            PlayerPrefs.SetString(SceneKey, "Level_03");
            PlayerPrefs.Save();
        }
    }
}
