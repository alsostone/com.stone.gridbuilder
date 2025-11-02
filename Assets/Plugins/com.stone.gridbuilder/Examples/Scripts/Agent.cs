using UnityEngine;

namespace ST.GridBuilder
{
    public class Agent : MonoBehaviour
    {
        private GridMap gridMap;
        private const float Speed = 2f;

        private void Start()
        {
            gridMap = FindObjectOfType<GridMap>();
        }

        // Update is called once per frame
        void Update()
        {
            var position = transform.position;
            var vector = gridMap.GetFieldVector(position);
            if (vector.sqrMagnitude > 0.01f)
            {
                transform.position += vector.normalized * (Speed * Time.deltaTime);
            }
        }
    }
}
