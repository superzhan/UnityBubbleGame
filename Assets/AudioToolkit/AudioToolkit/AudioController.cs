/*************************************************************
 *       Unity Audio Toolkit (c) by ClockStone 2013          *
 * 
 * Provides useful features for playing audio files in Unity:
 * 
 *  - ease of use: play audio files with a simple static function call, creation 
 *    of required AudioSource objects is handled automatically 
 *  - conveniently define audio assets in categories
 *  - play audios from within the inspector
 *  - set properties such as the volume for the entire category
 *  - change the volume of all playing audio objects within a category at any time
 *  - define alternative audio clips that get played with a specified 
 *    probability or order
 *  - advanced audio pick modes such as "RandomNotSameTwice", "TwoSimultaneously", etc.
 *  - uses audio object pools for optimized performance particularly on mobile devices
 *  - set audio playing parameters conveniently, such as: 
 *      + random pitch & volume
 *      + minimum time difference between play calls
 *      + delay
 *      + looping
 *  - fade out / in 
 *  - special functions for music including cross-fading 
 *  - music track playlist management with shuffle, loop, etc.
 *  - delegate event call if audio was completely played
 *  - audio event log
 *  - audio overview window
 * 
 * 
 * Usage:
 *  - create a unique GameObject named "AudioController" with the 
 *    AudioController script component added
 *  - Create an AudioObject prefab containing the following components: Unity's AudioSource, the AudioObject script, 
 *    and the PoolableObject script (if pooling is wanted). 
 *    Then set your custom AudioSource parameters in this prefab. Next, specify your custom prefab in the AudioController as 
 *    the "audio object".
 *  - create your audio categories in the AudioController using the Inspector, e.g. "Music", "SFX", etc.
 *  - for each audio to be played by a script create an 'audio item' with a unique name. 
 *  - specify any number of audio sub-items (= the AudioClip plus parameters) within an audio item. 
 *  - to play an audio item call the static function 
 *    AudioController.Play( "MyUniqueAudioItemName" )
 *  - Use AudioController.PlayMusic( "MusicAudioItemName" ) to play music. This function assures that only 
 *    one music file is played at a time and handles cross fading automatically according to the configuration
 *    in the AudioController instance
 *  - Note that when you are using pooling and attach an audio object to a parent object then make sure the parent 
 *    object gets deleted using ObjectPoolController.Destroy()
 *   
 ************************************************************/

#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5
#define UNITY_3_x
#endif

#if UNITY_FLASH && !UNITY_3_x
#error Due to Unity not supporting Flash anymore we can not support Audio Toolkit export to Flash for Unity v4 or newer (only 3.x)
#endif

#if UNITY_3_x || UNITY_4_0 || UNITY_4_0_1 || UNITY_FLASH
#define UNITY_AUDIO_FEATURES_4_0
#else
#define UNITY_AUDIO_FEATURES_4_1
#endif

#if UNITY_5 || UNITY_6 || UNITY_7
#define UNITY_5_OR_NEWER
#endif

using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System;

#pragma warning disable 1591 // undocumented XML code warning

/// <summary>
/// The audio managing class used to define and play audio items and categories.
/// </summary>
/// <remarks>
/// At least one instance of an AudioController must exist in each scene using the Audio Toolkit. Usually there is 
/// exactly one controller, but you can have additional controllers if they are marked as such (in the Unity insepector, see <see cref="isAdditionalAudioController"/>)
/// There a two options when setting up an AudioController. Either you can specify all audio files that are used in your
/// entire project in one single AudioController. Then add this AudioController to your initial scene and set 
/// it persistent from within the inspector, so it will survive when a new scene is loaded. This way all audios
/// are accessible from within your entire application. If you have a lot of audio files though, this may lead 
/// to a lengthy loading time and will have a rather large memory footprint. To avoid this, you can alternatively 
/// set up a specific AudioController for each scene which only contains those audio files needed in the particular 
/// scene.
/// </remarks>
/// <example>
/// Once you have defined your audio categories and items in the Unity inspector you can play music and sound effects 
/// very easily:
/// <code>
/// AudioController.Play( "MySoundEffect1" );
/// AudioController.Play( "MySoundEffect2", new Vector3( posX, posY, posZ ) );
/// AudioController.PlayMusic( "MusicTrack1" );
/// AudioController.SetCategoryVolume( "Music", 0.5f );
/// AudioController.PauseMusic();
/// </code>
/// </example>
/// 

#if AUDIO_TOOLKIT_DEMO
[AddComponentMenu( "ClockStone/Audio/AudioController Demo" )]
public class AudioController : MonoBehaviour, ISingletonMonoBehaviour // can not make DLL with SingletonMonoBehaviour
{
    static public AudioController Instance 
    {
        get {
            return UnitySingleton<AudioController>.GetSingleton( true, false );
        }
    }
    static public bool DoesInstanceExist()
    {
        var instance = UnitySingleton<AudioController>.GetSingleton( false, false );
        return !UnityEngine.Object.Equals( instance, null );
    }
    static public void SetSingletonType( Type type )
    {
        UnitySingleton<AudioController>._myType = type;
    }
#else
[AddComponentMenu( "ClockStone/Audio/AudioController" )]
public class AudioController : SingletonMonoBehaviour<AudioController>
{
#endif

    /// <summary>
    /// A string containing the version number of the Audio Toolkit
    /// </summary>
    public const string AUDIO_TOOLKIT_VERSION = "7.0";

    /// <summary>
    /// Disables all audio playback.
    /// </summary>
    /// <remarks>
    /// Does not stop currently playing audios. Call <see cref="StopAll()"/> to stop all currently playing.
    /// </remarks>
    public bool DisableAudio
    {
        set
        {
            if ( value != _audioDisabled )
            {
                if ( value == true )
                {
                    // changed in v3.6 - allows to disable Audio without stopping all current audios

                    /*if ( AudioController.DoesInstanceExist() ) // value can be changed by inspector in none-playmode.
                    {
                        StopAll();
                    }*/
                }
                _audioDisabled = value;
            }
        }
        get
        {
            return _audioDisabled;
        }
    }

    /// <summary>
    /// You may use several AudioControllers in the same scene in parallel. All but one (the main controller) must be marked as 'additional'. 
    /// You can play audio items of any of those controllers with the normal Play() calls.
    /// </summary>
    /// <remarks>
    /// This can be used for games with a large amount of audio where you don't want all audio to be in memory at all time. 
    /// In this case use a persistent main AudioController for audios shared between all scenes of your game, and additional AudioControllers 
    /// for each scene containing specific audio for this level.
    /// </remarks>
    public bool isAdditionalAudioController
    {
        get
        {
            return _isAdditionalAudioController;
        }
        set // to be changed only from within the inspector
        {
            _isAdditionalAudioController = value;
        }
    }

    /// <summary>
    /// The global volume applied to all categories.
    /// You change the volume by script and the change will be apply to all 
    /// playing audios immediately.
    /// </summary>
    public float Volume
    {
        get { return _volume; }
        set { if ( value != _volume ) { _volume = value; _ApplyVolumeChange(); } }
    }

    /// <summary>
    /// You must specify your AudioObject prefab here using the Unity inspector.
    /// <list type="bullet">
    ///     <listheader>
    ///          <description>The prefab must have the following components:</description>
    ///     </listheader>
    ///     <item>
    ///       <term>AudioObject</term>
    ///       <term>AudioSource (Unity built-in)</term>
    ///       <term>PoolableObject</term> <description>only required if pooling is uses</description>
    ///     </item>
    /// </list>
    ///  
    /// </summary>
    public GameObject AudioObjectPrefab;

    /// <summary>
    /// If enabled, the audio controller will survive scene changes
    /// </summary>
    /// <remarks>
    /// For projects with a large number of audio files you may consider having 
    /// separate AudioController version for each scene and only specify those audio items 
    /// that are really required in this scene. This can reduce memory consumption and
    /// speed up loading time for the initial scene.
    /// </remarks>
    public bool Persistent = false;

    /// <summary>
    /// If enabled all audio resources (AudioClips) specified in this AudioController are unloaded 
    /// from memory when the AudioController gets destroyed (e.g. when loading a new scene and <see cref="Persistent"/> 
    /// is not enabled)
    /// </summary>
    /// <remarks>
    /// Uses Unity's <c>Resources.UnloadAsset(...) </c>method. Can be used to save memory if many audio ressources are in use.
    /// It is recommended to use additional AudioControllers for audios that are used only within a specific scene, and a primary
    /// persistent AudioController for audio used throughout the entire application.
    /// </remarks>
    public bool UnloadAudioClipsOnDestroy = false;

    /// <summary>
    /// Enables / Disables AudioObject pooling
    /// </summary>
    /// <remarks>
    /// Warning: Use <see cref="PoolableReference{T}"/> to store an AudioObject reference if you have pooling enabled.
    /// </remarks>
    public bool UsePooledAudioObjects = true;

    /// <summary>
    /// If disabled, audios are not played if they have a resulting volume of zero.
    /// </summary>
    public bool PlayWithZeroVolume = false;

    /// <summary>
    /// If enabled fading is adjusted in a way so that cross-fades should result in the same power during the time of fadeing
    /// </summary>
    /// <remarks>
    /// Unfortunately not 100% correct as Unity uses unknown internal formulas for computing the volume.
    /// </remarks>
    public bool EqualPowerCrossfade = false;

    /// <summary>
    /// Gets or sets the musicEnabled.
    /// </summary>
    /// <value>
    ///   <c>true</c> enables music; <c>false</c> disables music
    /// </value>
    public bool musicEnabled
    {
        get { return _musicEnabled; }
        set
        {
            if ( _musicEnabled == value ) return;
            _musicEnabled = value;

            if ( _currentMusic )
            {
                if ( value )
                {
                    if ( _currentMusic.IsPaused() )
                    {
                        _currentMusic.Play();
                    }
                }
                else
                {
                    _currentMusic.Pause();

                }
            }

        }
    }

    /// <summary>
    /// Gets or sets the soundMuted.
    /// </summary>
    /// <value>
    ///   <c>true</c> enables sound mute; <c>false</c> disables sound mute
    /// </value>
    /// <remarks>
    /// 'Sound' means all audio except music
    /// </remarks>
    public bool soundMuted
    {
        get { return _soundMuted; }
        set
        {
            _soundMuted = value;
            _ApplyVolumeChange();
        }
    }

    /// <summary>
    /// If set to a value > 0 (in seconds) music will automatically be cross-faded with this fading time.
    /// </summary>
    /// <remarks>
    /// if <see cref="specifyCrossFadeInAndOutSeperately"/> is enabled, <see cref="musicCrossFadeTime_In"/> 
    /// and <see cref="musicCrossFadeTime_Out"/> are used instead.
    /// </remarks>
    public float musicCrossFadeTime = 0;

    /// <summary>
    /// Specifies a specific fade-in time for music cross fading. Only meaningful if <see cref="specifyCrossFadeInAndOutSeperately"/> is enabled.
    /// </summary>
    public float musicCrossFadeTime_In
    {
        get
        {
            if ( specifyCrossFadeInAndOutSeperately )
            {
                return _musicCrossFadeTime_In;
            }
            else
                return musicCrossFadeTime;
        }

        set
        {
            _musicCrossFadeTime_In = value;
        }
    }

    /// <summary>
    /// Specifies a specific fade-out time for music cross fading. Only meaningful if <see cref="specifyCrossFadeInAndOutSeperately"/> is enabled.
    /// </summary>
    public float musicCrossFadeTime_Out
    {
        get
        {
            if ( specifyCrossFadeInAndOutSeperately )
            {
                return _musicCrossFadeTime_Out;
            }
            else
                return musicCrossFadeTime;
        }

        set
        {
            _musicCrossFadeTime_Out = value;
        }
    }

    /// <summary>
    /// If enabled specific music cross-fading in and out times can be specified with <see cref="musicCrossFadeTime_In"/> 
    /// and <see cref="musicCrossFadeTime_Out"/> 
    /// </summary>
    public bool specifyCrossFadeInAndOutSeperately = false;

    [SerializeField]
    private float _musicCrossFadeTime_In = 0;

    [SerializeField]
    private float _musicCrossFadeTime_Out = 0;


    /// <summary>
    /// Specify your audio categories here using the Unity inspector.
    /// </summary>
    public AudioCategory[ ] AudioCategories;

    /// <summary>
    /// allows to specify a list of audioID that will be played as music one after the other
    /// </summary>
    public string[ ] musicPlaylist;

    /// <summary>
    /// specifies if the music playlist will get looped
    /// </summary>
    public bool loopPlaylist = false;

    /// <summary>
    /// enables / disables shuffling for the music playlist
    /// </summary>
    public bool shufflePlaylist = false;

    /// <summary>
    /// if enabled, the tracks on the playlist will get cross-faded as specified by <see cref="musicCrossFadeTime"/>
    /// </summary>
    public bool crossfadePlaylist = false;

    /// <summary>
    /// Mute time in between two tracks on the playlist.
    /// </summary>
    public float delayBetweenPlaylistTracks = 1;

    /// <summary>
    /// Returns the high precision audio system time size the application launch.
    /// </summary>
    /// <remarks>
    /// The audio system time does not increase if the application is paused.
    /// For performance reasons the time only gets updated with the frame rate. However,
    /// the time value does not lose precision even if the application is running for a
    /// long time (unlike Unity's 32bit float Time.systemTime
    /// </remarks>
    static public double systemTime
    {
        get
        {
            return _systemTime;
        }
    }

    /// <summary>
    /// Returns the high precision audio system delta time since the last frame update.
    /// </summary>
    static public double systemDeltaTime
    {
        get
        {
            return _systemDeltaTime;
        }
    }


    // **************************************************************************************************/
    //          public functions
    // **************************************************************************************************/

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> as music.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="volume">The volume betweeen 0 and 1 [default=1].</param>
    /// <param name="delay">The delay [default=0].</param>
    /// <param name="startTime">The start time [default=0]</param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist. 
    /// Warning: Use <see cref="PoolableReference{T}"/> to store an AudioObject reference if you have pooling enabled.
    /// </returns>
    /// <remarks>
    /// PlayMusic makes sure that only one music track is played at a time. If music cross fading is enabled in the AudioController
    /// fading is performed automatically.<br/>
    /// If "3D sound" is enabled in the audio import settings of the audio clip the object will be placed right 
    /// in front of the current audio listener which is usually on the main camera. Note that the audio object will not
    /// be parented - so you will hear when the audio listener moves.
    /// </remarks>
    static public AudioObject PlayMusic( string audioID, float volume, float delay, float startTime = 0 )
    {
        _isPlaylistPlaying = false;
        return Instance._PlayMusic( audioID, volume, delay, startTime );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> as music.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <remarks>
    /// Variant of <see cref="PlayMusic( string, float, float, float )"/> with default parameters.
    /// </remarks>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist. 
    /// Warning: Use <see cref="PoolableReference{T}"/> to store an AudioObject reference if you have pooling enabled.
    /// </returns>
    static public AudioObject PlayMusic( string audioID )
    {
        return AudioController.PlayMusic( audioID, 1, 0, 0 );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> as music.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="volume">The volume between 0 and 1 [default=1].</param>
    /// <remarks>
    /// Variant of <see cref="PlayMusic( string, float, float, float )"/> with default parameters.
    /// </remarks>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist. 
    /// Warning: Use <see cref="PoolableReference{T}"/> to store an AudioObject reference if you have pooling enabled.
    /// </returns>
    static public AudioObject PlayMusic( string audioID, float volume )
    {
        return AudioController.PlayMusic( audioID, volume, 0, 0 );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> as music at the specified position.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="worldPosition">The position in world coordinates.</param>
    /// <param name="parentObj">The parent transform or <c>null</c>.</param>
    /// <param name="volume">The volume between 0 and 1 [default=1].</param>
    /// <param name="delay">The delay [default=0].</param>
    /// <param name="startTime">The start time [default=0]</param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist. 
    /// Warning: Use <see cref="PoolableReference{T}"/> to store an AudioObject reference if you have pooling enabled.
    /// </returns>
    /// <remarks>
    /// PlayMusic makes sure that only one music track is played at a time. If music cross fading is enabled in the AudioController
    /// fading is performed automatically.
    /// </remarks>
    static public AudioObject PlayMusic( string audioID, Vector3 worldPosition, Transform parentObj = null, float volume = 1, float delay = 0, float startTime = 0 )
    {
        _isPlaylistPlaying = false;
        return Instance._PlayMusic( audioID, worldPosition, parentObj, volume, delay, startTime );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> as music at the specified position.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="parentObj">The parent transform or <c>null</c>.</param>
    /// <param name="volume">The volume between 0 and 1 [default=1].</param>
    /// <param name="delay">The delay [default=0].</param>
    /// <param name="startTime">The start time [default=0]</param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist. 
    /// Warning: Use <see cref="PoolableReference{T}"/> to store an AudioObject reference if you have pooling enabled.
    /// </returns>
    /// <remarks>
    /// PlayMusic makes sure that only one music track is played at a time. If music cross fading is enabled in the AudioController
    /// fading is performed automatically.
    /// </remarks>
    static public AudioObject PlayMusic( string audioID, Transform parentObj, float volume = 1, float delay = 0, float startTime = 0 )
    {
        _isPlaylistPlaying = false;
        return Instance._PlayMusic( audioID, parentObj.position, parentObj, volume, delay, startTime );
    }

    /// <summary>
    /// Stops the currently playing music.
    /// </summary>
    /// <returns>
    /// <c>true</c> if any music was stopped, otherwise <c>false</c>
    /// </returns>
    static public bool StopMusic()
    {
        return Instance._StopMusic( 0 );
    }

    /// <summary>
    /// Stops the currently playing music with fade-out.
    /// </summary>
    /// <param name="fadeOut">The fade-out time in seconds.</param>
    /// <returns>
    /// <c>true</c> if any music was stopped, otherwise <c>false</c>
    /// </returns>
    static public bool StopMusic( float fadeOut )
    {
        return Instance._StopMusic( fadeOut );
    }

    /// <summary>
    /// Pauses the currently playing music.
    /// </summary>
    /// <param name="fadeOut">The fade-out time in seconds.</param>
    /// <returns>
    /// <c>true</c> if any music was paused, otherwise <c>false</c>
    /// </returns>
    static public bool PauseMusic( float fadeOut = 0 )
    {
        return Instance._PauseMusic( fadeOut );
    }

    /// <summary>
    /// Uses to test if music is paused
    /// </summary>
    /// <returns>
    /// <c>true</c> if music is paused, otherwise <c>false</c>
    /// </returns>
    static public bool IsMusicPaused()
    {
        if ( _currentMusic != null )
        {
            return _currentMusic.IsPaused();
        }
        return false;
    }

    /// <summary>
    /// Unpauses the current music.
    /// </summary>
    /// <param name="fadeIn">The fade-in time in seconds.</param>
    /// <returns>
    /// <c>true</c> if any music was unpaused, otherwise <c>false</c>
    /// </returns>
    static public bool UnpauseMusic( float fadeIn = 0 )  // un-pauses music
    {
        if ( !Instance._musicEnabled )
        {
            return false;
        }

        if ( _currentMusic != null && _currentMusic.IsPaused() )
        {
            _currentMusic.Unpause( fadeIn );
            return true;
        }
        return false;
    }

    /// <summary>
    /// Enqueues an audio ID to the music playlist queue.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <returns>
    /// The number of music tracks on the playlist.
    /// </returns>
    static public int EnqueueMusic( string audioID )
    {
        return Instance._EnqueueMusic( audioID );
    }

    /// <summary>
    /// Gets a copy of the current playlist audioID array
    /// </summary>
    /// <returns>
    /// The playlist array
    /// </returns>
    static public string[ ] GetMusicPlaylist()
    {
        string[ ] playlistCopy = new string[ Instance.musicPlaylist != null ? Instance.musicPlaylist.Length : 0 ];

        if ( playlistCopy.Length > 0 )
        {
            Array.Copy( Instance.musicPlaylist, playlistCopy, playlistCopy.Length );
        }
        return playlistCopy;
    }

    /// <summary>
    /// Sets the current playlist to the specified audioID array
    /// </summary>
    /// <param name="playlist">The new playlist array</param>
    static public void SetMusicPlaylist( string[ ] playlist )
    {

        string[ ] playlistCopy = new string[ playlist != null ? playlist.Length : 0 ];

        if ( playlistCopy.Length > 0 )
        {
            Array.Copy( playlist, playlistCopy, playlistCopy.Length );
        }
        Instance.musicPlaylist = playlistCopy;
    }

    /// <summary>
    /// Start playing the music playlist.
    /// </summary>
    /// <returns>
    /// The <c>AudioObject</c> of the current music, or <c>null</c> if no music track could be played.
    /// </returns>
    static public AudioObject PlayMusicPlaylist()
    {
        return Instance._PlayMusicPlaylist();
    }

    /// <summary>
    /// Jumps to the next the music track on the playlist.
    /// </summary>
    /// <remarks>
    /// If shuffling is enabled it will jump to the next randomly chosen track.
    /// </remarks>
    /// <returns>
    /// The <c>AudioObject</c> of the current music, or <c>null</c> if no music track could be played.
    /// </returns>
    static public AudioObject PlayNextMusicOnPlaylist()
    {
        if ( IsPlaylistPlaying() )
        {
            return Instance._PlayNextMusicOnPlaylist( 0 );
        }
        else
            return null;
    }

    /// <summary>
    /// Jumps to the previous music track on the playlist.
    /// </summary>
    /// <remarks>
    /// If shuffling is enabled it will jump to the previously played track.
    /// </remarks>
    /// <returns>
    /// The <c>AudioObject</c> of the current music, or <c>null</c> if no music track could be played.
    /// </returns>
    static public AudioObject PlayPreviousMusicOnPlaylist()
    {
        if ( IsPlaylistPlaying() )
        {
            return Instance._PlayPreviousMusicOnPlaylist( 0 );
        }
        else
            return null;
    }

    /// <summary>
    /// Determines whether the playlist is playing.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if the current music track is from the playlist; otherwise, <c>false</c>.
    /// </returns>
    static public bool IsPlaylistPlaying()
    {
        if ( _isPlaylistPlaying )
        {
            if ( !_currentMusic )
            {
                _isPlaylistPlaying = false;
                return false;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears the music playlist.
    /// </summary>
    static public void ClearPlaylist()
    {
        Instance.musicPlaylist = null;
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c>.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist.
    /// Warning: Use <see cref="PoolableReference{T}"/> to store an AudioObject reference if you have pooling enabled.
    /// </returns>
    /// <remarks>
    /// If "3D sound" is enabled in the audio import settings of the audio clip the object will be placed right 
    /// in front of the current audio listener which is usually on the main camera. Note that the audio object will not
    /// be parented - so you will hear when the audio listener moves.
    /// </remarks>
    static public AudioObject Play( string audioID )
    {
        AudioListener al = GetCurrentAudioListener();

        if ( al == null )
        {
            Debug.LogWarning( "No AudioListener found in the scene" );
            return null;
        }

        return Play( audioID, al.transform.position + al.transform.forward, null, 1, 0, 0 );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c>.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="volume">The volume between 0 and 1 [default=1].</param>
    /// <param name="delay">The delay [default=0].</param>
    /// <param name="startTime">The start time [default=0]</param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist.
    /// Warning: Use <see cref="PoolableReference{T}"/> to store an AudioObject reference if you have pooling enabled.
    /// </returns>
    /// <remarks>
    /// If "3D sound" is enabled in the audio import settings of the audio clip the object will be placed right 
    /// in front of the current audio listener which is usually on the main camera. Note that the audio object will not
    /// be parented - so you will hear when the audio listener moves.
    /// </remarks>
    static public AudioObject Play( string audioID, float volume, float delay = 0, float startTime = 0 )
    {
        AudioListener al = GetCurrentAudioListener();

        if ( al == null )
        {
            Debug.LogWarning( "No AudioListener found in the scene" );
            return null;
        }

        return Play( audioID, al.transform.position + al.transform.forward, null, volume, delay, startTime );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> parented to a specified transform.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="parentObj">The parent transform.</param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist.
    /// </returns>
    /// <remarks>
    /// If the audio clip is marked as 3D the audio clip will be played at the position of the parent transform. 
    /// As the audio object will get attached to the transform, it is important to destroy the parent object using the
    /// <see cref="ObjectPoolController.Destroy"/> function, even if the parent object is not poolable itself
    /// </remarks>
    static public AudioObject Play( string audioID, Transform parentObj )
    {
        return Play( audioID, parentObj.position, parentObj, 1, 0, 0 );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> parented to a specified transform.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="parentObj">The parent transform.</param>
    /// <param name="volume">The volume between 0 and 1 [default=1].</param>
    /// <param name="delay">The delay [default=0].</param>
    /// <param name="startTime">The start time [default=0]</param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist.
    /// </returns>
    /// <remarks>
    /// If the audio clip is marked as 3D the audio clip will be played at the position of the parent transform. 
    /// As the audio object will get attached to the transform, it is important to destroy the parent object using the
    /// <see cref="ObjectPoolController.Destroy"/> function, even if the parent object is not poolable itself
    /// </remarks>
    static public AudioObject Play( string audioID, Transform parentObj, float volume, float delay = 0, float startTime = 0 )
    {
        return Play( audioID, parentObj.position, parentObj, volume, delay, startTime );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> parented to a specified transform with a world offset.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="worldPosition">The position in world coordinates.</param>
    /// <param name="parentObj">The parent transform [default=null]. </param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist.
    /// </returns>
    /// <remarks>
    /// If the audio clip is marked as 3D the audio clip will be played at the position of the parent transform. 
    /// As the audio object will get attached to the transform, it is important to destroy the parent object using the
    /// <see cref="ObjectPoolController.Destroy"/> function, even if the parent object is not poolable itself
    /// </remarks>
    static public AudioObject Play( string audioID, Vector3 worldPosition, Transform parentObj = null )
    {
        //Debug.Log( "Play: '" + audioID + "'" );
        return Instance._PlayAsSound( audioID, 1, worldPosition, parentObj, 0, 0, false );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> parented to a specified transform with a world offset.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="worldPosition">The position in world coordinates.</param>
    /// <param name="parentObj">The parent transform.</param>
    /// <param name="volume">The volume between 0 and 1 [default=1].</param>
    /// <param name="delay">The delay [default=0].</param>
    /// <param name="startTime">The start time [default=0]</param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist.
    /// </returns>
    /// <remarks>
    /// If the audio clip is marked as 3D the audio clip will be played at the position of the parent transform. 
    /// As the audio object will get attached to the transform, it is important to destroy the parent object using the
    /// <see cref="ObjectPoolController.Destroy"/> function, even if the parent object is not poolable itself
    /// </remarks>
    static public AudioObject Play( string audioID, Vector3 worldPosition, Transform parentObj, float volume, float delay = 0, float startTime = 0 )
    {
        //Debug.Log( "Play: '" + audioID + "'" );
        return Instance._PlayAsSound( audioID, volume, worldPosition, parentObj, delay, startTime, false );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> parented to a specified transform with a world offset scheduled at a specified high precision DSP time
    /// (see the Unity AudioSettings.dspTime documentation)
    /// </summary>
    /// <param name="dspTime">The high precision DSP time at which to start playing.</param>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="worldPosition">The position in world coordinates.</param>
    /// <param name="parentObj">The parent transform.</param>
    /// <param name="volume">The volume between 0 and 1 [default=1].</param>
    /// <param name="startTime">The start time seconds [default=0]</param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist.
    /// </returns>
    static public AudioObject PlayScheduled( string audioID, double dspTime, Vector3 worldPosition, Transform parentObj = null, float volume = 1.0f, float startTime = 0 )
    {
        return AudioController.Instance._PlayAsSound( audioID, volume, worldPosition, parentObj, 0, startTime, false, dspTime );
    }

    /// <summary>
    /// Plays an audio item with the name <c>audioID</c> right after the given <see cref="AudioObject"/> stops playing. 
    /// (see the Unity AudioSettings.dspTime documentation)
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="playingAudio">Playback will start after this <see cref="AudioObject"/> finished playing </param>
    /// <param name="deltaDspTime">A time delta (high precision DSP time) at which to start playing. Negative values will cause audios to overlap.</param>
    /// <param name="volume">The volume between 0 and 1 [default=1].</param>
    /// <param name="startTime">The start time seconds [default=0]</param>
    /// <returns>
    /// Returns the reference of the AudioObject that is used to play the audio item, or <c>null</c> if the audioID does not exist.
    /// </returns>
    /// <remarks>
    /// Uses the PlayScheduled function only available in Unity v4.1 or newer that allows to stitch two audios together at DSP level precision without a gap. 
    /// Can not be used to chain more then one audio.
    /// </remarks>
    static public AudioObject PlayAfter( string audioID, AudioObject playingAudio, double deltaDspTime = 0, float volume = 1.0f, float startTime = 0 )
    {
#if UNITY_AUDIO_FEATURES_4_1
        double dspTime;

        dspTime = AudioSettings.dspTime;

        if ( playingAudio.IsPlaying() )
        {
            dspTime += playingAudio.timeUntilEnd;
        }

        dspTime += deltaDspTime;

        return AudioController.PlayScheduled( audioID, dspTime, playingAudio.transform.position, playingAudio.transform.parent, volume, startTime );
#else
        Debug.LogError( "PlayAfter is only supported in Unity v4.1 or newer" );
        return null;
#endif
    }

    /// <summary>
    /// Stops all playing audio items with name <c>audioID</c> with a fade-out.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="fadeOutLength">The fade out time. If a negative value is specified, the subitem's <see cref="AudioSubItem.FadeOut"/> value is taken.</param>
    /// <returns>Return <c>true</c> if any audio was stopped.</returns>
    static public bool Stop( string audioID, float fadeOutLength )
    {
        AudioItem sndItem = Instance._GetAudioItem( audioID );

        if ( sndItem == null )
        {
            Debug.LogWarning( "Audio item with name '" + audioID + "' does not exist" );
            return false;
        }

        //if ( sndItem.PlayInstead.Length > 0 )
        //{
        //    return Stop( sndItem.PlayInstead, fadeOutLength );
        //}

        List<AudioObject> audioObjs = GetPlayingAudioObjects( audioID );

        for ( int index = 0; index < audioObjs.Count; index++ )
        {
            AudioObject audioObj = audioObjs[ index ];
            if ( fadeOutLength < 0 )
            {
                audioObj.Stop();
            }
            else
            {
                audioObj.Stop( fadeOutLength );
            }
        }
        return audioObjs.Count > 0;
    }

    /// <summary>
    /// Stops all playing audio items with name <c>audioID</c>.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <returns>Return <c>true</c> if any audio was stopped.</returns>
    static public bool Stop( string audioID )
    {
        return AudioController.Stop( audioID, -1 );
    }

    /// <summary>
    /// Fades out all playing audio items (including the music).
    /// </summary>
    /// <param name="fadeOutLength">The fade out time. If a negative value is specified, the subitem's <see cref="AudioSubItem.FadeOut"/> value is taken.</param>
    static public void StopAll( float fadeOutLength )
    {
        Instance._StopMusic( fadeOutLength );

        List<AudioObject> objs = GetPlayingAudioObjects();

        for ( int index = 0; index < objs.Count; index++ )
        {
            AudioObject o = objs[ index ];
            if ( o != null )
            {
                o.Stop( fadeOutLength );
            }
        }
    }

    /// <summary>
    /// Immediately stops playing audio items (including the music).
    /// </summary>
    static public void StopAll()
    {
        AudioController.StopAll( -1 );
    }

    /// <summary>
    /// Pauses all playing audio items (including the music).
    /// </summary>
    /// <param name="fadeOutLength">The fade-out time [Default=0]</param>
    static public void PauseAll( float fadeOutLength = 0 )
    {
        Instance._PauseMusic( fadeOutLength );

        List<AudioObject> objs = GetPlayingAudioObjects();

        for ( int index = 0; index < objs.Count; index++ )
        {
            AudioObject o = objs[ index ];
            if ( o != null )
            {
                o.Pause( fadeOutLength );
            }
        }
    }

    /// <summary>
    /// Un-pauses all playing audio items (including the music).
    /// </summary>
    /// <param name="fadeInLength">The fade-in time [Default=0]</param>
    static public void UnpauseAll( float fadeInLength = 0 )
    {
        AudioController.UnpauseMusic( fadeInLength );

        List<AudioObject> objs = GetPlayingAudioObjects( true );

        var ac = Instance;

        for ( int index = 0; index < objs.Count; index++ )
        {
            AudioObject o = objs[ index ];
            if ( o != null )
            {
                if ( o.IsPaused() )
                {
                    if ( !ac.musicEnabled && _currentMusic == o )
                    {
                        continue; // do not unpause music if music is disabled
                    }
                    o.Unpause( fadeInLength );
                }
            }
        }
    }

    /// <summary>
    /// Pauses all playing audio items in the specified category (including the music).
    /// </summary>
    /// <param name="categoryName">Name of category.</param>
    /// <param name="fadeOutLength">The fade-out time [Default=0]</param>
    static public void PauseCategory( string categoryName, float fadeOutLength = 0 )
    {
        if ( _currentMusic != null && _currentMusic.category.Name == categoryName ) AudioController.PauseMusic( fadeOutLength );

        List<AudioObject> objs = GetPlayingAudioObjectsInCategory( categoryName );

        for ( int index = 0; index < objs.Count; index++ )
        {
            AudioObject o = objs[ index ];
            o.Pause( fadeOutLength );
        }
    }

    /// <summary>
    /// Un-pauses all playing audio items in the specified category (including the music).
    /// </summary>
    /// <param name="categoryName">Name of category.</param>
    /// <param name="fadeInLength">The fade-in time [Default=0]</param>
    static public void UnpauseCategory( string categoryName, float fadeInLength = 0 )
    {
        if ( _currentMusic != null && _currentMusic.category.Name == categoryName ) AudioController.UnpauseMusic( fadeInLength );

        List<AudioObject> objs = GetPlayingAudioObjectsInCategory( categoryName, true );

        for ( int index = 0; index < objs.Count; index++ )
        {
            AudioObject o = objs[ index ];
            if ( o.IsPaused() )
            {
                o.Unpause( fadeInLength );
            }
        }
    }

    /// <summary>
    /// Stops all playing audio items in the specified category (including the music).
    /// </summary>
    /// <param name="categoryName">Name of category.</param>
    /// <param name="fadeOutLength">The fade-out time [Default=0]</param>
    static public void StopCategory( string categoryName, float fadeOutLength = 0 )
    {
        if ( _currentMusic != null && _currentMusic.category.Name == categoryName ) AudioController.StopMusic( fadeOutLength );

        List<AudioObject> objs = GetPlayingAudioObjectsInCategory( categoryName );

        for ( int index = 0; index < objs.Count; index++ )
        {
            AudioObject o = objs[ index ];
            o.Stop( fadeOutLength );
        }
    }

    /// <summary>
    /// Determines whether the specified audio ID is playing.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <returns>
    ///   <c>true</c> if the specified audio ID is playing; otherwise, <c>false</c>.
    /// </returns>
    static public bool IsPlaying( string audioID )
    {
        return GetPlayingAudioObjects( audioID ).Count > 0;
    }

    /// <summary>
    /// Returns an array of all playing audio objects with the specified <c>audioID</c>.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="includePausedAudio">If enabled the returned array will also contain paused audios.</param>
    /// <returns>
    /// Array of all playing audio objects with the specified <c>audioID</c>.
    /// </returns>
    static public List<AudioObject> GetPlayingAudioObjects( string audioID, bool includePausedAudio = false )
    {
        List<AudioObject> objs = GetPlayingAudioObjects( includePausedAudio );
        var matchesList = new List<AudioObject>( objs.Count );

        for ( int index = 0; index < objs.Count; index++ )
        {
            AudioObject o = objs[ index ];
            if ( o != null )
            {
                if ( o.audioID == audioID )
                {
                    matchesList.Add( o );
                }
            }
        }
        return matchesList;
    }

    /// <summary>
    /// Returns an array of all playing audio objects in the category with name <c>categoryName</c>.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <param name="includePausedAudio">If enabled the returned array will also contain paused audios.</param>
    /// <returns>
    /// Array of all playing audio objects belonging to the specified category or one of its child categories.
    /// </returns>
    static public List<AudioObject> GetPlayingAudioObjectsInCategory( string categoryName, bool includePausedAudio = false )
    {
        List<AudioObject> objs = GetPlayingAudioObjects( includePausedAudio );
        var matchesList = new List<AudioObject>( objs.Count );

        for ( int index = 0; index < objs.Count; index++ )
        {
            AudioObject o = objs[ index ];
            if ( o != null )
            {
                if ( o.DoesBelongToCategory( categoryName ) )
                {
                    matchesList.Add( o );
                }
            }
        }
        return matchesList;
    }

    /// <summary>
    /// Returns an array of all playing audio objects.
    /// </summary>
    /// <param name="includePausedAudio">If enabled the returned array will also contain paused audios.</param>
    /// <returns>
    /// Array of all playing audio objects.
    /// </returns>
    static public List<AudioObject> GetPlayingAudioObjects( bool includePausedAudio = false )
    {
        object[ ] objs = RegisteredComponentController.GetAllOfType( typeof( AudioObject ) );  // Flash compatible version
        var matchesList = new List<AudioObject>( objs.Length );

        for ( int i = 0; i < objs.Length; i++ )
        {
            var o = (AudioObject) objs[ i ];
            if ( o.IsPlaying() || ( includePausedAudio && o.IsPaused() ) )
            {
                matchesList.Add( o );
            }
        }

        return matchesList;
    }

    /// <summary>
    /// Returns the number of all playing audio objects with the specified <c>audioID</c>.
    /// </summary>
    /// <param name="audioID">The audio ID.</param>
    /// <param name="includePausedAudio">If enabled the returned array will also contain paused audios.</param>
    /// <returns>
    /// Number of all playing audio objects with the specified <c>audioID</c>.
    /// </returns>
    static public int GetPlayingAudioObjectsCount( string audioID, bool includePausedAudio = false )
    {
        List<AudioObject> objs = GetPlayingAudioObjects( includePausedAudio );

        int count = 0;

        for ( int index = 0; index < objs.Count; index++ )
        {
            AudioObject o = objs[ index ];
            if ( o != null )
            {
                if ( o.audioID == audioID )
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Enables the music.
    /// </summary>
    /// <param name="b">if set to <c>true</c> [b].</param>
    static public void EnableMusic( bool b )
    {
        AudioController.Instance.musicEnabled = b;
    }

    /// <summary>
    /// Mutes / Unmutes the sound.
    /// </summary>
    /// <remarks>
    /// 'Sound' means all audio except music.
    /// </remarks>
    /// <param name="b">if set to <c>true</c> [b].</param>
    static public void MuteSound( bool b )
    {
        AudioController.Instance.soundMuted = b;
    }

    /// <summary>
    /// Determines whether music is enabled.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if music is enabled; otherwise, <c>false</c>.
    /// </returns>
    static public bool IsMusicEnabled()
    {
        return AudioController.Instance.musicEnabled;
    }

    /// <summary>
    /// Determines whether sound is muted
    /// </summary>
    /// <returns>
    ///   <c>true</c> if sound is muted; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// 'Sound' means all audio except music.
    /// </remarks>
    static public bool IsSoundMuted()
    {
        return AudioController.Instance.soundMuted;
    }

    /// <summary>
    /// Gets the currently active Unity audio listener.
    /// </summary>
    /// <returns>
    /// Reference of the currently active AudioListener object.
    /// </returns>
    static public AudioListener GetCurrentAudioListener()
    {
        AudioController MyInstance = Instance;
        if ( MyInstance._currentAudioListener != null && MyInstance._currentAudioListener.gameObject == null ) // TODO: check if this is necessary and if it really works if object was destroyed
        {
            MyInstance._currentAudioListener = null;
        }

        if ( MyInstance._currentAudioListener == null )
        {
            MyInstance._currentAudioListener = (AudioListener) FindObjectOfType( typeof( AudioListener ) );
        }

        return MyInstance._currentAudioListener;
    }



    /// <summary>
    /// Gets the current music.
    /// </summary>
    /// <returns>
    /// Returns a reference to the AudioObject that is currently playing the music.
    /// </returns>
    static public AudioObject GetCurrentMusic()
    {
        return AudioController._currentMusic;
    }

    /// <summary>
    /// Gets a category.
    /// </summary>
    /// <param name="name">The category's name.</param>
    /// <returns>The category or <c>null</c> if no category with the specified name exists</returns>
    static public AudioCategory GetCategory( string name )
    {
        var primaryInstance = Instance;

        AudioCategory cat = primaryInstance._GetCategory( name );

        if ( cat != null )
        {
            return cat;
        }

        if ( primaryInstance._additionalAudioControllers != null )
        {
            for ( int index = 0; index < primaryInstance._additionalAudioControllers.Count; index++ )
            {
                var ac = primaryInstance._additionalAudioControllers[ index ];
                cat = ac._GetCategory( name );

                if ( cat != null )
                {
                    return cat;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Changes the category volume. Also effects currently playing audio items.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="volume">The volume (between 0 and 1).</param>
    static public void SetCategoryVolume( string name, float volume )
    {
        bool found = false;
        var primaryInstance = Instance;

        AudioCategory cat = primaryInstance._GetCategory( name );

        if ( cat != null )
        {
            cat.Volume = volume;
            found = true;
        }

        if ( primaryInstance._additionalAudioControllers != null )
        {
            for ( int index = 0; index < primaryInstance._additionalAudioControllers.Count; index++ )
            {
                var ac = primaryInstance._additionalAudioControllers[ index ];
                cat = ac._GetCategory( name );

                if ( cat != null )
                {
                    cat.Volume = volume;
                    found = true;
                }
            }
        }

        if ( !found )
        {
            Debug.LogWarning( "No audio category with name " + name );
        }
    }

    /// <summary>
    /// Gets the category volume.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <returns>The volume of the specified category</returns>
    static public float GetCategoryVolume( string name )
    {
        AudioCategory category = GetCategory( name );
        if ( category != null )
        {
            return category.Volume;
        }
        else
        {
            Debug.LogWarning( "No audio category with name " + name );
            return 0;
        }
    }

    /// <summary>
    /// Changes the global volume. Effects all currently playing audio items.
    /// </summary>
    /// <param name="volume">The volume (between 0 and 1).</param>
    /// <remarks>
    /// Volume change is also applied to all additional AudioControllers.
    /// </remarks>
    static public void SetGlobalVolume( float volume )
    {
        var primaryInstance = Instance;
        primaryInstance.Volume = volume;

        if ( primaryInstance._additionalAudioControllers != null )
        {
            for ( int index = 0; index < primaryInstance._additionalAudioControllers.Count; index++ )
            {
                var ac = primaryInstance._additionalAudioControllers[ index ];
                ac.Volume = volume;
            }
        }
    }

    /// <summary>
    /// Gets the global volume.
    /// </summary>
    /// <returns>
    /// The global volume (between 0 and 1).
    /// </returns>
    static public float GetGlobalVolume()
    {
        return Instance.Volume;
    }

    /// <summary>
    /// Creates a new audio category
    /// </summary>
    /// <param name="categoryName">Name of the category.</param>
    /// <returns>
    /// Reference to the new category.
    /// </returns>
    static public AudioCategory NewCategory( string categoryName )
    {
        // can not use ArrayHelper at this point because of buggy Flash compiler :(

        int oldCategoryCount = Instance.AudioCategories != null ? Instance.AudioCategories.Length : 0;
        var oldArray = Instance.AudioCategories;
        Instance.AudioCategories = new AudioCategory[ oldCategoryCount + 1 ];

        if ( oldCategoryCount > 0 )
        {
            oldArray.CopyTo( Instance.AudioCategories, 0 );
        }

        var newCategory = new AudioCategory( Instance );
        newCategory.Name = categoryName;

        Instance.AudioCategories[ oldCategoryCount ] = newCategory;
        Instance._InvalidateCategories();
        return newCategory;
    }


    /// <summary>
    /// Removes an audio category.
    /// </summary>
    /// <param name="categoryName">Name of the category to remove.</param>
    static public void RemoveCategory( string categoryName )
    {
        int i, index = -1;
        int oldCategoryCount;

        if ( Instance.AudioCategories != null )
        {
            oldCategoryCount = Instance.AudioCategories.Length;
        }
        else
            oldCategoryCount = 0;

        for ( i = 0; i < oldCategoryCount; i++ )
        {
            if ( Instance.AudioCategories[ i ].Name == categoryName )
            {
                index = i;
                break;
            }
        }

        if ( index == -1 )
        {
            Debug.LogError( "AudioCategory does not exist: " + categoryName );
            return;
        }

        //ArrayHelper.DeleteArrayElement( ref Instance.AudioCategories, index ); // can not use ArrayHelper because of buggy Flash compiler :(
        {
            var newArray = new AudioCategory[ Instance.AudioCategories.Length - 1 ];

            for ( i = 0; i < index; i++ )
            {
                newArray[ i ] = Instance.AudioCategories[ i ];
            }
            for ( i = index + 1; i < Instance.AudioCategories.Length; i++ )
            {
                newArray[ i - 1 ] = Instance.AudioCategories[ i ];
            }
            Instance.AudioCategories = newArray;
        }

        Instance._InvalidateCategories();
    }

    /// <summary>
    /// Adds a custom audio item to a category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="audioItem">The audio item.</param>
    /// <example>
    /// <code>
    /// var audioItem = new AudioItem();
    /// audioItem.SubItemPickMode = AudioPickSubItemMode.Sequence;
    /// 
    /// audioItem.subItems = new AudioSubItem[ 2 ];
    /// 
    /// audioItem.subItems[ 0 ] = new AudioSubItem();
    /// audioItem.subItems[ 0 ].Clip = audioClip0;
    /// audioItem.subItems[ 0 ].Volume = 0.7f;
    /// 
    /// audioItem.subItems[ 1 ] = new AudioSubItem();
    /// audioItem.subItems[ 1 ].Clip = audioClip1;
    /// audioItem.subItems[ 1 ].Volume = 0.8f;
    /// 
    /// AddToCategory( GetCategory( "CustomSFX" ), audioItem );
    /// </code>
    /// </example>
    /// <seealso cref="AudioController.NewCategory(string)"/>
    /// <seealso cref="AudioController.GetCategory(string)"/>
    static public void AddToCategory( AudioCategory category, AudioItem audioItem )
    {
        // can not use here because of Flash compuler bug: ArrayHelper.AddArrayElement( ref category.AudioItems, audioItem );

        int oldCount = category.AudioItems != null ? category.AudioItems.Length : 0;
        var oldArray = category.AudioItems;
        category.AudioItems = new AudioItem[ oldCount + 1 ];

        if ( oldCount > 0 )
        {
            oldArray.CopyTo( category.AudioItems, 0 );
        }

        category.AudioItems[ oldCount ] = audioItem;
        Instance._InvalidateCategories();
    }

    /// <summary>
    /// Creates an AudioItem with the name <c>audioID</c> containing a single subitem playing the specified 
    /// custom AudioClip. This AudioItem is then added to the specified category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <param name="audioClip">The custom audio clip.</param>
    /// <param name="audioID">The audioID for the AudioItem to create.</param>
    /// <returns>The <see cref="AudioItem"/> created with the specified <c>audioID</c></returns>
    /// <seealso cref="AudioController.NewCategory(string)"/>
    /// <seealso cref="AudioController.GetCategory(string)"/>
    static public AudioItem AddToCategory( AudioCategory category, AudioClip audioClip, string audioID )
    {
        var audioItem = new AudioItem();
        audioItem.Name = audioID;
        audioItem.subItems = new AudioSubItem[ 1 ];

        var audioSubItem = new AudioSubItem();
        audioSubItem.Clip = audioClip;
        audioItem.subItems[ 0 ] = audioSubItem;

        AddToCategory( category, audioItem );
        return audioItem;
    }

    /// <summary>
    /// Removes an AudioItem from the AudioController.
    /// </summary>
    /// <param name="audioID">Name of the audio item to remove.</param>
    /// <returns><c>true</c> if the audio item was found and successfully removed, otherwise <c>false</c></returns>
    static public bool RemoveAudioItem( string audioID )
    {
        var audioItem = Instance._GetAudioItem( audioID );

        if ( audioItem != null )
        {
            int index = audioItem.category._GetIndexOf( audioItem );
            if ( index < 0 ) return false; // should never be the case!

            var array = audioItem.category.AudioItems;

            //ArrayHelper.DeleteArrayElement( audioItem.category, index ); // Flash export does not currently work!! 
            {
                var newArray = new AudioItem[ array.Length - 1 ];
                int i;
                for ( i = 0; i < index; i++ )
                {
                    newArray[ i ] = array[ i ];
                }
                for ( i = index + 1; i < array.Length; i++ )
                {
                    newArray[ i - 1 ] = array[ i ];
                }
                audioItem.category.AudioItems = newArray;
            }

            if ( Instance._categoriesValidated )
            {
                Instance._audioItems.Remove( audioID );
            }
            return true;
        }
        else
            return false;
    }

    /// <summary>
    /// Tests if a given <c>audioID</c> is valid.
    /// </summary>
    /// <param name="audioID">The audioID</param>
    /// <returns><c>true</c> if the <c>audioID</c> is valid</returns>
    static public bool IsValidAudioID( string audioID )
    {
        return Instance._GetAudioItem( audioID ) != null;
    }

    /// <summary>
    /// Returns the <see cref="AudioItem"/> with the given <c>audioID</c>.
    /// </summary>
    /// <param name="audioID">The <c>audioID</c></param>
    /// <returns>The <see cref="AudioItem"/> if <c>audioID</c> is valid, else <c>null</c> </returns>
    static public AudioItem GetAudioItem( string audioID )
    {
        return Instance._GetAudioItem( audioID );
    }

    /// <summary>
    /// Detaches all audio objects possibly parented to the specified game object.
    /// </summary>
    /// <param name="gameObjectWithAudios">The GameObject with possibly playing AudioObjects.</param>
    /// <remarks>
    /// Use this method on a game object BEFORE destryoing it if you want to keep any audios playing 
    /// parented to this object.
    /// </remarks>
    static public void DetachAllAudios( GameObject gameObjectWithAudios )
    {
        var audioObjs = gameObjectWithAudios.GetComponentsInChildren<AudioObject>( true );
        for ( int index = 0; index < audioObjs.Length; index++ )
        {
            var a = audioObjs[ index ];
            a.transform.parent = null;
        }
    }

    /// <summary>
    /// Gets the audio item's max distance. (respects all proper default values and overwrites).
    /// </summary>
    /// <param name="audioID">The <c>audioID</c></param>
    /// <returns>The max distance applied to the AudioSource</returns>
    static public float GetAudioItemMaxDistance( string audioID )
    {
        var audioItem = AudioController.GetAudioItem( audioID );

        if ( audioItem.overrideAudioSourceSettings ) return audioItem.audioSource_MaxDistance;
        else if ( audioItem.category.AudioObjectPrefab != null )
        {
            return audioItem.category.AudioObjectPrefab.GetComponent<AudioSource>().maxDistance;
        }
        else
        {
            return audioItem.category.audioController.AudioObjectPrefab.GetComponent<AudioSource>().maxDistance;
        }
    }

    /// <summary>
    /// Unloads all AudioClips specified in this AudioController from memory. 
    /// </summary>
    /// <remarks>
    /// You will still be able to play the AudioClips, but you may experience performance hickups when Unity reloads the audio asset
    /// </remarks>

    public void UnloadAllAudioClips()
    {
        for ( int index = 0; index < AudioCategories.Length; index++ )
        {
            var c = AudioCategories[ index ];
            c.UnloadAllAudioClips();
        }
    }

    // **************************************************************************************************/
    //          private / protected functions and properties
    // **************************************************************************************************/

#if AUDIO_TOOLKIT_DEMO
    static protected AudioObject _currentMusic;
#else
    static protected PoolableReference<AudioObject> _currentMusicReference = new PoolableReference<AudioObject>();
    static private AudioObject _currentMusic
    {
        set
        {
            _currentMusicReference.Set( value, true );
        }
        get
        {
            return _currentMusicReference.Get();
        }
    }
#endif
    protected AudioListener _currentAudioListener = null;

    private bool _musicEnabled = true;
    private bool _soundMuted = false;
    private bool _categoriesValidated = false;

    [SerializeField]
    private bool _isAdditionalAudioController = false;

    [SerializeField]
    private bool _audioDisabled = false;

    Dictionary<string, AudioItem> _audioItems;

    static List<int> _playlistPlayed;
    static bool _isPlaylistPlaying = false;

    [SerializeField]
    private float _volume = 1.0f;

    static private double _systemTime;


    private void _ApplyVolumeChange()
    {
        List<AudioObject> objs = GetPlayingAudioObjects( true );

        for ( int index = 0; index < objs.Count; index++ )
        {
            AudioObject o = objs[ index ];
            if ( o != null )
            {
                o._ApplyVolumeBoth();
            }
        }
    }

    internal AudioItem _GetAudioItem( string audioID )
    {
        AudioItem sndItem;

        _ValidateCategories();

        if ( _audioItems.TryGetValue( audioID, out sndItem ) )
        {
            return sndItem;
        }

        return null;
    }

    protected AudioObject _PlayMusic( string audioID, float volume, float delay, float startTime )
    {
        AudioListener al = GetCurrentAudioListener();
        if ( al == null )
        {
            Debug.LogWarning( "No AudioListener found in the scene" );
            return null;
        }
        return _PlayMusic( audioID, al.transform.position + al.transform.forward, null, volume, delay, startTime );
    }

    protected bool _StopMusic( float fadeOutLength )
    {
        if ( _currentMusic != null )
        {
            _currentMusic.Stop( fadeOutLength );
            _currentMusic = null;
            return true;
        }
        return false;
    }

    protected bool _PauseMusic( float fadeOut )
    {
        if ( _currentMusic != null )
        {
            _currentMusic.Pause( fadeOut );
            return true;
        }
        return false;
    }

    protected AudioObject _PlayMusic( string audioID, Vector3 position, Transform parentObj, float volume, float delay, float startTime )
    {
        if ( !IsMusicEnabled() ) return null;

        bool doFadeIn;

        if ( _currentMusic != null && _currentMusic.IsPlaying() )
        {
            doFadeIn = true;
            _currentMusic.Stop( musicCrossFadeTime_Out );
        }
        else
            doFadeIn = false;

        //Debug.Log( "PlayMusic " + audioID );

        _currentMusic = _PlayAsMusic( audioID, volume, position, parentObj, delay, startTime, false );

        if ( _currentMusic )
        {
            if ( doFadeIn && musicCrossFadeTime_In > 0 )
            {
                _currentMusic.FadeIn( musicCrossFadeTime_In );
            }
        }

        return _currentMusic;
    }

    protected int _EnqueueMusic( string audioID )
    {
        int newLength;

        if ( musicPlaylist == null )
        {
            newLength = 1;
        }
        else
            newLength = musicPlaylist.Length + 1;

        string[ ] newPlayList = new string[ newLength ];

        if ( musicPlaylist != null )
        {
            musicPlaylist.CopyTo( newPlayList, 0 );
        }

        newPlayList[ newLength - 1 ] = audioID;
        musicPlaylist = newPlayList;

        return newLength;
    }

    protected AudioObject _PlayMusicPlaylist()
    {
        _ResetLastPlayedList();
        return _PlayNextMusicOnPlaylist( 0 );
    }

    private AudioObject _PlayMusicTrackWithID( int nextTrack, float delay, bool addToPlayedList )
    {
        if ( nextTrack < 0 )
        {
            return null;
        }
        _playlistPlayed.Add( nextTrack );
        _isPlaylistPlaying = true;
        //Debug.Log( "nextTrack: " + nextTrack );
        AudioObject audioObj = _PlayMusic( musicPlaylist[ nextTrack ], 1, delay, 0 );

        if ( audioObj != null )
        {
            audioObj._isCurrentPlaylistTrack = true;
            audioObj.primaryAudioSource.loop = false;
        }
        return audioObj;
    }

    internal AudioObject _PlayNextMusicOnPlaylist( float delay )
    {
        int nextTrack = _GetNextMusicTrack();
        return _PlayMusicTrackWithID( nextTrack, delay, true );
    }

    internal AudioObject _PlayPreviousMusicOnPlaylist( float delay )
    {
        int nextTrack = _GetPreviousMusicTrack();
        return _PlayMusicTrackWithID( nextTrack, delay, false );
    }

    private void _ResetLastPlayedList()
    {
        _playlistPlayed.Clear();
    }

    protected int _GetNextMusicTrack()
    {
        if ( musicPlaylist == null || musicPlaylist.Length == 0 ) return -1;
        if ( musicPlaylist.Length == 1 ) return 0;

        if ( shufflePlaylist )
        {
            return _GetNextMusicTrackShuffled();
        }
        else
        {
            return _GetNextMusicTrackInOrder();

        }
    }

    protected int _GetPreviousMusicTrack()
    {
        if ( musicPlaylist == null || musicPlaylist.Length == 0 ) return -1;
        if ( musicPlaylist.Length == 1 ) return 0;

        if ( shufflePlaylist )
        {
            return _GetPreviousMusicTrackShuffled();
        }
        else
        {
            return _GetPreviousMusicTrackInOrder();

        }
    }

    private int _GetPreviousMusicTrackShuffled()
    {
        if ( _playlistPlayed.Count >= 2 )
        {
            int id = _playlistPlayed[ _playlistPlayed.Count - 2 ];

            _RemoveLastPlayedOnList();
            _RemoveLastPlayedOnList();

            return id;
        }
        else
            return -1;
    }

    private void _RemoveLastPlayedOnList()
    {
        _playlistPlayed.RemoveAt( _playlistPlayed.Count - 1 );
    }

    private int _GetNextMusicTrackShuffled()
    {
        var playedTracksHashed = new HashSet_Flash<int>();

        int disallowTracksCount = _playlistPlayed.Count;

        int randomElementCount;

        if ( loopPlaylist )
        {
            randomElementCount = Mathf.Clamp( musicPlaylist.Length / 4, 2, 10 );

            if ( disallowTracksCount > musicPlaylist.Length - randomElementCount )
            {
                disallowTracksCount = musicPlaylist.Length - randomElementCount;

                if ( disallowTracksCount < 1 ) // the same track must never be played twice in a row
                {
                    disallowTracksCount = 1; // musicPlaylist.Length is always >= 2 
                }
            }
        }
        else
        {
            // do not play the same song twice
            if ( disallowTracksCount >= musicPlaylist.Length )
            {
                return -1; // stop playing as soon as all tracks have been played 
            }
        }


        for ( int i = 0; i < disallowTracksCount; i++ )
        {
            playedTracksHashed.Add( _playlistPlayed[ _playlistPlayed.Count - 1 - i ] );
        }

        var possibleTrackIDs = new List<int>();

        for ( int i = 0; i < musicPlaylist.Length; i++ )
        {
            if ( !playedTracksHashed.Contains( i ) )
            {
                possibleTrackIDs.Add( i );
            }
        }

        return possibleTrackIDs[ UnityEngine.Random.Range( 0, possibleTrackIDs.Count ) ];
    }

    private int _GetNextMusicTrackInOrder()
    {
        if ( _playlistPlayed.Count == 0 )
        {
            return 0;
        }
        int next = _playlistPlayed[ _playlistPlayed.Count - 1 ] + 1;

        if ( next >= musicPlaylist.Length ) // reached the end of the playlist
        {
            if ( loopPlaylist )
            {
                next = 0;
            }
            else
                return -1;
        }
        return next;
    }

    private int _GetPreviousMusicTrackInOrder()
    {
        if ( _playlistPlayed.Count < 2 )
        {
            if ( loopPlaylist )
            {
                return musicPlaylist.Length - 1;
            }
            else
                return -1;
        }

        int next = _playlistPlayed[ _playlistPlayed.Count - 1 ] - 1;

        _RemoveLastPlayedOnList();
        _RemoveLastPlayedOnList();

        if ( next < 0 ) // reached the end of the playlist
        {
            if ( loopPlaylist )
            {
                next = musicPlaylist.Length - 1;
            }
            else
                return -1;
        }
        return next;
    }

    protected AudioObject _PlayAsSound( string audioID, float volume, Vector3 worldPosition, Transform parentObj, float delay, float startTime, bool playWithoutAudioObject, double dspTime = 0, AudioObject useExistingAudioObject = null )
    {
        return _PlayEx( audioID, volume, worldPosition, parentObj, delay, startTime, playWithoutAudioObject, dspTime, useExistingAudioObject, false );
    }

    protected AudioObject _PlayAsMusic( string audioID, float volume, Vector3 worldPosition, Transform parentObj, float delay, float startTime, bool playWithoutAudioObject, double dspTime = 0, AudioObject useExistingAudioObject = null )
    {
        return _PlayEx( audioID, volume, worldPosition, parentObj, delay, startTime, playWithoutAudioObject, dspTime, useExistingAudioObject, true );
    }

    protected AudioObject _PlayEx( string audioID, float volume, Vector3 worldPosition, Transform parentObj, float delay, float startTime, bool playWithoutAudioObject, double dspTime = 0, AudioObject useExistingAudioObject = null, bool playAsMusic = false )
    {
        //Debug.Log( "AudioController Play: " + audioID );

        if ( _audioDisabled ) return null;

        AudioItem sndItem = _GetAudioItem( audioID );
        if ( sndItem == null )
        {
            Debug.LogWarning( "Audio item with name '" + audioID + "' does not exist" );
            return null;
        }


        if ( sndItem._lastPlayedTime > 0 && dspTime == 0 )
        {
            double deltaT = AudioController.systemTime - sndItem._lastPlayedTime;

            if ( deltaT < sndItem.MinTimeBetweenPlayCalls )
            {

#if UNITY_EDITOR && !AUDIO_TOOLKIT_DEMO
                var logData = new AudioLog.LogData_SkippedPlay();
                logData.audioID = audioID;
                if ( sndItem != null && sndItem.category != null )
                {
                    logData.category = sndItem.category.Name;
                }
                logData.delay = delay;
                logData.parentObject = parentObj != null ? parentObj.name : "";
                logData.position = worldPosition;
                logData.startTime = startTime;
                logData.volume = volume;

                logData.reasonForSkip = string.Format( "{0:0.00}", deltaT ) + " < MinTimeBetweenPlay";

#if UNITY_AUDIO_FEATURES_4_1
                if ( dspTime > 0 )
                {
                    logData.scheduledDspTime = Time.time + (float) ( dspTime - AudioSettings.dspTime );
                }
#endif

                AudioLog.Log( logData );
#endif

                return null;
            }
        }

        if ( sndItem.MaxInstanceCount > 0 )
        {
            var playingAudios = GetPlayingAudioObjects( audioID );  // TODO: check performance of GetPlayingAudioObjects

            bool isExceeding = playingAudios.Count >= sndItem.MaxInstanceCount;

            if ( isExceeding )
            {
                bool isExceedingByMoreThanOne = playingAudios.Count > sndItem.MaxInstanceCount;

                // search oldest audio and stop it.
                AudioObject oldestAudio = null;

                for ( int i = 0; i < playingAudios.Count; i++ )
                {
                    if ( !isExceedingByMoreThanOne )
                    {
                        if ( playingAudios[ i ].isFadingOut ) continue;
                    }
                    if ( oldestAudio == null || playingAudios[ i ].startedPlayingAtTime < oldestAudio.startedPlayingAtTime )
                    {
                        oldestAudio = playingAudios[ i ];
                    }
                }
                //oldestAudio.DestroyAudioObject(); // produces cracking noise

                if ( oldestAudio != null )
                {
                    oldestAudio.Stop( isExceedingByMoreThanOne ? 0 : 0.2f );
                }

            }
        }

        return PlayAudioItem( sndItem, volume, worldPosition, parentObj, delay, startTime, playWithoutAudioObject, useExistingAudioObject, dspTime, playAsMusic );
    }

    /// <summary>
    /// Plays a specific AudioItem.
    /// </summary>
    /// <remarks>
    /// This function is used by the editor extension and is normally not required for application developers. 
    /// Use <see cref="AudioController.Play(string)"/> instead.
    /// </remarks>
    /// <param name="sndItem">the AudioItem</param>
    /// <param name="volume">the volume</param>
    /// <param name="worldPosition">the world position </param>
    /// <param name="parentObj">the parent object, or <c>null</c></param>
    /// <param name="delay">the delay in seconds</param>
    /// <param name="startTime">the start time seconds</param>
    /// <param name="playWithoutAudioObject">if <c>true</c>plays the audio by using the Unity 
    /// function <c>PlayOneShot</c> without creating an audio game object. Allows playing audios from within the Unity inspector.
    /// </param>
    /// <param name="useExistingAudioObj">if specified this existing audio object is used instead of creating a new <see cref="AudioObject"/></param>
    /// <param name="dspTime">The high precision DSP time at which to schedule playing the audio [default=0]</param>
    /// <param name="playAsMusic">Determines if it is effected by sound muting [default=false]</param>
    /// <returns>
    /// The created <see cref="AudioObject"/> or <c>null</c>
    /// </returns>
    public AudioObject PlayAudioItem( AudioItem sndItem, float volume, Vector3 worldPosition, Transform parentObj = null, float delay = 0, float startTime = 0, bool playWithoutAudioObject = false, AudioObject useExistingAudioObj = null, double dspTime = 0, bool playAsMusic = false )
    {
        AudioObject audioObj = null;

        //Debug.Log( "PlayAudioItem '" + sndItem.Name + "'" );

        sndItem._lastPlayedTime = AudioController.systemTime;

        AudioSubItem[ ] sndSubItems = _ChooseSubItems( sndItem, useExistingAudioObj );

        if ( sndSubItems == null || sndSubItems.Length == 0 )
        {
            return null;
        }

        for ( int index = 0; index < sndSubItems.Length; index++ )
        {
            var sndSubItem = sndSubItems[ index ];
            if ( sndSubItem != null )
            {
                var audioObjRet = PlayAudioSubItem( sndSubItem, volume, worldPosition, parentObj, delay, startTime, playWithoutAudioObject, useExistingAudioObj, dspTime, playAsMusic );

                if ( audioObjRet )
                {
                    audioObj = audioObjRet;
                    audioObj.audioID = sndItem.Name;

                    if ( sndItem.overrideAudioSourceSettings )
                    {
                        audioObjRet._audioSource_MinDistance_Saved = audioObjRet.primaryAudioSource.minDistance;
                        audioObjRet._audioSource_MaxDistance_Saved = audioObjRet.primaryAudioSource.maxDistance;
                        audioObjRet.primaryAudioSource.minDistance = sndItem.audioSource_MinDistance;
                        audioObjRet.primaryAudioSource.maxDistance = sndItem.audioSource_MaxDistance;

                        if ( audioObjRet.secondaryAudioSource != null )
                        {
                            audioObjRet.secondaryAudioSource.minDistance = sndItem.audioSource_MinDistance;
                            audioObjRet.secondaryAudioSource.maxDistance = sndItem.audioSource_MaxDistance;
                        }
                    }
                }
            }
        }

        return audioObj;
    }

    internal AudioCategory _GetCategory( string name )
    {
        for ( int index = 0; index < AudioCategories.Length; index++ )
        {
            AudioCategory cat = AudioCategories[ index ];
            if ( cat.Name == name )
            {
                return cat;
            }
        }
        return null;
    }

    private static double _lastSystemTime = -1;
    private static double _systemDeltaTime = -1;

    void Update()
    {
        if ( !_isAdditionalAudioController )
        {
            _UpdateSystemTime();
        }
    }

    static private void _UpdateSystemTime()
    {
        double newSystemTime = SystemTime.timeSinceLaunch;

        if ( _lastSystemTime >= 0 )
        {
            _systemDeltaTime = newSystemTime - _lastSystemTime;
            if ( _systemDeltaTime <= Time.maximumDeltaTime + 0.01f )
            {
                _systemTime += _systemDeltaTime;
            }
            else
            {
                _systemDeltaTime = 0;
            }
        }
        else
        {
            _systemDeltaTime = 0;
            _systemTime = 0;
        }
        _lastSystemTime = newSystemTime;
    }


#if AUDIO_TOOLKIT_DEMO
    protected virtual void Awake()
    {
        if ( isAdditionalAudioController )
        {
            AudioController.Instance._RegisterAdditionalAudioController( this );
        }
    }

    protected virtual void OnDestroy()
    {
        if ( isAdditionalAudioController && AudioController.DoesInstanceExist() )
        {
            AudioController.Instance._UnregisterAdditionalAudioController( this );
        }
    }

    public virtual bool isSingletonObject
    {
        get
        {
            return !_isAdditionalAudioController;
        }
    }

#else
    protected override void Awake()
    {
        base.Awake();
        // all initialisation must be done in AwakeSingleton()

        if ( !isAdditionalAudioController )
        {
            AwakeSingleton(); // AwakeSingleton only gets called by UnitySingleton if this is the main singleton object
        }
    }

    void OnEnable()
    {
        if ( isAdditionalAudioController )
        {
            AudioController.Instance._RegisterAdditionalAudioController( this );
        }
        else
        {
            AwakeSingleton(); // AwakeSingleton only gets called by UnitySingleton if this is the main singleton object
        }
    }

    /// <summary>
    /// returns <c>true </c>if the AudioController is the main controller (not an additional controller)
    /// </summary>
    public override bool isSingletonObject
    {
        get
        {
            return !_isAdditionalAudioController;
        }
    }

    protected override void OnDestroy()
    {
        if ( UnloadAudioClipsOnDestroy )
        {
            UnloadAllAudioClips();
        }
        base.OnDestroy();
    }

    void OnDisable()
    {
        if ( isAdditionalAudioController && AudioController.DoesInstanceExist() )
        {
            AudioController.Instance._UnregisterAdditionalAudioController( this );
        }
    }
#endif

    void AwakeSingleton() // is called by singleton, can be called before Awake() 
    {
        _UpdateSystemTime();

        if ( Persistent )
        {
            DontDestroyOnLoad( gameObject );
        }

        if ( AudioObjectPrefab == null )
        {
            Debug.LogError( "No AudioObject prefab specified in AudioController." );
        }
        else
        {
            _ValidateAudioObjectPrefab( AudioObjectPrefab );
        }
        _ValidateCategories();

        if ( _playlistPlayed == null )
        {
            _playlistPlayed = new List<int>();
            _isPlaylistPlaying = false;
        }
    }

    protected void _ValidateCategories()
    {
        if ( !_categoriesValidated )
        {
            InitializeAudioItems();

            _categoriesValidated = true;
        }
    }

    protected void _InvalidateCategories()
    {
        _categoriesValidated = false;
    }

    /// <summary>
    /// Updates the internal <c>audioID</c> dictionary and initializes all registered <see cref="AudioItem"/> objects.
    /// </summary>
    /// <remarks>
    /// There is no need to call this function manually, unless <see cref="AudioItem"/> objects or categories are changed at runtime.
    /// </remarks>
    public void InitializeAudioItems()
    {
        if ( isAdditionalAudioController )
        {
            return;
        }

        _audioItems = new Dictionary<string, AudioItem>();

        _InitializeAudioItems( this );
        if ( _additionalAudioControllers != null )
        {
            for ( int index = 0; index < _additionalAudioControllers.Count; index++ )
            {
                var ac = _additionalAudioControllers[ index ];
                if ( ac != null )
                {
                    _InitializeAudioItems( ac );
                }
            }
        }
    }

    private void _InitializeAudioItems( AudioController audioController )
    {
        for ( int index = 0; index < audioController.AudioCategories.Length; index++ )
        {
            AudioCategory category = audioController.AudioCategories[ index ];
            category.audioController = audioController;
            category._AnalyseAudioItems( _audioItems );

            if ( category.AudioObjectPrefab )
            {
                _ValidateAudioObjectPrefab( category.AudioObjectPrefab );
            }
        }
    }

    private List<AudioController> _additionalAudioControllers;

    private void _RegisterAdditionalAudioController( AudioController ac )
    {
        if ( _additionalAudioControllers == null )
        {
            _additionalAudioControllers = new List<AudioController>();
        }

        _additionalAudioControllers.Add( ac );

        _InvalidateCategories();
        _SyncCategoryVolumes( ac, this );
    }

    private void _SyncCategoryVolumes( AudioController toSync, AudioController syncWith )
    {
        for ( int i=0; i<toSync.AudioCategories.Length; i++ )
        {
            var catDest = toSync.AudioCategories[ i ];
            var catSource = syncWith._GetCategory( catDest.Name );
            if ( catSource != null )
            {
                catDest.Volume = catSource.Volume;
            }
        }
    }

    private void _UnregisterAdditionalAudioController( AudioController ac )
    {
        if ( _additionalAudioControllers != null )
        {
            int i;
            for ( i = 0; i < _additionalAudioControllers.Count; i++ )
            {
                if ( _additionalAudioControllers[ i ] == ac )
                {
                    _additionalAudioControllers.RemoveAt( i );
                    _InvalidateCategories();
                    return;
                }
            }

        }
        else
        {
            Debug.LogWarning( "_UnregisterAdditionalAudioController: AudioController " + ac.name + " not found" );
        }

    }

    protected static AudioSubItem[ ] _ChooseSubItems( AudioItem audioItem, AudioObject useExistingAudioObj )
    {
        return _ChooseSubItems( audioItem, audioItem.SubItemPickMode, useExistingAudioObj );
    }

    internal static AudioSubItem _ChooseSingleSubItem( AudioItem audioItem, AudioPickSubItemMode pickMode, AudioObject useExistingAudioObj )
    {
        return _ChooseSubItems( audioItem, pickMode, useExistingAudioObj )[ 0 ];
    }

    protected static AudioSubItem[ ] _ChooseSubItems( AudioItem audioItem, AudioPickSubItemMode pickMode, AudioObject useExistingAudioObj )
    {
        if ( audioItem.subItems == null ) return null;
        int arraySize = audioItem.subItems.Length;
        if ( arraySize == 0 ) return null;

        int chosen = 0;
        AudioSubItem[ ] chosenItems;

        int lastChosen;

        bool useLastChosenOfExistingAudioObj = !object.ReferenceEquals( useExistingAudioObj, null );

        if ( useLastChosenOfExistingAudioObj ) // don't use Unity's operator here, because it will be called by GaplessAudioClipTransistion
        {
            lastChosen = useExistingAudioObj._lastChosenSubItemIndex;
        }
        else
            lastChosen = audioItem._lastChosen;

        if ( arraySize > 1 )
        {
            switch ( pickMode )
            {
            case AudioPickSubItemMode.Disabled:
                return null;

            case AudioPickSubItemMode.StartLoopSequenceWithFirst:
                if ( useLastChosenOfExistingAudioObj )
                {
                    chosen = ( lastChosen + 1 ) % arraySize;
                }
                else
                {
                    chosen = 0;
                }
                break;

            case AudioPickSubItemMode.Sequence:
                chosen = ( lastChosen + 1 ) % arraySize;
                break;

            case AudioPickSubItemMode.SequenceWithRandomStart:
                if ( lastChosen == -1 )
                {
                    chosen = UnityEngine.Random.Range( 0, arraySize );
                }
                else
                    chosen = ( lastChosen + 1 ) % arraySize;
                break;

            case AudioPickSubItemMode.Random:
                chosen = _ChooseRandomSubitem( audioItem, true, lastChosen );
                break;

            case AudioPickSubItemMode.RandomNotSameTwice:
                chosen = _ChooseRandomSubitem( audioItem, false, lastChosen );
                break;

            case AudioPickSubItemMode.AllSimultaneously:
                chosenItems = new AudioSubItem[ arraySize ];
                for ( int i = 0; i < arraySize; i++ )  // Array.Copy( audioItem.subItems, chosenItems, arraySize );  not working on Flash export
                {
                    chosenItems[ i ] = audioItem.subItems[ i ];
                }

                return chosenItems;

            case AudioPickSubItemMode.TwoSimultaneously:
                chosenItems = new AudioSubItem[ 2 ];
                chosenItems[ 0 ] = _ChooseSingleSubItem( audioItem, AudioPickSubItemMode.RandomNotSameTwice, useExistingAudioObj );
                chosenItems[ 1 ] = _ChooseSingleSubItem( audioItem, AudioPickSubItemMode.RandomNotSameTwice, useExistingAudioObj );
                return chosenItems;

            case AudioPickSubItemMode.RandomNotSameTwiceOddsEvens:
                chosen = _ChooseRandomSubitem( audioItem, false, lastChosen, true );
                break;
            }
        }

        if ( useLastChosenOfExistingAudioObj )
        {
            useExistingAudioObj._lastChosenSubItemIndex = chosen;
        }
        else
            audioItem._lastChosen = chosen;

        //Debug.Log( "chose:" + chosen );
        chosenItems = new AudioSubItem[ 1 ];
        chosenItems[ 0 ] = audioItem.subItems[ chosen ];
        return chosenItems;
    }

    private static int _ChooseRandomSubitem( AudioItem audioItem, bool allowSameElementTwiceInRow, int lastChosen, bool switchOddsEvens = false )
    {
        int arraySize = audioItem.subItems.Length; // is >= 2 at this point 
        int chosen = 0;

        float probRange;
        float lastProb = 0;

        if ( !allowSameElementTwiceInRow )
        {
            // find out probability of last chosen sub item
            if ( lastChosen >= 0 )
            {
                lastProb = audioItem.subItems[ lastChosen ]._SummedProbability;
                if ( lastChosen >= 1 )
                {
                    lastProb -= audioItem.subItems[ lastChosen - 1 ]._SummedProbability;
                }
            }
            else
                lastProb = 0;

            probRange = 1.0f - lastProb;
        }
        else
            probRange = 1.0f;

        float rnd = UnityEngine.Random.Range( 0, probRange );

        int i;
        for ( i = 0; i < arraySize - 1; i++ )
        {
            float prob;

            prob = audioItem.subItems[ i ]._SummedProbability;

            if ( switchOddsEvens )
            {
                if ( isOdd( i ) == isOdd( lastChosen ) )
                {
                    continue;
                }
            }

            if ( !allowSameElementTwiceInRow )
            {
                if ( i == lastChosen )
                {
                    if ( prob != 1.0f || !audioItem.subItems[ i ].DisableOtherSubitems ) // DisableOtherSubitems allows the same be played twice in a row 
                    {
                        continue; // do not play same audio twice
                    }
                }

                if ( i > lastChosen )
                {
                    prob -= lastProb;
                }
            }

            if ( prob > rnd )
            {
                chosen = i;
                break;
            }
        }
        if ( i == arraySize - 1 )
        {
            chosen = arraySize - 1;
        }

        return chosen;
    }

    static bool isOdd( int i )
    {
        return i % 2 != 0;
    }

    /// <summary>
    /// Plays a specific AudioSubItem.
    /// </summary>
    /// <remarks>
    /// This function is used by the editor extension and is normally not required for application developers. 
    /// Use <see cref="AudioController.Play(string)"/> instead.
    /// </remarks>
    /// <param name="subItem">the <see cref="AudioSubItem"/></param>
    /// <param name="volume">the volume</param>
    /// <param name="worldPosition">the world position </param>
    /// <param name="parentObj">the parent object, or <c>null</c></param>
    /// <param name="delay">the delay in seconds</param>
    /// <param name="startTime">the start time seconds</param>
    /// <param name="playWithoutAudioObject">if <c>true</c>plays the audio by using the Unity 
    /// function <c>PlayOneShot</c> without creating an audio game object. Allows playing audios from within the Unity inspector.
    /// </param>
    /// <param name="useExistingAudioObj">if specified this existing audio object is used instead of creating a new <see cref="AudioObject"/></param>
    /// <param name="dspTime">The high precision DSP time at which to schedule playing the audio [default=0]</param>
    /// <param name="playAsMusic">if <c>true</c>plays the audio as the music track [default=<c>false</c>]</param>
    /// <returns>
    /// The created <see cref="AudioObject"/> or <c>null</c>
    /// </returns>
    public AudioObject PlayAudioSubItem( AudioSubItem subItem, float volume, Vector3 worldPosition, Transform parentObj, float delay, float startTime, bool playWithoutAudioObject, AudioObject useExistingAudioObj, double dspTime = 0, bool playAsMusic = false )
    {
        _ValidateCategories();

        var audioItem = subItem.item;

        switch ( subItem.SubItemType )
        {
        case AudioSubItemType.Item:
            if ( subItem.ItemModeAudioID.Length == 0 )
            {
                Debug.LogWarning( "No item specified in audio sub-item with ITEM mode (audio item: '" + audioItem.Name + "')" );
                return null;
            }
            return _PlayAsSound( subItem.ItemModeAudioID, volume, worldPosition, parentObj, delay, startTime, playWithoutAudioObject, dspTime, useExistingAudioObj );

        case AudioSubItemType.Clip:
            break;
        }

        if ( subItem.Clip == null )
        {
            return null;
        }

        //Debug.Log( "PlayAudioSubItem Clip '" + subItem.Clip.name + "'" );

        var audioCategory = audioItem.category;

        float volumeWithoutCategory = subItem.Volume * audioItem.Volume * volume;

        if ( subItem.RandomVolume != 0 || audioItem.loopSequenceRandomVolume != 0 )
        {
            float randomVolume = subItem.RandomVolume + audioItem.loopSequenceRandomVolume;
            volumeWithoutCategory += UnityEngine.Random.Range( -randomVolume, randomVolume );
            volumeWithoutCategory = Mathf.Clamp01( volumeWithoutCategory );
        }

        float volumeWithCategory = volumeWithoutCategory * audioCategory.VolumeTotal;

        var subItemAudioController = _GetAudioController( subItem );

        if ( !subItemAudioController.PlayWithZeroVolume && ( volumeWithCategory <= 0 || Volume <= 0 ) )
        {
            return null;
        }

        GameObject audioObjInstance;

        //Debug.Log( "PlayAudioItem clip:" + subItem.Clip.name );

        GameObject audioPrefab;

        if ( audioCategory.AudioObjectPrefab != null )
        {
            audioPrefab = audioCategory.AudioObjectPrefab;
        }
        else
        {
            if ( subItemAudioController.AudioObjectPrefab != null )
            {
                audioPrefab = subItemAudioController.AudioObjectPrefab;
            }
            else
                audioPrefab = AudioObjectPrefab;
        }

        if ( playWithoutAudioObject )
        {
            audioPrefab.GetComponent<AudioSource>().PlayOneShot( subItem.Clip, AudioObject.TransformVolume( volumeWithCategory ) ); // unfortunately produces warning message, but works (tested only with Unity 3.5)

            //AudioSource.PlayClipAtPoint( subItem.Clip, Vector3.zero, AudioObject.TransformVolume( volumeWithCategory ) );
            return null;
        }

        AudioObject sndObj;

        if ( useExistingAudioObj == null )
        {
            if ( audioItem.DestroyOnLoad )
            {
#if AUDIO_TOOLKIT_DEMO
                audioObjInstance = (GameObject) GameObject.Instantiate( audioPrefab, worldPosition, Quaternion.identity );

#else
                if ( subItemAudioController.UsePooledAudioObjects )
                {
                    audioObjInstance = (GameObject) ObjectPoolController.Instantiate( audioPrefab, worldPosition, Quaternion.identity );
                }
                else
                {
                    audioObjInstance = (GameObject) ObjectPoolController.InstantiateWithoutPool( audioPrefab, worldPosition, Quaternion.identity );
                }
#endif
            }
            else
            {   // pooling does not work for DontDestroyOnLoad objects
#if AUDIO_TOOLKIT_DEMO
                audioObjInstance = (GameObject) GameObject.Instantiate( audioPrefab, worldPosition, Quaternion.identity );
#else
                audioObjInstance = (GameObject) ObjectPoolController.InstantiateWithoutPool( audioPrefab, worldPosition, Quaternion.identity );
#endif
                DontDestroyOnLoad( audioObjInstance );
            }
            if ( parentObj )
            {
                audioObjInstance.transform.parent = parentObj;
            }

            sndObj = audioObjInstance.gameObject.GetComponent<AudioObject>();
        }
        else
        {
            audioObjInstance = useExistingAudioObj.gameObject;
            sndObj = useExistingAudioObj;
        }

        sndObj.subItem = subItem;

        if ( object.ReferenceEquals( useExistingAudioObj, null ) )
        {
            sndObj._lastChosenSubItemIndex = audioItem._lastChosen;
        }

        sndObj.primaryAudioSource.clip = subItem.Clip;
        audioObjInstance.name = "AudioObject:" + sndObj.primaryAudioSource.clip.name;

        sndObj.primaryAudioSource.pitch = AudioObject.TransformPitch( subItem.PitchShift );
#if UNITY_5_OR_NEWER
        sndObj.primaryAudioSource.panStereo = subItem.Pan2D;
        sndObj.primaryAudioSource.spatialBlend = audioItem.spatialBlend;
#else
        sndObj.primaryAudioSource.pan = subItem.Pan2D;
#endif

        if ( subItem.RandomStartPosition )
        {
            startTime = UnityEngine.Random.Range( 0, sndObj.clipLength );
        }

        sndObj.primaryAudioSource.time = startTime + subItem.ClipStartTime;

        sndObj.primaryAudioSource.loop = ( audioItem.Loop == AudioItem.LoopMode.LoopSubitem || audioItem.Loop == (AudioItem.LoopMode) 3 ); // 3... deprecated gapless loop mode

        sndObj._volumeExcludingCategory = volumeWithoutCategory;
        sndObj._volumeFromScriptCall = volume;
        sndObj.category = audioCategory;
        sndObj.isPlayedAsMusic = playAsMusic;

        sndObj._ApplyVolumePrimary();

        if ( subItem.RandomPitch != 0 || audioItem.loopSequenceRandomPitch != 0 )
        {
            float randomPitch = subItem.RandomPitch + audioItem.loopSequenceRandomPitch;
            sndObj.primaryAudioSource.pitch *= AudioObject.TransformPitch( UnityEngine.Random.Range( -randomPitch, randomPitch ) );
        }

        if ( subItem.RandomDelay > 0 )
        {
            delay += UnityEngine.Random.Range( 0, subItem.RandomDelay );
        }

        if ( dspTime > 0 )
        {
            sndObj.PlayScheduled( dspTime + delay + subItem.Delay + audioItem.Delay );

        }
        else
            sndObj.Play( delay + subItem.Delay + audioItem.Delay );

        if ( subItem.FadeIn > 0 )
        {
            sndObj.FadeIn( subItem.FadeIn );
        }

#if UNITY_EDITOR && !AUDIO_TOOLKIT_DEMO
        var logData = new AudioLog.LogData_PlayClip();
        logData.audioID = audioItem.Name;
        logData.category = audioCategory.Name;
        logData.clipName = subItem.Clip.name;
        logData.delay = delay;
        logData.parentObject = parentObj != null ? parentObj.name : "";
        logData.position = worldPosition;
        logData.startTime = startTime;
        logData.volume = volumeWithCategory;
        logData.pitch = sndObj.primaryAudioSource.pitch;

#if UNITY_AUDIO_FEATURES_4_1
        if ( dspTime > 0 )
        {
            logData.scheduledDspTime = Time.time + (float) ( dspTime - AudioSettings.dspTime );
        }
#endif

        AudioLog.Log( logData );
#endif

        return sndObj;
    }

    private AudioController _GetAudioController( AudioSubItem subItem )
    {
        if ( subItem.item != null && subItem.item.category != null )
        {
            return subItem.item.category.audioController;
        }
        return this;
    }

    internal void _NotifyPlaylistTrackCompleteleyPlayed( AudioObject audioObject )
    {
        audioObject._isCurrentPlaylistTrack = false;
        if ( IsPlaylistPlaying() )
        {
            if ( _currentMusic == audioObject )
            {
                if ( _PlayNextMusicOnPlaylist( delayBetweenPlaylistTracks ) == null )
                {
                    _isPlaylistPlaying = false;
                }
            }
        }
    }

    private void _ValidateAudioObjectPrefab( GameObject audioPrefab )
    {
        if ( UsePooledAudioObjects )
        {
#if AUDIO_TOOLKIT_DEMO
        Debug.LogWarning( "Poolable Audio objects not supported by the Audio Toolkit Demo version" );
#else
            if ( audioPrefab.GetComponent<PoolableObject>() == null )
            {
                Debug.LogWarning( "AudioObject prefab does not have the PoolableObject component. Pooling will not work." );
            }
            else
            {
                ObjectPoolController.Preload( audioPrefab );
            }
#endif
        }

        if ( audioPrefab.GetComponent<AudioObject>() == null )
        {
            Debug.LogError( "AudioObject prefab must have the AudioObject script component!" );
        }
    }

    // is public because custom inspector must access it
    public AudioController_CurrentInspectorSelection _currentInspectorSelection = new AudioController_CurrentInspectorSelection();

    public AudioController()
    {
        AudioController.SetSingletonType( typeof( AudioController ) );
    }
}

[Serializable]
public class AudioController_CurrentInspectorSelection
{
    public int currentCategoryIndex = 0;
    public int currentItemIndex = 0;
    public int currentSubitemIndex = 0;
    public int currentPlaylistIndex = 0;
}


