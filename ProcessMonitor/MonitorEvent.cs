using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ProcessMonitor
{
    /**
     * Author      Scott Wolfskill
     * Created     01/30/2019
     * Last edit   01/30/2019
     * 
     * Class representing a timed event that usually occurs continuously until stopped,
     * e.g. continuous executing of a command every time interval.
     */
    class MonitorEvent
    {
        public readonly string name;
        public readonly int id; //unique ID for this MonitorEvent
        public readonly bool outputToConsole;
        public readonly DateTime created; //date and time this MonitorEvent was created

        protected Timer timer;
        protected TimerCallback eventFunction;
        protected object eventFunctionParam;
        protected long dueTime;
        protected long period;
        protected double repetitions;

        protected static int monitorEventCount = 0; //number of created MonitorEvents

        /// <summary>
        /// Determines if a list of MonitorEvents contains a MonitorEvent with the same name as nameToLookFor.
        /// </summary>
        /// <param name="monitorEventList">Linked List of MonitorEvents.</param>
        /// <param name="nameToLookFor">string name of a monitorEvent to look for.</param>
        /// <param name="foundID">out int that will be set to the MonitorEvent's id found in monitorEventList, or -1 if not found.</param>
        /// <returns>true if monitorEventList has a MonitorEvent with the same name as nameToLookFor, false otherwise.</returns>
        public static bool containsMonitorEvent(LinkedList<MonitorEvent> monitorEventList, string nameToLookFor, out int foundID)
        {
            foundID = -1;
            if (monitorEventList == null)
            {
                return false;
            }
            foreach (MonitorEvent monitorEvent in monitorEventList)
            {
                if (monitorEvent.name == nameToLookFor)
                {
                    foundID = monitorEvent.id;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets a MonitorEvent from a list by id.
        /// </summary>
        /// <param name="monitorEventList">LinkedList of MonitorEvents to search in.</param>
        /// <param name="monitorEventID">id of MonitorEvent to find in monitorEventList.</param>
        /// <returns>MonitorEvent with id monitorEventID, or null if not found.</returns>
        public static MonitorEvent getMonitorEventByID(LinkedList<MonitorEvent> monitorEventList, int monitorEventID)
        {
            MonitorEvent found = null;
            foreach (MonitorEvent monitorEvent in monitorEventList)
            {
                if (monitorEvent.id == monitorEventID)
                {
                    return monitorEvent;
                }
            }
            return found;
        }

        public static void displayMonitorEvents(LinkedList<MonitorEvent> monitorEventList)
        {
            int activeCount = 0;
            Console.WriteLine("Monitors:");
            if (monitorEventList.Count > 0)
            {
                Console.WriteLine("# | running | name descriptor | interval (ms) | repetitions | console output | created");
                Console.WriteLine("--------------------------------------------------------------------------------------");
                foreach (MonitorEvent m in monitorEventList)
                {
                    bool isRunning = m.running();
                    Console.WriteLine(m.id.ToString() + " | " + isRunning.ToString() + " | " + m.name + " | " + m.period.ToString() + " | "
                                    + m.repetitions.ToString() + " | " + m.outputToConsole.ToString() + " | " + m.created.ToString());
                    if (isRunning)
                    {
                        activeCount++;
                    }
                }
                Console.WriteLine("--------------------------------------------------------------------------------------");
            }
            int inactiveCount = monitorEventList.Count - activeCount;
            Console.WriteLine(activeCount.ToString() + " running, " + inactiveCount.ToString() + " stopped.");
        }

        /// <summary>
        /// Create a MonitorEvent.
        /// </summary>
        /// <param name="name">Name for the MonitorEvent. Expected to be a description of what event the monitor is triggering (e.g. a command w/ args).</param>
        /// <param name="eventFunction">TimerCallback function for the MonitorEvent to call after dueTime and every period after.</param>
        /// <param name="eventFunctionParam">Parameter to pass to eventFunction.</param>
        /// <param name="dueTime">Time in milliseconds before the first call to eventFunction.</param>
        /// <param name="period">Time in milliseconds between the first call to eventFunction and the second call, and so on.</param>
        /// <param name="created">DateTime marking the creation of this MonitorEvent (usually DateTime.Now)</param>
        /// <param name="repetitions">Number of times to call eventFunction (infinity by default).</param>
        /// <param name="outputToConsole">If true, outputs a descriptive message to console every time before eventFunction is called.</param>
        /// <param name="startImmediately">If true, starts the MonitorEvent immediately. If false, must start it manually with start().</param>
        public MonitorEvent(string name, TimerCallback eventFunction, object eventFunctionParam, long dueTime, long period, DateTime created,
                            double repetitions = double.PositiveInfinity, bool outputToConsole = true, bool startImmediately = true)
        {
            timer = null;
            this.name = name;
            this.eventFunction = eventFunction;
            this.eventFunctionParam = eventFunctionParam;
            this.dueTime = dueTime;
            this.period = period;
            this.created = created;
            this.repetitions = repetitions;
            this.id = ++monitorEventCount; //start IDs at 1 for user convenience
            this.outputToConsole = outputToConsole;
            if (startImmediately)
            {
                start(false);
            }
        }

        /// <summary>
        /// Starts the MonitorEvent, or throws InvalidOperationException if already started.
        /// </summary>
        /// <param name="outputToConsole">If true, outputs message to console saying which MonitorEvent was started.</param>
        public void start(bool outputToConsole)
        {
            if (timer != null)
            {
                throw new System.InvalidOperationException("Attempted to start already active MonitorEvent #" + id.ToString() + " '" + name + "'!");
            }
            else
            {
                timer = new Timer(timerCallback, eventFunctionParam, dueTime, period);
                if (outputToConsole) Console.WriteLine("Monitor #" + id + " '" + name + "' started.");
            }
        }

        /// <summary>
        /// Stops the MonitorEvent.
        /// </summary>
        /// <param name="outputToConsole">If true, outputs message to console saying which MonitorEvent was stopped.</param>
        public void stop(bool outputToConsole)
        {
            timer.Dispose();
            timer = null;
            if (outputToConsole) Console.WriteLine("Monitor #" + id + " '" + name + "' stopped.");
            //TODO: prompt to save log?
        }

        /// <summary>
        /// Toggles the MonitorEvent (starts if stopped, or stops if running).
        /// </summary>
        /// <param name="outputToConsole">If true, outputs message to console.</param>
        public void toggle(bool outputToConsole)
        {
            if (running())
            {
                stop(outputToConsole);
            }
            else
            {
                start(outputToConsole);
            }
        }

        /// <summary>
        /// Determine if a MonitorEvent is running.
        /// </summary>
        /// <returns>True if this MonitorEvent is running, false otherwise.</returns>
        public bool running()
        {
            return (timer != null);
        }

        /// <summary>
        /// Change the interval for this MonitorEvent.
        /// </summary>
        /// <param name="newDueTime">New amount of time before the MonitorEvent performs its first repetition.</param>
        /// <param name="newPeriod">New amount of time in between 1st and 2nd repetition, and so on.</param>
        /// <param name="outputToConsole">If true, output info to console.</param>
        public void changeInterval(long newDueTime, long newPeriod, bool outputToConsole)
        {
            if (running())
            {
                timer.Change(newDueTime, newPeriod);
            }
            this.dueTime = newDueTime;
            this.period = newPeriod;
            if (outputToConsole) Console.WriteLine("Monitor #" + id + " '" + name + "' interval changed to " + newPeriod.ToString() + "ms.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newRepetitions"></param>
        /// <param name="?"></param>
        public void changeRepetitions(double newRepetitions, bool outputToConsole)
        {
            this.repetitions = newRepetitions;
            if (outputToConsole) Console.WriteLine("Monitor #" + id + " '" + name + "' repetitions changed to " + newRepetitions.ToString() + ".");
        }
        
        /// <summary>
        /// Calls the specified eventFunction and does book-keeping and possibly outputs info to console.
        /// </summary>
        /// <param name="eventFunctionParam">Object parameter to pass to eventFunction.</param>
        protected void timerCallback(Object eventFunctionParam)
        {
            if (!Double.IsPositiveInfinity(repetitions))
            {
                repetitions--;
            }
            if (outputToConsole)
            {
                Console.WriteLine("Monitor #" + id + " (" + repetitions.ToString() + " repetitions remaining):");
            }
            eventFunction.Invoke(eventFunctionParam);
            if (repetitions <= 0)
            {
                stop(outputToConsole);
            }
        }
    }
}
