using System;
using System.Collections.Generic;

public static class Extentions
{
    public static List<Piece.PieceType> longRangePieceTypes = new List<Piece.PieceType>() { Piece.PieceType.Bishop, Piece.PieceType.Rook, Piece.PieceType.Queen };

    public static bool IsLongRangePieceType(this Piece.PieceType type)
    {
        return longRangePieceTypes.Contains(type);
    }

    public static bool IsDiagonalOffset(this Square.Directions offset)
    {
        return Square.diagonalOffsets.Contains(offset);
    }

    public static bool IsHorizontalOffset(this Square.Directions offset)
    {
        return Square.horizontalOffsets.Contains(offset);
    }

    public static bool IsVerticalOffset(this Square.Directions offset)
    {
        return Square.verticalOffsets.Contains(offset);
    }
}

