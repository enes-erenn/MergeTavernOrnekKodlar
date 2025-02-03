using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Algorithm
{
    /// <summary>
    /// RandomWalk algorithm starts from a position and it randomly moves to its neighbors. After it is clamped the algorithm finishes.
    /// But in this version, clamping is prevented. If it clamps then start in a random available tile.
    /// </summary>
    public class RandomWalker
    {
        public static List<Vector2> Tiles, ClampedTiles, UnsettledTiles, Directions;

        public event Action<Output> OnComplete;

        static Vector2 MapSize;
        static Tilemap Tilemap;
        static TileBase Tile;
        static TilemapBounds Bounds;

        static bool InsideBounds;
        static float FillPercentage;

        readonly int minMapSize = 10;

        public IEnumerator Initialize()
        {
            yield return HandleInitialize();

            Output output = GetOutput();

            OnComplete.Invoke(output);
        }

        /// <summary>
        /// Method prepares the <c>Random Walk</c> algorithm.
        /// <br/>
        /// 
        /// <br/>
        /// Map Size
        /// <br/>
        /// Amount of how many cells the tilemap will include. Example: 10,10 => 100 cells.
        /// <br/>
        /// 
        /// <br/>
        /// Fill Percentage
        /// <br/>
        /// Percentage of how many cells will be set from the max size. Example: 0.5 => 100 * 0.5 => 50.
        /// <br/>
        /// 
        /// <br/>
        /// Center
        /// <br/>
        /// Starting position of the tilemap.
        /// <br/>
        /// 
        /// <br/>
        /// Bounds
        /// <br/>
        /// Prevents to set tiles to the outside of the tilemap.
        /// <br/>
        ///
        /// <br/>
        /// Inside Bounds
        /// <br/>
        /// Check if you want to prevent tiles from being set outside of the map.
        /// <br/>
        /// </summary>
        public RandomWalker(Vector2 _mapSize, Tilemap _tileMap, TileBase _tile, float _fillPercentage, TilemapBounds bounds, bool insideBounds)
        {
            MapSize = _mapSize;
            Tilemap = _tileMap;
            Tile = _tile;
            FillPercentage = _fillPercentage;
            Bounds = bounds;
            InsideBounds = insideBounds;

            float fillPercentageAdj = _fillPercentage > 1 ? 1 : _fillPercentage < 0 ? 0.1f : _fillPercentage;
            FillPercentage = fillPercentageAdj;
        }

        IEnumerator HandleInitialize()
        {
            if (MapSize.x * MapSize.y < minMapSize)
            {
                Debug.Log("Map size is not enough to generate a tilemap!");
                yield break;
            }

            Tiles = new();
            ClampedTiles = new();
            Directions = new();
            UnsettledTiles = new();

            Vector3Int firstPos = Vector3Int.zero;

            // First tile from the starting position
            Tilemap.SetTile(firstPos, Tile);
            Tiles.Add(ToVector2(firstPos));

            yield return GenerateTiles();
        }

        Output GetOutput() => new()
        {
            Tiles = Tiles,
            ClampedTiles = ClampedTiles,
            UnsettledTiles = UnsettledTiles,
            Directions = Directions
        };

        /// <summary>
        /// Gives the direction offsets. Position Y is needed to calculate hexagonal offsets. Because offsets change according to the if hex is on the odd or even line
        /// </summary>
        /// <returns>Hexagonal offsets as array of Vector2</returns>
        public Vector2Int[] GetDirections(int gridY, bool shuffle = true)
        {
            Vector2Int[] directions = (gridY % 2 == 0) ? EvenDirections : OddDirections;

            if (shuffle)
                Shuffle(ref directions);

            return directions;
        }

        public static void Shuffle<T>(ref T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                // Pick a random index from 0 to i
                int j = UnityEngine.Random.Range(0, i + 1);

                // Swap array[i] with array[j]
                (array[j], array[i]) = (array[i], array[j]);
            }
        }

        /// <summary>
        /// Finds an unsettled (unused) tile withing the tilemap bounds
        /// </summary>
        /// <returns>A Vector2 cell position </returns>
        Vector2 GetUnSettledTile()
        {
            Vector2 nearbyPosition = Vector2.zero;

            for (int i = 0; i < Tiles.Count; i++)
            {
                Vector2 pos = Tiles[i];
                Vector2Int[] directions = GetDirections((int)pos.y);

                for (int d = 0; d < directions.Length; d++)
                {
                    Vector2 currNearbyPosition = pos + directions[d];

                    if (!Tiles.Contains(currNearbyPosition) && IsWithinBounds(ToVector3Int(currNearbyPosition)))
                    {
                        if (FillPercentage > 0.7f)
                        {
                            nearbyPosition = currNearbyPosition;
                            break;
                        }

                        if (AvailableNearbyTileCount(currNearbyPosition) >= 2)
                        {
                            nearbyPosition = currNearbyPosition;
                            break;
                        }
                    }
                }

                if (!UnsettledTiles.Contains(nearbyPosition))
                    UnsettledTiles.Add(nearbyPosition);
            }

            return nearbyPosition;
        }

        /// <summary>
        /// Count of how many available (unused before) tiles there are for the nearbyPosition
        /// </summary>
        /// <param name="nearbyPosition"></param>
        /// <returns>Count of available tiles for the given tile position</returns>
        int AvailableNearbyTileCount(Vector2 nearbyPosition)
        {
            int count = 0;

            int lastPos = (int)Tiles[^1].y;

            Vector2Int[] directions = GetDirections(lastPos);

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2 currNearbyPosition = nearbyPosition + directions[i];
                Vector3Int tilePos = ToVector3Int(currNearbyPosition);

                if (Tiles.Contains(currNearbyPosition))
                    continue;
                else if (IsWithinBounds(tilePos))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// It gives a tile position whatever the conditions the are:
        /// <br/>
        /// 
        /// <br/>
        /// Condition 1
        /// <br/>
        /// Tile may be clamped, and there may be zero direction to move. In that case, it finds an available neighbor tile of the used tiles
        /// <br/>
        /// 
        /// <br/>
        /// Condition 2
        /// <br/>
        /// Tile may be on the edge of map, and there may be zero direction to move. In that case, it finds an available neighbor tile of the used tiles
        /// <br/>
        /// 
        /// </summary>
        /// <param name="directions"></param>
        /// <returns>A tile position</returns>
        Vector3Int GetTilePosition(ref Vector2Int[] directions)
        {
            Vector3Int tilePos = Vector3Int.zero;

            for (int i = 0; i < directions.Length; i++)
            {
                // Last tile position + new direction = next position
                Vector2 nextTile = Tiles[^1] + directions[i];
                Vector3Int currTilePos = ToVector3Int(nextTile);

                bool isWithinBounds = !InsideBounds || IsWithinBounds(currTilePos);
                bool isAlreadyContains = Tiles.Contains(nextTile);

                // If new tile position is not used befaore and also within the bounds
                if (isAlreadyContains || !isWithinBounds)
                {
                    // If it is the last iteration of the loop
                    if (i == directions.Length - 1)
                    {
                        // Tile is clamped
                        ClampedTiles.Add(nextTile);
                        // We need to find an unused tile that neighbor of the used tiles
                        tilePos = ToVector3Int(GetUnSettledTile());
                    }
                    else continue; // Tile position is used already or not within the bounds, so we can't use this one but there are other directions to check!

                    break;
                }

                tilePos = currTilePos;
                Directions.Add(directions[i]);
                break;
            }

            return tilePos;
        }

        int GetMapLength()
        {
            // In a box, there will be a shifted row in every two rows, and shifted rows will include half hexagons.
            float halfHexagons = MapSize.y / 2;

            return (int)Math.Floor((MapSize.x * MapSize.y) - halfHexagons);
        }


        IEnumerator Timer(float duration)
        {
            yield return new WaitForSeconds(duration);
        }

        IEnumerator GenerateTiles()
        {
            int mapLength = GetMapLength();

            while (Tiles.Count / (float)mapLength < FillPercentage)
            {
                yield return Timer(0.1f);

                int lastPos = (int)Tiles[^1].y;

                Vector2Int[] directions = GetDirections(lastPos);

                Vector3Int tilePos = GetTilePosition(ref directions);

                bool isWithinBounds = IsWithinBounds(tilePos);

                Tilemap.SetTile(tilePos, Tile);

                if (!InsideBounds && !isWithinBounds)
                    Tilemap.SetColor(tilePos, Color.red);

                Tiles.Add(ToVector2(tilePos));
            }
        }

        bool IsWithinBounds(Vector3Int pos)
        {
            Vector3 newPos = Tilemap.CellToWorld(pos);

            return newPos.x - 0.1f > Bounds.Left && newPos.x + 0.1f < Bounds.Right &&
                   newPos.y > Bounds.Bottom && newPos.y < Bounds.Top;
        }

        // Normally pos.y should be on z axis but here in the tilemap pos.y corresponds to y axis
        public Vector3Int ToVector3Int(Vector2 pos)
        {
            return new((int)pos.x, (int)pos.y, 0);
        }

        Vector2 ToVector2(Vector3 pos)
        {
            return new(pos.x, pos.y);
        }

        // Directions that a hexagon can move to
        // Directions change according to the hexagon tile's row and also hexagon's type like: flat top or point top
        static readonly Vector2Int[] EvenDirections =
        {
            new (0, -1), // Right Top
            new (1, 0),  // Right
            new (0, 1),  // Right Bottom
            new (-1, 1), // Left Bottom
            new (-1, 0), // Left 
            new (-1, -1) // Left Top
        };

        static readonly Vector2Int[] OddDirections =
        {
            new (1, -1),
            new (1, 0),
            new (1, 1),
            new (0, 1),
            new (-1, 0),
            new (0, -1)
        };
    }

    [Serializable]
    public class TilemapBounds
    {
        public float Top;
        public float Bottom;
        public float Left;
        public float Right;
    }

    [Serializable]
    public class Output
    {
        /// <summary>
        /// Tile positions that are in the tilemap.
        /// </summary>
        public List<Vector2> Tiles { get; set; }
        /// <summary>
        /// Clamped tiles are those that have no available neighbor to move with.
        /// </summary>
        public List<Vector2> ClampedTiles { get; set; }
        /// <summary>
        /// Unsettled tiles are after a tile is clamped, it finds a random tile with no tile.
        /// </summary>
        public List<Vector2> UnsettledTiles { get; set; }
        public List<Vector2> Directions { get; set; }
    }
}

// Resources that I used to create this algorithm:
// https://www.redblobgames.com/grids/hexagons/
// https://github.com/GarnetKane99/RandomWalkerAlgo_YT/blob/main/Youtube%20-%20Random%20Walker/Assets/Scripts/WalkerGenerator.cs
