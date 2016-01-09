using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PathologicalGames;

/// <summary>
/// 泡泡的管理类 
/// </summary>
public class BubbleManager : MonoBehaviour {

	public static BubbleManager Instance = null;


	//当前的泡泡
	public List<BubbleUnit> bubbleList = new List<BubbleUnit>();


	SpawnPool pool;


	void Awake()
	{
		Instance = this;
	}

	void Start () {
		pool = PoolManager.Pools["BubblePool"];
		Init ();
//		if (testLayoutData != "") {
//			layoutData = testLayoutData;
//		} else {
//			layoutData = BubbleLayoutData.Instance.GetData (bubbleLayoutID);
//		}
//
//		InitByLayoutData (layoutData);
	}


	#region  TestCode

	void Init()
	{
		for (int i=0; i<60; ++i) {
			CreateNewRandomBubble();
		}

		CreateStoneBubble ();
	}

    public void CreateNewRandomBubble()
	{

		
		int randonID =10+ Random.Range (1, 6);
		Transform newBubble = GetBubbleTran (randonID);
		newBubble.parent = transform;

		BubbleUnit bubble=newBubble.GetComponent<BubbleUnit> ();
		bubble.SetData (randonID);
		bubbleList.Add(bubble);
		
		Vector3 randPos = new Vector3 (-200f+400*Random.value,410f+200*Random.value,0f);
		newBubble.transform.localPosition = randPos;
	}

	void CreateStoneBubble()
	{
		Vector3 startPos = new Vector3 (-210f,-210f, 0);
		float len = 52f;
		for (int i=0; i<5; ++i) {
		   

			int randonID =40+ Random.Range (1, 6);

			Transform newBubble = GetBubbleTran (randonID);
			newBubble.parent = transform;

			BubbleUnit bubble=newBubble.GetComponent<BubbleUnit> ();
			bubble.SetData (randonID);
			bubbleList.Add(bubble);

			Vector3 pos = startPos + new Vector3(i*len,0f,0f);
			bubble.transform.localPosition = pos;

		}
	}

	#endregion

	/// <summary>
	/// 触发游戏的消除
	/// </summary>
	/// <param name="bubble">Bubble.</param>
	public void Clean(BubbleUnit bubble)
	{
		Vector3 bubPos = bubble.transform.localPosition;



		//clean bubble
		List<BubbleUnit> linkList = GetLinkBubble (bubble);

		CreateSpecialBubble(linkList.Count,bubPos);

		int recycleCount = 0;
		if (linkList.Count > 2) {
			int i=0;
			while(i<linkList.Count)
			{
				linkList[i].ReduceHitCount();
				if(linkList[i].hitCount <=0)
				{
					recycleCount++;
				}

				RecycleBubble(linkList[i]);
				++i;
			}


			for (int k=0; k<recycleCount; ++k) {
				CreateNewRandomBubble();
			}
		}



	}

	/// <summary>
	/// 消除多个泡泡生成特殊泡泡
	/// </summary>
	void CreateSpecialBubble(int cleanCount,Vector3 pos)
	{
		if (cleanCount < 4) {
			return;
		}

		Transform bub;
		int randomID=21;
		if (cleanCount == 4) {
			randomID = 20 + Random.Range (1, 3);
		
		} else if (cleanCount == 5) {
			randomID = 24;
		} else if (cleanCount == 6) {
			randomID = 23;
		} else if (7 <= cleanCount && cleanCount <= 9) {
			randomID = 25;
		} else   {
			randomID =26;
		}

		bub = GetBubbleTran (randomID);
		BubbleUnit bubble = bub.GetComponent<BubbleUnit> ();
		bubble.SetData (randomID);

		bub.transform.parent = transform;
		bub.transform.localPosition = pos;

		bubbleList.Add (bubble);

	}

	/// <summary>
	/// 回收一个泡泡
	/// </summary>
	/// <param name="bubble">Bubble.</param>
	public void RecycleBubble(BubbleUnit bubble)
	{
		if (bubbleList.Contains (bubble) == false) {
			return;
		}

		if (bubble.hitCount > 0) {
			return;
		}

		EffectPool.Instance.Play("BubbleExplode",bubble.transform.position);
		bubbleList.Remove(bubble);
		pool.Despawn(bubble.transform);
	}


	/// <summary>
	/// 广度优先搜索 查找相连的 泡泡
	/// </summary>
	/// <returns>The link bubble.</returns>
	/// <param name="bubble">Bubble.</param>
	List<BubbleUnit> GetLinkBubble(BubbleUnit bubble)
	{
		List<BubbleUnit> resList = new List<BubbleUnit> ();
		List<BubbleUnit> tempList = new List<BubbleUnit> ();

		tempList.AddRange (bubbleList);

		//remove different bubble
		int index = 0;
		while (index <tempList.Count) {
		    if(IDTool.GetTypeID( tempList[index].ID ) != IDTool.GetTypeID( bubble.ID))
			{
				tempList.RemoveAt(index);
			}else
			{
				index++;
			}
		}

		//get the link bubble of select one
		resList.Add (bubble);
		tempList.Remove (bubble);
		int i = 0;
		while (i<resList.Count) {
			BubbleUnit curBub = resList[i];

			int j=0;
			while(j<tempList.Count)
			{
				if( IsTouch(curBub,tempList[j]) )
				{
					resList.Add(tempList[j]);
					tempList.RemoveAt(j);
				}else
				{
					++j;
				}

			}

			++i;
		   
		}
		return resList;
	}

	/// <summary>
	/// 判断两个泡泡是否相连
	/// </summary>
	/// <returns><c>true</c> if this instance is touch the specified a b; otherwise, <c>false</c>.</returns>
	/// <param name="a">The alpha component.</param>
	/// <param name="b">The blue component.</param>
	bool IsTouch(BubbleUnit a, BubbleUnit b)
	{
		float distance = Vector3.Distance (a.transform.localPosition, b.transform.localPosition);

		float radiusA = a.bubCollider.radius;
		float radiusB = b.bubCollider.radius;

		float offset = 5f;

		float calDistance = radiusA + radiusB + offset;
		if (calDistance >= distance) {
			return true;
		} else {
			return false;
		}
	}


	/// <summary>
	/// 特殊泡泡的功能 消除同样颜色的泡泡
	/// </summary>
	public void ClearSameColorBubble(BubbleUnit bubble)
	{
		int typeID = Random.Range (1, 6);

		List<BubbleUnit> colorBubList = new List<BubbleUnit> ();

		for (int i=0; i<this.bubbleList.Count; ++i) {
			BubbleUnit curBub= bubbleList[i];
			int curType = IDTool.GetType(curBub.ID);
			int curTypeID = IDTool.GetTypeID(curBub.ID);

			if( curType !=2 && curTypeID == typeID)
			{
				colorBubList.Add(curBub);
			}
		}

		colorBubList.Add (bubble);

		for (int j=0; j<colorBubList.Count; ++j) {
			RecycleBubble(colorBubList[j]);
		}

	}

	/// <summary>
	/// 特殊泡泡效果 全屏消除
	/// </summary>
	public void CleanAllBubble()
	{
		List<BubbleUnit> tempBubbleList = new List<BubbleUnit> ();
		tempBubbleList.AddRange (bubbleList);

		for (int i=0; i<tempBubbleList.Count; ++i) {
			RecycleBubble(tempBubbleList[i]);
		}
	}

	#region

	public int bubbleLayoutID=0;
	public string testLayoutData="";
	private string layoutData="";
	public List<BubbleConfigStruct> layoutList = new List<BubbleConfigStruct>();

	/// <summary>
	/// 根据配置文件 初始化泡泡布置
	/// </summary>
	public void InitByLayoutData(string layoutStr)
	{

		string[] items = layoutStr.Split ('|');

		for (int i=0; i<items.Length; ++i) {
			string oneItem= items[i];
			string[] itemInfos= oneItem.Split('^');
			BubbleConfigStruct info= new BubbleConfigStruct();
			info.id = int.Parse(itemInfos[0]);
			info.prefabName = itemInfos[1];
			info.pos= StrToVec(itemInfos[2]);
			info.rotate= StrToVec(itemInfos[3]);
			info.scale = StrToVec(itemInfos[4]);

			layoutList.Add(info);
		}

		for (int j=0; j<layoutList.Count; ++j) {
			BubbleConfigStruct info = layoutList[j];

			Transform newBubble = GetBubbleTran(info.id);
			newBubble.parent = transform;

			BubbleUnit bubble=newBubble.GetComponent<BubbleUnit> ();
			bubble.SetData (info.id);
			bubbleList.Add(bubble);

			newBubble.transform.localPosition = info.pos;
			newBubble.transform.localRotation = Quaternion.Euler(info.rotate);
			newBubble.transform.localScale = info.scale;
		}
	}

	Vector3 StrToVec(string str)
	{
		string[] strVec = str.Split ('*');
		Vector3 vec = new Vector3 (float.Parse(strVec[0]),float.Parse(strVec[1]),float.Parse(strVec[2]));
		return vec;
	}

	Transform GetBubbleTran(int ID)
	{
		int type = IDTool.GetType (ID);
		if (type == 1) {
			return pool.Spawn("BubbleUnit");
		}
		if (type == 2) {
			return pool.Spawn("SpecialBubbleUnit");
		}
		if (type == 3) {
			return pool.Spawn("FreezeBubbleUnit");
		}
		if (type == 4) {
			return pool.Spawn("StoneBubbleUnit");
		}

		return null;
	}

	#endregion
}
