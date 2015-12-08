using UnityEngine;
using System.Collections;

/// <summary>
/// Sprite manager.
/// </summary>
public class SpriteManager : MonoBehaviour {

	public static SpriteManager Instance =null;

	public Sprite[] spriteList;

	void Awake()
	{
		Instance = this;
		DontDestroyOnLoad (gameObject);
	}


	public Sprite GetSprite(string name)
	{
		for (int i=0; i<spriteList.Length; ++i) {
		    if(spriteList[i].name == name)
			{
				return spriteList[i];
			}
		}
		return null;
	}

}
