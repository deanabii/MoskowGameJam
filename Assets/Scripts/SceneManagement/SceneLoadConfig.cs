using System.Collections.Generic;
using UnityEngine;

namespace MoskowGameJam.SceneManagement
{
    [CreateAssetMenu(fileName = "SceneLoadConfig", menuName = "Scene Management/Scene Load Config")]
    public class SceneLoadConfig : ScriptableObject
    {
        [Header("Scene Settings")]
        [Tooltip("Daftar nama scene yang akan di-load secara Additive")]
        public List<string> scenesToLoad = new List<string>();

        [Tooltip("Nama scene yang akan dijadikan Active Scene setelah di-load (Opsional)")]
        public string activeSceneName;

        [Tooltip("Apakah proses loading menggunakan Asynchronous?")]
        public bool loadAsynchronously = true;
    }
}
