using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathologicalGames;


/// <description>
///	Example that spawns and particles at the position of this components GameObject
/// </description>
public class ReallySimpleAudioSpawner : MonoBehaviour 
{
    public AudioSource prefab;
    public float spawnInterval = 2;
    
    private SpawnPool pool;

    private void Start()
    {
        this.pool = this.GetComponent<SpawnPool>();
        this.StartCoroutine(this.Spawner());
    }

    private IEnumerator Spawner()
    {
        AudioSource current;
        while (true)
        {
            current = this.pool.Spawn
            (
                this.prefab, 
                this.transform.position, 
                this.transform.rotation
            );

            current.pitch = Random.Range(0.7f, 1.4f);

            yield return new WaitForSeconds(this.spawnInterval);
        }
    }
}