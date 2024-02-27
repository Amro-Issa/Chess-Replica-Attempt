using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Piece
{
    public enum PieceType
    {
        Pawn,
        King,
        Knight,
        Bishop,
        Rook,
        Queen,
    }

    public enum PieceColor
    {
        White,
        Black
    }

    //public readonly struct PieceStruct
    //{
    //    readonly PieceColor color;
    //    readonly PieceType type;

    //    public PieceStruct(PieceColor color, PieceType type)
    //    {
    //        this.color = color;
    //        this.type = type;
    //    }
    //}


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
            case 'k':
                return PieceType.King;
            case 'n':
                return PieceType.Knight;
            case 'b':
                return PieceType.Bishop;
            case 'r':
                return PieceType.Rook;
            case 'q':
                return PieceType.Queen;
            default:
                throw new Exception("Invalid character as argument in method \"GetPieceTypeFromLetter\"");
        };
    }
    public static char GetPieceLetter(PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn:
                return 'p';
            case PieceType.King:
                return 'k';
            case PieceType.Knight:
                return 'n';
            case PieceType.Bishop:
                return 'b';
            case PieceType.Rook:
                return 'r';
            case PieceType.Queen:
                return 'q';
            default:
                throw new Exception("Invalid type as argument in method \"GetPieceLetter\"");
        }
    }

    public static Piece CreateNewPiece(PieceColor color, PieceType type, Square square)
    {
        switch (type)
        {
            case PieceType.Pawn:
                return new Pawn(color, square);
            case PieceType.King:
                return new King(color, square);
            case PieceType.Knight:
                return new Knight(color, square);
            case PieceType.Bishop:
                return new Bishop(color, square);
            case PieceType.Rook:
                return new Rook(color, square);
            case PieceType.Queen:
                return new Queen(color, square);
            default:
                throw new Exception();
        }
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

        return moves.Count == 0 ? new List<int>() : moves;  //make it so that the method returns an empty list instead of <null> if there are no legal moves for any piece of color <color>
    }
    public static List<int> GetAllRawMoves(PieceColor color)
    {
        var moves = new List<int>();

        foreach (Piece piece in Board.GetPieces(color))
        {
            if (piece is King)
            {
                moves.AddRange(piece.GetRawMoves(true));
            }
            else if (piece is Pawn)
            {
                moves.AddRange(piece.GetSquaresDefending());
            }
            else
            {
                moves.AddRange(piece.GetRawMoves());
            }
        }

        return moves.Count == 0 ? new List<int>() : moves;  //fix: make it so that the method returns an empty list instead of <null> if there are no legal moves for any piece of color <color>
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
    
    public bool IsPinned(out Piece pinner, out List<int> lineOfAttackSquares)
    {
        pinner = null;
        lineOfAttackSquares = null;

        King friendly_king = Board.GetKing(color);
        //checking if the king and the piece are reachable, and if so, getting the offset
        if (Square.TryGetOffset(friendly_king.square.SquareNumber, square.SquareNumber, out int offset, out _))
        {
            Square.Direction direction = (Square.Direction)offset;
            
            //getting the squares between the piece guarding the king and the boundary reached by taking the same path of the offset from the king to the piece
            if (Square.TryGetSquaresInBetween(friendly_king.square.SquareNumber, Square.GetBoundarySquare(friendly_king.square.SquareNumber, direction), out List<int> squaresInBetween, inclusiveOfSquare2: true))
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
                            lineOfAttackSquares = Square.GetSquaresInBetween(friendly_king.square.SquareNumber, square, inclusiveOfSquare2: true);
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
        targetSquare.Occupy(this); //occupying new square with this piece
        gameObj.transform.position = targetSquare.transform.position;

        //Checking for check, checkmate and stalemate
        King.kingPieceUnderCheck = null;
        MoveManager.checkers.Clear();
        if (GetAllLegalMoves(color).Contains(Board.GetKing(GetOppositeColor(color)).square.SquareNumber))
        {
            //if enemy king is under check
            King.kingPieceUnderCheck = Board.GetKing(GetOppositeColor(color));
            
            foreach(Piece piece in Board.GetPieces(color))
            {
                if (piece.GetRawMoves().Contains(King.kingPieceUnderCheck.square.SquareNumber))
                {
                    MoveManager.checkers.Add(piece);
                }
            }

            if (GetAllLegalMoves(GetOppositeColor(color)).Count == 0)
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
        else if (GetAllLegalMoves(GetOppositeColor(color)).Count == 0)
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
            if (Square.TryGetSquaresInBetween(King.kingPieceUnderCheck.square.SquareNumber, MoveManager.checkers[0].square.SquareNumber, out List<int> squaresInBetween, inclusiveOfSquare2: true))
            {
                legalMoves = legalMoves.Intersect(squaresInBetween).ToList();
                return;
            }
            else if(legalMoves.Contains(MoveManager.checkers[0].square.SquareNumber))
            {
                legalMoves.Clear();
                legalMoves.Add(MoveManager.checkers[0].square.SquareNumber);
                return;
            }
        }
        
        legalMoves.Clear();
    }
    protected virtual List<int> GetSquaresDefending()
    {
        return GetRawMoves();
    }
    public abstract List<int> GetRawMoves(bool absolute = false);
}

public class Pawn : Piece
{
    public static Square enPassantSquare;
    public static Pawn enPassantPawn;

    public static void ResetEnPassant()
    {
        enPassantSquare = null;
        enPassantPawn = null;
    }

    public static Pawn pawnToBePromoted;

    public Pawn(PieceColor color, Square square) : base(PieceType.Pawn, color, square)
    {

    }

    public override void Move(Square targetSquare)
    {
        if (MoveManager.EnPassantAllowed)
        {
            if (IsMoveDoubleAdvancement(square.SquareNumber, targetSquare.SquareNumber))
            {
                enPassantSquare = Board.Squares[(square.SquareNumber + targetSquare.SquareNumber) / 2];
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
        
        if(MoveManager.PawnPromotionAllowed)
        {
            if((color == PieceColor.White && square.Rank == Board.MAX_RANK) || (color == PieceColor.Black && square.Rank == 0))
            {
                pawnToBePromoted = this;
                UI.PawnPromotionInProgress = true;
            }
        }
    }

    public override List<int> GetRawMoves(bool absolute = false)
    {
        var moves = new List<int>();
        List<Square.Direction> offsets = (color == PieceColor.White) ? Square.whitePawnOffsets : Square.blackPawnOffsets; //order is important

        foreach (var offset in offsets)
        {
            int num = square.SquareNumber + (int)offset;

            //checking if the square is actually on the board (in bound)
            if (Square.IsSquareInRange(num) && Square.AreSquaresAdjacent(square.SquareNumber, num))
            {
                if (Square.VerticalOffsets.Contains(offset))
                {
                    if (absolute || Board.Squares[num].piece == null)
                    {
                        moves.Add(num);

                        if (IsDoubleAdvancementAvailable()) moves.Add(num + (int)offset);
                    }
                }
                else if (Square.DiagonalOffsets.Contains(offset))
                {
                    if (absolute || Board.Squares[num].piece?.color == GetOppositeColor(color) || (Board.Squares[num] == enPassantSquare && enPassantPawn.color == GetOppositeColor(color)))
                    {
                        moves.Add(num);
                    }
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
        int rank = Square.GetRank(square.SquareNumber);

        if (!hasMoved)
        {
            if (color == PieceColor.White && (rank == 0 || rank == 1)) //rank == 0 for if a white pawn spawns on the back rank when generating a random position
            {
                if (raw == true || !Board.IsSquareOccupied(square.SquareNumber + (int)Square.Direction.Top * 2))
                {
                    return true;
                }
            }
            else if (color == PieceColor.Black && (rank == Board.MAX_RANK || rank == Board.MAX_RANK - 1)) //rank == Board.maxRank for if a black pawn spawns on the back rank when generating a random position
            {
                if (raw == true || !Board.IsSquareOccupied(square.SquareNumber + (int)Square.Direction.Bottom * 2))
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
        List<Square.Direction> offsets = color == PieceColor.White ? Square.whitePawnCaptureOffsets : Square.blackPawnCaptureOffsets; //order is important

        foreach (var offset in offsets)
        {
            int target = square.SquareNumber + (int)offset;

            if (Square.IsSquareInRange(target) && Square.AreSquaresAdjacent(square.SquareNumber, target))
            {
                squares.Add(target);
            }
        }

        return squares;
    }

    /// <summary>
    /// Promotes this pawn to the given piece type
    /// </summary>
    /// <param name="type">the piece to promote to</param>
    public void Promote(PieceType type)
    {
        MoveManager.DestroyPiece(this);
        Piece piece;
        switch (type)
        {
            case PieceType.Bishop:
                piece = new Bishop(color, square);
                break;
            case PieceType.Knight:
                piece = new Knight(color, square);
                break;
            case PieceType.Rook:
                piece = new Rook(color, square);
                break;
            case PieceType.Queen:
                piece = new Queen(color, square);
                break;
            default:
                throw new Exception("Cannot promote pawn to that piece");
        }
        piece.Move(square);
        piece.hasMoved = false;
    }
}

public sealed class Knight : Piece
{
    public Knight(PieceColor color, Square square) : base(PieceType.Knight, color, square)
    {

    }
    
    
    /// <param name="absolute">this method gets the absolute raw moves regardless of the value of this parameter</param>
    /// <returns></returns>
    public override List<int> GetRawMoves(bool absolute = true)
    {
        var moves = new List<int>();

        for (int i = 0; i < Square.knightOffsets.Count; i++)
        {
            int targetSquareNumber = square.SquareNumber + Square.knightOffsets[i];

            if (Square.IsSquareInRange(targetSquareNumber) && Square.GetFileDifference(square.SquareNumber, targetSquareNumber) <= 2)
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
        
    public override List<int> GetRawMoves(bool absolute = false)
    {
        var moves = new List<int>();

        foreach (Square.Direction offset in Square.DiagonalOffsets)
        {
            int reference = square.SquareNumber;

            while (Square.TryGetSquare(reference, offset, out int target))
            {
                moves.Add(target);
                reference = target;

                if (!absolute && Board.IsSquareOccupied(target)) //if square is occupied by either a friendly or enemy piece
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

    public override List<int> GetRawMoves(bool absolute = false)
    {
        var moves = new List<int>();

        foreach (Square.Direction offset in Square.OrthogonalOffsets)
        {
            int temp = square.SquareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                moves.Add(targetSquare);
                temp = targetSquare;
                
                if (!absolute && Board.IsSquareOccupied(targetSquare)) //if square is occupied by either a friendly or enemy piece
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

    public override List<int> GetRawMoves(bool absolute = false)
    {
        var moves = new List<int>();

        foreach (Square.Direction offset in Enum.GetValues(typeof(Square.Direction)))
        {
            int temp = square.SquareNumber;

            while (Square.TryGetSquare(temp, offset, out int targetSquare))
            {
                moves.Add(targetSquare);
                temp = targetSquare;
                
                if (!absolute && Board.IsSquareOccupied(targetSquare)) //if square is occupied by either a friendly or enemy piece
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
    public override void AdjustMovesForCheck(ref List<int> legalMoves)
    {
        var enemyRawMoves = GetAllRawMoves(GetOppositeColor(color));
        var checkersRawMoves = new List<int>();

        foreach (Piece checker in MoveManager.checkers)
        {
            if (!(checker is Pawn))
            {
                checkersRawMoves.AddRange(checker.GetRawMoves(true));
            }
        }

        foreach (int move in legalMoves.ToArray())
        {
            if (enemyRawMoves.Contains(move) || checkersRawMoves.Contains(move))
            {
                legalMoves.Remove(move);
            }
        }
    }

    public override List<int> GetRawMoves(bool absolute = false)
    {
        return GetRawMovesV2(absolute, includeCastleMoves: true);
    }

    public override void Move(Square targetSquare)
    {
        if (!Square.AreSquaresAdjacent(square.SquareNumber, targetSquare.SquareNumber))
        {
            Castle(square.SquareNumber > targetSquare.SquareNumber ? CastleType.QueenSide : CastleType.KingSide);
        }
        else
        {
            base.Move(targetSquare);
        }
    }

    private bool IsCastlingAvailable(CastleType castleType, out Rook rook, out Square kingCastleSquare, out Square rookCastleSquare)
    {
        //castleType = square.squareNumber > rook.square.squareNumber ? CastleType.QueenSide : CastleType.KingSide;

        //need to check that:
        //-king and rook have not moved
        //-king is not in check
        //-path between them is clear
        //-king's path (2 squares) between them is not guarded by enemy piece(s)

        rook = null;
        kingCastleSquare = null;
        rookCastleSquare = null;

        if (!hasMoved && kingPieceUnderCheck != this)
        {
            int rookSquareNum = square.SquareNumber + (castleType == CastleType.QueenSide ? -4 : 3);
            if (Square.IsSquareInRange(rookSquareNum))
            {
                rook = (Board.Squares[rookSquareNum].piece is Rook r) ? r : null;
            
                if (rook != null && !rook.hasMoved)
                {
                    if (Square.AreSquaresInBetweenEmpty(square.SquareNumber, rook.square.SquareNumber))
                    {
                        int add = castleType == CastleType.QueenSide ? -3 : 3;

                        //check that path is not guarded by enemy piece(s)
                        //GETS THE SQUARES BETWEEN THE KING AND THE SQUARE 3 SQUARES BESIDE IT (NOT FROM KING TO ROOK) SO THAT YOU CAN STILL CASTLE DESPITE IF THERE IS A PIECE BLOCKING THE ROOKS PATH BUT NOT THE KING's
                        if (!Square.AreSquaresGuarded(GetOppositeColor(color), Square.GetSquaresInBetween(square.SquareNumber, square.SquareNumber + add)))
                        {
                            kingCastleSquare = castleType == CastleType.QueenSide ? Board.Squares[square.SquareNumber - 2] : Board.Squares[square.SquareNumber + 2];
                            rookCastleSquare = castleType == CastleType.QueenSide ? Board.Squares[square.SquareNumber - 1] : Board.Squares[square.SquareNumber + 1];

                            return true;
                        }
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
        return GetRawMovesV2(absolute: true, includeCastleMoves: false);
    }

    private List<int> GetRawMovesV2(bool absolute = false, bool includeCastleMoves = true)
    {
        var moves = new List<int>();

        List<int> enemyRawMoves = null;

        if (!absolute)
        {
            enemyRawMoves = GetAllRawMoves(GetOppositeColor(color));
        }

        foreach (int offset in Enum.GetValues(typeof(Square.Direction)))
        {
            int targetSquareNumber = square.SquareNumber + offset;

            //if square is within board range and squares are adjacent
            if (Square.IsSquareInRange(targetSquareNumber) && Square.AreSquaresAdjacent(square.SquareNumber, targetSquareNumber))
            {
                if (absolute || !enemyRawMoves.Contains(targetSquareNumber))
                {
                    moves.Add(targetSquareNumber);
                }
            }
        }

        if (includeCastleMoves)
        {
            for (int i = 0; i < 2; i++)
            {
                if (IsCastlingAvailable((CastleType)i, out _, out Square kingCastleSquare, out _))
                {
                    moves.Add(kingCastleSquare.SquareNumber);
                }
            }
        }

        return moves;
    }
}
