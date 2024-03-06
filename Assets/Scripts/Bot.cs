using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Bot
{
    public static Piece.PieceColor color;

    private static float botPlayDelay = 1f;
    private static float BotPlayDelay
    {
        get => botPlayDelay;
        set
        {
            if (value >= 0)
            {
                botPlayDelay = value;
            }
        }
    }

    private static bool botMovementInProgress = false;
    public static bool BotMovementInProgress
    {
        get => botMovementInProgress;
        set
        {
            botMovementInProgress = value;
        }
    }

    //currently doesn't actually get the best move, just a random one
    internal static void GetBestMove(out Piece pieceToMove, out Square targetSquare)
    {
        List<Piece> pieces = Board.GetPieces(color);

        while (pieces.Count > 0)
        {
            int pieceIndex = UnityEngine.Random.Range(0, pieces.Count);

            pieceToMove = pieces[pieceIndex];

            List<int> legalMoves = pieceToMove.GetLegalMoves();

            if (legalMoves.Count > 0)
            {
                int moveIndex = UnityEngine.Random.Range(0, legalMoves.Count);

                targetSquare = Board.Squares[legalMoves[moveIndex]];
                
                return;
            }

            //removing piece from list because it has no legal moves
            pieces.RemoveAt(pieceIndex);
        }

        pieceToMove = null;
        targetSquare = null;
    }

    internal static IEnumerator PlayBestMove()
    {
        GetBestMove(out Piece pieceToMove, out Square targetSquare);

        if (pieceToMove != null && targetSquare != null)
        {
            BotMovementInProgress = true;
            yield return new WaitForSeconds(BotPlayDelay);
            MoveManager.PlayMove(pieceToMove, targetSquare);
            BotMovementInProgress = false;
            MoveManager.PlayerTurn = Piece.GetOppositeColor(color);
        }
        else
        {
            throw new Exception();
        }
    }
}
