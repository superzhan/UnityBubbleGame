using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;

public class GameUIBox : UIBoxBase {

	public Text scoreText;
	public Text timeText;
	public Text hightestText;


	public int totalTime = 90;
	private float timeCal=0;

	private int scoreTemp=0;

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

		BubbleManager.Instance.scoreChangeEvent += ScoreChange;

		hightestText.text = PlayerData.Instance.GetHightestScore ().ToString();

		AudioManager.Instance.PlayMusic ("GameMusic");
	} 

	public override void HideBox ()
	{
		base.HideBox ();

		BubbleManager.Instance.scoreChangeEvent -= ScoreChange;
	}


	void ScoreChange(int score)
	{
		DOTween.To (() => this.scoreTemp, x => this.scoreTemp = x, score, 0.6f).OnUpdate (ScoreChangeUpdate);;
	}
	void ScoreChangeUpdate()
	{
		scoreText.text = this.scoreTemp.ToString ();
	}




	public void OnPauseBtnClick()
	{
		UIManager.Instance.ShowBox (UIBoxType.PauseBox);
		UIManager.Instance.isPause = true;

		AudioManager.Instance.ButtonClick ();
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
