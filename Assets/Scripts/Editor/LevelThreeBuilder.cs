using UnityEditor;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Tools > Wickfall > Level > Build Level 03": the Demon Gate, the last level of the demo, reached by the
    /// stairs of Level 2. Water fountains, an imp hall, a pillar maze, a vault and the Demon Lord's arena.
    /// The stairs behind the boss lead back to the main menu. Legend on <see cref="DungeonLevelBuilder"/>.
    /// </summary>
    public static class LevelThreeBuilder
    {
        public const string ScenePath = "Assets/Scenes/lvls/03_DemonGate/Level_03.unity";

        // Top row first. One character = one 2x2-unit tile. Every row must have the same length.
        //   #  wall   (space) floor   P start   E stairs (back to the main menu)   B brazier
        //   D  Dripling   K skeleton   G slime   I imp   O Demon Lord (boss)
        //   S  spike trap   T chest   M mimic   F water fountain (on the floor just below a brick wall)
        //   w  wax   C column   x crate   k skull
        private static readonly string[] Map =
        {
            "################################################################",
            "################################################################",
            "################################################################",
            "##########################    F     F     F   ##################",
            "####     F    ############                    ##################",
            "#### w        ############       k            ######         ###",
            "####          ############  C              C  ######         ###",
            "####              S   S               I       ######   T  M  ###",
            "####              S   S         I                            ###",
            "####    P         S   S                                      ###",
            "####  B     k ############         I                         ###",
            "####          ############  C           I  C  ######     K   ###",
            "##########################          w         ######  w    x ###",
            "##########################                    ######         ###",
            "##################################   ###########################",
            "##################################   ###########################",
            "##################################   ###########################",
            "##################################   ###########################",
            "##################################   ###########################",
            "##################################   ###########################",
            "####    F     ####################   ###########################",
            "####          ####################   ###########################",
            "####          ########                           ###############",
            "####  T    D  ########   G               K  D    ###############",
            "####          ########     ##    ##    ##        ###############",
            "####                       ##    ##    ##        ###############",
            "####                           K   S S S      G  ###############",
            "####        k                                    ###############",
            "####  D       ########        ##    ##    ##     ###############",
            "####          ########  w     ##    ##    ##   k ###############",
            "#### x    w   ########                           ###############",
            "####          ##############################   #################",
            "############################################   #################",
            "############################################   #################",
            "############################################   #################",
            "##########################          F               F        ###",
            "##########################                   B               ###",
            "##########################  C                             C  ###",
            "##########################       I                           ###",
            "##########################     k                             ###",
            "##########################              O                    ###",
            "##########################                        I          ###",
            "##########################  C                             C  ###",
            "##########################    E                        w     ###",
            "################################################################",
            "################################################################",
        };

        [MenuItem("Tools/Wickfall/Level/Build Level 03")]
        public static void BuildLevelThree() => DungeonLevelBuilder.Build(new DungeonLevelSpec
        {
            Name = "Level 03 (Demon Gate)",
            ScenePath = ScenePath,
            Map = Map,
            NextScene = "MainMenu",
            BuildAfterScenePath = LevelTwoBuilder.ScenePath,
            GooChance = 0.1f,
            HoleChance = 0.15f,
            FountainColor = "blue",
            Seed = 3,
        });
    }
}
