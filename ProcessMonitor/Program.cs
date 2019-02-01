using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Threading;

namespace ProcessMonitor
{
    /**
     * Author      Scott Wolfskill
     * Created     06/17/2017
     * Last edit   01/31/2019
     * 
     * Simple command-line program for monitoring local processes and killing them efficiently and thoroughly if desired.
     */
    class Program
    {
        public const string APPLICATION_NAME = "ProcessMonitor"; //excludes file type .exe
        public static Settings settings = null;
        public static LinkedList<MonitorEvent> monitorEventList = null;

        public const char argSeparator = ' ';
        public const string header_ignore = "\\IGNORE\\";
        public const string header_kill = "\\KILL\\";
        public const string header_startup = "\\STARTUP\\";
        public const string header_ignore_name = "Ignore list";
        public const string header_kill_name = "Kill list";

        static void Main(string[] args)
        {
            Console.WriteLine("PROCESS MONITOR V2.0"
                    + Environment.NewLine + "Press CTRL+C to force exit at any time (however, settings won't be saved)."
                    + Environment.NewLine + "--------------------------------------------------------------------------");
            //Load settings
            string[] settingHeaders = new string[] { header_ignore, header_kill, header_startup };
            string settingsFolder = Environment.GetEnvironmentVariable("UserProfile") + "\\AppData\\Roaming\\Process Monitor";
            settings = new Settings(settingHeaders, settingsFolder);
            settings.loadSettings();
            //Initialize empty monitorEventList
            monitorEventList = new LinkedList<MonitorEvent>();
            //Start main command loop
            while (true)
            {
                bool validInput = false;
                while (!validInput)
                {
                    console_promptInput();
                    LinkedList<string> input = getArgs(Console.ReadLine(), argSeparator);
                    Console.WriteLine();
                    validInput = true;
                    if (input.First == null) //user hit enter
                    {
                        validInput = false; 
                        continue;
                    }
                    validInput = executeCommand(input);
                    if (!validInput)
                    {
                        Console.WriteLine("Command not recognized."
                            + Environment.NewLine + "(Note: for multiple optional arguments, either all or none must be specified)");
                    }
                }
                Console.WriteLine();
            }
        }

        private static void console_promptInput()
        {
            Console.WriteLine("Enter a command ('help' for more info, 'q' to quit):");
        }

        private static bool isInputValid(LinkedList<string> input)
        {
            return executeCommand(input, false, false); //don't output to console or execute anything
        }

        /// <summary>
        /// Execute a command (or determine if an input command is valid: set execute:=false), with optional output to console.
        /// </summary>
        /// <param name="input">Input arguments grouped into a string Linked List.</param>
        /// <param name="outputToConsole">If false, does not output text to console.</param>
        /// <param name="execute">If false, does not execute commands (used to determine if input is valid or not).</param>
        /// <returns>true if input was valid (a command could be executed), or false otherwise.</returns>
        private static bool executeCommand(LinkedList<string> input, bool outputToConsole = true, bool execute = true)
        {
            //bool validInput = true;
            switch (input.First.Value)
            {
                case "help":
                    if (outputToConsole)
                    {
                        Console.WriteLine("[required arg] (optional arg)" + Environment.NewLine
                            + "Interval format is hh:mm:ss(.xxx)" + Environment.NewLine
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
                            + "   monitor [interval] [repetitions] [console output (true/false)] [command with or without args]\tExecute a command continuously every interval for repetitions (inf for continuous), e.g. 'monitor 00:01:00 inf true killall'" + Environment.NewLine
                            + "   monitors\tList all running monitors." + Environment.NewLine
                            + "   settings\tList the location on disk where settings are stored." + Environment.NewLine
                            + "   startup (true/false)\tWith no arguments, lists whether it is true that this program will launch on startup. Args set the value.");
                    }
                    break;
                case "q":
                    if (execute) quit();
                    break;
                case "stop":
                    //tempMonitorEvent.stop();
                    break;
                case "ls":
                    bool sortByPID = false;
                    bool useIgnoreList = true;
                    if (input.First.Next != null)
                    {
                        try
                        {
                            sortByPID = bool.Parse(input.First.Next.Value);
                            //If 1st optional arg specified (sortByPID), 2nd must be specified (useIgnoreList) as well: 
                            useIgnoreList = bool.Parse(input.First.Next.Next.Value);
                        }
                        catch (Exception e)
                        {
                            return false;
                        }
                    }
                    if (execute && outputToConsole) ProcessMonitor.listProcesses(sortByPID, useIgnoreList, settings.getSetting(header_ignore));
                    break;
                case "ig":
                    if (input.First.Next == null) //mode 1: show ignore list
                    {
                        if(outputToConsole) settings.displaySetting(header_ignore, header_ignore_name);
                    }
                    else
                    {
                        switch (input.First.Next.Value)
                        {
                            case "add": //mode 2: add to ignore list
                                if (input.First.Next.Next == null || settings.addToSetting(header_ignore, input.First.Next.Next.Value, header_ignore_name, outputToConsole, execute) < 0)
                                    return false;
                                break;
                            case "rm": //mode 3: remove from ignore list
                                if (input.First.Next.Next == null || settings.removeFromSetting(header_ignore, input.First.Next.Next.Value, header_ignore_name, outputToConsole, execute) < 0)
                                    return false;
                                break;
                            default: 
                                return false;
                        }
                    }
                    break;
                case "killall":
                    if (input.First.Next != null) return false;
                    if (execute)
                    {
                        ProcessMonitor.killall(settings.getSetting(header_kill));
                    }
                    break;
                case "kill":
                    if (input.First.Next == null) return false;
                    if(execute)
                    {
                        ProcessMonitor.killProcess(input.First.Next.Value);
                    }
                    break;
                case "killpid":
                    if (input.First.Next == null)
                    {
                        return false;
                    }
                    int pid = stringToInt(input.First.Next.Value);
                    if (pid == -1)
                    {
                        return false;
                    }
                    if(execute) ProcessMonitor.killProcess(pid);
                    break;
                case "ki":
                    if (input.First.Next == null) //mode 1: Display kill list
                    {
                        if(outputToConsole) settings.displaySetting(header_kill, header_kill_name);
                    }
                    else
                    {
                        switch (input.First.Next.Value)
                        {
                            case "true": //mode 2: List all processes on kill list that are currently running
                                if (outputToConsole) settings.displaySetting(header_kill, header_kill_name + " (running)", true, true);
                                break;
                            case "false": //mode 3: List all processes on kill list that are currently non-running.
                                if (outputToConsole) settings.displaySetting(header_kill, header_kill_name + " (non-running)", true, false);
                                break;
                            case "add": //mode 4: Add process to kill list
                                if (input.First.Next.Next == null || settings.addToSetting(header_kill, input.First.Next.Next.Value, header_kill_name, outputToConsole, execute) < 0)
                                    return false;
                                break;
                            case "rm": //mode 5: Remove process from kill list
                                if (input.First.Next.Next == null || settings.removeFromSetting(header_kill, input.First.Next.Next.Value, header_kill_name, outputToConsole, execute) < 0)
                                    return false;
                                break;
                            default: 
                                return false;
                        }
                    }
                    break;
                case "monitor": //usage: monitor [interval] [repetitions] [console output] [command with or without args]
                    if (input.First.Next == null || input.First.Next.Next == null || input.First.Next.Next.Next == null)
                    {
                        return false;
                    }
                    //Parse first 3 arguments
                    long arg_interval;
                    double arg_repetitions;
                    bool arg_consoleOutput;
                    try
                    {
                        arg_interval = parseInterval(input.First.Next.Value); //attempt to parse interval in format hh:mm:ss(.xxx)
                        arg_consoleOutput = bool.Parse(input.First.Next.Next.Next.Value);
                        if (input.First.Next.Next.Value == "inf")
                        {
                            arg_repetitions = double.PositiveInfinity;
                        }
                        else
                        {
                            int reps = int.Parse(input.First.Next.Next.Value);
                            arg_repetitions = (double) reps;
                        }
                    }
                    catch (Exception e)
                    {
                        return false;
                    }
                    //Check validity of command argument (with or w/o args):
                    input.RemoveFirst(); //remove "monitor"
                    string monitorEventCommand = argsToString(input, argSeparator);
                    input.RemoveFirst(); //remove [interval]
                    input.RemoveFirst(); //remove [repetitions]
                    input.RemoveFirst(); //remove [console output]
                    if (!isInputValid(input))
                    {
                        return false;
                    }
                    //Make sure not adding a duplicate MonitorEvent.
                    int foundID;
                    if(MonitorEvent.containsMonitorEvent(monitorEventList, monitorEventCommand, out foundID))
                    {
                        if (outputToConsole) Console.WriteLine("Monitor #" + foundID.ToString() + " with those parameters is already active.");
                        return false;
                    }
                    //6. Create new MonitorEvent for command. Name it with its command as a string.
                    if (execute)
                    {
                        TimerCallback eventFunction = _executeCommand_outputToConsole;
                        if (!arg_consoleOutput) eventFunction = _executeCommand_noOutputToConsole;
                        DateTime created = DateTime.Now;
                        MonitorEvent monitorEvent = new MonitorEvent(monitorEventCommand, eventFunction, (object)input, arg_interval, 
                                                                     arg_interval, created, arg_repetitions, arg_consoleOutput, true);
                        monitorEventList.AddLast(monitorEvent);
                        if (outputToConsole)
                        {
                            Console.WriteLine("Monitor #" + monitorEvent.id.ToString() + " created successfully on " + created.ToString() + ".");
                        }
                    }
                    break;
                case "monitors":
                    if (input.First.Next == null)
                    {
                        if (outputToConsole) MonitorEvent.displayMonitorEvents(monitorEventList);
                    }
                    else
                    {
                        //TODO
                    }
                    break;
                case "settings":
                    if(outputToConsole) Console.WriteLine(settings.SETTINGS_PATH);
                    break;
                case "startup":
                    if(outputToConsole) Console.WriteLine("Sorry, launch on startup is currently unimplemented.");
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
                default: 
                    return false;
            }
            return true;
        }

        private static void _executeCommand_outputToConsole(Object command)
        {
            _executeCommand(command, true);
        }

        private static void _executeCommand_noOutputToConsole(Object command)
        {
            _executeCommand(command, false);
        }

        private static void _executeCommand(Object command, bool outputToConsole)
        {
            LinkedList<String> commandInput = null;
            commandInput = (LinkedList<String>)command;
            executeCommand(commandInput, outputToConsole, true);
            if (outputToConsole)
            {
                console_promptInput();
            }
        }

        /// <summary>
        /// Separate a string into a linked list where each entry is a word/argument that was separated by (separator) in the original string.
        /// </summary>
        /// <param name="text">Input text to extract arguments from.</param>
        /// <param name="separator">Separator character between arguments.</param>
        /// <returns></returns>Linked list of arguments, in order from first to last.
        private static LinkedList<string> getArgs(string text, char separator = ' ')
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

        /// <summary>
        /// Get a string representing a list of ordered arguments, each separated by a separator.
        /// </summary>
        /// <param name="argList">Linked List of arguments as strings.</param>
        /// <param name="separator">separator character to put between args.</param>
        /// <param name="consoleOutput">If true, outputs each arg on a separate line to the console.</param>
        /// <returns>string containing all args in order, each separated by separator.</returns>
        private static string argsToString(LinkedList<string> argList, char separator = ' ', bool consoleOutput = false)
        {
            string output = "";
            int i = 0;
            LinkedListNode<string> arg = argList.First;
            while (arg != null)
            {
                output += arg.Value;
                if (arg.Next != null)
                {
                    output += separator;
                }
                if(consoleOutput) Console.WriteLine("\tArg " + i.ToString() + ": '" + arg.Value + "'");
                arg = arg.Next;
                i++;
            }
            return output;
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
        /// Parse an input string into a time interval in milliseconds. Throws an exception on failure.
        /// </summary>
        /// <param name="input">Interval in format hh:mm:ss(.xxx) where (.xxx) milliseconds are optional. Do not include date.</param>
        /// <returns>Parsed interval in milliseconds as long.</returns>
        private static long parseInterval(string input)
        {
            long intervalMillis = -1L;
            if (input == null || input.Contains("/")) //date not accepted
            {
                throw new FormatException("parseInterval: input cannot be null or include the date (was '" + input + "')");
            }
            // DateTime automatically sets to current date when no date is specified. Subtract from current date to cancel that.
            DateTime zero = DateTime.Parse("00:00:00");
            DateTime inputDate = DateTime.Parse(input);
            const long ticksPerMilli = 10000L; //10,000 ticks per ms
            intervalMillis = (inputDate.Ticks - zero.Ticks) / ticksPerMilli;
            //Console.WriteLine("interval " + intervalMillis.ToString() + "ms");
            return intervalMillis;
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
