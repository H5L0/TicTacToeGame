using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;


[Serializable]
public class GuideStep
{
	public virtual UniTask DoAsync(GameController game) { return UniTask.CompletedTask; }
}

public class GuideStep_StartNewGame : GuideStep
{
	public BoardContext Board;
	public override UniTask DoAsync(GameController game)
	{
		game.StartGuideGame(Board.Clone());
		return UniTask.CompletedTask;
	}
}

public class GuideStep_WaitingGameOver : GuideStep
{
	[Multiline]
	public string Text;
	public override UniTask DoAsync(GameController game)
	{
		return game.WaitingGameOverAsync(Text);
	}
}

public class GuideStep_ShowMessage : GuideStep
{
	[Multiline]
	public string Text;
	public override async UniTask DoAsync(GameController game)
	{
		var tcs = new UniTaskCompletionSource();
		game.UI.ShowGuideMessageToConfirm(Text, () => tcs.TrySetResult());
		await tcs.Task;
		game.UI.HideGuideMessage();
	}
}

public class GuideStep_AiPlaceChess : GuideStep
{
	public int x;
	public int y;
	public override async UniTask DoAsync(GameController game)
	{
		await UniTask.Delay(900);
		game.PlaceChess((x, y));
		await UniTask.Delay(100);
	}
}

public class GuideStep_WaitingPlayerPlaceChess : GuideStep
{
	[Multiline]
	public string Text;
	public int x;
	public int y;

	public override async UniTask DoAsync(GameController game)
	{
		if (!string.IsNullOrEmpty(Text))
			game.UI.ShowGuideMessage(Text);
		var tcs = new UniTaskCompletionSource();
		game.SetGuideSpecifyPosition((x, y), () => tcs.TrySetResult());
		await tcs.Task;
		if (!string.IsNullOrEmpty(Text))
			game.UI.HideGuideMessage();
	}
}

public class GuideStep_WaitingPlayerRetract : GuideStep
{
	[Multiline]
	public string Text;
	public override async UniTask DoAsync(GameController game)
	{
		game.UI.ShowGuideMessage(Text);
		var tcs = new UniTaskCompletionSource();
		game.GuideRetractCallback = () => tcs.TrySetResult();
		await tcs.Task;
		game.GuideRetractCallback = null;
		game.UI.HideGuideMessage();
	}
}

public class GuideController
{

}


