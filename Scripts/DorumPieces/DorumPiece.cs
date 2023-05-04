using UnityEngine;
using System.Collections.Generic;


public enum DorumPieceType
{
    none = 0,
    Piece = 1,
}
public class DorumPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public DorumPieceType type;
    //private Vector3Team team;
    public int Team { get; set; }

    private Vector3 desiredPosition;
    private Vector3 desiredScale = new Vector3(80, 80, 80);

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref DorumPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        r.Add(new Vector2Int(1, 3));
        r.Add(new Vector2Int(2, 3));
        r.Add(new Vector2Int(2, 2));
        r.Add(new Vector2Int(3, 2));
        return r;
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
            transform.localScale = desiredScale;
    }

    /*public virtual void CheckForSequence(ref DorumPiece[,] board, int tileCountX, int tileCountY, Team team)
    {
        // Check for horizontal sequences
        for (int y = 0; y < tileCountY; y++)
        {
            for (int x = 0; x < tileCountX - 2; x++)
            {
                if (board[x, y] != null && board[x, y].team == team &&
                    board[x + 1, y] != null && board[x + 1, y].team == team &&
                    board[x + 2, y] != null && board[x + 2, y].team == team)
                {
                    // Found a sequence, allow the team to pick out the opponent's piece on right-click
                    board[x + 2, y].OnRightClick += () =>
                    {
                        board[x + 2, y] = null;
                    };
                }
            }
        }

        // Check for vertical sequences
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY - 2; y++)
            {
                if (board[x, y] != null && board[x, y].team == team &&
                    board[x, y + 1] != null && board[x, y + 1].team == team &&
                    board[x, y + 2] != null && board[x, y + 2].team == team)
                {
                    // Found a sequence, allow the team to pick out the opponent's piece on right-click
                    board[x, y + 2].OnRightClick += () =>
                    {
                        board[x, y + 2] = null;
                    };
                }
            }
        }
    }*/

}
