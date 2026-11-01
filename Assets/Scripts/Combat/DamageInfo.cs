using UnityEngine;

namespace Wickfall.Combat
{
    /// <summary>Which side something fights for. A hitbox only hurts the other team.</summary>
    public enum Team
    {
        Player,
        Enemy,
    }

    /// <summary>The five flames (design doc 2.1). Carried by hits so targets can apply type multipliers later.</summary>
    public enum FlameType
    {
        Amber,
        Blue,
        Green,
        White,
        Crimson,
    }

    /// <summary>One hit: how much, from where, from which team, with which flame.</summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector2 SourcePosition;
        public readonly Team SourceTeam;
        public readonly FlameType Flame;

        public DamageInfo(float amount, Vector2 sourcePosition, Team sourceTeam, FlameType flame = FlameType.Amber)
        {
            Amount = amount;
            SourcePosition = sourcePosition;
            SourceTeam = sourceTeam;
            Flame = flame;
        }
    }
}
