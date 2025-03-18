using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class AIPlayer
{
	public abstract UniTask<Position> GetMoveAsync(BoardContext board);

	public static AIPlayer GetAIPlayer(int level)
	{
		switch (level)
		{
			case 0: return new AIPlayer_Random();
			case 1: return new AIPlayer_AttackOrBlock();
			default: return new AIPlayer_MiniMax();
		}
	}
}


/// <summary>
/// 一级AI，随机落子
/// </summary>
public class AIPlayer_Random : AIPlayer
{
	public override async UniTask<Position> GetMoveAsync(BoardContext board)
	{
		await UniTask.WaitForSeconds(1f);
		List<Position> emptyPositions = board.GetEmptyCells().ToList();
		int i = Random.Range(0, emptyPositions.Count);
		return emptyPositions[i];
	}
}


/// <summary>
/// 二级AI，能赢就进攻，不能就防守
/// </summary>
public class AIPlayer_AttackOrBlock : AIPlayer
{
	public override async UniTask<Position> GetMoveAsync(BoardContext board)
	{
		var delayTime = Random.Range(1f, 1.5f);
		await UniTask.WaitForSeconds(delayTime);
		// 可以进攻
		var finalPositions = board.FindFinalCells(board.CurrentPlayer).ToList();
		if (finalPositions.Count > 0)
		{
			int i = Random.Range(0, finalPositions.Count);
			return finalPositions[i];
		}
		// 需要防守
		var dangerPositions = board.FindFinalCells(board.CurrentPlayer.Opponent).ToList();
		if (dangerPositions.Count > 0)
		{
			int i = Random.Range(0, dangerPositions.Count);
			return dangerPositions[i];
		}
		// 随机落子
		List<Position> emptyPositions = board.GetEmptyCells().ToList();
		int j = Random.Range(0, emptyPositions.Count);
		return emptyPositions[j];
	}
}


/// <summary>
/// 三级AI，MiniMax算法
/// </summary>
public class AIPlayer_MiniMax : AIPlayer
{
	public override async UniTask<Position> GetMoveAsync(BoardContext board)
	{
		await UniTask.WaitForSeconds(2f);
		return await UniTask.RunOnThreadPool(() => GetMinimax(board, 0, true).Item2);
	}

	// 返回此回合棋手最应该下的位置
	private (int, Position) GetMinimax(BoardContext board, int depth, bool isAiTurn)
	{
		if (board.GameResult == GameResult.Win) return (+1, Position.Invalid);
		if (board.GameResult == GameResult.Lose) return (-1, Position.Invalid);
		if (board.GameResult == GameResult.Tie) return (0, Position.Invalid);
		if (isAiTurn)
		{
			// AI回合，选择下了之后玩家得最少分的位置
			var best = (int.MaxValue, Position.Invalid);
			foreach (var pos in board.GetEmptyCells())
			{
				board.PlaceChess(pos);
				int score = GetMinimax(board, depth + 1, false).Item1;
				board.RetractChess();
				if (score < best.Item1)
					best = (score, pos);
			}
			return best;
		}
		else
		{
			// 玩家回合，选择下了之后自己得最多分的位置
			var best = (int.MinValue, Position.Invalid);
			foreach (var pos in board.GetEmptyCells())
			{
				board.PlaceChess(pos);
				int score = GetMinimax(board, depth + 1, true).Item1;
				board.RetractChess();
				if (score > best.Item1)
					best = (score, pos);
			}
			return best;
		}
	}
}
