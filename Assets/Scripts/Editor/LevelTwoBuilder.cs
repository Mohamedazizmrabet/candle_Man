using UnityEditor;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Tools > Wickfall > Level > Build Level 02": the Sunken Crypt, reached by the stairs of Level 1.
    /// Slime-covered and broken walls, blood fountains, spike traps, chests and mimics, skeletons and slimes.
    /// Edit <see cref="Map"/> and run the menu again; the legend is on <see cref="DungeonLevelBuilder"/>.
    /// </summary>
    public static class LevelTwoBuilder
    {
        public const string ScenePath = "Assets/Scenes/lvls/02_SunkenCrypt/Level_02.unity";

        // Top row first. One character = one 2x2-unit tile. Every row must have the same length.
        //   #  wall   (space) floor   P start   E stairs to Level 3   B brazier
        //   D  Dripling   K skeleton   G slime   S spike trap   T chest   M mimic
        //   F  blood fountain (on the floor just below a brick wall)   w wax   C column   x crate   k skull
        private static readonly string[] Map =
        {
            "############################################################",
            "############################################################",
            "############################################################",
            "###########################       F        #################",
            "###      F   ##############                ######        ###",
            "### w        ##############  C          C  ###### k      ###",
            "###             S  S  S                               T  ###",
            "###             S  S  S         K                        ###",
            "###    P        S  S  S                  x               ###",
            "###  B     k ##############           K    ######   G  x ###",
            "###          ##############  C          C  ######        ###",
            "###########################    w   k       #################",
            "###########################                #################",
            "#################################   ########################",
            "#################################   ########################",
            "#################################   ########################",
            "#################################   ########################",
            "#################################   ########################",
            "#################################   ########################",
            "#################################   ########################",
            "####     F     ##################   ########################",
            "####           ###########                   ###############",
            "####  T     M  ########### C          D    w ###############",
            "####           ###########   S S S           ###############",
            "####                                    M    ###############",
            "####                          S S S          ###############",
            "####                                      D  ###############",
            "####     K     ###########   S S S  k        ###############",
            "####           ###########                  x###############",
            "#### k       w #########################   #################",
            "####           #########################   #################",
            "########################################   #################",
            "##############################                          C ##",
            "############################## C              G     x     ##",
            "##############################        G         w         ##",
            "##############################   B                     E  ##",
            "##############################              k     K       ##",
            "##############################                            ##",
            "############################################################",
            "############################################################",
        };

        [MenuItem("Tools/Wickfall/Level/Build Level 02")]
        public static void BuildLevelTwo() => DungeonLevelBuilder.Build(new DungeonLevelSpec
        {
            Name = "Level 02 (Sunken Crypt)",
            ScenePath = ScenePath,
            Map = Map,
            NextScene = "Level_03",
            BuildAfterScenePath = LevelOneBuilder.ScenePath,
            GooChance = 0.15f,
            HoleChance = 0.1f,
            Seed = 2,
        });

        /// <summary>Builds every level in order in one go (used for batch builds too).</summary>
        [MenuItem("Tools/Wickfall/Level/Build All Levels")]
        public static void BuildAll()
        {
            LevelOneBuilder.BuildLevelOne();
            BuildLevelTwo();
            LevelThreeBuilder.BuildLevelThree();
        }
    }
}
