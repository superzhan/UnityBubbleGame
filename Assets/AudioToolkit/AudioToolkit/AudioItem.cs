using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An audio category represents a set of AudioItems. Categories allow to change the volume of all containing audio items.
/// </summary>
[System.Serializable]
public class AudioCategory
{
    /// <summary>
    /// The name of category ( = <c>categoryID</c> )
    /// </summary>
    public string Name;

    /// <summary>
    /// The volume factor applied to all audio items in the category (NOT including a possible <see cref="parentCategory"/>)
    /// If you change the volume by script the change will be applied to all 
    /// playing audios immediately.
    /// </summary>
    public float Volume
    {
        get { return _volume; }
        set { _volume = value; _ApplyVolumeChange(); }
    }

    /// <summary>
    /// The volume factor applied to all audio items in the category (including a possible <see cref="parentCategory"/>)
    /// </summary>
    public float VolumeTotal
    {
        get {
            if ( parentCategory != null )
            {
                return parentCategory.VolumeTotal * _volume;
            }
            else 
                return _volume; 
        }
    }

    /// <summary>
    /// If a parent category is specified, the category inherits the volume of its parent.
    /// </summary>
    public AudioCategory parentCategory
    {
        set
        {
            _parentCategory = value;

            if ( value != null )
            {
                _parentCategoryName = _parentCategory.Name;
            }
            else
                _parentCategoryName = null;
        }
        get
        {
            if ( string.IsNullOrEmpty( _parentCategoryName ) )
            {
                return null;
            }
            if ( _parentCategory == null )
            {
                if ( audioController != null )
                {
                    _parentCategory = audioController._GetCategory( _parentCategoryName );
                }
                else
                {
                    Debug.LogWarning( "_audioController == null" );
                }
            }
            return _parentCategory;
        }
    }

    private AudioCategory _parentCategory;

    [SerializeField]
    private string _parentCategoryName;


    /// <summary>
    /// The <see cref="AudioController"/> the category belongs to
    /// </summary>
    public AudioController audioController { get; set; }

    /// <summary>
    /// Allows to define a specific audio object prefab for this category. If none is defined, 
    /// the default prefab as set by <see cref="AudioController.AudioObjectPrefab"/> is taken.
    /// </summary>
    /// <remarks> This way you can e.g. use special effects such as the reverb filter for 
    /// a specific category. Just add the respective filter component to the specified prefab.</remarks>
    public GameObject AudioObjectPrefab;

    /// <summary>
    /// Define your AudioItems using Unity inspector.
    /// </summary>  
    public AudioItem[ ] AudioItems;

    [SerializeField]
    private float _volume = 1.0f;
    
    /// <summary>
    /// Instantiates an AudioCategory
    /// </summary>
    /// <param name="audioController">The <see cref="AudioController"/> the category belongs to.</param>
    public AudioCategory( AudioController audioController )
    {
        this.audioController = audioController;
    }

    internal void _AnalyseAudioItems( Dictionary<string, AudioItem> audioItemsDict )
    {
        if ( AudioItems == null ) return;

        foreach ( AudioItem ai in AudioItems )
        {
            if ( ai != null )
            {
                ai._Initialize( this );
#if AUDIO_TOOLKIT_DEMO
                int? demoMaxNumAudioItemsConst = 0x12345B;

                int? demoMaxNumAudioItems = (demoMaxNumAudioItemsConst & 0xf);
                demoMaxNumAudioItems++;

                if ( audioItemsDict.Count > demoMaxNumAudioItems )
                {
                    Debug.LogError( "Audio Toolkit: The demo version does not allow more than " + demoMaxNumAudioItems + " audio items." );
                    Debug.LogWarning( "Please buy the full version of Audio Toolkit!" );
                    return;
                }
#endif

                //Debug.Log( string.Format( "SubItem {0}: {1} {2} {3}", fi.Name, ai.FixedOrder, ai.RandomOrderStart, ai._lastChosen ) );

                if ( audioItemsDict != null )
                {
                    try
                    {
                        audioItemsDict.Add( ai.Name, ai );
                    }
                    catch ( ArgumentException )
                    {
                        Debug.LogWarning( "Multiple audio items with name '" + ai.Name + "'");
                    }
                }
            }

        }
    }

    internal int _GetIndexOf( AudioItem audioItem )
    {
        if ( AudioItems == null ) return -1;

        for ( int i = 0; i < AudioItems.Length; i++ )
        {
            if ( audioItem == AudioItems[ i ] ) return i;
        }
        return -1;
    }

    private void _ApplyVolumeChange()
    {
        var objs = AudioController.GetPlayingAudioObjects();

        for ( int i = 0; i < objs.Count; i++ )
        {
            var o = objs[ i ];
            if ( _IsCategoryParentOf( o.category, this ) )
            {
                //if ( o.IsPlaying() )
                {
                    o._ApplyVolumeBoth();
                }
            }
        }
    }

    bool _IsCategoryParentOf( AudioCategory toTest, AudioCategory parent )
    {
        var cat = toTest;
        while ( cat != null )
        {
            if ( cat == parent ) return true;
            cat = cat.parentCategory;
        }
        return false;
    }

    /// <summary>
    /// Unloads all AudioClips specified in the subitems from memory. 
    /// </summary>
    /// <remarks>
    /// You will still be able to play the AudioClips, but you may experience performance hickups when Unity reloads the audio asset
    /// </remarks>
    public void UnloadAllAudioClips()
    {
        for ( int i = 0; i < AudioItems.Length; i++ )
        {
            AudioItems[ i ].UnloadAudioClip();
        }
    }
}

/// <summary>
/// Used by <see cref="AudioItem"/> to determine which <see cref="AudioSubItem"/> is chosen. 
/// </summary>
public enum AudioPickSubItemMode
{
    /// <summary>disables playback</summary>  
    Disabled,

    /// <summary>chooses a random subitem with a probability in proportion to <see cref="AudioSubItem.Probability"/> </summary>  
    Random,

    /// <summary>chooses a random subitem with a probability in proportion to <see cref="AudioSubItem.Probability"/> and makes sure it is not played twice in a row (if possible)</summary>
    RandomNotSameTwice,

    /// <summary> chooses the subitems in a sequence one after the other starting with the first</summary>
    Sequence,

    /// <summary> chooses the subitems in a sequence one after the other starting with a random subitem</summary>
    SequenceWithRandomStart,

    /// <summary> chooses all subitems at the same time</summary>
    AllSimultaneously,

    /// <summary> chooses two different subitems at the same time (if possible)</summary>
    TwoSimultaneously,

    /// <summary> always chooses the first subitem</summary>
    StartLoopSequenceWithFirst,
    
	/// <summary> Same as RandomNotSameTwice but only picks from odds or evens switching every time. Useful for footsteps left/right</summary>
    RandomNotSameTwiceOddsEvens,
}

/// <summary>
/// The AudioItem class represents a uniquely named audio entity that can be played by scripts.
/// </summary>
/// <remarks>
/// AudioItem objects are defined in an AudioCategory using the Unity inspector.
/// </remarks>
[System.Serializable]
public class AudioItem
{
    /// <summary>
    /// The unique name of the audio item ( = audioID )
    /// </summary>
    public string Name;

    /// <summary>
    /// AudioItem loop mode.
    /// </summary>
    [Serializable]
    public enum LoopMode
    {
        /// <summary>
        /// No looping.
        /// </summary>
        DoNotLoop = 0,

        /// <summary>
        /// The chosen subitem (in dependence of the <see cref="SubItemPickMode"/> will be looped.
        /// </summary>
        LoopSubitem = 1,

        /// <summary>
        /// After the subitem chosen in dependence of the <see cref="SubItemPickMode"/> has stopped playing, 
        /// a new subitem will be chosen and played.
        /// </summary>
        /// <remarks>
        /// Use this loop mode to generate a randomly playing looping sequence. Since Unity v4.1 this is gapless.
        /// </remarks>
        LoopSequence = 2,

        // 3... deprecated LoopGapless

        /// <summary>
        /// Play as many sub-items as specified by <see cref="loopSequenceCount"/> and loop the last one picked. Specify zero to 
        /// play as many sub-items as specified in this audio item.
        /// </summary>
        PlaySequenceAndLoopLast = 4,

        /// <summary>
        /// Play as many sub-items as specified by <see cref="loopSequenceCount"/> (as intro) and loop the second last one picked. 
        /// If see AudioItem.Stop() is called the very last ist played as an outro.
        /// </summary>
        IntroLoopOutroSequence = 5,

    }

    /// <summary>
    /// If enabled the audio item will get looped when played.
    /// </summary>
    public LoopMode Loop = LoopMode.DoNotLoop;

    /// <summary>
    /// The number of sub-items to be played in the loop modes <see cref="LoopMode.LoopSequence"/>.
    /// </summary>
    /// <remarks>
    /// Specify 0 to loop infinitely (This is also the default value). In <see cref="LoopMode.PlaySequenceAndLoopLast"/> mode as many 
    /// sub-item will be picked as there are sub-items specified for this audio item.
    /// </remarks>
    public int loopSequenceCount = 0;


    /// <summary>
    /// Specifies a time overlap for the <see cref="LoopMode.LoopSequence"/>
    /// </summary>
    /// <remarks>
    /// Positive values mean an overlap, negative values mean a gap between two consequent sub-items in the loop sequence.
    /// </remarks>
    public float loopSequenceOverlap = 0;

    /// <summary>
    /// Specifies a random delay for the <see cref="LoopMode.LoopSequence"/>
    /// </summary>
    /// <remarks>
    /// A random delay between 0 and this value will be added between two subsequent subitmes in the <see cref="LoopMode.LoopSequence"/>. Can be combined with <see cref="loopSequenceOverlap"/>.
    /// </remarks>
    public float loopSequenceRandomDelay = 0;

	/// <summary>
	/// Specifies a random pitch for the <see cref="LoopMode.LoopSequence"/>
	/// </summary>
	/// <remarks>
	/// A random pitch between 0 and this value will be added to each subitem played in the <see cref="LoopMode.LoopSequence"/>
	/// </remarks>
	public float loopSequenceRandomPitch = 0;

	/// <summary>
	/// Specifies a random volume for the <see cref="LoopMode.LoopSequence"/>
	/// </summary>
	/// <remarks>
	/// A random volume value % will be added to each subitem played in the 'LoopSequence'. Will be combined with subitem random volume value.
	/// </remarks>
	public float loopSequenceRandomVolume = 0;
	
    /// <summary>
    /// If disabled, the audio will keep on playing if a new scene is loaded.
    /// </summary>
    public bool DestroyOnLoad = true;

    /// <summary>
    /// The volume applied to all audio sub-items of this audio item. 
    /// </summary>
    public float Volume = 1;

    /// <summary>
    /// Determines which <see cref="AudioSubItem"/> is chosen when playing an <see cref="AudioItem"/>
    /// </summary>
    public AudioPickSubItemMode SubItemPickMode = AudioPickSubItemMode.RandomNotSameTwice;

    /// <summary>
    /// Assures that the same audio item will not be played multiple times within this time frame. This is useful if several events triggered at almost the same time want to play the same audio item which can cause unwanted noise artifacts.
    /// </summary>
    public float MinTimeBetweenPlayCalls = 0.1f;

    /// <summary>
    /// Assures that the same audio item will not be played more than <c>MaxInstanceCount</c> times simultaneously.
    /// </summary>
    /// <remarks>Set to 0 to disable.</remarks>
    public int MaxInstanceCount = 0;

    /// <summary>
    /// Defers the playback of the audio item for <c>Delay</c> seconds.
    /// </summary>
    public float Delay = 0;

    /// <summary>
    /// If enabled you can specify specific AudioSource settings
    /// </summary>
    public bool overrideAudioSourceSettings = false;

    /// <summary>
    /// Overrides the AudioSource MinDistance value if <see cref="overrideAudioSourceSettings"/> is enabled.
    /// </summary>
    public float audioSource_MinDistance = 1;

    /// <summary>
    /// Overrides the AudioSource MaxDistance value if <see cref="overrideAudioSourceSettings"/> is enabled.
    /// </summary>
    public float audioSource_MaxDistance = 500;

    /// <summary>
    /// Sets the AudioSource spatialBlend value (0=2D 1=3D)
    /// </summary>
    public float spatialBlend = 0;
    
    /// <summary>
    /// Define your audio sub-items using the Unity inspector.
    /// </summary>
    public AudioSubItem[] subItems;

    internal int _lastChosen = -1;
    internal double _lastPlayedTime = -1; // high precision system time

    [System.NonSerializedAttribute] // circle reference causes problem with Unity serialisation
    private AudioCategory _category;

    /// <summary>
    /// the <c>AudioCategroy</c> the audio item belongs to.
    /// </summary>
    public AudioCategory category
    {
        private set { _category = value; }
        get { return _category; }
    }

    void Awake()
    {
        if ( (int) Loop == 3 ) // deprecated LoopGapless
        {
            Loop = LoopMode.LoopSequence;
        }
        _lastChosen = -1;
    }

    /// <summary>
    /// Initializes the audio item for a certain category. (Internal use only, not required to call).
    /// </summary>
    internal void _Initialize( AudioCategory categ )
    {
        category = categ;

        _NormalizeSubItems();
    }

    private void _NormalizeSubItems()
    {
        float sum = 0.0f;

        int subItemID = 0;

        bool arePriorityItems = false;
        foreach ( AudioSubItem i in subItems )
        {
            if( _IsValidSubItem( i ) && i.DisableOtherSubitems )
            {
                arePriorityItems = true;
                break;
            }
        }

        foreach ( AudioSubItem i in subItems )
        {
            i.item = this;
            if ( _IsValidSubItem( i ) && ( i.DisableOtherSubitems || !arePriorityItems ) )
            {
                sum += i.Probability;
            }
            i._subItemID = subItemID;
            subItemID++;
        }

        if ( sum <= 0 )
        {
            return;
        }

        // Compute normalized probabilities

        float summedProb = 0;

        foreach ( AudioSubItem i in subItems )
        {
            if ( _IsValidSubItem( i ) )
            {
                if ( i.DisableOtherSubitems || !arePriorityItems )
                {
                    summedProb += i.Probability / sum;
                }

                i._SummedProbability = summedProb;
            }
        }
    }

    private static bool _IsValidSubItem( AudioSubItem item )
    {
        switch ( item.SubItemType )
        {
        case AudioSubItemType.Clip:
            return item.Clip != null;
        case AudioSubItemType.Item:
            return item.ItemModeAudioID != null && item.ItemModeAudioID.Length > 0;
        }
        return false;
    }

    /// <summary>
    /// Unloads the AudioClip from memory. 
    /// </summary>
    /// <remarks>
    /// You will still be able to play the AudioClip, but you may experience performance hickups when Unity reloads the audio asset
    /// </remarks>
    public void UnloadAudioClip()
    {
        foreach( var si in subItems )
        {
            if( si.Clip )
            {
                Resources.UnloadAsset( si.Clip );
            }
        }
    }
}

/// <summary>
/// The type of an <see cref="AudioSubItem"/>  
/// </summary>
public enum AudioSubItemType
{
    /// <summary>The <see cref="AudioSubItem"/> plays an <see cref="UnityEngine.AudioClip"/></summary>
    Clip,
    /// <summary>The <see cref="AudioSubItem"/> plays an <see cref="AudioItem"/></summary>
    Item,
}

/// <summary>
/// An AudioSubItem represents a specific Unity audio clip.
/// </summary>
/// <remarks>
/// Add your AudioSubItem to an AudioItem using the Unity inspector.
/// </remarks>
[System.Serializable]
public class AudioSubItem
{
    /// <summary>
    /// Specifies the type of this <see cref="AudioSubItem"/>  
    /// </summary>
    public AudioSubItemType SubItemType = AudioSubItemType.Clip;

    /// <summary>
    /// If multiple sub-items are defined within an audio item, the specific audio clip is chosen with a probability in proportion to the <c>Probability</c> value.
    /// </summary>
    public float Probability = 1.0f;

    /// <summary>
    /// If enabled all other subitmes which do not have this option enabled will not be played. Useful for testing specific subitmes within a large list of subitems.
    /// </summary>
    public bool DisableOtherSubitems;

    /// <summary>
    /// Specifies the <c>audioID</c> to be played in case of the <see cref="AudioSubItemType.Item"/> mode
    /// </summary>
    public string ItemModeAudioID;

    /// <summary>
    /// Specifies the <see cref="UnityEngine.AudioClip"/> to be played in case of the <see cref="AudioSubItemType.Item"/> mode.
    /// </summary>
    public AudioClip Clip;

    /// <summary>
    /// The volume applied to the audio sub-item.
    /// </summary>
    public float Volume = 1.0f;

    /// <summary>
    /// Alters the pitch in units of semitones ( thus 12 = twice the speed)
    /// </summary>
    public float PitchShift = 0f;

    /// <summary>
    /// Alters the pan: -1..left,  +1..right
    /// </summary>
    public float Pan2D = 0;

    /// <summary>
    /// Defers the playback of the audio sub-item for <c>Delay</c> seconds.
    /// </summary>
    public float Delay = 0;

    /// <summary>
    /// Randomly shifts the pitch in units of semitones ( thus 12 = twice the speed)
    /// </summary>
    public float RandomPitch = 0;

    /// <summary>
    /// Randomly shifts the volume +/- this value
    /// </summary>
    public float RandomVolume = 0;

    /// <summary>
    /// Randomly adds a delay between 0 and RandomDelay
    /// </summary>
    public float RandomDelay = 0;

    /// <summary>
    /// Ends playing the audio at this time (in seconds).
    /// </summary>
    /// <remarks>
    /// Can be used as a workaround for an unknown clip length (e.g. for tracker files)
    /// </remarks>
    public float ClipStopTime = 0;

    /// <summary>
    /// Offsets the the audio clip start time (in seconds).
    /// </summary>
    /// <remarks>
    /// Does not work with looping.
    /// </remarks>
    public float ClipStartTime = 0;

    /// <summary>
    /// Automatic fade-in in seconds
    /// </summary>
    public float FadeIn = 0;

    /// <summary>
    /// Automatic fade-out in seconds
    /// </summary>
    public float FadeOut = 0;

    /// <summary>
    /// Starts playing at a random position.
    /// </summary>
    /// <remarks>
    /// Useful for audio loops.
    /// </remarks>
    public bool RandomStartPosition = false;

    private float _summedProbability = -1.0f; // -1 means not initialized or invalid
    internal int _subItemID = 0;

    internal float _SummedProbability
    {
        get { return _summedProbability; }
        set { _summedProbability = value; }
    }

    [System.NonSerializedAttribute] // circle reference causes problem with Unity serialisation
    private AudioItem _item;

    /// <summary>
    /// the <c>AudioItem</c> the sub-item belongs to.
    /// </summary>
    public AudioItem item
    {
        internal set { _item = value; }
        get { return _item; }
    }

    /// <summary>
    /// Returns the name of the audio clip for debugging.
    /// </summary>
    /// <returns>
    /// The debug output string.
    /// </returns>
    public override string ToString()
    {
        if ( SubItemType == AudioSubItemType.Clip )
        {
            return "CLIP: " + Clip.name;
        }
        else
            return "ITEM: " + ItemModeAudioID;
    }

}