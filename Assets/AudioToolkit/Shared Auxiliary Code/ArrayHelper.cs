using System;
using UnityEngine;

#pragma warning disable 1591 // undocumented XML code warning

static public class ArrayHelper
{
#if !UNITY_FLASH || UNITY_EDITOR
    static public T AddArrayElement<T>( ref T[] array ) where T : new()
    {
        return AddArrayElement<T>( ref array, new T() );
    }

    static public T AddArrayElement<T>( ref T[] array, T elToAdd )
    {
        if ( array == null )
        {
            array = new T[ 1 ];
            array[ 0 ] = elToAdd;
            return elToAdd;
        }

        var newArray = new T[ array.Length + 1 ];
        array.CopyTo( newArray, 0 );
        newArray[ array.Length ] = elToAdd;
        array = newArray;
        return elToAdd;
    }

    static public void DeleteArrayElement<T>( ref T[] array, int index )
    {
        if ( index >= array.Length || index < 0 )
        {
            Debug.LogWarning( "invalid index in DeleteArrayElement: " + index );
            return;
        }
        var newArray = new T[ array.Length - 1 ];
        int i;
        for ( i = 0; i < index; i++ )
        {
            newArray[ i ] = array[ i ];
        }
        for ( i = index + 1; i < array.Length; i++ )
        {
            newArray[ i - 1 ] = array[ i ];
        }
        array = newArray;
    }
#endif
}

