using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece
{
    public enum PieceType
    {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King,
    }

    public enum PieceColor
    {
        White,
        Black
    }

    public readonly PieceType type;
    public readonly PieceColor color;
    public readonly PieceTypeSO pieceTypeSO = null;
    public readonly GameObject gameObj = null;
    public Square square = null;

    public bool hasMoved = false;

    public Piece(PieceType type, PieceColor color, Square square)
    {
        this.type = type;
        this.color = color;
        this.square = square;
        pieceTypeSO = Board.PieceTypeToSO[type];

        gameObj = UnityEngine.Object.Instantiate(pieceTypeSO.piecePrefab, square.transform.position, Quaternion.identity, (color == PieceColor.White ? Board.Instance.whitePiecesParent : Board.Instance.blackPiecesParent).transform);
        gameObj.GetComponent<SpriteRenderer>().sprite = color == PieceColor.White ? pieceTypeSO.whitePieceSprite : pieceTypeSO.blackPieceSprite;

        square.Occupy(this);
    }

    public static PieceType GetPieceTypeFromLetter(char letter)
    {
        switch (char.Parse(letter.ToString().ToLower()))
        {
            case 'p':
                return PieceType.Pawn;
            case 'n':
                return PieceType.Knight;
            case 'b':
                return PieceType.Bishop;
            case 'r':
                return PieceType.Rook;
            case 'q':
                return PieceType.Queen;
            case 'k':
                return PieceType.King;
            default:
                throw new Exception("Invalid character as argument in method \"GetPieceTypeFromLetter\"");
        };
    }
    public static PieceColor GetOppositeColor(PieceColor color)
    {
        if (color == PieceColor.White)
        {
            return PieceColor.Black;
        }
        else if (color == PieceColor.Black)
        {
            return PieceColor.White;
        }
        else
        {
            throw new Exception("ERROR");
        }
    }
    public static List<int> GetAllLegalMoves(PieceColor color)
    {
        var moves = new List<int>();

        foreach (Piece piece in Board.GetPieces(color))
        {
            moves.AddRange(piece.GetLegalMoves());
        }

        return moves.Count == 0 ? null : moves;
    }
    public static List<int> GetAllDefendedSquares(PieceColor color)
    {
        var moves = new List<int>();

        foreach (Piece piece in Board.GetPieces(color))
        {
            moves.AddRange(piece.GetSquaresDefending());
        }

        return moves;
    }

    public bool Equals(Piece piece)
    {
        if (type == piece.type && color == piece.color)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the moves of the piece as if it is the only piece on the board
    /// </summary>
    /// <returns></returns>
    public List<int> GetRawMoves()
    {
        var moves = GetAdvancementMoves();
        moves.AddRange(GetCaptureMoves());
        return moves;
    }
    /// <summary>
    /// Gets the legal moves of the piece, not accounting for whether it is pinned, its king is under check, etc..
    /// If you account for check, but not for pinning, the piece will be able to block check or capture checker even if it is pinned?
    /// </summary>
    /// <returns></returns>
    public List<int> GetLegalMoves(bool accountForPinning = true, bool accountForCheck = true)
    {
        var moves = GetAdvancementMoves();
        moves.AddRange(GetCaptureMoves());
        
        //ORDER HERE IS IMPORTANT. YOU NEED TO GET THE LEGAL MOVES IF THE PIECE IS PINNED THEN ONLY THEN ADJUST FOR CHECK
        if (accountForPinning && !(this is King))
        {
            AdjustMovesForPotentialPinning(ref moves);
        }

        if (accountForCheck && King.kingPieceUnderCheck?.color == color)
        {
            AdjustMovesForCheck(ref moves);
        }

        return moves;
    }
    /// <summary>
    /// Adjusts the legal moves if the piece is pinned, otherwise, does nothing
    /// </summary>
    /// <param name="legalMoves"></param>
    /// <returns>Whether the piece is pinned or not</returns>
    public void AdjustMovesForPotentialPinning(ref List<int> legalMoves)
    {
        if (IsPinned(out _, out var squaresOfLineOfAttack))
        {
            var adjustedLegalMoves = new List<int>();

            foreach (int square in squaresOfLineOfAttack)
            {
                if (legalMoves.Contains(square)) adjustedLegalMoves.Add(square);
            }

            legalMoves = adjustedLegalMoves;
        }
    }
    public bool IsPinned(out Piece pieceOfPinner, out List<int> squaresOfLineOfAttack)
    {
        pieceOfPinner = null;
        squaresOfLineOfAttack = null;

        //checking if the king and the piece are reachable and if so, getting the offset
        if (Square.TryGetOffset(Board.GetKing(color).square.squareNumber, square.squareNumber, out int offset, out _))
        {
            Square.Directions direction = (Square.Directions)offset;

            //getting the squares between the piece and the boundary reached by taking the same path of the offset from the king to the piece
            if (Square.TryGetSquaresInBetween(square.squareNumber, Square.GetBoundarySquare(square.squareNumber, direction), out List<int> squaresInBetween, inclusiveOfSquare2: true))
            {
                foreach (int square in squaresInBetween)
                {
                    Piece piece = Board.SquareNumberToSquare[square].piece;

                    if (piece?.color == GetOppositeColor(color))
                    {
                        if (type == PieceType.Queen || (type == PieceType.Rook && Square.orthogonalOffsets.Contains(direction)) || (type == PieceType.Bishop && Square.diagonalOffsets.Contains(direction)))
                        {
                            pieceOfPinner = Board.SquareNumberToSquare[square].piece;
                            squaresOfLineOfAttack = Square.GetSquaresInBetween(this.square.squareNumber, square, inclusiveOfSquare2: true);
                            return true;
                        }

                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        
        return false;
    }

    public virtual void Move(Square targetSquare)
    {
        MoveManager.squareOfChecker = targetSquare;
        King.kingPieceUnderCheck = null;

        hasMoved = true;
        square.Unoccupy(); //unoccupying old square
        square = targetSquare;
        square.Occupy(this); //occupying new square with this piece
        gameObj.transform.position = targetSquare.transform.position;

        if (Pawn.enPassantPawn?.color != color)
        {
            Pawn.enPassantPawn = null;
            Pawn.enPassantSquare = null;
        }

        King.kingPieceUnderCheck = null;
        if (GetLegalMoves().Contains(Board.GetKing(GetOppositeColor(color)).square.squareNumber))
        {
            MoveManager.squareOfChecker = targetSquare;
            King.kingPieceUnderCheck = Board.GetKing(GetOppositeColor(color));
        }
    }
     
    /// <summary>
    /// Adjusts moves to cover check or capture checker
    /// </summary>
    /// <param name="legalMoves"></param>
    public virtual void AdjustMovesForCheck(ref List<int> legalMoves)
    {
        var adjustedLegalMoves = new List<int>();

        King king = Board.GetKing(color);
        
        //checking for blocking moves or capture
        if (Square.TryGetSquaresInBetween(king.square.squareNumber, MoveManager.squareOfChecker.squareNumber, out List<int> squaresInBetween, inclusiveOfSquare2: true))
        {
            foreach (int move in squaresInBetween)
            {
                if (legalMoves.Contains(move)) //if the legal moves contain that specific move that blocks the check or captures the checking piece
                {
                    adjustedLegalMoves.Add(move);
                }
            }
        }

        legalMoves = adjustedLegalMoves.Count == 0 ? null : adjustedLegalMoves;
    }
    protected virtual List<int> GetSquaresDefending()
    {
        return GetLegalMoves(false, false);
    }
    protected abstract List<int> GetAdvancementMoves();
    protected abstract List<int> GetCaptureMoves(); //make virtual? NO
}

public class Pawn : Piece
{
    public static Square enPassantSquare;
    public static Pawn enPassantPawn;

    public Pawn(PieceColor color, Square square) : base(PieceType.Pawn, color, square)
    {

    }

    public override void Move(Square targetSquare)
    {
        if (IsMoveDoubleAdvancement(square.squareNumber, targetSquare.squareNumber))
        {
            enPassantSquare = Board.SquareNumberToSquare[(square.squareNumber + targetSquare.squareNumber) / 2];
            enPassantPawn = this;
        }

        base.Move(targetSquare);

        if (targetSquare == enPassantSquare)
        {
            MoveManager.PlayCaptureSound();
            UnityEngine.Object.Destroy(enPassantPawn.gameObj);
            enPassantPawn.square.Unoccupy();
        }
    }

    protected override List<int> GetAdvancementMoves()
    {
        var moves = new List<int>();

        Square.Directions offset = color == PieceColor.White ? Square.Directions.Top : Square.Directions.Bottom; //order is important

        int temp = square.squareNumber + (int)offset;

        if (Square.IsSquareInRange(temp) && Square.AreSquaresAdjacent(square.squareNumber, temp))
        {
            moves.Add(temp);
                
            if (IsDoubleAdvancementAvailable(true))
            {
                moves.Add(temp + (int)offset);
            }
        }

        return moves;
    }
    protected override List<int> GetCaptureMoves()
    {
        var moves = new List<int>();

        List<Square.Directions> offsets =  color == PieceColor.White ? Square.whitePawnCaptureOffsets : Square.blackPawnCaptureOffsets; //order is important

        foreach (var offset in offsets)
        {
            int temp = square.squareNumber + (int)offset;

            if (Square.IsSquareInRange(temp) && Square.AreSquaresAdjacent(square.squareNumber, temp))
            {
                if (Board.SquareNumberToSquare[temp].piece?.color == GetOppositeColor(color))
                {
                    moves.Add(temp);
                }
            }
        }

        return moves;
    }
    protected override List<int> GetSquaresDefending()
    {
        return GetCaptureMoves();
    }

    /// <summary>
    /// Checks whether double advancement of the pawn is available
    /// </summary>
    /// <param name="raw">Whether to account for pieces occupying the double advancement square or not (false means account for it)</param>
    /// <returns></returns>
    private bool IsDoubleAdvancementAvailable(bool raw = false)
    {
        int rank = Square.GetRank(square.squareNumber);

        if (color == PieceColor.White && (rank == 0 || rank == 1)) //rank == 0 for if a white pawn spawns on the back rank when generating a random position
        {
            if (raw == true || Board.SquareNumberToSquare[square.squareNumber + Board.fileCount * 2].piece == null)
            {
                return true;
            }
        }
        else if (color == PieceColor.Black && (rank == Board.rankCount - 1 || rank == Board.rankCount - 2)) //rank == Board.rankCount - 1 for if a black pawn spawns on the back rank when generating a random position
        {
            if (raw == true || Board.SquareNumberToSquare[square.squareNumber - Board.fileCount * 2].piece == null)
            {
                return true;
            }
        }

        return false;
    }
    public static bool IsMoveDoubleAdvancement(int originalSquareNum, int destinationSquareNum)
    {
        return Math.Abs(destinationSquareNum - originalSquareNum) == Board.fileCount * 2;
    }
}

public class Knight : Piece
{
    public Knight(PieceColor color, Square square) : base(PieceType.Knight, color, square)
    {

    }
    
    protected override List<int> GetAdvancementMoves()
    {
        var moves = new List<int>();

        for (int i = 0; i < Square.knightOffsets.Count; i++)
        {
            int targetSquareNumber = square.squareNumber + Square.knightOffsets[i];

            if (Square.IsSquareInRange(targetSquareNumber) && (Square.GetFileDifference(square.squareNumber, targetSquareNumber) == 1 || Square.GetFileDifference(square.squareNumber, targetSquareNumber) == 2))
            {
                if (Board.SquareNumberToSquare[targetSquareNumber].piece == null)
                {
                    moves.Add(targetSquareNumber);
                }
            }
        }

        return moves;
    }
    protected override List<int> GetCaptureMoves()
    {
        var moves = new List<int>();

        for (int i = 0; i < Square.knightOffsets.Count; i++)
        {
            int targetSquareNumber = square.squareNumber + Square.knightOffsets[i];

            if (Square.IsSquareInRange(targetSquareNumber) && (Square.GetFileDifference(square.squareNumber, targetSquareNumber) == 1 || Square.GetFileDifference(square.squareNumber, targetSquareNumber) == 2))
            {
                if (Board.SquareNumberToSquare[targetSquareNumber].piece?.color == GetOppositeColor(color))
                {
                    moves.Add(targetSquareNumber);
                }
            }
        }

        return moves;
    }
}

public class Bishop : Piece
{
    public Bishop(PieceColor color, Square square) : base(PieceType.Bishop, color, square)
    {
            
    }
        
    protected override List<int> GetAdvancementMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Square.diagonalOffsets)
        {
            int temp = square.squareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                if (Board.SquareNumberToSquare[targetSquare].piece != null)
                {
                    break;
                }

                moves.Add(targetSquare);
                temp = targetSquare;
            }
        }

        return moves;
    }
    protected override List<int> GetCaptureMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Square.diagonalOffsets)
        {
            int temp = square.squareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                if (Board.SquareNumberToSquare[targetSquare].piece?.color == GetOppositeColor(color))
                {
                    moves.Add(targetSquare);
                }
                else if (Board.SquareNumberToSquare[targetSquare].piece?.color == color)
                {
                    break;
                }

                temp = targetSquare;
            }
        }

        return moves;
    }
}

public class Rook : Piece
{
    public Rook(PieceColor color, Square square) : base(PieceType.Rook, color, square)
    {

    }

    protected override List<int> GetAdvancementMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Square.orthogonalOffsets)
        {
            int temp = square.squareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                if (Board.SquareNumberToSquare[targetSquare].piece != null)
                {
                    break;
                }

                moves.Add(targetSquare);
                temp = targetSquare;
            }
        }

        return moves;
    }
    protected override List<int> GetCaptureMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Square.orthogonalOffsets)
        {
            int temp = square.squareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                if (Board.SquareNumberToSquare[targetSquare].piece?.color == GetOppositeColor(color))
                {
                    moves.Add(targetSquare);
                }
                else if (Board.SquareNumberToSquare[targetSquare].piece?.color == color)
                {
                    break;
                }

                temp = targetSquare;
            }
        }

        return moves;
    }
}

public class Queen : Piece
{
    public Queen(PieceColor color, Square square) : base(PieceType.Queen, color, square)
    {

    }

    protected override List<int> GetAdvancementMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Enum.GetValues(typeof(Square.Directions)))
        {
            int temp = square.squareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                if (Board.SquareNumberToSquare[targetSquare].piece != null)
                {
                    break;
                }

                moves.Add(targetSquare);
                temp = targetSquare;
            }
        }

        return moves;
    }
    protected override List<int> GetCaptureMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Enum.GetValues(typeof(Square.Directions)))
        {
            int temp = square.squareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                if (Board.SquareNumberToSquare[targetSquare].piece?.color == GetOppositeColor(color))
                {
                    moves.Add(targetSquare);
                }
                else if (Board.SquareNumberToSquare[targetSquare].piece?.color == color)
                {
                    break;
                }

                temp = targetSquare;
            }
        }

        return moves;
    }
}

public class King : Piece
{
    public enum CastleType
    {
        QueenSide,
        KingSide,
    }

    public static King kingPieceUnderCheck;

    public King(PieceColor color, Square square) : base(PieceType.King, color, square) { }

    /// <summary>
    /// Adjusts moves to prevent king from moving to illegal squares
    /// </summary>
    public override void AdjustMovesForCheck(ref List<int> rawLegalMoves)
    {
        var adjustedLegalMoves = new List<int>();
        var defendedSquares = GetAllDefendedSquares(GetOppositeColor(color));
        int attackOffset = Square.GetOffset(MoveManager.squareOfChecker.squareNumber, square.squareNumber, out _); //need the offset to check if king actually moved out of the attacker's line of sight

        foreach (int move in rawLegalMoves)
        {
            if (!defendedSquares.Contains(move) && Square.GetOffset(MoveManager.squareOfChecker.squareNumber, move, out _) != attackOffset)
            {
                adjustedLegalMoves.Add(move);
            }
        }

    }

    protected override List<int> GetAdvancementMoves()
    {
        var moves = new List<int>();

        foreach (int offset in Enum.GetValues(typeof(Square.Directions)))
        {
            int targetSquareNumber = square.squareNumber + offset;

            //if square is within board range, squares are adjacent and the target square is not occupied by a friendly piece
            if (Square.IsSquareInRange(targetSquareNumber) && Square.AreSquaresAdjacent(square.squareNumber, targetSquareNumber) && Board.SquareNumberToSquare[targetSquareNumber].piece == null)
            {
                moves.Add(targetSquareNumber);
            }
        }

        moves.AddRange(GetCastleMoves());

        return moves;
    }
    protected override List<int> GetCaptureMoves()
    {
        var moves = new List<int>();

        foreach (int offset in Enum.GetValues(typeof(Square.Directions)))
        {
            int targetSquareNumber = square.squareNumber + offset;

            //if square is within board range, squares are adjacent and the target square is not occupied by a friendly piece
            if (Square.IsSquareInRange(targetSquareNumber) && Square.AreSquaresAdjacent(square.squareNumber, targetSquareNumber) && Board.SquareNumberToSquare[targetSquareNumber].piece?.color == GetOppositeColor(color))
            {
                moves.Add(targetSquareNumber);
            }
        }

        return moves;
    }

    public override void Move(Square targetSquare)
    {
        if (!Square.AreSquaresAdjacent(square.squareNumber, targetSquare.squareNumber))
        {
            Castle(square.squareNumber > targetSquare.squareNumber ? CastleType.QueenSide : CastleType.KingSide);
        }
        else
        {
            base.Move(targetSquare);
        }
    }

    private List<int> GetCastleMoves()
    {
        var moves = new List<int>();

        for (int i = 0; i < 2; i++)
        {
            if (IsCastlingAvailable((CastleType)i, out _, out Square kingCastleSquare, out _))
            {
                moves.Add(kingCastleSquare.squareNumber);
            }
        }

        return moves;
    }
    private bool IsCastlingAvailable(CastleType castleType, out Rook rook, out Square kingCastleSquare, out Square rookCastleSquare)
    {
        //castleType = square.squareNumber > rook.square.squareNumber ? CastleType.QueenSide : CastleType.KingSide;

        rook = null;
        kingCastleSquare = null;
        rookCastleSquare = null;

        if (!hasMoved)
        {
            rook = Board.SquareNumberToSquare[square.squareNumber + (castleType == CastleType.QueenSide ? -4 : 3)].piece is Rook r ? r : null;

            if ((bool)!rook?.hasMoved)
            {
                if (Square.AreSquaresInBetweenEmpty(square.squareNumber, rook.square.squareNumber))
                {
                    int add = castleType == CastleType.QueenSide ? -3 : 3;

                    //check if path is guarded by enemy piece(s) //GETS THE SQUARES BETWEEN THE KING AND THE SQUARE 3 SQUARES BESIDE IT (NOT FROM KING TO ROOK) SO THAT YOU CAN STILL CASTLE DESPITE IF THERE IS A PIECE BLOCKING THE ROOKS PATH BUT NOT THE KING's
                    if (Square.AreSquaresGuarded(GetOppositeColor(color), Square.GetSquaresInBetween(square.squareNumber, square.squareNumber + add)))
                    {
                        return false;
                    }

                    kingCastleSquare = castleType == CastleType.QueenSide ? Board.SquareNumberToSquare[square.squareNumber - 2] : Board.SquareNumberToSquare[square.squareNumber + 2];
                    rookCastleSquare = castleType == CastleType.QueenSide ? Board.SquareNumberToSquare[square.squareNumber - 1] : Board.SquareNumberToSquare[square.squareNumber + 1];

                    return true;
                }
            }
        }

        return false;
    }
    private void Castle(CastleType castleType)
    {
        IsCastlingAvailable(castleType, out Rook rook, out Square kingCastleSquare, out Square rookCastleSquare);

        base.Move(kingCastleSquare);
        rook.Move(rookCastleSquare);

        //PLAY CASTLE SOUND EFFECT
    }
}