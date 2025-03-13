using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct PlayerId : IComparer<PlayerId>, IEquatable<PlayerId>
{
	public const byte Empty = 0;
	public const byte X = 1;
	public const byte O = 2;

	public byte Id;
	public PlayerId(byte id) => Id = id;
	public readonly bool IsEmpty => Id == 0;
	public readonly PlayerId Opposite => Id == 0 ? throw new Exception() : (Id == X ? O : X);

	public static implicit operator PlayerId(byte value) => new(value);
	public static bool operator ==(PlayerId x, PlayerId y) => x.Id == y.Id;
	public static bool operator !=(PlayerId x, PlayerId y) => x.Id != y.Id;
	public readonly int Compare(PlayerId x, PlayerId y) => x.Id.CompareTo(y.Id);
	public override int GetHashCode() =>  HashCode.Combine(Id);
	public override bool Equals(object obj) => obj is PlayerId id && Id == id.Id;
	public bool Equals(PlayerId other) => Id == other.Id;
}

public struct Position : IComparer<Position>, IEquatable<Position>
{
	public byte X;
	public byte Y;
	public readonly int Index => X + Y * 3; //Index(int gridSize) => X + Y * gridSize;
	public Position(byte x, byte y)
	{
		X = x;
		Y = y;
	}
	public static implicit operator Position((int x, int y) value) => new Position((byte)value.x, (byte)value.y);
	public static bool operator ==(Position a, Position b) => a.X == b.X && a.Y == b.Y;
	public static bool operator !=(Position a, Position b) => a.X != b.X || a.Y != b.Y;
	public readonly int Compare(Position a, Position b) => a.Y == b.Y ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
	public override int GetHashCode() => HashCode.Combine(X, Y);
	public override bool Equals(object obj) => obj is Position pos && X == pos.X && Y == pos.Y;
	public bool Equals(Position other) => X == other.X && Y == other.Y;
}



public class BoardContext
{
	private PlayerId[] _board;
	public PlayerId CurrentPlayer;
	public PlayerId SelfPlayer;
	public List<(PlayerId, Position)> History;
	public bool GameOver;

	public BoardContext(PlayerId self, bool selfFirst)
	{
		_board = new PlayerId[3 * 3];
		SelfPlayer = self;
		CurrentPlayer = selfFirst ? self : self.Opposite;
		GameOver = false;
	}

	public PlayerId GetCell(Position position)
	{
		return _board[position.Index];
	}

	public void PlaceChess(Position position)
	{
		if (_board[position.Index] != PlayerId.Empty)
			throw new InvalidOperationException("Already placed at this chess position");
		_board[position.Index] = CurrentPlayer;
		History.Add((CurrentPlayer, position));
		CurrentPlayer = CurrentPlayer == PlayerId.X ? PlayerId.O : PlayerId.X;
	}

	public void RetractChess()
	{

	}
}



public class BoardController : MonoBehaviour
{
	// 棋子放置的位置，先按行 123-456-789
	public Transform[] CellPositions;
	public GameObject XPrefab;
	public GameObject OPrefab;

	public BoardContext Context { get; private set; }
	public Action<Position> OnPlayerClickCell;

	private Dictionary<Position, GameObject> _chessObjects;


	void Start()
	{
		
	}

	public void Initialize(BoardContext context)
	{
		if (_chessObjects == null)
		{
			_chessObjects = new(21);
		}
		else if (_chessObjects.Count > 0)
		{
			foreach (var go in _chessObjects.Values)
				Destroy(go);
			_chessObjects.Clear();
		}

		Context = context;

		// 更新外观到当前棋盘状态
		for (int y = 0; y < 3; y++)
		{
			for (int x = 0; x < 3; x++)
			{
				Position position = (x, y);
				PlaceOneChess(position, context.GetCell(position));
			}
		}
	}

	private void PlaceOneChess(Position position, PlayerId player)
	{
		GameObject chessPrefab;
		if (player == PlayerId.X) chessPrefab = XPrefab;
		else if (player == PlayerId.O) chessPrefab = OPrefab;
		else throw new InvalidOperationException("Invalid player");

		var cellPosition = CellPositions[position.Index];
		var chessObject = Instantiate(chessPrefab, cellPosition.position, Quaternion.identity, cellPosition);
		_chessObjects.Add(position, chessObject);
	}

	// 当前玩家落子
	public void PlaceChess(Position position)
	{
		var player = Context.CurrentPlayer;
		Context.PlaceChess(position);
		PlaceOneChess(position, player);
	}


	public void Retract()
	{
		if (Context.History.Count == 0)
			return;

		var lastMove = Context.History[Context.History.Count - 1];
		var position = lastMove.Item2;
		var player = lastMove.Item1;
		Context.History.RemoveAt(Context.History.Count - 1);

		var chessObject = _chessObjects[position];
		Destroy(chessObject);
		_chessObjects.Remove(position);

		Context.CurrentPlayer = player;
	}

	public void Restart()
	{

	}



}
