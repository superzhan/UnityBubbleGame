using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathologicalGames;


/// <description>
///	An example that shows the creation of a pool.
/// </description>
public class CreationExample : MonoBehaviour 
{
    /// <summary>
    /// The prefab to spawn from.
    /// </summary>
    public Transform testPrefab;
    public string poolName = "Creator";
    public int spawnAmount = 50;
    public float spawnInterval = 0.25f;

    private SpawnPool pool;

    /// <summary>
    /// Setup the PrefabPool. Change to test different settings.
    /// </summary>
    private void Start()
	{
        this.pool = PoolManager.Pools.Create(this.poolName);

        // Make the pool's group a child of this transform for demo purposes
        this.pool.group.parent = this.transform;

        // Set the pool group's local position for demo purposes
        this.pool.group.localPosition = new Vector3(1.5f, 0, 0);
        this.pool.group.localRotation = Quaternion.identity;

        
        // Create a prefab pool, set culling options but don't need to pre-load any
        //  If no options are needed, this can be skipped entirely. Just use spawn()
        //  and a PrefabPool will be created automatically with defaults
        PrefabPool prefabPool = new PrefabPool(testPrefab);
        prefabPool.preloadAmount = 5;      // This is the default so may be omitted
        prefabPool.cullDespawned = true;
        prefabPool.cullAbove = 10;
        prefabPool.cullDelay = 1;
        prefabPool.limitInstances = true;
        prefabPool.limitAmount = 5;
        prefabPool.limitFIFO = true;

        this.pool.CreatePrefabPool(prefabPool);

        this.StartCoroutine(Spawner());


        // NEW EXAMPLE... Preabs[] dict
        // In the Shapes pool, we know we have a prefab "Cube". This example uses
        //    just this name to get a reference to the prefab and spawn an instance
        Transform cubePrefab   = PoolManager.Pools["Shapes"].prefabs["Cube"];
        Transform cubeinstance = PoolManager.Pools["Shapes"].Spawn(cubePrefab);
        cubeinstance.name = "Cube (Spawned By CreationExample.cs)"; // So we can see it.
     }


    /// <summary>
    /// Spawn an instance every this.spawnInterval
    /// </summary>
    private IEnumerator Spawner()
    {
        int count = this.spawnAmount;
        Transform inst;
        while (count > 0)
        {
            // Spawn in a line, just for fun
            inst = this.pool.Spawn(this.testPrefab, Vector3.zero, Quaternion.identity);
            inst.localPosition = new Vector3(this.spawnAmount - count, 0, 0);

            count--;

            yield return new WaitForSeconds(this.spawnInterval);
        }

        // When done, start despawning
        this.StartCoroutine(Despawner());
    }


    /// <summary>
    /// Despawn an instance every this.spawnInterval
    /// </summary>
    private IEnumerator Despawner()
    {
        while (this.pool.Count > 0)
        {
            // Despawn the last instance (like dequeue in a queue because 
            //   Despawn() will also remove the item from the list, so the list
            //   is being changed in-place.)
            Transform instance = this.pool[pool.Count - 1];
            this.pool.Despawn(instance);  // Internal count--

            yield return new WaitForSeconds(this.spawnInterval);
        }
    }

}