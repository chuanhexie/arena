using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Pathfinding;
using Random = UnityEngine.Random;

namespace Arena
{
    public class GridManager : MonoBehaviour 
    {
        [Header("(EDITABLE)")]
        [Header("Prefabs")]
        public GameObject prefabTile;
        public GameObject prefabSpawner;

        [Header("Sprites")]
        public Sprite floorSprite;
        public Sprite blockSprite;
        public Sprite floorSpawnSprite;

        [Header("Grid Size & Location")]
        public int gridWidth;
        public int gridHeight;
        public float gridCenterX;
        public float gridCenterY;

        [Header("Random Generation Modifiers")]
        public int amountOfBlocks;

        [Space(10)]

        [Header("(REFERENCE)")]
        public List<GameObject> allTilesInGrid;
        public List<GameObject> spawners;

        // Use this for initialization
        void Start () 
        {
            
        }

        // Update is called once per frame
        void Update () 
        {
            
        }

        public static GridManager singleton;
        void Awake()
        {
            singleton = this;
        }

        public void GenerateTiles()
        {
            var tileGameObject = Instantiate(prefabTile);   // instantiate model to base all tiles off of, this will be destroyed
            float tileScale = tileGameObject.GetComponent<Renderer>().bounds.size.x;
            float startingX = gridCenterX - ((gridWidth * tileScale) / 2);
            float startingY = gridCenterY - ((gridHeight * tileScale) / 2);
            float curX = startingX;
            float curY = startingY;
            Destroy(tileGameObject.gameObject);

            // GENERATE EMPTY TILES
            for (int row = 1; row <= gridHeight; row++)
            {
                for (int col = 1; col <= gridWidth; col++)
                {
                    tileGameObject = Instantiate(prefabTile);
                    BattleManager.singleton.battleObjects.Add(tileGameObject);
                    allTilesInGrid.Add(tileGameObject);
                    var tileScript = tileGameObject.GetComponent<Tile>();
                    tileScript.locationX = col;
                    tileScript.locationY = row;

                    Vector2 tilePosition = new Vector2(curX, curY);

                    tileGameObject.transform.position = tilePosition;

                    ChangeTileState(tileGameObject, false);
                    curX += tileScale;
                }

                curX = startingX;
                curY += tileScale;
            }

            // CHANGE SPECIFIC TILES INTO ENEMY SPAWNERS
            int middleRow = Mathf.RoundToInt((gridHeight + 1) / 2);
            int middleCol = Mathf.RoundToInt((gridWidth + 1) / 2);

            Dictionary<int, GameObject> spawnerDictionary = new Dictionary<int, GameObject>();

            foreach (GameObject tile in allTilesInGrid)
            {
                int dictionaryKey = 0;
                float senderLocalXPos = 0;
                float senderLocalYPos = 0;

                if (spawnerDictionary.Count() < 4)
                {
                    var tileScript = tile.GetComponent<Tile>();
                    bool createSpawner = false;
                    if (tileScript.locationX == gridWidth && tileScript.locationY == middleRow)
                    {
                        dictionaryKey = 1;
                        createSpawner = true;
                        senderLocalXPos = 1;
                        senderLocalYPos = 0;
                    }
                    else if (tileScript.locationX == middleCol && tileScript.locationY == 1)
                    {
                        dictionaryKey = 2;
                        createSpawner = true;
                        senderLocalXPos = 0;
                        senderLocalYPos = -1;
                    }
                    else if (tileScript.locationX == 1 && tileScript.locationY == middleRow)
                    {
                        dictionaryKey = 3;
                        createSpawner = true;
                        senderLocalXPos = -1;
                        senderLocalYPos = 0;
                    }
                    else if (tileScript.locationX == middleCol && tileScript.locationY == gridHeight)
                    {
                        dictionaryKey = 4;
                        createSpawner = true;
                        senderLocalXPos = 0;
                        senderLocalYPos = 1;
                    }

                    if (createSpawner)
                    {
                        ChangeTileState(tile, false, true);
                        var newSpawnerGameObject = Instantiate(prefabSpawner);
                        var newSpawnerScript = newSpawnerGameObject.GetComponent<Spawner>();
                        newSpawnerGameObject.transform.position = tile.transform.position;
                        newSpawnerScript.receiver = tile;
                        newSpawnerScript.sender = new GameObject(tile.name + "-spawner-sender");
                        tile.transform.SetParent(newSpawnerGameObject.transform);
                        newSpawnerScript.sender.transform.SetParent(newSpawnerGameObject.transform);
                        newSpawnerScript.sender.transform.localPosition = new Vector2(senderLocalXPos, senderLocalYPos);

                        spawnerDictionary.Add(dictionaryKey, newSpawnerGameObject);
                    }
                }
                else
                    break;
            }

            // ADD SPAWNERS IN SEQUENCE THAT THEY WILL SPAWN ENEMIES IN
            for (var i = 1; i <= spawnerDictionary.Count(); i++)
                spawners.Add(spawnerDictionary[i]);

            // ADD SPAWNER GATES ADJACENT TO SPAWNER TILES
            UIManager.singleton.InitSpawnerGateSystem();

            // BLOCK SPAWNING RANDOMIZER ELEMENTS
            int amountOfBlocks = Random.Range(0, Mathf.RoundToInt((allTilesInGrid.Count() - spawners.Count()) * .75f));

            int blockClumpChance = Random.Range(1, 101);
            bool willClumpNextBlock = Random.Range(1, 101) <= blockClumpChance;
            int rowColRemovalInitChance = (amountOfBlocks / 2);
            int borderRowColRemovalChance = Random.Range(1, 101);
            bool willRemoveBlockBorders = Random.Range(1, 101) <= borderRowColRemovalChance;
            bool currentTileIsAdjacentToAnyBlock = false;
            bool isFirstBlockSpawn = true;

            // PROHIBIT BLOCK SPAWNING ON RANDOM ROWS AND COLUMNS
            int totalRowsAndCols = gridWidth + gridHeight;

            float baseRowColProhibitBlockSpawnChance = 100;
            float rowColProhibitBlockSpawnChanceDegradeRate = 0.85f;
            int rowColProhibitBlockSpawnCount = 0;
            if (Random.Range(1, 101) <= rowColRemovalInitChance)
            {
                bool rowColRandomFailed = false;
                while (!rowColRandomFailed && rowColProhibitBlockSpawnCount < totalRowsAndCols)
                {
                    if (Random.Range(1, 101) <= baseRowColProhibitBlockSpawnChance)
                    {
                        rowColProhibitBlockSpawnCount += 1;
                        baseRowColProhibitBlockSpawnChance = Mathf.RoundToInt(baseRowColProhibitBlockSpawnChance * rowColProhibitBlockSpawnChanceDegradeRate);
                    }
                    else
                        rowColRandomFailed = true;
                }
            }

            // GENERATE REMOVAL INTS FOR ALL ROWS AND COLUMNS (to be randomly removed until they meet the total amount of removals needed)
            List<int> rowsToProhibitBlockSpawn = new List<int>();
            for (int i = 1; i <= gridHeight; i++)
            {
                rowsToProhibitBlockSpawn.Add(i);
            }
            List<int> colsToProhibitBlockSpawn = new List<int>();
            for (int i = 1; i <= gridWidth; i++)
            {
                colsToProhibitBlockSpawn.Add(i);
            }

            // REMOVE REMOVAL-INTS UNTIL THEY MEET THE AMOUNT OF REMOVAL INTS NEEDED
            int prohibitColChance = 20;
            while (rowsToProhibitBlockSpawn.Count() + colsToProhibitBlockSpawn.Count() > rowColProhibitBlockSpawnCount)
            {
                bool willProhibitCol = Random.Range(1, 101) <= prohibitColChance;

                // IF ROW AND ROW HAS ROOM
                if (willProhibitCol && rowsToProhibitBlockSpawn.Count() > 0)
                {
                    prohibitColChance /= 2;
                    if (prohibitColChance < 0)
                        prohibitColChance = 0;
                    rowsToProhibitBlockSpawn.Remove(rowsToProhibitBlockSpawn[Random.Range(0, rowsToProhibitBlockSpawn.Count())]);
                }
                // IF NOT ROW AND COL HAS ROOM
                else if (colsToProhibitBlockSpawn.Count() > 0)
                {
                    prohibitColChance += 10;
                    colsToProhibitBlockSpawn.Remove(colsToProhibitBlockSpawn[Random.Range(0, colsToProhibitBlockSpawn.Count())]);
                }
                // IF NEITHER HAVE ROOM THEN BREAK
                else
                    break;
            }

            // IF BORDER REMOVAL SUCCEEDS, REMOVE BORDERS FROM PROHIBITOR LIST
            if (willRemoveBlockBorders)
            {
                rowsToProhibitBlockSpawn.Add(1);
                rowsToProhibitBlockSpawn.Add(gridHeight);
                colsToProhibitBlockSpawn.Add(1);
                colsToProhibitBlockSpawn.Add(gridWidth);
            }

                
            // DEBUG STATS
//            Debug.Log("Amount of Blocks: " + amountOfBlocks);
//            Debug.Log("Block Clump Chance: " + blockClumpChance);
//            Debug.Log("Row/Col Removal Init Chance: " + rowColRemovalInitChance);
//            Debug.Log("Row/Col Border Removal Chance: " + borderRowColRemovalChance);
//            string blockedRows = "Blocked Rows:";
//            string blockedCols = "Blocked Cols:";
//            foreach (int randomRowNum in rowsToProhibitBlockSpawn)
//                blockedRows += " " + randomRowNum.ToString();
//            foreach (int randomColNum in colsToProhibitBlockSpawn)
//                blockedCols += " " + randomColNum.ToString();
//            Debug.Log(blockedRows);
//            Debug.Log(blockedCols);

            // SPAWN AS MANY BLOCKS AS SPECIFIED (will break if there is nowhere left to spawn based on generation mods)
            for (var i = 1; i <= amountOfBlocks; i++)
            {
                List<GameObject> curOpenTiles = new List<GameObject>();

                // GET LIST OF EMPTY TILES: find all that are empty tiles in order to get a list of spots to randomly spawn a block at
                foreach (GameObject tile in allTilesInGrid)
                {
                    var tileScript = tile.GetComponent<Tile>();

                    // FILTER OUT NON-EMPTY TILES
                    if (tileScript.isBlock == false && tileScript.isSpawner == false)
                    {
                        // IF THE NEXT BLOCK TO SPAWN SHOULD BE CLUMPED, THEN CHECK IF TILE IS ADJACENT TO ANY BLOCK
                        currentTileIsAdjacentToAnyBlock = false;
                        if (willClumpNextBlock && !isFirstBlockSpawn)
                        {
                            //Debug.Log(allTilesInGrid.Count() + " : " + GetAdjacentTiles(tile).Count());
                            currentTileIsAdjacentToAnyBlock = GetAdjacentTiles(tile).Any(adjTile => adjTile.GetComponent<Tile>().isBlock);
                        }

                        // ONLY ADD TILE TO POTENTIAL SPAWN LOCATIONS IF: doesn't need to be clumped OR needs to be clumped and is adjacent to block OR is first block
                        // AND IF BLOCK ISN'T IN A RANDOMLY PROHIBITED COL OR ROW
                        if ((!willClumpNextBlock || currentTileIsAdjacentToAnyBlock || isFirstBlockSpawn) &&
                            (!rowsToProhibitBlockSpawn.Any(y => y == tileScript.locationY) && !colsToProhibitBlockSpawn.Any(x => x == tileScript.locationX)))
                                curOpenTiles.Add(tile);
                    }
                }
                    
                // RANDOMLY SELECT EMPTY TILE TO SPAWN BLOCK AT
                bool curBlockPlaced = false;
                while (!curBlockPlaced)
                {
                    if (curOpenTiles.Count() > 0)
                    {
                        // RANDOM SELECTION
                        int randomTileIndex = Random.Range(0, (curOpenTiles.Count()));
                        GameObject curTile = curOpenTiles[randomTileIndex];

                        // NULLCHECK
                        if (curTile != null)
                        {
                            // GET LIST OF EMPTY TILES ADJACENT TO POTENTIAL BLOCK SPAWN LOCATION; PLUS ENEMY SPAWNING TILES
                            List<GameObject> tilesForAreaCheck = GetAdjacentTiles(curTile, true);
                            tilesForAreaCheck.AddRange(GetAllSpawnerTiles());

                            // ADD BLOCK IN POTENTIAL SPOT, AND SCAN NEW PATHFINDING MAP
                            ChangeTileState(curTile, true);
                            AstarPath.active.Scan();

                            // IF ADJACENT TILES AND ENEMY SPAWNER TILES STILL CAN ALL REACH EACH OTHER, KEEP BLOCK AND BREAK RANDOM BLOCK SPAWN LOOP;
                            // IF NOT, REMOVE THIS TILE FROM CANDIDATES, AND RUN LOOP AGAIN, IF NO CANDIDATGES LEFT THEN BREAK LOOP
                            if (AreTilesInSameArea(tilesForAreaCheck) != true)
                            {
                                ChangeTileState(curTile, false);
                                curOpenTiles.Remove(curTile);
                            }
                            else
                            {
                                curBlockPlaced = true;
                                isFirstBlockSpawn = false;

                                willClumpNextBlock = Random.Range(1, 101) <= blockClumpChance;
                            }
                        }
                    }
                    else
                        break;
                }
            }

            AstarPath.active.Scan();
        }

        public void ChangeTileState(GameObject tileGameObject, bool isBlock = false, bool isSpawner = false)
        {
            var tileScript = tileGameObject.GetComponent<Tile>();
            tileScript.isBlock = isBlock;

            var spriteRenderer = tileGameObject.GetComponent<SpriteRenderer>();
            var battleObjectScript = tileGameObject.GetComponent<BattleObject>();
            if (isBlock)
            {
                spriteRenderer.sprite = blockSprite;
                tileGameObject.GetComponent<BoxCollider2D>().enabled = true;
                battleObjectScript.defensiveCombatHitbox.GetComponent<BoxCollider2D>().enabled = true;
                battleObjectScript.curHP = battleObjectScript.maxHP;
            }
            else
            {
                if (isSpawner)
                {
                    spriteRenderer.sprite = floorSpawnSprite;
                    tileScript.isSpawner = true;
                }
                else
                    spriteRenderer.sprite = floorSprite;
                tileGameObject.GetComponent<BoxCollider2D>().enabled = false;
                battleObjectScript.defensiveCombatHitbox.GetComponent<BoxCollider2D>().enabled = false;
            }

        }

        public bool AreTilesInSameArea(List<GameObject> tilesToCheck)
        {
            bool output = true;
            uint previousNodeAreaId = 0;

            foreach (GameObject tile in tilesToCheck)
            {
                uint currentNodeAreaId = AstarPath.active.GetNearest(tile.transform.position).node.Area;

                if (previousNodeAreaId == 0)
                    previousNodeAreaId = currentNodeAreaId;

                if (currentNodeAreaId != previousNodeAreaId)
                {
                    output = false;
                    break;
                }
            }

            return output;
        }

        public List<GameObject> GetAdjacentTiles(GameObject tile, bool emptyTilesOnly = false)
        {
            var tileScript = tile.GetComponent<Tile>();
            var x = tileScript.locationX;
            var y = tileScript.locationY;

            List<GameObject> output = new List<GameObject>();

            output.Add(GetTileByCoordinates(x + 1, y, emptyTilesOnly));
            output.Add(GetTileByCoordinates(x - 1, y, emptyTilesOnly));
            output.Add(GetTileByCoordinates(x, y + 1, emptyTilesOnly));
            output.Add(GetTileByCoordinates(x, y - 1, emptyTilesOnly));

            output = output.Where(item => item != null).ToList();

            return output;
        }

        public GameObject GetTileByCoordinates(int _x, int _y, bool emptyTileOnly = false)
        {
            foreach (GameObject tile in allTilesInGrid)
            {
                var tileScript = tile.GetComponent<Tile>();

                if ((emptyTileOnly == false || (tileScript.isBlock != true && tileScript.isSpawner != true)) &&
                    (tileScript.locationX == _x && tileScript.locationY == _y))
                    return tile;
            }

            return null;
        }

        public List<GameObject> GetAllBlocksInGrid()
        {
            return allTilesInGrid.Where(curTile => curTile.GetComponent<Tile>().isBlock).ToList();
        }

        public List<GameObject> GetAllSpawnerTiles()
        {
            List<GameObject> output = new List<GameObject>();
            foreach (GameObject curSpawner in spawners)
                output.Add(curSpawner.GetComponent<Spawner>().receiver);

            return output;
        }
    }


}