using UnityEngine;
using Random = UnityEngine.Random;

namespace ST.GridBuilder
{
    public class AgentSpawner : MonoBehaviour
    {
        public GameObject agentPrefab;
        private GridBuilder gridBuilder;

        private void Start()
        {
            gridBuilder = FindObjectOfType<GridBuilder>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector3 position = new Vector3(Random.Range(-25, 25), 0, Random.Range(-25, 25));
                    Instantiate(agentPrefab, position, Quaternion.identity);
                }
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                GridMap gridMap = gridBuilder.RaycastGridMap(Input.mousePosition);
                if (gridMap && gridBuilder.RaycastTerrain(Input.mousePosition, out var pos))
                {
                    gridMap.SetDestination(pos);
                }
            }
        }
    }
}
