using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    static bool flag = false;

    internal static void PrintMoves(List<int> selectedPieceLegalMoves)
    {
        foreach(int move in selectedPieceLegalMoves)
        {
            string x = Square.SquareNumberToAlphaNumeric(move);
            print(x);
        }
    }
    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.N))
        //{
        //    foreach (int move in Piece.GetAllLegalMoves(Piece.PieceColor.White))
        //    {
        //        print($"{Square.SquareNumberToAlphaNumeric(move)}\n");
        //    }
        //}
        //else if (Input.GetKeyDown(KeyCode.M))
        //{
        //    foreach (int move in Piece.GetAllLegalMoves(Piece.PieceColor.Black))
        //    {
        //        print($"{Square.SquareNumberToAlphaNumeric(move)}\n");
        //    }
        //}

        if (Input.GetKeyDown(KeyCode.Mouse0) && !flag)
        {
            print("clicked mouse");
            flag = true;
        }
        else if (Input.GetKeyUp(KeyCode.Mouse0) && flag)
        {
            print("let go of mouse");
            flag = false;
        }
    }
}
