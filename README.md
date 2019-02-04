# ProcessMonitor
ProcessMonitor is a Windows console application that monitors running processes. 
It simplifies the viewing process by sorting by name or process ID (PID), 
optionally filtering out processes on a user-specified ignore list.
It also has the ability to list or kill only processes that are on a user-specified kill list.
Due to its ability to kill processes it requires admin privileges to start.

## Monitor Commands
Monitor commands showcase the best features of ProcessMonitor by allowing the user to execute any console command
continuously or for a set amount of repetitions once every specified interval of time. 
The main motivation for this was to create the functionality to continuously kill 
undesired programs on the kill list without having to manually enter the same command repetitively.

For example, `monitor 00:01:00 inf false killall`
executes `killall` once every minute (`00:01:00`) continuously (`inf`) without logging output to console (`false`).

Once a monitor is created it is started automatically, unless `repetitions` is set to 0.
The user may view a list of all created monitors and modify, start/stop or remove any monitor.
