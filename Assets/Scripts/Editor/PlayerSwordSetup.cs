using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Wickfall.Player;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Tools > Wickfall > Player > Add Sword": gives Candleman the 0x72 sword (CC0) and sets the attack hitbox
    /// to the blade's reach. Edits the existing Player prefab in place, so other changes to it are kept. Safe to run again.
    /// </summary>
    public static class PlayerSwordSetup
    {
        private const string SwordSprite = "weapon_regular_sword";
        private const float SwordScale = 0.8f;

        // Attack reach: the hitbox sits in front of the pivot, along the facing direction.
        private static readonly Vector3 PivotPosition = new Vector3(0f, 0.9f, 0f);
        private static readonly Vector3 HitboxOffset = new Vector3(1.2f, 0f, 0f);
        private static readonly Vector2 HitboxSize = new Vector2(1.8f, 2f);

        [MenuItem("Tools/Wickfall/Player/Add Sword")]
        public static void AddSwordToPlayerPrefab()
        {
            if (AddSword(TestSceneBuilder.PlayerPrefabPath))
            {
                Debug.Log($"[Wickfall] Sword added to {TestSceneBuilder.PlayerPrefabPath}.");
            }
        }

        public static bool AddSword(string prefabPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogError($"[Wickfall] No Player prefab at {prefabPath}. Build the test scene or Level 01 first.");
                return false;
            }

            DungeonArtImporter.Sprites().TryGetValue(SwordSprite, out var swordSprite);
            if (swordSprite == null)
            {
                Debug.LogError($"[Wickfall] Sprite {SwordSprite} not found: import the dungeon tileset first (Tools > Wickfall > Art).");
                return false;
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                // All of Candleman's parts sort together, by his feet, against the rest of the world.
                if (root.GetComponent<SortingGroup>() == null) root.AddComponent<SortingGroup>();

                var pivot = root.transform.Find("AttackPivot");
                var hitbox = root.transform.Find("AttackPivot/AttackHitbox");
                if (pivot != null) pivot.localPosition = PivotPosition;
                if (hitbox != null)
                {
                    hitbox.localPosition = HitboxOffset;
                    var box = hitbox.GetComponent<BoxCollider2D>();
                    if (box != null) box.size = HitboxSize;

                    // The orange placeholder box is replaced by the sword.
                    var preview = hitbox.Find("SwingPreview");
                    if (preview != null) Object.DestroyImmediate(preview.gameObject);
                }

                var sword = root.transform.Find("Sword");
                if (sword == null)
                {
                    sword = new GameObject("Sword").transform;
                    sword.SetParent(root.transform, false);
                }
                sword.localScale = new Vector3(SwordScale, SwordScale, 1f);
                // Explicit checks: Unity's "fake null" from GetComponent defeats the ?? operator.
                var swordRenderer = sword.GetComponent<SpriteRenderer>();
                if (swordRenderer == null) swordRenderer = sword.gameObject.AddComponent<SpriteRenderer>();
                swordRenderer.sprite = swordSprite;

                var visual = root.GetComponent<SwordVisual>();
                if (visual == null) visual = root.AddComponent<SwordVisual>();
                var so = new SerializedObject(visual);
                so.FindProperty("player").objectReferenceValue = root.GetComponent<PlayerController>();
                so.FindProperty("sword").objectReferenceValue = sword;
                so.FindProperty("swordRenderer").objectReferenceValue = swordRenderer;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
