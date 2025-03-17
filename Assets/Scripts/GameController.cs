using System;
using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;


public class LevelState
{
	public int LevelIndex;
	public int PassedCount;

	// 下一小关
	public LevelState GetNextGame()
	{
		var levelInfo = GameSettings.Instance.Levels[LevelIndex];
		// Count < 0: 无尽模式
		if (levelInfo.PassCount <= 0 || PassedCount + 1 < levelInfo.PassCount)
		{
			return new LevelState() { LevelIndex = LevelIndex, PassedCount = PassedCount + 1 };
		}
		else
		{
			return GetNextLevel();
		}
	}

	// 下一大关
	public LevelState GetNextLevel()
	{
		if (LevelIndex + 1 < GameSettings.Instance.Levels.Count)
		{
			return new LevelState() { LevelIndex = LevelIndex + 1 };
		}
		else
		{
			return new LevelState() { LevelIndex = -1 };
		}
	}

	public LevelInfo GetLevelInfo()
	{
		return GameSettings.Instance.Levels[LevelIndex];
	}
}

public class GameController : MonoBehaviour
{
	public UIController UI;
	public BoardController Board;

	private BoardContext _context;
	private AIPlayer _ai = new AIPlayer_AttackOrBlock();

	public enum GameState
	{
		None,
		Ready,
		Playing,
		Pause,
		GameOver
	}

	GameState _gameState = GameState.None;
	LevelState _levelState = new LevelState();

	// Start is called before the first frame update
	void Start()
	{
		Board.OnPlayerClickCell = OnPlayerClickCell;
		UI.RetractButton.onClick.AddListener(OnPlayerClickRetract);

		if (ScenePassingData.EnterGuide)
		{
			GuideStart();
		}
		else
		{
			StartGame(new LevelState());
		}
	}

	public void StartGame(LevelState state)
	{
		_levelState = state;
		_ai = AIPlayer.GetAIPlayer(state.GetLevelInfo().AiLevel);
		_context = new BoardContext(PlayerId.X, true);
		Board.Initialize(_context);
		StartCoroutine(IE_GameStart());
	}

	public void RestartGame()
	{
		StartGame(_levelState);
	}

	public void NextGame()
	{
		StartGame(_levelState.GetNextGame());
	}

	public void StartGuideGame(BoardContext context)
	{
		_gameState = GameState.Playing;
		_levelState = null;
		_context = context;
		UI.SetLevelInfoText("教程");
		Board.Initialize(_context);
		OnRoundUpdate();
	}

	IEnumerator IE_GameStart()
	{
		_gameState = GameState.Ready;
		UI.SetStatusText("游戏准备中...");

		var levelInfo = _levelState.GetLevelInfo();
		string levelText = levelInfo.GoalText;
		if (levelInfo.PassCount > 0)
			levelText += $" ({_levelState.PassedCount}/{levelInfo.PassCount})";
		UI.SetLevelInfoText(levelText);

		UI.ShowMainMessage("游戏开始！", 1);
		yield return new WaitForSeconds(1f);

		UI.ShowSubMessage("你先下");  // TODO: 富文本
		yield return new WaitForSeconds(0.5f);

		_gameState = GameState.Playing;
		OnRoundUpdate();
	}

	IEnumerator IE_GameOver()
	{
		_gameState = GameState.GameOver;

		// 保存关卡进度
		bool passed = false;
		if (!IsInGuide())
		{
			var levelSetting = _levelState.GetLevelInfo();
			if ((levelSetting.Condition == LevelInfo.PassCondition.Win && _context.GameResult == GameResult.Win)
				|| (levelSetting.Condition == LevelInfo.PassCondition.NotLose && _context.GameResult != GameResult.Lose))
			{
				passed = true;
				var nextGame = _levelState.GetNextGame();
				StorageManager.Instance.TrySaveLevelState(nextGame);
			}
			if (levelSetting.PassCount > 0 && _levelState.PassedCount + 1 == levelSetting.PassCount)
			{
				UI.SetLevelInfoText(levelSetting.GoalText + " (完成)");
			}
		}

		// TODO: 播放声音
		yield return new WaitForSeconds(0.5f);
		UI.SetStatusText("游戏结束");

		if (_context.GameResult != GameResult.Tie)
		{
			Board.ShowConnectLine(_context.WinnerLine);
			yield return new WaitForSeconds(1f);
		}
		else
		{
			yield return new WaitForSeconds(0.5f);
		}

		if (IsInGuide())
		{
			UI.ShowGameOverPanel(_context.GameResult, null,
				onContinue: InvokeGuideGameOver,
				guideMessage: _guideWaitingGameOverText);
		}
		else
		{
			var levelSetting = _levelState.GetLevelInfo();
			// 统计对局并解锁勋章
			var unlockFeatures = StorageManager.Instance.TryUnlockFeature(levelSetting.AiLevel, _context.GameResult);
			// 显示结束界面
			Action onContinue = passed ? NextGame : null;
			if (_context.Winner == _context.SelfPlayer)
			{
				UI.ShowGameOverPanel(_context.GameResult, RestartGame, onContinue, unlockFeatures);
			}
			else if (_context.Winner == PlayerId.None)
			{
				UI.ShowGameOverPanel(_context.GameResult, RestartGame, onContinue, unlockFeatures);
			}
			else
			{
				UI.ShowGameOverPanel(_context.GameResult, RestartGame, onContinue, unlockFeatures);
			}
		}
	}


	public void PlaceChess(Position position)
	{
		Board.PlaceChess(position);
		OnRoundUpdate();
	}

	void OnRoundUpdate()
	{
		if (_context.GameOver)
		{
			StartCoroutine(IE_GameOver());
		}
		else
		{
			if (_context.IsSelfRound)
			{
				UI.SetStatusText("你的回合");

			}
			else
			{
				UI.SetStatusText("等待对手下棋");
				// 教程里对手的棋是手动指定的
				if (IsInGuide())
					return;
					
				_ = DoOpponentRoundAsync();
			}
		}
	}

	void OnPlayerClickCell(Position position)
	{
		if (_gameState != GameState.Playing)
			return;

		if (_context.CurrentPlayer != _context.SelfPlayer)
			return;

		if (!_context.GetCell(position).IsNone)
			return;

		if (IsInGuide())
		{
			if (_guideSpecifyPosition != Position.Invalid && position == _guideSpecifyPosition)
			{
				PlaceChess(position);
				InvokeGuideSpecifyPosition();
			}
		}
		else
		{
			PlaceChess(position);
		}
	}

	async UniTask DoOpponentRoundAsync()
	{
		if (_context.GameOver)
			return;

		if (_context.IsSelfRound)
			throw new Exception("Not Ai Round");

		var position = await _ai.DoStep(_context);
		Board.PlaceChess(position);
		await UniTask.WaitForSeconds(0.2f);
		OnRoundUpdate();
	}

	bool CanRetract()
	{
		if (_gameState != GameState.Playing)
			return false;
		if (_context.IsSelfRound)
			return _context.History.Count >= 2;
		else
			return _context.History.Count >= 1;
	}

	async void OnPlayerClickRetract()
	{
		if (!CanRetract())
			return;
		_gameState = GameState.Pause;
		if (_context.IsSelfRound)
		{
			Board.RetractChess(); // 对手一回合
			await UniTask.Delay(500);
			Board.RetractChess(); // 自己上回合
		}
		else
		{
			Board.RetractChess(); // 自己上回合
		}
		_gameState = GameState.Playing;
		OnRoundUpdate();
		GuideRetractCallback?.Invoke();
	}



	private GuideLevelSettings _currentGuideLevelSetting;

	private bool IsInGuide()
	{
		return _currentGuideLevelSetting != null;
	}

	async void GuideStart()
	{
		_currentGuideLevelSetting = GameSettings.Instance.GuideLevelSetting;
		for (int i = 0; i < _currentGuideLevelSetting.Steps.Count; i++)
		{
			var step = _currentGuideLevelSetting.Steps[i].GetObject();
			await step.DoAsync(this);
		}
		GuideEnd();
	}

	void GuideEnd()
	{
		_currentGuideLevelSetting = null;
		StorageManager.Instance.HaveReadGuidance = true;
		if (ScenePassingData.ExitAfterGuideFinished)
		{
			ScenePassingData.ExitAfterGuideFinished = false;
			SceneManager.LoadScene("MenuScene");
		}
		else
		{
			StartGame(new LevelState());
		}
	}

	private Position _guideSpecifyPosition;
	private Action _guideSpecifyPositionCallback;
	public void SetGuideSpecifyPosition(Position position, Action callback)
	{
		_guideSpecifyPosition = position;
		_guideSpecifyPositionCallback = callback;
		Board.ShowMask(position);
	}
	public void InvokeGuideSpecifyPosition()
	{
		var callback = _guideSpecifyPositionCallback;
		UnsetGuideSpecifyPosition();
		callback?.Invoke();
	}
	public void UnsetGuideSpecifyPosition()
	{
		_guideSpecifyPosition = Position.Invalid;
		_guideSpecifyPositionCallback = null;
		Board.HideMask();
	}

	private UniTaskCompletionSource _guideWaitingGameOverTCS;
	private string _guideWaitingGameOverText;
	public async UniTask WaitingGameOverAsync(string text)
	{
		_guideWaitingGameOverText = text;
		_guideWaitingGameOverTCS = new UniTaskCompletionSource();
		await _guideWaitingGameOverTCS.Task;
		_guideWaitingGameOverTCS = null;
		_guideWaitingGameOverText = null;
	}

	private void InvokeGuideGameOver()
	{
		Debug.Assert(_guideWaitingGameOverTCS != null);
		_guideWaitingGameOverTCS.TrySetResult();
	}

	public Action GuideRetractCallback { get; set; }

}
