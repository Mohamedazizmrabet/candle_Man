using UnityEngine;

namespace Wickfall.Core
{
    /// <summary>
    /// The save used by Continue: which level you reached and, if you rested at a brazier there, where.
    /// Only the stairs (EnterLevel) and braziers (SaveCheckpoint) write it, so playing a test scene never changes it.
    /// </summary>
    public static class GameProgress
    {
        private const string FirstLevel = "Level_01";

        private const string SceneKey = "wickfall.progress.scene";
        private const string CheckpointXKey = "wickfall.progress.spawn.x";
        private const string CheckpointYKey = "wickfall.progress.spawn.y";

        /// <summary>The level Continue loads: the last one reached, or Level 1 if there is no save.</summary>
        public static string ContinueScene
        {
            get
            {
                string scene = PlayerPrefs.GetString(SceneKey, FirstLevel);
                // Ignore anything that is not a real level (an old save from a test scene, for example).
                return scene.StartsWith("Level_") ? scene : FirstLevel;
            }
        }

        /// <summary>New Game: forget the old run.</summary>
        public static void BeginNewGame()
        {
            PlayerPrefs.DeleteKey(SceneKey);
            PlayerPrefs.DeleteKey(CheckpointXKey);
            PlayerPrefs.DeleteKey(CheckpointYKey);
            PlayerPrefs.Save();
        }

        /// <summary>Stairs: this is now the current level, starting from its entrance (no checkpoint yet).</summary>
        public static void EnterLevel(string sceneName)
        {
            PlayerPrefs.SetString(SceneKey, sceneName);
            PlayerPrefs.DeleteKey(CheckpointXKey);
            PlayerPrefs.DeleteKey(CheckpointYKey);
            PlayerPrefs.Save();
        }

        /// <summary>Brazier: respawn here after dying, and Continue here later.</summary>
        public static void SaveCheckpoint(string sceneName, Vector2 position)
        {
            PlayerPrefs.SetString(SceneKey, sceneName);
            PlayerPrefs.SetFloat(CheckpointXKey, position.x);
            PlayerPrefs.SetFloat(CheckpointYKey, position.y);
            PlayerPrefs.Save();
        }

        /// <summary>Player start: the saved checkpoint, if there is one in this level. Never writes.</summary>
        public static bool TryGetCheckpoint(string sceneName, out Vector2 position)
        {
            position = default;
            if (PlayerPrefs.GetString(SceneKey, "") != sceneName || !PlayerPrefs.HasKey(CheckpointXKey)) return false;

            position = new Vector2(PlayerPrefs.GetFloat(CheckpointXKey), PlayerPrefs.GetFloat(CheckpointYKey));
            return true;
        }
    }
}
