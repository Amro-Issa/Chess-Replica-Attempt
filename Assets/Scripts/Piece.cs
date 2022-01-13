using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Piece
{
    public enum PieceType
    {
        None,
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King,
        Any
    }

    public enum PieceColor
    {
        None,
        White = 1,
        Black = 2,
        Any
    }

    public readonly PieceType type = PieceType.None;
    public readonly PieceColor color = PieceColor.None;
    public readonly PieceTypeSO pieceTypeSO = null;
    public readonly GameObject gameObj = null;
    public Square square = null;

    public bool hasMoved = false;

    public List<int> LegalMoves
    {
        get => GetRawMoves();
    }

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

    public virtual void Move(Square newSquare)
    {
        if (newSquare.piece == null)
        {
            MoveManager.PlayMoveSound();
        }
        else
        {
            MoveManager.PlayCaptureSound();
        }

        hasMoved = true;
        square.Unoccupy();
        square = newSquare;
        square.Occupy(this);
        gameObj.transform.position = newSquare.transform.position;
    }

    public bool Equals(Piece piece)
    {
        if (type == piece.type && color == piece.color)
        {
            return true;
        }

        return false;
    }

    public virtual List<int> GetRawMoves()
    {
        var moves = new List<int>();
        moves.AddRange(GetAdvancementMoves());
        moves.AddRange(GetCaptureMoves());

        return moves;
    }

    public virtual List<int> GetLegalMoves()
    {
        var moves = GetRawMoves();

        if (King.kingPieceUnderCheck != null)
        {
            AdjustForIllegalMoves(ref moves);
        }
        
        return moves;
    }

    protected abstract List<int> GetAdvancementMoves();

    protected abstract List<int> GetCaptureMoves();

    /// <summary>
    /// Adjusts moves to cover check or capture checker
    /// </summary>
    /// <param name="legalMoves"></param>
    public virtual void AdjustForIllegalMoves(ref List<int> legalMoves)
    {
        var newLegalMoves = new List<int>();

        int kingSquareNumber = Board.GetKing(color).square.squareNumber;
        
        //checking for blocking moves or capture
        if (Square.TryGetSquaresInBetween(kingSquareNumber, MoveManager.squareOfChecker.squareNumber, out List<int> squaresInBetween, true))
        {
            foreach (int move in squaresInBetween)
            {
                if (legalMoves.Contains(move)) //if the legal moves contain that specific move that blocks the check or captures the checking piece
                {
                    newLegalMoves.Add(move);
                }
            }
        }

        legalMoves = newLegalMoves;
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
            moves.AddRange(piece.GetRawMoves());
        }
        
        return moves;
    }

    public class Pawn : Piece
    {
        public static Square enPassantSquare;

        public Pawn(PieceColor color, Square square) : base(PieceType.Pawn, color, square)
        {

        }

        public override void Move(Square newSquare)
        {
            if (IsMoveDoubleAdvancement(square.squareNumber, newSquare.squareNumber))
            {
                enPassantSquare = Board.SquareNumberToSquare[(square.squareNumber + newSquare.squareNumber) / 2];
            }

            base.Move(newSquare);
        }

        public override List<int> GetRawMoves()
        {
            var advancementMoves = new List<int>();
            var captureMoves = new List<int>();

            List<Square.Directions> offsets = color == PieceColor.White ? Square.whitePawnOffsets : Square.blackPawnOffsets; //order is important

            foreach (int offset in offsets)
            {
                int temp = square.squareNumber + offset;

                if (Square.IsSquareInRange(temp) && Square.AreSquaresAdjacent(square.squareNumber, temp))
                {
                    if (Board.SquareNumberToSquare[temp].piece != null && Math.Abs(offset) == (int)Square.Directions.TopLeft || Math.Abs(offset) == (int)Square.Directions.TopRight)
                    {
                        captureMoves.Add(temp);
                    }
                    else if (Board.SquareNumberToSquare[temp].piece == null) //offset == 8 or -8
                    {
                        advancementMoves.Add(temp);

                        if (IsDoubleAdvancementAvailable())
                        {
                            advancementMoves.Add(temp + offset);
                        }
                    }
                }
            }

            List<int> totalMoves = new List<int>();
            totalMoves.AddRange(advancementMoves);
            totalMoves.AddRange(captureMoves);

            AdjustForIllegalMoves(ref totalMoves);

            return totalMoves;
        }

        protected override List<int> GetAdvancementMoves()
        {
            throw new NotImplementedException();
        }

        protected override List<int> GetCaptureMoves()
        {
            throw new NotImplementedException();
        }

        public override List<int> GetSquaresDefending()
        {
            var captureMoves = new List<int>();

            List<Square.Directions> offsets = color == PieceColor.White ? Square.whitePawnOffsets : Square.blackPawnOffsets; //order is important

            foreach (int offset in offsets)
            {
                int temp = square.squareNumber + offset;

                if (Square.IsSquareInRange(temp) && Square.AreSquaresAdjacent(square.squareNumber, temp))
                {
                    if (Math.Abs(offset) == (int)Square.Directions.TopLeft || Math.Abs(offset) == (int)Square.Directions.TopRight)
                    {
                        captureMoves.Add(temp);
                    }
                }
            }

            return captureMoves;
        }

        private bool IsDoubleAdvancementAvailable()
        {
            int rank = Square.GetRank(square.squareNumber);

            if (color == PieceColor.White && (rank == 0 || rank == 1)) //rank == 0 for if a white pawn spawns on the back rank when generating a random position
            {
                if (Board.SquareNumberToSquare[square.squareNumber + Board.fileCount * 2].piece == null)
                {
                    return true;
                }
            }
            else if (color == PieceColor.Black && (rank == Board.rankCount - 1 || rank == Board.rankCount - 2)) //rank == Board.rankCount - 1 for if a black pawn spawns on the back rank when generating a random position
            {
                if (Board.SquareNumberToSquare[square.squareNumber - Board.fileCount * 2].piece == null)
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

        public override List<int> GetRawMoves()
        {
            List<int> legalMoves = new List<int>();

            int counter = 0; //keeps count of which offset it is currently at

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    int targetSquareNumber = square.squareNumber + Square.knightOffsets[counter];

                    if (Square.IsSquareInRange(targetSquareNumber))
                    {
                        int fileDiff = Square.GetFileDifference(square.squareNumber, targetSquareNumber);

                        if ((i == 0 && fileDiff == 1) || (i == 1 && fileDiff == 2))
                        {
                            PieceColor colorOfPieceOnTargetSquare = Board.SquareNumberToSquare[targetSquareNumber].piece.color;

                            if (colorOfPieceOnTargetSquare != color)
                            {
                                legalMoves.Add(targetSquareNumber);
                            }
                        }
                    }

                    counter++;
                }
            }

            return legalMoves;
        }


    }

    public class Bishop : Piece
    {
        public Bishop(PieceColor color, Square square) : base(PieceType.Bishop, color, square)
        {
            
        }

        public override List<int> GetRawMoves()
        {
            var moves = new List<int>();

            foreach (Square.Directions offset in Square.diagonalOffsets)
            {
                int temp = square.squareNumber;

                while (Square.TryGetSquare(temp, offset, out int targetSquare))
                {
                    PieceColor pieceColorOnTargetSquare = Board.SquareNumberToSquare[targetSquare].piece.color;

                    if (pieceColorOnTargetSquare == PieceColor.None)
                    {
                        moves.Add(targetSquare);
                    }
                    else if (pieceColorOnTargetSquare == color) //same color
                    {
                        break;
                    }
                    else //opposite color
                    {
                        moves.Add(targetSquare);
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

        public override List<int> GetRawMoves()
        {
            var legalMoves = new List<int>();

            foreach (Square.Directions offset in Square.orthogonalOffsets)
            {
                int temp = squareNumber;

                while (Square.TryGetSquare(temp, offset, out int targetSquare))
                {
                    Piece.PieceColor pieceColorOnTargetSquare = Board.SquareNumberToSquare[targetSquare].piece.color;

                    if (pieceColorOnTargetSquare == Piece.PieceColor.None)
                    {
                        legalMoves.Add(targetSquare);
                    }
                    else if (pieceColorOnTargetSquare == color) //same color
                    {
                        break;
                    }
                    else //opposite color
                    {
                        legalMoves.Add(targetSquare);
                        break;
                    }

                    temp = targetSquare;
                }
            }

            return legalMoves;
        }
    }

    public class Queen : Piece, IDiagonalMovement, IOrthogonalMovement
    {
        public Queen(PieceColor color, Square square) : base(PieceType.Queen, color, square)
        {

        }

        public override List<int> GetRawMoves()
        {
            List<int> legalMoves = new List<int>();

            legalMoves.AddRange(CalculateBishopLegalMoves(squareNumber, color));
            legalMoves.AddRange(CalculateRookLegalMoves(squareNumber, color));

            return legalMoves;
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

        public override List<int> GetRawMoves()
        {
            List<int> moves = new List<int>();

            moves.AddRange(GetAdvancementMoves());
            moves.AddRange(GetCaptureMoves());
            moves.AddRange(GetCastleMoves());

            AdjustForIllegalMoves(ref moves);

            return moves;
        }

        /// <summary>
        /// Adjusts moves to prevent king from moving to illegal squares
        /// </summary>
        public override void AdjustForIllegalMoves(ref List<int> legalMoves)
        {
            var newLegalMoves = new List<int>();

            var defendedSquares = GetAllDefendedSquares(GetOppositeColor(color));

            int attackOffset = Square.GetOffset(MoveManager.squareOfChecker.squareNumber, square.squareNumber, out _);

            foreach (int move in legalMoves)
            {
                if (!defendedSquares.Contains(move))
                {
                    if (kingPieceUnderCheck?.color == color && Square.GetOffset(MoveManager.squareOfChecker.squareNumber, move, out _) == attackOffset) //under check
                    {
                        continue;
                    }

                    newLegalMoves.Add(move);
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
            
            return moves;
        }
        
        protected override List<int> GetCaptureMoves()
        {
            var moves = new List<int>();

            foreach (int offset in Enum.GetValues(typeof(Square.Directions)))
            {
                int targetSquareNumber = square.squareNumber + offset;

                //if square is within board range, squares are adjacent and the target square is not occupied by a friendly piece
                if (Square.IsSquareInRange(targetSquareNumber) && Square.AreSquaresAdjacent(square.squareNumber, targetSquareNumber) && Board.SquareNumberToSquare[targetSquareNumber].piece.color == GetOppositeColor(color))
                {
                    moves.Add(targetSquareNumber);
                }
            }

            return moves;
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

                        //check if path is guarded by enemy piece(s)
                        if (Square.AreSquaresGuarded(GetOppositeColor(color), Square.GetSquaresInBetween(square.squareNumber, square.squareNumber + add))) //GETS THE SQUARES BETWEEN THE KING AND THE SQUARE 3 SQUARES BESIDE IT (NOT FROM KING TO ROOK) SO THAT YOU CAN STILL CASTLE DESPITE IF THERE IS A PIECE BLOCKING THE ROOKS PATH BUT NOT THE KING's
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

            Move(kingCastleSquare);
            rook.Move(rookCastleSquare);

            //PLAY CASTLE SOUND EFFECT
        }
    }

    
    public interface IDiagonalMovement
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>The diagonal moves of the implementing piece</returns>
        public List<int> GetDiagonalMoves();
    }

    public interface IOrthogonalMovement
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>The orthogonal moves of the implementing piece</returns>
        public List<int> GetOrthogonalMoves();
    }
}