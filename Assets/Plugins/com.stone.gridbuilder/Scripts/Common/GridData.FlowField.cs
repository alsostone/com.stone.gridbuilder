using System;
using System.Collections.Generic;
using MemoryPack;

namespace ST.GridBuilder
{
    public partial class GridData
    {
        [MemoryPackInclude] private List<FlowFieldNode[]> flowFields = new List<FlowFieldNode[]>();
        [MemoryPackInclude] private Stack<int> freeFlowField = new Stack<int>();
        [MemoryPackIgnore] private Queue<CellData> flowFieldVisited = new Queue<CellData>();

        public FlowFieldNode[] GetFlowField(int index)
        {
            if (index < 0 || index >= flowFields.Count)
                return null;
            return flowFields[index];
        }
        
        public void ReleaseFlowField(int index)
        {
            if (index < 0 || index >= flowFields.Count)
                return;
            freeFlowField.Push(index);
        }
        
        public FieldV2 GetFieldVector(int flowFieldIndex, FieldV2 position)
        {
            IndexV2 indexCurrent = ConvertToIndex(position);
            if (indexCurrent.x < 0 || indexCurrent.x >= xLength || indexCurrent.z < 0 || indexCurrent.z >= zLength) {
                return new FieldV2(0, 0);
            }
            if (flowFieldIndex < 0 || flowFieldIndex >= flowFields.Count) {
                return new FieldV2(0, 0);
            }
            
            FlowFieldNode[] flowField = flowFields[flowFieldIndex];
            FieldV2 v1 = new FieldV2(0, 0);
            FieldV2 v2 = new FieldV2(0, 0);
            float half = cellSize / 2;
            
            int xLeft = (int)((position.x - half) / cellSize);
            int xRight = (int)((position.x + half) / cellSize);
            if (xLeft >= 0 && xLeft < xLength)
            {
                if (xRight >= 0 && xRight < xLength)
                {
                    FlowFieldNode left = flowField[xLeft + indexCurrent.z * xLength];
                    FlowFieldNode right = flowField[xRight + indexCurrent.z * xLength];
                
                    if (left.distance != int.MaxValue && right.distance != int.MaxValue)
                        v1 = left.direction.Lerp(right.direction, (position.x - (xLeft * cellSize + half)) / cellSize);
                    else if (left.distance != int.MaxValue)
                        v1 = left.direction;
                    else if (right.distance != int.MaxValue)
                        v1 = right.direction;
                }
                else
                {
                    FlowFieldNode left = flowField[xLeft + indexCurrent.z * xLength];
                    if (left.distance != int.MaxValue)
                        v1 = left.direction;
                }
            }
            else if (xRight >= 0 && xRight < xLength)
            {
                FlowFieldNode right = flowField[xRight + indexCurrent.z * xLength];
                if (right.distance != int.MaxValue)
                    v1 = right.direction;
            }
            
            
            int zTop = (int)((position.z + half) / cellSize);
            int zBottom = (int)((position.z - half) / cellSize);
            if (zTop >= 0 && zTop < zLength)
            {
                if (zBottom >= 0 && zBottom < zLength)
                {
                    FlowFieldNode top = flowField[indexCurrent.x + zTop * xLength];
                    FlowFieldNode bottom = flowField[indexCurrent.x + zBottom * xLength];
                
                    if (top.distance != int.MaxValue && bottom.distance != int.MaxValue)
                        v2 = top.direction.Lerp(bottom.direction, (position.z - (zTop * cellSize + half)) / cellSize);
                    else if (top.distance != int.MaxValue)
                        v2 = top.direction;
                    else if (bottom.distance != int.MaxValue)
                        v2 = bottom.direction;
                }
                else
                {
                    FlowFieldNode top = flowField[indexCurrent.x + zTop * xLength];
                    if (top.distance != int.MaxValue)
                        v2 = top.direction;
                }
            }
            else if (zBottom >= 0 && zBottom < zLength)
            {
                FlowFieldNode bottom = flowField[indexCurrent.x + zBottom * xLength];
                if (bottom.distance != int.MaxValue)
                    v2 = bottom.direction;
            }
            
            return v1.Lerp(v2, (position.z - (indexCurrent.z * cellSize + half)) / cellSize).Normalize();
        }

        public int GenerateFlowField(FieldV2 destination)
        {
            FlowFieldNode[] flowField;
            if (freeFlowField.TryPop(out int index)) {
                flowField = flowFields[index];
            } else {
                flowField = new FlowFieldNode[xLength * zLength];
                flowFields.Add(flowField);
                index = flowFields.Count - 1;
            }
            
            ResetDijkstraData(flowField, destination);
            GenerateDijkstraData(flowField);
            GenerateDirectionData(flowField, xLength, zLength);
            return index;
        }

        public int GenerateFlowField(List<FieldV2> destinations)
        {
            FlowFieldNode[] flowField;
            if (freeFlowField.TryPop(out int index)) {
                flowField = flowFields[index];
            } else {
                flowField = new FlowFieldNode[xLength * zLength];
                flowFields.Add(flowField);
                index = flowFields.Count - 1;
            }
            
            ResetDijkstraData(flowField, destinations);
            GenerateDijkstraData(flowField);
            GenerateDirectionData(flowField, xLength, zLength);
            return index;
        }

        private void ResetDijkstraData(FlowFieldNode[] flowField, FieldV2 destination)
        {
            Array.Fill(flowField, new FlowFieldNode { distance = int.MaxValue, direction = new FieldV2(0, 0)});
            flowFieldVisited.Clear();
            
            IndexV2 indexV2 = ConvertToIndex(new FieldV2(destination.x, destination.z));
            indexV2 = GetValidDest(indexV2);
            
            int index = indexV2.x + indexV2.z * xLength;
            flowField[index].distance = 0;
            flowFieldVisited.Enqueue(cells[index]);
        }

        private void ResetDijkstraData(FlowFieldNode[] flowField, List<FieldV2> destinations)
        {
            Array.Fill(flowField, new FlowFieldNode { distance = int.MaxValue, direction = new FieldV2(0, 0)});
            flowFieldVisited.Clear();
            
            foreach (FieldV2 dest in destinations)
            {
                IndexV2 indexV2 = ConvertToIndex(new FieldV2(dest.x, dest.z));
                indexV2 = GetValidDest(indexV2);
                
                int index = indexV2.x + indexV2.z * xLength;
                flowField[index].distance = 0;
                flowFieldVisited.Enqueue(cells[index]);
            }
        }

        private void GenerateDijkstraData(FlowFieldNode[] flowField)
        {
            while (flowFieldVisited.Count > 0)
            {
                CellData current = flowFieldVisited.Dequeue();
                int distance = flowField[current.index.x + current.index.z * xLength].distance + 1;
                
                if (current.index.x > 0) {
                    int index = (current.index.x - 1) + current.index.z * xLength;
                    CellData neighbour = cells[index];
                    if (!neighbour.IsFill && flowField[index].distance == int.MaxValue) {
                        flowField[index].distance = distance;
                        flowFieldVisited.Enqueue(neighbour);
                    }
                }
                if (current.index.x < xLength - 1) {
                    int index = (current.index.x + 1) + current.index.z * xLength;
                    CellData neighbour = cells[index];
                    if (!neighbour.IsFill && flowField[index].distance == int.MaxValue) {
                        flowField[index].distance = distance;
                        flowFieldVisited.Enqueue(neighbour);
                    }
                }
                if (current.index.z > 0) {
                    int index = current.index.x + (current.index.z - 1) * xLength;
                    CellData neighbour = cells[index];
                    if (!neighbour.IsFill && flowField[index].distance == int.MaxValue) {
                        flowField[index].distance = distance;
                        flowFieldVisited.Enqueue(neighbour);
                    }
                }
                if (current.index.z < zLength - 1) {
                    int index = current.index.x + (current.index.z + 1) * xLength;
                    CellData neighbour = cells[index];
                    if (!neighbour.IsFill && flowField[index].distance == int.MaxValue) {
                        flowField[index].distance = distance;
                        flowFieldVisited.Enqueue(neighbour);
                    }
                }
            }
        }

        private void GenerateDirectionData(FlowFieldNode[] flowField, int xLen, int zLen)
        {
            for (int x = 0; x < xLen; x++)
            for (int z = 0; z < zLen; z++)
            {
                int index = x + z * xLen;
                FlowFieldNode node = flowField[index];
                if (node.distance == 0 || node.distance == int.MaxValue)
                    continue;
                
                int distance = int.MaxValue;
                for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;
                    
                    int nx = x + dx;
                    int nz = z + dz;
                    if (nx < 0 || nz < 0 || nx >= xLen || nz >= zLen)
                        continue;

                    if (dx * dz != 0) {
                        FlowFieldNode diagonal1 = flowField[nx + z * xLen];
                        if (diagonal1.distance == int.MaxValue) 
                            continue;
                        FlowFieldNode diagonal2 = flowField[x + nz * xLen];
                        if (diagonal2.distance == int.MaxValue) 
                            continue;
                    }
                    
                    FlowFieldNode neighbour = flowField[nx + nz * xLen];
                    if (neighbour.distance >= distance)
                        continue;

                    distance = neighbour.distance;
                    flowField[index].direction = new FieldV2(nx - x, nz - z);
                }
            }
        }
        
    }
}