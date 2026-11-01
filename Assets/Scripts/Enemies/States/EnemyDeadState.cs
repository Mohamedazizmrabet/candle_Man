using UnityEngine;

namespace Wickfall.Enemies
{
    /// <summary>Stops colliding, fades out, then destroys the enemy. Wax drops listen to Damageable.Died.</summary>
    public class EnemyDeadState : EnemyState
    {
        private float elapsed;
        private SpriteRenderer[] renderers;

        public EnemyDeadState(EnemyBrain brain) : base(brain) { }

        public override void Enter()
        {
            elapsed = 0f;
            brain.ClearTint();
            brain.DisableBody();
            renderers = brain.GetComponentsInChildren<SpriteRenderer>();
        }

        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;
            float alpha = brain.DeathFadeTime > 0f ? 1f - Mathf.Clamp01(elapsed / brain.DeathFadeTime) : 0f;

            foreach (var r in renderers)
            {
                var c = r.color;
                c.a = alpha;
                r.color = c;
            }

            if (alpha <= 0f) Object.Destroy(brain.gameObject);
        }
    }
}
