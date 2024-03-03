using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class MenuUI : MonoBehaviour
{
    private static MenuUI Instance;

    [SerializeField] private Button playAgainstBotButton;
    [SerializeField] private Button playAgainstAnotherPlayerButton;
    [SerializeField] private Button devButton;
    [SerializeField] private Button quitButton;
    
    private const string againstBotScene = "Bot";
    private const string againstAnotherPlayerScene = "Local";
    private const string devScene = "Dev";


    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        playAgainstBotButton.onClick.AddListener(() => LoadScene(Board.GameState.Bot));
        playAgainstAnotherPlayerButton.onClick.AddListener(() => LoadScene(Board.GameState.Local));
        devButton.onClick.AddListener(() => LoadScene(Board.GameState.Dev));
        quitButton.onClick.AddListener(() => Application.Quit());
    }

    private void LoadScene(Board.GameState state)
    {
        Board.gameState = state;

        switch (state)
        {
            case Board.GameState.Bot:
                Bot.color = Piece.PieceColor.Black;
                SceneManager.LoadScene(againstBotScene);
                break;
            case Board.GameState.Local:
                MoveManager.AutoChangeView = true;
                SceneManager.LoadScene(againstAnotherPlayerScene);
                break;
            case Board.GameState.Dev:
                SceneManager.LoadScene(devScene);
                break;
            default:
                throw new Exception();
        }
    }

    void Update()
    {
        
    }
}
