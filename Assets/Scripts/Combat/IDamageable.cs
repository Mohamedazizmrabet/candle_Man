namespace Wickfall.Combat
{
    /// <summary>Anything a Hitbox can hurt. Hitboxes look for it on the collider or its parents.</summary>
    public interface IDamageable
    {
        Team Team { get; }

        /// <summary>Applies the hit. Returns false if it was ignored (invulnerable, dead...).</summary>
        bool TakeHit(DamageInfo hit);
    }
}
