using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 1591 // undocumented XML code warning

#if UNITY_EDITOR && !AUDIO_TOOLKIT_DEMO

public static class AudioLog
{
    static public LinkedList<LogData> logData;

    static public Action onLogUpdated;

    public abstract class LogData
    {
        public float time;
    }

    public class LogData_PlayClip : LogData
    {
        public string audioID;
        public string category;
        public float volume;
        public float startTime;
        public float delay;
        public Vector3 position;
        public string parentObject;
        public string clipName;
        public float scheduledDspTime;
        public float pitch;
    }

    public class LogData_Stop : LogData
    {
        public string audioID;
        public string category;
        public Vector3 position;
        public string parentObject;
        public string clipName;
    }

    public class LogData_Destroy : LogData
    {
        public string audioID;
        public string category;
        public Vector3 position;
        public string parentObject;
        public string clipName;
    }

    public class LogData_SkippedPlay : LogData
    {
        public string reasonForSkip;

        public string audioID;
        public string category;
        public float volume;
        public float startTime;
        public float delay;
        public Vector3 position;
        public string parentObject;
        public float scheduledDspTime;
    }

    static AudioLog()
    {
        logData = new LinkedList<LogData>();
        _OnLogUpdated();
    }

    public static void Clear()
    {
        logData.Clear();
        _OnLogUpdated();
    }

    public static void Log( LogData playClipData )
    {
        playClipData.time = Time.time;

        if ( logData.Count >= 1024 )
        {
            logData.RemoveLast();
        }

        logData.AddFirst( playClipData );

        _OnLogUpdated();

    }

    private static void _OnLogUpdated()
    {
        if ( onLogUpdated != null )
        {
            onLogUpdated.Invoke();
        }
    }
}

#endif