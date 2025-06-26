using System;
using System.Collections.Generic;
using MemoryPack;

namespace ST.GridBuilder
{
    [MemoryPackable]
    [Serializable]
    public partial class CellData
    {
        [MemoryPackIgnore] public bool IsFill => isObstacle || contentIds.Count > 0;
    
        [MemoryPackInclude] public List<long> contentIds;
        [MemoryPackInclude] public List<PlacedLayer> contentTypes;
        [MemoryPackInclude] public bool isObstacle;
        [MemoryPackInclude] public IndexV2 index = new IndexV2(-1, -1);
        [MemoryPackInclude] public IndexV2 parent = new IndexV2(-1, -1);
        [MemoryPackInclude] public int distance = int.MaxValue;

        public CellData()
        {
            contentIds = new List<long>();
            contentTypes = new List<PlacedLayer>();
        }
    
        [MemoryPackConstructor]
        public CellData(List<long> contentIds, List<PlacedLayer> contentTypes)
        {
            this.contentIds = contentIds;
            this.contentTypes = contentTypes;
        }
    
        public bool CanPut(PlacementData placementData)
        {
            if (isObstacle) {
                return false;
            }
        
            if (contentIds.Count > 0) {
                if (contentIds[^1] == placementData.id)
                    return true;
                if ((placementData.placedLayer & contentTypes[^1]) == 0)
                    return false;
            } else {
                if ((placementData.placedLayer & PlacedLayer.Map) == 0)
                    return false;
            }
            return true;
        }
    }

}
