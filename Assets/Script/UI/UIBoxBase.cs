using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 游戏UI模块的基类
/// 一个UI模块对应一个UIBox
/// </summary>
public class UIBoxBase : MonoBehaviour {
   public virtual void InitBox()
	{

	}

	public virtual void ShowBox()
	{
		this.gameObject.SetActive (true);		//Active Game Object
		this.transform.localScale = Vector3.zero;	// make size 0
		transform.DOScale (1, 0.3f).SetEase (Ease.OutBounce).OnComplete(ShowBoxFinish);
	}

	void ShowBoxFinish()
	{
		GameObject black = UIManager.Instance.Black;
		black.transform.parent = this.transform;
		black.transform.SetAsFirstSibling ();
	}

	public virtual void HideBox()
	{
		this.gameObject.SetActive (false);

		UIManager.Instance.Black.transform.parent = UIManager.Instance.transform;
	}
}
