using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;


/// <summary>
/// 游戏UI模块的控制类
// 控制UI模块的显示和隐藏
/// </summary>
public class UIManager : MonoBehaviour {

	//singleton
	public static UIManager Instance = null;
	void Awake()
	{
		Instance = this;
	}

	public GameObject Black;

	public bool isPause=false;

	#region Define UIBox
	public GameObject mainPageBox;
	public GameObject recordBox;
	public GameObject aboutBox;

	public GameObject gameUIBox;
	public GameObject gameBox;
	public GameObject pauseBox;
	public GameObject gameEndBox;

	#endregion

	Hashtable uiBoxTable = new Hashtable();

	// Use this for initialization
	void Start () {
		Init ();
	}

	private void Init()
	{
		uiBoxTable.Add (UIBoxType.MainPageBox, mainPageBox);
		uiBoxTable.Add (UIBoxType.RecordBox, recordBox);
		uiBoxTable.Add (UIBoxType.AboutBox, aboutBox);

		uiBoxTable.Add (UIBoxType.GameUIBox, gameUIBox);
		uiBoxTable.Add (UIBoxType.GameBox, gameBox);
		uiBoxTable.Add (UIBoxType.PauseBox, pauseBox);
		uiBoxTable.Add (UIBoxType.GameEndBox, gameEndBox);
	}

	public void ShowBox(UIBoxType type)
	{
		GameObject boxGO =(GameObject)uiBoxTable [type];
		if (boxGO == null) {
			Debug.LogError("no this type");
		}

		boxGO.GetComponent<UIBoxBase> ().ShowBox ();
	}

	public void HideBox(UIBoxType type)
	{
		GameObject boxGO =(GameObject)uiBoxTable [type];
		if (boxGO == null) {
			Debug.LogError("no this type");
		}
		
		boxGO.GetComponent<UIBoxBase> ().HideBox ();
	}

	
  
}
public enum UIBoxType{
	MainPageBox,
	RecordBox,
	AboutBox,

	GameUIBox,
	GameBox,
	PauseBox,
	GameEndBox
}
