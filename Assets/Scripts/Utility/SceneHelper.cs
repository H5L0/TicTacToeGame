using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SceneHelper : MonoBehaviour
{
	static SceneHelper _instance;
	public static SceneHelper Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = CreateInstance();
			}
			return _instance;
		}
	}

	public static SceneHelper CreateInstance()
	{
		var canvasPrefab = Resources.Load<GameObject>("Prefabs/CoverCanvas");
		var canvasObj = Instantiate(canvasPrefab);
		var helper = canvasObj.GetComponent<SceneHelper>();
		helper.fadeMask.enabled = false;
		GameObject.DontDestroyOnLoad(canvasObj);
		return helper; 
	}

	public static void FadeLoadScene(string sceneName)
	{
		SceneManager.LoadScene(sceneName);
		//Instance.DoFadeLoadScene(sceneName);
	}

	public Image fadeMask;
	public Color color;
	public float fadeTime = 0.7f;
	public void DoFadeLoadScene(string sceneName)
	{
		fadeMask.enabled = true;
		fadeMask.color = new(color.r, color.g, color.b, 0f);
		fadeMask.DOFade(1, fadeTime).SetUpdate(true).OnComplete(() =>
		{
			SceneManager.LoadScene(sceneName);
			fadeMask.DOFade(0, fadeTime).SetUpdate(true).OnComplete(() =>
			{
				fadeMask.enabled = false;
			});
		});
	}

}

