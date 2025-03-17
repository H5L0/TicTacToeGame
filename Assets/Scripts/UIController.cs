using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
	public GameObject MainMessage;
	public GameObject SubMessage;

	public TMP_Text StatusText;
	public TMP_Text LevelInfoText;
	public UIGuideMessage GuideMessage;
	public Button RetractButton;
	public Button ExitButton;
	public Button SettingsButton;

	public GameObject GameOverPanel;

	void Start()
	{
		MainMessage.SetActive(false);
		SubMessage.SetActive(false);
		HideGuideMessage();
		HideGameOverPanel();
	}


	public void ShowMainMessage(string message, int style = 0)
	{
		var go = Instantiate(MainMessage, transform);
		go.SetActive(true);
		go.GetComponent<UIObject>().SetText(message);
	}

	public void ShowSubMessage(string message, int style = 0)
	{
		var go = Instantiate(SubMessage, transform);
		go.SetActive(true);
		go.GetComponent<UIObject>().SetText(message);
	}

	public void SetStatusText(string text)
	{
		StatusText.text = text;
	}

	public void SetLevelInfoText(string text)
	{
		LevelInfoText.text = text;
	}

	//public void SetStatusTextOfPlayer(Player)
	//{
	//	StatusText.text = text;
	//}


	public void ShowGuideMessage(string text)
	{
		GuideMessage.Init(text);
		GuideMessage.gameObject.SetActive(true);
	}

	public void ShowGuideMessageToConfirm(string text, Action onConfirm)
	{
		GuideMessage.Init(text, onConfirm);
		GuideMessage.gameObject.SetActive(true);
	}

	public void HideGuideMessage()
	{
		GuideMessage.gameObject.SetActive(false);
	}


	public void ShowGameOverPanel(GameResult result, Action onRetry, Action onContinue,
		List<GameFeature> unlockFeatures = null,
		string guideMessage = null)
	{
		GameOverPanel.gameObject.SetActive(true);
		var panel = GameOverPanel.GetComponent<UIGameOverPanel>();
		var text = result switch
		{
			GameResult.Win => "你赢了！",
			GameResult.Lose => "你输了！",
			_ => "平局",
		};
		string subText = string.Empty;
		foreach (GameFeature feature in unlockFeatures)
		{
			subText += $"\n解锁了勋章【{feature.Name}】\n{feature.Description}";
		}
		panel.Init(text,
			onRetry == null ? null : () => { HideGameOverPanel(); onRetry(); },
			onContinue == null ? null : () => { HideGameOverPanel(); onContinue(); },
			subText);
		if (guideMessage != null)
			ShowGuideMessage(guideMessage);
	}

	public void HideGameOverPanel()
	{
		GameOverPanel.gameObject.SetActive(false);
		GuideMessage.gameObject.SetActive(false);
	}
}
