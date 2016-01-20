using UnityEngine;
using System.Collections;
using PathologicalGames;
using DG.Tweening;

public class BubbleGeneretor : MonoBehaviour {

	SpawnPool pool;

	void Start()
	{
		pool = PoolManager.Pools["BubblePool"];
	}

	void OnEnable()
	{

		
		StartCoroutine (Generate());
	}


	IEnumerator Generate()
	{
		while (true) {
			yield return new WaitForSeconds(0.8f);
			StartCoroutine(IEGenerateShowBubble());


		}
	}


	IEnumerator IEGenerateShowBubble()
	{
		Transform showBubbleTran = pool.Spawn ("ShowBubble");
		Vector3 startPos = new Vector3 (-200f + Random.value * 400, -480f, 0);
		Vector3 targetPos =new Vector3 (-500f+Random.value *1000,480f,0);

		showBubbleTran.parent = transform;
		showBubbleTran.position = startPos;

		float useTime = 12+ Random.value * 10;
		showBubbleTran.DOMove (targetPos,useTime).SetEase(Ease.Linear);

		yield return new WaitForSeconds(useTime);

		pool.Despawn (showBubbleTran);

	}
}
