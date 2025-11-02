using UnityEngine;
using UnityEngine.EventSystems;

namespace ST.GridBuilder
{
    public class GridBuilder : MonoBehaviour
    {
        [SerializeField] public Camera rayCamera;
        [SerializeField] public LayerMask gridMapMask;
        [SerializeField] public LayerMask terrainMask;
        [SerializeField] public GridMapIndicator gridMapIndicator;
        [SerializeField] public float raycastDistance = 1000.0f;
        
        private GridMap currentGridMap;
        private GridMap dragPlacementGridMap;
        private Placement dragPlacement;
        private Vector3 dragOffset;
        private int dragFingerId = -1;

        private void Awake()
        {
            if (rayCamera == null)
                rayCamera = Camera.main;
            
            currentGridMap = FindObjectOfType<GridMap>();
            if (gridMapIndicator == null) {
                gridMapIndicator = FindObjectOfType<GridMapIndicator>();
            }
            if (gridMapIndicator) {
                gridMapIndicator.SetGridMap(currentGridMap);
            }
        }

        private void Update()
        {
    #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;
                OnTouchBegin(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                OnTouchEnd(Input.mousePosition);
            }
    #else
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (dragFingerId == -1 && touch.phase == UnityEngine.TouchPhase.Began)
                {
                    if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                        continue;
                    if (OnTouchBegin(touch.position))
                        dragFingerId = touch.fingerId;
                }
                else if (touch.fingerId == dragFingerId && (touch.phase == UnityEngine.TouchPhase.Moved || touch.phase == UnityEngine.TouchPhase.Stationary))
                {
                    OnTouchMove(touch.position);
                }
                else if (touch.fingerId == dragFingerId && touch.phase == UnityEngine.TouchPhase.Ended)
                {
                    OnTouchEnd(touch.position);
                    dragFingerId = -1;
                }
            }
    #endif
            if (dragFingerId == -1)
            {
                OnTouchMove(Input.mousePosition);
            }
        }
        
        private bool OnTouchBegin(Vector3 touchPosition)
        {
            if (!dragPlacement && RaycastGridMap(touchPosition))
            {
                if (RaycastTarget(touchPosition, out GameObject target))
                {
                    Placement placement = target.GetComponent<Placement>();
                    if (!placement) {
                        return false;
                    }

                    Vector3 position = placement.GetPosition();
                    if (currentGridMap.gridData.CanTake(placement.placementData))
                    {
                        dragPlacementGridMap = currentGridMap;
                        dragPlacement = placement;
                        dragPlacement.SetPreviewMaterial();
                        RaycastTerrain(touchPosition, out Vector3 pos);
                        dragOffset = position - pos;
                        return true;
                    }
                    placement.DoShake();
                }
            }
            return false;
        }

        private void OnTouchMove(Vector3 touchPosition)
        {
            if (dragPlacement && RaycastGridMap(touchPosition))
            {
                if (RaycastTerrain(touchPosition, out Vector3 pos))
                {
                    IndexV2 index = currentGridMap.ConvertToIndex(pos + dragOffset);
                    int targetLevel = currentGridMap.gridData.GetShapeLevelCount(index.x, index.z, dragPlacement.placementData);
                    dragPlacement.SetPosition(currentGridMap.GetLevelPosition(index.x, index.z, targetLevel, dragPlacement.takeHeight));
                    dragPlacement.Rotation(0, currentGridMap.GetGridRotation());
                    if (gridMapIndicator) {
                        gridMapIndicator.GenerateIndicator(index.x, index.z, targetLevel, dragPlacement.placementData);
                    }
                }
            }
        }

        private void OnTouchEnd(Vector3 touchPosition)
        {
            if (dragPlacement && RaycastGridMap(touchPosition))
            {
                dragPlacement.ResetPreviewMaterial();
                if (RaycastTerrain(touchPosition, out Vector3 pos))
                {
                    IndexV2 index = currentGridMap.ConvertToIndex(pos + dragOffset);
                    if (currentGridMap.gridData.CanPut(index.x, index.z, dragPlacement.placementData))
                    {
                        if (dragPlacement.placementData.isNew)
                        {
                            dragPlacement.placementData.id = currentGridMap.gridData.GetNextGuid();
                            currentGridMap.Put(index.x, index.z, dragPlacement.placementData);
                        }
                        else if (index.x != dragPlacement.placementData.x || index.z != dragPlacement.placementData.z)
                        {
                            dragPlacementGridMap.Take(dragPlacement.placementData);
                            currentGridMap.Put(index.x, index.z, dragPlacement.placementData);
                        }
                        dragPlacement.SetPosition(currentGridMap.GetPutPosition(dragPlacement.placementData));
                    }
                    else {
                        if (dragPlacement.placementData.isNew) {
                            dragPlacement.Remove();
                        } else {
                            dragPlacement.SetPosition(currentGridMap.GetPutPosition(dragPlacement.placementData));
                        }
                    }
                } else {
                    if (dragPlacement.placementData.isNew) {
                        dragPlacement.Remove();
                    } else {
                        dragPlacement.SetPosition(currentGridMap.GetPutPosition(dragPlacement.placementData));
                    }
                }
                dragPlacementGridMap = null;
                dragPlacement = null;
                dragOffset = Vector3.zero;
                if (gridMapIndicator) {
                    gridMapIndicator.ClearIndicator();
                }
            }
        }
        
        public void SetPlacementObject(Placement placement)
        {
            if (dragPlacement)
            {
                if (dragPlacement.placementData.isNew) {
                    dragPlacement.Remove();
                } else {
                    dragPlacement.SetPosition(currentGridMap.GetPutPosition(dragPlacement.placementData));
                }
            }
            if (placement) {
                dragPlacementGridMap = currentGridMap;
                dragPlacement = placement;
                dragPlacement.ResetRotation(currentGridMap.GetGridRotation());
                dragPlacement.SetPreviewMaterial();
                dragOffset = Vector3.zero;
            }
        }

        public void RotatePlacementObject()
        {
            if (dragPlacement)
            {
                if (dragPlacement.placementData.isNew)
                {
                    dragPlacement.Rotation(1, currentGridMap.GetGridRotation());
                }
                else
                {
                    Debug.LogWarning("Cannot rotate a placement that is already placed.");
                }
            }
        }
        
        public void ClearPlacementObject()
        {
            if (dragPlacement)
            {
                dragPlacement.ResetPreviewMaterial();
                if (dragPlacement.placementData.isNew) {
                    dragPlacement.Remove();
                } else {
                    dragPlacement.SetPosition(currentGridMap.GetPutPosition(dragPlacement.placementData));
                }
                dragPlacementGridMap = null;
                dragPlacement = null;
                dragOffset = Vector3.zero;
            }
        }
        
        public GridMap RaycastGridMap(Vector3 position)
        {
            if (rayCamera == null)
                return null;
            
            Ray ray = rayCamera.ScreenPointToRay(position);
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, gridMapMask)) {
                currentGridMap = hit.transform.GetComponent<GridMap>();
                if (gridMapIndicator) {
                    gridMapIndicator.SetGridMap(currentGridMap);
                }
            }
            return currentGridMap;
        }
        
        public bool RaycastTerrain(Vector3 position, out Vector3 pos)
        {
            pos = default;
            
            if (rayCamera == null) {
                return false;
            }

            Ray ray = rayCamera.ScreenPointToRay(position);
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, terrainMask)) {
                pos = hit.point;
                return true;
            }

            return false;
        }

        private bool RaycastTarget(Vector3 position, out GameObject target)
        {
            target = null;
            
            if (rayCamera == null) {
                return false;
            }

            Ray ray = rayCamera.ScreenPointToRay(position);
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance)) {
                target = hit.collider.gameObject;
                return true;
            }
            return false;
        }

    #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (RaycastTerrain(Input.mousePosition, out Vector3 pos))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(rayCamera.transform.position, pos);
            }
        }
    #endif

    }
}