using UnityEngine;
using System.Collections;

public class PauseBox : UIBoxBase {

	public void OnPlayBtnClick()
	{
		UIManager.Instance.HideBox (UIBoxType.PauseBox);
		UIManager.Instance.isPause = false;
		AudioManager.Instance.ButtonClick ();
	}
}
