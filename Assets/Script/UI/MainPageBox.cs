using UnityEngine;
using System.Collections;

public class MainPageBox : UIBoxBase {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}



    public void OnStartGameClick()
	{
		Debug.Log ("Start Game");
		UIManager.Instance.HideBox (UIBoxType.MainPageBox);
		UIManager.Instance.ShowBox (UIBoxType.GameBox);
		UIManager.Instance.ShowBox (UIBoxType.GameUIBox);
	}
	public void OnAboutClick()
	{
		UIManager.Instance.ShowBox (UIBoxType.AboutBox);
	}

	public void OnRecordBtnClick()
	{
		UIManager.Instance.ShowBox (UIBoxType.RecordBox);
	}
	public void OnSoundBtnClick()
	{

	}
}
