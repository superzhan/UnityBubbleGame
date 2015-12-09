using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//
public class BubbleManager : MonoBehaviour {

	public static BubbleManager Instance = null;
	public GameObject bubblePrefab;

	public List<BubbleUnit> bubbleList = new List<BubbleUnit>();


	void Start () {
		Instance = this;
		Init ();
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void Init()
	{
		for (int i=0; i<60; ++i) {
			GameObject newBubble = (GameObject)Instantiate (bubblePrefab);
			newBubble.transform.parent = transform;

			int randonID = Random.Range (0, 5);
			BubbleUnit bubble=newBubble.GetComponent<BubbleUnit> ();
			bubble.SetData (randonID);
			bubbleList.Add(bubble);

			Vector3 randPos = new Vector3 (-2f+3*Random.value,4.1f+2*Random.value,0f);
			newBubble.transform.localPosition = randPos;
		}
	}


	public void Clean(BubbleUnit bubble)
	{
		List<BubbleUnit> linkList = GetLinkBubble (bubble);
		Debug.Log (linkList.Count);
		if (linkList.Count > 2) {
			int i=0;
			while(i<linkList.Count)
			{
				bubbleList.Remove(linkList[i]);
				Destroy(linkList[i].gameObject);
				++i;
			}
		
		}
	}

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
				if(curBub.bubCollider.IsTouching(tempList[j].bubCollider))
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

}
