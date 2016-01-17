using UnityEngine;
using System.Collections;

public class AboutBox : UIBoxBase {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void OnCloseBtnClick()
	{
		Debug.Log ("about Close");
		UIManager.Instance.HideBox (UIBoxType.AboutBox);
	}
}
