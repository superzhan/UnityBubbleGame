using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MainPageBox : UIBoxBase {

	public Image SoundImage;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public override void ShowBox ()
	{
		gameObject.SetActive (true);

		AudioManager.Instance.PlayMusic ("UIMusic");
	}


    public void OnStartGameClick()
	{
		Debug.Log ("Start Game");
		UIManager.Instance.HideBox (UIBoxType.MainPageBox);
		UIManager.Instance.ShowBox (UIBoxType.GameBox);
		UIManager.Instance.ShowBox (UIBoxType.GameUIBox);

		BubbleManager.Instance.InitGame ();
		BubbleManager.Instance.isCanClick = true;

		AudioManager.Instance.ButtonClick ();
	}
	public void OnAboutClick()
	{
		UIManager.Instance.ShowBox (UIBoxType.AboutBox);
		AudioManager.Instance.ButtonClick ();
	}

	public void OnRecordBtnClick()
	{
		UIManager.Instance.ShowBox (UIBoxType.RecordBox);
		AudioManager.Instance.ButtonClick ();
	}
	public void OnSoundBtnClick()
	{
		if (AudioManager.Instance.isPlaying) {
			AudioManager.Instance.isPlaying = false;
			AudioController.PauseAll();

			SoundImage.color = new Color(0.8f,0.8f,0.8f);

		} else {
			AudioManager.Instance.isPlaying = true;
			AudioController.PlayMusic("UIMusic");
			SoundImage.color = new Color(1f,1f,1f);
		}
	}
}
