#if UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0
#define UNITY_3_x
#endif

#if !UNITY_3_x && !UNITY_4_1 && !UNITY_4_2
#define UNITY_4_3_OR_NEWER
#endif

#if UNITY_5 || UNITY_6 || UNITY_7
#define UNITY_5_OR_NEWER
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

abstract public class EditorEx : Editor
{
    protected GUILayoutOption labelFieldOption; 
    protected GUIStyle styleLabel;
    protected GUIStyle styleUnit;
    protected GUIStyle styleFloat;
    protected GUIStyle stylePopup;
    protected GUIStyle styleEnum;

    bool setFocusNextField;
    bool userChanges;
    int fieldIndex;

    virtual protected void LogUndo( string label )
    {

    }

    protected void SetFocusForNextEditableField()
    {
        setFocusNextField = true;
    }

    protected void ShowFloat( float f, string label )
    {
        EditorGUILayout.LabelField( label, f.ToString() );
    }

    protected void ShowString( string text, string label )
    {
        EditorGUILayout.LabelField( label, text );
    }

    private float GetFloat( float f, string label, string unit, string tooltip = null )
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField( new GUIContent( label, tooltip ), labelFieldOption );
        //GUILayout.Label( label, styleLabel );
        //EditorGUILayout.Space();
        float f_ret = EditorGUILayout.FloatField( f, styleFloat );

        if ( !string.IsNullOrEmpty( unit ) )
        {
            GUILayout.Label( unit, styleUnit );
        }
        else
        {
            GUILayout.Label( " ", styleUnit );
        }
        
        EditorGUILayout.EndHorizontal();

        //float f_ret = EditorGUILayout.FloatField( label, f, styleFloat );
        return f_ret;
    }

    private int GetInt( int f, string label, string unit, string tooltip = null )
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label( new GUIContent( label, tooltip ), styleLabel );
        //EditorGUILayout.Space();
        int f_ret = EditorGUILayout.IntField( f, styleFloat );
        if ( !string.IsNullOrEmpty( unit ) )
        {
            GUILayout.Label( unit, styleUnit );
        }
        else
        {
            GUILayout.Label( " ", styleUnit );
        }
        EditorGUILayout.EndHorizontal();
        return f_ret;
    }

    private float GetFloat( float f, string label, float sliderMin, float sliderMax, string unit )
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label( label, styleLabel );
        //EditorGUILayout.Space();
        float f_ret = f;
        f_ret = EditorGUILayout.FloatField( f_ret, styleFloat, GUILayout.Width( 50 ) );
        f_ret = GUILayout.HorizontalSlider( f_ret, sliderMin, sliderMax );

        if ( !string.IsNullOrEmpty( unit ) )
        {
            GUILayout.Label( unit, styleUnit );
        }
        else
        {
            GUILayout.Label( " ", styleUnit );
        }

        EditorGUILayout.EndHorizontal();
        return f_ret;
    }

    private float GetFloatPercent( float f, string label, string unit, string tooltip = null )
    {
        EditorGUILayout.BeginHorizontal();
        //GUILayout.Label( label, styleLabel );
        EditorGUILayout.LabelField( new GUIContent( label, tooltip ), labelFieldOption );
        //EditorGUILayout.Space();
        float f_ret = f;
        f_ret = (float)EditorGUILayout.IntField( Mathf.RoundToInt( f_ret * 100 ), styleFloat, GUILayout.Width( 50 ) ) / 100;
        f_ret = GUILayout.HorizontalSlider( f_ret, 0, 1 );

        if ( !string.IsNullOrEmpty( unit ) )
        {
            GUILayout.Label( unit, styleUnit );
        }
        else
        {
            GUILayout.Label( " ", styleUnit );
        }

        EditorGUILayout.EndHorizontal();
        return f_ret;
    }

    private float GetFloatPlusMinusPercent( float f, string label, string unit )
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label( label, styleLabel );
        //EditorGUILayout.Space();
        float f_ret = f;
        f_ret = (float) EditorGUILayout.IntField( Mathf.RoundToInt( f_ret * 100 ), styleFloat, GUILayout.Width( 50 ) ) / 100;
        f_ret = GUILayout.HorizontalSlider( f_ret, -1, 1 );
        if ( !string.IsNullOrEmpty( unit ) )
        {
            GUILayout.Label( unit, styleUnit );
        }
        else
        {
            GUILayout.Label( " ", styleUnit );
        }
        EditorGUILayout.EndHorizontal();
        return f_ret;
    }

    protected bool EditFloat( ref float f, string label )
    {
        float new_f = GetFloat( f, label, null );

        if ( new_f != f )
        {
            LogUndo( label );
            f = new_f;
            return true;
        }

        return false;
    }

    protected bool EditFloat( ref float f, string label, string unit, string tooltip = null )
    {
        float new_f = GetFloat( f, label, unit, tooltip );

        if ( new_f != f )
        {
            LogUndo( label );
            f = new_f;
            return true;
        }

        return false;
    }

    private float GetFloat01( float f, string label )
    {
        return Mathf.Clamp01( GetFloatPercent( f, label, null ) );
    }

    private float GetFloat01( float f, string label, string unit, string tooltip = null )
    {
        return Mathf.Clamp01( GetFloatPercent( f, label, unit, tooltip ) );
    }

    private float GetFloatPlusMinus1( float f, string label, string unit )
    {
        return Mathf.Clamp( GetFloatPlusMinusPercent( f, label, unit ), -1, 1 );
    }

    private float GetFloatWithinRange( float f, string label, float minValue, float maxValue )
    {
        return Mathf.Clamp( GetFloat( f, label, minValue, maxValue, null ), minValue, maxValue );
    }

    protected bool EditFloatWithinRange( ref float f, string label, float minValue, float maxValue )
    {
        float new_f = GetFloatWithinRange( f, label, minValue, maxValue );

        if ( new_f != f )
        {
            LogUndo( label );
            f = new_f;
            return true;
        }

        return false;
    }

    protected bool EditInt( ref int f, string label )
    {
        int new_f = GetInt( f, label, null );

        if ( new_f != f )
        {
            LogUndo( label );
            f = new_f;
            return true;
        }
        return false;
    }

    protected bool EditInt( ref int f, string label, string unit, string tooltip = null )
    {
        int new_f = GetInt( f, label, unit, tooltip );

        if ( new_f != f )
        {
            LogUndo( label );
            f = new_f;
            return true;
        }
        return false;
    }
    
    protected bool EditFloat01( ref float f, string label )
    {
        float new_f = GetFloat01( f, label);

        if ( new_f != f )
        {
            LogUndo( label );
            f = new_f;
            return true;
        }

        return false;
    }

    protected bool EditFloat01( ref float f, string label, string unit, string tooltip = null )
    {
        float new_f = GetFloat01( f, label, unit, tooltip );

        if ( new_f != f )
        {
            LogUndo( label );
            f = new_f;
            return true;
        }

        return false;
    }

    protected bool EditFloatPlusMinus1( ref float f, string label, string unit )
    {
        float new_f = GetFloatPlusMinus1( f, label, unit );

        if ( new_f != f )
        {
            LogUndo( label );
            f = new_f;
            return true;
        }

        return false;

    }

    private bool GetBool( bool b, string label, string tooltip = null )
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField( new GUIContent( label, tooltip ), labelFieldOption );
        //GUILayout.Label( label, styleLabel );
        //EditorGUILayout.Space();
        bool b_ret = EditorGUILayout.Toggle( b, GUILayout.Width( 20 ) );
        EditorGUILayout.EndHorizontal();
        return b_ret;
    }

    protected bool EditBool( ref bool b, string label, string tooltip = null ) // returns was changed state
    {
        bool new_b = GetBool( b, label, tooltip );

        if ( new_b != b )
        {
            LogUndo( label );
            b = new_b;
            return true;
        }

        return false;
    }

    protected bool EditPrefab<T>( ref T prefab, string label, string tooltip = null ) where T : UnityEngine.Object
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField( new GUIContent( label, tooltip ), labelFieldOption );
        //GUILayout.Label( label, styleLabel );
        T new_f = (T) EditorGUILayout.ObjectField( prefab, typeof( T ), false );
        EditorGUILayout.EndHorizontal();

        if ( new_f != prefab )
        {
            LogUndo( label );
            prefab = new_f;
            return true;
        }
        return false;
    }

    protected bool EditString( ref string txt, string label, GUIStyle styleText = null, string tooltip = null )
    {
        EditorGUILayout.BeginHorizontal();
        //GUILayout.Label( label, styleLabel );
        EditorGUILayout.LabelField( new GUIContent( label, tooltip ), labelFieldOption );
        //EditorGUILayout.Space();
        BeginEditableField();

        string newTxt;
        if ( styleText != null )
        {
            newTxt = EditorGUILayout.TextField( txt, styleText );
        }
        else
        {
            newTxt = EditorGUILayout.TextField( txt );
        }
        EndEditableField();
        EditorGUILayout.EndHorizontal();

        if ( newTxt != txt )
        {
            LogUndo( label );
            txt = newTxt;
            return true;
        }
        return false;
    }

    protected int Popup( string label, int selectedIndex, string[] content, string tooltip = null, bool sortAlphabetically = true )
    {
        return PopupWithStyle( label, selectedIndex, content, stylePopup, tooltip, sortAlphabetically );
    }

    public class ContentWithIndex
    {
        public string content;
        public int index;

        public ContentWithIndex( string content, int index )
        {
            this.content = content;
            this.index = index;
        }
    }

    protected int PopupWithStyle( string label, int selectedIndex, string[] content, GUIStyle style, string tooltip = null, bool sortAlphabetically = true )
    {
        string[ ] contentSorted;

        List<ContentWithIndex> list = null;

        if ( content.Length == 0 )
        {
            sortAlphabetically = false;
        }

        if ( sortAlphabetically )
        {
            list = _CreateContentWithIndexList( content );
            contentSorted = new string[ content.Length ];
            int index = 0;
            foreach ( var el in list )
            {
                contentSorted[ index++ ] = el.content;
            }
        }
        else
            contentSorted = content;

        EditorGUILayout.BeginHorizontal();
        //GUILayout.Label( label, styleLabel );
        EditorGUILayout.LabelField( new GUIContent( label, tooltip ), labelFieldOption );
        int newIndex;
        if ( sortAlphabetically )
        {
            newIndex = EditorGUILayout.Popup( list.FindIndex( x => x.index == selectedIndex ) , contentSorted, style );

            newIndex = list[ newIndex ].index;
        }
        else
        {
            newIndex = EditorGUILayout.Popup( selectedIndex, contentSorted, style );
        }
        EditorGUILayout.EndHorizontal();
        if ( newIndex != selectedIndex )
        {
            LogUndo( label );
        }
        return newIndex;
    }

    private List<ContentWithIndex> _CreateContentWithIndexList( string[ ] content )
    {
        var list = new List<ContentWithIndex>();
        for ( int i = 0; i < content.Length;i++ )
        {
            list.Add( new ContentWithIndex( content[i], i ) );
        }
        return list.OrderBy( x => x.content ).ToList();
    }

    protected Enum EnumPopup( string label, Enum selectedEnum, string tooltip = null )
    {
        EditorGUILayout.BeginHorizontal();
        //GUILayout.Label( label, styleLabel );
        EditorGUILayout.LabelField( new GUIContent( label, tooltip ), labelFieldOption );
        Enum newEnum = EditorGUILayout.EnumPopup( selectedEnum, styleEnum );
        EditorGUILayout.EndHorizontal();

        if ( !object.Equals( newEnum, selectedEnum ) )
        {
            LogUndo( label );
        }
        return newEnum;
    }

    private void EndEditableField()
    {
        if ( setFocusNextField )
        {
            setFocusNextField = false;
            //GUI.FocusControl( GetCurrentFieldControlName() );  // TODO: not working for some reason
            //Debug.Log( "Set focus to: " + GetCurrentFieldControlName() );
            //Debug.Log( "Currently focused: " + GUI.GetNameOfFocusedControl() );
        }
    }

    private void BeginEditableField()
    {
        fieldIndex++;
        if ( setFocusNextField )
        {
            GUI.SetNextControlName( GetCurrentFieldControlName() );
        }
    }

    private String GetCurrentFieldControlName()
    {
        return string.Format( "field{0}", fieldIndex );
    }

    protected void BeginInspectorGUI()
    {
        serializedObject.Update();
        SetStyles();

        fieldIndex = 0;
        userChanges = false;
    }

    protected void SetStyles()
    {
        labelFieldOption = GUILayout.Width( 180 );
        styleLabel = new GUIStyle( EditorStyles.label );
        styleUnit = new GUIStyle( EditorStyles.label );
        styleFloat = new GUIStyle( EditorStyles.numberField );
        stylePopup = new GUIStyle( EditorStyles.popup );
        styleEnum = new GUIStyle( EditorStyles.popup );
        //styleFloat.alignment = TextAnchor.MiddleRight;
        //styleFloat.fixedWidth = 100;
        //styleFloat.stretchWidth = true;
        //styleFloat.contentOffset = new Vector2( styleFloat.contentOffset.x + 50, styleFloat.contentOffset.y );
        //GUILayout.Width( 60 );
        styleLabel.fixedWidth = 180;
        styleUnit.fixedWidth = 65;
    }

    protected void EndInspectorGUI()
    {
        if ( GUI.changed || userChanges )
        {
            EditorUtility.SetDirty( target );
        }
        serializedObject.ApplyModifiedProperties();
    }

    protected void KeepChanges()
    {
        userChanges = true;
        //EditorUtility.SetDirty( target );
    }
}

[CustomEditor( typeof(AudioController) )]
public class AudioController_Editor : EditorEx
{
    AudioController AC;

    int currentCategoryIndex
    {
        get
        {
            return AC._currentInspectorSelection.currentCategoryIndex;
        }
        set
        {
            AC._currentInspectorSelection.currentCategoryIndex = value;
        }
    }
    int currentItemIndex
    {
        get
        {
            return AC._currentInspectorSelection.currentItemIndex;
        }
        set
        {
            AC._currentInspectorSelection.currentItemIndex = value;
        }
    }

    int currentSubitemIndex
    {
        get
        {
            return AC._currentInspectorSelection.currentSubitemIndex;
        }
        set
        {
            AC._currentInspectorSelection.currentSubitemIndex = value;
        }
    }
    
    int currentPlaylistIndex
    {
        get
        {
            return AC._currentInspectorSelection.currentPlaylistIndex;
        }
        set
        {
            AC._currentInspectorSelection.currentPlaylistIndex = value;
        }
    }

    public static bool globalFoldout = true;
    public static bool playlistFoldout = true;
    public static bool musicFoldout = true;
    public static bool categoryFoldout = true;
    public static bool itemFoldout = true;
    public static bool subitemFoldout = true;

    GUIStyle foldoutStyle;
    GUIStyle centeredTextStyle;
    GUIStyle popupStyleColored;
    GUIStyle styleChooseItem;
    GUIStyle textAttentionStyle;
    GUIStyle textAttentionStyleLabel;
    GUIStyle textInfoStyleLabel;

    int lastCategoryIndex = -1;
    int lastItemIndex = -1;
    int lastSubItemIndex = -1;

    AudioCategory currentCategory
    {
        get
        {
            if ( currentCategoryIndex < 0 || AC.AudioCategories == null || currentCategoryIndex >= AC.AudioCategories.Length )
            {
                return null;
            }
            return AC.AudioCategories[ currentCategoryIndex ];
        }
    }
    AudioItem currentItem
    {
        get
        {
            AudioCategory curCategory = currentCategory;

            if ( currentCategory == null )
            {
                return null;
            }

            if ( currentItemIndex < 0 || curCategory.AudioItems == null || currentItemIndex >= curCategory.AudioItems.Length )
            {
                return null;
            }
            return currentCategory.AudioItems[ currentItemIndex ];
        }
    }

    AudioSubItem currentSubItem
    {
        get
        {
            AudioItem curItem = currentItem;

            if ( curItem == null )
            {
                return null;
            }

            if ( currentSubitemIndex < 0 || curItem.subItems == null || currentSubitemIndex >= curItem.subItems.Length )
            {
                return null;
            }
            return curItem.subItems[ currentSubitemIndex ];
        }
    }

    public int currentCategoryCount
    {
        get {
            if( AC.AudioCategories != null )
            {
                return AC.AudioCategories.Length;
            }
            else 
                return 0;
        }
    }

    public int currentItemCount
    {
        get
        {
            if ( currentCategory != null )
            {
                if ( currentCategory.AudioItems != null )
                {
                    return currentCategory.AudioItems.Length;
                }
                return 0;
            }
            else
                return 0;
        }
    }

    public int currentSubItemCount
    {
        get
        {
            if ( currentItem != null )
            {
                if ( currentItem.subItems != null )
                {
                    return currentItem.subItems.Length;
                }
                return 0;
            }
            else
                return 0;
        }
    }

    const string _playWithInspectorNotice = "Volume and pitch of audios are only correct when played during playmode. You can ignore the following Unity warning (if any).";
    const string _playNotSupportedOnMac = "On MacOS playing audios is only supported during play mode.";
    const string _nameForNewCategoryEntry = "!!! Enter Unique Category Name Here !!!";
    const string _nameForNewAudioItemEntry = "!!! Enter Unique Audio ID Here !!!";

    //public void OnEnable()
    //{
        
    //}

    protected override void LogUndo( string label )
    {
        //Debug.Log( "Undo: " + label );
#if UNITY_4_3_OR_NEWER
		Undo.RecordObject( AC, "AudioToolkit: " + label );
#else
        Undo.RegisterUndo( AC, "AudioToolkit: " + label );
#endif
    }

    public new void SetStyles()
    {
        base.SetStyles();

        foldoutStyle = new GUIStyle( EditorStyles.foldout );

        //var foldoutColor = new UnityEngine.Color( 0.3f, 0.75f, 0.75f );
        //var foldoutColor = new UnityEngine.Color( 0.1f, 0.6f, 0.05f );
        var foldoutColor = new UnityEngine.Color( 0.0f, 0.0f, 0.2f );

        //foldoutStyle.normal.background = EditorStyles.boldLabel.onNormal.background;
        //foldoutStyle.focused.background = EditorStyles.boldLabel.onNormal.background;
        //foldoutStyle.active.background = EditorStyles.boldLabel.onNormal.background;
        //foldoutStyle.hover.background = EditorStyles.boldLabel.onNormal.background;

        foldoutStyle.onNormal.background = EditorStyles.boldLabel.onNormal.background;
        foldoutStyle.onFocused.background = EditorStyles.boldLabel.onNormal.background;
        foldoutStyle.onActive.background = EditorStyles.boldLabel.onNormal.background;
        foldoutStyle.onHover.background = EditorStyles.boldLabel.onNormal.background;


        foldoutStyle.normal.textColor = foldoutColor;
        foldoutStyle.focused.textColor = foldoutColor;
        foldoutStyle.active.textColor = foldoutColor;
        foldoutStyle.hover.textColor = foldoutColor;
        foldoutStyle.fixedWidth = 500;

        //foldoutStyle.onNormal.textColor = foldoutColor;
        //foldoutStyle.onFocused.textColor = foldoutColor;
        //foldoutStyle.onActive.textColor = foldoutColor;
        //foldoutStyle.onHover.textColor = foldoutColor;

        centeredTextStyle = new GUIStyle( EditorStyles.label );
        centeredTextStyle.alignment = TextAnchor.UpperCenter;
        centeredTextStyle.stretchWidth = true;

        popupStyleColored = new GUIStyle( stylePopup );
        styleChooseItem = new GUIStyle( stylePopup );

        bool isDarkSkin = popupStyleColored.normal.textColor.grayscale > 0.5f;

        if ( isDarkSkin )
        {
            popupStyleColored.normal.textColor = new Color( 0.9f, 0.9f, 0.5f );
        } else
            popupStyleColored.normal.textColor = new Color( 0.6f, 0.1f, 0.0f );


        textAttentionStyle = new GUIStyle( EditorStyles.textField );

        if ( isDarkSkin )
        {
            textAttentionStyle.normal.textColor = new Color( 1, 0.3f, 0.3f );
        } else
            textAttentionStyle.normal.textColor = new Color( 1, 0f, 0f );

        textAttentionStyleLabel = new GUIStyle( EditorStyles.label );

        if ( isDarkSkin )
        {
            textAttentionStyleLabel.normal.textColor = new Color( 1, 0.3f, 0.3f );
        }
        else
            textAttentionStyleLabel.normal.textColor = new Color( 1, 0f, 0f );

        textInfoStyleLabel = new GUIStyle( EditorStyles.label );

        if ( isDarkSkin )
        {
            textInfoStyleLabel.normal.textColor = new Color( 0.4f, 0.4f, 0.4f );
        }
        else
            textInfoStyleLabel.normal.textColor = new Color( 0.6f, 0.6f, 0.6f );
    }


    public override void OnInspectorGUI()
    {
        SetStyles();

        BeginInspectorGUI();

        AC = (AudioController)target;

        _ValidateCurrentCategoryIndex();
        _ValidateCurrentItemIndex();
        _ValidateCurrentSubItemIndex();

        if( lastCategoryIndex != currentCategoryIndex ||
            lastItemIndex != currentItemIndex ||
            lastSubItemIndex != currentSubitemIndex )
        {
            GUIUtility.keyboardControl = 0; // workaround for Unity weirdness not changing the value of a focused GUI element when changing a category/item
            lastCategoryIndex = currentCategoryIndex;
            lastItemIndex = currentItemIndex;
            lastSubItemIndex = currentSubitemIndex;
        }

        

        EditorGUILayout.Space();

        if ( globalFoldout = EditorGUILayout.Foldout( globalFoldout, "Global Audio Settings", foldoutStyle ) )
        {
            bool currentlyAdditionalController = AC.isAdditionalAudioController;

            bool changed = EditBool( ref currentlyAdditionalController, "Additional Audio Controller", "A scene can contain multiple AudioControllers. All but the main AudioController must be marked as 'additional'." );
            if ( changed )
            {
                AC.isAdditionalAudioController = currentlyAdditionalController;
            }
            EditBool( ref AC.Persistent, "Persist Scene Loading", "A non-persisting AudioController will get destroyed when loading the next scene." );
            EditBool( ref AC.UnloadAudioClipsOnDestroy, "Unload Audio On Destroy", "This option will unload all AudioClips from memory which referenced by this AudioController if the controller gets destroyed (e.g. when loading a new scene and the AudioController is not persistent). \n" +
                "Use this option in combination with additional none-persistent AudioControllers to keep only those audios in memory that are used by the current scene. Use the primary persistent AudioController for all global audio that is used throughout all scenes."
                );

            bool currentlyDisabled = AC.DisableAudio;

            changed = EditBool( ref currentlyDisabled, "Disable Audio", "Disables all audio" );
            if( changed )
            {
                AC.DisableAudio = currentlyDisabled;
                if ( currentlyDisabled && AudioController.DoesInstanceExist() )
                {
                    AudioController.StopAll();
                }
            }

            float vol = AC.Volume;
            
            EditFloat01( ref vol, "Volume", "%" );

            AC.Volume = vol;

            EditPrefab( ref AC.AudioObjectPrefab, "Audio Object Prefab", "You must specify a prefab here that will get instantiated for each played audio. This prefab must contain the following components: AudioSource, AudioObject, PoolableObject." );
            EditBool( ref AC.UsePooledAudioObjects, "Use Pooled AudioObjects", "Pooling increases performance when playing many audio files. Strongly recommended particularly on mobile platforms." );
            EditBool( ref AC.PlayWithZeroVolume, "Play With Zero Volume", "If disabled Play() calls with a volume of zero will not create an AudioObject." );

            EditBool( ref AC.EqualPowerCrossfade, "Equal-power crossfade", "Unfortunatly not 100% correct due to unknown volume formulas used by Unity" );
        }

        VerticalSpace();

        // music specific
        if( musicFoldout = EditorGUILayout.Foldout( musicFoldout, "Music Settings", foldoutStyle ) )
        { 
            EditBool( ref AC.specifyCrossFadeInAndOutSeperately, "Separate crossfade in/out", "Allows to specify a separate fade-in and out value for all music");
            if ( AC.specifyCrossFadeInAndOutSeperately )
            {
                float v_in = AC.musicCrossFadeTime_In;
                EditFloat( ref v_in, "   Music Crossfade-in Time", "sec" ); AC.musicCrossFadeTime_In = v_in;

                float v_out = AC.musicCrossFadeTime_Out;
                EditFloat( ref v_out, "   Music Crossfade-out Time", "sec" ); AC.musicCrossFadeTime_Out = v_out;
            }
            else
            {
                EditFloat( ref AC.musicCrossFadeTime, "Music Crossfade Time", "sec" );
            }
        }

        VerticalSpace();

        // playlist specific
        if ( playlistFoldout = EditorGUILayout.Foldout( playlistFoldout, "Playlist Settings", foldoutStyle ) )
        {
            EditorGUILayout.BeginHorizontal();
            var playListNames = GetPlaylistNames();
            currentPlaylistIndex = Popup( "Playlist", currentPlaylistIndex, playListNames, "List of audioIDs, click on 'add to playlist' to add audio items", false );
            GUI.enabled = playListNames.Length > 0;
            if ( GUILayout.Button( "Up", GUILayout.Width( 35 ) ) && AC.musicPlaylist != null && AC.musicPlaylist.Length > 0 )
            {
                if ( SwapArrayElements( AC.musicPlaylist, currentPlaylistIndex, currentPlaylistIndex - 1 ) )
                {
                    currentPlaylistIndex--;
                    KeepChanges();
                }
            }
            if ( GUILayout.Button( "Dwn", GUILayout.Width( 40 ) ) && AC.musicPlaylist != null && AC.musicPlaylist.Length > 0 )
            {
                if ( SwapArrayElements( AC.musicPlaylist, currentPlaylistIndex, currentPlaylistIndex + 1 ) )
                {
                    currentPlaylistIndex++;
                    KeepChanges();
                }
            }
            if ( GUILayout.Button( "-", GUILayout.Width( 25 ) ) && AC.musicPlaylist != null && AC.musicPlaylist.Length > 0 )
            {
                ArrayHelper.DeleteArrayElement( ref AC.musicPlaylist, currentPlaylistIndex );
                currentPlaylistIndex = Mathf.Clamp( currentPlaylistIndex - 1, 0, AC.musicPlaylist.Length - 1 );
                KeepChanges();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            string itemToAdd = _ChooseItem( "Add to Playlist" );
            if ( !string.IsNullOrEmpty( itemToAdd ) )
            {
                AddToPlayList( itemToAdd );
            }
            
            EditBool( ref AC.loopPlaylist, "Loop Playlist" );
            EditBool( ref AC.shufflePlaylist, "Shuffle Playlist", "Enables random playback of music playlists. Takes care that the same audio will not get played again too early" );
            EditBool( ref AC.crossfadePlaylist, "Crossfade Playlist" );
            EditFloat( ref AC.delayBetweenPlaylistTracks, "Delay Betw. Playlist Tracks", "sec" );

        }

        VerticalSpace();

        int categoryCount = AC.AudioCategories != null ? AC.AudioCategories.Length : 0;
        currentCategoryIndex = Mathf.Clamp( currentCategoryIndex, 0, categoryCount - 1 );

        if ( categoryFoldout = EditorGUILayout.Foldout( categoryFoldout, "Category Settings", foldoutStyle ) )
        {

            // Audio Items 
            EditorGUILayout.BeginHorizontal();

            bool justCreatedNewCategory = false;

            var categoryNames = GetCategoryNames();

            int newCategoryIndex = PopupWithStyle( "Category", currentCategoryIndex, categoryNames, popupStyleColored );
            if ( GUILayout.Button( "+", GUILayout.Width( 30 ) ) )
            {
                bool lastEntryIsNew = false;

                if ( categoryCount > 0 )
                {
                    lastEntryIsNew = AC.AudioCategories[ currentCategoryIndex ].Name == _nameForNewCategoryEntry;
                }

                if ( !lastEntryIsNew )
                {
                    newCategoryIndex = AC.AudioCategories != null ? AC.AudioCategories.Length : 0;
                    ArrayHelper.AddArrayElement( ref AC.AudioCategories, new AudioCategory( AC ) );
                    AC.AudioCategories[ newCategoryIndex ].Name = _nameForNewCategoryEntry;
                    justCreatedNewCategory = true;
                    KeepChanges();
                }
            }

            if ( GUILayout.Button( "-", GUILayout.Width( 30 ) ) && categoryCount > 0 )
            {

                if ( currentCategoryIndex < AC.AudioCategories.Length - 1 )
                {
                    newCategoryIndex = currentCategoryIndex;
                }
                else
                {
                    newCategoryIndex = Mathf.Max( currentCategoryIndex - 1, 0 );
                }
                ArrayHelper.DeleteArrayElement( ref AC.AudioCategories, currentCategoryIndex );
                KeepChanges();
            }

            EditorGUILayout.EndHorizontal();

            if ( newCategoryIndex != currentCategoryIndex )
            {
                currentCategoryIndex = newCategoryIndex;
                currentItemIndex = 0;
                currentSubitemIndex = 0;
                _ValidateCurrentItemIndex();
                _ValidateCurrentSubItemIndex();
            }


            AudioCategory curCat = currentCategory;

            if ( curCat != null )
            {
                if ( curCat.audioController == null )
                {
                    curCat.audioController = AC;
                }
                if ( justCreatedNewCategory )
                {
                    SetFocusForNextEditableField();
                }
                EditString( ref curCat.Name, "Name", curCat.Name == _nameForNewCategoryEntry ? textAttentionStyle : null );

                float volTmp = curCat.Volume;
                EditFloat01( ref volTmp, "Volume", " %" );
                curCat.Volume = volTmp;

                EditPrefab( ref curCat.AudioObjectPrefab, "Audio Object Prefab Override", "Use different Audio Object prefabs if you want to specify different parameters such as the volume rolloff etc. per category" );

                int selectedParentCategoryIndex;

                var catList = _GenerateCategoryListIncludingNone( out selectedParentCategoryIndex, curCat.parentCategory );

                int newIndex = Popup( "Parent Category", selectedParentCategoryIndex, catList, "The effective volume of a category is multiplied with the volume of the parent category." );
                if ( newIndex != selectedParentCategoryIndex )
                {
                    KeepChanges();
                    
                    if ( newIndex <= 0 )
                    {
                        curCat.parentCategory = null;
                    }
                    else
                        curCat.parentCategory = _GetCategory( catList[ newIndex ] );
                }

                int itemCount = currentItemCount;
                _ValidateCurrentItemIndex();

                /*if ( GUILayout.Button( "Add all items in this category to playlist" ) )
                {
                    for ( int i = 0; i < itemCount; i++ )
                    {
                        ArrayHelper.AddArrayElement( ref AC.musicPlaylist, curCat.AudioItems[i].Name );
                    }
                    currentPlaylistIndex = AC.musicPlaylist.Length - 1;
                    KeepChanges();
                }*/



                VerticalSpace();

                AudioItem curItem = currentItem;

                if ( itemFoldout = EditorGUILayout.Foldout( itemFoldout, "Audio Item Settings", foldoutStyle ) )
                {
                    EditorGUILayout.BeginHorizontal();
                    if ( GUILayout.Button( "Add selected audio clips", EditorStyles.miniButton ) )
                    {
                        AudioClip[ ] audioClips = GetSelectedAudioclips();
                        if ( audioClips.Length > 0 )
                        {
                            int firstIndex = itemCount;
                            currentItemIndex = firstIndex;
                            foreach ( AudioClip audioClip in audioClips )
                            {
                                ArrayHelper.AddArrayElement( ref curCat.AudioItems );
                                AudioItem audioItem = curCat.AudioItems[ currentItemIndex ];
                                audioItem.Name = audioClip.name;
                                ArrayHelper.AddArrayElement( ref audioItem.subItems ).Clip = audioClip;
                                currentItemIndex++;
                            }
                            currentItemIndex = firstIndex;
                            KeepChanges();
                        }
                    }

                    GUILayout.Label( "use inspector lock!" );
                    EditorGUILayout.EndHorizontal();

                    // AudioItems

                    EditorGUILayout.BeginHorizontal();

                    int newItemIndex = PopupWithStyle( "Item", currentItemIndex, GetItemNames(), popupStyleColored );
                    bool justCreatedNewItem = false;


                    if ( GUILayout.Button( "+", GUILayout.Width( 30 ) ) )
                    {
                        bool lastEntryIsNew = false;

                        if ( itemCount > 0 )
                        {
                            lastEntryIsNew = curCat.AudioItems[ currentItemIndex ].Name == _nameForNewAudioItemEntry;
                        }

                        if ( !lastEntryIsNew )
                        {
                            newItemIndex = curCat.AudioItems != null ? curCat.AudioItems.Length : 0;
                            ArrayHelper.AddArrayElement( ref curCat.AudioItems );
                            curCat.AudioItems[ newItemIndex ].Name = _nameForNewAudioItemEntry;
                            justCreatedNewItem = true;
                            KeepChanges();
                        }
                    }

                    if ( GUILayout.Button( "-", GUILayout.Width( 30 ) ) && itemCount > 0 )
                    {
                        if ( currentItemIndex < curCat.AudioItems.Length - 1 )
                        {
                            newItemIndex = currentItemIndex;
                        }
                        else
                        {
                            newItemIndex = Mathf.Max( currentItemIndex - 1, 0 );
                        }
                        ArrayHelper.DeleteArrayElement( ref curCat.AudioItems, currentItemIndex );
                        KeepChanges();
                    }



                    if ( newItemIndex != currentItemIndex )
                    {
                        currentItemIndex = newItemIndex;
                        currentSubitemIndex = 0;
                        _ValidateCurrentSubItemIndex();
                    }

                    curItem = currentItem;
                  
                    EditorGUILayout.EndHorizontal();

                    if ( curItem != null )
                    {
                        GUILayout.BeginHorizontal();
                        if ( justCreatedNewItem )
                        {
                            SetFocusForNextEditableField();
                        }

                        bool isNewDummyName = curItem.Name == _nameForNewAudioItemEntry;

                        string originalName = curItem.Name;

                        if ( EditString( ref curItem.Name, "Name", isNewDummyName ? textAttentionStyle : null, "You must specify a unique name here (=audioID). This is the ID used in the script code to play this audio item."  ) )
                        {
                            if ( !isNewDummyName )
                            {
                                _RenamePlaylistEntries( originalName, curItem.Name );
                            }
                        }


                        /*if ( GUILayout.Button( "Add to playlist" ) )
                        {
                            AddToPlayList( curItem.Name );
                        }*/

                        GUILayout.EndHorizontal();

                        int newItemCategoryIndex = Popup( "Move to Category", currentCategoryIndex, GetCategoryNames() );

                        if ( newItemCategoryIndex != currentCategoryIndex )
                        {
                            var newCat = AC.AudioCategories[ newItemCategoryIndex ];
                            var oldCat = currentCategory;
                            ArrayHelper.AddArrayElement( ref newCat.AudioItems, curItem );
                            ArrayHelper.DeleteArrayElement( ref oldCat.AudioItems, currentItemIndex );
                            currentCategoryIndex = newItemCategoryIndex;
                            KeepChanges();
                            AC.InitializeAudioItems();
                            currentItemIndex = newCat.AudioItems.Length - 1;
                        }

                        if ( EditFloat01( ref curItem.Volume, "Volume", " %" ) )
                        {
                            _AdjustVolumeOfAllAudioItems( curItem, null );
                        }
                        EditFloat( ref curItem.Delay, "Delay", "sec", "Delays the playback" );
                        EditFloat( ref curItem.MinTimeBetweenPlayCalls, "Min Time Between Play", "sec", "If the same audio item gets played multiple times within this time frame the playback is skipped. This can prevent unwanted audio artifacts." );
                        EditInt( ref curItem.MaxInstanceCount, "Max Instance Count", "", "Sets the maximum number of simultaneously playing audio files of this particular audio item. If the maximum number would be exceeded, the oldest playing audio gets stopped." );

                        EditBool( ref curItem.DestroyOnLoad, "Stop When Scene Loads", "If disabled, this audio item will continue playing even if a different scene is loaded." );

                        if ( (int) curItem.Loop == 3 ) // deprecated gapless looping
                        {
                            curItem.Loop = AudioItem.LoopMode.LoopSequence;
                            KeepChanges();
                        }

                        curItem.Loop = (AudioItem.LoopMode)EnumPopup( "Loop Mode", curItem.Loop, "The Loop mode determines how the audio subitems are looped. \n'LoopSubitem' means that the chosen sub-item will loop. \n'LoopSequence' means that one subitem is played after the other. In which order the subitems are chosen depends on the subitem pick mode." );

                        if ( curItem.Loop == AudioItem.LoopMode.LoopSequence || 
                             curItem.Loop == AudioItem.LoopMode.PlaySequenceAndLoopLast ||
                             curItem.Loop == AudioItem.LoopMode.IntroLoopOutroSequence )
                        {
                            EditInt( ref curItem.loopSequenceCount, "   Stop after subitems", "", "Playing will stop after this number of different subitems were played. Specify zero to play endlessly in LoopSequence mode or play all sub-items in <c>PlaySequenceAndLoopLast</c> and <c>IntroLoopOutroSequence</c> mode" );
                            EditFloat( ref curItem.loopSequenceOverlap, "   Overlap", "sec", "Positive values mean that subitems will play overlapping, negative values mean that a delay is inserted before playing the next subitem in the 'LoopSequence'." );
                            EditFloat( ref curItem.loopSequenceRandomDelay, "   Random Delay", "sec", "A random delay between 0 and this value will be added between two subsequent subitems. Can be combined with the 'Overlap' value." );
							EditFloat01( ref curItem.loopSequenceRandomVolume, "   Random Volume", "±%", "A random volume value % will be added to each subitem played in the 'LoopSequence'. Will be combined with subitem random volume value." );
							EditFloat( ref curItem.loopSequenceRandomPitch, "   Random Pitch", "±semitone", "A random pitch between 0 and this value will be added to each subitem played in the 'LoopSequence'. Will be combined with subitem random pitch value." );
                        }

#if UNITY_5_OR_NEWER
                        EditFloat01( ref curItem.spatialBlend, "Spatial Blend", "%", "0% = 2D  100% = 3D" );
#endif

                        EditBool( ref curItem.overrideAudioSourceSettings, "Override AudioSource Settings" );

                        if ( curItem.overrideAudioSourceSettings )
                        {
                            //EditorGUI.indentLevel++;

                            EditFloat( ref curItem.audioSource_MinDistance, "   Min Distance", "", "Overrides the 'Min Distance' parameter in the AudioSource settings of the AudioObject prefab (for 3d sounds)" );
                            EditFloat( ref curItem.audioSource_MaxDistance, "   Max Distance", "", "Overrides the 'Max Distance' parameter in the AudioSource settings of the AudioObject prefab (for 3d sounds)" );

                            //EditorGUI.indentLevel--;
                        }

                        if ( curItem.Loop == AudioItem.LoopMode.IntroLoopOutroSequence )
                        {
                            EditorGUI.BeginDisabledGroup( true );
                            curItem.SubItemPickMode = AudioPickSubItemMode.StartLoopSequenceWithFirst;
                        }
                        curItem.SubItemPickMode = (AudioPickSubItemMode)EnumPopup( "Pick Subitem Mode", curItem.SubItemPickMode, "Determines which subitem is chosen when the audio item is played." );
                        if ( curItem.Loop == AudioItem.LoopMode.IntroLoopOutroSequence )
                        {
                            EditorGUI.EndDisabledGroup();
                        }
                        //EditString( ref curItem.PlayAdditional, "Play Additional" );
                        //EditString( ref curItem.PlayInstead, "Play Instead" );

                        EditorGUILayout.BeginHorizontal();

                        GUILayout.Label( "" );

                        bool isItemNotLooping = ( curItem != null && curItem.Loop == AudioItem.LoopMode.DoNotLoop );

#if UNITY_3_x
                        GUI.enabled = isItemNotLooping;
#else
                        GUI.enabled = _IsAudioControllerInPlayMode() &&  isItemNotLooping;
#endif

                        if ( GUILayout.Button( "Play", GUILayout.Width( 60 ) ) && curItem != null )
                        {
                            if ( _IsAudioControllerInPlayMode() )
                            {
                                AudioController.Play( curItem.Name );
                            }
                            else
                            {
                                if ( Application.platform == RuntimePlatform.OSXEditor )
                                {
                                    Debug.Log( _playNotSupportedOnMac );
                                }
                                else
                                {
                                    AC.InitializeAudioItems();
                                    Debug.Log( _playWithInspectorNotice );
                                    AC.PlayAudioItem( curItem, 1, Vector3.zero, null, 0, 0, true, null );
                                }
                            }
                        }

                        GUI.enabled = true;


                        EditorGUILayout.EndHorizontal();

                        VerticalSpace();

                        int subItemCount = curItem.subItems != null ? curItem.subItems.Length : 0;
                        currentSubitemIndex = Mathf.Clamp( currentSubitemIndex, 0, subItemCount - 1 );
                        AudioSubItem subItem = currentSubItem;

                        if ( subitemFoldout = EditorGUILayout.Foldout( subitemFoldout, "Audio Sub-Item Settings", foldoutStyle ) )
                        {
                            EditorGUILayout.BeginHorizontal();
                            if ( GUILayout.Button( "Add selected audio clips", EditorStyles.miniButton ) )
                            {
                                AudioClip[ ] audioClips = GetSelectedAudioclips();
                                if ( audioClips.Length > 0 )
                                {
                                    int firstIndex = subItemCount;
                                    currentSubitemIndex = firstIndex;
                                    foreach ( AudioClip audioClip in audioClips )
                                    {
                                        ArrayHelper.AddArrayElement( ref curItem.subItems ).Clip = audioClip;
                                        currentSubitemIndex++;
                                    }
                                    currentSubitemIndex = firstIndex;
                                    KeepChanges();
                                }
                            }
                            GUILayout.Label( "use inspector lock!" );
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.BeginHorizontal();

                            currentSubitemIndex = PopupWithStyle( "SubItem", currentSubitemIndex, GetSubitemNames(), popupStyleColored );

                            if ( GUILayout.Button( "+", GUILayout.Width( 30 ) ) )
                            {
                                bool lastEntryIsNew = false;

                                AudioSubItemType curSubItemType = AudioSubItemType.Clip;

                                if ( subItemCount > 0 )
                                {
                                    curSubItemType = curItem.subItems[ currentSubitemIndex ].SubItemType;
                                    if ( curSubItemType == AudioSubItemType.Clip )
                                    {
                                        lastEntryIsNew = curItem.subItems[ currentSubitemIndex ].Clip == null;
                                    }
                                    if ( curSubItemType == AudioSubItemType.Item )
                                    {
                                        lastEntryIsNew = curItem.subItems[ currentSubitemIndex ].ItemModeAudioID == null ||
                                                         curItem.subItems[ currentSubitemIndex ].ItemModeAudioID.Length == 0;
                                    }
                                }

                                if ( !lastEntryIsNew )
                                {
                                    currentSubitemIndex = subItemCount;
                                    ArrayHelper.AddArrayElement( ref curItem.subItems );
                                    curItem.subItems[ currentSubitemIndex ].SubItemType = curSubItemType;
                                    KeepChanges();
                                }
                            }

                            if ( GUILayout.Button( "-", GUILayout.Width( 30 ) ) && subItemCount > 0 )
                            {
                                ArrayHelper.DeleteArrayElement( ref curItem.subItems, currentSubitemIndex );
                                if ( currentSubitemIndex >= curItem.subItems.Length )
                                {
                                    currentSubitemIndex = Mathf.Max( curItem.subItems.Length - 1, 0 );
                                }
                                KeepChanges();
                            }
                            EditorGUILayout.EndHorizontal();

                            subItem = currentSubItem;

                            if ( subItem != null )
                            {
                                _SubitemTypePopup( subItem );
                                

                                if ( subItem.SubItemType == AudioSubItemType.Item )
                                {
                                    _DisplaySubItem_Item( subItem );

                                }
                                else
                                {
                                    _DisplaySubItem_Clip( subItem, subItemCount, curItem );
                                }
                            } 
                        }
                    }
                }
            }
        }

        VerticalSpace();

        EditorGUILayout.BeginHorizontal();

        if ( GUILayout.Button( "Show Audio Log" ) )
        {
            var win = EditorWindow.GetWindow( typeof( AudioLogView ) );
            win.Show();
        }

        if ( GUILayout.Button( "Show Item Overview" ) )
        {
            AudioItemOverview win = EditorWindow.GetWindow( typeof( AudioItemOverview ) ) as AudioItemOverview;
            win.Show( AC );
        }
       
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        if ( EditorApplication.isPlaying )
        {
            EditorGUILayout.BeginHorizontal();
            if ( GUILayout.Button( "Stop All Sounds" ) )
            {
                if ( EditorApplication.isPlaying && AudioController.DoesInstanceExist() )
                {
                    AudioController.StopAll();
                }
            }
            if ( GUILayout.Button( "Stop Music Only" ) )
            {
                if ( EditorApplication.isPlaying && AudioController.DoesInstanceExist() )
                {
                    AudioController.StopMusic();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        GUILayout.Label( string.Format( "----- ClockStone Audio Toolkit v{0} -----  ", AudioController.AUDIO_TOOLKIT_VERSION ), centeredTextStyle );

        EndInspectorGUI();

        //Debug.Log( "currentCategoryIndex: " + currentCategoryIndex );
    }

    private void _RenamePlaylistEntries( string originalName, string newName )
    {
        if ( AC.musicPlaylist == null ) return;

        for ( int i = 0; i < AC.musicPlaylist.Length; i++ )
        {
            if ( AC.musicPlaylist[ i ] == originalName )
            {
                AC.musicPlaylist[ i ] = newName;
            }
        }
    }    

    private string[] _GenerateCategoryListIncludingNone( out int selectedParentCategoryIndex, AudioCategory selectedAudioCategory )
    {
        string[] names;
        selectedParentCategoryIndex = 0;

        if ( AC.AudioCategories != null )
        {
            names = new string[ AC.AudioCategories.Length ];

            int index = 1;

            var curCat = currentCategory;

            for (int i=0; i< AC.AudioCategories.Length; i++)
            {
                if ( _IsCategoryChildOf( AC.AudioCategories[ i ], curCat ) ) // prevent loops in tree
                {
                    continue;
                }
                names[index] = AC.AudioCategories[i].Name;
                if ( selectedAudioCategory == AC.AudioCategories[ i ] )
                {
                    selectedParentCategoryIndex = index;
                }

                index++;
                if ( index == names.Length )
                {
                    break; // in case currentCategory is not found
                }
            }

            if ( index < names.Length )
            {
                var newNames = new string[ index ];
                Array.Copy( names, newNames, index );
                names = newNames;
            }
        }
        else
        {
            names = new string[ 1 ];
        }

        names[ 0 ] = "*none*";
        return names;
    }

    bool _IsCategoryChildOf( AudioCategory toTest, AudioCategory parent )
    {
        var cat = toTest;
        while ( cat != null )
        {
            if ( cat.audioController == null )
            {
                cat.audioController = AC;
            }

            if ( cat == parent ) return true;

            cat = cat.parentCategory;
        }
        return false;
    }

    private bool _IsAudioControllerInPlayMode()
    {
        return EditorApplication.isPlaying && AudioController.DoesInstanceExist();
    }
    private void _ValidateCurrentCategoryIndex()
    {
        int categoryCount = currentCategoryCount;
        if ( categoryCount > 0 ) currentCategoryIndex = Mathf.Clamp( currentCategoryIndex, 0, categoryCount - 1 );
        else currentCategoryIndex = -1;
    }

    private void _ValidateCurrentSubItemIndex()
    {
        int subitemCount = currentSubItemCount;
        if ( subitemCount > 0 ) currentSubitemIndex = Mathf.Clamp( currentSubitemIndex, 0, subitemCount - 1 );
        else currentSubitemIndex = -1;
    }

    private void _ValidateCurrentItemIndex()
    {
        int itemCount = currentItemCount;
        if ( itemCount > 0 ) currentItemIndex = Mathf.Clamp( currentItemIndex, 0, itemCount - 1 );
        else currentItemIndex = -1;
    }

    private void _SubitemTypePopup( AudioSubItem subItem )
    {
        var typeNames = new string[ 2 ];
        typeNames[ 0 ] = "Single Audio Clip";
        typeNames[ 1 ] = "Other Audio Item";

        int curIndex = 0;
        switch( subItem.SubItemType )
        {
        case AudioSubItemType.Clip: curIndex = 0; break;
        case AudioSubItemType.Item: curIndex = 1; break;
        }

        switch( Popup( "SubItem Type", curIndex, typeNames ) )
        {
        case 0: subItem.SubItemType = AudioSubItemType.Clip; break;
        case 1: subItem.SubItemType = AudioSubItemType.Item; break;
        }

        //subItem.SubItemType = (AudioSubItemType) EnumPopup( "SubItem Type", subItem.SubItemType );
    }

    public void AddToPlayList( string name )
    {
        ArrayHelper.AddArrayElement( ref AC.musicPlaylist, name );
        currentPlaylistIndex = AC.musicPlaylist.Length - 1;
        KeepChanges();
    }

    protected void EditAudioClip( ref AudioClip clip, string label ) 
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label( label, styleLabel );
        clip = (AudioClip) EditorGUILayout.ObjectField( clip, typeof( AudioClip ), false );
        if ( clip )
        {
            EditorGUILayout.Space();
            GUILayout.Label( string.Format( "{0:0.0} sec", clip.length ), GUILayout.Width( 60 ) );
        }
        EditorGUILayout.EndHorizontal();
    }

    private void _DisplaySubItem_Clip( AudioSubItem subItem, int subItemCount, AudioItem curItem )
    {

        // AudioSubItems

        if ( subItem != null )
        {
            EditAudioClip( ref subItem.Clip, "Audio Clip" );

            if( EditFloat01( ref subItem.Volume, "Volume", " %" ) )
            {
                _AdjustVolumeOfAllAudioItems( curItem, subItem );
            }

            EditFloat01( ref subItem.RandomVolume, "Random Volume", "±%" );

            EditFloat( ref subItem.Delay, "Delay", "sec" );
            //EditFloatWithinRange( ref subItem.Pan2D, "Pan2D [left..right]", -1.0f, 1.0f);
            EditFloatPlusMinus1( ref subItem.Pan2D, "Pan2D", "%left/right" );
            if( _IsRandomItemMode( curItem.SubItemPickMode ) )
            {
                EditFloat01( ref subItem.Probability, "Probability", " %", "Choose a higher value (in comparison to the probability values of the other audio clips) to increase the probability for this clip when using a random subitem pick mode." );
                EditBool( ref subItem.DisableOtherSubitems, "Disable Other Subitmes", "If enabled all other subitmes which do not have this option enabled will not be played. Useful for testing specific subitmes within a large list of subitems." );
            }
            EditFloat( ref subItem.PitchShift, "Pitch Shift", "semitone" );
            EditFloat( ref subItem.RandomPitch, "Random Pitch", "±semitone" );
            EditFloat( ref subItem.RandomDelay, "Random Delay", "sec" );
            EditFloat( ref subItem.FadeIn, "Fade-in", "sec" );
            EditFloat( ref subItem.FadeOut, "Fade-out", "sec" );           
            EditFloat( ref subItem.ClipStartTime, "Start at", "sec" );
            EditFloat( ref subItem.ClipStopTime, "Stop at", "sec" );
            EditBool( ref subItem.RandomStartPosition, "Random Start Position", "Starts playing at a random position. Useful when looping." );
        }

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label( " " );

#if UNITY_3_x
#else
        GUI.enabled = _IsAudioControllerInPlayMode();
#endif 

        if ( GUILayout.Button( "Play", GUILayout.Width( 60 ) ) && subItem != null )
        {
            if ( _IsAudioControllerInPlayMode() )
            {
                var audioListener = AudioController.GetCurrentAudioListener();
                Vector3 pos;
                if ( audioListener != null )
                {
                    pos = audioListener.transform.position + audioListener.transform.forward;
                }
                else
                    pos = Vector3.zero;

                AudioController.Instance.PlayAudioSubItem( subItem, 1, pos, null, 0, 0, false, null );

            }
            else
            {
                if ( Application.platform == RuntimePlatform.OSXEditor )
                {
                    Debug.Log( _playNotSupportedOnMac );
                }
                else
                {
                    Debug.Log( _playWithInspectorNotice );
                    AC.InitializeAudioItems();
                    AC.PlayAudioSubItem( subItem, 1, Vector3.zero, null, 0, 0, true, null );
                }
            }
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        
    }

    private void _AdjustVolumeOfAllAudioItems( AudioItem curItem, AudioSubItem subItem )
    {
        if ( _IsAudioControllerInPlayMode() )
        {
            var audioObjs = AudioController.GetPlayingAudioObjects();
            
            foreach( var a in audioObjs )
            {
                if ( curItem != a.audioItem ) continue;
                if ( subItem != null )
                {
                    if ( subItem != a.subItem ) continue;
                }
                a.volumeItem = a.audioItem.Volume * a.subItem.Volume;
            }
        }
    }

    private bool _IsRandomItemMode( AudioPickSubItemMode audioPickSubItemMode )
    {
        switch ( audioPickSubItemMode )
        {
        case AudioPickSubItemMode.Random: return true;
        case AudioPickSubItemMode.RandomNotSameTwice: return true;
        case AudioPickSubItemMode.TwoSimultaneously: return true;
		case AudioPickSubItemMode.RandomNotSameTwiceOddsEvens: return true;
        }
        return false;
    }

    private string _ChooseItem( string label )
    {
        string[ ] possibleAudioIDs_withCategory = _GetPossibleAudioIDs( true, "Choose Audio Item..." );

        int selected = PopupWithStyle( label, 0, possibleAudioIDs_withCategory, styleChooseItem );
        if( selected != 0 )
        {
            string[ ] possibleAudioIDs = _GetPossibleAudioIDs( false, "Choose Audio Item..." );
            return possibleAudioIDs[ selected ];
        }
        return null;
    }

    private void _DisplaySubItem_Item( AudioSubItem subItem )
    {
        EditFloat01( ref subItem.Probability, "Probability", " %" );
        int audioIndex = 0;
        string[ ] possibleAudioIDs = _GetPossibleAudioIDs( false, "*undefined*" );
        string[ ] possibleAudioIDs_withCategory = _GetPossibleAudioIDs( true, "*undefined*" );

        if ( subItem.ItemModeAudioID != null && subItem.ItemModeAudioID.Length > 0 )
        {
            string idToSearch = subItem.ItemModeAudioID.ToLowerInvariant();

            for ( int i = 1; i < possibleAudioIDs.Length; i++ )
            {
                if ( possibleAudioIDs[ i ].ToLowerInvariant() == idToSearch )
                {
                    audioIndex = i; break;
                }
            }
        }

        bool wasUndefinedBefore = ( audioIndex == 0 );

        audioIndex = Popup( "AudioItem", audioIndex, possibleAudioIDs_withCategory );
        if ( audioIndex > 0 )
        {
            subItem.ItemModeAudioID = possibleAudioIDs[ audioIndex ];
        }
        else
        {
            if ( !wasUndefinedBefore )
            {
                subItem.ItemModeAudioID = null;
            }
        }
    }

    private string[] _GetPossibleAudioIDs( bool withCategoryName, string firstEntryName )
    {
        var audioIDs = new List<string>();
        audioIDs.Add( firstEntryName );
        if ( AC.AudioCategories != null )
        {
            foreach ( var category in AC.AudioCategories )
            {
                _GetAllAudioIDs( audioIDs, category, withCategoryName );
            }
        }
        return audioIDs.ToArray();
    }

    private void _GetAllAudioIDs( List<string> audioIDs, AudioCategory c, bool withCategoryName )
    {
        if ( c.AudioItems != null )
        {
            foreach ( var audioItem in c.AudioItems )
            {
                if ( audioItem.Name.Length > 0 )
                {
                    if ( withCategoryName )
                    {
                        audioIDs.Add( string.Format( "{0}/{1}", c.Name, audioItem.Name ) );
                    }
                    else
                        audioIDs.Add( audioItem.Name );
                }
            }
        }
    }

    private bool SwapArrayElements<T>( T[ ] array, int index1, int index2 )
    {
        if ( array == null || index1 < 0 || index2 < 0 || index1 >= array.Length || index2 >= array.Length )
        {
            return false;
        }

        T tmp = array[ index1 ];
        array[ index1 ] = array[ index2 ];
        array[ index2 ] = tmp;
        return true;
    }

    private void VerticalSpace()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }

    private string[] GetCategoryNames()
    {
        if ( AC.AudioCategories == null )
        {
            return new string[ 0 ];
        }
        var names = new string[ AC.AudioCategories.Length ];
        for( int i=0; i< AC.AudioCategories.Length; i++ )
        {
            names[ i ] = AC.AudioCategories[ i ].Name;

            if ( names[ i ] == _nameForNewCategoryEntry )
            {
                names[ i ] = "---";
            }
        }
        return names;
    }

    private string[] GetItemNames()
    {
        AudioCategory curCat = currentCategory;
        if ( curCat == null || curCat.AudioItems == null )
        {
            return new string[0];
        }

        var names = new string[ curCat.AudioItems.Length ];
        for ( int i = 0; i < curCat.AudioItems.Length; i++ )
        {
            names[ i ] = curCat.AudioItems[ i ] != null ? curCat.AudioItems[ i ].Name : "";

            if ( names[ i ] == _nameForNewAudioItemEntry )
            {
                names[ i ] = "---";
            }
        }
        return names;
    }

    private string[] GetSubitemNames()
    {
        AudioItem curItem = currentItem;
        if ( curItem == null || curItem.subItems == null )
        {
            return new string[ 0 ];
        }

        var names = new string[ curItem.subItems.Length ];
        for ( int i = 0; i < curItem.subItems.Length; i++ )
        {
            AudioSubItemType subitemType = curItem.subItems[ i ] != null ? curItem.subItems[ i ].SubItemType : AudioSubItemType.Clip;

            if ( subitemType == AudioSubItemType.Item )
            {
                names[ i ] = string.Format( "ITEM {0}: {1}", i, ( curItem.subItems[ i ].ItemModeAudioID ?? "*undefined*" ) );
            }
            else
            {
                names[ i ] = string.Format( "CLIP {0}: {1}", i, ( curItem.subItems[ i ] != null ? curItem.subItems[ i ].Clip ? curItem.subItems[ i ].Clip.name : "*unset*" : "" ) );
            }
        }
        return names;
    }

    private string[ ] GetPlaylistNames()
    {
        if ( AC.musicPlaylist == null )
        {
            return new string[ 0 ];
        }

        var names = new string[ AC.musicPlaylist.Length ];
        for ( int i = 0; i < AC.musicPlaylist.Length; i++ )
        {
            names[ i ] = string.Format( "{0}: {1}", i, AC.musicPlaylist[ i ] );
        }
        return names;
    }

    static AudioClip[ ] GetSelectedAudioclips()
    { 
        var objList = Selection.GetFiltered( typeof( AudioClip ), SelectionMode.DeepAssets );
        var clipList = new AudioClip[objList.Length];

        for ( int i = 0; i < objList.Length; i++ )
        {
            clipList[ i ] = (AudioClip) objList[ i ];
        }
         
        return clipList;
    }

    AudioCategory _GetCategory( string name )
    {
        foreach ( AudioCategory cat in AC.AudioCategories )
        {
            if ( cat.Name == name )
            {
                return cat;
            }
        }
        return null;
    }
}
