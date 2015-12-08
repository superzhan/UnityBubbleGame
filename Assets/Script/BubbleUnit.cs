using UnityEngine;
using System.Collections;

/// <summary>
/// Bubble unit.
/// </summary>
public class BubbleUnit : MonoBehaviour {

	public CircleCollider2D collider;

	public enum BubbleType
	{
		YellowBubble=0,
		RedBubble,
		BlueBubble,
		OrangeBubble,
		GreenBubble
	}

	public BubbleType bubbleType;
	public SpriteRenderer bubbleSpriteRen;
	// Use this for initialization
	void Start () {
		this.collider = GetComponent<CircleCollider2D> ();
	}
	
   public void SetData(int ID)
	{
		bubbleType = (BubbleType)ID;
		Sprite curSprite = SpriteManager.Instance.GetSprite ( bubbleType.ToString());
		bubbleSpriteRen.sprite = curSprite;
	}

	void OnMouseDown() {
		Debug.Log (bubbleType.ToString());
		BubbleManager.Instance.Clean (this);
	}


}
