using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public static void PrintMoves(List<int> moves)
    {
        List<string> array = new List<string>();
        foreach (int move in moves)
        {
            array.Add(Square.SquareNumberToAlphaNumeric(move));
        }

        print($"[{string.Join(", ", array)}]");
    }
}
