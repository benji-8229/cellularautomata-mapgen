using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CellularGeneration : MonoBehaviour
{
    [Header("Map Settings")]
    public RawImage img;
    public Vector2Int mapSize;
    private Texture2D tex;

    [Header("Cellular Automata Settings")]
    public int numberOfSteps;
    public float mapGenWallPerc = .45f;
    public int deathLimit;
    public int birthNumber;
    private int[,] map;
    //public float desiredMapVolume = .5f;

    [Header("Postgen Settings")]
    public int spawnRequiredNBS;
    public float minDistanceFromSpawn;
    public float pointDistributionPerc;
    private List<List<Vector2Int>> caves = new List<List<Vector2Int>>();
    private List<Vector2Int> largestCave = new List<Vector2Int>();
    private Vector2Int spawnPoint;

    // Start is called before the first frame update
    void Start()
    {
        tex = new Texture2D(mapSize.x, mapSize.y);
        tex.filterMode = FilterMode.Point;
        img.texture = tex;
        map = new int[mapSize.x, mapSize.y];

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                map[x, y] = Random.value <= mapGenWallPerc ? 0 : 1;
            }
        }

        DrawMap();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(SlowGen(numberOfSteps, .2f));
        }
    }

    public int CountAliveNeighbors(int[,] map, int x, int y)
    {
        int neighbors = 0;

        for (int j = -1; j < 2; j++)
        {
            for (int k = -1; k < 2; k++)
            {
                int neighbor_x = x + j;
                int neighbor_y = y + k;

                if(j == 0 && k == 0)
                {
                    
                }
                else if (neighbor_x < 0 || neighbor_y < 0 || neighbor_x >= mapSize.x || neighbor_y >= mapSize.y)
                {
                    neighbors++;
                }
                else if (map[neighbor_x, neighbor_y] == 1)
                {
                    neighbors++;
                }
            }
        }

        return neighbors;
    }

    public int[,] DoSimulationStep(int[,] map)
    {
        int[,] newMap = new int[mapSize.x, mapSize.y];

        for(int x = 0; x < mapSize.x; x++)
        {
            for(int y = 0; y < mapSize.y; y++)
            {
                int neighbors = CountAliveNeighbors(map, x, y);

                if (map[x, y] == 1)
                {
                    newMap[x, y] = neighbors < deathLimit ? 0 : 1;
                }
                else
                {
                    newMap[x, y] = neighbors > birthNumber? 1 : 0;
                }
            }
        }

        return newMap;
    }

    public void DrawMap()
    {
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                switch (map[x, y])
                {
                    case 0:
                        tex.SetPixel(x, y, Color.black);
                        break;
                    case 1:
                        tex.SetPixel(x, y, Color.grey);
                        break;
                    case 2:
                        tex.SetPixel(x, y, Color.blue);
                        break;
                    case 3:
                        tex.SetPixel(x, y, Color.red);
                        break;
                    case 4:
                        tex.SetPixel(x, y, Color.blue);
                        break;
                }
            }
        }

        tex.Apply();
    }

    public IEnumerator SlowGen(int steps, float delay)
    {
        for (int i = 0; i <= steps; i++)
        {
            yield return new WaitForSeconds(delay);
            map = DoSimulationStep(map);
            DrawMap();
        }

        //floodfilling to find all caves
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (map[x, y] == 0)
                {
                    FloodFill(map, x, y); //floodfill the new cave we just found
                    caves.Add(FindFilledPoints()); //get the floodfilled points

                    foreach (Vector2Int coords in caves[caves.Count - 1]) //clear the cave we just found so it doesnt get filled again
                    {
                        map[coords.x, coords.y] = 1;
                    }

                    DrawMap();

                    yield return new WaitForSeconds(delay);
                }
            }
        }

        //finding the biggest cave by total number of points
        foreach (List<Vector2Int> cave in caves)
        {
            if (cave.Count > largestCave.Count)
            {
                largestCave = cave;
            }
        }

        //draw biggest cave
        foreach (Vector2Int coords in largestCave)
        {
            map[coords.x, coords.y] = 0;
        }

        spawnPoint = FindSpawnPoint(map, spawnRequiredNBS);
        GeneratePoints(map, largestCave, spawnPoint, minDistanceFromSpawn);

        DrawMap();
    }

    public void FloodFill(int[,] map, int x, int y)
    {
        //base cases
        if (x < 0 || y < 0 || x >= mapSize.x || y >= mapSize.y)
        {
            return;
        }

        if(map[x, y] != 0)
        {
            return;
        }

        map[x, y] = 2;

        FloodFill(map, x+1, y);
        FloodFill(map, x-1, y);
        FloodFill(map, x, y+1);
        FloodFill(map, x, y-1);
    }

    public List<Vector2Int> FindFilledPoints()
    {
        List<Vector2Int> retList = new List<Vector2Int>();

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (map[x, y] == 2)
                {
                    retList.Add(new Vector2Int(x, y));
                }
            }
        }

        return retList;
    }

    public Vector2Int FindSpawnPoint(int[,] map, int requiredNeighbors)
    {
        List<Vector2Int> temp = new List<Vector2Int>();

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                if (map[x, y] == 0)
                {
                    if(CountAliveNeighbors(map, x, y) > requiredNeighbors)
                    {
                        temp.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        Vector2Int newSpawn = temp[Random.Range(0, temp.Count - 1)];

        map[newSpawn.x, newSpawn.y] = 3;
        return newSpawn;
    }

    public void GeneratePoints(int[,] map, List<Vector2Int> points, Vector2Int spawn, float minDistance)
    {
        List<Vector2Int> crates = new List<Vector2Int>();

        foreach (Vector2Int cord in points)
        {
            if (Vector2Int.Distance(spawn, cord) >= minDistance)
            {
                if (Random.value <= pointDistributionPerc)
                {
                    map[cord.x, cord.y] = 4;
                }
            }
        }
    }
}
