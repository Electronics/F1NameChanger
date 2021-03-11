using System;
using System.Collections.Generic;
using System.Text;

namespace F1_2020_Names_Changer {
	class Lookups {
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
	}
}
