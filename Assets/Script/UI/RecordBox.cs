using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class RecordBox : UIBoxBase {

	public Text scoreTx;
	public Text timeTx;
	public Text recordTx;

	public override void ShowBox ()
	{
		base.ShowBox ();

		scoreTx.text = PlayerData.Instance.GetHightestScore ().ToString ();
		timeTx.text = PlayerData.Instance.GetGameTimes ().ToString ();

		List<int> recordList = PlayerData.Instance.GetRecord ();
		string recordStr = "";
		for (int i=recordList.Count-1; i>=0; --i) {
			recordStr+=recordList[i] + "\r\n";
			//Debug.Log(recordList[i]);
		}
		recordTx.text = recordStr;

	}

	public void OnCloseBtnClick()
	{
		UIManager.Instance.HideBox (UIBoxType.RecordBox);
	}
}
