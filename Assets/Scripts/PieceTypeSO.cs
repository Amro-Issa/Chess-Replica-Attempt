﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/PieceType")]
public class PieceTypeSO : ScriptableObject
{
    public Piece.PieceType pieceType;

    public GameObject piecePrefab;

    public Sprite whitePieceSprite;
    public Sprite blackPieceSprite;
}
