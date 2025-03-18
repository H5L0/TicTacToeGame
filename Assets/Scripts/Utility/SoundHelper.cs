using UnityEngine;
using System.Collections.Generic;
using System;

public enum Sound
{
	None,
	DoMove = 10,
	GameStart,
	Click,
	Win = 100,
	Lose,
	Tie,
	Retract = 110,
	DrawLine,

	BGM_Menu = 1000,
	BGM_Main,
}

public class SoundHelper : MonoBehaviour
{
	static SoundHelper _instance;
	public static SoundHelper Instance
	{
		get
		{
			if (_instance == null)
				_instance = CreateInstance();
			return _instance;
		}
	}

	public static SoundHelper CreateInstance()
	{
		var prefab = Resources.Load<GameObject>("Prefabs/SoundHelper");
		var obj = Instantiate(prefab);
		var mgr = obj.GetComponent<SoundHelper>();
		mgr.Init();
		GameObject.DontDestroyOnLoad(obj);
		return mgr;
	}


	public AudioSource globalSfxAudioSouce;
	public AudioSource bgmAudioSouce;


	[Serializable]
	public class SoundDefine
	{
		public Sound sound;
		public AudioClip clip;
	}

	public SoundDefine[] sounds;

	Dictionary<Sound, AudioClip> soundsDict = new Dictionary<Sound, AudioClip>();

	private void Init()
	{
		foreach (var d in sounds)
		{
			soundsDict[d.sound] = d.clip;
		}
	}

	public static void PlaySfx(Sound sound)
	{
		Instance.DoPlaySfx(sound);
	}

	public static void PlayBgm(Sound sound)
	{
		Instance.DoPlayBgm(sound);
	}

	public void DoPlaySfx(Sound sound)
	{
		if (soundsDict.TryGetValue(sound, out AudioClip clip))
		{
			bgmAudioSouce.PlayOneShot(clip);
		}
		else
		{
			Debug.LogWarning("No such sound " + sound);
		}
	}

	public void DoPlayBgm(Sound sound)
	{
		if (soundsDict.TryGetValue(sound, out AudioClip clip))
		{
			bgmAudioSouce.clip = clip;
			bgmAudioSouce.Play();
			//bgmAudioSouce.loop = true;  //prefab里设定
		}
		else
		{
			Debug.LogWarning("No such sound " + sound);
		}
	}

}

