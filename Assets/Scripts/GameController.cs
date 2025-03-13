using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{

    BoardController Board;


    // Start is called before the first frame update
    void Start()
    {
        var context = new BoardContext(PlayerId.X, true);
		Board.Initialize(context);
        Board.OnPlayerClickCell = OnPlayerClickCell;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    void OnPlayerClickCell(Position position)
    {
        if (Board.Context.CurrentPlayer != Board.Context.SelfPlayer)
            return;

        if (Board.Context.GetCell(position).IsEmpty)
            return;

        Board.PlaceChess(position);
    }

}
