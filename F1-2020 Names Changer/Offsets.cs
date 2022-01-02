using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F1_2020_Names_Changer {
	static class Offsets {

		public static IntPtr SEARCH_START = (IntPtr)0x1b000000000; // used for searching for below offsets if they don't work
        public static IntPtr SEARCH_START_ALT = (IntPtr)0x21000000000;
        public static IntPtr SEARCH_START_F12021 = (IntPtr)0x120000000;

        // these are addresses at which this program will start to search for the below search strings to identify the where the actual start of the region is
        public static IntPtr MENU_OFFSET_START;
        public static IntPtr MENU2_OFFSET_START;
        public static IntPtr CHARSELECTION_OFFSET_START;
        public static IntPtr INGAME_OFFSET_START;
        public static IntPtr TEAMS_OFFSET_START; // NB: doesn't include racing point

        // teams offset separately stated as there doesn't seem to be much order to how they're organised. Plus the memory locations are static
        public static IntPtr TEAMS_OFFSET_MENU_RACING_POINT;
        public static IntPtr TEAMS_OFFSET_MENU_MERCEDES;
        public static IntPtr TEAMS_OFFSET_MENU_FERRARI;
        public static IntPtr TEAMS_OFFSET_MENU_RED_BULL;
        public static IntPtr TEAMS_OFFSET_MENU_ALPHA_TAURI;
        public static IntPtr TEAMS_OFFSET_MENU_RENAULT;
        public static IntPtr TEAMS_OFFSET_MENU_ALFA_ROMEO;
        public static IntPtr TEAMS_OFFSET_MENU_WILLIAMS;
        public static IntPtr TEAMS_OFFSET_MENU_HAAS;
        public static IntPtr TEAMS_OFFSET_MENU_MCLAREN;

        public static IntPtr TEAMS_OFFSET_GAME_RACING_POINT;
        public static IntPtr TEAMS_OFFSET_GAME_MERCEDES;
        public static IntPtr TEAMS_OFFSET_GAME_FERRARI;
        public static IntPtr TEAMS_OFFSET_GAME_RED_BULL;
        public static IntPtr TEAMS_OFFSET_GAME_ALPHA_TAURI;
        public static IntPtr TEAMS_OFFSET_GAME_RENAULT;
        public static IntPtr TEAMS_OFFSET_GAME_ALFA_ROMEO;
        public static IntPtr TEAMS_OFFSET_GAME_WILLIAMS;
        public static IntPtr TEAMS_OFFSET_GAME_HAAS;
        public static IntPtr TEAMS_OFFSET_GAME_MCLAREN;

        public static void save() {
            Dictionary<string, IntPtr> data = new Dictionary<string, IntPtr>() {
                { "menuOffset", MENU_OFFSET_START },
                { "menuOffset2", MENU2_OFFSET_START },
                { "charOffset", CHARSELECTION_OFFSET_START },
                {"gameOffset", INGAME_OFFSET_START },
                {"teamsOffset", TEAMS_OFFSET_START },
                {"racingPointMenu", TEAMS_OFFSET_MENU_RACING_POINT },
                {"racingPointGame", TEAMS_OFFSET_GAME_RACING_POINT }
            };
            string json = JsonConvert.SerializeObject(data);
            System.IO.File.WriteAllText(@"offsets.json", json);
        }

        public static bool load() {
            string jsonStr = System.IO.File.ReadAllText(@"offsets.json");
            dynamic json = JsonConvert.DeserializeObject(jsonStr);
            MENU_OFFSET_START = (IntPtr)(long)json.menuOffset.value;
            MENU2_OFFSET_START = (IntPtr)(long)json.menuOffset2.value;
            CHARSELECTION_OFFSET_START = (IntPtr)(long)json.charOffset.value;
            INGAME_OFFSET_START = (IntPtr)(long)json.gameOffset.value;
            TEAMS_OFFSET_START = (IntPtr)(long)json.teamsOffset.value;
            TEAMS_OFFSET_MENU_RACING_POINT = (IntPtr)(long)json.racingPointMenu.value;
            TEAMS_OFFSET_GAME_RACING_POINT = (IntPtr)(long)json.racingPointGame.value;
            return true;
        }

        public static void loadDX12() {
            MENU_OFFSET_START = (IntPtr)0x2b1d12500;
            MENU2_OFFSET_START = (IntPtr)0x2b1f7f800;
            CHARSELECTION_OFFSET_START = (IntPtr)0x2b1b57000;
            INGAME_OFFSET_START = (IntPtr)0x2b18f5000;
            TEAMS_OFFSET_MENU_RACING_POINT = (IntPtr)0x1942e67cd;
            TEAMS_OFFSET_MENU_MERCEDES = (IntPtr)0x194350b33;
            TEAMS_OFFSET_MENU_FERRARI = (IntPtr)0x194350be5;
            TEAMS_OFFSET_MENU_RED_BULL = (IntPtr)0x194350db2;
            TEAMS_OFFSET_MENU_ALPHA_TAURI = (IntPtr)0x194350f1b;
            TEAMS_OFFSET_MENU_RENAULT = (IntPtr)0x19435100f;
            TEAMS_OFFSET_MENU_ALFA_ROMEO = (IntPtr)0x194351164;
            TEAMS_OFFSET_MENU_WILLIAMS = (IntPtr)0x194351248;
            TEAMS_OFFSET_MENU_HAAS = (IntPtr)0x19435141d;
            TEAMS_OFFSET_MENU_MCLAREN = (IntPtr)0x19435131c;
            TEAMS_OFFSET_GAME_RACING_POINT = (IntPtr)0x1942e6d54;
            TEAMS_OFFSET_GAME_MERCEDES = (IntPtr)0x194351232;
            TEAMS_OFFSET_GAME_FERRARI = (IntPtr)0x194350df6;
            TEAMS_OFFSET_GAME_RED_BULL = (IntPtr)0x194350a80;
            TEAMS_OFFSET_GAME_ALPHA_TAURI = (IntPtr)0x194350cf5;
            TEAMS_OFFSET_GAME_RENAULT = (IntPtr)0x194350e69;
            TEAMS_OFFSET_GAME_ALFA_ROMEO = (IntPtr)0x194350f84;
            TEAMS_OFFSET_GAME_WILLIAMS = (IntPtr)0x194350ab5;
            TEAMS_OFFSET_GAME_HAAS = (IntPtr)0x194350b09;
            TEAMS_OFFSET_GAME_MCLAREN = (IntPtr)0x194351273;
            TEAMS_OFFSET_START = TEAMS_OFFSET_GAME_RED_BULL;
        }

        public static void loadDX11() {
            MENU_OFFSET_START = (IntPtr)0x2b13d2000;
            MENU2_OFFSET_START = (IntPtr)0x2b1643200;
            CHARSELECTION_OFFSET_START = (IntPtr)0x2b1217100;
            INGAME_OFFSET_START = (IntPtr)0x2b0fb6900;
            TEAMS_OFFSET_MENU_RACING_POINT = (IntPtr)0x1939a67cd;
            TEAMS_OFFSET_MENU_MERCEDES = (IntPtr)0x193a10b33;
            TEAMS_OFFSET_MENU_FERRARI = (IntPtr)0x193a10be5;
            TEAMS_OFFSET_MENU_RED_BULL = (IntPtr)0x193a10db2;
            TEAMS_OFFSET_MENU_ALPHA_TAURI = (IntPtr)0x193a10f1b;
            TEAMS_OFFSET_MENU_RENAULT = (IntPtr)0x193a1100f;
            TEAMS_OFFSET_MENU_ALFA_ROMEO = (IntPtr)0x193a11164;
            TEAMS_OFFSET_MENU_WILLIAMS = (IntPtr)0x193a11248;
            TEAMS_OFFSET_MENU_HAAS = (IntPtr)0x193a1141d;
            TEAMS_OFFSET_MENU_MCLAREN = (IntPtr)0x193a1131c;
            TEAMS_OFFSET_GAME_RACING_POINT = (IntPtr)0x1939a6d54;
            TEAMS_OFFSET_GAME_MERCEDES = (IntPtr)0x193a11232;
            TEAMS_OFFSET_GAME_FERRARI = (IntPtr)0x193a10df6;
            TEAMS_OFFSET_GAME_RED_BULL = (IntPtr)0x193a10a80;
            TEAMS_OFFSET_GAME_ALPHA_TAURI = (IntPtr)0x193a10cf5;
            TEAMS_OFFSET_GAME_RENAULT = (IntPtr)0x193a10e69;
            TEAMS_OFFSET_GAME_ALFA_ROMEO = (IntPtr)0x193a10f84;
            TEAMS_OFFSET_GAME_WILLIAMS = (IntPtr)0x193a10ab5;
            TEAMS_OFFSET_GAME_HAAS = (IntPtr)0x193a10b09;
            TEAMS_OFFSET_GAME_MCLAREN = (IntPtr)0x193a11273;
            TEAMS_OFFSET_START = TEAMS_OFFSET_GAME_RED_BULL;
        }
    }
}
