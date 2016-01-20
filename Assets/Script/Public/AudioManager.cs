using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour {

	public static AudioManager Instance=null;

	public bool isPlaying= true;

	void Awake()
	{
		Instance = this;
	}
    

	public void PlayMusic(string music)
	{
		if (isPlaying == false) {
			return;
		}

		AudioController.PlayMusic (music);
	}

	public void Play(string sound)
	{
		if (isPlaying == false) {
			return;
		}

		AudioController.Play (sound);
	}

	public void ButtonClick()
	{
		if (isPlaying == false) {
			return;
		}
		Play ("Button");
	}

	public void PlayBubble()
	{
		if (isPlaying == false) {
			return;
		}
		int index = Random.Range (1, 3);
		Play ("Boom"+index);
	}
}
