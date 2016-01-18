using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper class to support HasSet for Flash export
/// </summary>
/// <typeparam name="T"></typeparam>
#if UNITY_FLASH
public class HashSet_Flash<T> : IEnumerable
{
    Dictionary< T, T> _dict = new Dictionary<T,T>();

    public bool Add( T obj )
    {
        try
        {
            _dict.Add( obj, obj );
        } catch( SystemException )
        {
             return false;
        }
        return true;
    }

    public bool Remove( T obj )
    {
        return  _dict.Remove( obj );
    }

    public int Count 
    {
        get
        {
            return _dict.Count;
        }
    }

    public bool Contains( T obj )
    {
        return _dict.ContainsKey( obj );
    }

    public IEnumerator GetEnumerator()
    {
        return new Enumerator( _dict.GetEnumerator() );
    }

    class Enumerator : IEnumerator
    {
        IEnumerator _dictEnumerator;

        public Enumerator( IEnumerator dictEnumerator )
        {
            _dictEnumerator = dictEnumerator;
        }

        public object Current
        {
            get { return ((KeyValuePair<T,T>)_dictEnumerator.Current).Value; }
        }

        public bool MoveNext()
        {
            return _dictEnumerator.MoveNext();
        }

        public void Reset()
        {
            _dictEnumerator.Reset();
        }
    }
}
#else
public class HashSet_Flash<T> : HashSet<T>
{
}
#endif