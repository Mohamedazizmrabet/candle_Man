using UnityEngine;
using Wickfall.Combat;

namespace Wickfall.Player
{
    /// <summary>
    /// Lets enemy hitboxes hurt Candleman. The player has no health of its own:
    /// a hit is passed to PlayerController, which removes wax and starts the hurt state.
    /// </summary>
    public class PlayerHurtbox : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerController playerController;

        public Team Team => Team.Player;

        private void Reset() => playerController = GetComponent<PlayerController>();

        public bool TakeHit(DamageInfo hit) =>
            hit.SourceTeam != Team.Player
            && playerController != null
            && playerController.ApplyHit(hit.Amount, hit.SourcePosition);
    }
}
