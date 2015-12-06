using UnityEngine;
using System.Collections;

public class BubbleBox : MonoBehaviour {

	public Transform bubbleUnit;
	public Transform bubblePanel;
	
	// Use this for initialization
	void Start () {
		
		StartCoroutine (ProduceBubble ());
	}

	IEnumerator ProduceBubble()
	{
		for (int i=0; i<100; ++i) {
			Transform newBubble = Instantiate(bubbleUnit);
			newBubble.parent = bubblePanel;
			newBubble.localPosition = new Vector3(-2+4*Random.value,4+3*Random.value,0);

			yield return new WaitForEndOfFrame();
		}
	}
}
