using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace F1_2020_Names_Changer {
    class MemoryChanger {

        static Dictionary<string, string> nameLookup = new Dictionary<string, string>();
        static Dictionary<string, string> nameLookup_short = new Dictionary<string, string>();
        static Dictionary<string, string> teamLookup = new Dictionary<string, string>();
        static Dictionary<string, string> teamLookup_short = new Dictionary<string, string>();

        static IntPtr processHandle;
        static bool stopRunning = false;

        static Form1 gui = new Form1();

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

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

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

        static readonly byte[] MENU_SEARCH_STR = Encoding.UTF8.GetBytes("{o:mixed}"); // GetEncoding(437)?! this is an encoding that doesn't mangle weird non-UTF8-characters like 255 and 128!
        static readonly byte[] CHARSELECTION_SEARCH_STR = Encoding.UTF8.GetBytes("Carlos SAINZ"); // he's always first, well, in some ways
        static readonly byte[] INGAME_SEARCH_STR = Encoding.UTF8.GetBytes("Carlos");

        // pointers to where we found the regions before (therefore allowing multiple undo cycles
        static int menuRegion1Offset = -1;
        static int menuRegion2Offset = -1;
        static int charRegionOffset = -1;
        static int ingameRegionOffset = -1;

        [STAThread] // for file dialog boxes
        static void Main(string[] args) {

            var config = new NLog.Config.LoggingConfiguration();
            var easyToReadLog = "${level:uppercase=true}: ${message}";

            var rtbTarget = new NLog.Windows.Forms.RichTextBoxTarget(); // garbage documentation from NLog - need nuget Nlog and Nlog.windows.forms
            rtbTarget.TargetForm = gui;
            rtbTarget.TargetRichTextBox = gui.logBox;
            rtbTarget.UseDefaultRowColoringRules = true;
            rtbTarget.Layout = easyToReadLog;

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "log.txt" };
            var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole");
            logconsole.Layout = easyToReadLog;
            logconsole.UseDefaultRowHighlightingRules = true;
            logfile.Layout = "${longdate} |" + easyToReadLog;


            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            config.AddRule(LogLevel.Info, LogLevel.Fatal, rtbTarget);
            NLog.LogManager.Configuration = config;

            log.Info("Program started");

            gui.ShowDialog();
        }

        public static void Stop() {
            log.Warn("Stopping Memory Changer");
            stopRunning = true;
		}

        public static void getF1Process(bool skipOffsetLoading=false) {
            stopRunning = false;
            if (Process.GetProcesses().Where(x=> x.ProcessName.StartsWith("F1_2020", StringComparison.OrdinalIgnoreCase)).Count() < 1) {
                log.Info("F1 Process not detected, waiting for game to be started");
                while (Process.GetProcesses().Where(x => x.ProcessName.StartsWith("F1_2020", StringComparison.OrdinalIgnoreCase)).Count() < 1) {
                    System.Threading.Thread.Sleep(1000);
                    log.Trace("Waiting for F1 Thread");
                    if (stopRunning) return;
                }
                log.Info("F1 Process detected, waiting 20 seconds or so for game to get ready");
                System.Threading.Thread.Sleep(20000);
            }
            Process process = Process.GetProcesses().Where(x => x.ProcessName.StartsWith("F1_2020", StringComparison.OrdinalIgnoreCase)).First(); // Get the F1 process
            if (process.ProcessName=="F1_2020_dx12" && !skipOffsetLoading) {
                log.Info("Detected as DX12 version, loading offsets");
                Offsets.loadDX12();
			}
            if (process.ProcessName== "F1_2020" && !skipOffsetLoading) {
                log.Info("Detected as DX11 version, loading offsets");
                Offsets.loadDX11();
			}
            processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
            log.Info("F1 Process detected");
        }

        public static void run(string nameLookupFile, string teamLookupFile, bool reversed=false, bool useCustomOffsets=false) {
            stopRunning = false;
            bool missingRegions = false; // used to indicate whether we should ask the user to try finding custom offsets instead
            log.Info("Memory Changer started");
            log.Debug($"With Name lookup file: {nameLookupFile}\n and team lookup file: {teamLookupFile}");

            log.Info("Clearing old lookups (if any)");
            if (reversed) {
                log.Info("Reversing lookup tables");
                // hopefully just use the previously used lookup tables as this is a one-time undo?
                if (nameLookup.Count < 1) {
                    log.Fatal("Cannot undo name changing as lookup tables are blank! Have you run the application at least once normally?");
                    gui.Stopped();
                    return;
				}

                // swap key and values of all lookups
                log.Info("Reversing previous lookup tables");
                try {
                    nameLookup = nameLookup.ToDictionary(x => x.Value, x => x.Key);
                    nameLookup_short = nameLookup_short.ToDictionary(x => x.Value, x => x.Key);
                    teamLookup = teamLookup.ToDictionary(x => x.Value, x => x.Key);
                    teamLookup_short = teamLookup_short.ToDictionary(x => x.Value, x => x.Key);

                    gui.Update("lookups", 1); // green indicator
                } catch(System.ArgumentException) {
                    log.Error("Cannot reverse lookup tables as new names contain duplicates");
                    gui.Update("lookups", 0); // red indicator
                    gui.Stopped();
                    return;
				}

            } else { 
                nameLookup.Clear();
                nameLookup_short.Clear();
                teamLookup.Clear();
                teamLookup_short.Clear();
                log.Info("Loading lookup tables");
                int result = loadLookupTables(nameLookupFile, teamLookupFile);
                if (result < 0) {
                    gui.Update("lookups", 0); // red indicator
                    gui.Stopped();
                    return;
                } else if (result == 0) {
                    gui.Update("lookups", 2); // yellow indicator
                } else { 
                    gui.Update("lookups", 1); // green indicator
                }
            }

            if (useCustomOffsets) {
                log.Info("Using custom offsets");
                if (!Offsets.load()) {
                    useCustomOffsets = false;
                    log.Warn("Using default offsets instead");
				}
			}

            getF1Process(useCustomOffsets); // refind the process anyway - if the game has been restarted we won't have picked up the new process handle
            if ((int)processHandle == 0) {
                log.Error("Failed to find process - was the process stopped?");
                gui.Stopped(false);
                return;
            }

            log.Info("Editing Menu Region 1...");

            // First off, let's find the start of the menu section
            IntPtr bytesRead = IntPtr.Zero;
            byte[] buffer = new byte[24000];
            ReadProcessMemory((IntPtr)processHandle, Offsets.MENU_OFFSET_START, buffer, buffer.Length, out bytesRead);
            log.Debug($"Read {bytesRead} bytes of RAM at {(long)Offsets.MENU_OFFSET_START:x}(Menu Region 1)");


            //temppppp
            //byte[] buffer = File.ReadAllBytes(@"C:\Users\Laurie\Desktop\F1RevEng\menuMemoryRegion.hex");

            if (menuRegion1Offset<0) {
                menuRegion1Offset = Search(buffer, MENU_SEARCH_STR); // search because Array.indexOf sucks
            } else {
                log.Debug("Using previously found memory region 1 offset");
			}
            if (menuRegion1Offset < 0) {
                log.Error("Failed to find Menu Region 1");
                gui.Update("region1", 0); // red indicator
                missingRegions = true;
            } else {
                log.Debug($"Found Menu Memory Region 1 Offset: {menuRegion1Offset}");
                
                if((int)bytesRead>0) gui.Update("region1", 1); // green indicator
                else gui.Update("region1", 0); // red indicator

                byte[] memOut = parseMenuMemoryRegion(buffer.Skip(menuRegion1Offset).ToArray(), 39);
                // write out the memory
                log.Debug("Writing new menu memory region 1 to RAM...");
                IntPtr bytesWritten = IntPtr.Zero;
                WriteProcessMemory((IntPtr)processHandle, Offsets.MENU_OFFSET_START + menuRegion1Offset, memOut, memOut.Length, out bytesWritten);
                log.Debug($"Written {bytesWritten} bytes to RAM at {(long)(Offsets.MENU_OFFSET_START + menuRegion1Offset):x}(Menu Region 1)");
                log.Info($"Sucesfully written to Menu Region 1");
            }

            // ---------------- Now for menu bit part 2 --------------
            Array.Clear(buffer, 0, buffer.Length);
            log.Info("Editing Menu Region 2...");


            ReadProcessMemory((IntPtr)processHandle, Offsets.MENU2_OFFSET_START, buffer, buffer.Length, out bytesRead);
            log.Debug($"Read {bytesRead} bytes of RAM at {(long)Offsets.MENU2_OFFSET_START:x}(Menu Region 2)");


            //temppppp
            //byte[] buffer = File.ReadAllBytes(@"C:\Users\Laurie\Desktop\F1RevEng\menuMemoryRegion.hex");


            if (menuRegion2Offset < 0) {
                menuRegion2Offset = Search(buffer, MENU_SEARCH_STR); // search because Array.indexOf sucks
            } else {
                log.Debug("Using previously found memory region 2 offset");
            }
            if (menuRegion2Offset < 0) {
                log.Error("Failed to find Menu Region 2");
                gui.Update("region2", 0); // red indicator
                missingRegions = true;
            } else {
                log.Debug($"Found Menu Memory Region 2 Offset: {menuRegion2Offset}");

                if ((int)bytesRead > 0) gui.Update("region2", 1); // green indicator
                else gui.Update("region2", 0); // red indicator

                byte[] memOut = parseMenuMemoryRegion(buffer.Skip(menuRegion2Offset).ToArray(), 52); // yup, this memory region has different size structs, go figure
                // write out the memory
                log.Debug("Writing new menu memory region 2 to RAM...");
                IntPtr bytesWritten = IntPtr.Zero;
                WriteProcessMemory((IntPtr)processHandle, Offsets.MENU2_OFFSET_START + menuRegion2Offset, memOut, memOut.Length, out bytesWritten);
                log.Debug($"Written {bytesWritten} bytes to RAM at {(long)(Offsets.MENU2_OFFSET_START + menuRegion2Offset):x}(Menu Region 2)");
                log.Info($"Sucesfully written to Menu Region 2");
            }


            // ---------------- Now for in character selection section --------------
            Array.Clear(buffer, 0, buffer.Length);

            log.Info("Editing Character Selection Region...");


            ReadProcessMemory((IntPtr)processHandle, Offsets.CHARSELECTION_OFFSET_START, buffer, buffer.Length, out bytesRead);
            log.Debug($"Read {bytesRead} bytes of RAM at {(long)Offsets.CHARSELECTION_OFFSET_START:x}(Character Selection Region)");


            //temppppp
            //byte[] buffer = File.ReadAllBytes(@"C:\Users\Laurie\Desktop\F1RevEng\menuMemoryRegion.hex");

            if (charRegionOffset < 0) {
                charRegionOffset = Search(buffer, CHARSELECTION_SEARCH_STR); // search because Array.indexOf sucks
            } else {
                log.Debug("Using previously found character selection region offset");
            }
            if (charRegionOffset < 0) {
                log.Error("Failed to find Character Selection Region");
                gui.Update("charRegion", 0); //red indicator
                missingRegions = true;
            } else {
                log.Debug($"Found Character Selection Region: {charRegionOffset}");

                if ((int)bytesRead > 0) gui.Update("charRegion", 1); // green indicator
                else gui.Update("charRegion", 0); // red indicator

                byte[] memOut = parseCharSelection(buffer.Skip(charRegionOffset).ToArray());
                // write out the memory
                log.Debug("Writing new Character Selection Region to RAM...");
                IntPtr bytesWritten = IntPtr.Zero;
                WriteProcessMemory((IntPtr)processHandle, Offsets.CHARSELECTION_OFFSET_START + charRegionOffset, memOut, memOut.Length, out bytesWritten);
                log.Debug($"Written {bytesWritten} bytes to RAM at {(long)(Offsets.CHARSELECTION_OFFSET_START + charRegionOffset):x}(Character Selection Region)");
                log.Info($"Sucesfully written to Character Selection Region");
            }

            // ---------------- Now for in game section --------------
            Array.Clear(buffer, 0, buffer.Length);

            log.Info("Editing In Game Region...");


            ReadProcessMemory((IntPtr)processHandle, Offsets.INGAME_OFFSET_START, buffer, buffer.Length, out bytesRead);
            log.Debug($"Read {bytesRead} bytes of RAM at {(long)Offsets.INGAME_OFFSET_START:x}(Game region)");


            //temppppp
            //byte[] buffer = File.ReadAllBytes(@"C:\Users\Laurie\Desktop\F1RevEng\menuMemoryRegion.hex");

            if (ingameRegionOffset < 0) {
                ingameRegionOffset = Search(buffer, INGAME_SEARCH_STR); // search because Array.indexOf sucks
            } else {
                log.Debug("Using previously found character selection region offset");
            }
            if (ingameRegionOffset < 0) {
                log.Error("Failed to find Game region");
                gui.Update("gameRegion", 0); // red indicator
                missingRegions = true;
            } else {
                log.Debug($"Found Game region: {ingameRegionOffset}");

                if ((int)bytesRead > 0) gui.Update("gameRegion", 1); // green indicator
                else gui.Update("gameRegion", 0); // red indicator

                byte[] memOut = parseInGame(buffer.Skip(ingameRegionOffset).ToArray(), reversed);
                // write out the memory
                log.Debug("Writing new Game region to RAM...");
                IntPtr bytesWritten = IntPtr.Zero;
                WriteProcessMemory((IntPtr)processHandle, Offsets.INGAME_OFFSET_START + ingameRegionOffset, memOut, memOut.Length, out bytesWritten);
                log.Debug($"Written {bytesWritten} bytes to RAM at {(long)(Offsets.INGAME_OFFSET_START + ingameRegionOffset):x}(Game region)");
            }

            // --------------------- Now finally teams ----------------------------------------
            if (teamLookup.Count > 0) {
				log.Info("Editing Teams...");


				ReadProcessMemory((IntPtr)processHandle, Offsets.TEAMS_OFFSET_START, buffer, buffer.Length, out bytesRead);
				log.Debug($"Read {bytesRead} bytes of RAM at {(long)Offsets.TEAMS_OFFSET_START:x}(Team region)");

                if ((int)bytesRead > 0) gui.Update("teamRegion", 1); // green indicator
                else {
                    gui.Update("teamRegion", 0); // red indicator
                    missingRegions = true;
                }
                writeTeamNames(processHandle, buffer, reversed);
            } else {
                log.Warn("Skipping team names as missing team lookups");
            }

            log.Info("Done!");
            gui.Finished(missingRegions);
        }
        static int Search(byte[] src, byte[] pattern, int offset=0) {
            int c = src.Length - pattern.Length + 1;
            int j;
            for (int i = offset; i < c; i++) {
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
            int nameCounter = 0; // just for some nice log.info stats

            while (ptr < buffer.Length) {

                // take the string bit and decode it so we can do string stuff with it
                String oldDriver = Encoding.UTF8.GetString(buffer.Skip(ptr).Take(nameSize).ToArray()).Split('\0')[0].Trim();
                // now strip out the name between the {} crap
                var tempSplit = oldDriver.Split("{o:mixed}");
                // TODO: Fix this skipping over drivers on the undo that have long names and therefore don't have {o:mixed}
                // maybe see if it's a perfect 2-name, mixed, upper case thing then pass it?
                String firstName;
                String lastName;
                try {
                    if (tempSplit.Length <= 1) {
                        // double check it's not a name still by looking for "Mixed UPPER"
                        var split = oldDriver.Split(" ");
                        if (split.Length == 2) {
                            if (IsAllUpper(split[1]) && split[0].Count(ch => char.IsUpper(ch)) == 1) {
                                firstName = split[0];
                                lastName = split[1];
                            } else {
                                goto SKIP;
                            }
                        } else {
                            goto SKIP;
                        }
                    } else {
                        oldDriver = tempSplit[1];
                        var nameSplit = oldDriver.Split("{/o}");
                        firstName = nameSplit[0];
                        lastName = nameSplit[1].Split(@"{o:upper}")[1];
                    }
                } catch(IndexOutOfRangeException) {
                    log.Error($"PARSEMENU: Index out of range exception: probably just some random text that looked like a name? \"{oldDriver}\"");
                    goto SKIP;
				}

                log.Debug($"PARSEMENU: Found memory region for {firstName} {lastName}", ConsoleColor.Cyan);

                // we can now lookup the name in a lookup table and replace it
                var rName = lookupName(firstName, lastName, true);

                if (!String.IsNullOrEmpty(rName)) {
                    String newName = generateMenuName(rName.Split(" ")[0], rName.Split(" ")[1], nameSize); // add the new menu item
                    log.Trace($"Replacing with {newName}");
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(newName), 0, buffer, ptr, nameSize);
                } else {
                    log.Trace($"Failed to find lookup for name");
				}
                lastNamePtr = ptr + nameSize;
                nameCounter++;

                SKIP:
                ptr += 64;
            }

            log.Info($"MENUREGION: Replaced {nameCounter} names sucesfully");

            return buffer.Take(lastNamePtr).ToArray();
        }

        static byte[] parseCharSelection(byte[] buffer) {
            // sadly the char selection memory region is not nicely serparated, *most* drivers are padded with DRIVER].bk2 but not all so instead we'll do what I probably should have in the other parser and just search for names from our lookup table in the memory region and replace them
            // saying this, every driver except for SAINZ occurs at a 2x48-byte interval (probably the size of the struct used)

            int lastNamePtr = 0; // I like this to keep writing too far into RAM accidentally, although this doesn't work great in this case as we're jumping all over the place

            int fullNameCounter = 0; // just for some nice log.info stats
            int firstNameCounter = 0;
            int lastNameCounter = 0;

            byte[] bufferToReturn = buffer.ToArray(); // make a copy of the buffer array so we don't end up finding names we might have already replaced!

            foreach (KeyValuePair<string, string> driver in nameLookup) {
                log.Trace($"CHARSELECT: Searching for {driver.Key}...");
                List<int> foundNames = new List<int>(); // hold full names we've already found - don't want to go replacing firstname/lastname after we've already changed the full name
                int ptr = 0;
                while ((ptr = Search(buffer, Encoding.UTF8.GetBytes(driver.Key), ptr)) >= 0) { // ptr+1 to make sure search finds the NEXT instance of the name
                    log.Debug($"\tCHARSELECT: Found, replacing with {driver.Value}");

                    // make sure we can fit it
                    byte[] newDriver = Encoding.UTF8.GetBytes(driver.Value);
                    if (newDriver.Length <= 24) {
                        Array.Clear(bufferToReturn, ptr, 24);
                        Buffer.BlockCopy(newDriver, 0, bufferToReturn, ptr, newDriver.Length);
                        // I think it's ok to overwrite all instances of the name, but there is typically 3 for each driver:
                        // the first padded as mentioned, is the one used in rendering
                        // the other two I'm not sure about though
                        if (ptr + driver.Value.Length > lastNamePtr) {
                            lastNamePtr = ptr + 24;
                        }
                        foundNames.Add(ptr);
                        fullNameCounter++;
                    } else {
                        log.Warn($"\tCHARSELECT: Driver name too long to fit in Character selection memory! ({driver.Value})");
                        gui.Update("charRegion", 2); // yellow
                    }

                    ptr += 24; // move the pointer on
                }

                // now search for just the lastnames!
                ptr = 0;
                while ((ptr = Search(buffer, Encoding.UTF8.GetBytes(driver.Key.Split(" ")[1]), ptr)) >= 0) {
                    if (containsNumberInRange(foundNames, ptr - 24, ptr)) {
                        log.Trace("Found name we've already replaced, skipping");
                        ptr += 24; // move on
                        continue; // don't change a name we've already done
                    }
                    log.Debug($"\tCHARSELECT: Found, replacing with {driver.Value.Split(" ")[1]}");

                    // make sure we can fit it
                    byte[] newDriver = Encoding.UTF8.GetBytes(driver.Value.Split(" ")[1]);
                    if (newDriver.Length <= 24) {
                        Array.Clear(bufferToReturn, ptr, 24);
                        Buffer.BlockCopy(newDriver, 0, bufferToReturn, ptr, newDriver.Length);
                        // I think it's ok to overwrite all instances of the name, but there is typically 3 for each driver:
                        // the first padded as mentioned, is the one used in rendering
                        // the other two I'm not sure about though
                        if (ptr + driver.Value.Length > lastNamePtr) {
                            lastNamePtr = ptr + 24;
                        }
                        lastNameCounter++;
                    } else {
                        log.Warn($"\tCHARSELECT: Driver name too long to fit in Character selection memory! ({driver.Value.Split(" ")[1]})");
                        gui.Update("charRegion", 2); // yellow
                    }

                    ptr += 24; // move the pointer on
                }

                // now search for just the firstnames!
                ptr = 0;
                while ((ptr = Search(buffer, Encoding.UTF8.GetBytes(driver.Key.Split(" ")[0]), ptr+1)) >= 0) {
                    if (containsNumberInRange(foundNames, ptr - 24, ptr)) {
                        log.Trace("Found name we've already replaced, skipping");
                        ptr += 24; // move on
                        continue; // don't change a name we've already done
                    }
                    log.Debug($"\tCHARSELECT: Found, replacing with {driver.Value.Split(" ")[0]}");


                    // make sure we can fit it
                    byte[] newDriver = Encoding.UTF8.GetBytes(driver.Value.Split(" ")[0]);
                    if (newDriver.Length <= 24) {
                        Array.Clear(bufferToReturn, ptr, 24);
                        Buffer.BlockCopy(newDriver, 0, bufferToReturn, ptr, newDriver.Length);
                        // I think it's ok to overwrite all instances of the name, but there is typically 3 for each driver:
                        // the first padded as mentioned, is the one used in rendering
                        // the other two I'm not sure about though
                        if (ptr + driver.Value.Length > lastNamePtr) {
                            lastNamePtr = ptr + 24;
                        }
                        firstNameCounter++;
                    } else {
                        log.Warn($"\tCHARSELECT: Driver name too long to fit in Character selection memory! ({driver.Value.Split(" ")[0]})");
                        gui.Update("charRegion", 2); // yellow
                    }

                    ptr += 24; // move the pointer on
                }
            }

            log.Info($"CHARSELECT: Replaced {fullNameCounter} full names, {firstNameCounter} first names, and {lastNameCounter} last names sucessfully");

            return bufferToReturn.Take(lastNamePtr).ToArray();
        }

        static byte[] parseInGame(byte[] buffer, bool reversed=false) {
            // this section is always 7968 bytes??
            // names are seperated into Mixed firstname, Upper lastname, Upper shortname (3 chars); separated by 32 bytes
            // except in cases where firstname or lastname is too long (or not unique?) and then that's stored somewher else and that field is skipped. GREAT
            // not sure what the max is, but there's seemingly always under 9 bytes

            // Could convert this to just use a static list as these don't move around, but feels safer to look them up each time

            const int regionSize = 7968;
            const int nameSize = 9;

            int shortNameCounter = 0; // just for some nice log.info stats
            int firstNameCounter = 0;
            int lastNameCounter = 0;

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
                            log.Warn($"\tINGAME: New firstname {newName} is too long (>{nameSize}), truncating"); //TODO: handle these cases in some other way?
                            // truncation happens in the blockCopy
                            gui.Update("gameRegion", 2); // yellow
                        }
                        log.Debug($"\tINGAME: Writing new ingame firstname {line}->{newName}");
                        Array.Clear(buffer, ptr, nameSize);
                        Buffer.BlockCopy(newName_bytes, 0, buffer, ptr, Math.Min(newName.Length, 9));
                        firstNameCounter++;
                    }
                // driver tag
                } else if (Regex.Match(line, @"^[A-Z]{3}$").Success) { // needs to somehow avoid surnames of 3 characters?
                    String newTag = lookupDriverCode(line, reversed);
                    if (!String.IsNullOrWhiteSpace(newTag)) {
                        byte[] newName_bytes = Encoding.UTF8.GetBytes(newTag);
                        log.Debug($"\tINGAME: Writing new ingame driver tag {line}->{newTag}");
                        Buffer.BlockCopy(newName_bytes, 0, buffer, ptr, Math.Min(newTag.Length, 4)); // up to 4?... it might break tbh
                        shortNameCounter++;
                    } else {
                        log.Debug($"\tINGAME: Uh oh, couldn't find who this driver code/tag lines up to: {line}");
                    }
                    // last name
                } else if (Regex.Match(line, @"^[A-Z]+$").Success) {
                    String newName = lookupName("", line);
                    if (newName != null) {
                        newName = newName.Split(" ")[1]; // get the last name
                        byte[] newName_bytes = Encoding.UTF8.GetBytes(newName);
                        if (newName_bytes.Length > 9) {
                            log.Warn($"INGAME: New lastname {newName} is too long (>{nameSize}), truncating");
                            // truncation happens in the blockCopy
                            gui.Update("gameRegion", 2); // yellow
                        }
                        log.Debug($"\tINGAME: Writing new ingame lastname {line}->{newName}", ConsoleColor.Green);
                        Array.Clear(buffer, ptr, nameSize);
                        Buffer.BlockCopy(newName_bytes, 0, buffer, ptr, Math.Min(newName.Length, 9));
                        lastNameCounter++;
                    }
                }
                ptr += 32;
			}

            log.Info($"INGAME: Replaced {shortNameCounter} driver tags, {firstNameCounter} first names, and {lastNameCounter} last names sucessfully");

            return buffer.Take(regionSize).ToArray();
		}

        static void writeTeamNames(IntPtr processHandle, byte[] buffer, bool reversed=false) {
            // from what I can tell, it doesn't matter how long the string is as long as it's null terminated ( we will overwrite some other stuff somewhere in the menu system, but whatever)
            // also, these are spread out a lot in memory, so better to 'punch' in and out directly, rather than reading and writing like 437k of memory
            // ^ above comment was about deprecated code, but still maintaining this ethos due to the racing point team being in a far region of memory compared to the rest
            bool tryCopyName(string oldName, long ptr, Dictionary<string,string> dict) {
                if(dict.ContainsKey(oldName)) {
                    byte[] newName_bytes = Encoding.UTF8.GetBytes(dict[oldName]+"\0");
                    IntPtr bytesWritten;
                    WriteProcessMemory((IntPtr)processHandle, (IntPtr)ptr, newName_bytes, newName_bytes.Length, out bytesWritten);
                    log.Debug($"\t{oldName}->{dict[oldName]} written successfully at 0x{ptr:x}");
                    return true;
                } else {
                    log.Trace($"\t{oldName} not found in teams lookup, skipping");
                    return false;
				}
			}

            List<int> foundTeams = new List<int>(); // keep a note of where we've already changed the longer names, we don't want to overwrite with shorter
            if (reversed) {
                foreach (var team in teamLookup) {
                    int ptr = 0;
                    while ((ptr = Search(buffer, Encoding.UTF8.GetBytes(team.Key), ptr)) >= 0) {
                        log.Trace($"Found {team.Key}");
                        if (tryCopyName(team.Value, ptr + (long)Offsets.TEAMS_OFFSET_START, Lookups.teams_rev)) {
                            foundTeams.Add(ptr);
                        }
                        ptr += 10;
                    }
                }

                // now in-game shorter names
                foreach (var team in teamLookup_short) {
                    int ptr = 0;
                    while ((ptr = Search(buffer, Encoding.UTF8.GetBytes(team.Key), ptr)) >= 0) {
                        log.Trace($"Found {team.Key}");
                        if (containsNumberInRange(foundTeams, ptr - 16, ptr)) {
                            log.Trace("Found team we've already replaced, skipping");
                            ptr += 10; // move on
                            continue; // don't change a name we've already done
                        }
                        tryCopyName(team.Value, ptr + (long)Offsets.TEAMS_OFFSET_START, Lookups.teams_short_rev);
                        ptr += 10;
                    }
                }
                // finally write the racing point bits as they're in a different memory region
                tryCopyName("Racing Point", (long)Offsets.TEAMS_OFFSET_MENU_RACING_POINT, Lookups.teams_rev);
                tryCopyName("Racing Point", (long)Offsets.TEAMS_OFFSET_GAME_RACING_POINT, Lookups.teams_short_rev);
            } else {
                // start in the main menu 
                foreach (var team in Lookups.teams) {
                    int ptr = 0;
                    while ((ptr = Search(buffer, Encoding.UTF8.GetBytes(team.Key), ptr)) >= 0) {
                        log.Trace($"Found {team.Key}");
                        if (tryCopyName(team.Value, ptr + (long)Offsets.TEAMS_OFFSET_START, teamLookup)) {
                            foundTeams.Add(ptr);
						}
                        ptr += 10;
                    }
                }

                // now in-game shorter names
                foreach (var team in Lookups.teams_short) {
                    int ptr = 0;
                    while ((ptr = Search(buffer, Encoding.UTF8.GetBytes(team.Key), ptr)) >= 0) {
                        log.Trace($"Found {team.Key}");
                        if (containsNumberInRange(foundTeams, ptr - 16, ptr)) {
                            log.Trace("Found team we've already replaced, skipping");
                            ptr += 10; // move on
                            continue; // don't change a name we've already done
                        }
                        tryCopyName(team.Value, ptr + (long)Offsets.TEAMS_OFFSET_START, teamLookup_short);
                        ptr += 10;
                    }
                }
                // finally write the racing point bits as they're in a different memory region
                tryCopyName("Racing Point", (long)Offsets.TEAMS_OFFSET_MENU_RACING_POINT, teamLookup);
                tryCopyName("Racing Point", (long)Offsets.TEAMS_OFFSET_GAME_RACING_POINT, teamLookup_short);
            }

            
        }


        static bool IsAllUpper(string input) {
            for (int i = 0; i < input.Length; i++) {
                if (!Char.IsUpper(input[i]))
                    return false;
            }

            return true;
        }

        static bool containsNumberInRange(IEnumerable<int> array, int lowerBound, int higherBound) {
            foreach (int item in array) {
                if (item >= lowerBound && item <= higherBound) return true;
			}
            return false;
		}

        static String lookupName(String firstname, String lastname, bool skipSingleNameLookup = false) {
            // firstname is expected to be mixed case (first upper), lastname is all upper case
            String newName = "";
            if (nameLookup.TryGetValue($"{firstname} {lastname}", out newName)) {
                log.Debug($"LOOKUP: Sucessfully found new name for {firstname} {lastname}->{newName}");
                return newName;
			}
            // try and find a match?
            if (!String.IsNullOrWhiteSpace(lastname)) {
                try {
                    var possibleKeys = nameLookup.Keys.Where(key => key.ToLower().Split(" ")[1].Equals(lastname.ToLower())).ToList();
                    if (possibleKeys.Count > 0) {
                        if (possibleKeys.Count == 1) {
                            newName = nameLookup[possibleKeys.First()];
                            log.Debug($"LOOKUP: Found probabilistic match based on last name for {firstname} {lastname} (Matched as {possibleKeys.First()})->{newName}");
                            return newName;
                        }
                        // could at this point try checking other things, but eh, let's put the onus on the user
                    }
				} catch (KeyNotFoundException) {
                    log.Warn($"LOOKUP: Key error in looking up reversed name");
                    return null;
				}
            }
            if (skipSingleNameLookup) {
                log.Trace("LOOKUP: Skipping single name lookup");
                return null;
            }
            // try firstnames?

            if (!String.IsNullOrWhiteSpace(firstname)) {
                var possibleKeys = nameLookup.Keys.Where(key => key.ToLower().Split(" ")[0].Equals(firstname.ToLower())).ToList();
                if (possibleKeys.Count > 0) {
                    if (possibleKeys.Count == 1) {
                        newName = nameLookup[possibleKeys.First()];
                        log.Debug($"LOOKUP: Found probabilistic match based on first name for {firstname} {lastname} (Matched as {possibleKeys.First()})->{newName}");
                        return newName;
                    }
                    // could at this point try checking other things, but eh, let's put the onus on the user
                }
            }
            log.Trace($"LOOKUP: Failed to find a lookup for {firstname} {lastname}, skipping");
            return null;
		}

        static String lookupDriverCode(String oldCode, bool reversed = false) {
            String oldName;
            if (reversed) {
                if (nameLookup_short.TryGetValue(oldCode, out oldName)) {
                    String newCode;
                    if (Lookups.shortNames_rev.TryGetValue(oldName, out newCode)) {
                        return newCode;
                    } else {
                        log.Warn($"Couldn't find a matching driver for tag {oldName}");
                    }
				} else {
                    log.Trace($"Couldn't find a matching driver for tag {oldCode}");
				}
            } else { 
                if (Lookups.shortNames.TryGetValue(oldCode, out oldName)) {
                    String newCode;
                    if (nameLookup_short.TryGetValue(oldName, out newCode)) {
                        return newCode;
                    } else {
                        log.Debug($"LOOKUP:  Couldn't find a matching driver tag for {oldName}");
                    }
                } else {
                    log.Debug($"LOOKUP: Couldn't find a matching driver tag lookup! {oldCode}");
                }
            }
            return null;
		}

        static String generateMenuName(String firstname, String lastname, int nameSize) { // be aware this adds the {o:mixed} on the front so we can expand if needed!
            String newMenuItem;
            if (firstname.Length + lastname.Length > 12) {
                log.Trace($"\tGENMENU1: {firstname} {lastname} exceeds 12 characters, using 2nd method of inserting names...");
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
            log.Debug($"\tGENMENU1: Generated memory region for new name: {firstname} {lastname}", ConsoleColor.Green);
            return newMenuItem + new string('\0',toPad);
        }

        static int loadLookupTables(string nameLookupFile, string teamLookupFile) {
            // load in name lookup table
            //TODO: fix-formatting of names to Mixed, Upper
            bool nameLookupSuccess = false;

            if (Path.GetExtension(nameLookupFile) == ".json") {
                try {
                    string jsonStr = File.ReadAllText(nameLookupFile);
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
                    log.Info("Loaded in json name lookup file");
                    nameLookupSuccess = true;
                } catch (FileNotFoundException) { }
            }

            if (Path.GetExtension(nameLookupFile) == ".txt") {
                try {
                    string txtStr = File.ReadAllText(nameLookupFile);
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
                    log.Debug("Loaded in txt name lookup file");
                    nameLookupSuccess = true;
                } catch (FileNotFoundException) { }
            }

            if (!nameLookupSuccess) {
                log.Error("Name lookup file (json or txt/csv) not found!");
                return -1;
            }

            bool teamLookupSuccess = false;
            if (Path.GetExtension(teamLookupFile) == ".json") {
                try {
                    string jsonStr = File.ReadAllText(teamLookupFile);
                    dynamic json = JsonConvert.DeserializeObject(jsonStr);

                    foreach (dynamic team in json) {
                        if (String.IsNullOrWhiteSpace((string)team.Name)) continue;
                        if (String.IsNullOrWhiteSpace((string)team.Value.name)) continue;
                        teamLookup.Add((string)team.Name, (string)team.Value.name);
                        if (!String.IsNullOrWhiteSpace((string)team.Value.ingame)) {
                            teamLookup_short.Add((string)team.Name, (string)team.Value.ingame);
                        }
                    }
                    log.Debug("Loaded in json team lookup file");
                    teamLookupSuccess = true;
                } catch (FileNotFoundException) { }
            }

            if (Path.GetExtension(teamLookupFile) == ".txt") {
                try {
                    string txtStr = File.ReadAllText(teamLookupFile);
                    using (var reader = new StringReader(txtStr)) {
                        for (string line = reader.ReadLine(); line != null; line = reader.ReadLine()) {

                            var split = line.Split(",");
                            if (split.Length < 2) continue;
                            String oldName = split[0].Trim();
                            String newName = split[1].Trim();
                            if (String.IsNullOrWhiteSpace(oldName)) continue;
                            if (String.IsNullOrWhiteSpace(newName)) continue;
                            teamLookup.Add(oldName, newName);
                            if (split.Length > 2) {
                                String newTag = split[2].Trim();
                                teamLookup_short.Add(oldName, newTag);
                            }
                        }
                    }
                    log.Debug("Loaded in txt team lookup file");
                    teamLookupSuccess = true;
                } catch (FileNotFoundException) { }
            }

            if (!teamLookupSuccess) {
                log.Warn("Team lookup file (json or txt/csv) not found! Skipping team changing for now.");
                return 0;
			}
            return 1;
        }

		public static void findOffsets() {
			getF1Process();
			log.Info("Attempting to discover offsets... This might take a minute or so");
			// read 5MB chunks at a time
			byte[] buffer = new byte[5000000];
			IntPtr bytesRead;
			byte[] menu_search_bytearr = Encoding.UTF8.GetBytes("{o:mixed}Carlos"); // byte arrays constructed before loop for speed
			byte[] menu2_search_bytearr = Encoding.UTF8.GetBytes("{o:mixed}Daniel{/o} {o:upper}RIC");
			byte[] char_search_bytearr = Encoding.UTF8.GetBytes("RIVER].bk2");
            byte[] game_search_bytearr = new byte[] { 0x43, 0x61, 0x72, 0x6C, 0x6F, 0x73, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x08, 0x00, 0x00, 0x00}; // Carlos...... , used to be SAINZ....SAI as well, but needs regex
            byte[] teams_search_bytearr = new byte[] { 0x52, 0x65, 0x64, 0x20, 0x42, 0x75, 0x6C, 0x6C, 0x20, 0x52, 0x61, 0x63, 0x69, 0x6E, 0x67, 0x00, 0x33, 0x30, 0x35, 0x32, 0x2E, 0x35, 0x00, 0x47, 0x75, 0x65, 0x6E, 0x74, 0x68, 0x65, 0x72, 0x20, 0x53, 0x74, 0x65, 0x69, 0x6E, 0x65, 0x72, 0x00, 0x32, 0x37, 0x00, 0x4A, 0x61, 0x6D, 0x65, 0x73, 0x20, 0x4B, 0x65, 0x79, 0x00, 0x57, 0x69, 0x6C, 0x6C, 0x69, 0x61, 0x6D, 0x73, 0x00, 0x38, 0x00 }; // translates to Red Bull Racing\03052\05\0Guenther Steiner\027\0James Key\0Williams\08\0
            byte[] teams_search_menu_racingPoint = new byte[] { 0x42, 0x57, 0x54, 0x20, 0x52, 0x61, 0x63, 0x69, 0x6E, 0x67, 0x20, 0x50, 0x6F, 0x69, 0x6E, 0x74, 0x20, 0x46, 0x31, 0x20, 0x54, 0x65, 0x61, 0x6D, 0x00, 0x43, 0x68, 0x61, 0x72, 0x6F, 0x75, 0x7A, 0x20, 0x52, 0x61, 0x63, 0x69, 0x6E, 0x67, 0x20, 0x53, 0x79, 0x73, 0x74, 0x65, 0x6D };
            byte[] teams_search_game_racingPoint = new byte[] { 0x42, 0x57, 0x54, 0x20, 0x52, 0x61, 0x63, 0x69, 0x6E, 0x67, 0x20, 0x50, 0x6F, 0x69, 0x6E, 0x74, 0x00, 0x5A, 0x48, 0x4F, 0x55, 0x00, 0x4D, 0x61, 0x78, 0x69, 0x6D, 0x69, 0x6C, 0x69, 0x61, 0x6E, 0x00, 0x4A, 0x6F, 0x72, 0x64, 0x61, 0x6E, 0x00 };


            bool foundCharRegion = false; // our search string finds a lot of matches on this bit, we just want the first
            bool foundTeamsRegion = false;
            bool foundRacingPoint = false; // we only want the first one (in local language)
            // rest of these are just for feedback to the user
            bool foundRacingPointGame = false;
            bool foundGameRegion = false;
            bool foundMenuRegion1 = false;
            bool foundMenuRegion2 = false;
            // assuming 2GB? of RAM
            long i = 0;
			while (i < 6e9) {
				ReadProcessMemory((IntPtr)processHandle, (IntPtr)((long)Offsets.SEARCH_START+i), buffer, buffer.Length, out bytesRead);
                //log.Trace($"Read chunk {i} (0x{(long)Offsets.SEARCH_START+i:x}), read {bytesRead} bytes");

                if (bytesRead==(IntPtr)0) {
                    i += 50000; // only skip forward a small amount - we can miss sections otherwise
                    //TODO: possibly change this for a search backwards if we've just skipped past a section we can't read?
                    continue;
				}

				// we need to search for 4 important offsets + the teams
				int foundOffset;
				if ((foundOffset = Search(buffer, menu_search_bytearr)) >= 0) {
                    long fullOffset = ((long)Offsets.SEARCH_START + i + (long)foundOffset);
                    log.Fatal($"(Not actually fatal) Found menu region 1: 0x{fullOffset:x}");
                    Offsets.MENU_OFFSET_START = (IntPtr)fullOffset - 0xb00;
                    foundMenuRegion1 = true;
				}

                if ((foundOffset = Search(buffer, menu2_search_bytearr)) >= 0) {
                    long fullOffset = ((long)Offsets.SEARCH_START + i + (long)foundOffset);
                    log.Fatal($"(Not actually fatal) Found menu region 2: 0x{fullOffset:x}");
                    Offsets.MENU2_OFFSET_START = (IntPtr)fullOffset - 0xb00;
                    foundMenuRegion2 = true;
                }
                if (!foundCharRegion) { // only find the first instance
                    if ((foundOffset = Search(buffer, char_search_bytearr)) >= 0) {
                        long fullOffset = ((long)Offsets.SEARCH_START + i + (long)foundOffset);
                        log.Fatal($"(Not actually fatal) Found character region: 0x{fullOffset:x}");
                        Offsets.CHARSELECTION_OFFSET_START = (IntPtr)fullOffset - 0xb00;
                        foundCharRegion = true;
                    }
                }
                if ((foundOffset = Search(buffer, game_search_bytearr)) >= 0) {
                    long fullOffset = ((long)Offsets.SEARCH_START + i + (long)foundOffset);
                    log.Fatal($"(Not actually fatal) Found in-game region: 0x{fullOffset:x}");
                    Offsets.INGAME_OFFSET_START = (IntPtr)fullOffset - 0xb00;
                    foundGameRegion = true;
                }
                if (!foundTeamsRegion) { // skip this slow bit after we've found it
                    if ((foundOffset = Search(buffer, teams_search_bytearr)) >= 0) {
                        long fullOffset = ((long)Offsets.SEARCH_START + i + (long)foundOffset);
                        log.Fatal($"(Not actually fatal) Found the start of the main teams region: 0x{fullOffset:x}");
                        Offsets.TEAMS_OFFSET_START = (IntPtr)fullOffset - 0xb00;
                        foundTeamsRegion = true;
                    }
                    // the following racing point items are in a different location, before the main section
                    if (!foundRacingPoint) {
                        if ((foundOffset = Search(buffer, teams_search_menu_racingPoint)) >= 0) {
                            long fullOffset = ((long)Offsets.SEARCH_START + i + (long)foundOffset);
                            log.Fatal($"(Not actually fatal) Found the racing point menu item: 0x{fullOffset:x}");
                            Offsets.TEAMS_OFFSET_MENU_RACING_POINT = (IntPtr)fullOffset;
                            foundRacingPoint = true;
                        }
                    }
                    if ((foundOffset = Search(buffer, teams_search_game_racingPoint)) >= 0) {
                        long fullOffset = ((long)Offsets.SEARCH_START + i + (long)foundOffset);
                        log.Fatal($"(Not actually fatal) Found the racing point game item: 0x{fullOffset:x}");
                        Offsets.TEAMS_OFFSET_GAME_RACING_POINT = (IntPtr)fullOffset;
                        foundRacingPointGame = true;
                    }
                }
                 
                //Teams detection, always early on
                /*if ((long)Offsets.SEARCH_START + i < 0x195000000) {
                    // convert to a string with special encoding so that weird (128/256) value bytes aren't changed in length, and then search with regex
                    string encodedBuffer = Encoding.GetEncoding(437).GetString(buffer);
                    Match m = mainTeamRegionSearch.Match(encodedBuffer);
                    if (m.Success) {
                        foundOffset = m.Index;
                        long fullOffset = ((long)Offsets.SEARCH_START + i + (long)foundOffset);
                        log.Fatal($"Found the start of the main teams region: 0x{fullOffset:x}");
                    }
				}*/

                i += buffer.Length;
            }
            log.Info($"Finished searching for offsets at {(long)Offsets.SEARCH_START + i:x}");
            Offsets.save();
            if (foundMenuRegion1&&foundMenuRegion2&&foundCharRegion&&foundGameRegion&&foundTeamsRegion&&foundRacingPoint&&foundRacingPointGame) {
                log.Info("SUCCESS: Found all expected offsets (unless there was duplicates)");
			} else {
                log.Warn("Failed to find one or more regions, this may cause some names and/or teams to be missed. Either try restarting the game and running \"Find Offsets\" again (In the Game menu), or disable custom offsets and use the default english offsets");
			}
        }
    }
    //the extension class must be declared as static
    public static class StringExtension { // why is .NETcore different to .NETframework...
        public static string[] Split(this string str, string splitter) {
            return str.Split(new[] { splitter }, StringSplitOptions.None);
        }
    }
}
