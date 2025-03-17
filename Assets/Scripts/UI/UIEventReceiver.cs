using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIEventReceiver : MonoBehaviour, IPointerClickHandler
{

	public Action<UIEventReceiver> OnClick;

	public void OnPointerClick(PointerEventData eventData)
	{
		OnClick?.Invoke(this);
	}
}
