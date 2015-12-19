using UnityEngine;
using System.Collections;

/// <summary>
/// Bubble unit.
/// </summary>
public class BubbleUnit : MonoBehaviour {

    //data
	public int ID = 0;
	public int SingleScore = 0;


	public CircleCollider2D bubCollider;

	public SpriteRenderer bubbleSpriteRen;
	
	// Use this for initialization
	void Start () {
		this.bubCollider = GetComponent<CircleCollider2D> ();
	}
	
   public virtual void SetData(int ID)
	{
		this.ID = ID;
		this.SingleScore = BubbleData.Instance.GetSingleScore (ID);

		string spriteName = BubbleData.Instance.GetSpriteName (ID);
		Sprite curSprite = SpriteManager.Instance.GetSprite (spriteName);
		bubbleSpriteRen.sprite = curSprite;
	}

	/// <summary>
	/// Raises the mouse down event.
	/// </summary>
	void OnMouseDown() {
		BubbleManager.Instance.Clean (this);
	}


}
