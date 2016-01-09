using UnityEngine;
using System.Collections;

public class SpecialCheckCollider : MonoBehaviour {

	SpecialBubbleUnit bubble;

	void OnEnable()
	{
		bubble = GetComponentInParent<SpecialBubbleUnit> ();
	}
   
	void OnTriggerEnter2D(Collider2D other) {

		if (bubble == null) {
			bubble = GetComponentInParent<SpecialBubbleUnit> ();
		}

	     if (other.CompareTag ("Bubble")) {
			BubbleUnit otherBubble = other.GetComponent<BubbleUnit>();
			if(otherBubble != null)
			{
				bubble.DealEffectCleanBubble(otherBubble);
			}

		}
	}

}
