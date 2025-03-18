using System;
using System.Collections;
using System.Collections.Generic;
using HL;
using UnityEngine;


[CreateAssetMenu(fileName = "GameSettings", menuName = "ScriptableObjects/GameSettings", order = 1)]
public class GameSettings : ScriptableObject
{
	public static GameSettings _instance;
	public static GameSettings Instance
	{
		get
		{
			if (_instance == null)
				_instance = Resources.Load<GameSettings>("GameSettings");
			return _instance;
		}
	}

	public GuideLevelSettings GuideLevelSetting;
	public List<LevelInfo> Levels;
	public List<GameFeature> Features;
}



[Serializable]
public class GuideLevelSettings
{
	public int PlayerSide;
	public List<SubClassSerializeHelperRef<GuideStep>> Steps;
}


[Serializable]
public class LevelInfo
{
	public enum PassCondition
	{
		Win, NotLose,
	}

	public int Id;       // 101/102/.../201/...
	public int AiLevel;  // 1/2/3
	public string OpponentName;
	public PassCondition Condition;
	public int PassCount;
	public string GoalText;
	public string[] UnlockFeatures;
}


[Serializable]
public class GameFeature
{
	public enum UnlockType
	{
		Manual,
		WinCount,
		WinInRowCount,
		LoseCount,
		LoseInRowCount,
		TieCount,
		TieInRowCount,
		NotLoseCount,
		NotLoseInRowCount,
	}

	public string Key;
	public string Name;
	public string Description;
	public UnlockType Type;
	public int Count;
	public int TargetId;
}
