using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace F1_2020_Names_Changer {
    class Program {

        static Dictionary<string, string> nameLookup = new Dictionary<string, string>();
        static Dictionary<string, string> nameLookup_short = new Dictionary<string, string>();

        static void cwc(String s, ConsoleColor fg, ConsoleColor bg = ConsoleColor.Black) {
            ConsoleColor bg_ = Console.BackgroundColor;
            ConsoleColor fg_ = Console.ForegroundColor;
            Console.BackgroundColor = bg;
            Console.ForegroundColor = fg;
            Console.Write(s);
            Console.BackgroundColor = bg_;
            Console.ForegroundColor = fg_;
            Console.WriteLine();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);


        const int PROCESS_WM_READ = 0x0010;
        const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        // these are addresses at which this program will start to search for the below search strings to identify the where the actual start of the region is
        const Int64 MENU_OFFSET_START = 0x2b0cc2500; // 0x2b0cc2542
        const Int64 MENU2_OFFSET_START = 0x2b0f32000;
        const Int64 CHARSELECTION_OFFSET_START = 0x2b0b07920;
        const Int64 INGAME_OFFSET_START = 0x2b08a7000;

        static readonly byte[] MENU_SEARCH_STR = Encoding.UTF8.GetBytes("{o:mixed}"); // GetEncoding(437)?! this is an encoding that doesn't mangle weird non-UTF8-characters like 255 and 128!
        static readonly byte[] CHARSELECTION_SEARCH_STR = Encoding.UTF8.GetBytes("Mr HEINZ"); // he's always first, well, in some ways
        static readonly byte[] INGAME_SEARCH_STR = Encoding.UTF8.GetBytes("Carlos");

        static void Main(string[] args) {

            loadLookupTables();

            if (Process.GetProcessesByName("F1_2020_dx12").Length < 1) {
                Console.WriteLine("F1 Process not detected, waiting for game to be started");
                while (Process.GetProcessesByName("F1_2020_dx12").Length < 1) {
                    System.Threading.Thread.Sleep(1000);
                }
                Console.WriteLine("F1 Process detected, waiting 20 seconds or so for game to get ready");
                System.Threading.Thread.Sleep(20000);
            }
            Process process = Process.GetProcessesByName("F1_2020_dx12")[0]; // Get the F1 process
            IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
            cwc("F1 Process detected", ConsoleColor.Green);


            // First off, let's find the start of the menu section
            IntPtr bytesRead = IntPtr.Zero;
            byte[] buffer = new byte[12000];
            ReadProcessMemory((IntPtr)processHandle, (IntPtr)MENU_OFFSET_START, buffer, buffer.Length, out bytesRead);
            cwc($"Read {bytesRead} bytes of RAM at {MENU_OFFSET_START:X}(Menu Region 1)", ConsoleColor.Blue, ConsoleColor.DarkYellow);


            //temppppp
            //byte[] buffer = File.ReadAllBytes(@"C:\Users\Laurie\Desktop\F1RevEng\menuMemoryRegion.hex");


            int menuOffsetAdditional = Search(buffer, MENU_SEARCH_STR); // search because Array.indexOf sucks
            if (menuOffsetAdditional < 0) {
                cwc("Failed to find Menu Region 1", ConsoleColor.Red);
            } else {
                cwc($"Found Menu Memory Region 1 Offset: {menuOffsetAdditional}", ConsoleColor.Green);

                byte[] memOut = parseMenuMemoryRegion(buffer.Skip(menuOffsetAdditional).ToArray(), 39);
                // write out the memory
                cwc("Writing new menu memory region 1 to RAM...", ConsoleColor.Blue, ConsoleColor.DarkYellow);
                IntPtr bytesWritten = IntPtr.Zero;
                WriteProcessMemory((IntPtr)processHandle, (IntPtr)MENU_OFFSET_START + menuOffsetAdditional, memOut, memOut.Length, out bytesWritten);
                cwc($"Written {bytesWritten} bytes to RAM at {MENU_OFFSET_START + menuOffsetAdditional:X}(Menu Region 1)", ConsoleColor.Blue, ConsoleColor.DarkYellow);
            }

            // ---------------- Now for menu bit part 2 --------------
            Array.Clear(buffer, 0, buffer.Length);


            ReadProcessMemory((IntPtr)processHandle, (IntPtr)MENU2_OFFSET_START, buffer, buffer.Length, out bytesRead);
            cwc($"Read {bytesRead} bytes of RAM at {MENU2_OFFSET_START:X}(Menu Region 2)", ConsoleColor.Blue, ConsoleColor.DarkYellow);


            //temppppp
            //byte[] buffer = File.ReadAllBytes(@"C:\Users\Laurie\Desktop\F1RevEng\menuMemoryRegion.hex");


            menuOffsetAdditional = Search(buffer, MENU_SEARCH_STR); // search because Array.indexOf sucks
            if (menuOffsetAdditional < 0) {
                cwc("Failed to find Menu Region 2", ConsoleColor.Red);
            } else {
                cwc($"Found Menu Memory Region 2 Offset: {menuOffsetAdditional}", ConsoleColor.Green);

                byte[] memOut = parseMenuMemoryRegion(buffer.Skip(menuOffsetAdditional).ToArray(), 44); // yup, this memory region has different size structs, go figure
                // write out the memory
                cwc("Writing new menu memory region 2 to RAM...", ConsoleColor.Blue, ConsoleColor.DarkYellow);
                IntPtr bytesWritten = IntPtr.Zero;
                WriteProcessMemory((IntPtr)processHandle, (IntPtr)MENU2_OFFSET_START + menuOffsetAdditional, memOut, memOut.Length, out bytesWritten);
                cwc($"Written {bytesWritten} bytes to RAM at {MENU2_OFFSET_START + menuOffsetAdditional:X}(Menu Region 2)", ConsoleColor.Blue, ConsoleColor.DarkYellow);
            }


            // ---------------- Now for in character selection section --------------
            Array.Clear(buffer, 0, buffer.Length);


            ReadProcessMemory((IntPtr)processHandle, (IntPtr)CHARSELECTION_OFFSET_START, buffer, buffer.Length, out bytesRead);
            cwc($"Read {bytesRead} bytes of RAM at {CHARSELECTION_OFFSET_START:X}(Character Selection Region)", ConsoleColor.Blue, ConsoleColor.DarkYellow);


            //temppppp
            //byte[] buffer = File.ReadAllBytes(@"C:\Users\Laurie\Desktop\F1RevEng\menuMemoryRegion.hex");


            menuOffsetAdditional = Search(buffer, CHARSELECTION_SEARCH_STR); // search because Array.indexOf sucks
            if (menuOffsetAdditional < 0) {
                cwc("Failed to find Character Selection Region", ConsoleColor.Red);
            } else {
                cwc($"Found Character Selection Region: {menuOffsetAdditional}", ConsoleColor.Green);

                byte[] memOut = parseCharSelection(buffer.Skip(menuOffsetAdditional).ToArray());
                // write out the memory
                cwc("Writing new Character Selection Region to RAM...", ConsoleColor.Blue, ConsoleColor.DarkYellow);
                IntPtr bytesWritten = IntPtr.Zero;
                WriteProcessMemory((IntPtr)processHandle, (IntPtr)CHARSELECTION_OFFSET_START + menuOffsetAdditional, memOut, memOut.Length, out bytesWritten);
                cwc($"Written {bytesWritten} bytes to RAM at {CHARSELECTION_OFFSET_START + menuOffsetAdditional:X}(Character Selection Region)", ConsoleColor.Blue, ConsoleColor.DarkYellow);
            }

            // ---------------- Now for in game section --------------
            Array.Clear(buffer, 0, buffer.Length);


            ReadProcessMemory((IntPtr)processHandle, (IntPtr)INGAME_OFFSET_START, buffer, buffer.Length, out bytesRead);
            cwc($"Read {bytesRead} bytes of RAM at {INGAME_OFFSET_START:X}(Game region)", ConsoleColor.Blue, ConsoleColor.DarkYellow);


            //temppppp
            //byte[] buffer = File.ReadAllBytes(@"C:\Users\Laurie\Desktop\F1RevEng\menuMemoryRegion.hex");


            menuOffsetAdditional = Search(buffer, INGAME_SEARCH_STR); // search because Array.indexOf sucks
            if (menuOffsetAdditional < 0) {
                cwc("Failed to find Game region", ConsoleColor.Red);
            } else {
                cwc($"Found Game region: {menuOffsetAdditional}", ConsoleColor.Green);

                byte[] memOut = parseInGame(buffer.Skip(menuOffsetAdditional).ToArray());
                // write out the memory
                cwc("Writing new Game region to RAM...", ConsoleColor.Blue, ConsoleColor.DarkYellow);
                IntPtr bytesWritten = IntPtr.Zero;
                WriteProcessMemory((IntPtr)processHandle, (IntPtr)INGAME_OFFSET_START + menuOffsetAdditional, memOut, memOut.Length, out bytesWritten);
                cwc($"Written {bytesWritten} bytes to RAM at {INGAME_OFFSET_START + menuOffsetAdditional:X}(Game region)", ConsoleColor.Blue, ConsoleColor.DarkYellow);
            }



            Console.ReadLine();
        }

        static int Search(byte[] src, byte[] pattern) {
            int c = src.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < c; i++) {
                if (src[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && src[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }

        static byte[] parseMenuMemoryRegion(byte[] buffer, int nameSize = 39) {
            // we know for this first memory region, all the lines are memory aligned by 64 bytes starting with the {o:mixed}[firstname]{/o} {o:upper}[lastname]{/o}
            // with a maximum total length of this section of 39 bytes as is cut by the game (doesn't have to be null termianted if it hits this limit?
            //  > 39 bytes including {} stuff, 12 bytes of firstname+lastname?
            // OR it seems the {o:} stuff can be removed as long as text is formatted correctly and accepts full 39 bytes

            // after the first instance, the drivers are basically in a 64 byte recurring struct with 38 bytes reserved for the string (or more accurately 38 bytes we can push into)
            int ptr = 0;
            int lastNamePtr = 0;

            while (ptr < buffer.Length) {

                // take the string bit and decode it so we can do string stuff with it
                String oldDriver = Encoding.UTF8.GetString(buffer.Skip(ptr).Take(nameSize).ToArray()).Replace("\0", string.Empty).Trim();
                // now strip out the name between the {} crap
                var tempSplit = oldDriver.Split("{o:mixed}");
                if (tempSplit.Length <= 1) {
                    goto SKIP;
                }
                oldDriver = tempSplit[1];

                var nameSplit = oldDriver.Split("{/o}");
                String firstName = nameSplit[0];
                String lastName = nameSplit[1].Split(@"{o:upper}")[1];
                cwc($"PARSEMENU: Found memory region for {firstName} {lastName}", ConsoleColor.Cyan);

                // we can now lookup the name in a lookup table and replace it
                var rName = lookupName(firstName, lastName);

                if (!String.IsNullOrEmpty(rName)) {
                    String newName = generateMenuName(rName.Split(" ")[0], rName.Split(" ")[1], nameSize); // add the new menu item
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(newName), 0, buffer, ptr, nameSize);
                }
                lastNamePtr = ptr + nameSize;

                SKIP:
                ptr += 64;
            }

            return buffer.Take(lastNamePtr).ToArray();
        }

        static byte[] parseCharSelection(byte[] buffer) {
            // sadly the char selection memory region is not nicely serparated, *most* drivers are padded with DRIVER].bk2 but not all so instead we'll do what I probably should have in the other parser and just search for names from our lookup table in the memory region and replace them
            // saying this, every driver except for SAINZ occurs at a 2x48-byte interval (probably the size of the struct used)

            int lastNamePtr = 0; // I like this to keep writing too far into RAM accidentally, although this doesn't work great in this case as we're jumping all over the place
            foreach (KeyValuePair<string, string> driver in nameLookup) {
                cwc($"CHARSELECT: Searching for {driver.Key}...", ConsoleColor.Cyan);
                int ptr = 0;
                while ((ptr = Search(buffer, Encoding.UTF8.GetBytes(driver.Key))) >= 0) {
                    cwc($"\tCHARSELECT: Found, replacing with {driver.Value}", ConsoleColor.Green);

                    // make sure we can fit it
                    byte[] newDriver = Encoding.UTF8.GetBytes(driver.Value);
                    if (newDriver.Length <= 24) {
                        Array.Clear(buffer, ptr, 24);
                        Buffer.BlockCopy(newDriver, 0, buffer, ptr, newDriver.Length);
                        // I think it's ok to overwrite all instances of the name, but there is typically 3 for each driver:
                        // the first padded as mentioned, is the one used in rendering
                        // the other two I'm not sure about though
                        if (ptr + driver.Value.Length > lastNamePtr) {
                            lastNamePtr = ptr + 24;
                        }
                    } else {
                        cwc($"\tCHARSELECT: Driver name too long to fit in Character selection memory! ({driver.Value})", ConsoleColor.Red);
                    }
                }

                // now search for just the lastnames!
                ptr = 0;
                while ((ptr = Search(buffer, Encoding.UTF8.GetBytes(driver.Key.Split(" ")[1]))) >= 0) {
                    cwc($"\tCHARSELECT: Found, replacing with {driver.Value.Split(" ")[1]}", ConsoleColor.Green);

                    // make sure we can fit it
                    byte[] newDriver = Encoding.UTF8.GetBytes(driver.Value.Split(" ")[1]);
                    if (newDriver.Length <= 24) {
                        Array.Clear(buffer, ptr, 24);
                        Buffer.BlockCopy(newDriver, 0, buffer, ptr, newDriver.Length);
                        // I think it's ok to overwrite all instances of the name, but there is typically 3 for each driver:
                        // the first padded as mentioned, is the one used in rendering
                        // the other two I'm not sure about though
                        if (ptr + driver.Value.Length > lastNamePtr) {
                            lastNamePtr = ptr + 24;
                        }
                    } else {
                        cwc($"\tCHARSELECT: Driver name too long to fit in Character selection memory! ({driver.Value.Split(" ")[1]})", ConsoleColor.Red);
                    }
                }

                // now search for just the firstnames!
                ptr = 0;
                while ((ptr = Search(buffer, Encoding.UTF8.GetBytes(driver.Key.Split(" ")[0]))) >= 0) {
                    cwc($"\tCHARSELECT: Found, replacing with {driver.Value.Split(" ")[0]}", ConsoleColor.Green);

                    // make sure we can fit it
                    byte[] newDriver = Encoding.UTF8.GetBytes(driver.Value.Split(" ")[0]);
                    if (newDriver.Length <= 24) {
                        Array.Clear(buffer, ptr, 24);
                        Buffer.BlockCopy(newDriver, 0, buffer, ptr, newDriver.Length);
                        // I think it's ok to overwrite all instances of the name, but there is typically 3 for each driver:
                        // the first padded as mentioned, is the one used in rendering
                        // the other two I'm not sure about though
                        if (ptr + driver.Value.Length > lastNamePtr) {
                            lastNamePtr = ptr + 24;
                        }
                    } else {
                        cwc($"\tCHARSELECT: Driver name too long to fit in Character selection memory! ({driver.Value.Split(" ")[0]})", ConsoleColor.Red);
                    }
                }
            }
            return buffer.Take(lastNamePtr).ToArray();
        }

        static byte[] parseInGame(byte[] buffer) {
            // this section is always 7968 bytes??
            // names are seperated into Mixed firstname, Upper lastname, Upper shortname (3 chars); separated by 32 bytes
            // except in cases where firstname or lastname is too long (or not unique?) and then that's stored somewher else and that field is skipped. GREAT
            // not sure what the max is, but there's seemingly always under 9 bytes

            // Could convert this to just use a static list as these don't move around, but feels safer to look them up each time

            const int regionSize = 7968;
            const int nameSize = 9;

            int ptr = 0;
            
            while(ptr<regionSize) {
                // check what size it is - we might be on a line that has skipped the normal flow
                // this effectively actually just sets what we're on rather than relying on stepping through
                String line = Encoding.UTF8.GetString(buffer.Skip(ptr).Take(nameSize).ToArray()).Replace("\0", string.Empty).Trim();
                
                //TODO: add additional catch so can accept 3 letter lastnames by checking to see if 2 tags are next to each other or some other method?

                // firstname, always has lower case bits
                if (Regex.Match(line, @"^[A-Z][a-z]+$").Success) {
                    String newName = lookupName(line, "");
                    if (newName != null) {
                        newName = newName.Split(" ")[0]; // get the first name
                        byte[] newName_bytes = Encoding.UTF8.GetBytes(newName);
                        if (newName_bytes.Length > 9) {
                            cwc($"\tINGAME: New firstname {newName} is too long (>{nameSize}), truncating", ConsoleColor.DarkYellow); //TODO: handle these cases in some other way?
                            // truncation happens in the blockCopy
                        }
                        cwc($"\tINGAME: Writing new ingame firstname {line}->{newName}", ConsoleColor.Green);
                        Array.Clear(buffer, ptr, nameSize);
                        Buffer.BlockCopy(newName_bytes, 0, buffer, ptr, Math.Min(newName.Length, 9));
                    }
                // driver tag
                } else if (Regex.Match(line, @"^[A-Z]{3}$").Success) { // needs to somehow avoid surnames of 3 characters?
                    String newTag = lookupDriverCode(line);
                    if (!String.IsNullOrWhiteSpace(newTag)) {
                        byte[] newName_bytes = Encoding.UTF8.GetBytes(newTag);
                        cwc($"\tINGAME: Writing new ingame driver tag {line}->{newTag}", ConsoleColor.Green);
                        Buffer.BlockCopy(newName_bytes, 0, buffer, ptr, Math.Min(newTag.Length, 4)); // up to 4?... it might break tbh
                    } else {
                        cwc($"\tINGAME: Uh oh, couldn't find who this driver code/tag lines up to: {line}", ConsoleColor.Red);
                    }
                    // last name
                } else if (Regex.Match(line, @"^[A-Z]+$").Success) {
                    String newName = lookupName("", line);
                    if (newName != null) {
                        newName = newName.Split(" ")[1]; // get the last name
                        byte[] newName_bytes = Encoding.UTF8.GetBytes(newName);
                        if (newName_bytes.Length > 9) {
                            cwc($"INGAME: New lastname {newName} is too long (>{nameSize}), truncating", ConsoleColor.DarkYellow);
                            // truncation happens in the blockCopy
                        }
                        cwc($"\tINGAME: Writing new ingame lastname {line}->{newName}", ConsoleColor.Green);
                        Array.Clear(buffer, ptr, nameSize);
                        Buffer.BlockCopy(newName_bytes, 0, buffer, ptr, Math.Min(newName.Length, 9));
                    }
                }
                ptr += 32;
			}
            return buffer.Take(regionSize).ToArray();
		}

        static bool IsAllUpper(string input) {
            for (int i = 0; i < input.Length; i++) {
                if (!Char.IsUpper(input[i]))
                    return false;
            }

            return true;
        }

        static String lookupName(String firstname, String lastname) {
            // firstname is expected to be mixed case (first upper), lastname is all upper case
            String newName = "";
            if (nameLookup.TryGetValue($"{firstname} {lastname}", out newName)) {
                cwc($"\tLOOKUP: Sucessfully found new name for {firstname} {lastname}->{newName}", ConsoleColor.DarkGreen);
                return newName;
			}
            // try and find a match?
            if (!String.IsNullOrWhiteSpace(lastname)) {
                var possibleKeys = nameLookup.Keys.Where(key => key.ToLower().Contains(lastname.ToLower())).ToList();
                if (possibleKeys.Count > 0) {
                    if (possibleKeys.Count == 1) {
                        newName = nameLookup[possibleKeys.First()];
                        cwc($"\tLOOKUP: Found probabilistic match based on last name for {firstname} {lastname} (Matched as {possibleKeys.First()})->{newName}", ConsoleColor.Yellow);
                        return newName;
                    }
                    // could at this point try checking other things, but eh, let's put the onus on the user
                }
            }
            // try firstnames?

            if (!String.IsNullOrWhiteSpace(firstname)) {
                var possibleKeys = nameLookup.Keys.Where(key => key.ToLower().Contains(firstname.ToLower())).ToList();
                if (possibleKeys.Count > 0) {
                    if (possibleKeys.Count == 1) {
                        newName = nameLookup[possibleKeys.First()];
                        cwc($"\tLOOKUP: Found probabilistic match based on first name for {firstname} {lastname} (Matched as {possibleKeys.First()})->{newName}", ConsoleColor.Yellow);
                        return newName;
                    }
                    // could at this point try checking other things, but eh, let's put the onus on the user
                }
            }
            cwc($"\tLOOKUP: Failed to find a lookup for {firstname} {lastname}, skipping", ConsoleColor.Yellow);
            return null;
		}

        static String lookupDriverCode(String oldCode) {
            String oldName;
            if (Lookups.shortNames.TryGetValue(oldCode, out oldName)) {
                String newCode;
                if (nameLookup_short.TryGetValue(oldName, out newCode)) {
                    return newCode;
				} else {
                    cwc($"\tLOOKUP:  Couldn't find a matching driver tag for {oldName}", ConsoleColor.Yellow);
				}
			} else {
                cwc($"\tLOOKUP: Couldn't find a matching driver tag lookup! {oldCode}", ConsoleColor.Red);
			}
            return null;
		}

        static String generateMenuName(String firstname, String lastname, int nameSize) { // be aware this adds the {o:mixed} on the front so we can expand if needed!
            String newMenuItem;
            if (firstname.Length + lastname.Length > 12) {
                cwc($"\tGENMENU1: {firstname} {lastname} exceeds 12 characters, using 2nd method of inserting names...", ConsoleColor.DarkYellow);
                // try without the {/o} stuff? Not as tested
                if (firstname.Length + lastname.Length > nameSize) {
                    throw new InvalidDataException($"Firstname + Lastname cannot exceed {nameSize} bytes!");
                } else {
                    newMenuItem = firstname + " " + lastname.ToUpper();
                }
            } else {
                newMenuItem = @"{o:mixed}" + firstname + @"{/o} {o:upper}" + lastname + @"{/o}";
            }
            // now pad the returning string so it lines up to -39- nameSize bytes
            int toPad = nameSize - newMenuItem.Length;
            if (toPad < 0) {
                throw new ArithmeticException($"New menu item exceeds {nameSize} bytes... somehow? \'" + newMenuItem+"\'");
			}
            cwc($"\tGENMENU1: Generated memory region for new name: {firstname} {lastname}", ConsoleColor.Green);
            return newMenuItem + new string('\0',toPad);
        }

        static void loadLookupTables() {
            // load in name lookup table
            //TODO: fix-formatting of names to Mixed, Upper
            try {
                string jsonStr = File.ReadAllText(@"names.json");
                dynamic json = JsonConvert.DeserializeObject(jsonStr);

                foreach (dynamic person in json) {
                    if (String.IsNullOrWhiteSpace((string)person.Name)) continue;
                    if (!String.IsNullOrWhiteSpace((string)person.Value.name)) {
                        nameLookup.Add((string)person.Name, (string)person.Value.name);
                    }
                    if (!String.IsNullOrWhiteSpace((string)person.Value.tag)) {
                        nameLookup_short.Add((string)person.Name, (string)person.Value.tag);
                    }
                }
                cwc("Loaded in json lookup file", ConsoleColor.Green);
                return;
            } catch (FileNotFoundException) { }

            try {
                string txtStr = File.ReadAllText(@"names.txt");
                using (var reader = new StringReader(txtStr)) {
                    for (string line = reader.ReadLine(); line != null; line = reader.ReadLine()) {

                        var split = line.Split(",");
                        if (split.Length < 2) continue;
                        String oldName = split[0].Trim();
                        String newName = split[1].Trim();
                        if (String.IsNullOrWhiteSpace(oldName)) continue;
                        if (!String.IsNullOrWhiteSpace(newName)) {
                            nameLookup.Add(oldName, newName);
                        }
                        if (split.Length > 2) {
                            String newTag = split[2].Trim();
                            nameLookup_short.Add(oldName, newTag);
                        }
                    }
                }
                cwc("Loaded in txt lookup file", ConsoleColor.Green);
                return;
            } catch (FileNotFoundException) { }

            cwc("Name lookup file (json or txt/csv) not found!", ConsoleColor.Red);
            Console.ReadLine();
            Environment.Exit(1);
        }
    }
    //the extension class must be declared as static
    public static class StringExtension { // why is .NETcore different to .NETframework...
        public static string[] Split(this string str, string splitter) {
            return str.Split(new[] { splitter }, StringSplitOptions.None);
        }
    }
}
