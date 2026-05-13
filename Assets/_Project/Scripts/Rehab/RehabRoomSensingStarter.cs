using UnityEngine;

namespace PicoElderCare.Rehab
{
    public class RehabRoomSensingStarter : MonoBehaviour
    {
        public bool logInEditor = true;

        private void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("Rehab room sensing managers are active. Spatial mesh data will update through the PICO XR mesh subsystem.");
#else
            if (logInEditor)
            {
                Debug.Log("Rehab room sensing is armed. PICO room data starts on a supported Android runtime.");
            }
#endif
        }
    }
}
