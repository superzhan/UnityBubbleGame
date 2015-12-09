using UnityEngine;
using System.Collections;
using PathologicalGames;

/// <summary>
/// 特效池
/// </summary>
public class EffectPool : MonoBehaviour {

	public static EffectPool Instance = null;

	SpawnPool pool;

	void Awake()
	{
		Instance = this;
	}
	// Use this for initialization
	void Start () {
		pool = PoolManager.Pools["EffectPool"];
	}


	/// <summary>
	/// Play the specified name and position.
	/// </summary>
	/// <param name="name">Name.</param>
	/// <param name="position">Position.</param>
    public void Play(string name,Vector3 position)
	{
		Transform particleTran = pool.Spawn (name);
		if (particleTran == null) {
			return;
		}

		ParticleSystem particle = particleTran.GetComponent<ParticleSystem> ();
		if (particle == null) {
			return;
		}

		particleTran.position = position;
		particle.Play ();
		StartCoroutine (Recycle(particle));
	}

	IEnumerator Recycle(ParticleSystem particle)
	{
		float time = particle.duration;
		yield return new WaitForSeconds(time +0.1f);
		pool.Despawn (particle.transform);
	}
}
