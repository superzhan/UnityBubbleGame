/*************************************************************
 *           Unity Object Pool (c) by ClockStone 2011        *
 * 
 * Allows to "pool" prefab objects to avoid large number of
 * Instantiate() calls.
 * 
 * Usage:
 * 
 * Add the PoolableObject script component to the prefab to be pooled.
 * You can set the maximum number of objects to be be stored in the 
 * pool from within the inspector.
 * 
 * Replace all Instantiate( myPrefab ) calls with 
 * ObjectPoolController.Instantiate( myPrefab)
 * 
 * Replace all Destroy( myObjectInstance ) calls with 
 * ObjectPoolController.Destroy( myObjectInstance )
 * 
 * Note that Awake(), and OnDestroy() get called correctly for 
 * pooled objects. However, make sure that all component data that could  
 * possibly get changed during its lifetime get reinitialized by the
 * Awake() function.
 * The Start() function gets also called, but just after the Awake() function
 * during ObjectPoolController.Instantiate(...)
 * 
 * If a poolable objects gets parented to none-poolable object, the parent must
 * be destroyed using ObjectPoolController.Destroy( ... )
 * 
 * Be aware that OnDestroy() will get called multiple times: 
 *   a) the time ObjectPoolController.Destroy() is called when the object is added
 *      to the pool
 *   b) when the object really gets destroyed (e.g. if a new scene is loaded)
 *   
 * References to pooled objects will not change to null anymore once an object has 
 * been "destroyed" and moved to the pool. Use PoolableReference if you need such checks.
 * 
 * ********************************************************************
*/

#if UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0
#define UNITY_3_x
#endif

#if UNITY_FLASH || UNITY_WINRT
#define REDUCED_REFLECTION
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;

#pragma warning disable 1591 // undocumented XML code warning

/// <summary>
/// Add this component to your prefab to make it poolable. 
/// </summary>
/// <remarks>
/// See <see cref="ObjectPoolController"/> for an explanation how to set up a prefab for pooling.
/// The following messages are sent to a poolable object:
/// <list type="bullet">
/// <item> 
///   <c>Awake</c> and <c>OnDestroy</c> whenever a poolable object is activated or deactivated from the pool. 
///   This way the same behaviour is simulated as if the object was instantiated respectively destroyed.
///   These messages are only sent when <see cref="sendAwakeStartOnDestroyMessage"/> is enabled.
/// </item>
/// <item> 
///   <c>OnPoolableInstanceAwake</c> and <c>OnPoolableInstanceDestroy</c> when the object was actually instantiated respectively destroyed. 
///   Because of current Unity limitations <c>OnPoolableInstanceDestroy</c> does not work on Flash!
/// </item>
/// /// <item> 
///   <c>OnPoolableObjectActivated</c> and <c>OnPoolableObjectDeactivated</c> whenever a poolable object is activated or deactivated from the pool.
///   These messages are only sent when <see cref="sendPoolableActivateDeactivateMessages"/> is enabled.
/// </item>
/// </list>
/// </remarks>
/// <seealso cref="ObjectPoolController"/>
[AddComponentMenu( "ClockStone/PoolableObject" )]
public class PoolableObject : MonoBehaviour
{
    /// <summary>
    /// The maximum number of instances of this prefab to get stored in the pool.
    /// </summary>
    public int maxPoolSize = 10;

    /// <summary>
    /// This number of instances will be preloaded to the pool if <see cref="ObjectPoolController.Preload(GameObject)"/> is called.
    /// </summary>
    public int preloadCount = 0;

    /// <summary>
    /// If enabled the object will not get destroyed if a new scene is loaded
    /// </summary>
    public bool doNotDestroyOnLoad = false;

    /// <summary>
    /// If enabled Awake(), Start(), and OnDestroy() messages are sent to the poolable object if the object is set 
    /// active respectively inactive whenever <see cref="ObjectPoolController.Destroy(GameObject)"/> or 
    /// <see cref="ObjectPoolController.Instantiate(GameObject)"/> is called. <para/>
    /// This way it is simulated that the object really gets instantiated respectively destroyed.
    /// </summary>
    /// <remarks>
    /// The Start() function is called immedialtely after Awake() by <see cref="ObjectPoolController.Instantiate(GameObject)"/> 
    /// and not next frame. So do not set data after <see cref="ObjectPoolController.Instantiate(GameObject)"/> that Start()
    /// relies on. In some cases you may not want the  Awake(), Start(), and OnDestroy() messages to be sent for performance 
    /// reasons because it may not be necessary to fully reinitialize a game object each time it is activated from the pool. 
    /// You can still use the <c>OnPoolableObjectActivated</c> and <c>OnPoolableObjectDeactivated</c> messages to initialize 
    /// specific data.
    /// </remarks>
    public bool sendAwakeStartOnDestroyMessage = true;

    /// <summary>
    /// If enabled a <c>OnPoolableObjectActivated</c> and <c>OnPoolableObjectDeactivated</c> message is sent to 
    /// the poolable instance if the object is activated respectively deactivated by the <see cref="ObjectPoolController"/>
    /// </summary>
    public bool sendPoolableActivateDeactivateMessages = false;

    internal bool _isAvailableForPooling = false;
    internal bool _createdWithPoolController = false;
    internal bool _destroyMessageFromPoolController = false;
    internal bool _wasPreloaded = false;
    internal bool _wasStartCalledByUnity = false;
    internal ObjectPoolController.ObjectPool _myPool = null;
    internal int _serialNumber = 0;
    internal int _usageCount = 0;

#if UNITY_EDITOR
    protected void Awake()
    {
        //Debug.Log( string.Format( "Awake: {0} Pool:{1}", name, _myPool != null ) );
        if ( _myPool == null && !ObjectPoolController._isDuringInstantiate )
        {
            Debug.LogWarning( "Poolable object " + name + " was instantiated without ObjectPoolController" );
        }
    }
#endif

    protected void Start()
    {
        _wasStartCalledByUnity = true;
    }

#if !REDUCED_REFLECTION
    private static void _InvokeMethodByName( MonoBehaviour behaviour, string methodName )
    {
        // Get the Type for the class
        Type calledType = behaviour.GetType();

        var methodInfo = calledType.GetMethod( methodName,
                        BindingFlags.Instance | BindingFlags.NonPublic );

        if ( methodInfo != null )
        {
            methodInfo.Invoke( behaviour, null );
        }
    }
#endif

    // broadcasts also to inactive game objects, does not broadcast to poolable child objects
    static private void _BroadcastMessageToGameObject( GameObject go, string message )
    {
#if !REDUCED_REFLECTION
        var components = go.GetComponents( typeof( MonoBehaviour ) );

        foreach ( var c in components )
        {
            _InvokeMethodByName( ( MonoBehaviour ) c, message );
        }

        if ( go.transform.childCount > 0 )
        {
            _BroadcastMessageToAllChildren( go, message );
        }
#else
        go.BroadcastMessage( message, SendMessageOptions.DontRequireReceiver ); // workaround for Flash - does not work for OnPoolableInstanceDestroy!!
#endif
    }

    private static void _BroadcastMessageToAllChildren( GameObject go, string message )
    {
        Transform[] children = new Transform[ go.transform.childCount ]; // save child array, as it may change

        for ( int i = 0; i < go.transform.childCount; i++ )
            children[ i ] = go.transform.GetChild( i );

        //now recursively do the same for all children
        for ( int i = 0; i < children.Length; i++ )
        {
            if ( children[ i ].GetComponent<PoolableObject>() == null ) // if child object is PoolableObject then don't broadcast
            {
                PoolableObject._BroadcastMessageToGameObject( children[ i ].gameObject, message );
            }
        }
    }
    protected void OnDestroy()
    {
        if ( !_destroyMessageFromPoolController && _myPool != null )
        {
            // Poolable object was destroyed by using the default Unity Destroy() function -> Use ObjectPoolController.Destroy() instead
            // This can also happen if objects are automatically deleted by Unity e.g. due to level change or if an object is parented to an object that gets destroyed

            _myPool.Remove( this );
            //Debug.LogError( "Destroy S/N:" + _serialNumber );
        }

        if ( !_destroyMessageFromPoolController )
        {
            // can't use Unity's BroadcastMessage because it doesn't send to object that are just being destroyed
            _BroadcastMessageToGameObject( gameObject, "OnPoolableInstanceDestroy" );
        }

        _destroyMessageFromPoolController = false;
    }

    /// <summary>
    /// Gets the object's pool serial number. Each object has a unique serial number. Can be useful for debugging purposes.
    /// </summary>
    /// <returns>
    /// The serial number (starting with 1 for each pool).
    /// </returns>
    public int GetSerialNumber() // each new instance receives a unique serial number
    {
        return _serialNumber;
    }

    /// <summary>
    /// Gets the usage counter which gets increased each time an object is re-used from the pool.
    /// </summary>
    /// <returns>
    /// The usage counter
    /// </returns>
    public int GetUsageCount()
    {
        return _usageCount;
    }

    /// <summary>
    /// Moves all poolable objects of this kind (instantiated from the same prefab as this instance) back to the pool. 
    /// </summary>
    /// <returns>
    /// The number of instances deactivated and moved back to its pool.
    /// </returns>
    public int DeactivateAllPoolableObjectsOfMyKind()
    {
        if ( _myPool != null )
        {
            return _myPool._SetAllAvailable();
        }
        return 0;
    }

    /// <summary>
    /// Checks if the object is deactivated and in the pool.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the object is in the pool of deactivated objects, otherwise <c>false</c>.
    /// </returns>
    public bool IsDeactivated()
    {
        return _isAvailableForPooling;
    }

    /// <summary>
    /// Retrieves an array of all poolable objects of this kind (instantiated from the same prefab as this instance). 
    /// </summary>
    /// <param name="includeInactiveObjects">
    /// If enabled, the returned array will also include the inactive objects in the pool.
    /// </param>
    /// <returns>
    /// The array of poolable objects.
    /// </returns>
    public PoolableObject[] GetAllPoolableObjectsOfMyKind( bool includeInactiveObjects )
    {
        if ( _myPool != null )
        {
            return _myPool._GetAllObjects( includeInactiveObjects );
        }
        return null;
    }
}



/// <summary>
/// A static class used to create and destroy poolable objects.
/// </summary>
/// <remarks>
/// What is pooling? <para/>
/// GameObject.Instantiate(...) calls are relatively time expensive. If objects of the same
/// type are frequently created and destroyed it is good practice to use object pools, particularly on mobile
/// devices. This can greatly reduce the performance impact for object creation and garbage collection. <para/>
/// How does pooling work?<para/>
/// Instead of actually destroying object instances, they are just set inactive and moved to an object "pool".
/// If a new object is requested it can then simply be pulled from the pool, instead of creating a new instance. <para/>
/// Awake(), Start() and OnDestroy() are called if objects are retrieved from or moved to the pool like they 
/// were instantiated and destroyed normally.
/// </remarks>
/// <example>
/// How to set up a prefab for pooling:
/// <list type="number">
/// <item>Add the PoolableObject script component to the prefab to be pooled.
/// You can set the maximum number of objects to be be stored in the pool from within the inspector.</item>
/// <item> Replace all <c>Instantiate( myPrefab )</c> calls with <c>ObjectPoolController.Instantiate( myPrefab )</c></item>
/// <item> Replace all <c>Destroy( myObjectInstance )</c> calls with <c>ObjectPoolController.Destroy( myObjectInstance )</c></item>
/// </list>
/// Attention: Be aware that:
/// <list type="bullet">
/// <item>All data must get initialized in the Awake() or Start() function</item>
/// <item><c>OnDestroy()</c> will get called a second time once the object really gets destroyed by Unity</item>
/// <item>If a poolable objects gets parented to none-poolable object, the parent must
/// be destroyed using <c>ObjectPoolController.Destroy( ... )</c> even if it is none-poolable itself.</item>
/// <item>If you store a reference to a poolable object then this reference does not evaluate to <c>null</c> after <c>ObjectPoolController.Destroy( ... )</c>
/// was called like other references to Unity objects normally would. This is because the object still exists - it is just in the pool. 
/// To make sure that a stored reference to a poolable object is still valid you must use <see cref="PoolableReference{T}"/>.</item>
/// </list>
/// </example>
/// <seealso cref="PoolableObject"/>
static public class ObjectPoolController
{

    static public bool isDuringPreload
    {
        get;
        private set;
    }

    // **************************************************************************************************/
    //          public functions
    // **************************************************************************************************/

    /// <summary>
    /// Retrieves an instance of the specified prefab. Either returns a new instance or it claims an instance 
    /// from the pool.
    /// </summary>
    /// <param name="prefab">The prefab to be instantiated.</param>
    /// <returns>
    /// An instance of the prefab.
    /// </returns>
    /// <remarks>
    /// Can be used on none-poolable objects as well. It is good practice to use <c>ObjectPoolController.Instantiate</c>
    /// whenever you may possibly make your prefab poolable in the future.
    /// </remarks>
    /// <seealso cref="Destroy(GameObject)"/>
    static public GameObject Instantiate( GameObject prefab )
    {
        PoolableObject prefabPool = prefab.GetComponent<PoolableObject>();
        if ( prefabPool == null )
        {
            //Debug.LogWarning( "Object " + prefab.name + " not poolable " );
            return ( GameObject ) GameObject.Instantiate( prefab ); // prefab not pooled, instantiate normally
        }

        GameObject go = _GetPool( prefabPool ).GetPooledInstance( null, null );

        if ( go )
        {
            return go;
        }
        else // pool is full
        {
            return InstantiateWithoutPool( prefab );
        }
    }

    /// <summary>
    /// Retrieves an instance of the specified prefab. Either returns a new instance or it claims an instance
    /// from the pool.
    /// </summary>
    /// <param name="prefab">The prefab to be instantiated.</param>
    /// <param name="position">The position in world coordinates.</param>
    /// <param name="quaternion">The rotation quaternion.</param>
    /// <returns>
    /// An instance of the prefab.
    /// </returns>
    /// <remarks>
    /// Can be used on none-poolable objects as well. It is good practice to use <c>ObjectPoolController.Instantiate</c>
    /// whenever you may possibly make your prefab poolable in the future.
    /// </remarks>
    /// <seealso cref="Destroy(GameObject)"/>
    static public GameObject Instantiate( GameObject prefab, Vector3 position, Quaternion quaternion )
    {
        PoolableObject prefabPool = prefab.GetComponent<PoolableObject>();
        if ( prefabPool == null )
        {
            // no warning displayed by design because this allows to decide later if the object will be poolable or not
            // Debug.LogWarning( "Object " + prefab.name + " not poolable "); 
            return ( GameObject ) GameObject.Instantiate( prefab, position, quaternion ); // prefab not pooled, instantiate normally
        }

        GameObject go = _GetPool( prefabPool ).GetPooledInstance( position, quaternion );

        if ( go )
        {
            return go;
        }
        else // pool is full
        {
            //Debug.LogWarning( "Pool Full" );
            return InstantiateWithoutPool( prefab, position, quaternion );
        }
    }

    /// <summary>
    /// Instantiates the specified prefab without using pooling.
    /// from the pool.
    /// </summary>
    /// <param name="prefab">The prefab to be instantiated.</param>
    /// <returns>
    /// An instance of the prefab.
    /// </returns>
    /// <remarks>
    /// If the prefab is poolable, the <see cref="PoolableObject"/> component will be removed.
    /// This way no warning is generated that a poolable object was created without pooling.
    /// </remarks>
    static public GameObject InstantiateWithoutPool( GameObject prefab )
    {
        return InstantiateWithoutPool( prefab, new Vector3( 0, 0, 0 ), Quaternion.identity );
    }

    /// <summary>
    /// Instantiates the specified prefab without using pooling.
    /// from the pool.
    /// </summary>
    /// <param name="prefab">The prefab to be instantiated.</param>
    /// <param name="position">The position in world coordinates.</param>
    /// <param name="quaternion">The rotation quaternion.</param>
    /// <returns>
    /// An instance of the prefab.
    /// </returns>
    /// <remarks>
    /// If the prefab is poolable, the <see cref="PoolableObject"/> component will be removed.
    /// This way no warning is generated that a poolable object was created without pooling.
    /// </remarks>
    static public GameObject InstantiateWithoutPool( GameObject prefab, Vector3 position, Quaternion quaternion )
    {
        GameObject go;
        _isDuringInstantiate = true;
        go = ( GameObject ) GameObject.Instantiate( prefab, position, quaternion ); // prefab not pooled, instantiate normally
        _isDuringInstantiate = false;

        PoolableObject pool = go.GetComponent<PoolableObject>();
        if ( pool )
        {
            if ( pool.doNotDestroyOnLoad )
            {
                GameObject.DontDestroyOnLoad( go );
            }

            pool._createdWithPoolController = true; // so no warning is displayed if object gets ObjectPoolCOntroller.Destroy()-ed before the component actually gets removed
            Component.Destroy( pool );
        }

        return go;

    }

    /// <summary>
    /// Destroys the specified game object, respectively sets the object inactive and adds it to the pool.
    /// </summary>
    /// <param name="obj">The game object.</param>
    /// <remarks>
    /// Can be used on none-poolable objects as well. It is good practice to use <c>ObjectPoolController.Destroy</c>
    /// whenever you may possibly make your prefab poolable in the future. <para/>
    /// Must also be used on none-poolable objects with poolable child objects so the poolable child objects are correctly
    /// moved to the pool.
    /// </remarks>
    /// <seealso cref="Instantiate(GameObject)"/>
    static public void Destroy( GameObject obj ) // destroys poolable and none-poolable objects. Destroys poolable children correctly
    {

        PoolableObject poolObj = obj.GetComponent<PoolableObject>();
        if ( poolObj == null )
        {
            _DetachChildrenAndDestroy( obj.transform ); // child objects may be poolable
            GameObject.Destroy( obj ); // prefab not pooled, destroy normally
            return;
        }
        if ( poolObj._myPool != null )
        {
            poolObj._myPool._SetAvailable( poolObj, true );
        }
        else
        {
            if ( !poolObj._createdWithPoolController )
            {
                Debug.LogWarning( "Poolable object " + obj.name + " not created with ObjectPoolController" );
            }
            GameObject.Destroy( obj ); // prefab not pooled, destroy normally
        }

    }

    /// <summary>
    /// Preloads as many instances to the pool so that there are at least as many as
    /// specified in <see cref="PoolableObject.preloadCount"/>. 
    /// </summary>
    /// <param name="prefab">The prefab.</param>
    /// <remarks>
    /// Use ObjectPoolController.isDuringPreload to check if an object is preloaded in the <c>Awake()</c> function.
    /// If the pool already contains at least <see cref="PoolableObject.preloadCount"/> objects, the function does nothing. 
    /// </remarks>
    /// <seealso cref="PoolableObject.preloadCount"/>
    static public void Preload( GameObject prefab ) // adds as many instances to the prefab pool as specified in the PoolableObject
    {
        PoolableObject poolObj = prefab.GetComponent<PoolableObject>();
        if ( poolObj == null )
        {
            Debug.LogWarning( "Can not preload because prefab '" + prefab.name + "' is not poolable" );
            return;
        }

        var pool = _GetPool( poolObj ); // _preloadDone is set by _GetPool

        int delta = poolObj.preloadCount - pool.GetObjectCount();
        if ( delta <= 0 )
        {
            return;
        }

        isDuringPreload = true;

        try
        {
            for ( int i = 0; i < delta; i++ )
            {
                pool.PreloadInstance();
            }
        }
        finally
        {
            isDuringPreload = false;
        }

        //Debug.Log( "preloaded: " + prefab.name + " " + poolObj.preloadCount + " times" );
    }


    // **************************************************************************************************/
    //          protected / private  functions
    // **************************************************************************************************/

    internal static int _globalSerialNumber = 0;
    internal static bool _isDuringInstantiate = false;

    internal class ObjectPool
    {
        HashSet_Flash<PoolableObject> _pool;
        PoolableObject _prefabPoolObj;
        Transform _poolParentDummy;

        internal Transform poolParentDummy
        {
            get
            {
                _ValidatePoolParentDummy();
                return _poolParentDummy;
            }
        }

        private void _ValidatePoolParentDummy()
        {
            if ( !_poolParentDummy )
            {
                var poolParentDummyGameObject = new GameObject( "POOL:" + _prefabPoolObj.name );
                _poolParentDummy = poolParentDummyGameObject.transform;
                _SetActive( poolParentDummyGameObject, false );
                if ( _prefabPoolObj.doNotDestroyOnLoad )
                {
                    GameObject.DontDestroyOnLoad( poolParentDummyGameObject );
                }
            }
        }

        public ObjectPool( GameObject prefab )
        {
            _prefabPoolObj = prefab.GetComponent<PoolableObject>();
        }

        private void _ValidatePooledObjectDataContainer()
        {
            if ( _pool == null )
            {
                _pool = new HashSet_Flash<PoolableObject>();
                _ValidatePoolParentDummy();
            }
        }

        internal void Remove( PoolableObject poolObj )
        {
            _pool.Remove( poolObj );
        }

        internal int GetObjectCount()
        {
            if ( _pool == null ) return 0;
            return _pool.Count;
        }

        internal GameObject GetPooledInstance( Vector3? position, Quaternion? rotation )
        {
            _ValidatePooledObjectDataContainer();

            Transform prefabTransform = _prefabPoolObj.transform;
            Transform objTransform;

            foreach ( PoolableObject o in _pool )
            {
                if ( o != null && o._isAvailableForPooling )
                {
                    objTransform = o.transform;
                    objTransform.position = ( position != null ) ? ( Vector3 ) position : prefabTransform.position;
                    objTransform.rotation = ( rotation != null ) ? ( Quaternion ) rotation : prefabTransform.rotation;
                    objTransform.localScale = prefabTransform.localScale;
                    o._usageCount++;
                    _SetAvailable( o, false );
                    return o.gameObject;
                }
            }

            if ( _pool.Count < _prefabPoolObj.maxPoolSize ) // add new element to pool 
            {
                return _NewPooledInstance( position, rotation ).gameObject;
            }

            // pool is full
            return null;
        }

        internal PoolableObject PreloadInstance()
        {
            _ValidatePooledObjectDataContainer();

            PoolableObject poolObj = _NewPooledInstance( null, null );

            poolObj._wasPreloaded = true;

            _SetAvailable( poolObj, true );

            return poolObj;
        }

        private PoolableObject _NewPooledInstance( Vector3? position, Quaternion? rotation )
        {
            GameObject go;

            _isDuringInstantiate = true;

            if ( position != null && rotation != null )
            {
                go = ( GameObject ) GameObject.Instantiate( _prefabPoolObj.gameObject, ( Vector3 ) position, ( Quaternion ) rotation );
            }
            else
            {
                go = ( GameObject ) GameObject.Instantiate( _prefabPoolObj.gameObject );
            }

            _isDuringInstantiate = false;

            PoolableObject poolObj = go.GetComponent<PoolableObject>();
            poolObj._createdWithPoolController = true;
            poolObj._myPool = this;
            poolObj._isAvailableForPooling = false;
            poolObj._serialNumber = ++_globalSerialNumber;
            poolObj._usageCount++;

            if ( poolObj.doNotDestroyOnLoad )
            {
                GameObject.DontDestroyOnLoad( go );
            }

            _pool.Add( poolObj );

            go.BroadcastMessage( "OnPoolableInstanceAwake", SendMessageOptions.DontRequireReceiver );
            return poolObj;

        }

        /// <summary>
        /// Deactivate all active pooled objects
        /// </summary>
        internal int _SetAllAvailable()
        {
            int count = 0;
            foreach ( PoolableObject o in _pool )
            {
                if ( o != null && !o._isAvailableForPooling )
                {
                    _SetAvailable( o, true );
                    count++;
                }
            }
            return count;
        }

        internal PoolableObject[] _GetAllObjects( bool includeInactiveObjects )
        {
            var list = new List<PoolableObject>();
            foreach ( PoolableObject o in _pool )
            {
                if ( o != null )
                {
                    if ( includeInactiveObjects || !o._isAvailableForPooling )
                    {
                        list.Add( o );
                    }
                }
            }

            return list.ToArray();
        }

        internal void _SetAvailable( PoolableObject poolObj, bool b )
        {
            poolObj._isAvailableForPooling = b;

            var objTransform = poolObj.transform;

            if ( b )
            {
                if ( poolObj.sendAwakeStartOnDestroyMessage )
                {
                    poolObj._destroyMessageFromPoolController = true;
                }

                objTransform.parent = null; // object could still be parented, so detach

                _RecursivelySetInactiveAndSendMessages( poolObj.gameObject, poolObj, false );

                objTransform.parent = poolObj._myPool.poolParentDummy; // attach to dummy Parent

                //poolObj.gameObject.name = "pooled:" + poolObj._myPool._prefabPoolObj.name;

            }
            else
            {
                objTransform.parent = null; // detach from poolParentDummy
                _SetActiveAndSendMessages( poolObj.gameObject, poolObj );

                //poolObj.gameObject.name = poolObj._myPool._prefabPoolObj.name;
            }
        }

        private void _SetActive( GameObject obj, bool active )
        {
#if UNITY_3_x
            if ( active )
            {
                obj.SetActiveRecursively( true );
            } else 
                obj.active = false;
#else
            obj.SetActive( active );
#endif
        }

        private bool _GetActive( GameObject obj )
        {
#if UNITY_3_x
            return obj.active;            
#else
            return obj.activeInHierarchy;
#endif
        }

        private void _SetActiveAndSendMessages( GameObject obj, PoolableObject parentPoolObj )
        {
            _SetActive( obj, true );

            if ( parentPoolObj.sendAwakeStartOnDestroyMessage )
            {
                obj.BroadcastMessage( "Awake", null, SendMessageOptions.DontRequireReceiver );

                if ( _GetActive( obj )
                    && // Awake could deactivate object
                        parentPoolObj._wasStartCalledByUnity ) // for preloaded objects Unity will call Start
                {
                    obj.BroadcastMessage( "Start", null, SendMessageOptions.DontRequireReceiver );
                }
            }

            if ( parentPoolObj.sendPoolableActivateDeactivateMessages )
            {
                obj.BroadcastMessage( "OnPoolableObjectActivated", null, SendMessageOptions.DontRequireReceiver );
            }
        }

        private void _RecursivelySetInactiveAndSendMessages( GameObject obj, PoolableObject parentPoolObj, bool recursiveCall )
        {
            // Create a local copy of all of the children before we potentially modify it
            // by removing a child PoolableObject by making a call to _SetAvailable()

            var objTransform = obj.transform;
            Transform[ ] children = new Transform[ objTransform.childCount ];

            for ( int i = 0; i < objTransform.childCount; i++ )
                children[ i ] = objTransform.GetChild( i );

            //now recursively do the same for all children
            for ( int i = 0; i < children.Length; i++ )
            {
                Transform child = children[ i ];

                var poolableChild = child.gameObject.GetComponent<PoolableObject>();

                if ( poolableChild && poolableChild._myPool != null ) //if child is poolable itself it has to be detached and moved to the pool
                {
                    _SetAvailable( poolableChild, true );
                }
                else
                {
                    _RecursivelySetInactiveAndSendMessages( child.gameObject, parentPoolObj, true );
                }
            }

            if ( parentPoolObj.sendAwakeStartOnDestroyMessage )
            {
                obj.SendMessage( "OnDestroy", null, SendMessageOptions.DontRequireReceiver );
            }

            if ( parentPoolObj.sendPoolableActivateDeactivateMessages )
            {
                obj.SendMessage( "OnPoolableObjectDeactivated", null, SendMessageOptions.DontRequireReceiver );
            }

#if UNITY_3_x
#else
            if ( !recursiveCall )
#endif
            {
                _SetActive( obj, false );
            }
        }
    }

    static private Dictionary<GameObject, ObjectPool> _pools = new Dictionary<GameObject, ObjectPool>();

    static internal ObjectPool _GetPool( PoolableObject prefabPoolComponent )
    {
        ObjectPool pool;

        GameObject prefab = prefabPoolComponent.gameObject;

        if ( !_pools.TryGetValue( prefab, out pool ) )
        {
            pool = new ObjectPool( prefab );
            _pools.Add( prefab, pool );
        }

        return pool;
    }

    static private void _DetachChildrenAndDestroy( Transform transform )
    {
        int childCount = transform.childCount;

        Transform[ ] children = new Transform[ childCount ];

        var myTransform = transform; // cache for performance reasons

        int i;
        for ( i = 0; i < childCount; i++ )
        {
            children[ i ] = myTransform.GetChild( i );
        }
        myTransform.DetachChildren();

        for ( i = 0; i < childCount; i++ )
        {
            GameObject obj = children[ i ].gameObject;
            if ( obj )
            {
                ObjectPoolController.Destroy( obj );
            }
        }

    }
}

/// <summary>
/// Auxiliary class to overcome the problem of references to pooled objects that should become <c>null</c> when 
/// objects are moved back to the pool after calling <see cref="ObjectPoolController.Destroy(GameObject)"/>.
/// </summary>
/// <typeparam name="T">A <c>UnityEngine.Component</c></typeparam>
/// <example>
/// Instead of a normal reference to a script component on a poolable object use 
/// <code>
/// MyScriptComponent scriptComponent = PoolableObjectController.Instantiate( prefab ).GetComponent&lt;MyScriptComponent&gt;();
/// var myReference = new PoolableReference&lt;MyScriptComponent&gt;( scriptComponent );
/// if( myReference.Get() != null ) // will check if poolable instance still belongs to the original object
/// {
///     myReference.Get().MyComponentFunction();
/// }
/// </code>
/// </example>
public class PoolableReference<T> where T : Component
{
    PoolableObject _pooledObj;
    int _initialUsageCount;

#if REDUCED_REFLECTION
    Component _objComponent;
#else
    T _objComponent;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolableReference&lt;T&gt;"/> class with a <c>null</c> reference.
    /// </summary>
    public PoolableReference()
    {
        Reset();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolableReference&lt;T&gt;"/> class with the specified reference.
    /// </summary>
    /// <param name="componentOfPoolableObject">The referenced component of the poolable object.</param>
#if REDUCED_REFLECTION
    public PoolableReference( Component componentOfPoolableObject )
#else
    public PoolableReference( T componentOfPoolableObject )
#endif
    {
        Set( componentOfPoolableObject, false );
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PoolableReference&lt;T&gt;"/> class from 
    /// a given <see cref="PoolableReference&lt;T&gt;"/>.
    /// </summary>
    /// <param name="poolableReference">The poolable reference.</param>
    public PoolableReference( PoolableReference<T> poolableReference )
    {
        _objComponent = poolableReference._objComponent;
        _pooledObj = poolableReference._pooledObj;
        _initialUsageCount = poolableReference._initialUsageCount;
    }

    /// <summary>
    /// Resets the reference to <c>null</c>.
    /// </summary>
    public void Reset()
    {
        _pooledObj = null;
        _objComponent = null;
        _initialUsageCount = 0;
    }

    /// <summary>
    /// Gets the reference to the script component, or <c>null</c> if the object was 
    /// already destroyed or moved to the pool.
    /// </summary>
    /// <returns>
    /// The reference to <c>T</c> or null
    /// </returns>
    public T Get()
    {
        if ( !_objComponent ) return null;

        if ( _pooledObj ) // could be set to a none-poolable object
        {
            if ( _pooledObj._usageCount != _initialUsageCount || _pooledObj._isAvailableForPooling )
            {
                _objComponent = null;
                _pooledObj = null;
                return null;
            }
        }
        return ( T ) _objComponent;
    }

#if REDUCED_REFLECTION
    public void Set( Component componentOfPoolableObject, bool allowNonePoolable )
#else
    public void Set( T componentOfPoolableObject )
    {
        Set( componentOfPoolableObject, false );
    }

    /// <summary>
    /// Sets the reference to a poolable object with the specified component.
    /// </summary>
    /// <param name="componentOfPoolableObject">The component of the poolable object.</param>
    /// <param name="allowNonePoolable">If set to false an error is output if the object does not have the <see cref="PoolableObject"/> component.</param>
    public void Set( T componentOfPoolableObject, bool allowNonePoolable )
#endif
    {
        if ( !componentOfPoolableObject )
        {
            Reset();
            return;
        }
        _objComponent = ( T ) componentOfPoolableObject;
        _pooledObj = _objComponent.GetComponent<PoolableObject>();
        if ( !_pooledObj )
        {
            if ( allowNonePoolable )
            {
                _initialUsageCount = 0;
            }
            else
            {
                Debug.LogError( "Object for PoolableReference must be poolable" );
                return;
            }
        }
        else
        {
            _initialUsageCount = _pooledObj._usageCount;
        }
    }
}