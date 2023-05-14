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
    public bool isPinned = false;

    public static PieceType GetPieceType(char letter)
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

        return moves.Count == 0 ? null : moves;  //make it so that the method returns an empty list instead of <null> if there are no legal moves for any piece of color <color>
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

    public Piece(PieceType type, PieceColor color, Square square)
    {
        this.type = type;
        this.color = color;
        pieceTypeSO = Board.PieceTypeToSO[type];
        this.square = square;

        gameObj = UnityEngine.Object.Instantiate(pieceTypeSO.piecePrefab, square.transform.position, Quaternion.identity, (color == PieceColor.White ? Board.Instance.whitePiecesParent : Board.Instance.blackPiecesParent).transform);
        gameObj.GetComponent<SpriteRenderer>().sprite = color == PieceColor.White ? pieceTypeSO.whitePieceSprite : pieceTypeSO.blackPieceSprite;

        square.Occupy(this);
    }

    /// <summary>
    /// Gets the legal moves of the piece, not accounting for whether it is pinned, its king is under check, etc..
    /// If you account for check, but not for pinning, the piece will be able to block check or capture checker even if it is pinned?
    /// </summary>
    /// <returns></returns>
    public List<int> GetLegalMoves(bool accountForPin = true, bool accountForCheck = true)
    {
        var moves = GetRawMoves();
        
        foreach(int move in moves.ToArray())
        {
            if(Board.IsSquareOccupied(move, color))
            {
                moves.Remove(move);
            }
        }

        //ORDER HERE IS IMPORTANT. YOU NEED TO GET THE LEGAL MOVES IF THE PIECE IS PINNED, THEN, AND ONLY THEN, ADJUST FOR CHECK
        if (accountForPin && !(this is King))
        {
            if (IsPinned(out _, out List<int> squaresOfLineOfAttack))
            {
                moves = moves.Intersect(squaresOfLineOfAttack).ToList();
                isPinned = true;
            }
            else
            {
                isPinned = false;
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

        King friendly_king = Board.GetKing(color);
        //checking if the king and the piece are reachable and if so, getting the offset
        if (Square.TryGetOffset(friendly_king.square.squareNumber, square.squareNumber, out int offset, out _))
        {
            Square.Directions direction = (Square.Directions)offset;
            
            //getting the squares between the piece guarding the king and the boundary reached by taking the same path of the offset from the king to the piece
            if (Square.TryGetSquaresInBetween(friendly_king.square.squareNumber, Square.GetBoundarySquare(friendly_king.square.squareNumber, direction), out List<int> squaresInBetween, inclusiveOfSquare2: true))
            {
                bool thisPieceReached = false;
                foreach (int square in squaresInBetween)
                {
                    Piece piece = Board.Squares[square].piece;

                    if (piece == null || piece == this)
                    {
                        thisPieceReached = true;
                        continue;
                    }
                    else if (piece.color == color)
                    {
                        //if the first encountered piece (other than the one in question of being pinned)
                        //is of the same color as the piece guarding the king, we conclude logically that the guarding piece is not pinned (i.e. there is another guard)
                        return false;
                    }
                    else if (piece?.color == GetOppositeColor(color))
                    {
                        //if the first encountered piece (other than the one in question of being pinned)
                        //is of the opposite color as the piece guarding the king, and the piece guarding the king is the one in between the king and the attacker
                        if (thisPieceReached && piece.type == PieceType.Queen || (piece.type == PieceType.Rook && Square.OrthogonalOffsets.Contains(direction)) || (piece.type == PieceType.Bishop && Square.DiagonalOffsets.Contains(direction)))
                        {
                            pinner = Board.Squares[square].piece;
                            squaresOfLineOfAttack = Square.GetSquaresInBetween(friendly_king.square.squareNumber, square, inclusiveOfSquare2: true);
                            return true;
                        }

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

        //Checking for check, checkmate and stalemate
        King.kingPieceUnderCheck = null;
        MoveManager.checkers.Clear();
        if (GetAllLegalMoves(color).Contains(Board.GetKing(GetOppositeColor(color)).square.squareNumber))
        {
            //if enemy king is under check
            King.kingPieceUnderCheck = Board.GetKing(GetOppositeColor(color));
            
            foreach(Piece piece in Board.GetPieces(color))
            {
                if (piece.GetRawMoves().Contains(King.kingPieceUnderCheck.square.squareNumber))
                {
                    MoveManager.checkers.Add(piece);
                }
            }

            if (GetAllLegalMoves(GetOppositeColor(color)) == null)
            {
                //if enemy has no legal moves (checkmate)
                MoveManager.GameOver = true;
                Debug.Log($"Game Over! {color} checkmated {GetOppositeColor(color)}.");
            }
            else
            {
                Debug.Log($"{GetOppositeColor(color)} is under check!");
                Debug.Log($"Number of checkers: {MoveManager.checkers.Count}");
            }
        }
        else if (GetAllLegalMoves(GetOppositeColor(color)) == null)
        {
            //if enemy is not under check and has no legal moves (stalemate)
            MoveManager.GameOver = true;
            Debug.Log($"Game Over! Draw by Stalemate!");
        }

        if (Pawn.enPassantPawn?.color == GetOppositeColor(color))
        {
            Pawn.ResetEnPassant();
        }
    }
     
    /// <summary>
    /// Adjusts moves to cover check or capture checker
    /// </summary>
    /// <param name="legalMoves"></param>
    public virtual void AdjustMovesForCheck(ref List<int> legalMoves)
    {
        if (MoveManager.checkers.Count == 0)
        {
            throw new Exception("Method \"AdjustMovesForCheck\" was called even though king is not under check");
        }

        //need to account for one checker and more than one checker
        if (MoveManager.checkers.Count == 1)
        {
            //checking for blocking moves or capture
            if (Square.TryGetSquaresInBetween(King.kingPieceUnderCheck.square.squareNumber, MoveManager.checkers[0].square.squareNumber, out List<int> squaresInBetween, inclusiveOfSquare2: true))
            {
                legalMoves = legalMoves.Intersect(squaresInBetween).ToList();
                return;
            }
            else if(legalMoves.Contains(MoveManager.checkers[0].square.squareNumber))
            {
                legalMoves.Clear();
                legalMoves.Add(MoveManager.checkers[0].square.squareNumber);
                return;
            }
        }
        
        legalMoves.Clear();
    }
    protected virtual List<int> GetSquaresDefending()
    {
        return GetRawMoves();
    }
    protected abstract List<int> GetRawMoves();
}

public sealed class Pawn : Piece
{
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
            else if (targetSquare == enPassantSquare)
            {
                //capturing by en passant
                MoveManager.PlayCaptureSound();
                UnityEngine.Object.Destroy(enPassantPawn.gameObj);
                enPassantPawn.square.Unoccupy();
            }
        }

        base.Move(targetSquare);
    }

    protected override List<int> GetRawMoves()
    {
        var moves = new List<int>();
        List<Square.Directions> offsets = (color == PieceColor.White) ? Square.whitePawnOffsets : Square.blackPawnOffsets; //order is important

        foreach (var offset in offsets)
        {
            int num = square.squareNumber + (int)offset;

            //checking if the square is actually on the board (in bound)
            if (Square.IsSquareInRange(num) && Square.AreSquaresAdjacent(square.squareNumber, num))
            {
                if (Square.VerticalOffsets.Contains(offset) && Board.Squares[num].piece == null)
                {
                    moves.Add(num);

                    if (IsDoubleAdvancementAvailable()) moves.Add(num + (int)offset);
                }
                else if (Square.DiagonalOffsets.Contains(offset) && Board.Squares[num].piece?.color == GetOppositeColor(color) || (Board.Squares[num] == enPassantSquare && enPassantPawn.color == GetOppositeColor(color)))
                {
                    moves.Add(num);
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

public sealed class Knight : Piece
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
                moves.Add(targetSquareNumber);
            }
        }

        return moves;
    }
}

public sealed class Bishop : Piece
{
    public Bishop(PieceColor color, Square square) : base(PieceType.Bishop, color, square)
    {
            
    }
        
    protected override List<int> GetRawMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Square.DiagonalOffsets)
        {
            int reference = square.squareNumber;

            while (Square.TryGetSquare(reference, offset, out int target))
            {
                moves.Add(target);
                reference = target;

                if (Board.IsSquareOccupied(target)) //if square is occupied by either a friendly or enemy piece
                {   
                    break;
                }
            }
        }

        return moves;
    }
}

public sealed class Rook : Piece
{
    public Rook(PieceColor color, Square square) : base(PieceType.Rook, color, square)
    {

    }

    protected override List<int> GetRawMoves()
    {
        var moves = new List<int>();

        foreach (Square.Directions offset in Square.OrthogonalOffsets)
        {
            int temp = square.squareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                moves.Add(targetSquare);
                temp = targetSquare;
                
                if (Board.IsSquareOccupied(targetSquare)) //if square is occupied by either a friendly or enemy piece
                {
                    break;
                }
            }
        }

        return moves;
    }
}

public sealed class Queen : Piece
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
                moves.Add(targetSquare);
                temp = targetSquare;
                
                if (Board.IsSquareOccupied(targetSquare)) //if square is occupied by either a friendly or enemy piece
                {
                    break;
                }
            }
        }

        return moves;
    }
}

public sealed class King : Piece
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
        
    }

    protected override List<int> GetRawMoves()
    {
        return GetMoves(MoveManager.CastleAllowed);
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
            int num = square.squareNumber + (castleType == CastleType.QueenSide ? -4 : 3);
            if (Square.IsSquareInRange(num))
            {
                rook = (Board.Squares[num].piece is Rook r) ? r : null;
            
                if (rook != null && !rook.hasMoved)
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

    protected override List<int> GetSquaresDefending()
    {
        return GetMoves(false, true);
    }

    private List<int> GetMoves(bool includeCastleMoves = true, bool canWalkIntoCheck = false)
    {
        var moves = new List<int>();

        foreach (int offset in Enum.GetValues(typeof(Square.Directions)))
        {
            int targetSquareNumber = square.squareNumber + offset;

            //if square is within board range and squares are adjacent
            if (Square.IsSquareInRange(targetSquareNumber) && Square.AreSquaresAdjacent(square.squareNumber, targetSquareNumber))
            {
                if (canWalkIntoCheck)
                {
                    moves.Add(targetSquareNumber);
                }
                else
                {
                    var defendedSquares = GetAllDefendedSquares(GetOppositeColor(color));

                    if (!defendedSquares.Contains(targetSquareNumber))
                    {
                        moves.Add(targetSquareNumber);
                    }
                }
            }
        }

        if (includeCastleMoves)
        {
            for (int i = 0; i < 2; i++)
            {
                if (IsCastlingAvailable((CastleType)i, out _, out Square kingCastleSquare, out _))
                {
                    moves.Add(kingCastleSquare.squareNumber);
                }
            }
        }

        return moves;
    }
}