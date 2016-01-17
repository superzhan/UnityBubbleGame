using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// 游戏结束
/// </summary>
public class GameEndBox : UIBoxBase {

	public Text scoreTx;
	public Text hightScoreTx;



	public override void ShowBox ()
	{
		base.ShowBox ();

		BubbleManager.Instance.isCanClick = false;

		int score = BubbleManager.Instance.score;
		scoreTx.text = score.ToString();

		PlayerData.Instance.AddGameTimes ();
		PlayerData.Instance.AddRecord (score);

		int hightestScore = PlayerData.Instance.GetHightestScore ();
		hightScoreTx.text = hightestScore.ToString();
	}

	public void OnPlayAgainBtnClick()
	{

		UIManager.Instance.HideBox (UIBoxType.GameEndBox);
		UIManager.Instance.HideBox (UIBoxType.GameBox);
		UIManager.Instance.HideBox (UIBoxType.GameUIBox);
		UIManager.Instance.ShowBox (UIBoxType.MainPageBox);
	}
}
