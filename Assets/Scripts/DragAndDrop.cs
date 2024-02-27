using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragAndDrop : MonoBehaviour
{
    private static bool active = false;

    private static PieceIdentifier draggedPieceIdentifier;
    private static GameObject draggedPiece;

    [SerializeField] private GameObject pieceImage;

    [SerializeField] private string pieceSelectorTag;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Input.mousePosition, Vector2.zero);

            if (hit.collider?.tag == pieceSelectorTag)
            {
                draggedPieceIdentifier= hit.collider.GetComponent<PieceIdentifier>();
                Drag();
            }
        }

        if (active)
        {
            draggedPiece.transform.position = Input.mousePosition;

            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                Drop();
                active = false;
            }
            else if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                //suspends drag and drop if right mouse button was clicked
                Destroy(draggedPiece);
                draggedPiece = null;
                active = false;
            }
        }
    }

    public void Drag()
    {
        active = true;

        draggedPiece = Instantiate(pieceImage, UI.Instance.canvas.transform);

        PieceTypeSO typeSO = Board.PieceTypeToSO[draggedPieceIdentifier.type];
        Sprite sprite = draggedPieceIdentifier.color == Piece.PieceColor.White ? typeSO.whitePieceSprite : typeSO.blackPieceSprite;

        draggedPiece.GetComponent<Image>().sprite = sprite;
    }

    private void Drop()
    {
        RaycastHit2D hit = Physics2D.Raycast(Utils.GetMouseWorldPosition(), Vector3.zero, 10, MoveManager.Instance.squaresLayer);

        if (hit.collider != null) //if we actually hit a square
        {
            Square square = hit.collider.GetComponent<Square>();

            square.Unoccupy(destroyPiece: true);
            Piece.CreateNewPiece(draggedPieceIdentifier.color, draggedPieceIdentifier.type, square);
        }

        Destroy(draggedPiece);
        draggedPiece = null;
    }
}
