﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public static UI Instance;

    public static string currentFenInputFieldValue;

    [SerializeField] private GameObject RandomPositionSettingsGameObject;
    [SerializeField] private Toggle[] CheckmarkToggles;
    [SerializeField] private Toggle[] RulesToggles;
    [SerializeField] private Text LegalMovesDisplay;


    void Start()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Board.Instance.spritesParent.SetActive(!Board.Instance.spritesParent.activeInHierarchy);
            RandomPositionSettingsGameObject.SetActive(!RandomPositionSettingsGameObject.activeInHierarchy);
        }
    }

    public static List<char> GetToggledPieces()
    {
        List<char> validCharacters = new List<char>();

        string characters = "PNBRQKpnbrqk";

        for (int i = 0; i < Instance.CheckmarkToggles.Length; i++)
        {
            if (Instance.CheckmarkToggles[i].isOn)
            {
                validCharacters.Add(characters[i]);
            }
        }

        print("valid characters: " + new string(validCharacters.ToArray()));
        return validCharacters;
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
    }

    public static void UpdateLegalMovesDisplay(List<int> selectedPieceLegalMoves)
    {
        string x = "";
        foreach (int move in selectedPieceLegalMoves)
        {
            x += Square.SquareNumberToAlphaNumeric(move).ToUpper() + "\n";
        }
        Instance.LegalMovesDisplay.text = x;
    }

    public void Reset()
    {
        for (int i = 0; i < Instance.RulesToggles.Length; i++)
        {
            Instance.RulesToggles[i].isOn = true;
        }
    }
}
