using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(GridMap))]
public class GridMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GridMap gridMap = target as GridMap;
        GUILayout.BeginHorizontal();
        GUILayout.Label("xLength", GUILayout.Width(EditorGUIUtility.labelWidth));
        gridMap.gridData.xLength = EditorGUILayout.IntSlider(gridMap.gridData.xLength, 16, 96);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("zLength", GUILayout.Width(EditorGUIUtility.labelWidth));
        gridMap.gridData.zLength = EditorGUILayout.IntSlider(gridMap.gridData.zLength, 16, 96);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Cell Size", GUILayout.Width(EditorGUIUtility.labelWidth));
        gridMap.gridData.cellSize = EditorGUILayout.Slider(gridMap.gridData.cellSize, 0.5f, 5.0f);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("block Level Max", GUILayout.Width(EditorGUIUtility.labelWidth));
        gridMap.gridData.blockLevelMax = EditorGUILayout.IntField(gridMap.gridData.blockLevelMax);
        GUILayout.EndHorizontal();
        
        if (GUILayout.Button("Force Refresh"))
        {
            GUI.changed = true;
        }
        
        if (GUI.changed)
        {
            gridMap.gridData.ResetCells();
            GenerateObstacle(gridMap);
            GenerateBuilding(gridMap);
            EditorUtility.SetDirty(gridMap);

            GridMapLines lines = FindObjectOfType<GridMapLines>();
            if (lines != null)
            {
                lines.GenerateLines();
                EditorUtility.SetDirty(lines);
            }
            GridMapTiles tiles = FindObjectOfType<GridMapTiles>();
            if (tiles != null)
            {
                tiles.GenerateTiles();
                EditorUtility.SetDirty(tiles);
            }
            
            if (!Application.isPlaying) {
                EditorSceneManager.MarkSceneDirty(gridMap.gameObject.scene);
            }
        }
    }
    
    private void GenerateObstacle(GridMap gridMap)
    {
        GridData gridData = gridMap.gridData;
        for (int x = 0; x < gridData.xLength; x++)
        {
            for (int z = 0; z < gridData.zLength; z++)
            {
                Vector3 pos = gridMap.GetCellPosition(x, z);
                pos.y = gridMap.raycastHeight;

                var offset = gridData.cellSize / 2 * gridMap.raycastFineness;
                if (Physics.Raycast(pos + new Vector3(-offset, 0, -offset), Vector3.down, out RaycastHit _, gridMap.raycastHeight, gridMap.obstacleMask))
                {
                    gridData.SetObstacle(x, z, true);
                }
                else if (Physics.Raycast(pos + new Vector3(offset, 0, -offset), Vector3.down, out RaycastHit _, gridMap.raycastHeight, gridMap.obstacleMask))
                {
                    gridData.SetObstacle(x, z, true);
                }
                else if (Physics.Raycast(pos + new Vector3(-offset, 0, offset), Vector3.down, out RaycastHit _, gridMap.raycastHeight, gridMap.obstacleMask))
                {
                    gridData.SetObstacle(x, z, true);
                }
                else if (Physics.Raycast(pos + new Vector3(offset, 0, offset), Vector3.down, out RaycastHit _, gridMap.raycastHeight, gridMap.obstacleMask))
                {
                    gridData.SetObstacle(x, z, true);
                }
            }
        }
    }

    private void GenerateBuilding(GridMap gridMap)
    {
        Placement[] buildings = FindObjectsOfType<Placement>();
        foreach (Placement building in buildings)
        {
            Vector3Int index = gridMap.ConvertToIndex(building.transform.position);
            if (!gridMap.gridData.CanPut(index.x, index.z, building.placementData)) {
                continue;
            }

            building.placementData.Id = gridMap.gridData.GetNextGuid(building.placementData);
            gridMap.gridData.Put(index.x, index.z, building.placementData);
            building.SetPutPosition(gridMap.GetPutPosition(building.placementData));
            EditorUtility.SetDirty(building);
        }
    }

}
