using UnityEngine;
using System.Collections;


/// <summary>
/// Identifier tool.
/// </summary>
public class IDTool  {

	public static int GetType(int ID)
	{
		int type = ID / 10;
		return type;
	}

	public static int GetTypeID(int ID)
	{
		int typeID = ID % 10;
		return typeID;
	}

}
