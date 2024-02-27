using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Square : MonoBehaviour
{
    public enum Direction
    {//comment represents standard for 8x8 board
        TopLeft = Top + Left, //7
        Top = Board.FILE_COUNT, //8
        TopRight = Top + Right, //9
        Left = -1,
        Right = 1,
        BottomLeft = Bottom + Left, //-9
        Bottom = -Board.FILE_COUNT, //-8
        BottomRight = Bottom + Right, //-7
    }

    public enum SquareColor
    {
        Light,
        Dark,
        Highlighted
    }

    private int? square_number = null;
    public int SquareNumber
    {
        get
        {
            if (square_number != null)
            {
                return (int)square_number;
            }
            else
            {
                throw new Exception("Field is null");
            }
        }
        set
        {
            if (square_number == null)
            {
                square_number = value;
            }
        }
    }

    public int File
    {
        get
        {
            return GetFile(SquareNumber);
        }
    }

    public int Rank
    {
        get
        {
            return GetRank(SquareNumber);
        }
    }

    private SquareColor _color;
    public Color _physicalColor { get; private set; }
    public SquareColor Color
    {
        get
        {
            return _color;
        }
        set
        {
            if(value == SquareColor.Light)
            {
                _color = SquareColor.Light;
            }
            else if(value == SquareColor.Dark)
            {
                _color = SquareColor.Dark;
            }
            else if(value == SquareColor.Highlighted)
            {
                _physicalColor = Board.Instance.selectionSquareColor;
                return;
            }
            else
            {
                throw new ArgumentException();
            }

            _physicalColor = GetPhysicalColor(value);
        }
    }


    public Piece piece;

    public static Color GetPhysicalColor(SquareColor color)
    {
        if (color == SquareColor.Light)
        {
            return Board.Instance.lightSquareColor;
        }
        else if (color == SquareColor.Dark)
        {
            return Board.Instance.darkSquareColor;
        }
        else
        {
            throw new ArgumentException("Invalid color argument");
        }
    }

    public void Unoccupy(bool destroyPiece = false)
    {
        if (destroyPiece && piece != null)
        {
            Destroy(piece.gameObj);
        }

        piece = null;
    }

    public void Occupy(Piece newPiece)
    {
        piece = newPiece;
    }

    #region Offsets
    public static readonly List<Direction> HorizontalOffsets = new List<Direction> { Direction.Left, Direction.Right };
    public static readonly List<Direction> VerticalOffsets = new List<Direction> { Direction.Top, Direction.Bottom };
    public static readonly List<Direction> OrthogonalOffsets = new List<Direction> { Direction.Left, Direction.Right, Direction.Top, Direction.Bottom };
    public static readonly List<Direction> DiagonalOffsets = new List<Direction> { Direction.TopLeft, Direction.TopRight, Direction.BottomLeft, Direction.BottomRight };

    #region PawnOffsets
    public static readonly List<Direction> whitePawnOffsets = new List<Direction> { Direction.TopLeft, Direction.Top, Direction.TopRight };
    public static readonly List<Direction> whitePawnCaptureOffsets = new List<Direction> { Direction.TopLeft, Direction.TopRight };

    public static readonly List<Direction> blackPawnOffsets = new List<Direction> { Direction.BottomLeft, Direction.Bottom, Direction.BottomRight };
    public static readonly List<Direction> blackPawnCaptureOffsets = new List<Direction> { Direction.BottomLeft, Direction.BottomRight };
    #endregion

    public static readonly List<int> knightOffsets = new List<int>
    {//comment is standard 8x8 board
        (int)Direction.Bottom * 2 + (int)Direction.Left, //-17
        (int)Direction.Bottom * 2 + (int)Direction.Right, //-15
        (int)Direction.Top * 2 + (int)Direction.Left, //15
        (int)Direction.Top * 2 + (int)Direction.Right, //17
        (int)Direction.Bottom + (int)Direction.Left * 2, //-10
        (int)Direction.Bottom + (int)Direction.Right * 2, //-6
        (int)Direction.Top + (int)Direction.Left * 2, //6
        (int)Direction.Top + (int)Direction.Right * 2, //10
    }; //order is important, the first 4 offsets are 1 file away from the original square, while the last 4 are 2 files away
    #endregion

    public static string SquareNumberToAlphaNumeric(int squareNum)
    {
        char letter = (char)(97 + GetFile(squareNum)); //integer that gets casted to an ASCII letter
        return letter + (GetRank(squareNum) + 1).ToString(); //combining letter and rank #
    }

    #region IsSquareInRange
    public static bool IsSquareInRange(int square)
    {
        return square >= 0 && square <= Board.MAX_SQUARE;
    }
    public static bool IsSquareInRange(int file, int rank)
    {
        return file >= 0 && file <= Board.FILE_COUNT - 1 && rank >= 0 && rank <= Board.RANK_COUNT - 1;
    }
    #endregion

    #region GetFileOrRankOrDifference
    public static int GetFile(int squareNumber)
    {
        int temp = squareNumber;

        while (temp > Board.FILE_COUNT - 1)
        {
            temp -= Board.FILE_COUNT;
        }

        return temp;
    }
    public static int GetRank(int squareNumber)
    {
        return squareNumber / Board.FILE_COUNT;
    }
    public static int GetFileDifference(int square1, int square2, bool absoluteValue = true)
    {
        return absoluteValue ? Mathf.Abs(GetFile(square1) - GetFile(square2)) : GetFile(square1) - GetFile(square2);
    }
    public static int GetRankDifference(int square1, int square2, bool absoluteValue = true)
    {
        return absoluteValue ? Mathf.Abs(GetRank(square1) - GetRank(square2)) : GetRank(square1) - GetRank(square2);
    }
    #endregion

    #region GetOrTryGetSquare
    public static int GetSquare(int file, int rank)
    {
        return rank * Board.FILE_COUNT + file; //(file * Board.rankCount + rank) also works
    }
    public static int GetSquare(int referenceSquare, Direction direction)
    {
        return referenceSquare + (int)direction;
    }
    public static bool TryGetSquare(int file, int rank, out int targetSquare)
    {
        if (!IsSquareInRange(file, rank))
        {
            targetSquare = -1;
            return false;
        }

        targetSquare = GetSquare(file, rank);
        return true;
    } //use when arguments are not certain
    public static bool TryGetSquare(int referenceSquare, Direction direction, out int targetSquare) //returns true if operation was successful
    {
        targetSquare = referenceSquare + (int)direction;

        if (!IsSquareInRange(referenceSquare) || !IsSquareInRange(targetSquare) || !AreSquaresAdjacent(referenceSquare, targetSquare))
        {
            targetSquare = -1;
            return false;
        }

        return true;
    } //use when arguments are not certain
    public static int GetBoundarySquare(int referenceSquare, Direction direction)
    {
        int file = GetFile(referenceSquare);
        int rank = GetRank(referenceSquare);

        int multiplier = 0;

        switch (direction)
        {
            case Direction.Left:
                multiplier = file;
                break;
            case Direction.Right:
                multiplier = Board.MAX_FILE - file;
                break;
            case Direction.Bottom:
                multiplier = rank;
                break;
            case Direction.Top:
                multiplier = Board.MAX_RANK - rank;
                break;
            case Direction.BottomLeft:
                multiplier = Math.Min(file, rank);
                break;
            case Direction.TopLeft:
                multiplier = Math.Min(file, Board.MAX_RANK - rank);
                break;
            case Direction.BottomRight:
                multiplier = Math.Min(Board.MAX_FILE - file, rank);
                break;
            case Direction.TopRight:
                multiplier = Math.Min(Board.MAX_FILE - file, Board.MAX_RANK - rank);
                break;
        }

        return referenceSquare + (int)direction * multiplier;
    }
    #endregion

    public static bool AreSquaresAdjacent(int square1, int square2)
    {
        return GetFileDifference(square1, square2) <= 1 && GetRankDifference(square1, square2) <= 1;
    }

    #region SquaresInBetween
    public static List<int> GetSquaresInBetween(int square1, int square2, bool inclusiveOfSquare1 = false, bool inclusiveOfSquare2 = false)
    {
        TryGetSquaresInBetween(square1, square2, out var squares, inclusiveOfSquare1, inclusiveOfSquare2);

        return squares;
    }
    /// <summary>
    /// Gets the squares in between, if possible
    /// </summary>
    /// <param name="square1"></param>
    /// <param name="square2"></param>
    /// <param name="squares"></param>
    /// <returns>True if there are squares in between, and false if there aren't, or the squares are not reachable, or the squares are the same</returns>
    public static bool TryGetSquaresInBetween(int square1, int square2, out List<int> squares, bool inclusiveOfSquare1 = false, bool inclusiveOfSquare2 = false)
    {
        squares = new List<int>();

        if (!TryGetOffset(square1, square2, out int offsetFromSquare1, out int diff))
        {
            return false;
        }

        for (int i = 0, temp = square1 + (inclusiveOfSquare1 ? 0 : offsetFromSquare1); i < (inclusiveOfSquare2 ? diff : diff - 1); i++) //(diff - 1) is the number of squares between the two squares (exclusive)
        {
            squares.Add(temp);
            temp += offsetFromSquare1;
        }

        if (squares.Count == 0)
        {
            return false;
        }

        return true;
    }
    public static bool AreSquaresInBetweenEmpty(int square1, int square2)
    {
        if (TryGetSquaresInBetween(square1, square2, out List<int> squaresInBetween))
        {
            foreach (int squareNumber in squaresInBetween)
            {
                if (Board.Squares[squareNumber].piece == null)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        throw new Exception();
    }
    public static bool AreSquaresInBetweenEmpty(Piece piece1, Piece piece2)
    {
        if (TryGetSquaresInBetween(piece1.square.SquareNumber, piece2.square.SquareNumber, out List<int> squaresInBetween))
        {
            foreach (int squareNumber in squaresInBetween)
            {
                if (Board.Squares[squareNumber].piece == null)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        throw new System.Exception();
    }
    #endregion

    #region Offset
    /// <summary>
    /// Gets the offset that square1 must take to reach square2
    /// </summary>
    /// <param name="square1"></param>
    /// <param name="square2"></param>
    /// <param name="fileOrRankDiff"></param>
    /// <returns></returns>
    public static int GetOffset(int square1, int square2, out int fileOrRankDiff)
    {
        if (!(square1 >= 0 && square1 <= Board.MAX_SQUARE) || !(square2 >= 0 && square2 <= Board.MAX_SQUARE) || (square1 == square2))
        {
            throw new System.Exception("ERROR");
        }

        int fileDiff = GetFileDifference(square1, square2);
        int rankDiff = GetRankDifference(square1, square2);

        //if rank or file difference is 0, then we know the attack is not coming in from a diagonal, like a bishop's attack
        if (fileDiff == 0) //vertical
        {
            fileOrRankDiff = rankDiff;
        }
        else if (rankDiff == 0) //horizontal
        {
            fileOrRankDiff = fileDiff;
        }
        else if (fileDiff == rankDiff) //diagonal
        {
            fileOrRankDiff = fileDiff; //here it doesn't matter whether we get the file or rank difference because they are the same
        }
        else //squares are not "lined up" in a legally "straight line" (i.e. if we were to extrapolate the lines emerging from square1, we wouldn't hit square2)
        {
            throw new System.Exception("ERROR");
        }

        return (square2 - square1) / fileOrRankDiff; //equation
    }
    /// <summary>
    /// Gets the offset between two squares with the origin being at square1 
    /// </summary>
    /// <param name="fileOrRankDiff">The difference between either the files or the ranks, depending on which one isn't 0</param>
    /// <returns>Boolean: whether the function was successful or not</returns>
    public static bool TryGetOffset(int square1, int square2, out int offset, out int fileOrRankDiff)
    {
        offset = 0;
        fileOrRankDiff = 0;

        if (!IsSquareInRange(square1) || !IsSquareInRange(square2) || (square1 == square2))
        {
            return false;
        }

        int fileDiff = GetFileDifference(square1, square2);
        int rankDiff = GetRankDifference(square1, square2);

        //if rank or file difference is 0, then we know the attack is not coming in from a diagonal, like a bishop's attack
        if (fileDiff == 0) //vertical attack
        {
            fileOrRankDiff = rankDiff;
        }
        else if (rankDiff == 0) //horizontal attack
        {
            fileOrRankDiff = fileDiff;
        }
        else if (fileDiff == rankDiff) //diagonal attack
        {
            fileOrRankDiff = fileDiff; //here it doesn't matter whether we get the file or rank difference because they are the same
        }
        else //the attacker is not attacking the defender square in a legally "straight line" (i.e. if we were to extrapolate the lines emerging from the attacker square from each offset, we wouldn't hit the defender square)
        {
            return false;
        }

        offset = (square2 - square1) / fileOrRankDiff; //equation

        return true;
    }
    #endregion

    /// <summary>
    /// Checks and returns whether the two squares can be reached from one another by taking a horizontal, vertical or diagonal path
    /// </summary>
    /// <param name="square1"></param>
    /// <param name="square2"></param>
    /// <returns></returns>
    public static bool AreSquaresReachable(int square1, int square2)
    {
        return TryGetOffset(square1, square2, out _, out _);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="color">The color of the guarding pieces</param>
    /// <param name="squares">The squares potentially being guarded</param>
    /// <returns>True if any one of the squares passed in are guarded by the color specified</returns>
    public static bool AreSquaresGuarded(Piece.PieceColor color, List<int> squares)
    {
        var defendedSquares = Piece.GetAllDefendedSquares(color);

        foreach (int square in squares)
        {
            if (defendedSquares.Contains(square))
            {
                return true;
            }
        }

        return false;
    }
}
