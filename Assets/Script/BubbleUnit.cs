using UnityEngine;
using System.Collections;

/// <summary>
/// Bubble unit.
/// </summary>
public class BubbleUnit : MonoBehaviour {

	public Sprite sp;

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
	
	}
	
   public void SetData(int ID)
	{
		bubbleType = (BubbleType)ID;
		Sprite curSprite = SpriteManager.Instance.GetSprite ( bubbleType.ToString());
		bubbleSpriteRen.sprite = curSprite;
	}


}
