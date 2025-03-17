using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public static class ScenePassingData
{
	public static bool EnterGuide;
	public static bool ExitAfterGuideFinished;
}

public class MenuController : MonoBehaviour
{
	public Button StartButton;
	public Button LevelSelectButton;
	public Button GuideButton;
	public Button SettingButton;
	public Button ExitButton;


	// Start is called before the first frame update
	void Start()
	{
		bool haveReadGuidance = StorageManager.Instance.HaveReadGuidance;
		if (!haveReadGuidance)
		{
			GuideButton.gameObject.SetActive(false);
			LevelSelectButton.gameObject.SetActive(false);
		}

		StartButton.onClick.AddListener(OnClickStart);
		GuideButton.onClick.AddListener(OnClickGuide);
		LevelSelectButton.onClick.AddListener(OnClickLevelSelect);
		SettingButton.onClick.AddListener(OnClickSetting);
		ExitButton.onClick.AddListener(OnClickExit);
	}


	void OnClickStart()
	{
		bool haveReadGuidance = StorageManager.Instance.HaveReadGuidance;
		ScenePassingData.EnterGuide = !haveReadGuidance;
		ScenePassingData.ExitAfterGuideFinished = false;
		SceneManager.LoadScene("MainScene");
	}

	void OnClickGuide()
	{
		ScenePassingData.EnterGuide = true;
		ScenePassingData.ExitAfterGuideFinished = true;
		SceneManager.LoadScene("MainScene");
	}

	void OnClickLevelSelect()
	{
		SceneManager.LoadScene("LevelSelectScene");
	}

	void OnClickSetting()
	{
		SceneManager.LoadScene("SettingScene");
	}

	void OnClickExit()
	{
		#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
		#else
			Application.Quit();
		#endif
	}
}
