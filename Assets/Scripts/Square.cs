using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Square : MonoBehaviour
{
    public enum Directions
    {
        TopLeft = Board.fileCount - 1,
        Top = Board.fileCount,
        TopRight = Board.fileCount + 1,
        Left = -1,
        Right = 1,
        BottomLeft = -Board.fileCount - 1,
        Bottom = -Board.fileCount,
        BottomRight = -Board.fileCount + 1,
    }

    public int squareNumber;

    public Color color;

    public Piece piece;

    public static readonly List<Directions> diagonalOffsets = new List<Directions> { Directions.TopLeft, Directions.TopRight, Directions.BottomLeft, Directions.BottomRight };
    public static readonly List<Directions> orthogonalOffsets = new List<Directions> { Directions.Left, Directions.Right, Directions.Top, Directions.Bottom };
    public static readonly List<Directions> horizontalOffsets = new List<Directions> { Directions.Left, Directions.Right };
    public static readonly List<Directions> verticalOffsets = new List<Directions> { Directions.Top, Directions.Bottom };
    public static readonly List<Directions> whitePawnOffsets = new List<Directions> { Directions.TopLeft, Directions.Top, Directions.TopRight };
    public static readonly List<Directions> blackPawnOffsets = new List<Directions> { Directions.BottomLeft, Directions.Bottom, Directions.BottomRight };
    public static readonly List<int> knightOffsets = new List<int>
    { 
        (int)Directions.Bottom * 2 + (int)Directions.Left, //-17 for 8x8 
        (int)Directions.Bottom * 2 + (int)Directions.Right, //-15 for 8x8
        (int)Directions.Top * 2 + (int)Directions.Left, //15 for 8x8
        (int)Directions.Top * 2 + (int)Directions.Right, //17 for 8x8
        (int)Directions.Bottom + (int)Directions.Left * 2, //-10 for 8x8
        (int)Directions.Bottom + (int)Directions.Right * 2, //-6 for 8x8
        (int)Directions.Top + (int)Directions.Left * 2, //6 for 8x8
        (int)Directions.Top + (int)Directions.Right * 2, //10 for 8x8
    }; //order is important, the first 4 offsets are 1 file away from the original square, while the last 4 are 2 files away

    public void Unoccupy()
    {
        piece = null;
    }

    public void Occupy(Piece newPiece)
    {
        piece = newPiece;
    }

    #region IsSquareInRange
    public static bool IsSquareInRange(int square)
    {
        return square >= 0 && square <= Board.maxSquareNumber;
    }

    public static bool IsSquareInRange(int file, int rank)
    {
        return file >= 0 && file <= Board.fileCount - 1 && rank >= 0 && rank <= Board.rankCount - 1;
    }
    #endregion

    #region GetFileOrRankOrDifference
    public static int GetFile(int squareNumber)
    {
        int temp = squareNumber;

        while (temp > Board.fileCount - 1)
        {
            temp -= Board.fileCount;
        }

        return temp;
    }

    public static int GetRank(int squareNumber)
    {
        return squareNumber / Board.fileCount;
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
        return rank * Board.fileCount + file; //(file * Board.rankCount + rank) also works
    }

    public static int GetSquare(int referenceSquare, Directions direction)
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

    public static bool TryGetSquare(int referenceSquare, Directions direction, out int targetSquare) //returns true if operation was successful
    {
        targetSquare = referenceSquare + (int)direction;

        if (!IsSquareInRange(referenceSquare) || !IsSquareInRange(targetSquare) || !AreSquaresAdjacent(referenceSquare, targetSquare))
        {
            targetSquare = -1;
            return false;
        }

        return true;
    } //use when arguments are not certain

    public static int GetBoundarySquare(int referenceSquare, Directions direction)
    {
        int file = GetFile(referenceSquare);
        int rank = GetRank(referenceSquare);
        int min = Math.Min(file, rank);
        int max = Math.Max(file, rank);

        int temp = referenceSquare;

        int multiplier = 0;

        switch (direction)
        {
            case Directions.Left: //
                multiplier = file;
                break;
            case Directions.Right: //
                multiplier = Board.fileCount - 1 - file;
                break;
            case Directions.Bottom: //
                multiplier = rank;
                break;
            case Directions.Top: //
                multiplier = Board.rankCount - 1 - rank;
                break;
            case Directions.BottomLeft: //
                multiplier = min;
                break;
            case Directions.TopLeft:
                multiplier = Board.rankCount - 2 - max;
                break;
            case Directions.BottomRight:
                while (GetFile(temp) != Board.fileCount - 1 && GetRank(temp) != 0)
                {
                    temp += (int)direction;
                }

                return temp;
            case Directions.TopRight: //
                while (GetFile(temp) != Board.fileCount - 1 && GetRank(temp) != Board.rankCount - 1)
                {
                    temp += (int)direction;
                }

                return temp;
        }

        return referenceSquare + (int)direction * multiplier;
    }
    #endregion

    public static bool AreSquaresAdjacent(int square1, int square2)
    {
        return GetFileDifference(square1, square2) <= 1 && GetRankDifference(square1, square2) <= 1;
    }

    #region SquaresInBetween
    public static List<int> GetSquaresInBetween(int square1, int square2)
    {
        TryGetSquaresInBetween(square1, square2, out var squares);

        if (squares.Count == 0)
        {
            return null;
        }

        return squares;
    }

    /// <summary>
    /// Gets the squares in between, if possible
    /// </summary>
    /// <param name="square1"></param>
    /// <param name="square2"></param>
    /// <param name="squares"></param>
    /// <returns>True if there are squares in between, and false if there aren't, or the squares are not reachable or the squares are the same</returns>
    public static bool TryGetSquaresInBetween(int square1, int square2, out List<int> squares, bool inclusiveOfSquare2 = false)
    {
        squares = new List<int>();

        if (!TryGetOffset(square1, square2, out int offsetFromSquare1, out int diff))
        {
            return false;
        }

        for (int i = 0, temp = square1 + offsetFromSquare1; i < (inclusiveOfSquare2 ? diff : diff - 1); i++) //(diff - 1) is the number of squares between the two squares
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
                if (Board.SquareNumberToSquare[squareNumber].piece == Piece.Empty)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        throw new System.Exception();
    }

    public static bool AreSquaresInBetweenEmpty(Piece piece1, Piece piece2)
    {
        if (TryGetSquaresInBetween(piece1.square.squareNumber, piece2.square.squareNumber, out List<int> squaresInBetween))
        {
            foreach (int squareNumber in squaresInBetween)
            {
                if (Board.SquareNumberToSquare[squareNumber].piece == Piece.Empty)
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
        if (!(square1 >= 0 && square1 <= 63) || !(square2 >= 0 && square2 <= 63) || (square1 == square2))
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
    /// <param name="square1"></param>
    /// <param name="square2"></param>
    /// <param name="offset"></param>
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
        var m = MoveManager.GetAllDefendedSquares(color);

        foreach (int square in squares)
        {
            if (m.Contains(square))
            {
                return true;
            }
        }

        return false;
    }
}
