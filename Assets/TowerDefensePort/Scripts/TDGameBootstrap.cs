using UnityEngine;

namespace TowerDefensePort
{
    public static class TDGameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Object.FindFirstObjectByType<TDGameController>() != null)
            {
                return;
            }

            var root = new GameObject("TowerDefenseRuntime");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<TDGameController>();
        }
    }
}
