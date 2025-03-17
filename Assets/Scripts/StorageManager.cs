using System.Collections.Generic;
using UnityEngine;

public class StorageManager
{
	public static StorageManager _instance;
	public static StorageManager Instance => _instance ??= new StorageManager();

	const string BattleDataPrefix = "BattleData_";
	const string FeaturePrefix = "UnlockFeature_";

	public bool HaveReadGuidance { get; set; }
	public int PassedLevelIndex = -1;

	class BattleData
	{
		public int WinCount;
		public int LoseCount;
		public int TieCount;
		public int WinInRowCount;
		public int LoseInRowCount;
		public int TieCountInRowCount;
		public int NotLoseInRowCount;
	}

	Dictionary<int, BattleData> _battleDataDict = new();


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

	public void TrySaveLevelState(LevelState state)
	{
		//PlayerPrefs.SetInt($"LevelPassCount_{levelIndex}", count);
		//PlayerPrefs.Save();
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
		// 更新战绩数据
		switch (result)
		{
			case GameResult.Win:
				data.WinCount++;
				data.WinInRowCount++;
				data.NotLoseInRowCount++;
				data.LoseInRowCount = 0;
				data.TieCountInRowCount = 0;
				break;
			case GameResult.Lose:
				data.LoseCount++;
				data.LoseInRowCount++;
				data.WinInRowCount = 0;
				data.TieCountInRowCount = 0;
				data.NotLoseInRowCount = 0;
				break;
			case GameResult.Tie:
				data.TieCount++;
				data.TieCountInRowCount++;
				data.NotLoseInRowCount++;
				data.WinInRowCount = 0;
				data.LoseInRowCount = 0;
				break;
		}
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
