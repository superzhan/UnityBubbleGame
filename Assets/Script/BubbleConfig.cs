using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class BubbleConfig : MonoBehaviour {

	public int sortCol=3;
	public float sortLen=62f;
	public bool isSortView = false;

	public bool isCreateConfigData=false;
	public string resConfigData="";

	public bool isShowConfig= false;
	public string showConfigData="";

#if UNITY_EDITOR

	void Update()
	{
		if (isCreateConfigData) {
			isCreateConfigData = false;
			CreateConfigData();
		}

		if (isSortView) {
			isSortView = false;
			SortView();
		}
	}


	void CreateConfigData()
	{
		//deal name
		for (int i=0; i<transform.childCount; i++) {
			string childName = transform.GetChild(i).name;
			int sIndex= childName.IndexOf(' ');

			if(sIndex >0)
			{
				childName =  childName.Remove(sIndex);
				transform.GetChild(i).name= childName;
			}
		}

		List<BubbleConfigStruct> infoList = new List<BubbleConfigStruct> ();

		for (int j=0; j<transform.childCount; ++j) {
			Transform child= transform.GetChild(j);

			BubbleConfigStruct info= new BubbleConfigStruct();

			info.id= child.GetComponent<BubbleUnit>().ID;
			info.prefabName= child.name;
			info.pos = child.localPosition;
			info.rotate = child.localRotation.eulerAngles;
			info.scale = child.localScale;

			infoList.Add(info);
		}

		resConfigData = "";
		for (int k =0; k<infoList.Count; ++k) {
			BubbleConfigStruct info = infoList[k];

			string str="";
			str+=info.id+"^";
			str+=info.prefabName+"^";
			str+=VecToStr(info.pos) +"^";
			str+=VecToStr(info.rotate) +"^";
			str+=VecToStr(info.scale);

			if(k!=infoList.Count -1)
			{
				str+="|";
			}

			resConfigData +=str;

		}


	}


	void SortView()
	{
		Transform[] trans = Selection.GetTransforms (SelectionMode.TopLevel);
		float len = sortLen;
		int x = 1;
		int y = 0;
		Vector3 startPos = trans [0].localPosition;

		int i = 1;
		while (i<trans.Length) {
			trans[i].localPosition = startPos + new Vector3(x*len,y*len,0);

			++x;
			++i;

			if(x>=sortCol)
			{
				x=0;
				y+=1;
			}
		}
	}

#endif

	string VecToStr(Vector3 vec)
	{
		string str = "";
		str+=vec.x +"*";
		str+=vec.y +"*";
		str += vec.z;

		return str;
	}



}

/// <summary>
/// 一个泡泡的结构体
/// </summary>
public class BubbleConfigStruct
{
	public int id;
	public string prefabName;
	public Vector3 pos;
	public Vector3 rotate;
	public Vector3 scale;
}
