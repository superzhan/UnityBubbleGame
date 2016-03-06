using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

/// <summary>
/// Sprite manager.
/// </summary>
[ExecuteInEditMode]
public class SpriteManager : MonoBehaviour {

	public static SpriteManager Instance =null;

	
	public bool isRreshPrefab;
	public string rootPath = "/Image/";


	public List<Sprite> spriteList;
	public Hashtable spriteTable = new Hashtable();

	void Awake()
	{
		Instance = this;
		DontDestroyOnLoad (gameObject);

		for (int i=0; i<spriteList.Count; ++i) {
			spriteTable.Add(spriteList[i].name,spriteList[i]);
		}
	}
	void Start()
	{
		Instance = this;
	}


	public Sprite GetSprite(string name)
	{
		Sprite sp = (Sprite)spriteTable [name];
		//Debug.Log (spriteTable.Count);
		return sp;
	}

	public Sprite GetSpriteInEditor(string name)
	{
		for (int i=0; i<spriteList.Count; ++i) {
			if(name == spriteList[i].name)
			{
				return spriteList[i];
			}
		}

		return null;
	}


	#if UNITY_EDITOR

	List<string> objPaths = new List<string>();
	// Update is called once per frame
	void Update () {
		
		if (isRreshPrefab) {
			isRreshPrefab = false;

			spriteList.Clear();
			spriteTable.Clear();
		
			string objPath  = Application.dataPath + rootPath; 

			objPaths.Clear();
			DirectoryInfo d = new DirectoryInfo(objPath);
			GetAll(d);

			for(int i = 0; i < objPaths.Count; i++)
			{
				Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(objPaths[i]);
				spriteList.Add(sp);

			}
		
			PrefabUtility.ReplacePrefab(gameObject, PrefabUtility.GetPrefabParent(gameObject), ReplacePrefabOptions.ConnectToPrefab);
			
		}
	}
	
	void  GetAll(DirectoryInfo dir)//搜索文件夹中的文件
	{
		FileInfo[] allFile = dir.GetFiles();
		foreach (FileInfo fi in allFile)
		{
			if(fi.Extension==".png" || fi.Extension ==".jpg")
			{
				string p = fi.FullName;
				p=p.Substring(p.IndexOf("Assets"));
				objPaths.Add(p);
			}

		}
		
		DirectoryInfo[] allDir= dir.GetDirectories();
		foreach (DirectoryInfo d in allDir)
		{
			GetAll(d);
		} ;
	}

	#endif

}
