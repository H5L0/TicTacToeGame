using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public enum GameEntryMode
{
	StartFromGuide,
	GuideOnly,
	StartFromFirstLevel,
	LoadFromLevel,
	JumpToLevel,
}

public static class ScenePassingData
{
	public static GameEntryMode EntryMode;
	public static LevelState LoadLevelState;
	public static BoardContext LoadBoardContext;
	public static int JumpToLevelId;
}

public class MenuController : MonoBehaviour
{
	public Button StartButton;
	public Button ContinueButton;
	public Button RestartButton;
	public Button MedalButton;
	public Button GuideButton;
	public Button SettingButton;
	public Button ExitButton;
	public Button DebugButton;

	public GameObject LevelPanel;
	public Button NormalLevelButton;
	public Button[] LevelButtons;
	public Button LevelReturnButton;

	public GameObject MedalPanel;
	public Transform MedalRoot;
	public GameObject MedalObject;
	public GameObject MedalLockedObject;
	public Button MedalReturnButton;


	// Start is called before the first frame update
	void Start()
	{
		bool haveReadGuidance = StorageManager.Instance.HaveReadGuidance;
		if (!haveReadGuidance)
		{
			StartButton.onClick.AddListener(OnClickStart);
			ContinueButton.gameObject.SetActive(false);
			RestartButton.gameObject.SetActive(false);
			GuideButton.gameObject.SetActive(false);
		}
		else if (StorageManager.Instance.TryGetLevelState(out var levelState))
		{
			StartButton.gameObject.SetActive(false);
			ContinueButton.onClick.AddListener(OnClickStart);
			RestartButton.onClick.AddListener(OnClickRestart);
		}
		else
		{
			StartButton.onClick.AddListener(OnClickStart);
			ContinueButton.gameObject.SetActive(false);
			RestartButton.gameObject.SetActive(false);
		}

		GuideButton.onClick.AddListener(StartGuideOnlyGame);
		MedalButton.onClick.AddListener(OnClickOpenMedalPanel);
		//SettingButton.onClick.AddListener(OnClickSetting);
		ExitButton.onClick.AddListener(OnClickExit);

		LevelPanel.SetActive(false);
		MedalPanel.SetActive(false);
		LevelReturnButton.onClick.AddListener(OnClickQuitLevelPanel);
		MedalReturnButton.onClick.AddListener(OnClickQuitMedalPanel);

		DebugButton.onClick.AddListener(() =>
		{
			StorageManager.Instance.ClearAllData();
			SceneHelper.FadeLoadScene("MenuScene");
		});

		SoundHelper.PlayBgm(Sound.BGM_Menu);
	}


	void OnClickStart()
	{
		bool haveReadGuidance = StorageManager.Instance.HaveReadGuidance;
		if (!haveReadGuidance)
		{
			SoundHelper.PlaySfx(Sound.Click);
			StartIntroGame();
		}
		else if (StorageManager.Instance.TryGetLevelState(out var levelState))
		{
			SoundHelper.PlaySfx(Sound.Click);
			ContinueGame(levelState);
		}
		else
		{
			OnClickRestart();
		}
	}

	void OnClickRestart()
	{
		var feature0 = GameSettings.Instance.Features[0];
		bool haveUnlockLevel = StorageManager.Instance.IsFeatureUnlock(feature0.Key);
		if (haveUnlockLevel)
		{
			OnClickLevelPanel();
		}
		else
		{
			SoundHelper.PlaySfx(Sound.Click);
			StartChallengeGame();
		}
	}

	void StartIntroGame()
	{
		ScenePassingData.EntryMode = GameEntryMode.StartFromGuide;
		SceneHelper.FadeLoadScene("MainScene");
	}

	void ContinueGame(LevelState levelState)
	{
		ScenePassingData.EntryMode = GameEntryMode.LoadFromLevel;
		ScenePassingData.LoadLevelState = levelState;
		StorageManager.Instance.TryGetBoardContext(out var boardContext);
		ScenePassingData.LoadBoardContext = boardContext;
		StorageManager.Instance.DeleteLevelState();
		SceneHelper.FadeLoadScene("MainScene");
	}

	void StartChallengeGame()
	{
		StorageManager.Instance.DeleteLevelState();
		ScenePassingData.EntryMode = GameEntryMode.StartFromFirstLevel;
		SceneHelper.FadeLoadScene("MainScene");
	}

	void StartLevelGame(int levelId)
	{
		StorageManager.Instance.DeleteLevelState();
		ScenePassingData.EntryMode = GameEntryMode.JumpToLevel;
		ScenePassingData.JumpToLevelId = levelId;
		SceneHelper.FadeLoadScene("MainScene");
	}

	void StartGuideOnlyGame()
	{
		ScenePassingData.EntryMode = GameEntryMode.GuideOnly;
		SceneHelper.FadeLoadScene("MainScene");
	}

	void OnClickLevelPanel()
	{
		SoundHelper.PlaySfx(Sound.Click);
		LevelPanel.SetActive(true);
		NormalLevelButton.onClick.AddListener(StartChallengeGame);
		for (int i = 0; i < 3; i++)
		{
			var feature = GameSettings.Instance.Features[i];
			bool unlocked = StorageManager.Instance.IsFeatureUnlock(feature.Key);
			LevelButtons[i].gameObject.SetActive(unlocked);
			int levelId = 1000 + i;
			LevelButtons[i].onClick.AddListener(() => StartLevelGame(levelId));
		}
	}



	void OnClickQuitLevelPanel()
	{
		SoundHelper.PlaySfx(Sound.Click);
		LevelPanel.SetActive(false);
	}

	void OnClickOpenMedalPanel()
	{
		SoundHelper.PlaySfx(Sound.Click);
		MedalPanel.SetActive(true);
		MedalObject.SetActive(false);
		MedalLockedObject.SetActive(false);
		for (int i = 2; i < MedalRoot.childCount; i++)
			Destroy(MedalRoot.GetChild(i).gameObject);
		foreach (var feature in GameSettings.Instance.Features)
		{
			bool unlocked = StorageManager.Instance.IsFeatureUnlock(feature.Key);
			var go = Instantiate(unlocked ? MedalObject : MedalLockedObject, MedalRoot);
			go.SetActive(true);
			go.GetComponent<UIObject>().SetText(feature.Name);
		}
	}

	void OnClickQuitMedalPanel()
	{
		SoundHelper.PlaySfx(Sound.Click);
		MedalPanel.SetActive(false);
	}

	//void OnClickSetting()
	//{
	//	
	//}

	void OnClickExit()
	{
		SoundHelper.PlaySfx(Sound.Click);
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
