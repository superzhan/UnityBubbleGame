using UnityEngine;
using System.Collections;

/// <summary>
/// Bubble data.
/// </summary>
public class BubbleData : IData {

	static string dataName="";
	static BubbleData _instance = null;
	public static BubbleData Instance{
		get{
			if(_instance == null)
			{
				_instance = new BubbleData();
				dataName="Data/BubbleData";
				_instance.InitData(dataName);
			}
			return _instance;
		}
	}

	public void Refresh()
	{
		InitData (dataName);
	}

	public string GetSpriteName(int id)
	{
		return GetProperty("SpriteName",id);
	}

	public int GetSingleScore(int id)
	{
		string str = GetProperty ("SingleScore",id);
		int sc = int.Parse (str);
		return sc;
	}
}
