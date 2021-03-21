This is a memory editing tool to allow for the names in the F1 2020 game to be changed as the user wishes. The code is written in C# and is probably a little hacky in places, but should work for the most part - if it doesn't, please submit a bug report and I'll try to look at it.

I wrote this rather than using cheat engine, as I felt the scripting tools availble in cheat engine was rather limited.

# Running

Simply download the executable from the releases section, and supply a `names.json` or `names.txt` (and optional `teams.json` or `teams.txt`) file in the same directory as a lookup table. Example files are given with all the existing names pre-filled.
See known issues below on problems with longer names.

Run the executable before or after the game has started and it should do its magic! BEWARE: There is no undo button at this time! If you want to reset your driver names you will need to restart the game - no persistent changes are made to the game itself.

## Config

For team names, if you wish to have some lower-case sections of the name in the longer name, surround the particular bit with `{o:lower}` and `{/o}`. The short names will be displayed as-is with the upper/lower case as typed. Team names are (for the most part) not limited in length.

##### `.txt`

The text files are the simplest way of configuring this tool as they are basically a csv file.

The text file can simply be opened as a csv file and consists of one driver per line (in no particular driver order): `old NAME, new NAME, newDriverTag`.
The teams file similarly, is one team per line: `old team name, new team name, shortened in-game name`. 
See the example files for a list of old driver/team names.

##### `.json`

The JSON file is a bit different as `"original NAME": {"name": "new NAME", "tag": "newDriverTag"},`.
For example `Carlos SAINZ, Example DRIVER, DRV` or `"Carlos SAINZ": {"name": "Example DRIVER", "tag": "DRV"},`.
All driver names should be in the format mixed-case first name ("Carlos"), upper-case last-name ("SAINZ") but the application *should* fix any issues with this.

# Memory Locations

- **Menu Region 1**: Where the names for leaderboards are kept, they are stored in their full name format i.e. `Carlos SAINZ`. Usually in memory in the format `{o:mixed}Carlos{/o} {o:upper}SAINZ{/o}` and limited to 39 bytes in total. The `{o:mixed}` bits can be ommitted without any penalty and I use this to account for longer names. The struct that the UTF-8 encoded strings sit in is in 64-byte chunks, but can have extra stuff in (I think relating to player models?)
- **Menu Region 2**: A continuation from Menu Region 1 but with a byte limit of 44 this time, for some reason the game segments these 2 and also intersperces some team name bits at the beginning and end causing some fragmentation
- **Character Selection Region**: Where the names are stored for the character selection screen on a new game. This region of memory is sadly not very continuous and contains some duplication for unknown reasons and fragmentation of other data again. Names usually aligned every 48 byte interval. For this region I search for names rather than checking on the byte alignment due to the duplications
- **In Game Region**: This region is a large regular table with a new line of data every 32 bytes, and is 7968 bytes long. The lines *usually* alternate between firstname, lastname, driver tag (3 letters); but sometimes if a driver name is too long is ommitted and skipped. All driver names and lines of text must be 9 bytes or less.
- **Team Names**: This is less of a region and just a jumble of strings used within the game, luckily, these offsets do not move around and are set statically. No length limitations exist other than running over into other bits of menu text.

# Observations / Known Issues

- Due to how the ingame-names are stored, depending on the original length of the name (lastname usually as it shows up in the sidebar), the new name can be truncated by the game to the original name's length. As far as I can tell, this can't be easily fixed in any way (see [Reverse Engineering](Reverse%20Engineering/Reverse%20Engineering.md)) for more details on this
- Lastnames of 3 characters might cause issues being incorrectly identified as driver tags, I don't think this is an issue unless this tool is run multiple times in sucession, or in future the game adds additional drivers with these properties
- Lookups for 3 letter driver tags need to be completed
- There is no Undo button!
