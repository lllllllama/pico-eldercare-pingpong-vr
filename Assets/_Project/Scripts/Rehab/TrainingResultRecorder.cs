using System.IO;
using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class TrainingResultRecorder : MonoBehaviour
    {
        public string resultFolderName = "RehabResults";

        public string LastSavedPath { get; private set; }

        public string SaveResult(RehabTrainingResult result)
        {
            if (result == null)
            {
                Debug.LogWarning("Rehab result was null and was not saved.");
                return string.Empty;
            }

            var folder = Path.Combine(Application.persistentDataPath, resultFolderName);
            Directory.CreateDirectory(folder);

            var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = string.Format("rehab_session_{0}_{1}.json", timestamp, SanitizeFilePart(result.sessionId));
            var path = Path.Combine(folder, fileName);
            var json = JsonUtility.ToJson(result, true);

            File.WriteAllText(path, json);
            LastSavedPath = path;
            Debug.Log("Rehab result saved: " + path);
            return path;
        }

        private static string SanitizeFilePart(string value)
        {
            if (string.IsNullOrEmpty(value)) return "unknown";

            var invalidChars = Path.GetInvalidFileNameChars();
            for (var i = 0; i < invalidChars.Length; i++)
            {
                value = value.Replace(invalidChars[i], '_');
            }

            return value;
        }
    }
}
