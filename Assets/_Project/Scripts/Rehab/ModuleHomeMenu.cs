using UnityEngine;
using UnityEngine.SceneManagement;

namespace PicoElderCare.Rehab
{
    public class ModuleHomeMenu : MonoBehaviour
    {
        public string mainEntrySceneName = "00_MainEntry";

        public void LoadMainEntry()
        {
            SceneManager.LoadScene(mainEntrySceneName);
        }
    }
}
