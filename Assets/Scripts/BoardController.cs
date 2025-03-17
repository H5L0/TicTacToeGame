using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[Serializable]
public struct PlayerId : IComparer<PlayerId>, IEquatable<PlayerId>
{
	public static readonly PlayerId None = new(0);
	public static readonly PlayerId X = new(1);
	public static readonly PlayerId O = new(2);

	public byte Id;
	public PlayerId(byte id) => Id = id;
	public readonly bool IsNone => Id == 0;
	public readonly PlayerId Opponent => Id == 0 ? throw new Exception() : (Id == X ? O : X);

	public static implicit operator PlayerId(byte value) => new(value);
	public static bool operator ==(PlayerId x, PlayerId y) => x.Id == y.Id;
	public static bool operator !=(PlayerId x, PlayerId y) => x.Id != y.Id;

	public readonly int Compare(PlayerId x, PlayerId y) => x.Id.CompareTo(y.Id);
	public readonly bool Equals(PlayerId other) => Id == other.Id;
	public override readonly bool Equals(object obj) => obj is PlayerId id && Id == id.Id;
	public override readonly int GetHashCode() => HashCode.Combine(Id);
	public override readonly string ToString() => IsNone ? "None" : $"Player{Id}";
}

[Serializable]
public struct Position : IComparer<Position>, IEquatable<Position>
{
	public static readonly Position Invalid = new(0xFF, 0xFF);

	public byte X;
	public byte Y;
	public Position(byte x, byte y) { X = x; Y = y; }
	public readonly int Index => X + Y * 3; //Index(int gridSize) => X + Y * gridSize;
	public static Position FromIndex(int index) => new((byte)(index % 3), (byte)(index / 3));

	public static implicit operator Position((int x, int y) value) => new((byte)value.x, (byte)value.y);
	public static bool operator ==(Position a, Position b) => a.X == b.X && a.Y == b.Y;
	public static bool operator !=(Position a, Position b) => a.X != b.X || a.Y != b.Y;

	public readonly int Compare(Position a, Position b) => a.Y == b.Y ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
	public readonly bool Equals(Position other) => X == other.X && Y == other.Y;
	public override readonly bool Equals(object obj) => obj is Position pos && X == pos.X && Y == pos.Y;
	public override readonly int GetHashCode() => HashCode.Combine(X, Y);
	public override readonly string ToString() => $"({X}, {Y})";
}


public enum BoardLineType
{
	None, Row, Col, MainDia, AntiDia
}

public readonly struct BoardLine
{
	public readonly BoardLineType Type;
	public readonly int Index;
	public BoardLine(BoardLineType type, int index) { Type = type; Index = index; }
	public static BoardLine MainDia => new(BoardLineType.MainDia, 0);
	public static BoardLine AntiDia => new(BoardLineType.AntiDia, 0);
}

public enum GameResult : byte
{
	None, Win, Tie, Lose
}

[Serializable]
public class BoardContext : ISerializationCallbackReceiver
{
	public const int GridSize = 3;
	private PlayerId[] _board;
	public PlayerId FirstPlayer;
	public PlayerId SelfPlayer;
	public GameResult GameResult { get; private set; }
	public BoardLine WinnerLine { get; private set; }
	public List<Position> History;

	public PlayerId CurrentPlayer => (History.Count & 1) == 0 ? FirstPlayer : FirstPlayer.Opponent;
	public bool IsSelfRound => CurrentPlayer == SelfPlayer;
	public bool GameOver => GameResult != GameResult.None;
	public PlayerId Winner => GameResult switch
	{
		GameResult.Win => SelfPlayer,
		GameResult.Lose => SelfPlayer.Opponent,
		_ => PlayerId.None,
	};

	public BoardContext(PlayerId self, bool selfFirst)
	{
		_board = new PlayerId[GridSize * GridSize];
		FirstPlayer = selfFirst ? self : self.Opponent;
		SelfPlayer = self;
		GameResult = GameResult.None;
		WinnerLine = default;
		History = new List<Position>();
	}

	public BoardContext(BoardContext source)
	{
		_board = (PlayerId[])source._board.Clone();
		FirstPlayer = source.FirstPlayer;
		SelfPlayer = source.SelfPlayer;
		GameResult = source.GameResult;
		WinnerLine = source.WinnerLine;
		History = new(source.History);
	}

	public void OnBeforeSerialize()
	{
	}

	public void OnAfterDeserialize()
	{
		_board = new PlayerId[GridSize * GridSize];
		for (int i = 0; i < History.Count; i++)
		{
			var p = (i & 1) == 0 ? FirstPlayer : FirstPlayer.Opponent;
			var pos = History[i];
			_board[pos.Index] = p;
		}
	}

	public BoardContext Clone()
	{
		return new BoardContext(this);
	}

	public PlayerId GetCell(Position position)
	{
		return _board[position.Index];
	}

	// 下一步棋
	public void PlaceChess(Position position)
	{
		if (GameOver)
			throw new InvalidOperationException("The game is over");
		if (_board[position.Index] != PlayerId.None)
			throw new InvalidOperationException("Already placed at this chess position");
		_board[position.Index] = CurrentPlayer;
		History.Add(position);
		CheckGameOver();
	}

	// 悔一步棋
	public Position RetractChess()
	{
		if (History.Count == 0)
			throw new InvalidOperationException("No history");
		var position = History[^1];
		History.RemoveAt(History.Count - 1);
		_board[position.Index] = PlayerId.None;
		GameResult = GameResult.None;
		WinnerLine = default;
		return position;
	}

	private void CheckGameOver()
	{
		// 横、竖、对角线
		foreach (var line in GetAllLines())
		{
			if (IsLineSame(line, out var value) && !value.IsNone)
			{
				GameResult = value == SelfPlayer ? GameResult.Win : GameResult.Lose;
				WinnerLine = line;
				return;
			}
		}

		// 平局
		if (History.Count == GridSize * GridSize)
		{
			GameResult = GameResult.Tie;
		}
	}


	public bool IsLineSame(BoardLine line, out PlayerId value)
	{
		value = PlayerId.None;
		bool hasValue = false;
		var positions = GetLineCells(line);
		foreach (var pos in positions)
		{
			if (!hasValue)
			{
				value = GetCell(pos);
				hasValue = true;
			}
			else if (GetCell(pos) != value)
			{
				value = PlayerId.None;
				return false;
			}
		}
		return true;
	}

	// 40B GC
	public IEnumerable<BoardLine> GetAllLines()
	{
		for (int i = 0; i < GridSize; i++)
		{
			yield return new BoardLine(BoardLineType.Row, i);
			yield return new BoardLine(BoardLineType.Col, i);
		}
		yield return new BoardLine(BoardLineType.MainDia, 0);
		yield return new BoardLine(BoardLineType.AntiDia, 0);
	}

	public IEnumerable<Position> GetLineCells(BoardLine line)
	{
		if (line.Type == BoardLineType.Row)
		{
			for (int k = 0; k < GridSize; k++)
				yield return (k, line.Index);
		}
		else if (line.Type == BoardLineType.Col)
		{
			for (int k = 0; k < GridSize; k++)
				yield return (line.Index, k);
		}
		else if (line.Type == BoardLineType.MainDia)
		{
			for (int k = 0; k < GridSize; k++)
				yield return (k, k);
		}
		else if (line.Type == BoardLineType.AntiDia)
		{
			for (int k = 0; k < GridSize; k++)
				yield return (GridSize - 1 - k, k);
		}
	}

	public IEnumerable<Position> GetEmptyCells()
	{
		const int n = GridSize * GridSize;
		for (int i = 0; i != n; i++)
			if (_board[i] == PlayerId.None)
				yield return Position.FromIndex(i);
	}

	public IEnumerable<Position> FindFinalCells(PlayerId player)
	{
		foreach (var line in GetAllLines())
			if (FindFinalCell(line, player, out var pos))
				yield return pos;
	}

	private bool FindFinalCell(BoardLine line, PlayerId player, out Position value)
	{
		value = default;
		bool hasEmpty = false;
		var positions = GetLineCells(line);
		foreach (var pos in positions)
		{
			var cell = GetCell(pos);
			if (cell == player)
				continue;
			// 有对方的棋子，不可能成为终局
			if (cell != PlayerId.None)
				return false;
			// 不止一个空位，不可能成为终局
			if (hasEmpty)
				return false;
			hasEmpty = true;
			value = pos;
		}
		return hasEmpty;
	}
}



public class BoardController : MonoBehaviour
{
	// 棋格（先行后列：123-456-789）
	public Transform[] CellPositions;
	public GameObject XPrefab;
	public GameObject OPrefab;
	public GameObject ConnectLinePrefab;
	public GameObject MaskObject;

	public BoardContext Context { get; private set; }
	public Action<Position> OnPlayerClickCell;

	private Dictionary<Position, GameObject> _chessObjects;
	private GameObject _connectLineObject;


	void Start()
	{
		HideMask();
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

		// 初始化已有的棋子
		for (int y = 0; y < 3; y++)
		{
			for (int x = 0; x < 3; x++)
			{
				Position position = (x, y);
				CellPositions[position.Index].GetComponent<UIEventReceiver>().OnClick = OnCellClick;
				var player = context.GetCell(position);
				if (player.IsNone)
					continue;
				CreateOneChess(position, player);
			}
		}

		HideMask();
		HideConnectLine();
	}

	private void CreateOneChess(Position position, PlayerId player)
	{
		GameObject chessPrefab;
		if (player == PlayerId.X) chessPrefab = XPrefab;
		else if (player == PlayerId.O) chessPrefab = OPrefab;
		else throw new InvalidOperationException("Invalid player");

		var cellPosition = CellPositions[position.Index];
		var chessObject = Instantiate(chessPrefab,
			cellPosition.position, Quaternion.identity, cellPosition);
		_chessObjects.Add(position, chessObject);
	}

	public void PlaceChess(Position position)
	{
		var player = Context.CurrentPlayer;
		Context.PlaceChess(position);
		CreateOneChess(position, player);
	}


	public void RetractChess()
	{
		var position = Context.RetractChess();
		Destroy(_chessObjects[position]);
		_chessObjects.Remove(position);
		if (_connectLineObject != null)
		{
			Destroy(_connectLineObject);
			_connectLineObject = null;
		}
	}


	private void OnCellClick(UIEventReceiver receiver)
	{
		var index = Array.IndexOf(CellPositions, receiver.transform);
		if (index == -1)
			return;
		var position = Position.FromIndex(index);
		OnPlayerClickCell?.Invoke(position);
	}


	public void ShowConnectLine(BoardLine line)
	{
		(Position pos, float angle) = line.Type switch
		{
			BoardLineType.Row => ((1, line.Index), 0),
			BoardLineType.Col => ((line.Index, 1), -90),
			BoardLineType.MainDia => ((1, 1), -45),
			BoardLineType.AntiDia => ((1, 1), -135),
			_ => throw new InvalidOperationException("Invalid line type")
		};

		_connectLineObject = Instantiate(ConnectLinePrefab,
			CellPositions[pos.Index].position, Quaternion.Euler(0, 0, angle), transform);

		var image = _connectLineObject.GetComponent<Image>();
		image.fillAmount = 0;
		var tw = image.DOFillAmount(1, 0.5f);
	}

	public void HideConnectLine()
	{
		if (_connectLineObject != null)
		{
			Destroy(_connectLineObject);
			_connectLineObject = null;
		}
	}

	public void ShowMask(Position position)
	{
		DOTween.Kill(MaskObject);
		MaskObject.SetActive(true);
		MaskObject.transform.position = CellPositions[position.Index].position;
		var mask = MaskObject.GetComponent<Image>();
		mask.color = Color.clear;
		mask.DOFade(0.5f, 0.5f);
	}

	public void HideMask()
	{
		var mask = MaskObject.GetComponent<Image>();
		mask.DOFade(0f, 0.333f).OnComplete(() => MaskObject.SetActive(false));
	}
}
