using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AudioLogView : EditorWindow
{
    [MenuItem( "Window/Audio Toolkit/Log" )]
    static void ShowWindow()
    {
        EditorWindow.GetWindow( typeof( AudioLogView ) );
    }

    static Vector2 _scrollPos;

#if AUDIO_TOOLKIT_DEMO
    void OnGUI()
    {
         EditorGUILayout.LabelField( "Audio Log is not available in the FREE version of Audio Toolkit. Please buy the full version." );
    }
#else
    bool showStopEvents = true;

    void OnGUI()
    {
        // header

        float defaultColumnWidth = 120;
        float timeColumnWidth = 60;
        float typeColumnWidth = 50;
        float nameColumnWidth = 200;
        float parentColumnWidth = 200;

        GUIStyle headerStyle = new GUIStyle( EditorStyles.boldLabel );


        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField( "time", GUILayout.Width( timeColumnWidth ) );
        EditorGUILayout.LabelField( "type", GUILayout.Width( typeColumnWidth ) );
        EditorGUILayout.LabelField( "audioID", headerStyle, GUILayout.Width( defaultColumnWidth ) );
        EditorGUILayout.LabelField( "clipName", headerStyle, GUILayout.Width( nameColumnWidth ) );
        EditorGUILayout.LabelField( "category", headerStyle, GUILayout.Width( nameColumnWidth ) );
        EditorGUILayout.LabelField( "volume", GUILayout.Width( timeColumnWidth ) );
		EditorGUILayout.LabelField( "pitch", GUILayout.Width( timeColumnWidth ) );
        EditorGUILayout.LabelField( "startTime", GUILayout.Width( timeColumnWidth ) );
        EditorGUILayout.LabelField( "scheduledDSP", GUILayout.Width( timeColumnWidth ) );
        EditorGUILayout.LabelField( "delay", GUILayout.Width( timeColumnWidth ) );
        EditorGUILayout.LabelField( "parent", headerStyle, GUILayout.Width( parentColumnWidth ) );
        EditorGUILayout.LabelField( "worldPos", headerStyle, GUILayout.Width( defaultColumnWidth ) );

        EditorGUILayout.EndHorizontal();

        // data

        AudioLog.LogData_PlayClip loggedClip;

        _scrollPos = EditorGUILayout.BeginScrollView( _scrollPos );

        foreach ( var log in AudioLog.logData )
        {
            EditorGUILayout.BeginHorizontal();
            
            loggedClip = log as AudioLog.LogData_PlayClip;
            if( loggedClip != null )
            {
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", loggedClip.time ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( "PLAY", GUILayout.Width( typeColumnWidth ) );
                EditorGUILayout.LabelField( loggedClip.audioID, GUILayout.Width( defaultColumnWidth ) );
                EditorGUILayout.LabelField( loggedClip.clipName, GUILayout.Width( nameColumnWidth ) );
                EditorGUILayout.LabelField( loggedClip.category, GUILayout.Width( nameColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", loggedClip.volume ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", loggedClip.pitch ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", loggedClip.startTime ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", loggedClip.scheduledDspTime ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", loggedClip.delay ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( loggedClip.parentObject, GUILayout.Width( parentColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.0} / {1:0.0} / {2:0.0}", loggedClip.position.x, loggedClip.position.y, loggedClip.position.z ), GUILayout.Width( defaultColumnWidth ) );
                
            }

            var skippedClip = log as AudioLog.LogData_SkippedPlay;
            if ( skippedClip != null )
            {
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", skippedClip.time ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( "SKIP", GUILayout.Width( typeColumnWidth ) );
                EditorGUILayout.LabelField( skippedClip.audioID, GUILayout.Width( defaultColumnWidth ) );
                EditorGUILayout.LabelField( skippedClip.reasonForSkip, GUILayout.Width( nameColumnWidth ) );
                EditorGUILayout.LabelField( skippedClip.category, GUILayout.Width( nameColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", skippedClip.volume ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", skippedClip.startTime ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", skippedClip.scheduledDspTime ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.00}", skippedClip.delay ), GUILayout.Width( timeColumnWidth ) );
                EditorGUILayout.LabelField( skippedClip.parentObject, GUILayout.Width( parentColumnWidth ) );
                EditorGUILayout.LabelField( string.Format( "{0:0.0} / {1:0.0} / {2:0.0}", skippedClip.position.x, skippedClip.position.y, skippedClip.position.z ), GUILayout.Width( defaultColumnWidth ) );

            }

            if ( showStopEvents )
            {
                var stopClip = log as AudioLog.LogData_Stop;
                if ( stopClip != null )
                {
                    EditorGUILayout.LabelField( string.Format( "{0:0.00}", stopClip.time ), GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( "STOP", GUILayout.Width( typeColumnWidth ) );
                    EditorGUILayout.LabelField( stopClip.audioID, GUILayout.Width( defaultColumnWidth ) );
                    EditorGUILayout.LabelField( stopClip.clipName, GUILayout.Width( nameColumnWidth ) );
                    EditorGUILayout.LabelField( stopClip.category, GUILayout.Width( nameColumnWidth ) );
                    EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( stopClip.parentObject, GUILayout.Width( parentColumnWidth ) );
                    EditorGUILayout.LabelField( string.Format( "{0:0.0} / {1:0.0} / {2:0.0}", stopClip.position.x, stopClip.position.y, stopClip.position.z ), GUILayout.Width( defaultColumnWidth ) );
                }

                var destroyClip = log as AudioLog.LogData_Destroy;
                if ( destroyClip != null )
                {
                    EditorGUILayout.LabelField( string.Format( "{0:0.00}", destroyClip.time ), GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( "DESTROY", GUILayout.Width( typeColumnWidth ) );
                    EditorGUILayout.LabelField( destroyClip.audioID, GUILayout.Width( defaultColumnWidth ) );
                    EditorGUILayout.LabelField( destroyClip.clipName, GUILayout.Width( nameColumnWidth ) );
                    EditorGUILayout.LabelField( destroyClip.category, GUILayout.Width( nameColumnWidth ) );
                    EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( "", GUILayout.Width( timeColumnWidth ) );
                    EditorGUILayout.LabelField( destroyClip.parentObject, GUILayout.Width( parentColumnWidth ) );
                    EditorGUILayout.LabelField( string.Format( "{0:0.0} / {1:0.0} / {2:0.0}", destroyClip.position.x, destroyClip.position.y, destroyClip.position.z ), GUILayout.Width( defaultColumnWidth ) );
                }
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();

        if ( GUILayout.Button( "Clear", GUILayout.Width( 120 ) ) )
        {
            AudioLog.Clear();
        }

        showStopEvents = GUILayout.Toggle( showStopEvents, "Show Stop Events" );
        EditorGUILayout.EndHorizontal();
    }

    void OnNewLogEntry()
    {
        Repaint();
    }

    void OnEnable()
    {
        AudioLog.onLogUpdated += OnNewLogEntry;
    }

    void OnDisable()
    {
        AudioLog.onLogUpdated -= OnNewLogEntry;

    }
#endif
}
