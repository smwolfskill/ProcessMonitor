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
        public readonly long dueTime;
        public readonly long period;
        public readonly int id; //unique ID for this MonitorEvent
        public readonly bool outputToConsole;
        public readonly DateTime created; //date and time this MonitorEvent was created

        protected Timer timer;
        protected TimerCallback eventFunction;
        protected object eventFunctionParam;
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

        public static void displayMonitorEvents(LinkedList<MonitorEvent> monitorEventList)
        {
            int activeCount = 0;
            Console.WriteLine("Monitors:");
            if (monitorEventCount > 0)
            {
                Console.WriteLine("# | running | name descriptor | interval (ms) | repetitions | created");
                Console.WriteLine("---------------------------------------------------------------------");
                foreach (MonitorEvent m in monitorEventList)
                {
                    bool isRunning = m.running();
                    Console.WriteLine(m.id.ToString() + " | " + isRunning.ToString() + " | " + m.name + " | "
                                    + m.period.ToString() + " | " + m.repetitions.ToString() + " | " + m.created.ToString());
                    if (isRunning)
                    {
                        activeCount++;
                    }
                }
                Console.WriteLine("---------------------------------------------------------------------");
            }
            int inactiveCount = monitorEventCount - activeCount;
            //Console.WriteLine(monitorEventCount.ToString() + " monitors :");
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
            this.id = monitorEventCount++;
            this.outputToConsole = outputToConsole;
            if (startImmediately)
            {
                start();
            }
        }

        /// <summary>
        /// Starts the MonitorEvent, or throws InvalidOperationException if already started.
        /// </summary>
        public void start()
        {
            if (timer != null)
            {
                throw new System.InvalidOperationException("Attempted to start already active MonitorEvent #" + id.ToString() + " '" + name + "'!");
            }
            else
            {
                timer = new Timer(timerCallback, eventFunctionParam, dueTime, period);
            }
        }

        /// <summary>
        /// Stops the MonitorEvent.
        /// </summary>
        /// <param name="outputToConsole">If true, outputs message to console saying which MonitorEvent was stopped.</param>
        public void stop(bool outputToConsole = true)
        {
            timer.Dispose();
            timer = null;
            if (outputToConsole) Console.WriteLine("Monitor #" + id + " '" + name + "' stopped.");
            //TODO: prompt to save log?
        }

        /// <summary>
        /// Determine if a MonitorEvent is running.
        /// </summary>
        /// <returns>True if this MonitorEvent is running, false otherwise.</returns>
        public bool running()
        {
            return (timer != null);
        }

        public void changeInterval(long newDueTime, long newPeriod)
        {
            timer.Change(newDueTime, newPeriod);
        }
        
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
