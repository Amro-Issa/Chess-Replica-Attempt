using System;
using System.Collections.Generic;
using System.Linq;
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


    /// <summary>
    /// Gets the legal moves of the piece, not accounting for whether it is pinned, its king is under check, etc..
    /// If you account for check, but not for pinning, the piece will be able to block check or capture checker even if it is pinned?
    /// </summary>
    /// <returns></returns>
    public List<int> GetLegalMoves(bool accountForPin = true, bool accountForCheck = true)
    {
        var moves = GetRawMoves();
            
        //ORDER HERE IS IMPORTANT. YOU NEED TO GET THE LEGAL MOVES IF THE PIECE IS PINNED, THEN, AND ONLY THEN, ADJUST FOR CHECK
        if (accountForPin && !(this is King))
        {
            if (IsPinned(out _, out List<int> squaresOfLineOfAttack))
            {
                moves = moves.Intersect(squaresOfLineOfAttack).ToList();
            }
        }
        
        if (accountForCheck && MoveManager.CheckAllowed && King.kingPieceUnderCheck?.color == color) //King.kingPieceUnderCheck? -- the question mark is so that the condition is only evaluated if the field is not null
        {
            AdjustMovesForCheck(ref moves);
        }

        return moves;
    }
    
    public bool IsPinned(out Piece pinner, out List<int> squaresOfLineOfAttack)
    {
        pinner = null;
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
                    Piece piece = Board.Squares[square].piece;

                    if (piece == null)
                    {
                        continue;
                    }
                    else if (piece?.color == GetOppositeColor(color))
                    {
                        if (type == PieceType.Queen || (type == PieceType.Rook && Square.orthogonalOffsets.Contains(direction)) || (type == PieceType.Bishop && Square.diagonalOffsets.Contains(direction)))
                        {
                            pinner = Board.Squares[square].piece;
                            squaresOfLineOfAttack = Square.GetSquaresInBetween(this.square.squareNumber, square, inclusiveOfSquare2: true);
                            return true;
                        }

                        return false;
                    }
                    else if (piece.color == color)
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
        hasMoved = true;
        square.Unoccupy(); //unoccupying old square
        square = targetSquare;
        square.Occupy(this); //occupying new square with this piece
        gameObj.transform.position = targetSquare.transform.position;

        //Checking for check
        King.kingPieceUnderCheck = null;
        if (GetLegalMoves().Contains(Board.GetKing(GetOppositeColor(color)).square.squareNumber))
        {
            MoveManager.squareOfChecker = targetSquare;
            King.kingPieceUnderCheck = Board.GetKing(GetOppositeColor(color));

            Debug.Log($"{color} is under check!");
        }
    }
     
    /// <summary>
    /// Adjusts moves to cover check or capture checker
    /// </summary>
    /// <param name="legalMoves"></param>
    public virtual void AdjustMovesForCheck(ref List<int> legalMoves)
    {
        //checking for blocking moves or capture
        if (Square.TryGetSquaresInBetween(King.kingPieceUnderCheck.square.squareNumber, MoveManager.squareOfChecker.squareNumber, out List<int> squaresInBetween, inclusiveOfSquare2: true))
        {
            legalMoves = legalMoves.Intersect(squaresInBetween).ToList();
        }
    }
    protected virtual List<int> GetSquaresDefending()
    {
        return GetLegalMoves(false, false);
    }
    protected abstract List<int> GetRawMoves();
}

public class Pawn : Piece
{
    private static int enPassantElapsedMoves;
    public static int EnPassantElapsedMoves
    {
        get => enPassantElapsedMoves;
        set
        {
            if (value == 1)
            {
                enPassantElapsedMoves++;
                ResetEnPassant();
            }
            else
            {
                throw new Exception();
            }
        }
    }

    public static Square enPassantSquare;
    public static Pawn enPassantPawn;

    public static void ResetEnPassant()
    {
        enPassantSquare = null;
        enPassantPawn = null;
    }

    public Pawn(PieceColor color, Square square) : base(PieceType.Pawn, color, square)
    {

    }

    public override void Move(Square targetSquare)
    {
        if (MoveManager.EnPassantAllowed)
        {
            if (IsMoveDoubleAdvancement(square.squareNumber, targetSquare.squareNumber))
            {
                enPassantSquare = Board.Squares[(square.squareNumber + targetSquare.squareNumber) / 2];
                enPassantPawn = this;
            }

            if (targetSquare == enPassantSquare)
            {
                MoveManager.PlayCaptureSound();
                UnityEngine.Object.Destroy(enPassantPawn.gameObj);
                enPassantPawn.square.Unoccupy();
            }

            if (enPassantPawn != this)
            {
                ResetEnPassant();
            }
        }

        base.Move(targetSquare);
    }

    protected override List<int> GetRawMoves()
    {
        var moves = new List<int>();
        List<Square.Directions> offsets = color == PieceColor.White ? Square.whitePawnOffsets : Square.blackPawnOffsets; //order is important

        foreach (var offset in offsets)
        {
            int temp = square.squareNumber + (int)offset;

            if (Square.IsSquareInRange(temp) && Square.AreSquaresAdjacent(square.squareNumber, temp))
            {
                if (offset == Square.Directions.Top || offset == Square.Directions.Bottom && Board.Squares[temp].piece == null)
                {
                    moves.Add(temp);

                    if (IsDoubleAdvancementAvailable()) moves.Add(temp + (int)offset);
                }
                else if (Board.Squares[temp].piece?.color == GetOppositeColor(color) || (Board.Squares[temp] == enPassantSquare && enPassantPawn.color != color))
                {
                    moves.Add(temp);
                }
            }
        }

        return moves;
    }

    /// <summary>
    /// Checks whether double advancement of the pawn is available
    /// </summary>
    /// <param name="raw">Whether to account for piece potentially occupying the double advancement square or not (true means don't account for it)</param>
    /// <returns></returns>
    private bool IsDoubleAdvancementAvailable(bool raw = false)
    {
        int rank = Square.GetRank(square.squareNumber);

        if (!hasMoved)
        {
            if (color == PieceColor.White && (rank == 0 || rank == 1)) //rank == 0 for if a white pawn spawns on the back rank when generating a random position
            {
                if (raw == true || !Board.IsSquareOccupied(square.squareNumber + (int)Square.Directions.Top * 2))
                {
                    return true;
                }
            }
            else if (color == PieceColor.Black && (rank == Board.MaxRank || rank == Board.MaxRank - 1)) //rank == Board.maxRank for if a black pawn spawns on the back rank when generating a random position
            {
                if (raw == true || !Board.IsSquareOccupied(square.squareNumber + (int)Square.Directions.Bottom * 2))
                {
                    return true;
                }
            }
        }

        return false;
    }
    public static bool IsMoveDoubleAdvancement(int originalSquareNum, int destinationSquareNum)
    {
        return Square.GetRankDifference(originalSquareNum, destinationSquareNum) == 2;
    }

    protected override List<int> GetSquaresDefending()
    {
        var squares = new List<int>();
        List<Square.Directions> offsets = color == PieceColor.White ? Square.whitePawnCaptureOffsets : Square.blackPawnCaptureOffsets; //order is important

        foreach (var offset in offsets)
        {
            int target = square.squareNumber + (int)offset;

            if (Square.IsSquareInRange(target) && Square.AreSquaresAdjacent(square.squareNumber, target))
            {
                squares.Add(target);
            }
        }

        return squares;
    }
}

public class Knight : Piece
{
    public Knight(PieceColor color, Square square) : base(PieceType.Knight, color, square)
    {

    }
    
    protected override List<int> GetRawMoves()
    {
        var moves = new List<int>();

        for (int i = 0; i < Square.knightOffsets.Count; i++)
        {
            int targetSquareNumber = square.squareNumber + Square.knightOffsets[i];

            if (Square.IsSquareInRange(targetSquareNumber) && Square.GetFileDifference(square.squareNumber, targetSquareNumber) <= 2)
            {
                if (!Board.IsSquareOccupied(targetSquareNumber, color)) //if target square is not occupied by a friendly piece
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
        
    protected override List<int> GetRawMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Square.diagonalOffsets)
        {
            int temp = square.squareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                if (Board.IsSquareOccupied(targetSquare)) //if square is occupied by either a friendly or enemy piece
                {
                    if (Board.IsSquareOccupied(targetSquare, GetOppositeColor(color))) //if square is occupied by enemy piece
                    {
                        moves.Add(targetSquare);
                    }

                    break;
                }

                moves.Add(targetSquare);
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

    protected override List<int> GetRawMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Square.orthogonalOffsets)
        {
            int temp = square.squareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                if (Board.IsSquareOccupied(targetSquare)) //if square is occupied by either a friendly or enemy piece
                {
                    if (Board.IsSquareOccupied(targetSquare, GetOppositeColor(color))) //if square is occupied by enemy piece
                    {
                        moves.Add(targetSquare);
                    }

                    break;
                }

                moves.Add(targetSquare);
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

    protected override List<int> GetRawMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Enum.GetValues(typeof(Square.Directions)))
        {
            int temp = square.squareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                if (Board.IsSquareOccupied(targetSquare)) //if square is occupied by either a friendly or enemy piece
                {
                    if (Board.IsSquareOccupied(targetSquare, GetOppositeColor(color))) //if square is occupied by enemy piece
                    {
                        moves.Add(targetSquare);
                    }

                    break;
                }

                moves.Add(targetSquare);
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

        foreach (int move in rawLegalMoves)
        {
            if (!defendedSquares.Contains(move) && !Square.AreSquaresReachable(MoveManager.squareOfChecker.squareNumber, move))
            {
                adjustedLegalMoves.Add(move);
            }
        }

        rawLegalMoves = adjustedLegalMoves;
    }

    protected override List<int> GetRawMoves()
    {
        var moves = new List<int>();
        moves.AddRange(GetAllButCastleMoves());
        if (MoveManager.CastleAllowed) moves.AddRange(GetCastleMoves());

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

    private bool IsCastlingAvailable(CastleType castleType, out Rook rook, out Square kingCastleSquare, out Square rookCastleSquare)
    {
        //castleType = square.squareNumber > rook.square.squareNumber ? CastleType.QueenSide : CastleType.KingSide;

        rook = null;
        kingCastleSquare = null;
        rookCastleSquare = null;

        if (!hasMoved && kingPieceUnderCheck != this)
        {
            rook = (Board.Squares[square.squareNumber + (castleType == CastleType.QueenSide ? -4 : 3)].piece is Rook r) ? r : null;

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

                    kingCastleSquare = castleType == CastleType.QueenSide ? Board.Squares[square.squareNumber - 2] : Board.Squares[square.squareNumber + 2];
                    rookCastleSquare = castleType == CastleType.QueenSide ? Board.Squares[square.squareNumber - 1] : Board.Squares[square.squareNumber + 1];

                    return true;
                }
            }
        }

        return false;
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
    private void Castle(CastleType castleType)
    {
        IsCastlingAvailable(castleType, out Rook rook, out Square kingCastleSquare, out Square rookCastleSquare);

        base.Move(kingCastleSquare);
        rook.Move(rookCastleSquare);

        //PLAY CASTLE SOUND EFFECT
    }

    protected override List<int> GetSquaresDefending()
    {
        return GetAllButCastleMoves();
    }

    private List<int> GetAllButCastleMoves()
    {
        var moves = new List<int>();

        foreach (int offset in Enum.GetValues(typeof(Square.Directions)))
        {
            int targetSquareNumber = square.squareNumber + offset;

            //if square is within board range and squares are adjacent
            if (Square.IsSquareInRange(targetSquareNumber) && Square.AreSquaresAdjacent(square.squareNumber, targetSquareNumber))
            {
                //if target square is not occupied by a friendly piece
                if (Board.Squares[targetSquareNumber].piece == null || Board.Squares[targetSquareNumber].piece.color == GetOppositeColor(color))
                {
                    moves.Add(targetSquareNumber);
                }
            }
        }

        return moves;
    }
}