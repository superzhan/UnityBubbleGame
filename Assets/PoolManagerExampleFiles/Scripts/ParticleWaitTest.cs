using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathologicalGames;


/// <description>
///	Example that spawns and desapwns instances and particles
/// </description>
public class ParticleWaitTest : MonoBehaviour 
{

    public float spawnInterval = 0.25f;
    public string particlesPoolName;
    public ParticleSystem particleSystemPrefab;

    private IEnumerator Start()
    {
        SpawnPool particlesPool = PoolManager.Pools[this.particlesPoolName];

        ParticleSystem prefab = this.particleSystemPrefab;
        Vector3 prefabXform = this.particleSystemPrefab.transform.position;
        Quaternion prefabRot = this.particleSystemPrefab.transform.rotation;

        while (true)
        {
            yield return new WaitForSeconds(this.spawnInterval);

            // Spawn an emitter that will auto-despawn when all particles die
            //  testEmitterPrefab is already a ParticleEmitter, so just pass it.
            // Note the return type is also a ParticleEmitter
            ParticleSystem emitter = particlesPool.Spawn(prefab, prefabXform, prefabRot);

            while (emitter.IsAlive(true))
            {
                // Wait for a little while to be sure we can see it despawn
                yield return new WaitForSeconds(3);
            }

            ParticleSystem inst = particlesPool.Spawn(prefab, prefabXform, prefabRot);

            // Early despawn (in 2 seconds) and re-spawn
            particlesPool.Despawn(inst.transform, 2);

            yield return new WaitForSeconds(2);

            particlesPool.Spawn(prefab, prefabXform, prefabRot);
        }
    }


}