using UnityEngine;
using UnityEngine.SceneManagement;

namespace PicoElderCare.Rehab
{
    public class UnifiedEntryMenu : MonoBehaviour
    {
        public string pingPongSceneName = "01_PingPongDemo";
        public string rehabSceneName = "MR_Rehab_Main";

        public void LoadPingPong()
        {
            SceneManager.LoadScene(pingPongSceneName);
        }

        public void LoadRehab()
        {
            SceneManager.LoadScene(rehabSceneName);
        }
    }
}
