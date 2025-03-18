using System.Collections.Generic;
using UnityEngine;

public class StorageManager
{
	public static StorageManager _instance;
	public static StorageManager Instance => _instance ??= new StorageManager();

	const string HaveReadGuidancePrefix = "HaveReadGuidance";
	const string LevelStatePrefix = "LevelState";
	const string BattleDataPrefix = "BattleData_";
	const string FeaturePrefix = "UnlockFeature_";

	public bool HaveReadGuidance
	{
		get => PlayerPrefs.GetInt(HaveReadGuidancePrefix, 0) != 0;
		set
		{
			PlayerPrefs.SetInt(HaveReadGuidancePrefix, value ? 1: 0);
			PlayerPrefs.Save();
		}
	}

	private Dictionary<int, BattleData> _battleDataDict = new();
	private Dictionary<string, bool> _featureDict;

	public StorageManager()
	{
		// 从PlayerPrefs加载已解锁的Feature
		_featureDict = new Dictionary<string, bool>();
		foreach (var feature in GameSettings.Instance.Features)
			_featureDict[feature.Key] = PlayerPrefs.GetInt($"{FeaturePrefix}{feature.Key}", 0) != 0;

		TryLoadBattleData(-1);
		TryLoadBattleData(0);
		TryLoadBattleData(1);
		TryLoadBattleData(2);
	}

	public void ClearAllData()
	{
		PlayerPrefs.DeleteAll();
		PlayerPrefs.Save();
		_instance = new StorageManager();
	}

	public void TrySaveLevelState(LevelState state)
	{
		var saveJson = JsonUtility.ToJson(state);
		PlayerPrefs.SetString(LevelStatePrefix, saveJson);
		PlayerPrefs.Save();
	}

	public bool TryGetLevelState(out LevelState state)
	{
		var json = PlayerPrefs.GetString(LevelStatePrefix, "");
		if (string.IsNullOrEmpty(json))
		{
			state = null;
			return false;
		}
		else
		{
			state = new LevelState();
			JsonUtility.FromJsonOverwrite(json, state);
			return true;
		}
	}

	public void DeleteLevelState()
	{
		PlayerPrefs.DeleteKey(LevelStatePrefix);
		PlayerPrefs.Save();
	}

	public bool IsFeatureUnlock(string key)
	{
		return _featureDict.TryGetValue(key, out bool unlocked) && unlocked;
	}

	public void SetFeatureUnlock(GameFeature feature)
	{
		_featureDict[feature.Key] = true;
		PlayerPrefs.SetInt($"{FeaturePrefix}{feature.Key}", 1);
		PlayerPrefs.Save();
	}

	private BattleData TryLoadBattleData(int targetId)
	{
		if (_battleDataDict.TryGetValue(targetId, out var data))
			return data;

		data = new BattleData();
		_battleDataDict[targetId] = data;

		var json = PlayerPrefs.GetString(BattleDataPrefix + targetId, "");
		if (!string.IsNullOrEmpty(json))
			JsonUtility.FromJsonOverwrite(json, data);
		return data;
	}

	private void TrySaveBattleData(int targetId, BattleData data)
	{
		var saveJson = JsonUtility.ToJson(data);
		PlayerPrefs.SetString(BattleDataPrefix + targetId, saveJson);
		PlayerPrefs.Save();
	}

	private void AddBattleData(int targetId, GameResult result)
	{
		var data = TryLoadBattleData(targetId);
		data.AddBattleData(result);
		TrySaveBattleData(targetId, data);
	}

	private int TryUnlockFeature(int targetId, GameResult result, List<GameFeature> unlockFeatures)
	{
		AddBattleData(targetId, result);

		int count = 0;
		if (!_battleDataDict.TryGetValue(targetId, out var data))
			return count;

		foreach (var feature in GameSettings.Instance.Features)
		{
			if (feature.TargetId != targetId)
				continue;
			if (IsFeatureUnlock(feature.Key))
				continue;
			bool shouldUnlock = feature.Type switch
			{
				GameFeature.UnlockType.WinCount => data.WinCount >= feature.Count,
				GameFeature.UnlockType.WinInRowCount => data.WinInRowCount >= feature.Count,
				GameFeature.UnlockType.LoseCount => data.LoseCount >= feature.Count,
				GameFeature.UnlockType.LoseInRowCount => data.LoseInRowCount >= feature.Count,
				GameFeature.UnlockType.TieCount => data.TieCount >= feature.Count,
				GameFeature.UnlockType.TieInRowCount => data.TieCountInRowCount >= feature.Count,
				GameFeature.UnlockType.NotLoseCount => data.TieCount + data.WinCount >= feature.Count,
				GameFeature.UnlockType.NotLoseInRowCount => data.NotLoseInRowCount >= feature.Count,
				_ => false,
			};
			if (!shouldUnlock)
				continue;
			SetFeatureUnlock(feature);
			unlockFeatures.Add(feature);
			count++;
		}
		return count;
	}

	public List<GameFeature> TryUnlockFeature(int targetId, GameResult result)
	{
		var unlockFeatures = new List<GameFeature>();
		TryUnlockFeature(-1, result, unlockFeatures);
		TryUnlockFeature(targetId, result, unlockFeatures);
		return unlockFeatures;
	}
}




public class BattleData
{
	public int WinCount;
	public int LoseCount;
	public int TieCount;
	public int WinInRowCount;
	public int LoseInRowCount;
	public int TieCountInRowCount;
	public int NotLoseInRowCount;

	public void AddBattleData(GameResult result)
	{
		switch (result)
		{
			case GameResult.Win:
				WinCount++;
				WinInRowCount++;
				NotLoseInRowCount++;
				LoseInRowCount = 0;
				TieCountInRowCount = 0;
				break;
			case GameResult.Lose:
				LoseCount++;
				LoseInRowCount++;
				WinInRowCount = 0;
				TieCountInRowCount = 0;
				NotLoseInRowCount = 0;
				break;
			case GameResult.Tie:
				TieCount++;
				TieCountInRowCount++;
				NotLoseInRowCount++;
				WinInRowCount = 0;
				LoseInRowCount = 0;
				break;
		}
	}
}

public class LevelState
{
	public int LevelIndex;
	public int PassedCount;
	public BattleData BattleData;
	public BoardContext BoardContext;

	public LevelState(int levelIndex = 0, int passedCount = 0)
	{
		LevelIndex = levelIndex;
		PassedCount = passedCount;
		BattleData = new();
		BoardContext = new(PlayerId.X, true);
	}

	// 下一小关
	public LevelState GetNextGame()
	{
		var levelInfo = GameSettings.Instance.Levels[LevelIndex];
		// Count < 0: 无尽模式
		if (levelInfo.PassCount <= 0 || PassedCount + 1 < levelInfo.PassCount)
		{
			// 换X/O (X总是第一个)
			var self = BoardContext.SelfPlayer.Opponent;
			return new LevelState()
			{
				LevelIndex = LevelIndex,
				PassedCount = PassedCount + 1,
				BattleData = BattleData,
				BoardContext = new BoardContext(self, self == PlayerId.X),
			};
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
			return new LevelState(LevelIndex + 1);
		}
		else
		{
			//暂时不使用这种情况
			return new LevelState(-1);
		}
	}

	public LevelInfo GetLevelInfo()
	{
		return GameSettings.Instance.Levels[LevelIndex];
	}
}
