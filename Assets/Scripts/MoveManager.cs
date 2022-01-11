using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveManager : MonoBehaviour
{
    public static MoveManager Instance;

    public static Piece.PieceColor playerTurn = Piece.PieceColor.White;
    private static bool pieceSelected = false;
    private static int? squareNumberOfSelectedPiece = null;
    private static List<int> currentLegalMoves = null;

    public static bool WhiteKingUnderCheck { get => whiteKingUnderCheck; set { whiteKingUnderCheck = CheckAllowed ? value : false; } }
    private static bool whiteKingUnderCheck = false;

    public static bool BlackKingUnderCheck { get => blackKingUnderCheck; set { blackKingUnderCheck = CheckAllowed ? value : false; } }
    private static bool blackKingUnderCheck = false;

    private static Square squareOfChecker = null;

    public static bool CastleAllowed { get; set; } = true;
    public static bool CheckAllowed { get; set; } = true;
    public static bool EnPassantAllowed { get; set; } = true;

    public static Square kingSideKingCastleSquare = null;
    public static Square kingSideRookCastleSquare = null;

    public static Square queenSideKingCastleSquare = null;
    public static Square queenSideRookCastleSquare = null;

    private static int? enPassantSquareNumber = null;

    [SerializeField] private LayerMask squaresLayer;
    [SerializeField] private GameObject audioSources;

    void Start()
    {
        if (Instance != null)
        {
            Destroy(this);
        }

        Instance = this;
    }

    void Update()
    {
        if (!pieceSelected)
        {
            CheckForPieceSelection(playerTurn);
        }
        else
        {
            CheckForMove();
        }
    }

    public static void CheckForPieceSelection(Piece.PieceColor color)
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            ResetSquareHighlighting();

            RaycastHit2D hit = Physics2D.Raycast(Utils.GetMouseWorldPosition(), Vector3.zero, 10, Instance.squaresLayer);

            if (hit.collider != null)
            {
                Square square = hit.collider.GetComponent<Square>();
                
                if (square.piece.color == color)
                {
                    pieceSelected = true;
                    squareNumberOfSelectedPiece = square.squareNumber;

                    currentLegalMoves = CalculateLegalMoves(square.piece);

                    if (WhiteKingUnderCheck || BlackKingUnderCheck)
                    {
                        AdjustLegalMovesToDealWithCheck(ref currentLegalMoves, square.piece.type, square.piece.color);
                    }

                    HighlightLegalSquares(square.squareNumber, currentLegalMoves);
                }
            }
        }
    }

    public static void CheckForMove()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Utils.GetMouseWorldPosition(), Vector3.zero, 10, Instance.squaresLayer);

            if (hit.collider != null)
            {
                Square destinationSquare = hit.collider.GetComponent<Square>();

                if (currentLegalMoves.Contains(destinationSquare.squareNumber)) //seeing if the destination square is a legal square that the piece can move to
                {
                    PlayMove(Board.SquareNumberToSquare[(int)squareNumberOfSelectedPiece].piece, destinationSquare);

                    if (playerTurn == Piece.PieceColor.White)
                    {
                        WhiteKingUnderCheck = false; //since white played a move, they must now be out of check, if they were
                        playerTurn = Piece.PieceColor.Black;

                        if (CheckForStalemate(Piece.PieceColor.Black))
                        {
                            if (BlackKingUnderCheck)
                            {
                                print("CHECKMATE, BLACK HAS NO LEGAL MOVES");
                            }
                            else
                            {
                                print("STALEMATE, BLACK HAS NO LEGAL MOVES");
                            }
                        }
                    }
                    else
                    {
                        BlackKingUnderCheck = false; //since black played a move, they must now be out of check, if they were
                        playerTurn = Piece.PieceColor.White;

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
                    }
                }
            }

            ResetSquareHighlighting();
            pieceSelected = false;
            squareNumberOfSelectedPiece = null;
        }
    }

    public static void PlayMove(Piece piece, Square targetSquare)
    {
        bool moveWasACapture = false;
        GameObject pieceToDestroy = null;
        bool moveWasCastling = false;

        if (targetSquare.piece.type == Piece.PieceType.None)
        {
            //case of castling
            if (piece.type == Piece.PieceType.King && Square.GetFileDifference(piece.square.squareNumber, targetSquare.squareNumber) > 1)
            {
                if (piece.square.squareNumber > targetSquare.squareNumber) //queen side
                {
                    Castle(piece, Board.SquareNumberToSquare[piece.square.squareNumber - 4].piece, CastleType.QueenSide);
                }
                else if(piece.square.squareNumber < targetSquare.squareNumber) //king side
                {
                    Castle(piece, Board.SquareNumberToSquare[piece.square.squareNumber + 3].piece, CastleType.KingSide);
                }

                moveWasCastling = true;
            }
            //case of en passant
            else if (EnPassantAllowed && piece.type == Piece.PieceType.Pawn && targetSquare.squareNumber == enPassantSquareNumber)
            {
                Square actualSquareOfTarget = null;

                if (piece.color == Piece.PieceColor.White)
                {
                    actualSquareOfTarget = Board.SquareNumberToSquare[targetSquare.squareNumber - Board.fileCount];
                }
                else if (piece.color == Piece.PieceColor.Black)
                {
                    actualSquareOfTarget = Board.SquareNumberToSquare[targetSquare.squareNumber + Board.fileCount];
                }

                pieceToDestroy = actualSquareOfTarget.piece.gameObj;
                actualSquareOfTarget.Unoccupy();
                moveWasACapture = true;
            }
        }
        else
        {
            moveWasACapture = true;
            pieceToDestroy = targetSquare.piece.gameObj;
        }

        if (moveWasACapture)
        {
            Destroy(pieceToDestroy);
            //play capture sound
            Instance.audioSources.transform.Find("PieceCapture").GetComponent<AudioSource>().Play();
        }
        else
        {
            //play move sound
            Instance.audioSources.transform.Find("Move").GetComponent<AudioSource>().Play();
        }

        if (!moveWasCastling) piece.Move(targetSquare);

        #region Check
        //checking for check on the opposite color king
        List<int> legalMovesOfPieceOnNewSquare = CalculateLegalMoves(piece);

        Square enemyKingSquare = (Piece.GetOppositeColor(piece.color) == Piece.PieceColor.White ? Board.WhiteKing : Board.BlackKing).square;

        if (CheckAllowed && enemyKingSquare != null && legalMovesOfPieceOnNewSquare.Contains(enemyKingSquare.squareNumber))
        {
            if (enemyKingSquare.piece.color == Piece.PieceColor.Black)
            {
                BlackKingUnderCheck = true;
                print("Black king is under check");
            }
            else if (enemyKingSquare.piece.color == Piece.PieceColor.White)
            {
                WhiteKingUnderCheck = true;
                print("White king is under check");
            }

            squareOfChecker = targetSquare;
        }
        #endregion
    }

    public static void PlayMoveSound()
    {
        Instance.audioSources.transform.Find("Move").GetComponent<AudioSource>().Play();
    }

    public static void PlayCaptureSound()
    {
        Instance.audioSources.transform.Find("PieceCapture").GetComponent<AudioSource>().Play();
    }

    public static void AdjustLegalMovesToDealWithCheck(ref List<int> legalMoves, Piece.PieceType type, Piece.PieceColor color)
    {
        var newLegalMoves = new List<int>();

        int kingSquareNumber = (color == Piece.PieceColor.White ? Board.WhiteKing : Board.BlackKing).square.squareNumber;

        if (type == Piece.PieceType.King) //since piece is king, apart from capturing, king can only move to safe square (obv cannot block check with himself)
        {
            var allDefendedSquaresOfOppositeColor = GetAllDefendedSquares(Piece.GetOppositeColor(color));

            foreach (int move in legalMoves)
            {
                if (!allDefendedSquaresOfOppositeColor.Contains(move) && !Square.AreSquaresReachable(squareOfChecker.squareNumber, move)) //checking that the king actually MOVED out of the line of attack of the long-distance checker (queen, rook, bishop)
                {
                    newLegalMoves.Add(move);
                }
            }
        }
        //checking for blocking moves
        else if (Square.TryGetSquaresInBetween(squareOfChecker.squareNumber, kingSquareNumber, out List<int> squaresInBetween))
        {
            foreach (int move in squaresInBetween)
            {
                if (legalMoves.Contains(move)) //if the legal moves contain that specific move that blocks the check
                {
                    newLegalMoves.Add(move);
                }
            }
        }

        //checking for a capture move
        if (legalMoves.Contains(squareOfChecker.squareNumber)) //if move is capturing the checker
        {
            newLegalMoves.Add(squareOfChecker.squareNumber);
        }

        legalMoves = newLegalMoves;
    }

    public static void AdjustLegalMovesForKing(List<int> legalMoves, Piece.PieceColor color)
    {
        var allDefendedSquaresOfOppositeColor = GetAllDefendedSquares(Piece.GetOppositeColor(color));

        foreach (int move in legalMoves.ToArray())
        {
            if (allDefendedSquaresOfOppositeColor.Contains(move))
            {
                legalMoves.Remove(move);
            }
        }
    }

    public static List<int> CalculateLegalMoves(Piece piece)
    { 
        AdjustLegalMovesForPinning(piece, ref moves);

        return moves;
    }

    public static List<int> GetAllLegalMoves(Piece.PieceColor color)
    {
        List<int> allLegalMoves = new List<int>();

        foreach(Square square in Board.SquareNumberToSquare.Values)
        {
            if (square.piece.color == color)
            {
                var pieceLegalMoves = CalculateLegalMoves(square.piece);

                if (square.piece.type == Piece.PieceType.King)
                {
                    AdjustLegalMovesForKing(pieceLegalMoves, color);
                }

                allLegalMoves.AddRange(pieceLegalMoves);
            }
        }

        return allLegalMoves;
    }

    public static bool CheckForStalemate(Piece.PieceColor color)
    {
        return GetAllLegalMoves(color).Count == 0;
    }

    public enum CastleType
    {
        KingSide,
        QueenSide
    }

    public static void Castle(Piece king, Piece rook, CastleType castleType)
    {
        int newKingSquareNumber;
        int newRookSquareNumber;

        if (castleType == CastleType.KingSide)
        {
            newKingSquareNumber = king.square.squareNumber + 2;
            newRookSquareNumber = rook.square.squareNumber - 2;
        }
        else
        {
            newKingSquareNumber = king.square.squareNumber - 2;
            newRookSquareNumber = rook.square.squareNumber + 3;
        }

        king.Move(Board.SquareNumberToSquare[newKingSquareNumber]);
        rook.Move(Board.SquareNumberToSquare[newRookSquareNumber]);

        //PLAY CASTLE SOUND EFFECT
    }

    public static bool IsCastlingAvailable(Piece.PieceColor color, Piece rook, out CastleType castleType, out Square rookCastleSquare, out Square kingCastleSquare, Piece king = null)
    {
        if (king == null) king = color == Piece.PieceColor.White ? Board.WhiteKing : Board.BlackKing;

        castleType = rook.square.squareNumber < king.square.squareNumber ? CastleType.QueenSide : CastleType.KingSide;

        rookCastleSquare = null;
        kingCastleSquare = null;

        if (!king.hasMoved && !rook.hasMoved)
        {
            int mult = castleType == CastleType.QueenSide ? 1 : 0;

            if (Square.AreSquaresInBetweenEmpty(king.square.squareNumber, rook.square.squareNumber + mult) /*&& todo*/) //FOR NOW KEEP BUT LATER...CHANGE TO GET THE SQUARES BETWEEN THE KING AND THE SQUARE 3 SQUARES BESIDE IT (NOT FROM KING TO ROOK) SO THAT YOU CAN STILL CASTLE DESPITE IF THERE IS A PIECE BLOCKING THE ROOKS PATH BUT NOT THE KING's
            {
                //check if path is guarded by enemy piece(s)
                if (Square.AreSquaresGuarded(Piece.GetOppositeColor(color), Square.GetSquaresInBetween(king.square.squareNumber, rook.square.squareNumber + mult)))
                {
                    return false;
                }

                //Queen side castle                                         //King side castle
                kingCastleSquare = king.square.squareNumber > rook.square.squareNumber ? Board.SquareNumberToSquare[king.square.squareNumber - 2] : Board.SquareNumberToSquare[king.square.squareNumber + 2];
                rookCastleSquare = king.square.squareNumber > rook.square.squareNumber ? Board.SquareNumberToSquare[king.square.squareNumber + 3] : Board.SquareNumberToSquare[king.square.squareNumber - 2];

                return true;
            }
        }

        return false;
    }

    //not really highlighting, just "drawing" a point over the square
    public static void HighlightLegalSquares(int currentSquareNum, List<int> legalMoves)
    {
        Board.SquareNumberToSquare[currentSquareNum].GetComponent<SpriteRenderer>().color = Board.Instance.selectColor;

        if (legalMoves != null)
        {
            foreach (int squareNumber in legalMoves)
            {
                Board.SquareNumberToSquare[squareNumber].transform.GetChild(0).gameObject.SetActive(true);
            }
        }
    }

    public static void AdjustLegalMovesForPinning(Piece piece, ref List<int> moves)
    {
        int pieceSquareNum = piece.square.squareNumber;

        if (Square.TryGetOffset(Board.GetKing(piece.color).square.squareNumber, pieceSquareNum, out int offset, out _)) //checking if the king and the piece are reachable and if so, getting the offset
        {
            Square.Directions direction = (Square.Directions)offset;

            if (Square.TryGetSquaresInBetween(pieceSquareNum, Square.GetBoundarySquare(pieceSquareNum, direction), out var squaresInBetween, true)) //getting the squares between the piece and the boundary reached by taking the same path of the offset from the king to the piece
            { 
                foreach (int square in squaresInBetween)
                {
                    Piece.PieceColor color = Board.SquareNumberToSquare[square].piece.color;
                    Piece.PieceType type = Board.SquareNumberToSquare[square].piece.type;

                    if (color == Piece.GetOppositeColor(piece.color))
                    {
                        if ((type == Piece.PieceType.Pawn || type == Piece.PieceType.King || type == Piece.PieceType.Knight) || (type == Piece.PieceType.Rook && Square.diagonalOffsets.Contains(direction)) || (type == Piece.PieceType.Bishop && Square.orthogonalOffsets.Contains(direction)))
                        {

                        }
                        else
                        {
                            foreach (int move in moves.ToArray())
                            {
                                if (!squaresInBetween.Contains(move)) moves.Remove(move);
                            }
                        }

                        return;
                    }
                    else if (color == piece.color)
                    {
                        return;
                    }
                }
            }
        }
    }

    public static void ResetSquareHighlighting()
    {
        foreach (Square square in Board.SquareNumberToSquare.Values)
        {
            square.GetComponent<SpriteRenderer>().color = square.color;
            square.transform.GetChild(0).gameObject.SetActive(false);
        }
    }

    public static void ResetFields()
    {
        /*foreach (System.Reflection.FieldInfo fieldInfo in Instance.GetType().GetFields())
        {
            fieldInfo.SetValue(fieldInfo.FieldType, default(fieldInfo.FieldType));
        }*/

        playerTurn = Piece.PieceColor.White;
        pieceSelected = false;
        squareNumberOfSelectedPiece = null;
        currentLegalMoves = null;

        WhiteKingUnderCheck = false;
        BlackKingUnderCheck = false;
        squareOfChecker = null;

        enPassantSquareNumber = null;

        kingSideKingCastleSquare = null;
        kingSideRookCastleSquare = null;
        queenSideKingCastleSquare = null;
        queenSideRookCastleSquare = null;
    }
}
