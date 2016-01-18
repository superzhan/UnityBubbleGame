#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1
#define UNITY_AUDIO_FEATURES_4_0
#else
#define UNITY_AUDIO_FEATURES_4_1
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor( typeof(AudioObject) )]
public class AudioObject_Editor : EditorEx
{
    protected AudioObject AO;

    public override void OnInspectorGUI()
    {
        DrawInspector();
    }

    string FormatVolume( float volume )
    {
        float dB = 20 * Mathf.Log10( AudioObject.TransformVolume( volume ) );
        return string.Format( "{0:0.000} ({1:0.0} dB)", volume, dB );
    }

    private void DrawInspector()
    {
        AO = (AudioObject) target;

        BeginInspectorGUI();

        //DrawDefaultInspector();
        //VerticalSpace();

        ShowString( AO.audioID, "Audio ID:" );
        ShowString( AO.category != null ? AO.category.Name : "---" , "Audio Category:" );
        ShowString( FormatVolume( AO.volume ), "Item Volume:" );
        ShowString( FormatVolume( AO.volumeTotal ), "Total Volume:" );
        ShowFloat( (float) AO.startedPlayingAtTime, "Time Started:" );
        if ( AO.primaryAudioSource )
        {
            ShowString( string.Format( "{0:0.00} half-tones", AudioObject.InverseTransformPitch( AO.primaryAudioSource.pitch ) ), "Pitch:" );
            if ( AO.primaryAudioSource.clip )
            {
                ShowString( string.Format( "{0} / {1}", AO.primaryAudioSource.time, AO.clipLength ), "Time:" );
            }

#if UNITY_AUDIO_FEATURES_4_1
            if ( AO.scheduledPlayingAtDspTime > 0 )
            {
                ShowFloat( (float) ( AO.scheduledPlayingAtDspTime - AudioSettings.dspTime ), "Scheduled Play In seconds: " );

            }
#endif

        }
        if ( AO.secondaryAudioSource )
        {
           
           ShowString( string.Format( "Secondary: T:{0} Playing:{1}", AO.secondaryAudioSource.time, AO.secondaryAudioSource.isPlaying ), "Time:" );
        }
        

        EditorGUILayout.BeginHorizontal();
        if ( !AO.IsPaused() )
        {
            if ( GUILayout.Button( "Pause" ) )
            {
                AO.Pause();
            }
        }
        else
        {
            if ( GUILayout.Button( "Unpause" ) )
            {
                AO.Unpause();
            }
        }

        if ( GUILayout.Button( "Stop" ) )
        {
            AO.Stop( 0.5f );
        }
        
        if ( GUILayout.Button( "FadeIn" ) )
        {
            AO.FadeIn( 2 );
        }
        if ( GUILayout.Button( "FadeOut" ) )
        {
            AO.FadeOut( 2 );
        }
        if ( GUILayout.Button( "Refresh" ) )
        {
        }
        EditorGUILayout.EndHorizontal();


        EndInspectorGUI();
    }

    
    private void VerticalSpace()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }
   
}
