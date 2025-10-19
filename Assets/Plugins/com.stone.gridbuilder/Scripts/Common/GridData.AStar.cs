using System;
using System.Collections.Generic;
using MemoryPack;

namespace ST.GridBuilder
{
    public partial class GridData
    {
        [MemoryPackIgnore] private PriorityQueue<CellData, int> frontier = new ();
        [MemoryPackIgnore] private HashSet<CellData> visited = new ();
        
        public bool FindPath(CellData start, CellData end, List<CellData> results)
        {
            ClearAStarData();
            
            frontier.Enqueue(start, 0);
            visited.Add(start);

            start.distance = 0;
            start.prev = null;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current == end) {
                    break;
                }
                
                int cost = current.distance + 1;
                if (current.index.x > 0) {
                    CellData neighbour = cells[current.index.x - 1 + current.index.z * xLength];
                    if (!neighbour.IsFill) {
                        if (neighbour.distance > cost) {
                            neighbour.distance = cost;
                            neighbour.prev = current;
                        }
                        if (!visited.Contains(neighbour)) {
                            frontier.Enqueue(neighbour, cost + Heuristic(neighbour, end));
                            visited.Add(neighbour);
                        }
                    }
                }
                if (current.index.x < xLength - 1) {
                    CellData neighbour = cells[current.index.x + 1 + current.index.z * xLength];
                    if (!neighbour.IsFill) {
                        if (neighbour.distance > cost) {
                            neighbour.distance = cost;
                            neighbour.prev = current;
                        }
                        if (!visited.Contains(neighbour)) {
                            frontier.Enqueue(neighbour, cost + Heuristic(neighbour, end));
                            visited.Add(neighbour);
                        }
                    }
                }
                if (current.index.z > 0) {
                    CellData neighbour = cells[current.index.x + (current.index.z - 1) * xLength];
                    if (!neighbour.IsFill) {
                        if (neighbour.distance > cost) {
                            neighbour.distance = cost;
                            neighbour.prev = current;
                        }
                        if (!visited.Contains(neighbour)) {
                            frontier.Enqueue(neighbour, cost + Heuristic(neighbour, end));
                            visited.Add(neighbour);
                        }
                    }
                }
                if (current.index.z < zLength - 1) {
                    CellData neighbour = cells[current.index.x + (current.index.z + 1) * xLength];
                    if (!neighbour.IsFill) {
                        if (neighbour.distance > cost) {
                            neighbour.distance = cost;
                            neighbour.prev = current;
                        }
                        if (!visited.Contains(neighbour)) {
                            frontier.Enqueue(neighbour, cost + Heuristic(neighbour, end));
                            visited.Add(neighbour);
                        }
                    }
                }
            }

            return BacktrackToPath(end, results);
        }
        
        private void ClearAStarData()
        {
            foreach (var cell in cells) {
                cell.distance = int.MaxValue;
                cell.prev = null;
            }
            frontier.Clear();
            visited.Clear();
        }
        
        private bool BacktrackToPath(CellData end, List<CellData> results)
        {
            results.Clear();
            
            CellData current = end;
            while (current != null)
            {
                results.Add(current);
                current = current.prev;
            }

            results.Reverse();
            return results.Count > 0;
        }
        
        private int Heuristic(CellData a, CellData b)
        {
            return Math.Abs(a.index.x - b.index.x) + Math.Abs(a.index.z - b.index.z);
        }
    }
}