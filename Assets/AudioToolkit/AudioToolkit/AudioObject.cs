// AudioObject - copyright by ClockStone 2013
// part of the ClockStone Unity Audio Framework, see AudioController.cs

#if UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1
#define UNITY_AUDIO_FEATURES_4_0
#else
#define UNITY_AUDIO_FEATURES_4_1
#endif

#if UNITY_5 || UNITY_6 || UNITY_7
#define UNITY_5_OR_NEWER
#endif

using UnityEngine;
using System.Collections;

/// <summary>
/// The object playing the audio clip associated with a <see cref="AudioSubItem"/>
/// </summary>
/// <remarks>
/// If audio object pooling is enabled make sure you store references to an AudioObject
/// by using <see cref="PoolableReference{T}"/>
/// </remarks>
/// <example>
/// <code>
/// var soundFX = new PoolableReference&lt;AudioObject&gt;( AudioController.Play( "someSFX" ) );
/// 
/// // some other part of the code executed later when the sound may have stopped playing 
/// // and was moved back to the pool
/// AudioObject audioObject = soundFX.Get();
/// if( audioObject != null )
/// {
///     // it is safe to access audioObject here
///     audioObject.Stop();
/// }
/// </code>
/// </example>
[RequireComponent( typeof( AudioSource ) )]
[AddComponentMenu("ClockStone/Audio/AudioObject")]
public class AudioObject : RegisteredComponent
#if OUYA_PAUSE_SYSTEM
, OuyaSDK.IPauseListener, OuyaSDK.IResumeListener
#endif
{

	// **************************************************************************************************/
	//          public functions
	// **************************************************************************************************/

	/// <summary>
	/// Gets the audio ID.
	/// </summary>
    public string audioID
    {
        get;
        internal set;
    }

    [System.NonSerializedAttribute] // circle reference causes problem with Unity serialisation
    private AudioCategory _category;

	/// <summary>
	/// Gets the category.
	/// </summary>
	public AudioCategory category
	{
        get { return _category; }
        internal set
        {
            _category = value;
        }
	}

    /// <summary>
    /// Gets the corresponding AudioSubItem
    /// </summary>
    public AudioSubItem subItem
    {
        get {
            return _subItemPrimary;
        }
        internal set
        {
            _subItemPrimary = value;
        }
    }

    /// <summary>
    /// Returns true if the audio object is treated as music
    /// </summary>
    public bool isPlayedAsMusic
    {
        get;
        internal set;
    }

    private AudioSubItem _subItemPrimary;
    private AudioSubItem _subItemSecondary;


    /// <summary>
    /// Gets the corresponding AudioSubItem
    /// </summary>
    public AudioItem audioItem
    {
        get
        {
            if( subItem != null )
            {
                return subItem.item;
            }
            return null;
        }
    }

    /// <summary>
    /// The audio event delegate type.
    /// </summary>
    public delegate void AudioEventDelegate( AudioObject audioObject );

    /// <summary>
    /// Gets or sets the delegate to be called once an audio clip was completely played.
    /// </summary>
    public AudioEventDelegate completelyPlayedDelegate
    { 
        set
        {
            _completelyPlayedDelegate = value;
         }
        get
        { 
            return _completelyPlayedDelegate;
        }
    }

    private AudioEventDelegate _completelyPlayedDelegate;

	/// <summary>
	/// Gets or sets the volume.
	/// </summary>
    /// <remarks>
    /// This is the adjusted volume value with which the Audio clip is currently playing.
    /// It is the value resulting from multiplying the volume of the subitem, item, the category, and the script parameter. 
    /// It does not contain the global volume or the fading value.<para/>
    /// "Adjusted" means that the value does not equal Unity's internal audio clip volume value,
    /// because Unity's volume range is not distributed is a perceptually even manner.<para/>
    /// <c>unityVolume = Mathf.Pow( adjustedVolume, 1.6 )</c>
    /// </remarks>
	public float volume
	{
		get
		{
			return _volumeWithCategory;
		}
		set
		{
			// sets _volumeExcludingCategory so that _volumeWithCategory = volume = value 

            float volCat = _volumeFromCategory;

            if ( volCat > 0 )
            {
                _volumeExcludingCategory = value / volCat;
            }
            else
                _volumeExcludingCategory = value;

            _ApplyVolumeBoth();
		}
	}

    /// <summary>
    /// Gets or sets the volume of the audio item.
    /// </summary>
    /// <remarks>
    /// This is the adjusted volume value with which the Audio clip is currently playing.
    /// It is the value resulting from multiplying the volume of the subitem and the item. 
    /// It does not contain the global volume, the category, the script parameter, or the fading value.<para/>
    /// "Adjusted" means that the value does not equal Unity's internal audio clip volume value,
    /// because Unity's volume range is not distributed is a perceptually even manner.<para/>
    /// <c>unityVolume = Mathf.Pow( adjustedVolume, 1.6 )</c>
    /// </remarks>
    public float volumeItem
    {
        get
        {
            if ( _volumeFromScriptCall > 0 )
            {
                return _volumeExcludingCategory / _volumeFromScriptCall;
            }
            else 
                return _volumeExcludingCategory;
        }
        set
        {
            _volumeExcludingCategory = value * _volumeFromScriptCall;
            _ApplyVolumeBoth();
        }
    }

    /// <summary>
    /// Gets the total volume.
    /// </summary>
    /// <remarks>
    /// This is the adjusted volume value with which the Audio clip is currently playing.
    /// It is the value resulting from multiplying the volume of the subitem, item, the category, the script parameter,  
    /// the global volume, and the fading value.<para/>
    /// "Adjusted" means that the value does not equal Unity's internal audio clip volume value,
    /// because Unity's volume range is not distributed is a perceptually even manner.<para/>
    /// <c>unityVolume = Mathf.Pow( adjustedVolume, 1.6 )</c>
    /// </remarks>
    public float volumeTotal
    {
        get
        {
            return volumeTotalWithoutFade * _volumeFromPrimaryFade;
        }

    }

    /// <summary>
    /// Gets the total volume.
    /// </summary>
    /// <remarks>
    /// This is the adjusted volume value with which the Audio clip is currently playing without fade in/out.
    /// It is the value resulting from multiplying the volume of the subitem, item, the category, the script parameter,  
    /// the global volume and sound muting.<para/>
    /// "Adjusted" means that the value does not equal Unity's internal audio clip volume value,
    /// because Unity's volume range is not distributed is a perceptually even manner.<para/>
    /// <c>unityVolume = Mathf.Pow( adjustedVolume, 1.6 )</c>
    /// </remarks>
    public float volumeTotalWithoutFade
    {
        get
        {
            float vol = _volumeWithCategory;

            AudioController ac = null;

            if ( category != null )
            {
                ac = category.audioController; // considers additional AudioControllers
            }
            else
                ac = _audioController;

            if ( ac != null )
            {
                vol *= ac.Volume;

                if ( ac.soundMuted && !isPlayedAsMusic )
                {
                    vol = 0;
                }
            }
            
            return vol;
        }

    }

    /// <summary>
    /// Gets the <see cref="AudioController.systemTime"/> at which the audio Play() function was called. 
    /// </summary>
    /// <remarks>
    /// If a play was scheduled or delayed, the actual time at which the audio started playing is different.
    /// </remarks>
    public double playCalledAtTime
    {
        get
        {
            return _playTime;
        }
    }

    /// <summary>
    /// Gets the <see cref="AudioController.systemTime"/> at which the audio started playing.  
    /// </summary>
    /// <remarks>
    /// If a play was scheduled or delayed, this value is different than <see cref="playCalledAtTime"/>
    /// </remarks>
    public double startedPlayingAtTime
    {
        get
        {
            return _playStartTimeSystem;
        }
    }

    /// <summary>
    /// Gets the time until the clip will stop. 
    /// </summary>
    /// <remarks>
    /// Is effected by <see cref="AudioSubItem.ClipStopTime"/>
    /// </remarks>
    public float timeUntilEnd
    {
        get
        {
            return clipLength - audioTime;
        }
    }

    /// <summary>
    /// Gets or sets the DSP time at which the audio is scheduled to play. 
    /// </summary>
     /// <returns>
    /// Returns -1 if no audio is scheduled.
    /// </returns>
    public double scheduledPlayingAtDspTime
    {
        get
        {
            return _playScheduledTimeDsp;
        }
        set
        {
#if UNITY_AUDIO_FEATURES_4_1
            _playScheduledTimeDsp = value;
            primaryAudioSource.SetScheduledStartTime( _playScheduledTimeDsp );
#endif
        }
    }

    /// <summary>
    /// Gets the length of the clip. 
    /// </summary>
    /// <remarks>
    /// Is effected by <see cref="AudioSubItem.ClipStopTime"/> and <see cref="AudioSubItem.ClipStartTime"/>
    /// </remarks>
    public float clipLength
    {
        get
        {
            if ( _stopClipAtTime > 0 )
            {
                return _stopClipAtTime - _startClipAtTime;
            }
            else
            {
                if ( primaryAudioSource.clip != null )
                {
                    return primaryAudioSource.clip.length - _startClipAtTime;
                }
                else return 0;
            }
        }
    }

    /// <summary>
    /// Sets or gets the current audio time relative to <see cref="AudioSubItem.ClipStartTime"/> 
    /// </summary>
    public float audioTime
    {
        get
        {
            return primaryAudioSource.time - _startClipAtTime;
        }
        set
        {
            primaryAudioSource.time = value + _startClipAtTime;
        }
    }

    /// <summary>
    /// return <c>true</c> if the audio is currently fading out
    /// </summary>
    /// <remarks>
    /// If the fade-out is complete then <see cref="isFadingOut"/> return <c>false</c> and <see cref="isFadeOutComplete"/> returns <c>true</c>
    /// </remarks>
    public bool isFadingOut
    {
        get
        {
            return _primaryFader.isFadingOut;
        }
    }

    /// <summary>
    /// return <c>true</c> if the audio has completely faded out
    /// </summary>
    public bool isFadeOutComplete
    {
        get
        {
            return _primaryFader.isFadingOutComplete;
        }
    }

    /// <summary>
    /// return <c>true</c> if the audio is currently fading out or is scheduled to fade out
    /// </summary>
    /// <remarks>
    /// <see cref="isFadingOutOrScheduled"/> returns <c>true</c> even if the fade out is complete.
    /// </remarks>
    public bool isFadingOutOrScheduled
    {
        get
        {
            return _primaryFader.isFadingOutOrScheduled;
        }
    }

    /// <summary>
    /// return <c>true</c> if the audio is currently fading in
    /// </summary>
    public bool isFadingIn
    {
        get
        {
            return _primaryFader.isFadingIn;
        }
    }

    /// <summary>
    /// Sets or gets the audio pitch.
    /// </summary>
    public float pitch
    {
        get
        {
            return primaryAudioSource.pitch;
        }
        set
        {
            primaryAudioSource.pitch = value;
        }
    }

    /// <summary>
    /// Sets or gets the audio pan.
    /// </summary>
    public float pan
    {
        get
        {
#if UNITY_5_OR_NEWER
            return primaryAudioSource.panStereo;
#else
           return primaryAudioSource.pan;
#endif
        }
        set
        {
#if UNITY_5_OR_NEWER
            primaryAudioSource.panStereo = value;
#else
            primaryAudioSource.pan = value;
#endif
        }
    }
    
    /// <summary>
    /// Returns the high precision local time of this audio object
    /// </summary>
    /// <remarks>
    /// The local time is paused when the audio object is paused.
    /// </remarks>
    public double audioObjectTime
    {
        get 
        {
            return _audioObjectTime;
        }
    }

    /// <summary>
    /// If enabled, the audio will stop plaing if a fadeout is finished.
    /// </summary>
    /// <remarks>
    /// Enabled by default.
    /// </remarks>
    public bool stopAfterFadeOut
    {
        get { return _stopAfterFadeoutUserSetting; }
        set { _stopAfterFadeoutUserSetting = value; }
    }
    
	/// <summary>
	/// Fades-in a playing audio.
	/// </summary>
	/// <param name="fadeInTime">The fade time in seconds.</param>
	public void FadeIn( float fadeInTime )
	{
        if ( _playStartTimeLocal > 0 )
        {
            double timeUntilStart = _playStartTimeLocal - audioObjectTime;
            if ( timeUntilStart > 0 )
            {
                _primaryFader.FadeIn( fadeInTime, _playStartTimeLocal );
                _UpdateFadeVolume();
                return;
            }
        }
       
        _primaryFader.FadeIn( fadeInTime, audioObjectTime, !_shouldStopIfPrimaryFadedOut );
        _UpdateFadeVolume();
	}

    /// <summary>
    /// Plays the audio clip at the specified high precision DSP time (see the Unity AudioSettings.dspTime documentation)
    /// </summary>
    /// <param name="dspTime">The high precision DSP time.</param>
    public void PlayScheduled( double dspTime )
    {
#if UNITY_AUDIO_FEATURES_4_1
        _PlayScheduled( dspTime );
#else
        Debug.LogError( "PlayScheduled requires Unity v4.1 or newer" );
#endif
    }

    /// <summary>
    /// Plays the specified audio after the current has finished playing
    /// </summary>
    /// <param name="audioID">The audioID to be played.</param>
    /// <param name="deltaDspTime">Optional delta time (high precsion DSP time), Default = 0.</param>
    /// <param name="volume">The volume [Default = 0].</param>
    /// <param name="startTime">The start time [Default = 0].</param>
    public void PlayAfter( string audioID, double deltaDspTime = 0, float volume = 1.0f, float startTime = 0 )
    {
        AudioController.PlayAfter( audioID, this, deltaDspTime, volume, startTime );
    }

    /// <summary>
    /// Plays the specified audio.
    /// </summary>
    /// <param name="audioID">The audioID to be played.</param>
    /// <param name="delay">Start playing after this amount of seconds [Default = 0].</param>
    /// <param name="volume">The volume [Default = 0].</param>
    /// <param name="startTime">The start time [Default = 0].</param>
    /// <remarks>
    /// Does not stop the secondary audio source (if playing). See <see cref="SwitchAudioSources"/>. 
    /// </remarks>
    public void PlayNow( string audioID, float delay = 0, float volume = 1.0f, float startTime = 0 )
    {
        var newAudioItem = AudioController.GetAudioItem( audioID );

        if ( newAudioItem == null )
        {
            Debug.LogWarning( "Audio item with name '" + audioID + "' does not exist" );
            return;
        }

        _audioController.PlayAudioItem( newAudioItem, volume, transform.position, transform.parent, delay, startTime, false, this );
    }

	/// <summary>
	/// Plays the audio clip with the specified delay.
	/// </summary>
	/// <param name="delay">The delay [Default=0].</param>
	public void Play( float delay  = 0)
	{
		_PlayDelayed( delay );
	}

	/// <summary>
	/// Stops playing this instance.
	/// </summary>
    /// <remarks>
    /// Uses fade out as specified in the corresponding <see cref="AudioSubItem.FadeOut"/>.
    /// </remarks>
	public void Stop()
	{
		Stop( -1.0f );
	}

	/// <summary>
	/// Stops a playing audio with fade-out.
	/// </summary>
    /// <param name="fadeOutLength">The fade time in seconds.
    /// If a negative value is specified, the fade out as specified in the corresponding <see cref="AudioSubItem.FadeOut"/> is used</param>
    public void Stop( float fadeOutLength )
	{
        Stop( fadeOutLength, 0 );
	}

    /// <summary>
    /// Stops a playing audio with fade-out at a specified time.
    /// </summary>
    /// <remarks>
    /// If the audio is already fading out the requested fade-out is combined with the existing one.
    /// </remarks>
    /// <param name="fadeOutLength">The fade time in seconds. If a negative value is specified, the fade out as specified in the corresponding <see cref="AudioSubItem.FadeOut"/> is used</param>
    /// <param name="startToFadeTime">Fade out starts after <c>startToFadeTime</c> seconds have passed</param>
    public void Stop( float fadeOutLength, float startToFadeTime )
    {
        if ( IsPaused( false ) ) // if already paused, stop immediately
        {
            fadeOutLength = 0;
            startToFadeTime = 0;
        }

        if ( startToFadeTime > 0 )
        {
            StartCoroutine( _WaitForSecondsThenStop( startToFadeTime, fadeOutLength ) );
            return;
        }
        _stopRequested = true;

        if ( fadeOutLength < 0 )
        {
            if ( subItem != null )
            {
                fadeOutLength = subItem.FadeOut;
            }
            else
                fadeOutLength = 0;
        }
        if ( fadeOutLength == 0 && startToFadeTime == 0)
        {
            _Stop();
            return;
        }

        FadeOut( fadeOutLength, startToFadeTime );
        if ( IsSecondaryPlaying() )
        {
            SwitchAudioSources();
            FadeOut( fadeOutLength, startToFadeTime );
            SwitchAudioSources();
        }
    }

    /// <summary>
    /// Finishes a playing sequence, depending on the AudioItem's loop mode <seealso cref="AudioItem.LoopMode"/>:
    /// <list type="bullet">
    /// <item>
    /// <see cref="AudioItem.LoopMode.LoopSequence"/>: The sequence will stop after the current item has finished playing
    /// </item>
    /// <item>
    /// <see cref="AudioItem.LoopMode.PlaySequenceAndLoopLast"/>: The sequence will stop after the current item has finished playing. If the sequence is during the looping part the looping will stop after the current loop reached its end.
    /// </item>
    /// <item>
    /// <see cref="AudioItem.LoopMode.IntroLoopOutroSequence"/>: The sequence will stop after the current item has finished playing. If the sequence is during the looping part the outro will be played and the sequence will stop afterwards.
    /// </item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Has no effect if the audio is not in a sequence loop mode.
    /// </remarks>
    public void FinishSequence()
    {
        if ( !_finishSequence )
        {
            var audItem = audioItem;
            if ( audItem != null )
            {
                switch ( audItem.Loop )
                {
                case AudioItem.LoopMode.LoopSequence:
                case (AudioItem.LoopMode)3: // deprecated gapless loop mode
                     _finishSequence = true;
                    break;
                case AudioItem.LoopMode.PlaySequenceAndLoopLast:
                case AudioItem.LoopMode.IntroLoopOutroSequence:
                    primaryAudioSource.loop = false;
                     _finishSequence = true;
                    break;
                default: // not applicable for none sequence loop modes
                    break;
                }
            }
        }
    }

    private IEnumerator _WaitForSecondsThenStop( float startToFadeTime, float fadeOutLength )
    {
        yield return new WaitForSeconds( startToFadeTime );
        if ( !_IsInactive )
        {
            Stop( fadeOutLength );
        }
    }

    /// <summary>
    /// Starts a fade-out. If the AudioItem mode is is a sequence, the next sub-item will continue to play
    /// after the this sub-item is completely faded out. 
    /// </summary>
    /// <remarks>
    /// If the audio is already fading out the requested fade-out is combined with the existing one.
    /// This function only fades-out the primary audio source.
    /// </remarks>
    /// <param name="fadeOutLength">The fade time in seconds. If a negative value is specified, the fade out as specified in the corresponding <see cref="AudioSubItem.FadeOut"/> is used</param>
    public void FadeOut( float fadeOutLength )
    {
        FadeOut( fadeOutLength, 0 );
    }

    /// <summary>
    /// Starts a fade-out at a specified time. If the AudioItem mode is is a sequence, the next sub-item will continue to play
    /// after the this sub-item is completely faded out. 
    /// </summary>
    /// <remarks>
    /// If the audio is already fading out the requested fade-out is combined with the existing one.
    /// This function only fades-out the primary audio source.
    /// </remarks>
    /// <param name="fadeOutLength">The fade time in seconds. If a negative value is specified, the fade out as specified in the corresponding <see cref="AudioSubItem.FadeOut"/> is used</param>
    /// <param name="startToFadeTime">Fade out starts after <c>startToFadeTime</c> seconds have passed</param>
    public void FadeOut( float fadeOutLength, float startToFadeTime )
    {
        if ( fadeOutLength < 0 )
        {
            if ( subItem != null )
            {
                fadeOutLength = subItem.FadeOut;
            }
            else
                fadeOutLength = 0;
        }

        if ( fadeOutLength > 0 || startToFadeTime > 0 )
        {
            _primaryFader.FadeOut( fadeOutLength, startToFadeTime );
        }
        else
        {
            if ( fadeOutLength == 0 )
            {
                if ( _shouldStopIfPrimaryFadedOut )
                {
                    _Stop();
                }
                else
                {
                    _primaryFader.FadeOut( 0, startToFadeTime );
                }
            }
        }
    }

    /// <summary>
	/// Pauses the audio clip.
	/// </summary>
    public void Pause()
    {
        Pause( 0 );
    }

    private int _pauseCoroutineCounter = 0;

	/// <summary>
	/// Pauses the audio clip with a fade-out.
	/// </summary>
    /// <param name="fadeOutTime">The fade-out time in seconds.</param>
    public void Pause( float fadeOutTime )
	{
        if ( _paused )
        {
            return;
        }

        _paused = true;
        if ( fadeOutTime > 0 )
        {
            _pauseWithFadeOutRequested = true;
            FadeOut( fadeOutTime );
            StartCoroutine( _WaitThenPause( fadeOutTime, ++_pauseCoroutineCounter ) );
            return;
        }
        _PauseNow();
	}

    private void _PauseNow()
    {

#if UNITY_AUDIO_FEATURES_4_1
        if ( _playScheduledTimeDsp > 0 )
        {
            _dspTimeRemainingAtPause = _playScheduledTimeDsp - AudioSettings.dspTime;
            scheduledPlayingAtDspTime = 9e9; // postpone play, re-schedule during unpause
        }
#endif

        _PauseAudioSources();
        if ( _pauseWithFadeOutRequested )
        {
            _pauseWithFadeOutRequested = false;
            _primaryFader.Set0();
        }
    }

    /// <summary>
    /// Unpauses the audio clip.
    /// </summary>
    public void Unpause()
    {
        Unpause( 0 );
    }

    /// <summary>
    /// Unpauses the audio clip with a fade-in.
    /// </summary>
    /// <param name="fadeInTime">The fade-in time in seconds.</param>
    public void Unpause( float fadeInTime )
    {
        if ( !_paused )
        {
            return;
        }

        _UnpauseNow();

        if ( fadeInTime > 0 )
        {
            FadeIn( fadeInTime );
        }

        _pauseWithFadeOutRequested = false;
    }

    private void _UnpauseNow()
    {
        _paused = false;

        if ( secondaryAudioSource && _secondaryAudioSourcePaused )
        {
            secondaryAudioSource.Play();
        }

        if ( _dspTimeRemainingAtPause > 0 && _primaryAudioSourcePaused )
        {
#if UNITY_AUDIO_FEATURES_4_1
            double dspTime = AudioSettings.dspTime + _dspTimeRemainingAtPause;
            _playStartTimeSystem = AudioController.systemTime + _dspTimeRemainingAtPause;
            primaryAudioSource.PlayScheduled( dspTime );
            scheduledPlayingAtDspTime = dspTime;
            _dspTimeRemainingAtPause = -1;
#endif
        }
        else
        {
            if ( _primaryAudioSourcePaused )
            {
                primaryAudioSource.Play();
            }
        }
    }

    private IEnumerator _WaitThenPause( float waitTime, int counter )
    {
        yield return new WaitForSeconds( waitTime );
        if ( _pauseWithFadeOutRequested && counter == _pauseCoroutineCounter )
        {
            _PauseNow();
        }
    }    

    private void _PauseAudioSources()
    {
        if ( primaryAudioSource.isPlaying )
        {
            _primaryAudioSourcePaused = true;
            primaryAudioSource.Pause();
        }
        else
            _primaryAudioSourcePaused = false;

        if ( secondaryAudioSource && secondaryAudioSource.isPlaying )
        {
            _secondaryAudioSourcePaused = true;
            secondaryAudioSource.Pause();
        }
        else
            _secondaryAudioSourcePaused = false;
    }

	/// <summary>
	/// Determines whether the audio clip is paused.
	/// </summary>
    /// <param name="returnTrueIfStillFadingOut">If <c>true</c> the fuction will return <c>true</c> 
    /// even if the item is still fading out due to a Pause request with a fade-out.</param>
	/// <returns>
	///   <c>true</c> if paused; otherwise, <c>false</c>.
	/// </returns>
    public bool IsPaused( bool returnTrueIfStillFadingOut = true )
	{
        if ( !returnTrueIfStillFadingOut )
        {
            return !_pauseWithFadeOutRequested && _paused;
        }
		return _paused;
	}

	/// <summary>
	/// Determines if either the primary or the secondary audio clip is playing.
	/// </summary>
	/// <returns>
	///   <c>true</c> if the audio clip is playing; otherwise, <c>false</c>.
	/// </returns>
	public bool IsPlaying()
	{
        return IsPrimaryPlaying() || IsSecondaryPlaying();
	}

    /// <summary>
    /// Determines if the primary audio clip is playing.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if the audio clip is playing; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPrimaryPlaying()
    {
        return primaryAudioSource.isPlaying;
    }

    /// <summary>
    /// Determines if the secondary audio clip is playing.
    /// </summary>
    /// <returns>
    ///   <c>true</c> if the audio clip is playing; otherwise, <c>false</c>.
    /// </returns>
    public bool IsSecondaryPlaying()
    {
        return ( secondaryAudioSource != null && secondaryAudioSource.isPlaying );
    }

    /// <summary>
    /// returns the primary AudioSource
    /// </summary>
    /// <remarks>
    /// some features like "loop sequence" require an additional AudioSource.
    /// Functions like Stop(), FadeIn(), etc. always act on the primary audio source.
    /// </remarks>
    public AudioSource primaryAudioSource
    {
        get
        {
            return _audioSource1;
        }
    }

    /// <summary>
    /// returns the secondary AudioSource
    /// </summary>
    /// <remarks>
    /// some features like "loop sequence" require an additional AudioSource.
    /// Functions like Stop(), FadeIn(), etc. always act on the primary audio source.
    /// </remarks>
    public AudioSource secondaryAudioSource
    {
        get
        {
            return _audioSource2;
        }
    }

    /// <summary>
    /// Switches the primary and secondary audio source
    /// </summary>
    /// <remarks>
    /// This way a single AudioObject can play two audio clips at the same time. 
    /// You can use it e.g. to cross fade between two audios using the same AudioObject.
    /// </remarks>
    /// <example>
    /// playingAudioObject.FadeOut( 3 );
    /// playingAudioObject.SwitchAudioSources();
    /// playingAudioObject.PlayNow( "otherAudioID" );
    /// playingAudioObject.FadeIn( 3 );
    /// </example>
    public void SwitchAudioSources()
    {
        if ( _audioSource2 == null )
        {
            _CreateSecondAudioSource();
        }

        _SwitchValues( ref _audioSource1, ref _audioSource2 );
        _SwitchValues( ref _primaryFader, ref _secondaryFader );
        _SwitchValues( ref _subItemPrimary, ref _subItemSecondary );
        _SwitchValues( ref _volumeFromPrimaryFade, ref _volumeFromSecondaryFade );
    }

    void _SwitchValues<T>( ref T v1, ref T v2 )
    {
        T tmp = v1;
        v1 = v2;
        v2 = tmp;
    }

	// **************************************************************************************************/
	//          private / protected functions and properties
	// **************************************************************************************************/

	internal float _volumeFromCategory
	{
		get
		{
			if ( category != null )
			{
				return category.VolumeTotal;
			}
			return 1.0f;
		}
	}

	internal float _volumeWithCategory
	{
		get
		{
			return _volumeFromCategory * _volumeExcludingCategory;
		}
	}

	internal float _volumeExcludingCategory = 1;  // untransformed volume
    private float _volumeFromPrimaryFade = 1;
    private float _volumeFromSecondaryFade = 1;
    internal float _volumeFromScriptCall = 1;

	bool _paused = false;
	bool _applicationPaused = false;

    AudioFader _primaryFader;
    AudioFader _secondaryFader;

    double _playTime = -1;
    double _playStartTimeLocal = -1; // delayed or scheduled time
    double _playStartTimeSystem = -1; // delayed or scheduled time
    double _playScheduledTimeDsp = -1;

    double _audioObjectTime = 0;

    bool _IsInactive = true; // remains true until first play call
    bool _stopRequested = false;
    bool _finishSequence = false;
    int _loopSequenceCount = 0;
    private bool _stopAfterFadeoutUserSetting;
    private bool _pauseWithFadeOutRequested;
    private double _dspTimeRemainingAtPause;

    AudioController _audioController;

    float _stopClipAtTime
    {
        get
        {
            return subItem != null ? subItem.ClipStopTime : 0;
        }
    }
    float _startClipAtTime
    {
        get
        {
            return subItem != null ? subItem.ClipStartTime : 0;
        }
    }
    internal bool _isCurrentPlaylistTrack = false;

    internal float _audioSource_MinDistance_Saved = 1;
    internal float _audioSource_MaxDistance_Saved = 500;

    internal int _lastChosenSubItemIndex = -1;


    private AudioSource _audioSource1; // for performance reasons use cached reference
    private AudioSource _audioSource2;

    bool _primaryAudioSourcePaused = false;
    bool _secondaryAudioSourcePaused = false;

	/// <summary>
	/// Initializes this instance.
	/// </summary>
	protected override void Awake()
	{
		base.Awake();

        if ( _primaryFader == null )
        {
            _primaryFader = new AudioFader();
        }
        else
            _primaryFader.Set0();

        if ( _secondaryFader == null )
        {
            _secondaryFader = new AudioFader();
        }
        else
            _secondaryFader.Set0();

        if ( _audioSource1 == null )
        {
            _audioSource1 = GetComponent<AudioSource>();
        }
        else
        {
            if ( _audioSource2 && _audioSource1 != GetComponent<AudioSource>() ) // make sure that by default the primary audio source is _audioSource1 
            {
                SwitchAudioSources();
            }
        }

        _Set0();

        _audioController = AudioController.Instance;
		//Debug.Log( "AudioObject.Awake" );

#if OUYA_PAUSE_SYSTEM
        OuyaSDK.registerPauseListener(this);
        OuyaSDK.registerResumeListener(this);
#endif
    }

    private void _CreateSecondAudioSource()
    {
       _audioSource2 = gameObject.AddComponent<AudioSource>();
       _audioSource2.rolloffMode = _audioSource1.rolloffMode;
       _audioSource2.minDistance = _audioSource1.minDistance;
       _audioSource2.maxDistance = _audioSource1.maxDistance;
       _audioSource2.dopplerLevel = _audioSource1.dopplerLevel;
       _audioSource2.spread = _audioSource1.spread;
#if UNITY_5_OR_NEWER
       _audioSource2.spatialBlend = _audioSource1.spatialBlend;
#else
       _audioSource2.panLevel = _audioSource1.panLevel;
#endif
       _audioSource2.velocityUpdateMode = _audioSource1.velocityUpdateMode;
       _audioSource2.ignoreListenerVolume = _audioSource1.ignoreListenerVolume;
       _audioSource2.playOnAwake = false;

#if !UNITY_FLASH
       _audioSource2.priority = _audioSource1.priority;
       _audioSource2.bypassEffects = _audioSource1.bypassEffects;
#endif


#if UNITY_AUDIO_FEATURES_4_1
       _audioSource2.ignoreListenerPause = _audioSource1.ignoreListenerPause;
#endif
    }

    private void _Set0()
    {
        _SetReferences0();

        _audioObjectTime = 0;

        primaryAudioSource.playOnAwake = false;
        if ( secondaryAudioSource )
        {
            secondaryAudioSource.playOnAwake = false;
        }
        _lastChosenSubItemIndex = -1;
        _primaryFader.Set0();
        _secondaryFader.Set0();
		_playTime = -1;
        _playStartTimeLocal = -1;
        _playStartTimeSystem = -1;
        _playScheduledTimeDsp = -1;
        _volumeFromPrimaryFade = 1;
        _volumeFromSecondaryFade = 1;
        _volumeFromScriptCall = 1;
		_IsInactive = true;
        _stopRequested = false;
        _finishSequence = false;
        _volumeExcludingCategory = 1;
		_paused = false;
		_applicationPaused = false;
        _isCurrentPlaylistTrack = false;
        _loopSequenceCount = 0;
        _stopAfterFadeoutUserSetting = true;
        _pauseWithFadeOutRequested = false;
        _dspTimeRemainingAtPause = -1;
        _primaryAudioSourcePaused = false;
        _secondaryAudioSourcePaused = false;
	}

    private void _SetReferences0()
    {
        _audioController = null;
        primaryAudioSource.clip = null;

        if ( secondaryAudioSource != null )
        {
            secondaryAudioSource.playOnAwake = false;
            secondaryAudioSource.clip = null;
        }
        subItem = null;
        category = null;
        _completelyPlayedDelegate = null;
    }

#if UNITY_AUDIO_FEATURES_4_1
    private void _PlayScheduled( double dspTime )
    {
        if ( !primaryAudioSource.clip )
        {
            Debug.LogError( "audio.clip == null in " + gameObject.name );
            return;
        }

        _playScheduledTimeDsp = dspTime;
        double deltaDsp = dspTime - AudioSettings.dspTime;
        _playStartTimeLocal = deltaDsp + audioObjectTime;
        _playStartTimeSystem = deltaDsp + AudioController.systemTime;

        primaryAudioSource.PlayScheduled( dspTime );

        _OnPlay();
    }
#endif

	private void _PlayDelayed( float delay )
	{
		//Debug.Log( "_PlayInitial:" + audioID + " sub:" + _subItemID );

        if ( !primaryAudioSource.clip )
        {
            Debug.LogError( "audio.clip == null in " + gameObject.name );
            return;
        }

#if !UNITY_AUDIO_FEATURES_4_1
        ulong d = (ulong)( ( 44100.0f / primaryAudioSource.clip.frequency ) * delay * primaryAudioSource.clip.frequency ); // http://unity3d.com/support/documentation/ScriptReference/AudioSource.Play.html
        primaryAudioSource.Play( d );
#else
        primaryAudioSource.PlayDelayed( delay );
#endif

        //Debug.Log( "Play Clip:" + _audioSource.clip.name + " S/N:"+ GetComponent<PoolableObject>().GetSerialNumber());

        _playScheduledTimeDsp = -1;
        _playStartTimeLocal = audioObjectTime + delay;
        _playStartTimeSystem = AudioController.systemTime + delay;

        _OnPlay();
	}

    private void _OnPlay()
    {
        _IsInactive = false;
        _playTime = audioObjectTime;
        _paused = false;
        _primaryAudioSourcePaused = false;
        _secondaryAudioSourcePaused = false;
        _primaryFader.Set0();
    }

	private void _Stop()
	{
#if UNITY_EDITOR && !AUDIO_TOOLKIT_DEMO

        if ( primaryAudioSource != null && primaryAudioSource.isPlaying )
        {
            var logData = new AudioLog.LogData_Stop();
            logData.audioID = audioID;
            if ( category != null )
            {
                logData.category = category.Name;
            }
            logData.clipName = primaryAudioSource.name;
            logData.position = transform.position;
            if ( !object.ReferenceEquals( transform.parent, null ) )
            {
                logData.parentObject = transform.parent.name;
            }
            AudioLog.Log( logData );
        }

#endif
        _primaryFader.Set0();
        _secondaryFader.Set0();
		//Debug.Log( "Stop " );
		primaryAudioSource.Stop();
        if ( secondaryAudioSource )
        {
            secondaryAudioSource.Stop();
        }
        _paused = false;
        _primaryAudioSourcePaused = false;
        _secondaryAudioSourcePaused = false;
    }

	private void Update()
	{
		if ( _IsInactive ) return;

        if ( !IsPaused( false ) )
        {
            _audioObjectTime += AudioController.systemDeltaTime;
            _primaryFader.time = _audioObjectTime;
            _secondaryFader.time = _audioObjectTime;
        }
        else
        {
            //TODO: postpone scheduled play?
        }

        if ( _playScheduledTimeDsp > 0 )
        {
            if ( _audioObjectTime > _playStartTimeLocal ) // is scheduled audio playing already
            {
                _playScheduledTimeDsp = -1;
            }
        }

		if ( !_paused && !_applicationPaused )
		{
            bool primaryPlaying = IsPrimaryPlaying();
            bool secondaryPlaying = IsSecondaryPlaying();

            if ( primaryPlaying == false && secondaryPlaying == false )
            {
                bool destroy = true;

                if ( !_stopRequested )
                {
                    if ( destroy && completelyPlayedDelegate != null )
                    {
                        completelyPlayedDelegate( this );
                        destroy = !IsPlaying();
                    }
                }

                if ( _isCurrentPlaylistTrack )
                {
                    if ( AudioController.DoesInstanceExist() ) AudioController.Instance._NotifyPlaylistTrackCompleteleyPlayed( this );
                }

                if ( destroy )
                {
                    DestroyAudioObject();
                    return;
                }
            }
            else
            {
                if ( !_stopRequested && _IsAudioLoopSequenceMode() && !IsSecondaryPlaying() )
                {
                    if ( timeUntilEnd < 1.0f + Mathf.Max( 0, audioItem.loopSequenceOverlap ) ) // leave the audio engine enough time to prepare playing the next audio play
                    {
                        if ( _playScheduledTimeDsp < 0 )
                        {
                            _ScheduleNextInLoopSequence();
                        }
                    }
                }

                if( !primaryAudioSource.loop )
                {
                    if ( _isCurrentPlaylistTrack &&  
                         _audioController && _audioController.crossfadePlaylist &&
                         audioTime > clipLength - _audioController.musicCrossFadeTime_Out )
                    {
                        if ( AudioController.DoesInstanceExist() )
                        {
                            AudioController.Instance._NotifyPlaylistTrackCompleteleyPlayed( this ); // don't use _audioController as it might be additionalAudioController
                        }
                    }
                    else
                    {
                        _StartFadeOutIfNecessary();

                        if ( secondaryPlaying )
                        {
                            SwitchAudioSources();
                            _StartFadeOutIfNecessary();
                            SwitchAudioSources();
                        }
                    }
                }
            }
		}

        _UpdateFadeVolume();
	}

    private void _StartFadeOutIfNecessary()
    {
        if ( subItem == null )
        {
            // can happen if switching between primary/secondary audio
            Debug.LogWarning( "subItem == null" );
            return;
        }
        float at = audioTime;
        if ( !isFadingOutOrScheduled && subItem.FadeOut > 0 && at > clipLength - subItem.FadeOut )
        {
            //Debug.Log( "FadeOut with t=" + at );
            FadeOut( subItem.FadeOut );
        }
    }

    private bool _IsAudioLoopSequenceMode()
    {
        var audItem = audioItem;
        if ( audItem != null )
        {
            switch ( audItem.Loop )
            {
            case AudioItem.LoopMode.LoopSequence:
            case (AudioItem.LoopMode)3: // deprecated gapless loop mode
                return true;
            case AudioItem.LoopMode.PlaySequenceAndLoopLast:
            case AudioItem.LoopMode.IntroLoopOutroSequence:
                return primaryAudioSource.loop == false;
            }
        }
        return false;
    }

    private bool _ScheduleNextInLoopSequence()
    {
#if UNITY_AUDIO_FEATURES_4_1

        int itemCount;
        if ( audioItem.loopSequenceCount > 0 )
        {
            itemCount = audioItem.loopSequenceCount;
        }
        else
            itemCount = audioItem.subItems.Length;

        if ( _finishSequence )
        {
            if ( audioItem.Loop == AudioItem.LoopMode.IntroLoopOutroSequence )
            {
                if ( _loopSequenceCount <= itemCount - 3) // is before loop
                {
                    return false;
                }

                if ( _loopSequenceCount >= itemCount - 1 ) // is during outro
                {
                    return false;
                }
                // we are in the loop track, so play outro next
            }
            else
            {
                return false;
            }
        }

        if ( audioItem.loopSequenceCount > 0 )
        {
            if ( audioItem.loopSequenceCount <= _loopSequenceCount + 1 )
            {
                return false;
            }
        }

        //Debug.Log( "_ScheduleNextInLoopSequence: " + _lastChosenSubItemIndex + " dt: " + timeUntilEnd );

        double dspTime = AudioSettings.dspTime + timeUntilEnd + _GetRandomLoopSequenceDelay( audioItem );

        var audioItemBeforeSwitch = audioItem;

        SwitchAudioSources();
        _audioController.PlayAudioItem( audioItemBeforeSwitch, _volumeFromScriptCall, Vector3.zero, null, 0, 0, false, this, dspTime );
        _loopSequenceCount++;

        if ( audioItem.Loop == AudioItem.LoopMode.PlaySequenceAndLoopLast ||
             audioItem.Loop == AudioItem.LoopMode.IntroLoopOutroSequence )
        {
            if ( audioItem.Loop == AudioItem.LoopMode.IntroLoopOutroSequence )
            {
                if ( !_finishSequence && itemCount <= _loopSequenceCount + 2 )
                {
                    primaryAudioSource.loop = true;
                }
            }
            else // AudioItem.LoopMode.PlaySequenceAndLoopLast
            {
                if ( itemCount <= _loopSequenceCount + 1 )
                {
                    primaryAudioSource.loop = true;
                }
            }
        }
        return true;
#else
        _loopSequenceCount++; // removes compiler warning
        return false;
#endif
    }

    private void _UpdateFadeVolume()
    {
        bool finishedFadeOut;

        // primary AudioSource
        float fadeVolumePrimary = _EqualizePowerForCrossfading( _primaryFader.Get( out finishedFadeOut ) );

        if ( finishedFadeOut )
        {
            if ( _stopRequested )
            {
                _Stop();
                return;
            }

            if ( !_IsAudioLoopSequenceMode() )
            {
                if ( _shouldStopIfPrimaryFadedOut )
                {
                    _Stop();
                }
                return;
            }  

        }

        if ( fadeVolumePrimary != _volumeFromPrimaryFade )
        {
            _volumeFromPrimaryFade = fadeVolumePrimary;
            _ApplyVolumePrimary();
        }

        // secondary AudioSource
        if ( _audioSource2 != null )
        {
            float fadeVolumeSecondary = _EqualizePowerForCrossfading( _secondaryFader.Get( out finishedFadeOut ) );

            if ( finishedFadeOut )
            {
                _audioSource2.Stop();
            }
            else
            {
                if ( fadeVolumeSecondary != _volumeFromSecondaryFade )
                {
                    _volumeFromSecondaryFade = fadeVolumeSecondary;
                    _ApplyVolumeSecondary();
                }
            }
        }

    }

    private float _EqualizePowerForCrossfading( float v )
    {
        if ( !_audioController.EqualPowerCrossfade ) return v;

        return InverseTransformVolume( Mathf.Sin( v * Mathf.PI * 0.5f ) );
    }

    private bool _shouldStopIfPrimaryFadedOut
    {
        get
        {
            return _stopAfterFadeoutUserSetting && !_pauseWithFadeOutRequested; // pause requested with fadeout
        }
    }

#if OUYA_PAUSE_SYSTEM
  public void OuyaOnPause() {
    SetApplicationPaused(true);
  }

  public void OuyaOnResume() {
    SetApplicationPaused(false);
  }
#else
	private void OnApplicationPause( bool b )
	{
    SetApplicationPaused(b);
	}
#endif

  private void SetApplicationPaused(bool isPaused) {
    _applicationPaused = isPaused;
  }
	
	/// <summary>
    /// Destroys the audio object (using <see cref="ObjectPoolController"/> if pooling is enabled)
	/// </summary>
	public void DestroyAudioObject()
	{
#if UNITY_EDITOR && !AUDIO_TOOLKIT_DEMO

        if( !_IsInactive )
        {
            var logData = new AudioLog.LogData_Destroy();
            logData.audioID = audioID;
            if ( category != null )
            {
                logData.category = category.Name;
            }
            if ( primaryAudioSource != null )
            {
                logData.clipName = primaryAudioSource.name;
            }
            logData.position = transform.position;
            if ( !object.ReferenceEquals( transform.parent, null ) )
            {
                logData.parentObject = transform.parent.name;
            }
            //Debug.Log( "DestroyAudioObject " + audioID );
            AudioLog.Log( logData );
        }

#endif
        if ( IsPlaying() )
        {
            _Stop();
        }

#if OUYA_PAUSE_SYSTEM
        OuyaSDK.unregisterPauseListener(this);
        OuyaSDK.unregisterResumeListener(this);
#endif

		//Debug.Log( "Destroy:" + _audioSource.clip.name );
#if AUDIO_TOOLKIT_DEMO
		GameObject.Destroy( gameObject );
#else
		ObjectPoolController.Destroy( gameObject );
#endif
		_IsInactive = true;
	}

    const float VOLUME_TRANSFORM_POWER = 1.6f;

    /// <summary>
    /// Transforms the volume to make it perceptually more intuitive to scale and cross-fade.
    /// </summary>
    /// <param name="volume">The volume to transform.</param>
    /// <returns>
    /// The transformed volume <c> = Pow( volume, 1.6 ) </c>
    /// </returns>
	static public float TransformVolume( float volume )
	{
        return Mathf.Pow( volume, VOLUME_TRANSFORM_POWER ); 
	}

    /// <summary>
    ///Inverse volume transformation <see cref="TransformVolume"/>
    /// </summary>
    /// <param name="volume">The volume to inverse-transform.</param>
    /// <returns>
    /// The inverse-transformed volume
    /// </returns>
    static public float InverseTransformVolume( float volume )
    {
        return Mathf.Pow( volume, 1.0f / VOLUME_TRANSFORM_POWER );
    }

    /// <summary>
    /// Transforms the pitch from semitones to a multiplicative factor
    /// </summary>
    /// <param name="pitchSemiTones">The pitch shift in semitones to transform.</param>
    /// <returns>
    /// The transformed pitch <c> = Pow( 2, pitch / 12 ) </c>
    /// </returns>
    static public float TransformPitch( float pitchSemiTones )
	{
        return Mathf.Pow( 2, pitchSemiTones / 12.0f );
	}


    /// <summary>
    /// Inverse pitch transformation: <see cref="TransformPitch"/>
    /// </summary>
    /// <param name="pitch">The transformed pitch</param>
    /// <returns>The pitch shift in semitones</returns>
    static public float InverseTransformPitch( float pitch )
    {
        return ( Mathf.Log( pitch ) / Mathf.Log( 2.0f ) ) * 12.0f;
    }

    internal void _ApplyVolumeBoth()
    {
        var volTot = volumeTotalWithoutFade;
        float volumeToSet = TransformVolume( volTot * _volumeFromPrimaryFade );

        primaryAudioSource.volume = volumeToSet;

        if ( secondaryAudioSource )
        {
            volumeToSet = TransformVolume( volTot * _volumeFromSecondaryFade );
            secondaryAudioSource.volume = volumeToSet;
        }
    }

    internal void _ApplyVolumePrimary()
    {
        float volumeToSet = TransformVolume( volumeTotalWithoutFade * _volumeFromPrimaryFade );
        primaryAudioSource.volume = volumeToSet;
    }

    internal void _ApplyVolumeSecondary()
    {
        if ( secondaryAudioSource )
        {
            float volumeToSet = TransformVolume( volumeTotalWithoutFade * _volumeFromSecondaryFade );
            secondaryAudioSource.volume = volumeToSet;
        }
    }

    /// <summary>
    /// If pooling is enabled <c>OnDestroy</c> is called every time the object is deactivated and moved to the pool.
    /// </summary>
	protected override void OnDestroy()
	{
        base.OnDestroy();
		//Debug.Log( "Destroy:" + audio.clip.name );

        var item = audioItem;

        if ( item != null )
        {
            if ( item.overrideAudioSourceSettings )  // restore _audioSource source settings
            {
                _RestoreAudioSourceSettings();
            }
        }

        // make sure assets can be freed by garbage collector
        _SetReferences0();
	}

    private void _RestoreAudioSourceSettings()
    {
        primaryAudioSource.minDistance = _audioSource_MinDistance_Saved;
        primaryAudioSource.maxDistance = _audioSource_MaxDistance_Saved;

        if ( secondaryAudioSource != null )
        {
            secondaryAudioSource.minDistance = _audioSource_MinDistance_Saved;
            secondaryAudioSource.maxDistance = _audioSource_MaxDistance_Saved;
        }
    }

    /// <summary>
    /// Checks if this <see cref="AudioObject"/> belongs to a specific category 
    /// </summary>
    /// <param name="categoryName">The name of the category</param>
    /// <returns><c>true</c> if the category with the specified name or one of its child categories 
    /// contains the <see cref="AudioItem"/> the <see cref="AudioObject"/> belongs to.</returns>
    public bool DoesBelongToCategory( string categoryName )
    {
        AudioCategory curCategory = category;

        while( curCategory != null )
        {
            if ( curCategory.Name == categoryName )
            {
                return true;
            }
            curCategory = curCategory.parentCategory;
        }
        return false;
    }

    float _GetRandomLoopSequenceDelay( AudioItem audioItem )
    {
        float retVal = -audioItem.loopSequenceOverlap;
        if( audioItem.loopSequenceRandomDelay > 0 )
        {
            retVal += Random.Range(0, audioItem.loopSequenceRandomDelay );
        }
        return retVal; 
    }

}


