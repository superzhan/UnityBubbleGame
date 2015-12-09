using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathologicalGames;


/// <description>
///	Example that spawns and particles at the position of this components GameObject
/// </description>
public class ReallySimpleParticleSpawner : MonoBehaviour 
{
    public string poolName;
    public ParticleSystem prefab;
    public float spawnInterval = 1;

    private void Start()
    {
        this.StartCoroutine(ParticleSpawner());
    }

    private IEnumerator ParticleSpawner()
    {
        while (true)
        {
            PoolManager.Pools[this.poolName].Spawn
            (
                this.prefab, 
                this.transform.position, 
                this.transform.rotation
            );

            yield return new WaitForSeconds(this.spawnInterval);
        }
    }
}