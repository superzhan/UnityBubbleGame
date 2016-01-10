using UnityEngine;
using System.Collections;

public class GameUIBox : UIBoxBase {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public override void ShowBox ()
	{
		this.gameObject.SetActive (true);
	} 
}
