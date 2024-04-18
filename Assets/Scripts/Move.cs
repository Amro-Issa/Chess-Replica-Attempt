using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    protected Square originalSquare;
    protected Square destinationSquare;

    public Move(Square originalSquare, Square destinationSquare)
    {
        this.originalSquare = originalSquare;
        this.destinationSquare = destinationSquare;
    }
}

public class PieceMove : Move
{
    protected Piece piece;

    public PieceMove(Square originalSquare, Square destinationSquare, Piece piece) : base(originalSquare, destinationSquare)
    {
        this.piece = piece;
    }

    public virtual void PlayMove()
    {
        destinationSquare.Unoccupy(true);
        destinationSquare.Occupy(piece);
        //TODO: physically move piece to new square
    }
}

public class PawnMove : PieceMove
{
    private Piece enPassantPiece = null;

    public PawnMove(Square originalSquare, Square destinationSquare, Pawn pawn) : base(originalSquare, destinationSquare, pawn)
    { 
    
    }

    public PawnMove(Square originalSquare, Square destinationSquare, Pawn pawn, Piece enPassantPiece) : base(originalSquare, destinationSquare, pawn)
    {
        this.enPassantPiece = enPassantPiece;
    }

    public override void PlayMove()
    {
        base.PlayMove();

        if (enPassantPiece != null)
        {
            enPassantPiece.square.Unoccupy();
        }
    }
}

public class KingMove : PieceMove
{
    private Rook castleRook = null;
    private Square rookCastleSquare = null;

    public KingMove(Square originalSquare, Square destinationSquare, King king) : base(originalSquare, destinationSquare, king)
    {

    }

    public KingMove(Square originalSquare, Square destinationSquare, King king, Rook castleRook, Square rookCastleSquare) : this(originalSquare, destinationSquare, king)
    {
        this.rookCastleSquare = rookCastleSquare;
        this.castleRook = castleRook;
    }

    public override void PlayMove()
    {
        base.PlayMove();

        //if move is a castling move
        if (castleRook != null)
        {
            rookCastleSquare.Unoccupy();
            rookCastleSquare.Occupy(castleRook);
            castleRook.square.Unoccupy();
            castleRook.square = rookCastleSquare;
        }
    }
}