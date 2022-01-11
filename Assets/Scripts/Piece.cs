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
        get => CalculateLegalMoves();
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

    public abstract List<int> CalculateLegalMoves();

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

    public class Pawn : Piece, IRestrictedMovement
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

        public override List<int> CalculateLegalMoves() //rename to "GetLegalMoves"
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

            return totalMoves;
        }

        public List<int> GetSquaresDefending()
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

        public override List<int> CalculateLegalMoves()
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

        public override List<int> CalculateLegalMoves()
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

        public override List<int> CalculateLegalMoves()
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

    public class Queen : IDiagonalMovement, IOrthogonalMovement
    {
        public Queen(PieceColor color, Square square) : base(PieceType.Queen, color, square)
        {

        }

        public override List<int> CalculateLegalMoves()
        {
            List<int> legalMoves = new List<int>();

            legalMoves.AddRange(CalculateBishopLegalMoves(squareNumber, color));
            legalMoves.AddRange(CalculateRookLegalMoves(squareNumber, color));

            return legalMoves;
        }
    }

    public class King : Piece, IRestrictedMovement
    {
        public King(PieceColor color, Square square) : base(PieceType.King, color, square)
        {

        }

        public override List<int> CalculateLegalMoves()
        {
            List<int> legalMoves = new List<int>();

            //filtering the moves
            foreach (int offset in Enum.GetValues(typeof(Square.Directions)))
            {
                int targetSquareNumber = squareNumber + offset;

                //if square is within board range, squares are adjacent and the target square is not occupied by a friendly piece
                if (Square.IsSquareInRange(targetSquareNumber) && Square.AreSquaresAdjacent(squareNumber, targetSquareNumber) && Board.SquareNumberToSquare[targetSquareNumber].piece.color != color)
                {
                    legalMoves.Add(targetSquareNumber);
                }
            }

            if (addCastlingMoves)
            {
                foreach (Piece rook in Board.GetAllPieces(color, Piece.PieceType.Rook))
                {
                    if (IsCastlingAvailable(color, rook, out _, out _, out Square kingCastleSquare))
                    {
                        legalMoves.Add(kingCastleSquare.squareNumber);
                    }
                }
            }

            return legalMoves;
        }
    }

    //implemented on a piece when its movement is restricted to certain conditions (not including check or pinning). Examples of pieces that should implement this are pawns and kings
    public interface IRestrictedMovement
    {
        public List<int> GetSquaresDefending();
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