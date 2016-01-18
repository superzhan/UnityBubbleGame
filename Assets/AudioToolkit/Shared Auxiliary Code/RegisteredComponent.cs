/*************************************************************************
 *           Registered Component (c) by ClockStone 2011                    *
 * 
 * A component derived from this class will get registered in a global 
 * component reference list. This way one can receive a list of 
 * registered components much faster than by using GameObject.FindObjectsOfType
 * 
 * Usage:
 * 
 * Derive your component class from RegisteredComponent instead of
 * MonoBehaviour.
 * 
 * Use RegisteredComponentController.GetAllOfType<MyType>() to retrieve a
 * list of all present components of type MyType. Note that this will also 
 * return all references of classes derived from MyType.
 * 
 * Works with PoolableObject if Awake and OnDestroy messages are sent.
 * 
 * ***********************************************************************
*/

#if UNITY_FLASH || UNITY_METRO
#define REDUCED_REFLECTION
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 1591 // undocumented XML code warning

public interface IRegisteredComponent
{
    Type GetRegisteredComponentBaseClassType();
}


/// <summary>
/// Derive your MonoBehaviour class from RegisteredComponent and all references to instances of this
/// component will be saved in an internal array. Use <see cref="RegisteredComponentController.GetAllOfType()"/>
/// to retrieve this array, which is much faster than using Unity's GameObject.FindObjectsOfType() function.
/// </summary>
public abstract class RegisteredComponent : MonoBehaviour, IRegisteredComponent
{
    bool isRegistered = false;
    bool isUnregistered = false;

    // redundant code with RegisteredComponentEx !!

    protected virtual void Awake()
    {
        if ( !isRegistered )
        {
            RegisteredComponentController._Register( this );
            isRegistered = true;
            isUnregistered = false;
        }
        else
            Debug.LogWarning( "RegisteredComponent: Awake() / OnDestroy() not correctly called. Object: " + name );
    }

    protected virtual void OnDestroy()
    {
        if ( isRegistered && !isUnregistered )
        {
            RegisteredComponentController._Unregister( this );
            isRegistered = false;
            isUnregistered = true;
        }
        else
        {
            bool alreadyUnregisteredProperly = !isRegistered && isUnregistered;

            if ( !alreadyUnregisteredProperly ) // for poolable objects OnDestroy() can get called multiple times
            {
                Debug.LogWarning( "RegisteredComponent: Awake() / OnDestroy() not correctly called. Object: " + name + " isRegistered:" + isRegistered + " isUnregistered:" + isUnregistered );
            }
        }
    }

    public Type GetRegisteredComponentBaseClassType()
    {
        return typeof( RegisteredComponent );
    }
}

/// <summary>
/// This controller provides fast access to all currently existing RegisteredComponent instances. 
/// </summary>
/// <remarks>
/// The function <see cref="RegisteredComponentController.GetAllOfType()"/> is understood as a replacement for Unity's 
/// slow GameObject.FindObjectsOfType() function.
/// </remarks>
static public class RegisteredComponentController
{
    public class InstanceContainer : HashSet_Flash<IRegisteredComponent>
    {

    }

    static Dictionary<Type, InstanceContainer> _instanceContainers = new Dictionary<Type, InstanceContainer>();

#if !REDUCED_REFLECTION
    /// <summary>
    /// Retrieves an array of all currently existing instances of the class <c>T</c>, 
    /// where <c>T</c> must be a <see cref="RegisteredComponent"/>
    /// </summary>
    /// <typeparam name="T">a class derived from <see cref="RegisteredComponent"/></typeparam>
    /// <returns>
    /// The array of instances.
    /// </returns>
    static public T[] GetAllOfType<T>() where T : IRegisteredComponent
    {
        InstanceContainer container;

        if ( !_instanceContainers.TryGetValue( typeof( T ), out container ) )
        {
            return new T[ 0 ];
        }

        var array = new T[ container.Count ];
        int count = 0;

        foreach ( IRegisteredComponent cpnt in container )
        {
            array[ count++ ] = ( T ) cpnt;
        }

        return array;
    }
#endif

    /// <summary>
    /// Retrieves an array of all currently existing instances of the class with type <c>type</c>, 
    /// (type must be a <see cref="RegisteredComponent"/> )
    /// </summary>
    /// <param name="type">The type of the instances to be retrieved</param>
    /// <returns>
    /// The array of instances.
    /// </returns>
    /// <remarks>
    /// Use this function instead of GetAllOfType&lt;T&gt; if you need Flash compatibility.
    /// </remarks>
    static public object[] GetAllOfType( Type type )  // Flash compatible version
    {
        InstanceContainer container;

        if ( !_instanceContainers.TryGetValue( type, out container ) )
        {
            return new object[ 0 ];
        }

        var array = new object[ container.Count ];
        int count = 0;

        foreach ( IRegisteredComponent cpnt in container )
        {
            array[ count++ ] = cpnt;
        }

        return array;
    }

    /// <summary>
    /// Return the number of all currently existing instances of the class <c>T</c>, 
    /// where <c>T</c> must be a <see cref="RegisteredComponent"/>
    /// </summary>
    /// <typeparam name="T">a class derived from <see cref="RegisteredComponent"/></typeparam>
    /// <returns>
    /// The number of instances.
    /// </returns>
    static public int InstanceCountOfType<T>() where T : IRegisteredComponent
    {
        InstanceContainer container;

        if ( !_instanceContainers.TryGetValue( typeof( T ), out container ) )
        {
            return 0;
        }

        return container.Count;
    }


    static private InstanceContainer _GetInstanceContainer( Type type )
    {
        InstanceContainer container;

        if ( _instanceContainers.TryGetValue( type, out container ) )
        {
            return container;
        }
        else
        {
            container = new InstanceContainer();
            _instanceContainers.Add( type, container );
        }

        return container;
    }

    static private void _RegisterType( IRegisteredComponent component, Type type )
    {
        InstanceContainer container = _GetInstanceContainer( type );
        if ( !container.Add( component ) )
        {
            Debug.LogError( "RegisteredComponentController error: Tried to register same instance twice" );
        }

        //Debug.Log( "Registered " + type.Name + ": " + component.gameObject.name );
    }

    static internal void _Register( IRegisteredComponent component )
    {
        Type type = component.GetType();

#if REDUCED_REFLECTION
        _RegisterType( component, type ); // BaseType not supported by Flash - so only top level class is registered!!
#else
        do
        {
            _RegisterType( component, type );
            type = type.BaseType;

        } while ( type != component.GetRegisteredComponentBaseClassType() );
#endif

        //Debug.Log( "Registered " + component.GetType().Name + ": " + component.gameObject.name );
    }

    static internal void _UnregisterType( IRegisteredComponent component, Type type )
    {
        InstanceContainer container = _GetInstanceContainer( type );
        if ( !container.Remove( component ) )
        {
            Debug.LogError( "RegisteredComponentController error: Tried to unregister unknown instance" );
        }

        //Debug.Log( "Unregistered " + type.Name + ": " + component.gameObject.name );
    }

    static internal void _Unregister( IRegisteredComponent component )
    {
        Type type = component.GetType();

#if REDUCED_REFLECTION
        _UnregisterType( component, type ); // BaseType not supported by Flash !!
#else
        do
        {
            _UnregisterType( component, type );

            type = type.BaseType;

        } while ( type != component.GetRegisteredComponentBaseClassType() );
#endif

        //Debug.Log( "Unregistered " + component.GetType().Name + ": " + component.gameObject.name );
    }
}
