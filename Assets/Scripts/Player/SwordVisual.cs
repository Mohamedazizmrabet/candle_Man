using UnityEngine;

namespace Wickfall.Player
{
    /// <summary>
    /// Candleman's sword. At rest it hangs from his free hand; during an attack it sweeps an arc
    /// across the direction he faces. Purely visual: the damage comes from the AttackHitbox.
    /// The sword sprite's pivot is at the hilt and the blade points up (+Y) with no rotation.
    /// </summary>
    public class SwordVisual : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform sword;
        [SerializeField] private SpriteRenderer swordRenderer;

        [Header("At rest")]
        [Tooltip("Hand position, from the player's feet.")]
        [SerializeField] private Vector2 restPosition = new Vector2(0.5f, 1.2f);
        [Tooltip("Blade angle at rest (0 = pointing up, 180 = pointing down).")]
        [SerializeField] private float restAngle = 160f;

        [Header("Swing")]
        [Tooltip("Where the hilt sits during a swing, from the player's feet.")]
        [SerializeField] private Vector2 swingCenter = new Vector2(0f, 1.1f);
        [Tooltip("How far the hilt moves toward the facing direction.")]
        [SerializeField] private float swingReach = 0.5f;
        [Tooltip("Total arc in degrees, centered on the facing direction.")]
        [SerializeField] private float swingArc = 140f;

        [Header("Draw order (inside the player's Sorting Group)")]
        [SerializeField] private int orderInFront = 2;
        [SerializeField] private int orderBehind = -1;

        private void Reset() => player = GetComponent<PlayerController>();

        private void LateUpdate()
        {
            if (player == null || sword == null) return;

            float progress = player.AttackProgress;
            if (progress < 0f)
            {
                sword.localPosition = restPosition;
                sword.localRotation = Quaternion.Euler(0f, 0f, restAngle);
                SetOrder(player.Facing.y > 0.5f);
                return;
            }

            Vector2 dir = player.AttackDirection;
            // Blade angle that points along the facing direction (sprite points up at 0).
            float facingAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            // Ease out: fast start, slowing at the end of the arc.
            float t = 1f - (1f - progress) * (1f - progress);
            float angle = facingAngle + Mathf.Lerp(swingArc / 2f, -swingArc / 2f, t);

            sword.localPosition = swingCenter + dir * swingReach;
            sword.localRotation = Quaternion.Euler(0f, 0f, angle);
            // Swinging upward, the blade passes behind him.
            SetOrder(dir.y > 0.5f);
        }

        private void SetOrder(bool behind)
        {
            if (swordRenderer != null) swordRenderer.sortingOrder = behind ? orderBehind : orderInFront;
        }
    }
}
