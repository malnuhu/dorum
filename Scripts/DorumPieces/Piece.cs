using System.Collections.Generic;
using UnityEngine;

public class Piece : DorumPiece
{

    public override List<Vector2Int> GetAvailableMoves(ref DorumPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> availableMoves = new List<Vector2Int>();

        // Check if it is possible to move up
        if (currentY + 1 < tileCountY && board[currentX, currentY + 1] == null)
            availableMoves.Add(new Vector2Int(currentX, currentY + 1));

        // Check if it is possible to move down
        if (currentY - 1 >= 0 && board[currentX, currentY - 1] == null)
            availableMoves.Add(new Vector2Int(currentX, currentY - 1));

        // Check if it is possible to move right
        if (currentX + 1 < tileCountX && board[currentX + 1, currentY] == null)
            availableMoves.Add(new Vector2Int(currentX + 1, currentY));

        // Check if it is possible to move left
        if (currentX - 1 >= 0 && board[currentX - 1, currentY] == null)
            availableMoves.Add(new Vector2Int(currentX - 1, currentY));

        return availableMoves;
    }

}
