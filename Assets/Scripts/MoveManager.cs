using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveManager : MonoBehaviour
{
    public static MoveManager Instance;

    public static Piece.PieceColor playerTurn = Piece.PieceColor.White;
    private static bool pieceSelected = false;
    private static Piece selectedPiece = null;
    private static List<int> selectedPieceLegalMoves = null;

    public static Square squareOfChecker = null;

    public static bool CastleAllowed { get; set; } = true;
    public static bool CheckAllowed { get; set; } = true;
    public static bool EnPassantAllowed { get; set; } = true;
    public static bool GameOver { get; set; } = false;

    [SerializeField] private LayerMask squaresLayer;
    [SerializeField] private GameObject audioSources;

    void Start()
    {
        if (Instance != null) Destroy(this);

        Instance = this;
    }

    void Update()
    {
        if (!GameOver)
        {
            CheckForPieceSelectionOrMove(playerTurn);
        }
    }

    public static void CheckForPieceSelectionOrMove(Piece.PieceColor color)
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            ResetSquareHighlighting();
            RaycastHit2D hit = Physics2D.Raycast(Utils.GetMouseWorldPosition(), Vector3.zero, 10, Instance.squaresLayer);
            
            if (hit.collider != null) //if we actually hit something
            {
                Square squareHit = hit.collider.GetComponent<Square>();

                if (squareHit.piece?.color == color)
                {
                    pieceSelected = true;
                    selectedPiece = squareHit.piece;
                    selectedPieceLegalMoves = selectedPiece.GetLegalMoves();

                    HighlightLegalSquares(squareHit.squareNumber, selectedPieceLegalMoves);
                    UI.UpdateLegalMovesDisplay(selectedPieceLegalMoves, selectedPiece.isPinned);
                }
                else if (pieceSelected) //checking for move play
                {
                    if (selectedPiece.GetLegalMoves().Contains(squareHit.squareNumber)) //seeing if the destination square is a legal square that the piece can move to
                    {
                        PlayMove(selectedPiece, squareHit);
                        playerTurn = Piece.GetOppositeColor(playerTurn);

                        #region
                        /*if (playerTurn == Piece.PieceColor.White)
                        {
                            playerTurn = Piece.PieceColor.Black;
                            WhiteKingUnderCheck = false; //since white played a move, they must now be out of check, if they were

                            if (CheckForStalemate(Piece.PieceColor.Black))
                            {
                                if (BlackKingUnderCheck) print("CHECKMATE, BLACK HAS NO LEGAL MOVES");
                                else print("STALEMATE, BLACK HAS NO LEGAL MOVES");
                            }
                        }
                        else
                        {
                            playerTurn = Piece.PieceColor.White;
                            BlackKingUnderCheck = false; //since black played a move, they must now be out of check, if they were

                            if (CheckForStalemate(Piece.PieceColor.White))
                            {
                                if (WhiteKingUnderCheck)
                                {
                                    print("CHECKMATE, WHITE HAS NO LEGAL MOVES");
                                }
                                else
                                {
                                    print("STALEMATE, WHITE HAS NO LEGAL MOVES");
                                }
                            }
                        }*/
                        #endregion
                    }

                    ResetSelection();
                }
                
            }
        }
        /*else if (Input.GetKey(KeyCode.Mouse0))
        {

        }*/
    }

    public static void PlayMove(Piece piece, Square targetSquare)
    {
        if (targetSquare.piece == null)
        {
            PlayMoveSound();
        }
        else
        {
            PlayCaptureSound();
            Destroy(targetSquare.piece.gameObj);
        }

        piece.Move(targetSquare);
    }

    public static void PlayMoveSound()
    {
        Instance.audioSources.transform.Find("Move").GetComponent<AudioSource>().Play();
    }
    public static void PlayCaptureSound()
    {
        Instance.audioSources.transform.Find("PieceCapture").GetComponent<AudioSource>().Play();
    }

    public static bool CheckForStalemate(Piece.PieceColor color)
    {
        return King.kingPieceUnderCheck is null && Piece.GetAllLegalMoves(color) is null;
    }

    //not really highlighting, just "drawing" a point over the square
    public static void HighlightLegalSquares(int selectedSquare, List<int> legalMoves)
    {
        Board.Squares[selectedSquare].Color = Square.SquareColor.Highlighted;

        if (legalMoves != null)
        {
            foreach (int move in legalMoves)
            {
                Board.Squares[move].transform.GetChild(0).gameObject.SetActive(true);
            }
        }
    }
    public static void ResetSquareHighlighting()
    {
        foreach (Square square in Board.Squares.Values)
        {
            square.Color = square.Color;
            square.transform.GetChild(0).gameObject.SetActive(false);
        }
    }
    public static void ResetSelection()
    {
        pieceSelected = false;
        selectedPiece = null;
        selectedPieceLegalMoves = null;
        ResetSquareHighlighting();
    }

    public static void ResetFields()
    {
        /*foreach (System.Reflection.FieldInfo fieldInfo in Instance.GetType().GetFields())
        {
            fieldInfo.SetValue(fieldInfo.FieldType, default(fieldInfo.FieldType));
        }*/

        playerTurn = Piece.PieceColor.White;
        pieceSelected = false;
        selectedPiece = null;
        selectedPieceLegalMoves = null;
        squareOfChecker = null;

        CastleAllowed = true;
        CheckAllowed = true;
        EnPassantAllowed = true;

        GameOver = false;
    }
}
