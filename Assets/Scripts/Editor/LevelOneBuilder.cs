using UnityEditor;

namespace Wickfall.EditorTools
{
    /// <summary>
    /// Menu "Tools > Wickfall > Level > Build Level 01": the first dungeon (rooms, corridors, dead ends,
    /// stairs down to Level 2). Edit <see cref="Map"/> and run the menu again; the legend is on <see cref="DungeonLevelBuilder"/>.
    /// </summary>
    public static class LevelOneBuilder
    {
        public const string ScenePath = "Assets/Scenes/lvls/01_ChampsDeSuif/Level_01.unity";

        // Top row first. One character = one 2x2-unit tile. Every row must have the same length.
        //   #  wall   (space) floor   P start   E stairs to Level 2   B brazier   D Dripling
        //   w  wax drop   C column   x crate   k skull   (full legend on DungeonLevelBuilder)
        private static readonly string[] Map =
        {
            "########################################################",
            "########################################################",
            "########################################################",
            "########################################################",
            "#############################################         ##",
            "#######       ############################### C     C ##",
            "#######     k ##########                 ####    E    ##",
            "#######  D    ##########  C           C  ####         ##",
            "#######                         k        ####         ##",
            "#######    D                                          ##",
            "####### w                    D                        ##",
            "#######       ##########                           D  ##",
            "####         ###########           D     ####  B      ##",
            "####  w    x ########### x             k ####         ##",
            "####      w  ###########                 ####         ##",
            "####         #################   #######################",
            "####    k    #################   #######################",
            "####         #################   #######################",
            "#######   ####################   #######################",
            "#######   ####################   #######################",
            "#######   ####################   #######################",
            "#######   ####################   #######################",
            "#######   ####################   #######################",
            "#######   ####################   #######################",
            "#######   ###############              #################",
            "#######   ###############  C        C  #################",
            "###            ##########              #################",
            "###            ##########    w         ########       ##",
            "###        k   ##########              ########  D  x ##",
            "###                            D       ########       ##",
            "###                                               w   ##",
            "###     P                         D                   ##",
            "###  B         ##########                             ##",
            "###            ##########  C        C  ######## x   k ##",
            "#########################              ########       ##",
            "########################################################",
            "########################################################",
            "########################################################",
        };

        [MenuItem("Tools/Wickfall/Level/Build Level 01")]
        public static void BuildLevelOne() => DungeonLevelBuilder.Build(new DungeonLevelSpec
        {
            Name = "Level 01",
            ScenePath = ScenePath,
            Map = Map,
            NextScene = "Level_02",
            BuildAfterScenePath = "Assets/Scenes/MainMenu/MainMenu.unity",
        });
    }
}
