using UnityEngine;
using TMPro;

public class UIObject : MonoBehaviour
{
	public void SetText(string text)
	{
		GetComponentInChildren<TMP_Text>().text = text;
	}

	public void DestroyThis()
	{
		Destroy(gameObject);
	}
}