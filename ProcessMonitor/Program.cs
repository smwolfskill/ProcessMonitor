using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace ProcessMonitor
{
    /**
     * Author      Scott Wolfskill
     * Created     06/17/2017
     * Last edit   01/30/2019
     * 
     * Simple command-line program for monitoring local processes and killing them efficiently and thoroughly if desired.
     */
    class Program
    {
        public const string APPLICATION_NAME = "ProcessMonitor"; //excludes file type .exe
        public static Settings settings;
        public const string header_ignore = "\\IGNORE\\";
        public const string header_kill = "\\KILL\\";
        public const string header_startup = "\\STARTUP\\";
        public const string header_ignore_name = "Ignore list";
        public const string header_kill_name = "Kill list";

        static void Main(string[] args)
        {
            Console.WriteLine("PROCESS MONITOR V1.3"
                    + Environment.NewLine + "Press CTRL+C to force exit at any time (however, settings won't be saved)."
                    + Environment.NewLine + "--------------------------------------------------------------------------");
            //Load settings
            string[] settingHeaders = new string[] { header_ignore, header_kill, header_startup };
            string settingsFolder = Environment.GetEnvironmentVariable("UserProfile") + "\\AppData\\Roaming\\Process Monitor";
            settings = new Settings(settingHeaders, settingsFolder);
            settings.loadSettings();
            //Start main command loop
            while (true)
            {
                bool validInput = false;
                while (!validInput)
                {
                    Console.WriteLine("Enter a command ('help' for more info, 'q' to quit):");
                    LinkedList<string> input = getArgs(Console.ReadLine(), ' ');
                    Console.WriteLine();
                    validInput = true;
                    if (input.First == null) //user hit enter
                    {
                        validInput = false; 
                        continue;
                    }
                    switch (input.First.Value)
                    {
                        case "help":
                            Console.WriteLine("[required arg] (optional arg)" + Environment.NewLine 
                                + "   ls (sort by pid = false) (use ignore list = true)\tList all running processes. Sorts by name and ignores those on the ignore list by default." + Environment.NewLine
                                + "   ig\tList all processes in the ignore list." + Environment.NewLine
                                + "   ig add [name]\tAdd a process name to the ignore list (won't list with ls)." + Environment.NewLine
                                + "   ig rm [name]\tRemove a process from the ignore list." + Environment.NewLine
                                + "   killall\tTerminate all running processes on the kill list." + Environment.NewLine
                                + "   kill [name]\tTerminate a named process, if running." + Environment.NewLine
                                + "   killpid [pid]\tTerminate a process with specified id, if running." + Environment.NewLine
                                + "   ki (true/false)\tList all processes in the kill list. true = list only running processes; false = list only non-running processes." + Environment.NewLine
                                + "   ki add [name]\tAdd a process name to the kill list." + Environment.NewLine
                                + "   ki rm [name]\tRemove a process from the kill list." + Environment.NewLine
                                + "   settings\tList the location on disk where settings are stored." + Environment.NewLine
                                + "   startup (true/false)\tWith no parameters, lists whether it is true that this program will launch on startup. Params set the value.");
                            break;
                        case "q":
                            quit();
                            break;
                        case "ls":
                            bool sortByPID = false;
                            bool useIgnoreList = true;
                            if (input.First.Next != null)
                            {
                                try {
                                    sortByPID = bool.Parse(input.First.Next.Value);
                                    //If 1st optional arg specified (sortByPID), 2nd must be specified (useIgnoreList) as well: 
                                    useIgnoreList = bool.Parse(input.First.Next.Next.Value);
                                } catch (Exception e) {
                                    validInput = false;
                                    break;
                                }
                            }
                            if (validInput) ProcessMonitor.listProcesses(sortByPID, useIgnoreList, settings.getSetting(header_ignore));
                            break;
                        case "ig":
                            if (input.First.Next == null) //mode 1: show ignore list
                            {
                                settings.displaySetting(header_ignore, header_ignore_name);
                            }
                            else
                            {
                                switch (input.First.Next.Value)
                                {
                                    case "add": //mode 2: add to ignore list
                                        if (input.First.Next.Next == null) validInput = false;
                                        else
                                        {
                                            if (settings.addToSetting(header_ignore, input.First.Next.Next.Value, header_ignore_name) < 0) validInput = false;
                                        }
                                        break;
                                    case "rm": //mode 3: remove from ignore list
                                        if (input.First.Next.Next == null) validInput = false;
                                        else
                                        {
                                            if (settings.removeFromSetting(header_ignore, input.First.Next.Next.Value, header_ignore_name) < 0) validInput = false;
                                        }
                                        break;
                                    default: validInput = false;
                                        break;
                                }
                            }
                            break;
                        case "killall":
                            if (input.First.Next != null) validInput = false;
                            else
                            {
                                ProcessMonitor.killall(settings.getSetting(header_kill));
                            }
                            break;
                        case "kill":
                            if (input.First.Next == null) validInput = false;
                            else
                            {
                                ProcessMonitor.killProcess(input.First.Next.Value);
                            }
                            break;
                        case "killpid":
                            if (input.First.Next == null) validInput = false;
                            else
                            {
                                int pid = stringToInt(input.First.Next.Value);
                                if (pid == -1)
                                {
                                    validInput = false;
                                    break;
                                }
                                ProcessMonitor.killProcess(pid);
                            }
                            break;
                        case "ki":
                            if (input.First.Next == null) //mode 1: Display kill list
                            {
                                settings.displaySetting(header_kill, header_kill_name);
                            }
                            else
                            {
                                switch (input.First.Next.Value)
                                {
                                    case "true": //mode 2: List all processes on kill list that are currently running
                                        settings.displaySetting(header_kill, header_kill_name + " (running)", true, true);
                                        break;
                                    case "false": //mode 3: List all processes on kill list that are currently non-running.
                                        settings.displaySetting(header_kill, header_kill_name + " (non-running)", true, false);
                                        break;
                                    case "add": //mode 4: Add process to kill list
                                        if (input.First.Next.Next == null) validInput = false;
                                        else
                                        {
                                            if (settings.addToSetting(header_kill, input.First.Next.Next.Value, header_kill_name) < 0) validInput = false;
                                        }
                                        break;
                                    case "rm": //mode 5: Remove process from kill list
                                        if (input.First.Next.Next == null) validInput = false;
                                        else
                                        {
                                            if (settings.removeFromSetting(header_kill, input.First.Next.Next.Value, header_kill_name) < 0) validInput = false;
                                        }
                                        break;
                                    default: validInput = false; break;
                                }
                            }
                            break;
                        case "settings":
                            Console.WriteLine(settings.SETTINGS_PATH);
                            break;
                        case "startup":
                            Console.WriteLine("Sorry, launch on startup is currently unimplemented.");
                            /*if (input.First.Next == null) //mode 1: Display setting value T/F for launch on startup
                            {
                                Console.WriteLine("Launch this application on startup: " + Environment.NewLine 
                                    + "   " + settings[2].First.Value);
                            }
                            else
                            {
                                switch (input.First.Next.Value)
                                {
                                    case "true":
                                        if (settings[2].First.Value != "true")
                                        {
                                            try
                                            {
                                                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                                                rk.SetValue(APPLICATION_NAME, Environment.CurrentDirectory + "\\" + APPLICATION_NAME, RegistryValueKind.String);
                                                settings[2].First.Value = "true";
                                                Console.WriteLine("Successfully set this application to launch on startup.");
                                                Console.WriteLine("   (Created registry string value in key '" + rk.Name + "' with name '" + APPLICATION_NAME + "')");
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine("FAILURE:" + Environment.NewLine + "   " + e.Message);
                                            }
                                        }
                                        else Console.WriteLine("This application is already set to launch on startup.");
                                        break;
                                    case "false":
                                        if (settings[2].First.Value != "false")
                                        {
                                            try
                                            {
                                                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                                                rk.DeleteValue(APPLICATION_NAME, true);
                                                settings[2].First.Value = "false";
                                                Console.WriteLine("Successfully changed settings to not launch on startup.");
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine("FAILURE:" + Environment.NewLine + "   " + e.Message);
                                            }
                                        }
                                        else Console.WriteLine("This application is already set to not launch on startup.");
                                        break;
                                    default: validInput = false; break;
                                }
                            }*/
                            break;
                        default: validInput = false;
                            break;
                    }
                    if (!validInput)
                    {
                        Console.WriteLine("Command not recognized."
                            + Environment.NewLine + "(Note: for multiple optional arguments, either all or none must be specified)");
                    }
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Separate a string into a linked list where each entry is a word/argument that was separated by (separator) in the original string.
        /// </summary>
        /// <param name="text">Input text to extract arguments from.</param>
        /// <param name="separator">Separator character between arguments.</param>
        /// <returns></returns>Linked list of arguments, in order from first to last.
        private static LinkedList<string> getArgs(string text, char separator)
        {
            char[] charArr = text.ToCharArray();
            int len = charArr.Length;
            int spaceIndex = -1; //index of last space we saw in charArr
            LinkedList<string> args = new LinkedList<string>();
            for (int i = 0; i < len; i++)
            {
                if (charArr[i] == separator || i == len - 1) //end of new argument
                {
                    if(i != len - 1)
                        args.AddLast(text.Substring(spaceIndex + 1, i - spaceIndex - 1));
                    else
                        args.AddLast(text.Substring(spaceIndex + 1, i - spaceIndex));
                    spaceIndex = i;
                }
            }
            return args;
        }

        private static void listArgs(LinkedList<string> argList)
        {
            int i = 0;
            LinkedListNode<string> cur = argList.First;
            while (cur != null)
            {
                Console.WriteLine("\tArg " + i.ToString() + ": '" + cur.Value + "'");
                cur = cur.Next;
                i++;
            }
        }

        /// <summary>
        /// Save settings and quit the program. Prompts user to try again if saving unsuccessful.
        /// </summary>
        public static void quit()
        {
            bool tryAgain = true;
            while (tryAgain && !settings.saveSettings())
            {
                Console.WriteLine("Try again (Y/N)?");
                char input = Console.ReadKey().KeyChar;
                switch (input)
                {
                    case 'Y': continue;
                    case 'N':
                        Console.WriteLine("Aborting.");
                        tryAgain = false;
                        break;
                }
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// Convert a string in base-10 format to a positive integer. Returns the integer, or -1 if none was detected.
        /// </summary>
        private static int stringToInt(string numericalString)
        {
            int len = numericalString.Length;
            char[] charArr = numericalString.ToCharArray();
            int toReturn = 0;
            int digits = 0;
            for (int i = len - 1; i >= 0; i--)
            {
                if (charArr[i] == '0') digits++;
                else if ((int)charArr[i] >= (int)'1' && (int)charArr[i] <= (int)'9')
                {
                    toReturn += ((int)charArr[i] - (int)'0') * (int)Math.Pow(10.0, digits);
                    digits++;
                }
            }
            if (digits == 0) return -1;
            return toReturn;
        }
    }
}
