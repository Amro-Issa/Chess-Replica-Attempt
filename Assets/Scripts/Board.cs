using System;
using System.Collections.Generic;
using UnityEngine;


public class Board : MonoBehaviour
{
    public enum GameState
    {
        Bot,
        Local,
        Dev
    }

    public static Board Instance { get; private set; }

    [SerializeField] private GameObject squarePrefab, squaresParent;
    public GameObject whitePiecesParent, blackPiecesParent;

    public Color lightSquareColor, darkSquareColor, selectionSquareColor;

    public PieceTypeSO[] pieceTypeSOArray = new PieceTypeSO[5]; //ORDER MUST BE: pawn,knight,bishop,rook,queen,king

    public static Dictionary<Piece.PieceType, PieceTypeSO> PieceTypeToSO = new Dictionary<Piece.PieceType, PieceTypeSO>();
    public static Dictionary<int, Square> Squares = new Dictionary<int, Square>();

    public const string STARTING_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"; //in case you lose the string: rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR

    private const int PLAYER_COUNT = 2;
    
    public const int FILE_COUNT = 8;
    public const int RANK_COUNT = 8;
    private const int SQUARE_COUNT = FILE_COUNT * RANK_COUNT;

    public const int MAX_FILE = FILE_COUNT - 1;
    public const int MAX_RANK = RANK_COUNT - 1;
    public const int MAX_SQUARE = SQUARE_COUNT - 1;

    private const string PIECE_LETTERS = "pnbrqk"; //note: convert this to a dictionary with Piece.PieceType as they key type so that it is safer and cleaner

    public static HashSet<Piece.PieceType> RandomGenerationExclusions = new HashSet<Piece.PieceType>();

    public static List<Piece> WhitePieces
    {
        get
        {
            return GetPieces(Piece.PieceColor.White);
        }
    }
    public static List<Piece> BlackPieces
    {
        get
        {
            return GetPieces(Piece.PieceColor.Black);
        }
    }

    public const KeyCode clearBoardKey = KeyCode.C;
    public const KeyCode defaultBoardKey = KeyCode.D;
    public const KeyCode randomBoardKey = KeyCode.R;

    public static GameState gameState;


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
            PieceTypeToSO.Add(pieceTypeSOArray[i].pieceType, pieceTypeSOArray[i]);
        }

        CreateBoard();
        CreatePositionFromFen(STARTING_FEN);
    }

    void Update()
    {
        if (gameState == GameState.Dev && UI.IsSettingsActive)
        {
            if (Input.GetKeyDown(clearBoardKey))
            {
                ClearBoard();
            }
            else if (Input.GetKeyDown(defaultBoardKey)) //starting pos
            {
                CreatePositionFromFen(STARTING_FEN);
            }
            else if (Input.GetKeyDown(randomBoardKey)) //random pos
            {
                CreatePositionFromFen(GenerateRandomStartingFen(RandomGenerationExclusions));
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
    }

    private static void CreateSquare(Vector2 position, Square.SquareColor color, int squareNumber)
    {
        GameObject squareObject = Instantiate(Instance.squarePrefab, position, Quaternion.identity, Instance.squaresParent.transform);
        
        squareObject.name = squareNumber.ToString();
        squareObject.GetComponent<SpriteRenderer>().color = color == Square.SquareColor.Light ? Instance.lightSquareColor : Instance.darkSquareColor;

        Square squareClass = squareObject.GetComponent<Square>();
        squareClass.SquareNumber = squareNumber;
        squareClass.Color = color;

        Squares.Add(squareNumber, squareClass);
    }
    
    private static void CreateBoard()
    {
        if (Squares.Count != 0)
        {
            Squares.Clear();
        }

        for (int rank = 0; rank < RANK_COUNT; rank++) //file = column
        {
            for (int column = 0; column < FILE_COUNT; column++) //rank = row
            {
                Vector2 spawnPos = new Vector2(column, rank);
                Square.SquareColor squareColor = (column + rank) % 2 == 0 ? Square.SquareColor.Dark : Square.SquareColor.Light; //if (column + rank) is even, that means that square is black, otherwise it is white
                int squareNumber = (rank * FILE_COUNT) + column;
                
                CreateSquare(spawnPos, squareColor, squareNumber);
            }
        }
    }

    private static void ClearBoard()
    {
        MoveManager.ResetSelection();
        MoveManager.ResetFields();
        UI.Instance?.Reset();

        foreach (Square square in Squares.Values)
        {
            if (square.piece?.gameObj != null)
            {
                Destroy(square.piece.gameObj);
            }

            square.Unoccupy();
        }
    }

    public static void CreatePositionFromFen(string fen)
    {
        //method assumes FEN is valid
        ClearBoard();

        //fen starts from square 56 for a 8x8 board
        int currentSquareNumber = SQUARE_COUNT - FILE_COUNT;

        foreach(char character in fen)
        {
            if (character == '/')
            {
                //next rank
                currentSquareNumber -= FILE_COUNT * 2;
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
                    case Piece.PieceType.King:
                        new King(color, square);
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
                    default:
                        throw new Exception();
                }
                
                currentSquareNumber++;
            }
        }

        //resetting the board view (was causing some problems)
        UI.ChangeBoardView(Piece.PieceColor.White);
    }
    private static string GenerateRandomStartingFen()
    {
        string fen = "";

        for (int i = 0; i < 2; i++) //looping twice, once for each color
        {
            bool kingGenerated = false;

            for (int j = 0; j < FILE_COUNT * 2; j++) //looping the number of pieces to put on the board (16 times for a board with 8 files)
            {
                if (j == FILE_COUNT)
                {
                    //next row
                    fen += '/';
                }

                //excluding king piece if one has already been generated
                int randomIndex = kingGenerated ? UnityEngine.Random.Range(0, PIECE_LETTERS.Length - 1) : UnityEngine.Random.Range(0, PIECE_LETTERS.Length);

                if (PIECE_LETTERS[randomIndex] == 'k') kingGenerated = true;

                char character = i == 0 ? PIECE_LETTERS[randomIndex] : char.ToUpper(PIECE_LETTERS[randomIndex]);
                fen += character;
            }

            //setting up generation location of other color
            if (i == 0)
            {
                for (int k = 0; k < 4; k++)
                {
                    fen += $"/{FILE_COUNT}";
                }
                fen += "/";
            }
        }

        //print("The random fen produced is: " + fen);
        return fen;
    }
    private static string GenerateRandomStartingFen(HashSet<Piece.PieceType> exclusions)
    {
        string pieceCharacters = "";

        foreach(char character in PIECE_LETTERS)
        {
            if (!exclusions.Contains(Piece.GetPieceType(character)))
            {
                pieceCharacters += character;
            }    
        }

        string fen = "";
        for (int i = 0; i < 2; i++) //looping twice, once for each color
        {
            bool kingGenerated = false;

            for (int j = 0; j < FILE_COUNT * 2; j++) //looping the number of pieces to put on the board (16 times for a board with 8 files)
            {
                if (j == FILE_COUNT)
                {
                    //next row
                    fen += '/';
                }

                //excluding king piece if one has already been generated
                int randomIndex = kingGenerated ? UnityEngine.Random.Range(0, pieceCharacters.Length - 1) : UnityEngine.Random.Range(0, pieceCharacters.Length);

                if (pieceCharacters[randomIndex] == 'k')
                {
                    kingGenerated = true;
                }

                char character = i == 0 ? pieceCharacters[randomIndex] : char.ToUpper(pieceCharacters[randomIndex]);
                fen += character;
            }

            //setting up generation location of other color
            if (i == 0)
            {
                for (int k = 0; k < 4; k++)
                {
                    fen += $"/{FILE_COUNT}";
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
            if ((counter >= FILE_COUNT && character != '/') || (counter < FILE_COUNT && character == '/'))
            {
                return false;
            }

            if (!PIECE_LETTERS.Contains(character.ToString().ToLower()))
            {
                if (!int.TryParse(character.ToString(), out int parsedCharacter) || parsedCharacter <= 0 || parsedCharacter > FILE_COUNT) //checks if character is an integer, if it is, checks if it is between 0 (inclusive) and filecount, and if it's not, it's invalid
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

        return overallCounter == SQUARE_COUNT;
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

        throw new Exception($"UNABLE TO FIND PIECE {type}");
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