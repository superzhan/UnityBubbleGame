using UnityEngine;
using System.Collections;

/// <summary>
/// 特殊的泡泡
/// </summary>
public class SpecialBubbleUnit : BubbleUnit {

	public CircleCollider2D rangeCollider;
	public BoxCollider2D lineCollider;


	public override void SetData (int ID)
	{
		base.SetData (ID);
	}


	public void DealEffectCleanBubble(BubbleUnit bubble)
	{
		if (bubble != this) {
			BubbleManager.Instance.RecycleBubble(bubble);
		}
	}


	/// <summary>
	/// Raises the mouse down event.
	/// </summary>
	void OnMouseDown() {
		BubbleManager.Instance.RecycleBubble (this);
	}

	private void CleanRow()
	{
		Transform left = Instantiate (lineCollider.transform);
		Transform right = lineCollider.transform;


	}

	private void CleanCol()
	{

	}

	private void CleanRowCol()
	{

	}


}
