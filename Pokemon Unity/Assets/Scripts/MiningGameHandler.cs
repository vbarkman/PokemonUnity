using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiningGameHandler : MonoBehaviour
{
    public Texture2D boardTilesheet;
    [SerializeField]
    private Sprite[] boardTiles;
    public GameObject boardTilePrefab;//prefab used to populate the board with tiles
    //Blockers
    public BoardShape[] blockers;   //collection of the blockers that can appear on the board
    public BoardShape[] items;      //collection of the items that can appear on the board
    public int boardHeight = 14;    //vertical size of teh board
    public int boardWidth = 14;     //horizontal size of the board
    public int richness = 4;        //richness of the board, number of treasures
    public GameObject BoardUIElement;
    public GameObject HammerUIElement;
    public GameObject PickUIElement;
    public DialogBoxHandlerNew Dialog;
    public GameObject Cursor;
    public GameObject ToolCursor;
    public float cursorTilesPerSecond = 1;              //moves per second

    private Vector2 cursorPosition;
    private string toolSelected = "pick";
    private Coroutine draw;
    private Coroutine mainRoutine;
    private bool gameOn = true;
    private int toolCursorPositionY = 0;
    private GameObject[,] boardTilesData;   //represent the shallow layer of the board
    private List<BoardShape> boardItemsData;   //represent the base layer of the board and contains items and blockers
    private int[,] boardTilesValues;        //represent the resistance values of the shallow layer of the board
    private int[,] boardItemsValues;        //represent items shapes on the board
    private int boardHealth = 99;

    void OnValidate()
    {
        boardTiles = Resources.LoadAll<Sprite>("UndergroundDiggingSprites/" + boardTilesheet.name);
    }

    public IEnumerator mainLoop()
    {
        if (BoardUIElement == null || HammerUIElement == null || PickUIElement == null)
        {
            Debug.LogWarning("UI Element missing from MiningGameHandler !");
            yield return null;
        }
        InitializeBoard();
        SelectPick();
        //ping
        Dialog.DrawDialogBox(2);
        yield return Dialog.StartCoroutine(Dialog.DrawText($"Something pinged in the wall ! {richness} items confirmed"));
        while (!(Input.GetButtonDown("Select") || Input.GetButtonDown("Select")))
            yield return null;
        Dialog.UndrawDialogBox();
        yield return new WaitForSeconds(0.2f);
        //
        gameOn = true;
        while (gameOn)
        {
                var v = Input.GetAxisRaw("Vertical");
                var h = Input.GetAxisRaw("Horizontal");
                if (v != 0)
                {
                    if (ToolCursor.activeSelf)
                    {
                        toolCursorPositionY += v > 0 ? 1 : -1;
                        if (toolCursorPositionY < 0)
                            toolCursorPositionY = 0;
                        if (toolCursorPositionY > 1)
                            toolCursorPositionY = 1;
                    }
                    else
                        cursorPosition.y += v > 0 ? 1 : -1;
                    UpdateCursor();
                    yield return new WaitForSeconds(1 / cursorTilesPerSecond);
                }
                else if (h != 0)
                {
                    cursorPosition.x += h > 0 ? 1 : -1;
                    UpdateCursor();
                    yield return new WaitForSeconds(1 / cursorTilesPerSecond);
                }
                else if (Input.GetButtonDown("Select"))
                {
                    //0 is the pick
                    if (cursorPosition.x == boardWidth)
                    {
                        if (toolCursorPositionY == 0)
                        {
                            SelectPick();
                        }
                        else if (toolCursorPositionY == 1)
                        {
                            SelectHammer();
                        }
                        yield return new WaitForSeconds(0.2f);
                    }
                    else
                    {
                        UseToolOnPosition();
                        CheckForUncoveredItems();
                    yield return new WaitForSeconds(0.2f);
                }
                }
                else if (Input.GetButtonDown("Back"))
                {
                    if (draw != null)
                        StopCoroutine(draw);
                    Dialog.DrawDialogBox();
                    draw = Dialog.StartCoroutine(Dialog.DrawText("Quit digging ?"));
                    yield return draw;
                    yield return Dialog.StartCoroutine(Dialog.DrawChoiceBox(0));
                    int chosenIndex = Dialog.chosenIndex;
                    if (chosenIndex == 1)
                    {
                        //GameOver();
                        gameOn = false;
                    }
                    Dialog.UndrawDialogBox();
                    Dialog.UndrawChoiceBox();
                    yield return new WaitForSeconds(0.2f);
                }
                yield return null;
        }
        foreach(var item in boardItemsData)
        {
            if (!item.isBlocking && item.isUncovered)
            {
                var itemName = ItemDatabase.getItem(item.Item).getName();
                SaveData.currentSave.Bag.addItem(itemName, 1);
                //ping
                Dialog.DrawDialogBox(2);
                yield return Dialog.StartCoroutine(Dialog.DrawText($"You dug out {itemName} !"));
                while (!(Input.GetButtonDown("Select") || Input.GetButtonDown("Select")))
                    yield return null;
                Dialog.UndrawDialogBox();
                yield return new WaitForSeconds(0.2f);
            }

        }
        this.gameObject.SetActive(false);
        CleanBoard();
    }

    void SelectHammer()
    {
        toolSelected = "hammer";
        PickUIElement.SetActive(false);
        HammerUIElement.SetActive(true);
        Cursor.GetComponent<Image>().color = Color.red;
    }
    void SelectPick()
    {
        toolSelected = "pick";
        PickUIElement.SetActive(true);
        HammerUIElement.SetActive(false);
        Cursor.GetComponent<Image>().color = Color.blue;
    }
    void UseToolOnPosition()
    {
        var oX = (int)cursorPosition.x;
        var oY = (int)cursorPosition.y;
        if (toolSelected == "hammer")
        {
            boardHealth -= 2;
            //iron piece uncovered, dampen the hit 
            if (boardTilesValues[oX, oY] == 0 && boardItemsValues[oX, oY] == 2)
            {
                Cursor.GetComponent<Animator>().SetTrigger("Fail");
                return;
            }
            for (int j = oY - 1; j <= oY + 1; j++)
            {
                for (int i = oX - 1; i <= oX + 1; i++)
                {
                    if (i >= 0 && j >= 0 && i < boardWidth && j < boardHeight)
                        ChangeTileValue(i, j);
                }
            }
            Cursor.GetComponent<Animator>().SetTrigger("Hammer");
        }
        else if(toolSelected == "pick")
        {
            boardHealth -= 2;
            if (boardTilesValues[oX, oY] == 0 && boardItemsValues[oX, oY] == 2)
            {
                Cursor.GetComponent<Animator>().SetTrigger("Fail");
                return;
            }
            ChangeTileValue(oX, oY);
            if (oX + 1 < boardWidth)
                ChangeTileValue(oX + 1, oY);
            if (oX  > 0)
                ChangeTileValue(oX - 1, oY);
            if (oY + 1 < boardHeight)
                ChangeTileValue(oX, oY + 1);
            if (oY > 0)
                ChangeTileValue(oX, oY - 1);
            Cursor.GetComponent<Animator>().SetTrigger("Pick");
        }
    }

    void CheckForUncoveredItems()
    {
        foreach(var item in boardItemsData)
        {
            var broke = false;
            //blockers and items already discovered dont count 
            if (item.isBlocking || item.isUncovered == true)
                continue;
            var trans = item.rTransform;
            var coordinates = new Vector2(trans.anchoredPosition.x / 16, trans.anchoredPosition.y / 16);
            Debug.Log(coordinates);
            for (int y = 0; y < item.height; y++)
            {
                for (int x = 0; x < item.width; x++)
                {
                    //only continue checking if the shape part empty or the board tile is 0
                    if (item.GetShapePartAtPosition(x, y) && boardTilesValues[x + (int)coordinates.x, y + (int)coordinates.y] > 0)
                    {
                        Debug.Log("broke out");
                        broke = true;
                        break;
                    }
                }
                if (broke == true)
                    break;
            }
            if (broke == false)
            {
                item.Uncovered();
                Debug.Log("yeahhh" + item);
                //add item to inventory
            }
        }
        // while there is an uncovered item
        gameOn = boardItemsData.Find(i => !i.isUncovered && !i.isBlocking);
    }

    void UpdateCursor()
    {
        if (cursorPosition.x < 0)
            cursorPosition.x = 0;
        if (cursorPosition.y < 0)
            cursorPosition.y = 0;
        if (cursorPosition.y >= boardHeight)
            cursorPosition.y--;
        if (cursorPosition.x > boardWidth)
            cursorPosition.x--;

        if (ToolCursor.activeSelf == false && cursorPosition.x >= boardWidth)
        {
            if (Input.GetButtonDown("Horizontal"))
                cursorPosition.x = boardWidth;
            else
                cursorPosition.x--;
        }
        if (cursorPosition.x == boardWidth)
        {
            Cursor.SetActive(false);
            //tools
            ToolCursor.SetActive(true);
            if (toolCursorPositionY == 1)
                ToolCursor.GetComponent<RectTransform>().localPosition = HammerUIElement.transform.localPosition;
            else if (toolCursorPositionY == 0)
                ToolCursor.GetComponent<RectTransform>().localPosition = PickUIElement.transform.localPosition;
        }
        else
        {
            Cursor.SetActive(true);
            ToolCursor.SetActive(false);
            Cursor.GetComponent<RectTransform>().anchoredPosition = cursorPosition * 16;
        }
    }

    void InitializeBoard()
    {
        boardTilesData = new GameObject[boardWidth, boardHeight];
        boardItemsData = new List<BoardShape>();
        boardTilesValues = new int[boardWidth, boardHeight];
        boardItemsValues = new int[boardWidth, boardHeight];
        boardHealth = 99;
        //place random items on the board depending on the richness
        for (int r = 0; r < richness; r++)
        {
            var randomIndex = Random.Range(0, items.Length);
            var item = Instantiate(items[randomIndex]);
            item.RotateRandom();
            item.isUncovered = false;
            if (!TryToPlace(item))
            {
                //shouldn't happen
                Destroy(item.gameObject);
            }
        }
        //place blockers after the items to make sure the richness is respected
        var numberOfBlockers = Random.Range(1, 5);
        for (int b = 0; b < numberOfBlockers; b++)
        {
            var randomIndex = Random.Range(0, blockers.Length);
            var blocker = Instantiate(blockers[randomIndex]);
            blocker.RotateRandom();
            if (!TryToPlace(blocker))
                Destroy(blocker.gameObject);
        }
        //cover the items with tiles
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                InstantiateTile(x, y);
            }
        }
    }

    bool TryToPlace(BoardShape shape)
    {
        var maxX = boardWidth - shape.width;
        var maxY = boardHeight - shape.height;

        var randX = Random.Range(0, maxX);
        var randY = Random.Range(0, maxY);
        for (int y = randY; y < maxY; y++)
        {
            for (int x = randX; x < maxX; x++)
            {
                if (CheckBoardAt(x, y, shape))
                {
                    Place(shape, x, y);
                    return true;
                }
            }
        }
        return false;
    }

    void Place(BoardShape shape, int x, int y)
    {
        for (int j = 0; j < shape.height; j++)
        {
            for (int i = 0; i < shape.width; i++)
            {
                if (shape.GetShapePartAtPosition(i, j))
                    boardItemsValues[x + i, y + j] = shape.GetShapePartAtPosition(i, j) ? 1 + (shape.isBlocking ? 1: 0) : 0;
            }
        }
        shape.transform.SetParent(BoardUIElement.transform);
        shape.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(x * 16, y * 16);
        shape.name += $"onBoard: {x},{y}";
        boardItemsData.Add(shape);
    }

    bool CheckBoardAt(int x, int y, BoardShape shape)
    {
        for (int j = 0; j < shape.height; j++)
        {
            for (int i = 0; i < shape.width; i++)
            {
                if (shape.GetShapePartAtPosition(i, j) && boardItemsValues[x + i, y + j] > 0)
                    return false;
            }
        }
        return true;
    }

    public int GetRandomWeightedIndex(int[] weights)
    {
        int weightSum = 0;
        //add up all weights
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] > 0)
            {
                weightSum += weights[i];
            }
        }
        //calculate weighted random
        float r = Random.Range(0, weightSum + 1);
        float s = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0)
                continue;
            s += weights[i];
            if (s >= r)
                return i;
        }
        return 0;
    }

    public void ResetBoard()
    {
        CleanBoard();
        InitializeBoard();

    }

    void CleanBoard()
    {
        //clear up all the data
        for (int y = 0; y < boardTilesData.GetLength(1); y++)
        {
            for (int x = 0; x < boardTilesData.GetLength(0); x++)
            {
                Destroy(boardTilesData[x, y]);

            }
        }
        foreach (var item in boardItemsData)
            Destroy(item.gameObject);
    }

    void InstantiateTile(int x, int y)
    {
        var newTile = Instantiate(boardTilePrefab);
        newTile.transform.SetParent(BoardUIElement.transform);
        //the origin is the bottom left corner
        newTile.GetComponent<RectTransform>().anchoredPosition = new Vector3(x * 16, y * 16);
        //randomize the tile resistance, start at 1 because we don't want empty tiles
        var tileValue = GetRandomWeightedIndex(new int[] { 0, 0, 1, 0, 1, 0, 1 });
        newTile.GetComponent<Image>().sprite = boardTiles[tileValue];
        boardTilesValues[x, y] = tileValue;
        boardTilesData[x, y] = newTile;
    }

    void ChangeTileValue(int x, int y)
    {
        if (boardTilesValues[x, y] > 0)
        {
            boardTilesValues[x, y]--;
            if (boardTilesValues[x, y] == 0)
                boardTilesData[x, y].SetActive(false);
            else
                boardTilesData[x, y].GetComponent<Image>().sprite = boardTiles[boardTilesValues[x, y]];
        }
    }
}
