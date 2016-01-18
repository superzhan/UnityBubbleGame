using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class AudioItemOverview : EditorWindow
{
    [MenuItem( "Window/Audio Toolkit/Item Overview" )]
    static void ShowWindow()
    {
        EditorWindow.GetWindow( typeof( AudioItemOverview ) );
    }

    static Vector2 _scrollPos;

    AudioController _selectedAC;
    int _selectedACIndex;
    AudioController[ ] _audioControllerList;
    string[ ] _audioControllerNameList;

    string _searchString;

    public void Show( AudioController audioController )
    {
        _selectedAC = audioController;
        _FindAudioController();
        base.Show();
    }

    void OnGUI()
    {
        if ( !_selectedAC )
        {
            _selectedAC = _FindAudioController();
        }

        if ( _selectedAC == null && Selection.activeGameObject != null )
        {
            _selectedAC = Selection.activeGameObject.GetComponent<AudioController>();
        }

        if ( _audioControllerNameList == null )
        {
            _FindAudioController();

            if ( _audioControllerNameList == null && _selectedAC != null ) // can happen if AC was selected by Show( AC )
            {
                _audioControllerNameList = new string[ 1 ] { _GetPrefabName( _selectedAC ) };
            }
        }

        if ( !_selectedAC )
        {
            EditorGUILayout.LabelField( "No AudioController found!" );
            return;
        }

        // header

        int buttonSize = 80;

        GUIStyle headerStyle = new GUIStyle( EditorStyles.boldLabel );
        GUIStyle headerStyleButton = new GUIStyle( EditorStyles.popup );
        //headerStyleButton.fixedWidth = 350;

        UnityEngine.Color acColor;

        bool isDarkSkin = headerStyleButton.normal.textColor.grayscale > 0.5f;

        if ( isDarkSkin )
        {
            acColor = new Color( 0.9f, 0.9f, 0.5f );
        }
        else
            acColor = new Color( 0.6f, 0.1f, 0.0f );

        headerStyleButton.normal.textColor = acColor;
        headerStyleButton.focused.textColor = acColor;
        headerStyleButton.active.textColor = acColor;
        headerStyleButton.hover.textColor = acColor;

        GUIStyle styleButton = new GUIStyle( EditorStyles.miniButton );
        styleButton.fixedWidth = buttonSize;
        EditorGUILayout.BeginHorizontal();

        int newACIndex = EditorGUILayout.Popup( _selectedACIndex, _audioControllerNameList, headerStyleButton );
        if ( newACIndex != _selectedACIndex )
        {
            _selectedACIndex = newACIndex;
            _selectedAC = _audioControllerList[ _selectedACIndex ];
            _SelectCurrentAudioController();
        }

        if ( _searchString == null )
        {
            _searchString = "";
        }

        _searchString = EditorGUILayout.TextField( "                  search item: ", _searchString );
        EditorGUILayout.EndHorizontal();

        GUIStyle styleEmptyButton = new GUIStyle( styleButton );

        styleEmptyButton.normal = headerStyle.normal;
        styleEmptyButton.focused = headerStyle.focused;
        styleEmptyButton.active = headerStyle.active;
        styleEmptyButton.hover = headerStyle.hover;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Button( "      ", styleEmptyButton );
        EditorGUILayout.LabelField( "Category", headerStyle );
        EditorGUILayout.LabelField( "Item", headerStyle );
        EditorGUILayout.LabelField( "Sub Item", headerStyle );


        EditorGUILayout.EndHorizontal();

        // data

        _scrollPos = EditorGUILayout.BeginScrollView( _scrollPos );

        int lastCategoryIndex = -1;

        string sameTypeString = ".";

        if ( _selectedAC.AudioCategories != null )
        {
            for ( int categoryIndex=0; categoryIndex < _selectedAC.AudioCategories.Length; categoryIndex++ )
            {
                var category = _selectedAC.AudioCategories[categoryIndex];
                if ( category.AudioItems == null ) continue;

                var sortedAudioItems = category.AudioItems.OrderBy( x => x.Name ).ToArray();
                int lastItemIndex = -1;

                for ( int itemIndex = 0; itemIndex < sortedAudioItems.Length; itemIndex++ )
                {
                    var item = sortedAudioItems[ itemIndex ];

                    if ( !string.IsNullOrEmpty( _searchString ) )
                    {
                        if( !item.Name.ToLowerInvariant().Contains( _searchString.ToLowerInvariant() ) )
                        {
                            continue;
                        }
                    }

                    if ( item.subItems == null )
                    {
                        lastItemIndex = itemIndex;
                        continue;
                    }
                    for ( int subitemIndex = 0; subitemIndex < item.subItems.Length; subitemIndex++ )
                    {
                        var subItem = item.subItems[ subitemIndex ];
                        EditorGUILayout.BeginHorizontal();

                        if ( GUILayout.Button( "Select", styleButton ) )
                        {
                            _selectedAC._currentInspectorSelection.currentCategoryIndex = categoryIndex;
                            _selectedAC._currentInspectorSelection.currentItemIndex = Array.FindIndex( category.AudioItems, x => x.Name == item.Name );
                            _selectedAC._currentInspectorSelection.currentSubitemIndex = subitemIndex;
                            _SelectCurrentAudioController();
                        }

                        EditorGUILayout.LabelField( ( categoryIndex != lastCategoryIndex ) ? category.Name : sameTypeString );
                        EditorGUILayout.LabelField( ( itemIndex != lastItemIndex || categoryIndex != lastCategoryIndex ) ? item.Name : sameTypeString );

                        string subItemName;
                        if ( subItem.SubItemType == AudioSubItemType.Clip )
                        {
                            if ( subItem.Clip != null )
                            {
                                subItemName = "CLIP: " + subItem.Clip.name;
                            }
                            else
                            {
                                subItemName = "CLIP: *unset*";

                            }
                        }
                        else
                            subItemName = "ITEM: " + subItem.ItemModeAudioID;

                        EditorGUILayout.LabelField( subItemName );
                        EditorGUILayout.EndHorizontal();

                        lastItemIndex = itemIndex;
                        lastCategoryIndex = categoryIndex;

                    }
                }
            }
        }
        
        EditorGUILayout.EndScrollView();

    }

    private void _SelectCurrentAudioController()
    {
        var gos = new GameObject[ 1 ];
        gos[ 0 ] = _selectedAC.gameObject;
        Selection.objects = gos;
    }

    private AudioController _FindAudioController()
    {
        _audioControllerList = FindObjectsOfType( typeof( AudioController ) ) as AudioController[];
        if ( _audioControllerList != null && _audioControllerList.Length > 0 )
        {
            _audioControllerNameList = new string[ _audioControllerList.Length ];
            _selectedACIndex = -1;
            for ( int i = 0; i < _audioControllerList.Length; i++ )
            {
                _audioControllerNameList[ i ] = _audioControllerList[ i ].name;
                if ( _selectedAC == _audioControllerList[ i ] )
                {
                    _selectedACIndex = i;
                }
            }
            if ( _selectedACIndex == -1 )
            {
                if ( _selectedAC != null )
                {
                    ArrayHelper.AddArrayElement<string>( ref _audioControllerNameList, _GetPrefabName( _selectedAC ) );
                    ArrayHelper.AddArrayElement<AudioController>( ref _audioControllerList, _selectedAC );
                    _selectedACIndex = _audioControllerNameList.Length -1;
                }
                else
                {
                    _selectedACIndex = 0;
                }
            }

            if ( _selectedACIndex >= 0 )
            {
                return _audioControllerList[ _selectedACIndex ];
            }
            else
            {
                return null;
            }
        }
        return null;
    }

    private string _GetPrefabName( AudioController _selectedAC )
    {
        return "PREFAB: " + _selectedAC.name;
    }
}
