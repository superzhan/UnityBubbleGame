using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[ExecuteInEditMode]
public class EditorBubble : MonoBehaviour {

	public bool isRefreshView= false;
	BubbleUnit bubble;
	int preID=0;

#if UNITY_EDITOR

	// Use this for initialization
	void Start () {
		bubble = GetComponent<BubbleUnit> ();
		preID = bubble.ID;
	}
	
	// Update is called once per frame
	void Update () {

		if (preID != bubble.ID) {
			SetView();
		}
	}

	void SetView()
	{
		string spName = BubbleData.Instance.GetSpriteName (bubble.ID);
		Sprite sp =  GameObject.FindObjectOfType<SpriteManager>().GetSpriteInEditor (spName); 
		bubble.bubbleImage.sprite = sp;
	}

#endif

}
