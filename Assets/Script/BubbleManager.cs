using UnityEngine;
using System.Collections;

//
public class BubbleManager : MonoBehaviour {

	public GameObject bubblePrefab;
	// Use this for initialization
	void Start () {
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
			newBubble.GetComponent<BubbleUnit> ().SetData (randonID);
			Vector3 randPos = new Vector3 (-2f+3*Random.value,4.1f+2*Random.value,0f);
			newBubble.transform.localPosition = randPos;
		}
	}
}
