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
     * Created     01/30/2019
     * Last edit   01/30/2019
     * 
     * Static class to execute various process monitoring commands and output information to the console.
     */
    static class ProcessMonitor
    {
        /// <summary>
        /// Lists currently running processes as output to the console.
        /// </summary>
        /// <param name="sortByPID">If true, sorts processes by process PID. If false, sorts them by process name.</param>
        /// <param name="useIgnoreList">If true, ignores processes on the ignore list. If false, lists all running processes.</param>
        /// <param name="ignoreList">Linked List of process names on the ignore list.</param>
        public static void listProcesses(bool sortByPID, bool useIgnoreList, LinkedList<string> ignoreList = null)
        {
            Process[] running = Process.GetProcesses();
            string msg = running.Length.ToString() + " processes running locally.";

            //1. Sort process list
            IEnumerable<Process> sortQuery;
            if (sortByPID)
            {
                sortQuery = running.OrderBy(process => process.Id);
            }
            else //sort by process name
            {
                sortQuery = running.OrderBy(process => process.ProcessName);
            }

            //2. List processes 
            Console.WriteLine("pid | name" + Environment.NewLine + "----------------");
            if (useIgnoreList) //filter out any processes on ignore list
            {
                int ignoredCount = 0; //count of how many processes were ignored due to being on the ignore list.
                bool ignore = false;
                foreach(Process process in sortQuery) 
                {
                    ignore = false;
                    LinkedListNode<string> ignoreListElement = ignoreList.First;
                    while (ignoreListElement != null) //iterate through all elements of ignore list
                    {
                        if (process.ProcessName == ignoreListElement.Value) //check ignore list
                        {
                            ignoredCount++;
                            ignore = true;
                            break;
                        }
                        ignoreListElement = ignoreListElement.Next;
                    }
                    if (!ignore)
                    {
                        Console.WriteLine(process.Id.ToString() + " | " + process.ProcessName);
                    }
                }
                Console.WriteLine("----------------" + Environment.NewLine + msg);
                Console.WriteLine((running.Length - ignoredCount).ToString() + " listed, " + ignoredCount.ToString() + " ignored.");
            }
            else //list all running processes
            {
                foreach(Process process in sortQuery)
                {
                    Console.WriteLine(process.Id.ToString() + " | " + process.ProcessName);
                }
                Console.WriteLine("----------------" + Environment.NewLine + msg);
            }
        }

        /// <summary>
        /// Kill a process, if running.
        /// </summary>
        /// <param name="processName">Name of the running process to kill.</param>
        /// <param name="outputToConsole">If true, outputs info to console.</param>
        public static void killProcess(String processName, bool outputToConsole)
        {
            LinkedList<string> killList = new LinkedList<string>();
            killList.AddFirst(processName);
            killall(killList, outputToConsole);
        }

        /// <summary>
        /// Kill a process by specified PID, if running. Output log to console.
        /// </summary>
        /// <param name="pid">Process PID to kill.</param>
        /// <param name="outputToConsole">If true, outputs info to console.</param>
        public static void killProcess(int pid, bool outputToConsole)
        {
            //1. Check that process w/ pid exists
            Process proc = null;
            try
            {
                proc = Process.GetProcessById(pid);
            }
            catch (Exception e)
            {
                if (outputToConsole) Console.WriteLine("No process with pid " + pid.ToString() + " found.");
                return;
            }
            //2. Kill
            Console.WriteLine("Process with pid " + pid.ToString() + " found. Terminating...");
            try
            {
                proc.Kill();
                if (outputToConsole) Console.WriteLine("Terminated process successfully.");
            }
            catch (Exception e)
            {
                if (outputToConsole) Console.WriteLine("FAILURE:" + Environment.NewLine + "\t" + e.Message.ToString());
            }
        }

        /// <summary>
        /// Kill all running processes on a kill list. Output log to console.
        /// </summary>
        /// <param name="killList">Linked List of process names on the kill list.</param>
        /// <param name="outputToConsole">If true, outputs info about processes killed to console.</param>
        public static void killall(LinkedList<string> killList, bool outputToConsole)
        {
            bool exists = false;
            short numTerminated = 0;
            try
            {
                LinkedListNode<string> killListElement = killList.First; //start of Kill list
                while (killListElement != null)
                {
                    Process[] proc = Process.GetProcessesByName(killListElement.Value);
                    if (proc.Length > 0)
                    {
                        if (outputToConsole)
                        {
                            if (!exists)
                            {
                                Console.WriteLine("Terminating...");
                            }
                            Console.WriteLine("   " + killListElement.Value + " (" + proc.Length.ToString() + ")");
                        }
                        foreach (Process p in proc)
                        {
                            p.Kill();
                            numTerminated++;
                        }
                        exists = true;
                    }
                    killListElement = killListElement.Next;
                }
            }
            catch (Exception e)
            {
                if (outputToConsole) Console.WriteLine("FAILURE:" + Environment.NewLine + "\t" + e.Message.ToString());
            }
            if (outputToConsole)
            {
                if (exists) Console.WriteLine("Terminated " + numTerminated.ToString() + " process(es) successfully.");
                else Console.WriteLine("Nothing to kill: no processes specified were running.");
            }
        }

        /// <summary>
        /// Remove a MonitorEvent from a list by id, stopping it first if running.
        /// </summary>
        /// <param name="monitorEventList">LinkedList of MonitorEvents to look in.</param>
        /// <param name="monitorEventID">ID of MonitorEvent to search for in monitorEventList.</param>
        /// <param name="outputToConsole">If true, output info to console.</param>
        public static void monitorEvent_rm(LinkedList<MonitorEvent> monitorEventList, int monitorEventID, bool outputToConsole)
        {
            if (monitorEventList.Count == 0)
            {
                if (outputToConsole) Console.WriteLine("Cannot remove monitor; no monitors have been created yet.");
                return;
            }
            foreach (MonitorEvent monitorEvent in monitorEventList)
            {
                if (monitorEvent.id == monitorEventID)
                {
                    if (monitorEvent.running())
                    {
                        monitorEvent.stop(outputToConsole);
                    }
                    monitorEventList.Remove(monitorEvent);
                    if (outputToConsole) Console.WriteLine("Monitor #" + monitorEvent.id + " '" + monitorEvent.name + "' removed from monitors successfully.");
                    return;
                }
            }
            if (outputToConsole) Console.WriteLine("No monitor with id #" + monitorEventID.ToString() + " was found.");
        }

        /// <summary>
        /// Start all stoppped MonitorEvents in a list, if any.
        /// </summary>
        /// <param name="monitorEventList">LinkedList of MonitorEvents.</param>
        /// <param name="outputToConsole">If true, output info to console.</param>
        public static void monitorEvent_startall(LinkedList<MonitorEvent> monitorEventList, bool outputToConsole)
        {
            if (monitorEventList.Count == 0)
            {
                if (outputToConsole) Console.WriteLine("Cannot start monitors; no monitors have been created yet.");
                return;
            }
            int numStarted = 0;
            foreach (MonitorEvent monitorEvent in monitorEventList)
            {
                if (!monitorEvent.running())
                {
                    monitorEvent.start(outputToConsole);
                    numStarted++;
                }
            }
            if (outputToConsole)
            {
                if (numStarted == 0)
                {
                    Console.WriteLine("No monitors were started; all " + monitorEventList.Count.ToString() + " are currently running.");
                }
                else
                {
                    Console.WriteLine(numStarted.ToString() + " monitor(s) started.");
                }
            }
        }

        /// <summary>
        /// Stop all running MonitorEvents in a list, if any.
        /// </summary>
        /// <param name="monitorEventList">LinkedList of MonitorEvents.</param>
        /// <param name="outputToConsole">If true, output info to console.</param>
        public static void monitorEvent_stopall(LinkedList<MonitorEvent> monitorEventList, bool outputToConsole)
        {
            if (monitorEventList.Count == 0)
            {
                if (outputToConsole) Console.WriteLine("Cannot stop monitors; no monitors have been created yet.");
                return;
            }
            int numStopped = 0;
            foreach (MonitorEvent monitorEvent in monitorEventList)
            {
                if (monitorEvent.running())
                {
                    monitorEvent.stop(outputToConsole);
                    numStopped++;
                }
            }
            if (outputToConsole)
            {
                if (numStopped == 0)
                {
                    Console.WriteLine("No monitors were stopped; all " + monitorEventList.Count.ToString() + " are currently stopped.");
                }
                else
                {
                    Console.WriteLine(numStopped.ToString() + " monitor(s) stopped.");
                }
            }
        }
    }
}
