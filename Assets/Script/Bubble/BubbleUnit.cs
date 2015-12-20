using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Bubble unit.
/// </summary>
public class BubbleUnit : MonoBehaviour {

    //data
	public int ID = 0;
	public int SingleScore = 0;

	public int hitCount =1;



	public CircleCollider2D bubCollider;

	public Image bubbleImage;
	

	
   public virtual void SetData(int ID)
	{
		this.ID = ID;
		this.SingleScore = BubbleData.Instance.GetSingleScore (ID);

		string spriteName = BubbleData.Instance.GetSpriteName (ID);
		Sprite curSprite = SpriteManager.Instance.GetSprite (spriteName);
		bubbleImage.sprite = curSprite;
	}

	/// <summary>
	/// Raises the mouse down event.
	/// </summary>
	void OnMouseDown() {
		BubbleManager.Instance.Clean (this);
	}

	public virtual void ReduceHitCount()
	{
		hitCount -= 1;
	}


}
