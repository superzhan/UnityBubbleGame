using UnityEngine;
using System.Collections;

public class GameEndBox : UIBoxBase {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void OnPlayAgainBtnClick()
	{

		UIManager.Instance.HideBox (UIBoxType.GameEndBox);
		UIManager.Instance.HideBox (UIBoxType.GameBox);
		UIManager.Instance.HideBox (UIBoxType.GameUIBox);
		UIManager.Instance.ShowBox (UIBoxType.MainPageBox);
	}
}
