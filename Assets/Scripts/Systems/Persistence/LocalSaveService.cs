using System.IO;
using UnityEngine;

namespace AINPC.Systems.Persistence
{
    public static class LocalSaveService
    {
        private static string Root =>
            Path.Combine(Application.persistentDataPath, "saves");

        public static void Save(string key, string json)
        {
            if (!Directory.Exists(Root))
                Directory.CreateDirectory(Root);

            File.WriteAllText(Path.Combine(Root, key + ".json"), json);
        }

        public static string Load(string key)
        {
            string path = Path.Combine(Root, key + ".json");
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
    }
}
