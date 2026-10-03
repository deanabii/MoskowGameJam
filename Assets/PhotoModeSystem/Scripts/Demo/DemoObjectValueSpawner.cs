using UnityEngine;
using MoskowGameJam.Interaction;

namespace PhotoModeSystem
{
    public class DemoObjectValueSpawner : MonoBehaviour
    {
        [Header("Demo Objects Setup")]
        [Tooltip("Otomatis buat 3 objek 3D sampel dengan ObjectValue jika scene belum memiliki ObjectValue.")]
        public bool spawnDemoObjectsIfEmpty = true;

        private void Start()
        {
            if (spawnDemoObjectsIfEmpty)
            {
                ObjectValue[] existing = FindObjectsOfType<ObjectValue>();
                if (existing.Length == 0)
                {
                    SpawnSampleObjects();
                }
            }
        }

        public void SpawnSampleObjects()
        {
            // Object 1: Golden Box (Value = 150)
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Demo_GoldenBox";
            cube.transform.position = new Vector3(-2f, 1f, 5f);
            cube.GetComponent<Renderer>().material.color = new Color(1f, 0.84f, 0f);
            ObjectValue ov1 = cube.AddComponent<ObjectValue>();
            ov1.objectName = "Golden Box";
            ov1.valueAmount = 150f;

            // Object 2: Red Crystal (Value = 250)
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Demo_RedCrystal";
            sphere.transform.position = new Vector3(0f, 1.5f, 6f);
            sphere.GetComponent<Renderer>().material.color = Color.red;
            ObjectValue ov2 = sphere.AddComponent<ObjectValue>();
            ov2.objectName = "Red Crystal";
            ov2.valueAmount = 250f;

            // Object 3: Ancient Statue (Value = 400)
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Demo_AncientStatue";
            cylinder.transform.position = new Vector3(2f, 1.2f, 5f);
            cylinder.GetComponent<Renderer>().material.color = new Color(0.3f, 0.7f, 0.9f);
            ObjectValue ov3 = cylinder.AddComponent<ObjectValue>();
            ov3.objectName = "Ancient Statue";
            ov3.valueAmount = 400f;

            Debug.Log("[DemoObjectValueSpawner] 3 Objek Sampel Demo (Golden Box, Red Crystal, Ancient Statue) berhasil dibuat!");
        }
    }
}
