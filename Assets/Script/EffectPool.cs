using UnityEngine;
using System.Collections;
using PathologicalGames;
using DG.Tweening;
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

	public void PlayBubbleExplode(int type, Vector3 pos)
	{
		string effectName = "";
	    if (type == 1) {
			effectName = "BubbleExplodeYellow";
		} else  if (type == 2) {
			effectName = "BubbleExplodeRed";
		} else  if (type == 3) {
			effectName = "BubbleExplodeBlue";
		} else  if (type == 4) {
			effectName = "BubbleExplodeOrange";
		} else  if (type == 5) {
			effectName = "BubbleExplodeGreen";
		} else {
			effectName = "BubbleExplodeGreen";
		}

		Play (effectName,pos);

	}

	public void PlayFlowEffect(Vector3 pos)
	{
		StartCoroutine (IEPlayFlowEffect(pos));
	}
	IEnumerator IEPlayFlowEffect(Vector3 pos)
	{
		Transform particleTran = pool.Spawn ("FlowEffect");

		ParticleSystem particle = particleTran.GetComponent<ParticleSystem> ();

		
		particleTran.position = pos;
		particle.Play ();

		particleTran.DOMove (new Vector3 (-200, 360, 0f), 0.7f);

		yield return  new WaitForSeconds (1f);

		pool.Despawn (particleTran);

	}
}
