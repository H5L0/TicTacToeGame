using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIGuideMessage : MonoBehaviour
{
	public TMP_Text Text;
	public Button Button;

	public void Init(string text)
	{
		Text.text = text;
		Button.onClick.RemoveAllListeners();
		Button.gameObject.SetActive(false);
	}

	public void Init(string text, Action onConfirm)
	{
		Text.text = text;
		Button.onClick.RemoveAllListeners();
		Button.onClick.AddListener(onConfirm.Invoke);
		Button.gameObject.SetActive(true);
	}
}
