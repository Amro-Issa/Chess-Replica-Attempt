using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    enum Menu
    {
        Game,
        RandomPositionsSettings,
        DragAndDrop
    }

    public static UI Instance { get; private set; }

    [SerializeField] public GameObject canvas;
    public static string currentFenInputFieldValue;

    private static bool _isSettingsActive = true;
    public static bool IsSettingsActive
    {
        get
        {
            return _isSettingsActive;
        }
        set
        {   
            _isSettingsActive = value;
            Instance.SettingsGameObject.SetActive(value);
        }
    }
    
    private static bool _pawnPromotionInProgress = false;
    public static bool PawnPromotionInProgress
    {
        get
        {
            return _pawnPromotionInProgress;
        }
        set
        {
            if (value)
            {
                for(int i = 1; i < Instance.PawnPromotionGUI.transform.childCount; i++)
                {
                    GameObject selection = Instance.PawnPromotionGUI.transform.GetChild(i).gameObject;
                    Piece.PieceType type;
                    switch (selection.name)
                    {
                        case "Bishop":
                            type = Piece.PieceType.Bishop;
                            break;
                        case "Knight":
                            type = Piece.PieceType.Knight;
                            break;
                        case "Rook":
                            type = Piece.PieceType.Rook;
                            break;
                        case "Queen":
                            type = Piece.PieceType.Queen;
                            break;
                        default:
                            throw new Exception();
                    }
                    selection.GetComponent<Image>().sprite = Pawn.pawnToBePromoted.color == Piece.PieceColor.White ? Board.PieceTypeToSO[type].whitePieceSprite : Board.PieceTypeToSO[type].blackPieceSprite;
                }
            }
            else
            {
                Pawn.pawnToBePromoted = null;
            }

            _pawnPromotionInProgress = value;
            Instance.PawnPromotionGUI.SetActive(value);
            IsSettingsActive = !value;
        }
    }

    [SerializeField] private GameObject GameMenuObject;
    [SerializeField] private GameObject DragAndDropMenuObject;
    [SerializeField] private GameObject RandomPositionSettingsGameObject;

    [SerializeField] private GameObject SettingsGameObject;

    [SerializeField] private Text HelpText;

    [SerializeField] private Toggle[] RulesToggles;

    [SerializeField] private Text LegalMovesDisplay;

    [SerializeField] private GameObject PawnPromotionGUI;

    private const KeyCode randomPosMenuKey = KeyCode.Tab;
    private const KeyCode dragAndDropKey = KeyCode.X;
    private const KeyCode changeViewKey = KeyCode.Space;

    public Button ToggleTurnButton;
    public Text ToggleTurnText;

    [SerializeField] private GameObject boardSpritesParent;
    private readonly List<Menu> menusRequiringBoard = new List<Menu>{ Menu.Game, Menu.DragAndDrop };

    public Toggle autoChangeViewToggle;

    void Awake()
    {
        Instance = this;
        ToggleTurnButton.onClick.AddListener(() => MoveManager.PlayerTurn = MoveManager.PlayerTurn == Piece.PieceColor.White ? Piece.PieceColor.Black : Piece.PieceColor.White);

        autoChangeViewToggle.onValueChanged.AddListener((bool state) => MoveManager.AutoChangeView = state);

        HelpText.text = $"- Press {Board.clearBoardKey} to clear board" +
             $"\n- Press {Board.defaultBoardKey} to generate default starting position" +
             $"\n- Press {Board.randomBoardKey} to generate random starting position" +
             "\n- To generate a specific position, enter the FEN in the input field above" +
             $"\n- Press {randomPosMenuKey} to adjust which pieces are included in the random generation" +
             $"\n- Press {dragAndDropKey} to open the drag and drop menu" +
             $"\n- Press {changeViewKey} to change board view";
    }

    void Update()
    {
        if (!PawnPromotionInProgress)
        {
            if (Input.GetKeyDown(randomPosMenuKey))
            {
                OpenMenu(RandomPositionSettingsGameObject.activeInHierarchy ? Menu.Game : Menu.RandomPositionsSettings);
            }
            else if (Input.GetKeyDown(dragAndDropKey))
            {
                OpenMenu(DragAndDropMenuObject.activeInHierarchy ? Menu.Game : Menu.DragAndDrop);
            }
            else if (Input.GetKeyDown(changeViewKey))
            {
                ChangeBoardView();
            }
        }
    }

    public void TogglePiece(PieceTypeSO pieceTypeSO)
    {
        if (Board.RandomGenerationExclusions.Contains(pieceTypeSO.pieceType))
        {
            Board.RandomGenerationExclusions.Remove(pieceTypeSO.pieceType);
        }
        else
        {
            Board.RandomGenerationExclusions.Add(pieceTypeSO.pieceType);
        }
    }

    public void UpdateFen(string newFen)
    {
        currentFenInputFieldValue = newFen;
    }

    public void GenerateFenButtonMethod()
    {
        if (currentFenInputFieldValue != null && Board.IsFenValid(currentFenInputFieldValue))
        {
            MoveManager.ResetSelection();
            Board.CreatePositionFromFen(currentFenInputFieldValue);
        }
        else
        {
            print("FEN is invalid! Change it.");
        }        
    }

    /// <summary>
    /// Updates the game settings checkboxes (like allowCheck, allowEnPassant, and allowCastling)
    /// </summary>
    public void UpdateGameSettings()
    {
        MoveManager.CastleAllowed = RulesToggles[0].isOn;
        MoveManager.CheckAllowed = RulesToggles[1].isOn;
        MoveManager.EnPassantAllowed = RulesToggles[2].isOn;
        MoveManager.PawnPromotionAllowed = RulesToggles[3].isOn;
    }

    public static void UpdateLegalMovesDisplay(List<int> legalMoves, bool isPinned, Piece piece)
    {
        string x = $"{Square.SquareNumberToAlphaNumeric(piece.square.SquareNumber)} {piece.type}\n";
        x += $"({legalMoves.Count} legal moves)\n";
        foreach (int move in legalMoves)
        {
            x += Square.SquareNumberToAlphaNumeric(move).ToUpper() + "\n";
        }
        x += $"\n This piece {(isPinned ? "is" : "is not")} pinned";
        Instance.LegalMovesDisplay.text = x;
    }

    public void SelectPawnPromotion(PieceTypeSO pieceTypeSO)
    {
        Pawn.pawnToBePromoted.Promote(pieceTypeSO.pieceType);
        PawnPromotionInProgress = false;
    }

    private GameObject GetMenuGameObject(Menu menu)
    {
        switch (menu)
        {
            case Menu.Game:
                return GameMenuObject;
            case Menu.RandomPositionsSettings:
                return RandomPositionSettingsGameObject;
            case Menu.DragAndDrop:
                return DragAndDropMenuObject;
            default:
                throw new Exception();
        }
    }

    private void OpenMenu(Menu menu)
    {
        foreach (Menu m in Enum.GetValues(typeof(Menu)))
        {
            GetMenuGameObject(m).SetActive(m == menu ? true : false);
        }

        bool requiresBoard = menusRequiringBoard.Contains(menu);
        boardSpritesParent.SetActive(requiresBoard);
    }

    public static void ChangeBoardView()
    {
        foreach (List<Piece> lst in new List<Piece>[] { Board.WhitePieces, Board.BlackPieces })
        {
            foreach (Piece piece in lst)
            {
                piece.gameObj.transform.Rotate(Vector3.forward * 180);
            }
        }

        Camera.main.transform.Rotate(Vector3.forward * 180);
    }

    public static void ChangeBoardView(Piece.PieceColor color)
    {
        Camera.main.transform.rotation = Quaternion.identity; //resets camera rotation

        foreach (List<Piece> lst in new List<Piece>[] { Board.WhitePieces, Board.BlackPieces })
        {
            foreach (Piece piece in lst)
            {
                piece.gameObj.transform.rotation = Quaternion.identity;
            }
        }

        //rotate camera if black, otherwise, do nothing because it is already on white
        if (color == Piece.PieceColor.Black)
        {
            ChangeBoardView();
        }
    }

    public void Reset()
    {
        for (int i = 0; i < Instance.RulesToggles.Length; i++)
        {
            Instance.RulesToggles[i].isOn = true;
        }
    }
}
