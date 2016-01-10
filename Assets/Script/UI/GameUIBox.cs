using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GameUIBox : UIBoxBase {

	public Text scoreText;
	public Text timeText;

	public int totalTime = 90;
	private float timeCal=0;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public override void ShowBox ()
	{
		this.gameObject.SetActive (true);
		StartCoroutine ("TimeDown");
	} 

	public void OnPauseBtnClick()
	{
		UIManager.Instance.ShowBox (UIBoxType.PauseBox);
		UIManager.Instance.isPause = true;
	}


	IEnumerator TimeDown()
	{
		timeCal = totalTime;
		while (true) {
		    while(UIManager.Instance.isPause ==true)
			{
				yield return 0;
			}

			timeCal -=Time.deltaTime;
			timeText.text = ((int)timeCal).ToString();

			if(timeCal <= 0)
			{
				GameEnd();

				StopCoroutine("TimeDown");
			}


			yield return 0;
		}
	}

	void GameEnd()
	{
		UIManager.Instance.ShowBox (UIBoxType.GameEndBox);
	}
}
