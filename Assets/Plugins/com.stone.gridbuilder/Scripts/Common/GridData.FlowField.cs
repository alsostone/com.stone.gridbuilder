using System;
using System.Collections.Generic;
using MemoryPack;

namespace ST.GridBuilder
{
    public partial class GridData
    {
        [MemoryPackIgnore] private Queue<CellData> visit = new Queue<CellData>();

        public void ResetFlowField()
        {
            ClearDijkstraData();
            IndexV2 dest = GetValidDestination(new IndexV2(xLength / 2, zLength / 2));
            GenerateDijkstraData(dest);
            GenerateFlowField();
        }
        
        private void ClearDijkstraData()
        {
            foreach (var cell in cells)
            {
                cell.parent = new IndexV2(-1, -1);
                cell.distance = int.MaxValue;
            }
            visit.Clear();
        }
        
        private void GenerateDijkstraData(IndexV2 destination)
        {
            CellData dest = GetCell(destination.x, destination.z);
            if (dest == null || dest.IsFill) {
                return;
            }

            dest.distance = 0;
            visit.Enqueue(dest);
            
            while (visit.Count > 0)
            {
                CellData current = visit.Dequeue();
                
                if (current.index.x > 0) {
                    CellData neighbour = cells[current.index.x - 1 + current.index.z * xLength];
                    if (!neighbour.IsFill && neighbour.distance == int.MaxValue) {
                        neighbour.distance = current.distance + 1;
                        visit.Enqueue(neighbour);
                    }
                }
                if (current.index.x < xLength - 1) {
                    CellData neighbour = cells[current.index.x + 1 + current.index.z * xLength];
                    if (!neighbour.IsFill && neighbour.distance == int.MaxValue) {
                        neighbour.distance = current.distance + 1;
                        visit.Enqueue(neighbour);
                    }
                }
                if (current.index.z > 0) {
                    CellData neighbour = cells[current.index.x + (current.index.z - 1) * xLength];
                    if (!neighbour.IsFill && neighbour.distance == int.MaxValue) {
                        neighbour.distance = current.distance + 1;
                        visit.Enqueue(neighbour);
                    }
                }
                if (current.index.z < zLength - 1) {
                    CellData neighbour = cells[current.index.x + (current.index.z + 1) * xLength];
                    if (!neighbour.IsFill && neighbour.distance == int.MaxValue) {
                        neighbour.distance = current.distance + 1;
                        visit.Enqueue(neighbour);
                    }
                }
            }
        }

        private void GenerateFlowField()
        {
            for (int x = 0; x < xLength; x++)
            for (int z = 0; z < zLength; z++)
            {
                CellData cell = cells[x + z * xLength];
                if (cell.distance == 0 || cell.distance == int.MaxValue)
                    continue;
                
                int distance = int.MaxValue;
                for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;
                    
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nz < 0 || nx >= xLength || nz >= zLength)
                        continue;

                    if (dx * dz != 0) {
                        CellData diagonal1 = cells[nx + z * xLength];
                        if (diagonal1.distance == int.MaxValue) 
                            continue;
                        CellData diagonal2 = cells[x + nz * xLength];
                        if (diagonal2.distance == int.MaxValue) 
                            continue;
                    }
                    
                    CellData neighbour = cells[nx + nz * xLength];
                    if (neighbour.distance >= distance)
                        continue;

                    distance = neighbour.distance;
                    cell.parent = new IndexV2(nx, nz);
                }
            }
        }
        
        private IndexV2 GetValidDestination(IndexV2 destination)
        {
            CellData dest = GetCell(destination.x, destination.z);
            if (dest == null || dest.IsFill)
            {
                // Find the nearest valid cell
                int minDistance = int.MaxValue;
                IndexV2 nearest = new IndexV2(-1, -1);
                for (int x = 0; x < xLength; x++)
                {
                    for (int z = 0; z < zLength; z++)
                    {
                        CellData cell = GetCell(x, z);
                        if (cell != null && !cell.IsFill)
                        {
                            int distance = Math.Abs(x - destination.x) + Math.Abs(z - destination.z);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                nearest = new IndexV2(x, z);
                            }
                        }
                    }
                }
                return nearest;
            }
            return destination;
        }
    }
}