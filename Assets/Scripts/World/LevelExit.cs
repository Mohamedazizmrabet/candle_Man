using UnityEngine;
using Wickfall.Core;

namespace Wickfall.World
{
    /// <summary>
    /// Stairs down to the next level: walking onto them fades out, loads the next scene and fades in.
    /// The next scene must be in File > Build Profiles > Scene List.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        [SerializeField] private string nextScene = "Level_02";
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.8f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.8f;

        private bool used;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Only the player's body (not his sword hitbox), and only once.
            if (used || other.isTrigger || !other.CompareTag("Player")) return;
            if (SceneFader.FadeToScene(nextScene, fadeOutSeconds, fadeInSeconds)) used = true;
        }
    }
}
