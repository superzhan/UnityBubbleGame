using UnityEngine;
using PathologicalGames;

public class OnCreatedDelegateExample : MonoBehaviour 
{
	private void Awake() 
	{
		PoolManager.Pools.AddOnCreatedDelegate("Audio", this.RunMe);
	}
	
	public void RunMe(SpawnPool pool)
	{
		Debug.Log("Delegate ran for pool " + pool.poolName);
	}
}
