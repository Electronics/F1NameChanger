using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1_2020_Names_Changer {
	class Lookups {
		public Lookups() {
			generateReverseDicts();
		}

		public static Dictionary<string, string> firstNames = new Dictionary<string, string>(){ // from single 'unique' firstname to full Mixed/Upper standard name
			{"Carlos", "Carlos SAINZ"},
			{"Daniil", "Daniil KVYAT"},
			{"Daniel", "Daniel RICCIARDO" },
			{"Felipe", "Felipe MASSA" },
			{"Jolyon", "Jolyon PALMER" },
			{"Kimi", "Kimi RÄIKKÖNEN" },
			{"Lewis", "Lewis HAMILTON" },
			{"Marcus", "Marcus ERICSSON" },
			{"Max", "Max VERSTAPPEN" },
			{"Nico", "Nico HÜLKENBERG" },
			{"Kevin", "Kevin MAGNUSSEN" },
			{"Romain", "Romain GROSJEAN" },
			{"Sergio", "Sergio PEREZ" },
			{"Pascal", "Pascal WEHRLEIN" },
			{"Esteban", "Esteban OCON" },
			{"Stoffel", "Stoffel Vandoorne" },
			{"Lance", "Lance STROLL" },
			{"Arron", "Arron BARNES" },
			{"Martin", "Martin GILES" },
			{"Alex", "Alex MURRAY" },
			{"Lucas", "Lucas ROTH" },
			{"Igor", "Igor CORREIA" },
			{"Sophie", "Sophie FLÖRSCH" },
			{"Jonas", "Jonas SFR???" },
			{"Alain", "Alain FOREST" },
			{"Jay", "Jay LET???" },
			{"Esto", "Esto SAARI" },
			{"Yasar", "Yasar ATIYEH" },
			{"Naota", "Naota IZUMI" },
			{"Howard", "Howard CLARKE" },
			// insert more here I'm bored of typing them all out
			{"Jack", "Jack AITKEN" },
			{"George", "George RUSSELL" },
			{"Lando", "Lando NORRIS"}

		};

		public static Dictionary<string, string> shortNames = new Dictionary<string, string>() {
			{"AIT", "Jack AITKEN"},
			{"ALB", "Alexander ALBON"},
			{"ALO", "Fernando ALONSO"},
			{"BOT", "Valtteri BOTTAS"},
			{"FIT", "Pietro FITTIPALDI"},
			{"GAS", "Pierre GASLY"},
			{"GIO", "Antonio GIOVINAZZI"},
			{"GRO", "Romain GROSJEAN"},
			{"HAM", "Lewis HAMILTON"},
			{"HUL", "Nico HÜLKENBERG"},
			{"KUB", "Robert KUBICA"},
			{"KVY", "Daniil KVYAT"},
			{"LAT", "Nicholas LATIFI"},
			{"LEC", "Charles LECLERC"},
			{"MAG", "Kevin MAGNUSSEN"},
			{"MAZ", "Nikita MAZEPIN"},
			{"NOR", "Lando NORRIS"},
			{"OCO", "Esteban OCON"},
			{"PER", "Sergio PEREZ"},
			{"RAI", "Kimi RÄIKKÖNEN"},
			{"RIC", "Daniel RICCIARDO"},
			{"RUS", "George RUSSELL"},
			{"SAI", "Carlos SAINZ"},
			{"SCH", "Mick SCHUMACHER"},
			{"STR", "Lance STROLL"},
			{"TSU", "Yuki TSUNODA"},
			{"VER", "Max VERSTAPPEN"},
			{"VET", "Sebastian VETTEL"}
		};

		public static Dictionary<string, string> shortNames_rev;

		public static Dictionary<string, string> teams = new Dictionary<string, string>() { // used for looking up found team names in memory to our lookup names
			{"Alfa Romeo Racing Orlen", "Alfa Romeo"},
			{"Scuderia AlphaTauri Honda", "AlphaTauri"},
			{"Scuderia Ferrari", "Ferrari"},
			{"Haas F1 Team", "Haas"},
			{"{o:upper}M{/o}{o:lower}c{/o}{o:upper}L{/o}aren F1 Team", "McLaren"},
			{"Mercedes-AMG Petronas F1 Team", "Mercedes"},
			{"BWT Racing Point F1 Team" , "Racing Point"},
			{"Aston Martin Red Bull Racing", "Red Bull"},
			{"Renault DP World F1 Team", "Renault"},
			{"Williams Racing", "Williams"}
		};

		public static Dictionary<string, string> teams_2021Patch = new Dictionary<string, string>() { // patches onto the main teams dict if we're on the 2021 version (as we can't have duplicate entries if team names have changed)
			{"Red Bull Racing Honda", "Red Bull"},
			{"Alpine F1 Team", "Alpine"}
		};

		public static Dictionary<string, string> teams_short = new Dictionary<string, string>() { // same as above, but for the short-team names used in-game leaderboard
			{"Alfa Romeo", "Alfa Romeo"},
			{"AlphaTauri", "AlphaTauri"},
			{"Alpine", "Alpine"},
			{"Ferrari", "Ferrari" },
			{"Haas", "Haas"},
			{"{o:upper}M{/o}{o:lower}c{/o}{o:upper}L{/o}aren", "McLaren"},
			{"Mercedes-AMG Petronas", "Mercedes"},
			{"Racing Point", "Racing Point"},
			{"Red Bull Racing", "Red Bull" },
			{"Renault", "Renault"},
			{"Williams", "Williams"}
		};

		public static Dictionary<string, string> teams_rev;
		public static Dictionary<string, string> teams_short_rev;

		public static void generateReverseDicts() {
			teams_rev = teams.ToDictionary(x => x.Value, x => x.Key);
			teams_short_rev = teams_short.ToDictionary(x => x.Value, x => x.Key);
			shortNames.ToDictionary(x => x.Value, x => x.Key);
		}
	}
}
