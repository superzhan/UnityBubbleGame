using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathologicalGames;

//
public class BubbleManager : MonoBehaviour {

	public static BubbleManager Instance = null;
	public GameObject bubblePrefab;

	public List<BubbleUnit> bubbleList = new List<BubbleUnit>();


	SpawnPool pool;


	void Awake()
	{
		Instance = this;
	}

	void Start () {
		pool = PoolManager.Pools["BubblePool"];
		Init ();

		Debug.Log (BubbleData.Instance.GetSpriteName (11));
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void Init()
	{
		for (int i=0; i<60; ++i) {
			CreateNewRandomBubble();
		}
	}

    public void CreateNewRandomBubble()
	{
		Transform newBubble = pool.Spawn("BubbleUnit");
		newBubble.parent = transform;
		
		int randonID = Random.Range (0, 5);
		BubbleUnit bubble=newBubble.GetComponent<BubbleUnit> ();
		bubble.SetData (randonID);
		bubbleList.Add(bubble);
		
		Vector3 randPos = new Vector3 (-2f+3*Random.value,4.1f+2*Random.value,0f);
		newBubble.transform.localPosition = randPos;
	}


	/// <summary>
	/// 触发游戏的消除
	/// </summary>
	/// <param name="bubble">Bubble.</param>
	public void Clean(BubbleUnit bubble)
	{
		List<BubbleUnit> linkList = GetLinkBubble (bubble);
		int linkCount=linkList.Count;

		if (linkList.Count > 2) {
			int i=0;
			while(i<linkList.Count)
			{
				EffectPool.Instance.Play("BubbleExplode",linkList[i].transform.position);
				bubbleList.Remove(linkList[i]);
				pool.Despawn(linkList[i].transform);
				++i;
			}


			for (int k=0; k<linkCount; ++k) {
				CreateNewRandomBubble();
			}
		
		}


	}


	/// <summary>
	/// 广度优先搜索 查找相连的 泡泡
	/// </summary>
	/// <returns>The link bubble.</returns>
	/// <param name="bubble">Bubble.</param>
	List<BubbleUnit> GetLinkBubble(BubbleUnit bubble)
	{
		List<BubbleUnit> resList = new List<BubbleUnit> ();
		List<BubbleUnit> tempList = new List<BubbleUnit> ();

		tempList.AddRange (bubbleList);

		//remove different bubble
		int index = 0;
		while (index <tempList.Count) {
		    if(tempList[index].bubbleType != bubble.bubbleType)
			{
				tempList.RemoveAt(index);
			}else
			{
				index++;
			}
		}

		//get the link bubble of select one
		resList.Add (bubble);
		tempList.Remove (bubble);
		int i = 0;
		while (i<resList.Count) {
			BubbleUnit curBub = resList[i];

			int j=0;
			while(j<tempList.Count)
			{
				if( IsTouch(curBub,tempList[j]) )
				{
					resList.Add(tempList[j]);
					tempList.RemoveAt(j);
				}else
				{
					++j;
				}

			}

			++i;
		   
		}
		return resList;
	}

	bool IsTouch(BubbleUnit a, BubbleUnit b)
	{
		float distance = Vector3.Distance (a.transform.localPosition, b.transform.localPosition);

		float radiusA = a.bubCollider.radius;
		float radiusB = b.bubCollider.radius;

		float offset = 0.05f;

		float calDistance = radiusA + radiusB + offset;
		if (calDistance >= distance) {
			return true;
		} else {
			return false;
		}
	}

}
