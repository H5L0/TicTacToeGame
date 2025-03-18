using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


[Serializable]
public class GuideStep
{
	public virtual UniTask DoAsync(GameController game, CancellationToken cancellationToken)
	{
		return UniTask.CompletedTask;
	}

	protected static UniTask WaitAction(Action<Action> binder, CancellationToken cancellationToken)
	{
		var tcs = new UniTaskCompletionSource();
		binder.Invoke(() => tcs.TrySetResult());
		return tcs.Task.AttachExternalCancellation(cancellationToken);
	}
}

public class GuideStep_StartNewGame : GuideStep
{
	public BoardContext Board;
	public override UniTask DoAsync(GameController game, CancellationToken cancellationToken)
	{
		game.StartGuideGame(Board.Clone());
		return UniTask.CompletedTask;
	}
}

public class GuideStep_WaitingGameOver : GuideStep
{
	[Multiline]
	public string Text;
	public override async UniTask DoAsync(GameController game, CancellationToken cancellationToken)
	{
		var tcs = new UniTaskCompletionSource();
		game.GuideWaitingGameOverMessage = Text;
		await WaitAction(action => game.GuideWaitingGameOverCallback = action, cancellationToken);
		game.GuideWaitingGameOverCallback = null;
		game.GuideWaitingGameOverMessage = null;
		game.UI.HideGuideMessage();
	}
}

public class GuideStep_ShowMessage : GuideStep
{
	[Multiline]
	public string Text;
	public override async UniTask DoAsync(GameController game, CancellationToken cancellationToken)
	{
		await WaitAction(action => game.UI.ShowGuideMessageToConfirm(Text, action), cancellationToken);
		game.UI.HideGuideMessage();
	}
}

public class GuideStep_AiPlaceChess : GuideStep
{
	public int x;
	public int y;
	public override async UniTask DoAsync(GameController game, CancellationToken cancellationToken)
	{
		await UniTask.WaitForSeconds(1f, cancellationToken: cancellationToken);
		game.PlaceChess((x, y));
		await UniTask.WaitForSeconds(0.5f, cancellationToken: cancellationToken);
	}
}

public class GuideStep_WaitingPlayerPlaceChess : GuideStep
{
	[Multiline]
	public string Text;
	public int x;
	public int y;

	public override async UniTask DoAsync(GameController game, CancellationToken cancellationToken)
	{
		if (!string.IsNullOrEmpty(Text))
			game.UI.ShowGuideMessage(Text);
		await WaitAction(action => game.SetGuideSpecifyPosition((x, y), action), cancellationToken);
		if (!string.IsNullOrEmpty(Text))
			game.UI.HideGuideMessage();
	}
}

public class GuideStep_WaitingPlayerRetract : GuideStep
{
	[Multiline]
	public string Text;
	public override async UniTask DoAsync(GameController game, CancellationToken cancellationToken)
	{
		game.UI.ShowGuideMessage(Text, 2);
		game.UI.RestartButton.gameObject.SetActive(true);
		game.UI.RetractButton.gameObject.SetActive(true);
		await WaitAction(action => game.GuideRetractCallback = action, cancellationToken);
		game.GuideRetractCallback = null;
		game.UI.HideGuideMessage();
	}
}

public class GuideController
{

}


