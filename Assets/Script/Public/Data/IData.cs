using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 数据读取的基类
/// </summary>
public class IData{

	//存数数据
	protected Hashtable DataTable;
	
	int DataRow;
	
	//设置读取数据表的名称
	protected void InitData(string fileName)
	{
		DataTable = new Hashtable();
		TextAsset binAsset = Resources.Load (fileName, typeof(TextAsset)) as TextAsset;

		//读取每一行的内容
		string[] lineArray = binAsset.text.Split ('\r');
		//创建二维数组
		string[][] levelArray = new string [lineArray.Length][];
		//把csv中的数据储存在二位数组中
		for(int i =0;i < lineArray.Length; i++)
		{
			levelArray[i] = lineArray[i].Split (',');
		}
		//将数据存储到哈希表中，存储方法：Key为name+id，Value为值
		int nRow = levelArray.Length;
		int nCol = levelArray[0].Length;
		
		DataRow = nRow - 1;
		
		for (int i = 1; i < nRow; ++i) 
		{
			if(levelArray[i][0]=="\n" || levelArray[i][0]=="")
			{
				nRow--;
				DataRow = nRow - 1;
				continue;
			}
			
			string id = levelArray[i][0].Trim();
			
			for (int j = 1; j < nCol; ++j) 
			{  
				DataTable.Add(levelArray[0][j] + "_" + id, levelArray[i][j]);
			}
		}
	}
	
	/// <summary>
	/// Gets the data row.
	/// 返回表格的行数
	/// </summary>
	/// <returns>
	/// The data row.
	/// </returns>
	public int GetDataRow() {
		return DataRow;
	}
	
	//根据name和id获取相关属性，返回string类型
	protected virtual string GetProperty(string name, int id)
	{
		return GetProperty(name, id.ToString());
	}
	
	protected virtual string GetProperty(string name, string id)
	{
		string key = name + "_" + id;
		if(DataTable.ContainsKey(key))
			return DataTable[key].ToString();
		else
			return "";
	}
	
	public void JustInit(){}
}