using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    internal static void PrintMoves(List<int> selectedPieceLegalMoves)
    {
        foreach(int move in selectedPieceLegalMoves)
        {
            string x = Square.SquareNumberToAlphaNumeric(move);
            print(x);
        }
    }
}
