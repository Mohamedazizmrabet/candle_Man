using System;
using UnityEngine;

namespace Wickfall.Core
{
    /// <summary>
    /// Live player data shared with other systems (enemies read it every frame).
    /// Only the player's own components write here. Values are not saved into the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Wickfall/Runtime/Player Runtime", fileName = "PlayerRuntime")]
    public class PlayerRuntimeSO : ScriptableObject
    {
        [NonSerialized] public bool IsRegistered;
        [NonSerialized] public Vector2 Position;
        [NonSerialized] public float LightRadius;
        [NonSerialized] public float DetectionMultiplier = 1f;
        [NonSerialized] public bool IsDimmed;
        [NonSerialized] public bool IsAlive;

        public void ResetValues()
        {
            IsRegistered = false;
            Position = Vector2.zero;
            LightRadius = 0f;
            DetectionMultiplier = 1f;
            IsDimmed = false;
            IsAlive = false;
        }

        private void OnEnable() => ResetValues();
    }
}
