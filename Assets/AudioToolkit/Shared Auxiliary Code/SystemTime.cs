using System;

/// <summary>
/// Provides access to a high precision system time value
/// </summary>
static public class SystemTime
{
    static private double _timeAtLaunch = time;
    
    /// <summary>
    /// Return the system time in seconds since 1st of January 0001
    /// </summary>
    static public double time
    {
        get
        {
            const double ticks2seconds = 1 / (double)TimeSpan.TicksPerSecond;
            long ticks = DateTime.Now.Ticks;
            double seconds = ((double)ticks ) * ticks2seconds;
            return seconds;
        }

    }

    /// <summary>
    /// Return the system time in seconds since the program launch
    /// </summary>
    static public double timeSinceLaunch
    {
        get {
            return time - _timeAtLaunch;
        }
    }
}
