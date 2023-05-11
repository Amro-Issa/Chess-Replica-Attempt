using System;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public static Board Instance;

    [SerializeField] private GameObject squarePrefab, squaresParent;
    public GameObject whitePiecesParent, blackPiecesParent;
    public GameObject spritesParent;

    public Color lightSquareColor, darkSquareColor, selectionSquareColor;

    public PieceTypeSO[] pieceTypeSOArray = new PieceTypeSO[5]; //ORDER MUST BE: pawn,knight,bishop,rook,queen,king

    public static Dictionary<Piece.PieceType, PieceTypeSO> PieceTypeToSO = new Dictionary<Piece.PieceType, PieceTypeSO>();
    public static Dictionary<int, Square> Squares = new Dictionary<int, Square>();

    public const string StartingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"; //in case you lose the string: rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR

    private const int PlayerCount = 2;
    
    public const int FileCount = 8;
    public const int RankCount = 8;
    private const int SquareCount = FileCount * RankCount;

    public const int MaxFile = FileCount - 1;
    public const int MaxRank = RankCount - 1;
    public const int MaxSquare = SquareCount - 1;
    private const string PieceLetters = "pnbrqk";

    private static List<Piece> WhitePieces
    {
        get
        {
            return GetPieces(Piece.PieceColor.White);
        }
    }
    private static List<Piece> BlackPieces
    {
        get
        {
            return GetPieces(Piece.PieceColor.Black);
        }
    }


    void Start()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }


        for (int i = 0; i < Enum.GetNames(typeof(Piece.PieceType)).Length; i++)
        {
            PieceTypeToSO.Add((Piece.PieceType)i, pieceTypeSOArray[i]);
        }

        CreateBoard();
        CreatePositionFromFen(StartingFen);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearBoard();
        }
        else if (Input.GetKeyDown(KeyCode.D)) //starting pos
        {
            CreatePositionFromFen(StartingFen);
        }
        else if (Input.GetKeyDown(KeyCode.R)) //random pos
        {
            CreatePositionFromFen(GenerateRandomStartingFen());
        }
        #region delete?
        /*else if (Input.GetKeyDown(KeyCode.E)) //Generate with the specified exceptions
        {
            ClearBoard();

            int counter = 0;
            string fen;

            List<char> validCharacters = UI.GetToggledPieces();

            OUTER:
            while (true)
            {
                counter++;

                fen = GenerateRandomStartingFen();

                foreach (char character in fen)
                {
                    if (!validCharacters.Contains(character) && character != '/' && character != '8')
                    {
                        if (counter == 1000000)
                        {
                            print(counter + " tries and the position still hasn't been reached! Incredible!");
                            goto EndOfLoop;
                        }

                        goto OUTER;
                    }
                }

                CreatePositionFromFen(fen);
                print("It took " + counter + " tries to get to this position");
                break;
            }
            EndOfLoop:;
        }*/
        #endregion
    }

    private static void CreateSquare(Vector2 position, Color squareColor, int squareNumber)
    {
        GameObject squareObject = Instantiate(Instance.squarePrefab, position, Quaternion.identity, Instance.squaresParent.transform);

        squareObject.name = squareNumber.ToString();
        squareObject.GetComponent<SpriteRenderer>().color = squareColor;

        Square squareClass = squareObject.GetComponent<Square>();
        squareClass.squareNumber = squareNumber;
        squareClass.color = squareColor;

        Squares.Add(squareNumber, squareClass);
    }
    
    private static void CreateBoard()
    {
        if (Squares.Count != 0)
        {
            Squares.Clear();
        }

        for (int rank = 0; rank < RankCount; rank++) //file = column
        {
            for (int column = 0; column < FileCount; column++) //rank = row
            {
                Vector2 spawnPos = new Vector2(column, rank);
                Color squareColor = (column + rank) % 2 == 0 ? Instance.darkSquareColor : Instance.lightSquareColor; //if (column + rank) is even, that means that square is black, otherwise it is white
                int squareNumber = (rank * FileCount) + column;
                
                CreateSquare(spawnPos, squareColor, squareNumber);
            }
        }
    }

    private static void ClearBoard()
    {
        MoveManager.ResetSelection();
        MoveManager.ResetFields();
        UI.Instance?.Reset();

        foreach (Square squareClass in Squares.Values)
        {
            if (squareClass.piece?.gameObj != null)
            {
                Destroy(squareClass.piece.gameObj);
            }

            squareClass.Unoccupy();
        }
    }

    public static void CreatePositionFromFen(string fen)
    {
        ClearBoard();

        MoveManager.playerTurn = Piece.PieceColor.White;

        //fen starts from square 56 for a 8x8 board
        int currentSquareNumber = SquareCount - FileCount;

        foreach(char character in fen)
        {
            if (character == '/')
            {
                //next rank
                currentSquareNumber -= FileCount * 2;
            }
            else if (int.TryParse(character.ToString(), out int number))
            {
                currentSquareNumber += number;
            }
            else
            {
                Piece.PieceColor color = char.IsUpper(character) ? Piece.PieceColor.White : Piece.PieceColor.Black; //white is uppercase, black is lowercase
                Square square = Squares[currentSquareNumber];

                switch (Piece.GetPieceType(char.ToLower(character)))
                { 
                    case Piece.PieceType.Pawn:
                        new Pawn(color, square);
                        break;
                    case Piece.PieceType.Knight:
                        new Knight(color, square);
                        break;
                    case Piece.PieceType.Bishop:
                        new Bishop(color, square);
                        break;
                    case Piece.PieceType.Rook:
                        new Rook(color, square);
                        break;
                    case Piece.PieceType.Queen:
                        new Queen(color, square);
                        break;
                    case Piece.PieceType.King:
                        new King(color, square);
                        break;
                    default:
                        throw new Exception();
                }
                
                currentSquareNumber++;
            }
        }
    }
    private static string GenerateRandomStartingFen()
    {
        string fen = "";

        for (int i = 0; i < 2; i++) //looping twice, once for each color
        {
            bool kingGenerated = false;

            for (int j = 0; j < FileCount * 2; j++) //looping the number of pieces to put on the board (16 times for a board with 8 files)
            {
                if (j == FileCount)
                {
                    //next row
                    fen += '/';
                }

                //excluding king piece if one has already been generated
                int randomIndex = kingGenerated ? UnityEngine.Random.Range(0, PieceLetters.Length - 1) : UnityEngine.Random.Range(0, PieceLetters.Length);

                if (PieceLetters[randomIndex] == 'k') kingGenerated = true;

                char character = i == 0 ? PieceLetters[randomIndex] : char.ToUpper(PieceLetters[randomIndex]);
                fen += character;
            }

            //setting up generation location of other color
            if (i == 0)
            {
                for (int k = 0; k < 4; k++)
                {
                    fen += $"/{FileCount}";
                }
                fen += "/";
            }
        }

        //print("The random fen produced is: " + fen);
        return fen;
    }
    public static bool IsFenValid(string fen)
    {
        int overallCounter = 0;
        int counter = 0;
        
        foreach (char character in fen)
        {
            if ((counter >= FileCount && character != '/') || (counter < FileCount && character == '/'))
            {
                return false;
            }

            if (!PieceLetters.Contains(character.ToString().ToLower()))
            {
                if (!int.TryParse(character.ToString(), out int parsedCharacter) || parsedCharacter <= 0 || parsedCharacter > FileCount) //checks if character is an integer, if it is, checks if it is between 0 (inclusive) and filecount, and if it's not, it's invalid
                {
                    if (character != '/')
                    {
                        return false;
                    }
                    else
                    {
                        counter = 0;
                    }
                }
                else
                {
                    counter += parsedCharacter;
                    overallCounter += parsedCharacter;
                }
            }
            else
            {
                counter++;
                overallCounter++;
            }
        }

        return overallCounter == SquareCount;
    }

    public static bool IsSquareOccupied(int squareNum)
    {
        return Squares[squareNum].piece != null;
    }
    public static bool IsSquareOccupied(int squareNum, Piece.PieceColor occupantColor)
    {
        return Squares[squareNum].piece?.color == occupantColor;
    }

    /// <summary>
    /// Gets the first instance of the passed in piece type
    /// </summary>
    /// <returns></returns>
    public static Piece GetPiece(Piece.PieceColor color, Piece.PieceType type)
    {
        foreach(Square square in Squares.Values)
        {
            if (square.piece?.type == type && square.piece?.color == color)
            {
                return square.piece;
            }
        }

        return null;
    }
    /// <summary>
    /// Tries to get the first instance of the passed in piece type, if it couldn't, returns false
    /// </summary>
    /// <returns></returns>
    public static bool TryGetPiece(Piece.PieceColor color, Piece.PieceType type, out Piece piece)
    {
        foreach (Square square in Squares.Values)
        {
            if (square.piece.type == type && square.piece.color == color)
            {
                piece = square.piece;
                return true;
            }
        }

        piece = null;
        return false;
    }
    public static King GetKing(Piece.PieceColor color)
    {
        return (King)GetPiece(color, Piece.PieceType.King);
    }
    public static List<Piece> GetPieces(Piece.PieceColor? color = null, Piece.PieceType? type = null)
    {
        var pieces = new List<Piece>();

        foreach (Square square in Squares.Values)
        {
            if ((color == null || square.piece?.color == color) && (type == null || square.piece?.type == type))
            {
                pieces.Add(square.piece);
            }
        }

        return pieces;
    }
}
