using System;
using System.Collections.Generic;
using Unity.Networking.Transport;
using UnityEngine.UI;
using UnityEngine;


public class dorumboard : MonoBehaviour
{

    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.02f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 50.0f;
    [SerializeField] private float deathSpacing = 0.55f;
    [SerializeField] private float dragOffset = 1.0f;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private Transform rematchIndicator;
    [SerializeField] private Button rematchButton;


    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    //LOGIC
    public DorumPiece[,] dorumPieces;
    private DorumPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<DorumPiece> deadWhites = new List<DorumPiece>();
    private List<DorumPiece> deadBlacks = new List<DorumPiece>();
    private const int TILE_COUNT_X = 5;
    private const int TILE_COUNT_Y = 6;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    private bool isWhiteTurn;

    //Multi Player Logic
    private int playerCount = -1;
    private int currentTeam = -1;
    private bool localGame = true;
    private bool[] playerRematch = new bool[2];
   

    private void Start()
    {
        isWhiteTurn = true;
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        PlacePieces();
        PositionAllPieces();

        RegisterEvents();
    }
    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            // Getting the index of tile hit
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            // Beginning of hovering
            if (currentHover == Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            // Changing the previuos tile hovered upon to default
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //When we press down the mouse
            if (Input.GetMouseButtonDown(0))
            {
                if (dorumPieces[hitPosition.x, hitPosition.y] != null)
                {
                    //Is it our turn
                    if ((dorumPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && currentTeam == 0) || (dorumPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && currentTeam == 1))
                    {
                        currentlyDragging = dorumPieces[hitPosition.x, hitPosition.y];

                        //Get the List of Where i can go and Highlight tiles as well
                        availableMoves = currentlyDragging.GetAvailableMoves(ref dorumPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        HighlightTiles();
                    }
                }
            }

            //When we are releasing the mouse button
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                if (ContainsValidMove(ref availableMoves, new Vector2(hitPosition.x, hitPosition.y)))
                {
                    MoveTo(previousPosition.x, previousPosition.y, hitPosition.x, hitPosition.y);

                    //Net Implementation
                    NetMakeMove mm = new NetMakeMove();
                    mm.originalX = previousPosition.x;
                    mm.originalY = previousPosition.y;
                    mm.destinationX = hitPosition.x;
                    mm.destinationY = hitPosition.y;
                    mm.teamId = currentTeam;
                    Client.Instance.SendToServer(mm);
                }
                else
                {
                    currentlyDragging.SetPosition(GetTileCentre(previousPosition.x, previousPosition.y));
                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
                
            }

            //When we click the right mouse button
            if (Input.GetMouseButtonDown(1))
            {
                DorumPiece pieceToRemove = dorumPieces[hitPosition.x, hitPosition.y];
                RemovePiece(pieceToRemove, hitPosition.x, hitPosition.y);

            }

        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = Vector2Int.one;
            }
            if (currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCentre(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        //If we are Dragging a piece
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
        }
    }

    // Generate the board
    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y;
        bounds = new Vector3((tileCountY / 2) * tileSize, 0, (tileCountY / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
            for (int y = 0; y < tileCountY; y++)
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffset, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffset, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffset, (y + 1) * tileSize) - bounds;


        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // Placing of pieces
    private void PlacePieces()
    {
        dorumPieces = new DorumPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0, blackTeam = 1;

        //whiteTeam
        dorumPieces[0, 3] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[0, 1] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[1, 5] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[1, 3] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[1, 0] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[2, 5] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[2, 4] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[2, 2] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[3, 2] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[3, 1] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[4, 3] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);
        dorumPieces[4, 0] = PlaceAPiece(DorumPieceType.Piece, whiteTeam);

        //BlackTeam
        dorumPieces[0, 4] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[0, 2] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[1, 2] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[1, 1] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[2, 3] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[2, 1] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[2, 0] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[3, 4] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[3, 3] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[3, 0] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[4, 5] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
        dorumPieces[4, 2] = PlaceAPiece(DorumPieceType.Piece, blackTeam);
       
    }

    //Placing a Single Piece
    private DorumPiece PlaceAPiece(DorumPieceType type, int team)
    {
        DorumPiece dp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<DorumPiece>();

        dp.type = type;
        dp.team = team;
        dp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return dp;
    }



    //Positioning
    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (dorumPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
       

        dorumPieces[x, y].currentX = x;
        dorumPieces[x, y].currentY = y;
        dorumPieces[x, y].SetPosition(GetTileCentre(x, y), force);

    }
    private Vector3 GetTileCentre(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }
   
    // Highlight of Tiles
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

        availableMoves.Clear();
    }


    //Winning Section
    private void WinCondition(int team)
    {
        DisplayVictory(team);
    }
    private void DisplayVictory(int winningTeam)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(winningTeam).gameObject.SetActive(true);
    }
    public void OnReMatchButton()
    {
        if (localGame)
        {
            NetRematch wrm = new NetRematch();
            wrm.teamId = 0;
            wrm.wantRematch = 1;
            Client.Instance.SendToServer(wrm);

            NetRematch brm = new NetRematch();
            brm.teamId = 1;
            brm.wantRematch = 0;
            Client.Instance.SendToServer(brm);
        }
        else
        {
            NetRematch rm = new NetRematch();
            rm.teamId = currentTeam;
            rm.wantRematch = 1;
            Client.Instance.SendToServer(rm);
        }
        
    }
    public void GameReset()
    {
        rematchButton.interactable = true;
        //Setting of the UI on reset
        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false);

        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);

        //Field reset
        currentlyDragging = null;
        availableMoves.Clear();
        playerRematch[0] = playerRematch[1] = false;

        // Section for Cleaning left over pieces on the board
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (dorumPieces[x, y] != null)
                    Destroy(dorumPieces[x, y].gameObject);

                dorumPieces[x, y] = null;
            }
        }
        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject);
        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject);

        deadWhites.Clear();
        deadBlacks.Clear();

        PlacePieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }
    public void OnMenuButton()
    {
        NetRematch rm = new NetRematch();
        rm.teamId = currentTeam;
        rm.wantRematch = 0;
        Client.Instance.SendToServer(rm);

        GameReset();
        GameUI.Instance.OnLeaveFromGameMenu();

        Invoke("ShutdownRelay", 1.0f);

        playerCount = -1;
        currentTeam = -1;
    }

    // Operations
    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;
        return false;
    }
    private void MoveTo(int originalX, int originalY, int x, int y)
    {

        DorumPiece dp = dorumPieces[originalX, originalY];
        Vector2Int previousPosition = new Vector2Int(originalX, originalY);

        //Is there another piece on the target position?
       if (dorumPieces[x, y] != null)
        {
            DorumPiece odp = dorumPieces[x, y];

            if (dp.team == odp.team)
            return;
        }

        dorumPieces[x, y] = dp;
        dorumPieces[previousPosition.x, previousPosition.y] = null;

        PositionSinglePiece(x, y);
        isWhiteTurn = !isWhiteTurn;
        if (localGame)
            currentTeam = (currentTeam == 0)? 1 : 0;
        if(currentlyDragging)
            currentlyDragging = null;
        RemoveHighlightTiles();

        return;
    }
    private bool RemovePiece(DorumPiece pdp, int x, int y)
    {
        if (dorumPieces[x, y] == null)
        {
            return false;
        }
        Vector2Int previousPosition = new Vector2Int(pdp.currentX, pdp.currentY);

        if (pdp.team == 0)
        {
            deadWhites.Add(pdp);
            pdp.SetScale(Vector3.one * deathSize);
            pdp.SetPosition(
                new Vector3(5 * tileSize, yOffset, -1 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + (Vector3.forward * deathSpacing) * deadWhites.Count);

            dorumPieces[previousPosition.x, previousPosition.y] = null;

            // Check if this is the last white piece, and if so, black team wins
            if (deadWhites.Count == 12)
                WinCondition(1);
            return true;
        }
        else
        {
            deadBlacks.Add(pdp);
            pdp.SetScale(Vector3.one * deathSize);
            pdp.SetPosition(
                new Vector3(-1 * tileSize, yOffset, 5 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + (Vector3.back * deathSpacing) * deadBlacks.Count);

            dorumPieces[previousPosition.x, previousPosition.y] = null;

            // Check if this is the last black piece, and if so, white team wins
            if (deadBlacks.Count == 12)
                WinCondition(0);
            return true;
        }
        //return false;
    }
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);
        return -Vector2Int.one;  // Seeking for non existing tile
    }

    #region
    private void RegisterEvents()
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_REMATCH += OnRematchServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_REMATCH += OnRematchClient;

        GameUI.Instance.SetLocalGame += OnSetLocalGame;
    }
    private void UnRegisterEvents()
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_REMATCH -= OnRematchServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_REMATCH -= OnRematchClient;

        GameUI.Instance.SetLocalGame -= OnSetLocalGame;

    }
    //Server Messages
    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
    {
        //Client has connected, assign a team and return the message back to him
        NetWelcome nw = msg as NetWelcome;

        //Assign a team
        nw.AssignedTeam = ++playerCount;

        //Return back to the client
        Server.Instance.SendToClient(cnn, nw);

        // If full, Start the game
        if(playerCount == 1)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }
    }
    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        NetMakeMove mm = msg as NetMakeMove;

     
        Server.Instance.Broadcast(msg);
    }
    private void OnRematchServer(NetMessage msg, NetworkConnection cnn)
    {
        Server.Instance.Broadcast(msg);
    }

    //Client Message 
    private void OnWelcomeClient(NetMessage msg)
    {
        // Receive the connection message
        NetWelcome nw = msg as NetWelcome;

        //Assign the team
        currentTeam = nw.AssignedTeam;

        Debug.Log($"My Assigned team is {nw.AssignedTeam}");

        if (localGame && currentTeam == 0)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }
    }
    private void OnStartGameClient(NetMessage msg)
    {
        // Change the Camera
        GameUI.Instance.ChangeCamera((currentTeam == 0) ? CameraAngle.whiteTeam : CameraAngle.blackTeam);
    }
    private void OnMakeMoveClient(NetMessage msg)
    {
        NetMakeMove mm = msg as NetMakeMove;

        Debug.Log($"MM : {mm.teamId} : {mm.originalX} {mm.originalY} -> {mm.destinationX} {mm.destinationY} ");

        if(mm.teamId != currentTeam)
        {
            DorumPiece target = dorumPieces[mm.originalX, mm.originalY];

            availableMoves = target.GetAvailableMoves(ref dorumPieces, TILE_COUNT_X, TILE_COUNT_Y); 

            MoveTo(mm.originalX, mm.originalY, mm.destinationX, mm.destinationY);
        }
    }
    private void OnRematchClient(NetMessage msg)
    {
        NetRematch rm = msg as NetRematch;

        //Set the boolean for Rematch
        playerRematch[rm.teamId] = rm.wantRematch == 1;

        // Activate the piece of UI
        if(rm.teamId != currentTeam)
        {
            rematchIndicator.transform.GetChild((rm.wantRematch == 1) ? 0:1).gameObject.SetActive(true);
            if (rm.wantRematch != 1)
            {
                rematchButton.interactable = false;
            }
        }

        //If both wants to rematch
        if (playerRematch[0] && playerRematch[1])
            GameReset();
    }


    //
    private void ShutdownRelay()
    {
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }
    private void OnSetLocalGame(bool v)
    {
        playerCount = -1;
        currentTeam = -1;
        localGame = v;
    }

    #endregion
}

