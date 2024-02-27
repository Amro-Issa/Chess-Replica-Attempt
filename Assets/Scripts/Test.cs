using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    static bool flag = false;
    static Square highlightedSquare;

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

        //if (Input.GetKeyDown(KeyCode.Mouse0) && !flag)
        //{
        //    print("clicked mouse");
        //    flag = true;
        //}
        //else if (Input.GetKeyUp(KeyCode.Mouse0) && flag)
        //{
        //    print("let go of mouse");
        //    flag = false;
        //}
    }

    private void Test1()
    {
        //highlights boundary square of selected square with given direction
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (highlightedSquare != null)
            {
                highlightedSquare.gameObject.GetComponent<SpriteRenderer>().color = highlightedSquare._physicalColor;
            }

            RaycastHit2D hit = Physics2D.Raycast(Utils.GetMouseWorldPosition(), Vector3.zero, 10, MoveManager.Instance.squaresLayer);

            if (hit.collider != null) //if we actually hit something
            {
                Square squareHit = hit.collider.GetComponent<Square>();

                int boundary = Square.GetBoundarySquare(squareHit.SquareNumber, Square.Direction.BottomRight);

                highlightedSquare = Board.Squares[boundary];

                highlightedSquare.gameObject.GetComponent<SpriteRenderer>().color = Color.cyan;
            }
        }
    }
}
