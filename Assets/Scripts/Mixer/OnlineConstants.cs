using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Mixer
{
    public class OnlineConstants
    {
        public static String GROUP_START = "start";
        public static String GROUP_CONTROLS = "controls";
        public static String GROUP_VIEWERS = "viewers";
        public static String GROUP_HELP = "help";

        public static String SCENE_CONTROLS = "playerControls";
        public static String SCENE_LOBBY = "lobby";
        public static String SCENE_HELP = "giveHelp";

        public static String CONTROL_FORWARD = "forward";
        public static String CONTROL_LEFT = "left";
        public static String CONTROL_BACK = "back";
        public static String CONTROL_RIGHT = "right";
        public static String CONTROL_FIRE = "fire";

        public static String CONTROL_P1_JOIN = "joinPlayer1";
        public static String CONTROL_P2_JOIN = "joinPlayer2";
        public static String CONTROL_STATUS = "statusUpdate";

        public static String CONTROL_HELP_RED = "helpRed";
        public static String CONTROL_HELP_BLUE = "helpBlue";
        public static String CONTROL_INFO_UPDATE = "infoUpdate";
    }
}
