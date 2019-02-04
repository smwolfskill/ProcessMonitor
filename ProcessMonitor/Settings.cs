using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace ProcessMonitor
{
    /**
     * Author      Scott Wolfskill
     * Created     01/30/2017
     * Last edit   01/31/2019
     * 
     * Stores, loads and saves settings information for an application. A single setting is a LinkedList<string>.
     */
    class Settings
    {
        public readonly string SETTINGS_FOLDER;
        public readonly int NUM_SETTINGS;
        public readonly string SETTINGS_NAME; //filename of the stored settings file (default "settings.txt")
        public readonly string SETTINGS_PATH; //full path of stored settings file (SETTINGS_FOLDER/SETTINGS_NAME)
        public readonly string[] settingHeaders;

        public LinkedList<string>[] settings; //each setting will have an information list

        public Settings(string[] settingHeaders, string settingsFolder, string settingsFilename = "settings.txt")
        {
            this.SETTINGS_FOLDER = settingsFolder;
            this.settingHeaders = settingHeaders;
            this.NUM_SETTINGS = settingHeaders.Length;
            this.SETTINGS_NAME = settingsFilename;
            this.SETTINGS_PATH = SETTINGS_FOLDER + "\\" + SETTINGS_NAME;
        }

        /// <summary>
        /// Load settings from SETTINGS_PATH, or creates the path and settings file if it does not exist.
        /// </summary>
        public void loadSettings()
        {
            //Check settings path, and if not found it is created.
            if (!Directory.Exists(SETTINGS_FOLDER)) //new launch: create settings path and file
            {
                Directory.CreateDirectory(SETTINGS_FOLDER);
            }
            if (!File.Exists(SETTINGS_PATH)) //new launch: create settings file
            {
                string writeString = ""; //text to write to settings file
                settings = new LinkedList<string>[NUM_SETTINGS];
                for (int i = 0; i < NUM_SETTINGS; i++)
                {
                    settings[i] = new LinkedList<string>();
                    writeString += settingHeaders[i] + "\r\n";
                    if (i == 2) writeString += "false\r\n"; //default setting: don't launch this app on startup
                }
                File.WriteAllText(SETTINGS_PATH, writeString);
            }
            else
            {
                parseSettings();
            }
        }

        /// <summary>
        /// Gets a setting by its header.
        /// </summary>
        /// <param name="settingHeader">Setting header to get the settings of.</param>
        /// <param name="partialMatch">if true, returns a setting if specified settingHeader is contained within its header.</param>
        /// <returns>Setting as Linked List, or null if not found.</returns>
        public LinkedList<string> getSetting(string settingHeader, bool partialMatch = false)
        {
            for (int i = 0; i < settingHeaders.Length; i++)
            {
                if (settingHeader == settingHeaders[i] || (partialMatch && settingHeaders[i].Contains(settingHeader)))
                {
                    return settings[i];
                }
            }
            return null; //not found
        }

        /// <summary>
        /// Display a setting in the console.
        /// </summary>
        /// <param name="settingHeader">Setting header to get the settings of.</param>
        /// <param name="settingName">The name of the setting that we are listing.</param>
        /// <param name="filterRunning">If true, uses (running) param to filter out processes based on whether or not they are running.</param>
        /// <param name="running">(filterRunning) must be true to have any effect. true -> list only running processes in this setting. false -> list only non-running.</param>
        public void displaySetting(string settingHeader, string settingName, bool filterRunning = false, bool running = true, bool partialMatch = false)
        {
            Console.WriteLine(settingName + ":" + Environment.NewLine + "#  | name"
                                    + Environment.NewLine + "----------");
            int i = 0;
            int runningCount = 0; //only relevant if filterRunning == true. If running, count of how many in list are running. If !running, count of how many in list are not running.
            LinkedList<string> setting = getSetting(settingHeader, partialMatch); //if returns null (not found), will throw exception
            LinkedListNode<string> settingElement = setting.First;
            while (settingElement != null)
            {
                if (filterRunning)
                {
                    Process[] proc = Process.GetProcessesByName(settingElement.Value);
                    if ((running && proc.Length > 0) || (!running && proc.Length == 0))
                    {
                        Console.WriteLine("#" + i.ToString() + " | " + settingElement.Value);
                        runningCount++;
                    }
                }
                else Console.WriteLine("#" + i.ToString() + " | " + settingElement.Value);
                settingElement = settingElement.Next;
                i++;
            }
            if (filterRunning)
            {
                Console.WriteLine("----------");
                if (running) Console.WriteLine(runningCount.ToString() + " running, " + (setting.Count - runningCount).ToString() + " non-running.");
                else Console.WriteLine((setting.Count - runningCount).ToString() + " running, " + runningCount.ToString() + " non-running.");
            }
        }

        /// <summary>
        /// Adds a value to a setting. Does not add duplicates.
        /// </summary>
        /// <param name="settingHeader">Setting header to get the settings of.</param>
        /// <param name="value">Value to add to the setting.</param>
        /// <param name="settingName">Descriptive name of the setting to output to console.</param>
        /// <param name="partialMatch">If true, fetches the setting if specified settingHeader is contained within its header.</param>
        /// <returns>-2 if reserved word, -1 if invalid information, 0 if success, 1 if already present (no action needed)</returns>
        public int addToSetting(string settingHeader, string value, string settingName, bool outputToConsole, bool execute, bool partialMatch = false)
        {
            //1. Check for invalid value
            if (value == null || value == "") return -1;
            //2. Check for reserved keywords (setting headers)
            int header = contained(value, settingHeaders);
            if (header >= 0)
            {
                if(outputToConsole) Console.WriteLine(settingHeaders[header] + " is a reserved keyword and cannot be a process name.");
                return -2;
            }
            //3. Check if already in list
            LinkedList<string> setting = getSetting(settingHeader, partialMatch); //if returns null (not found), will throw exception
            LinkedListNode<string> settingElement = setting.First;
            while (settingElement != null)
            {
                if (settingElement.Value == value) //already present; no action needed.
                {
                    if(outputToConsole) Console.WriteLine("Process '" + value + "' is already in the " + settingName + ".");
                    return 1;
                }
                settingElement = settingElement.Next;
            }
            //4. Not already in list, so add:
            if (execute)
            {
                setting.AddLast(value);
                if (outputToConsole) Console.WriteLine("Added process '" + value + "' to the " + settingName + ".");
            }
            return 0;
        }

        /// <summary>
        /// Removes a value from a setting, if it exists.
        /// </summary>
        /// <param name="settingHeader">Setting header to get the settings of.</param>
        /// <param name="value">Value to add to the setting.</param>
        /// <param name="settingName">Descriptive name of the setting to output to console.</param>
        /// <param name="partialMatch">If true, fetches the setting if specified settingHeader is contained within its header.</param>
        /// <returns>-1 if invalid information, 0 if success, 1 if not found (no action needed)</returns>
        public int removeFromSetting(string settingHeader, string value, string settingName, bool outputToConsole, bool execute, bool partialMatch = false)
        {
            //1. Check for invalid value
            if (value == null || value == "") return -1;
            //2. Make sure it's in the list:
            bool present = false;
            LinkedList<string> setting = getSetting(settingHeader, partialMatch); //if returns null (not found), will throw exception
            LinkedListNode<string> settingElement = setting.First;
            while (settingElement != null)
            {
                if (settingElement.Value == value)
                {
                    present = true;
                    break;
                }
                settingElement = settingElement.Next;
            }
            if (!present)
            {
                if(outputToConsole) Console.WriteLine("Process '" + value + "' not found in " + settingName + ".");
                return 1;
            }
            //3. In list, so remove:
            if (execute)
            {
                setting.Remove(settingElement);
                if (outputToConsole) Console.WriteLine("Removed process '" + value + "' from the " + settingName + ".");
            }
            return 0;
        }

        /// <summary>
        /// Attempt to save settings at SETTINGS_PATH, and log success or failure to console.
        /// </summary>
        /// <returns>true if successful, false if exception encountered.</returns>
        public bool saveSettings()
        {
            try
            {
                FileStream fs = File.Open(SETTINGS_PATH, FileMode.Create);
                LinkedListNode<string> cur;
                for (int i = 0; i < NUM_SETTINGS; i++)
                {
                    writeToOpenFile(fs, settingHeaders[i] + Environment.NewLine); //1. Write header
                    cur = settings[i].First;
                    while (cur != null) //2. Write all setting information
                    {
                        writeToOpenFile(fs, cur.Value + Environment.NewLine);
                        cur = cur.Next;
                    }
                }
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("SAVE SETTINGS FAILURE: " + e.Message);
                return false;
            }
            Console.WriteLine("Saved settings successfully.");
            return true;
        }

        protected static void writeToOpenFile(FileStream openFile, string toWrite)
        {
            writeToOpenFile(openFile, toWrite.ToCharArray());
        }

        protected static void writeToOpenFile(FileStream openFile, char[] toWrite)
        {
            int len = toWrite.Length;
            for (int i = 0; i < len; i++)
            {
                openFile.WriteByte((byte)toWrite[i]);
            }
        }

        /// <summary>
        /// Load settings from SETTINGS_PATH.
        /// </summary>
        /// <param name="showLog">If true, logs to console settings parsed.</param>
        protected void parseSettings(bool showLog = false)
        {
            /* Every piece of information is separated by a linebreak, 
             * and certain lines (headers) will denote which setting we're referring to. */
            settings = new LinkedList<string>[NUM_SETTINGS];
            for (int i = 0; i < settingHeaders.Length; i++)
            {
                settings[i] = new LinkedList<string>();
            }
            int headerIndex = -1; //which setting we're currently parsing
            IEnumerable<string> lines = File.ReadLines(SETTINGS_PATH);
            foreach (string line in lines)
            {
                if (showLog) Console.WriteLine("   Cur setting read: '" + line + "'");
                if (headerIndex + 1 < NUM_SETTINGS && line == settingHeaders[headerIndex + 1]) //start next setting section
                {
                    headerIndex++;
                    if (showLog) Console.WriteLine("\tLine matched header " + headerIndex.ToString());
                }
                else //more information for current setting
                {
                    settings[headerIndex].AddLast(line);
                }
            }
        }

        /// <summary>
        /// Determine if a string toLookFor is present in an array of strings toSearch.
        /// </summary>
        /// <param name="toLookFor">string to search for.</param>
        /// <param name="toSearch">string array to look for toLookFor.</param>
        /// <returns>First index of toSearch that equals toLookFor, or -1 if not found.</returns>
        protected static int contained(string toLookFor, string[] toSearch)
        {
            int len = toSearch.Length;
            for (int i = 0; i < len; i++)
            {
                if (toLookFor == toSearch[i]) return i;
            }
            return -1;
        }

    }
}
