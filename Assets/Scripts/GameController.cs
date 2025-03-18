using System;
using System.Collections;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

public class GameController : MonoBehaviour
{
	public UIController UI;
	public BoardController Board;

	public enum GameState
	{
		None, Ready, Playing, Pause, GameOver
	}

	GameState _gameState = GameState.None;
	LevelState _levelState;
	BoardContext Context => _levelState.BoardContext;
	BattleData BattleData => _levelState.BattleData;
	AIPlayer _ai;

	// Start is called before the first frame update
	void Start()
	{
		Board.SetScore(0, 0, 0);
		Board.OnPlayerClickCell = OnPlayerClickCell;
		UI.SetCallbacks(OnPlayerClickRetract, OnPlayerClickRestart);
		SoundHelper.PlayBgm(Sound.BGM_Main);

		if (ScenePassingData.EntryMode == GameEntryMode.GuideOnly
			|| ScenePassingData.EntryMode == GameEntryMode.StartFromGuide)
		{
			GuideStart();
		}
		else if (ScenePassingData.EntryMode == GameEntryMode.StartFromFirstLevel)
		{
			var levelState = new LevelState();
			StartGame(levelState);
		}
		else if (ScenePassingData.EntryMode == GameEntryMode.LoadFromLevel)
		{
			StartGame(ScenePassingData.LoadLevelState);
			ScenePassingData.LoadLevelState = null;
		}
		else if (ScenePassingData.EntryMode == GameEntryMode.JumpToLevel)
		{
			int index = GameSettings.Instance.Levels.FindIndex(e => e.Id == ScenePassingData.JumpToLevelId);
			var levelState = new LevelState(index);
			StartGame(levelState);
		}
	}

	public void StartGuideGame(BoardContext context)
	{
		_gameState = GameState.Playing;
		_levelState.BoardContext = context;
		_ai = null;
		UI.SetLevelInfoText("教程");
		Board.Initialize(Context);
		Board.SetPlayerIcon("你", "对手");
		OnRoundUpdate();
	}

	public void StartGame(LevelState state)
	{
		StorageManager.Instance.TrySaveLevelState(state);
		_gameState = GameState.None;
		_levelState = state;
		_ai = AIPlayer.GetAIPlayer(state.GetLevelInfo().AiLevel);
		UI.SetGuideMode(false, OnPlayerClickExit);
		StartCoroutine(IE_GameStart());
	}

	public void RestartGame()
	{
		var lastContext = _levelState.BoardContext;
		_levelState.BoardContext = new BoardContext(lastContext.SelfPlayer, lastContext.IsSelfFirst);
		StartGame(_levelState);
	}

	public void NextGame()
	{
		StartGame(_levelState.GetNextGame());
	}

	IEnumerator IE_GameStart()
	{
		_gameState = GameState.Ready;
		UI.SetStatusText("游戏准备中...");
		SoundHelper.PlaySfx(Sound.GameStart);

		yield return null;
		Board.Initialize(Context);
		Board.SetPlayerIcon("你", _levelState.GetLevelInfo().OpponentName);
		Board.SetScore(BattleData.WinCount, BattleData.TieCount, BattleData.LoseCount);

		var levelInfo = _levelState.GetLevelInfo();
		string levelText = levelInfo.GoalText;
		if (levelInfo.PassCount > 0)
			levelText += $"({_levelState.PassedCount}/{levelInfo.PassCount})";
		UI.SetLevelInfoText(levelText);

		UI.ShowMainMessage("游戏开始！", 1);
		yield return new WaitForSeconds(1f);

		_gameState = GameState.Playing;
		OnRoundUpdate();
	}

	async UniTask OnGameOver()
	{
		_gameState = GameState.GameOver;

		// 更新得分
		BattleData.AddBattleData(Context.GameResult);
		Board.SetScore(BattleData.WinCount, BattleData.TieCount, BattleData.LoseCount);

		// 保存关卡进度
		bool passed = false;
		if (!IsInGuide())
		{
			var levelSetting = _levelState.GetLevelInfo();
			if (levelSetting.PassCount <= 0
				||(levelSetting.Condition == LevelInfo.PassCondition.Win && Context.GameResult == GameResult.Win)
				|| (levelSetting.Condition == LevelInfo.PassCondition.NotLose && Context.GameResult != GameResult.Lose))
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

		await UniTask.WaitForSeconds(0.5f);
		UI.SetStatusText("游戏结束");

		// 画线
		if (Context.GameResult != GameResult.Tie)
		{
			SoundHelper.PlaySfx(Sound.DrawLine);
			await Board.ShowConnectLine(Context.WinnerLine);
		}
		await UniTask.WaitForSeconds(0.25f);

		if (IsInGuide())
		{
			UI.ShowGameOverPanel(Context.GameResult, null,
				onContinue: GuideWaitingGameOverCallback,
				guideMessage: GuideWaitingGameOverMessage);
		}
		else
		{
			var levelSetting = _levelState.GetLevelInfo();
			// 统计对局并解锁勋章
			var unlockFeatures = StorageManager.Instance.TryUnlockFeature(levelSetting.AiLevel, Context.GameResult);
			// 显示结束界面
			Action onRetry = passed ? null : RestartGame;
			Action onContinue = passed ? NextGame : null;
			if (Context.Winner == Context.SelfPlayer)
			{
				SoundHelper.PlaySfx(Sound.Win);
				UI.ShowGameOverPanel(Context.GameResult, onRetry, onContinue, unlockFeatures);
			}
			else if (Context.Winner == PlayerId.None)
			{
				SoundHelper.PlaySfx(Sound.Tie);
				UI.ShowGameOverPanel(Context.GameResult, onRetry, onContinue, unlockFeatures);
			}
			else
			{
				SoundHelper.PlaySfx(Sound.Lose);
				UI.ShowGameOverPanel(Context.GameResult, onRetry, onContinue, unlockFeatures);
			}

		}
	}

	void OnRoundUpdate()
	{
		UpdateRetractButton();
		Board.UpdatePlayerRoundIndicator();
		if (Context.GameOver)
		{
			OnGameOver().Forget();
		}
		else
		{
			if (Context.IsSelfRound)
			{
				if (!IsInGuide())
				{
					if (Context.History.Count == 0)
						UI.ShowSubMessage("你先下");
					//else
					//	UI.ShowSubMessage("轮到你了");
				}
				UI.SetStatusText("你的回合");
			}
			else
			{
				if (!IsInGuide() && Context.History.Count == 0)
					UI.ShowSubMessage("对手先下");
				UI.SetStatusText("等待对手下棋");
				if (IsInGuide()) // 教程里对手的棋已经手动指定了
					return;
				DoOpponentRoundAsync().Forget();
			}
		}
	}

	void OnPlayerClickCell(Position position)
	{
		if (_gameState != GameState.Playing)
			return;

		if (Context.CurrentPlayer != Context.SelfPlayer)
			return;

		if (!Context.GetCell(position).IsNone)
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
		if (Context.GameOver)
			return;

		if (Context.IsSelfRound)
			throw new Exception("Not Ai Round");

		var position = await _ai.GetMoveAsync(Context);
		PlaceChess(position);
	}

	public void PlaceChess(Position position)
	{
		Board.PlaceChess(position);
		SoundHelper.PlaySfx(Sound.DoMove);
		OnRoundUpdate();
	}

	bool CanRetract()
	{
		if (_gameState != GameState.Playing)
			return false;
		// 写死只能撤销一次
		if (Context.RetractCount >= 1)
			return false;
		if (Context.IsSelfRound)
			return Context.History.Count >= 2;
		else
			return Context.History.Count >= 1;
	}

	void UpdateRetractButton()
	{
		UI.SetRetractable(CanRetract());
	}

	async void OnPlayerClickRetract()
	{
		if (!CanRetract())
			return;
		_gameState = GameState.Pause;
		if (Context.IsSelfRound)
		{
			SoundHelper.PlaySfx(Sound.Retract);
			Board.RetractChess(); // 撤销对手一回合
			await UniTask.Delay(500);
			SoundHelper.PlaySfx(Sound.Retract);
			Board.RetractChess(); // 撤销自己上回合
		}
		else
		{
			SoundHelper.PlaySfx(Sound.Retract);
			Board.RetractChess(); // 撤销自己上回合
		}
		_gameState = GameState.Playing;
		OnRoundUpdate();
		GuideRetractCallback?.Invoke();
	}

	void OnPlayerClickRestart()
	{
		RestartGame();
	}

	void OnPlayerClickExit()
	{
		if (!IsInGuide() && (_gameState == GameState.Playing || _gameState == GameState.Pause))
		{
			StorageManager.Instance.TrySaveLevelState(_levelState);
		}
		SceneHelper.FadeLoadScene("MenuScene");
	}


	#region Guide
	private GuideLevelSettings _currentGuideLevelSetting;
	private CancellationTokenSource _guideCancelationSource;

	private bool IsInGuide()
	{
		return _currentGuideLevelSetting != null;
	}

	async void GuideStart()
	{
		_levelState = new();
		_currentGuideLevelSetting = GameSettings.Instance.GuideLevelSetting;
		_guideCancelationSource = new();
		UI.SetGuideMode(true, OnSkipGuide);
		for (int i = 0; i < _currentGuideLevelSetting.Steps.Count; i++)
		{
			var step = _currentGuideLevelSetting.Steps[i].GetObject();
			try { await step.DoAsync(this, _guideCancelationSource.Token); }
			catch { break; }
		}
		await UniTask.WaitForSeconds(0.5f);
		GuideEnd();
	}

	void GuideEnd()
	{
		// 清理回调
		UnsetGuideSpecifyPosition();
		GuideWaitingGameOverCallback = null;
		GuideWaitingGameOverMessage = null;
		GuideRetractCallback = null;
		UI.HideGuideMessage();

		_currentGuideLevelSetting = null;
		StorageManager.Instance.HaveReadGuidance = true;
		if (ScenePassingData.EntryMode == GameEntryMode.GuideOnly)
		{
			SceneHelper.FadeLoadScene("MenuScene");
		}
		else
		{
			StartGame(new LevelState());
		}
	}

	void OnSkipGuide()
	{
		if (_currentGuideLevelSetting != null)
		{
			_guideCancelationSource.Cancel();
			//GuideEnd();
		}
	}

	private Position _guideSpecifyPosition = Position.Invalid;
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

	public string GuideWaitingGameOverMessage { get; set; }
	public Action GuideWaitingGameOverCallback { get; set; }
	public Action GuideRetractCallback { get; set; }

	#endregion
}
