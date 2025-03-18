using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIConfirmDialog : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMPro.TMP_Text messageText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [SerializeField] private float fadeTime = 0.3f;
    
    private Action onConfirm;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClick);
        cancelButton.onClick.AddListener(OnCancelClick);
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Show(string message, Action confirmCallback)
    {
        gameObject.SetActive(true);
        messageText.text = message;
        onConfirm = confirmCallback;
        
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, fadeTime);
    }

    private void OnConfirmClick()
    {
        onConfirm?.Invoke();
        Hide();
    }

    private void OnCancelClick()
    {
        Hide();
    }

    private void Hide()
    {
        canvasGroup.DOFade(0f, fadeTime).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void OnDestroy()
    {
        confirmButton.onClick.RemoveListener(OnConfirmClick);
        cancelButton.onClick.RemoveListener(OnCancelClick);
    }
}
