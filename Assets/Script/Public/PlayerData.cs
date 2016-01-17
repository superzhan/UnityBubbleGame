using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 全局的数据存档类
/// </summary>
public class PlayerData : MonoBehaviour {

	private static PlayerData _instance=null;
	public static PlayerData Instance{
		get{
			if(_instance == null)
			{
				GameObject playerGO = new GameObject();
				playerGO.name= "PlayerData";

				_instance = playerGO.AddComponent<PlayerData>();
				//_instance.Init();

			}
			return _instance;
		}
	}


	public void Init()
	{
		if (PlayerPrefs.GetInt ("IsFirstInit", 0) == 0) {
			PlayerPrefs.SetInt("IsFirstInit",1);
		}
	}	


	public int GetHightestScore()
	{
		return PlayerPrefs.GetInt ("HightestScore",0);
	}
	public void SetHightestScore(int score)
	{
		PlayerPrefs.SetInt ("HightestScore",score);
	}




	public void SetGameTimes(int times)
	{
		PlayerPrefs.SetInt ("GameTimes",times);
	}
	public void AddGameTimes()
	{
		SetGameTimes(GetGameTimes() +1);
	}
	public int GetGameTimes()
	{
		return PlayerPrefs.GetInt ("GameTimes",0);
	}




	public void AddRecord(int score)
	{
		List<int> recordList = GetRecord ();
		recordList.Add (score);

		string recordStr = "";
		for (int i=0; i<recordList.Count; ++i) {
			recordStr += recordList[i].ToString();

			if(i!=recordList.Count-1)
			{
				recordStr+=",";
			}
		}
		PlayerPrefs.SetString ("Record",recordStr);


		if (score > GetHightestScore ()) {
			SetHightestScore(score);
		}
	}
	public List<int> GetRecord()
	{
		List<int> recordList = new List<int> ();

		string recordStr = PlayerPrefs.GetString("Record");

		if (recordStr == "") {
			return recordList;
		}

		string[] recordArry = recordStr.Split (',');
		for (int i=0; i<recordArry.Length; ++i) {
			int re = int.Parse(recordArry[i]); 
			recordList.Add(re);

			if(i>3)
				break;
		}

		return recordList;
	}

}
