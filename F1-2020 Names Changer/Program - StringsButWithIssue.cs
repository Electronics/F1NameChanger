using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace F1_2020_Names_Changer {
	class Program {

        static Dictionary<string, string> nameLookup = new Dictionary<string, string>();

        static void cwc(String s, ConsoleColor fg, ConsoleColor bg=ConsoleColor.Black) {
            ConsoleColor bg_ = Console.BackgroundColor;
            ConsoleColor fg_ = Console.ForegroundColor;
            Console.BackgroundColor = bg;
            Console.ForegroundColor = fg;
			Console.WriteLine(s);
            Console.BackgroundColor = bg_;
            Console.ForegroundColor = fg_;
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

        const Int64 MENU_OFFSET_START = 0x2b0cc2500; // 0x2b0cc2542
        const Int64 MENU2_OFFSET_START = 0x2b0f32200;
        const Int64 CHARSELECTION_OFFSET_START = 0x2b0b07920;

        static readonly byte[] MENU_SEARCH_STR = Encoding.GetEncoding(437).GetBytes("{o:mixed}"); // GetEncoding(437)?! this is an encoding that doesn't mangle weird non-UTF8-characters like 255 and 128!
        static readonly byte[] CHARSELECTION_SEARCH_STR = Encoding.GetEncoding(437).GetBytes("Carlos SAINZ");

        static void Main(string[] args) {

            // for some reason we have to *register* encoding 437 just so we can get JsonDeserialize to work again after we use .GetEncoding(437).GetBytes() once????? wtf
            CodePagesEncodingProvider.Instance.GetEncoding(437);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // load in name lookup table
            try {
				string json = File.ReadAllText(@"C:\Users\Laurie\Desktop\F1RevEng\names.json");
				nameLookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
			} catch (FileNotFoundException) {
				cwc("Name lookup file not found!", ConsoleColor.Red);
				Console.ReadLine();
				return;
			}

			if (Process.GetProcessesByName("F1_2020_dx12").Length< 1) {
				Console.WriteLine("F1 Process not detected, waiting for game to be started");
                while(Process.GetProcessesByName("F1_2020_dx12").Length<1) {
                    System.Threading.Thread.Sleep(1000);
				}
                Console.WriteLine("F1 Process detected, waiting 15 seconds or so for game to get ready");
                System.Threading.Thread.Sleep(15000);
            }
            Process process = Process.GetProcessesByName("F1_2020_dx12")[0]; // Get the F1 process
			IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);


			// First off, let's find the start of the menu section
			IntPtr bytesRead = IntPtr.Zero;
			byte[] buffer = new byte[12000];
			ReadProcessMemory((IntPtr)processHandle, (IntPtr)MENU_OFFSET_START, buffer, buffer.Length, out bytesRead);
			cwc($"Read {bytesRead} bytes of RAM at {MENU_OFFSET_START:X}(Menu Region 1)", ConsoleColor.Blue, ConsoleColor.DarkYellow);


			//temppppp
			//byte[] buffer = File.ReadAllBytes(@"C:\Users\Laurie\Desktop\F1RevEng\menuMemoryRegion.hex");


			int menuOffsetAdditional = Search(buffer, MENU_SEARCH_STR); // search because Array.indexOf sucks
            if (menuOffsetAdditional<0) {
                cwc("Failed to find Menu Region 1", ConsoleColor.Red);
            } else {
                cwc($"Found Menu Memory Region 1 Offset: {menuOffsetAdditional}", ConsoleColor.Green);

                String memOut = parseMenuMemoryRegion1(Encoding.GetEncoding(437).GetString(buffer));
                if (!String.IsNullOrEmpty(memOut)) {
                    // write out the memory
                    cwc("Writing new menu memory region 1 to RAM...", ConsoleColor.Blue, ConsoleColor.DarkYellow);
                    Array.Clear(buffer, 0, buffer.Length);
                    buffer = Encoding.GetEncoding(437).GetBytes(memOut.Substring(menuOffsetAdditional)); // should fit in the buffer - we checked this already
                    IntPtr bytesWritten = IntPtr.Zero;
                    WriteProcessMemory((IntPtr)processHandle, (IntPtr)MENU_OFFSET_START + menuOffsetAdditional, buffer, memOut.Length - menuOffsetAdditional, out bytesWritten);
                    cwc($"Written {bytesWritten} bytes to RAM at {MENU_OFFSET_START+menuOffsetAdditional:X}(Menu Region 1)", ConsoleColor.Blue, ConsoleColor.DarkYellow);
                }
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

                String memOut = parseMenuMemoryRegion1(Encoding.GetEncoding(437).GetString(buffer));
                if (!String.IsNullOrEmpty(memOut)) {
                    // write out the memory
                    cwc("Writing new menu memory region 2 to RAM...", ConsoleColor.Blue, ConsoleColor.DarkYellow);
                    Array.Clear(buffer, 0, buffer.Length);
                    buffer = Encoding.GetEncoding(437).GetBytes(memOut.Substring(menuOffsetAdditional)); // should fit in the buffer - we checked this already
                    IntPtr bytesWritten = IntPtr.Zero;
                    WriteProcessMemory((IntPtr)processHandle, (IntPtr)MENU2_OFFSET_START + menuOffsetAdditional, buffer, memOut.Length - menuOffsetAdditional, out bytesWritten);
                    cwc($"Written {bytesWritten} bytes to RAM at {MENU_OFFSET_START + menuOffsetAdditional:X}(Menu Region 2)", ConsoleColor.Blue, ConsoleColor.DarkYellow);
                }
            }


            // ---------------- Now for in game section --------------
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

                String memOut = parseCharSelection(Encoding.GetEncoding(437).GetString(buffer));
                if (!String.IsNullOrEmpty(memOut)) {
                    // write out the memory
                    cwc("Writing new Character Selection Region to RAM...", ConsoleColor.Blue, ConsoleColor.DarkYellow);
                    Array.Clear(buffer, 0, buffer.Length);
                    buffer = Encoding.GetEncoding(437).GetBytes(memOut.Substring(menuOffsetAdditional)); // should fit in the buffer
                    IntPtr bytesWritten = IntPtr.Zero;
                    WriteProcessMemory((IntPtr)processHandle, (IntPtr)CHARSELECTION_OFFSET_START + menuOffsetAdditional, buffer, memOut.Length - menuOffsetAdditional, out bytesWritten);
                    cwc($"Written {bytesWritten} bytes to RAM at {CHARSELECTION_OFFSET_START + menuOffsetAdditional:X}(Character Selection Region)", ConsoleColor.Blue, ConsoleColor.DarkYellow);
                }
            }



            Console.ReadLine();



            /*byte[] buffer2 = Encoding.Unicode.GetBytes("It works!\0"); // '\0' marks the end of string

            IntPtr bytesWritten = IntPtr.Zero;

            // replace 0x0046A3B8 with your address
            WriteProcessMemory((IntPtr)processHandle, (IntPtr)0x15A33B85DA0, buffer2, buffer2.Length, out bytesWritten);
            Console.ReadLine();*/
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

        static String parseMenuMemoryRegion1(String buffer) {
            // we know for this first memory region, all the lines are memory aligned by 64 bytes starting with the {o:mixed}[firstname]{/o} {o:upper}[lastname]{/o}
            // with a maximum total length of this section of 39 bytes as is cut by the game (doesn't have to be null termianted if it hits this limit?
            //  > 39 bytes including {} stuff, 12 bytes of firstname+lastname?
            // OR it seems the {o:} stuff can be removed as long as text is formatted correctly and accepts full 39 bytes
            
            String retstr = ""; // our return string after we've modified the memory contents

            var splits = buffer.Split(@"{o:mixed}"); // we now split the memory section into bits where we know the names are
            bool first = true;
            int lastNamePtr = 0; // so we know when we've finished making the bit of memory we want to change (don't want to change stuff we don't know about!)
            foreach (String split in splits) {
                if (split.Contains("{/o}")) {
                    Console.WriteLine($"First o:mixed: {retstr.IndexOf("{o:mixed}")}");
                    var nameSplit = split.Split("{/o}");
                    if (nameSplit.Length > 2) {
                        var firstName = nameSplit[0];
                        var lastName = nameSplit[1].Split(@"{o:upper}")[1];

                        cwc($"PARSEMENU1: Found memory region for {firstName} {lastName}", ConsoleColor.Cyan);

                        // we can now lookup the name in a lookup table and replace it
                        var rName = lookupName(firstName, lastName);
                        rName = "Abcdefgh JKLMNOPQR";

                        if (String.IsNullOrEmpty(rName)) {
                            // we've already given some console output, let's just skip this quietly now
                            // I was going to use a goto here to skip to the checking for first item, but this code path can never occur on the first split
                            retstr += "{o:mixed}" + split;
                            continue;
						}

                        String newName = generateMenuName(rName.Split(" ")[0], rName.Split(" ")[1]); // add the new menu item
                        retstr += newName + split.Substring(newName.Length-9); // add the rest of the line back on, length should = 39-9 (length of {o:mixed})

                        lastNamePtr = retstr.Length;

                    } else {
                        cwc($"PARSEMENU1: Line contained the start of a name but nothing followed?...\n\'{split}\'", ConsoleColor.DarkYellow);
                        retstr += "{o:mixed}"+split; // add the {mixed} bit back in
                    }
                } else {
                    cwc("PARSEMENU1: Skipping unused memory area...", ConsoleColor.DarkCyan);
                    if (first) {
                        first = false;
                        retstr += split;
                    } else {
                        retstr += "{o:mixed}" + split; // add the {mixed} bit back in, but not the first time!
                    }
                }
			}

            int newLen = retstr.Length;
            int oldLen = retstr.Length;
            if (newLen != oldLen) {
                cwc($"PARSEMENU1: New memory region does not equal the original size, something has gone badly wrong!!", ConsoleColor.Red);
                return null;
			}

            // should be good to replace memory region now
            return retstr.Substring(0,lastNamePtr);
        }

        static String parseCharSelection(String buffer) {
            // sadly the char selection memory region is not nicely serparated, *most* drivers are padded with DRIVER].bk2 but not all so instead we'll do what I probably should have in the other parser and just search for names from our lookup table in the memory region and replace them
            // saying this, every driver except for SAINZ occurs at a 2x48-byte interval (probably the size of the struct used)

            int lastNamePtr = 0; // I like this to keep writing too far into RAM accidentally
            foreach (KeyValuePair<string, string> driver in nameLookup) {
                cwc($"CHARSELECT: Searching for {driver.Key}...", ConsoleColor.Cyan);
                int ptr = 0;
                while ((ptr = buffer.IndexOf(driver.Key,ptr)) > 0) {
                    cwc($"\tCHARSELECT: Found, replacing with {driver.Value}", ConsoleColor.Green);
                    // if the driver name we're replacing with is shorter than the original, we need to blank out the original!
                    if (driver.Key.Length > driver.Value.Length) {
                        cwc($"\tCHARSELECT: New driver name is shorter, making sure to blank out previous name...", ConsoleColor.DarkYellow);
                        buffer = buffer.Remove(ptr, driver.Key.Length).Insert(ptr, new string('\0', driver.Key.Length));
					}
                    if (driver.Value.Length <= 24) {
                        buffer = buffer.Remove(ptr, driver.Value.Length).Insert(ptr, driver.Value);
                        // I think it's ok to overwrite all instances of the name, but there is typically 3 for each driver:
                        // the first padded as mentioned, is the one used in rendering
                        // the other two I'm not sure about though
                        lastNamePtr = ptr + driver.Value.Length;
                    } else {
                        cwc($"\tCHARSELECT: Driver name too long to fit in Character selection memory! ({driver.Value})", ConsoleColor.Red);
					}
                }
            }
            return buffer.Substring(0, lastNamePtr);
        }

        static String lookupName(String firstname, String lastname) {
            // firstname is expected to be mixed case (first upper), lastname is all upper case
            String newName = "";
            if (nameLookup.TryGetValue($"{firstname} {lastname}", out newName)) {
                cwc($"\tLOOKUP: Sucessfully found new name for {firstname} {lastname}->{newName}", ConsoleColor.DarkGreen);
                return newName;
			}
            // try and find a match?
            var possibleKeys = nameLookup.Keys.Where(key => key.ToLower().Contains(lastname.ToLower())).ToList();
            if (possibleKeys.Count > 0) {
                if (possibleKeys.Count == 1) {
                    newName = nameLookup[possibleKeys.First()];
                    cwc($"\tLOOKUP: Found probabilistic match based on last name for {firstname} {lastname} (Matched as {possibleKeys.First()})->{newName}", ConsoleColor.Yellow);
                    return newName;
				}
                // could at this point try checking other things, but eh, let's put the onus on the user
            }
            cwc($"\tLOOKUP: Failed to find a lookup for {firstname} {lastname}, skipping", ConsoleColor.Yellow);
            return null;
		}

        static String generateMenuName(String firstname, String lastname) { // be aware this adds the {o:mixed} on the front so we can expand if needed!
            String newMenuItem;
            if (firstname.Length + lastname.Length > 12) {
                cwc($"\tGENMENU1: {firstname} {lastname} exceeds 12 characters, using 2nd method of inserting names...", ConsoleColor.DarkYellow);
                // try without the {/o} stuff? Not as tested
                if (firstname.Length + lastname.Length > 38) {
                    throw new InvalidDataException("Firstname + Lastname cannot exceed 38 bytes!");
                } else {
                    newMenuItem = firstname + " " + lastname.ToUpper();
                }
            } else {
                newMenuItem = @"{o:mixed}" + firstname + @"{/o} {o:upper}" + lastname + @"{/o}";
            }
            // now pad the returning string so it lines up to 39 bytes
            int toPad = 39 - newMenuItem.Length;
            if (toPad < 0) {
                throw new ArithmeticException("New menu item exceeds 39 bytes... somehow? \'"+newMenuItem+"\'");
			}
            cwc($"\tGENMENU1: Generated memory region for new name: {firstname} {lastname}", ConsoleColor.Green);
            return newMenuItem + new String('\0', toPad);
        }

        static String fixNames(String input) { // convert special characters into the unicode (we need to do this as we use the weird encoding to allow non-string stuff to pass through un-mangled)
            return input.Replace("Ü", "\xc3\x9c").Replace("Ä", "\xc3\x84").Replace("Ö","\xc3\x96"); 
        }
    }
}
