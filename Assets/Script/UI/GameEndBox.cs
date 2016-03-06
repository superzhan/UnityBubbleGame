using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// 游戏结束
/// </summary>
public class GameEndBox : UIBoxBase {

	public Text scoreTx;
	public Text hightScoreTx;

	private int adTime=0;

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


#if UNITY_EDITOR
		return;

#endif

#if UNITY_ANDROID

//		if(adTime%2==0)
//			return;
//
//		adTime++;

		Debug.Log("Call Add");
		//show add
		AndroidJavaClass androidJC;
		AndroidJavaObject androidJO;
		androidJC = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		androidJO = androidJC.GetStatic<AndroidJavaObject>("currentActivity");
		
		androidJO.Call("CallAd");
#endif

	}

	public void OnPlayAgainBtnClick()
	{

		UIManager.Instance.HideBox (UIBoxType.GameEndBox);
		UIManager.Instance.HideBox (UIBoxType.GameBox);
		UIManager.Instance.HideBox (UIBoxType.GameUIBox);
		UIManager.Instance.ShowBox (UIBoxType.MainPageBox);

		AudioManager.Instance.ButtonClick ();
	}
}
