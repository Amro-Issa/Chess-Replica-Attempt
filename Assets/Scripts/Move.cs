using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    Square originalSquare;
    Square destinationSquare;

    public Move(Square originalSquare, Square destinationSquare)
    {
        this.originalSquare = originalSquare;
        this.destinationSquare = destinationSquare;
    }
}

public class PawnMove : Move
{
    public PawnMove(Square originalSquare, Square destinationSquare) : base(originalSquare, destinationSquare)
    {
        
    }
}