Procedure for finding new offsets:

(All offsets we care about will be >0x2b000000, stuff below tends to be history/menu cruft)

All found offsets, subtract off ~0xb00 for randomisation.

MENU_OFFSET_START: Look for `{o:mixed}Carlos`
MENU2_OFFSET_START: Look for `{o:mixed}Daniel{/o} {o:upper}RIC` (should be ~ 0x27100 above MENU_OFFSET_START)
CHARSELECTION_OFFSET_START: Look for `Carlos SAINZ`, looking for a region with drivers ending with `.DRIVER].bk2` (or some truncation of)
INGAME_OFFSET_START: Look for `Carlos`, looking for region with 32-byte aligned rows of Firstname, Lastname, Tag
TEAMS_OFFSET_MENU_*: look for `Chief Technical Officer` or `{o:upper}M{/o}{o:lower}c{/o}{o:upper}L{/o}aren F1 Team`, it should be around this region. Have to manually go through and find each offset, trying each time as there are duplicates which the game doesn't use. It seems to almost always be the address just before 0x20000000.
TEAMS_OFFSET_GAME_*: look for `Mercedes-AMG Petronas` nearest 0x20000000 (should be around TEAMS_OFFSET_MENU stuff), again manual finding
