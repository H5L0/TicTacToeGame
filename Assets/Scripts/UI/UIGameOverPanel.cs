using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIGameOverPanel : MonoBehaviour
{
	public TMP_Text TitleText;
	public TMP_Text SubText;
	public Button RetryButton;
	public Button ContinueButton;

	// Start is called before the first frame update
	void Start()
	{

	}

	public void Init(string text, Action onRetry = null, Action onContinue = null, string subText = null)
	{
		TitleText.text = text;
		SubText.text = subText;

		if (onContinue != null)
		{
			ContinueButton.gameObject.SetActive(true);
			ContinueButton.onClick.RemoveAllListeners();
			ContinueButton.onClick.AddListener(onContinue.Invoke);
		}
		else
		{
			ContinueButton.gameObject.SetActive(false);
		}

		if (onRetry != null)
		{
			RetryButton.gameObject.SetActive(true);
			RetryButton.onClick.RemoveAllListeners();
			RetryButton.onClick.AddListener(onRetry.Invoke);
		}
		else
		{
			RetryButton.gameObject.SetActive(false);
		}
	}

}
