using System;
using MemoryPack;

namespace ST.GridBuilder
{
    [MemoryPackable]
    [Serializable]
    public partial struct IndexV2
    {
        [MemoryPackInclude] public int x;
        [MemoryPackInclude] public int z;

        public IndexV2(int x,int z)
        {
            this.x = x;
            this.z = z;
        }
        
        public bool Equals(IndexV2 other)
        {
            return x == other.x && z == other.z;
        }
        public override bool Equals(object obj)
        {
            return obj is IndexV2 other && Equals(other);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(x, z);
        }
    }
    
    [MemoryPackable]
    [Serializable]
    public partial class GridData
    {
        [MemoryPackInclude] public int xPosition = 0;
        [MemoryPackInclude] public int zPosition = 0;
        [MemoryPackInclude] public int xLength = 16;
        [MemoryPackInclude] public int zLength = 16;
        [MemoryPackInclude] public float cellSize = 1;
        [MemoryPackInclude] public int blockLevelMax = 2;

        [MemoryPackInclude] public CellData[] cells;
        [MemoryPackInclude] public int currentGuid = 0;

        public void ResetCells()
        {
            currentGuid = 0;
            if (cells == null || xLength * zLength != cells.Length)
            {
                cells = new CellData[xLength * zLength];
                for (int x = 0; x < xLength; x++)
                for (int z = 0; z < zLength; z++)
                {
                    CellData data = new CellData { index = new IndexV2(x, z) };
                    cells[x + z * xLength] = data;
                }
            }
            else
            {
                for (int x = 0; x < xLength; x++)
                for (int z = 0; z < zLength; z++)
                {
                    CellData cellData = cells[x + z * xLength];
                    cellData.isObstacle = false;
                    cellData.contentIds.Clear();
                    cellData.contentTypes.Clear();
                }
            }
        }
        
        public IndexV2 ConvertToIndex(ref FieldV2 position)
        {
            return new IndexV2((int)((position.x - xPosition) / cellSize), (int)((position.z - zPosition) / cellSize));
        }
        
        public CellData GetCell(int x, int z)
        {
            if (x >= 0 && z >= 0 && x < xLength && z < zLength)
                return cells[x + z * xLength];
            return null;
        }
        
        public FieldV2 GetCellPosition(int x, int z)
        {
            return new FieldV2(cellSize * (x + 0.5f) + xPosition, cellSize * (z + 0.5f) + zPosition);
        }

        public bool IsInside(int x, int z)
        {
            return x >= 0 && z >= 0 && x < xLength && z < zLength;
        }

        public long GetNextGuid()
        {
            return ++currentGuid;
        }

        public bool CanTake(PlacementData placementData)
        {
            for (int x1 = 0; x1 < PlacementData.width; x1++)
            for (int z1 = 0; z1 < PlacementData.height; z1++)
            {
                if (placementData.points[x1 + z1 * PlacementData.width])
                {
                    int x2 = placementData.x + x1 - PlacementData.xOffset;
                    int z2 = placementData.z + z1 - PlacementData.zOffset;
                    if (!IsInside(x2, z2))
                    {
                        return false;
                    }

                    CellData data = cells[x2 + z2 * xLength];
                    if (data.contentIds.Count == 0)
                    {
                        return false;
                    }

                    if (data.contentIds[^1] != placementData.id)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void Take(PlacementData placementData)
        {
            for (int x1 = 0; x1 < PlacementData.width; x1++)
            for (int z1 = 0; z1 < PlacementData.height; z1++)
            {
                if (placementData.points[x1 + z1 * PlacementData.width])
                {
                    int x2 = placementData.x + x1 - PlacementData.xOffset;
                    int z2 = placementData.z + z1 - PlacementData.zOffset;
                    CellData cellData = cells[x2 + z2 * xLength];
                    cellData.contentIds.RemoveAt(cellData.contentIds.Count - 1);
                    cellData.contentTypes.RemoveAt(cellData.contentTypes.Count - 1);
                }
            }
        }

        public bool CanPut(int x, int z, PlacementData placementData)
        {
            int level = -1;
            for (int x1 = 0; x1 < PlacementData.width; x1++)
            for (int z1 = 0; z1 < PlacementData.height; z1++)
            {
                if (placementData.points[x1 + z1 * PlacementData.width])
                {
                    int x2 = x + x1 - PlacementData.xOffset;
                    int z2 = z + z1 - PlacementData.zOffset;
                    CellData data = GetCell(x2, z2);
                    if (data == null || !data.CanPut(placementData))
                    {
                        return false;
                    }

                    int count = data.contentIds.Count;
                    if (data.contentIds.IndexOf(placementData.id) != -1)
                    {
                        count -= 1;
                    }

                    if (!CanPutLevel(count, placementData))
                    {
                        return false;
                    }

                    if (level == -1)
                    {
                        level = count;
                    }
                    else if (count != level)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void Put(int x, int z, PlacementData placementData)
        {
            for (int x1 = 0; x1 < PlacementData.width; x1++)
            for (int z1 = 0; z1 < PlacementData.height; z1++)
            {
                if (placementData.points[x1 + z1 * PlacementData.width])
                {
                    int x2 = x + x1 - PlacementData.xOffset;
                    int z2 = z + z1 - PlacementData.zOffset;
                    CellData cellData = cells[x2 + z2 * xLength];
                    cellData.contentIds.Add(placementData.id);
                    cellData.contentTypes.Add(placementData.placementType);
                }
            }

            placementData.isNew = false;
            placementData.x = x;
            placementData.z = z;
        }

        public bool IsInsideShape(int xOffset, int zOffset, PlacementData placementData)
        {
            int x = xOffset + PlacementData.xOffset;
            int z = zOffset + PlacementData.zOffset;
            if (x < 0 || x >= PlacementData.width || z < 0 || z >= PlacementData.height)
            {
                return false;
            }

            return placementData.points[x + z * PlacementData.width];
        }

        public bool CanPutLevel(int level, PlacementData placementData)
        {
            if ((placementData.placementType & PlacedLayer.Tower) == PlacedLayer.Tower)
                return true;
            if (blockLevelMax == -1)
                return true;
            return blockLevelMax > level;
        }

        public int GetShapeLevelCount(int x, int z, PlacementData placementData)
        {
            int level = (placementData.placedLayer & PlacedLayer.Map) == PlacedLayer.Map ? 0 : 1;
            for (int x1 = 0; x1 < PlacementData.width; x1++)
            {
                for (int z1 = 0; z1 < PlacementData.height; z1++)
                {
                    if (placementData.points[x1 + z1 * PlacementData.width])
                    {
                        int x2 = x + x1 - PlacementData.xOffset;
                        int z2 = z + z1 - PlacementData.zOffset;
                        level = Math.Max(level, GetPointLevelCount(x2, z2, placementData));
                    }
                }
            }

            return level;
        }

        public int GetPointLevelCount(int x, int z, PlacementData placementData)
        {
            if (!IsInside(x, z)) return 0;

            int blockLevel = 0;
            CellData data = cells[x + z * xLength];
            for (int i = 0; i < data.contentTypes.Count; i++)
            {
                PlacedLayer placedLayer = data.contentTypes[i];
                if (data.contentIds[i] != placementData.id && (placedLayer & PlacedLayer.Block) == PlacedLayer.Block)
                {
                    blockLevel++;
                }
            }

            return blockLevel;
        }

        public void SetObstacle(int x, int z, bool isObstacle)
        {
            if (IsInside(x, z))
            {
                cells[x + z * xLength].isObstacle = isObstacle;
            }
        }
    }
}