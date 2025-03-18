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
	public Vector2[] GuideMessagePositions;
	public Button RetractButton;
	public Button RestartButton;
	public Button ExitButton;
	public Button SkipGuideButton;
	public Button SettingsButton;

	public UIConfirmDialog ConfirmDialogPanel;
	public UIGameOverPanel GameOverPanel;

	void Start()
	{
		MainMessage.SetActive(false);
		SubMessage.SetActive(false);
		ConfirmDialogPanel.gameObject.SetActive(false);
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

	public void ShowConfirmDialog(string message, Action onConfirm)
	{
		if (ConfirmDialogPanel.gameObject.activeSelf)
			return;
		ConfirmDialogPanel.GetComponent<UIConfirmDialog>().Show(message, onConfirm);
	}

	public void SetCallbacks(Action onRetract, Action onRestart)
	{
		RetractButton.onClick.AddListener(onRetract.Invoke);
		RestartButton.onClick.AddListener(() =>
		{
			ShowConfirmDialog("确定要重开游戏吗？", onRestart);
		});
	}

	public void SetRetractable(bool retractable)
	{
		RetractButton.interactable = retractable;
	}

	public void SetGuideMode(bool guideMode, Action onExit)
	{
		SkipGuideButton.gameObject.SetActive(guideMode);
		ExitButton.gameObject.SetActive(!guideMode);
		var showButton = guideMode ? SkipGuideButton : ExitButton;
		showButton.onClick.RemoveAllListeners();
		showButton.onClick.AddListener(() =>
		{
			string msg = guideMode
				? "确定要跳过教程吗？\n你可以在菜单重新打开。"
				: "确定要退出游戏吗？\n当前对局和进度会自动保存。";
			ShowConfirmDialog(msg, onExit);
		});
		RetractButton.gameObject.SetActive(!guideMode);
		RestartButton.gameObject.SetActive(!guideMode);
		RestartButton.interactable = !guideMode;
	}


	public void ShowGuideMessage(string text, int position = 0)
	{
		GuideMessage.Init(text);
		GuideMessage.gameObject.SetActive(true);
		GuideMessage.GetComponent<RectTransform>().anchoredPosition = GuideMessagePositions[position];
	}

	public void ShowGuideMessageToConfirm(string text, Action onConfirm)
	{
		GuideMessage.Init(text, onConfirm);
		GuideMessage.gameObject.SetActive(true);
		GuideMessage.GetComponent<RectTransform>().anchoredPosition = GuideMessagePositions[0];
	}

	public void HideGuideMessage()
	{
		GuideMessage.gameObject.SetActive(false);
	}


	public void ShowGameOverPanel(GameResult result, Action onRetry, Action onContinue,
		List<GameFeature> unlockFeatures = null,
		string guideMessage = null)
	{
		var text = result switch
		{
			GameResult.Win => "你赢了",
			GameResult.Lose => "你输了",
			_ => "平局",
		};

		string subText = string.Empty;
		if (unlockFeatures != null)
			foreach (GameFeature feature in unlockFeatures)
				subText += $"\n解锁了勋章【{feature.Name}】\n{feature.Description}\n";

		GameOverPanel.Init(text,
			onRetry == null ? null : () => { HideGameOverPanel(); onRetry(); },
			onContinue == null ? null : () => { HideGameOverPanel(); onContinue(); },
			subText);
		if (guideMessage != null)
			ShowGuideMessage(guideMessage, 1);
		GameOverPanel.Show();
	}

	public void HideGameOverPanel()
	{
		GameOverPanel.Hide();
		GuideMessage.gameObject.SetActive(false);
	}
}
