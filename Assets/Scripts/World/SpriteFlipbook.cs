using UnityEngine;

namespace Wickfall.World
{
    /// <summary>Loops a few sprites on a SpriteRenderer (fountains, torches...). No Animator needed.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(0.1f)] private float framesPerSecond = 6f;
        [Tooltip("Start at a random frame so neighbours don't animate in lockstep.")]
        [SerializeField] private bool randomStart = true;

        private SpriteRenderer spriteRenderer;
        private float offset;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (randomStart) offset = Random.value * 10f;
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0) return;
            int index = (int)((Time.time + offset) * framesPerSecond) % frames.Length;
            spriteRenderer.sprite = frames[index];
        }
    }
}
